using System.Collections.Generic;
using UnityEngine;

namespace GameModes
{
    /// <summary>
    /// Manipulator mode - allows selecting and commanding NPCs/troops
    /// Handles unit selection, decision registration, and work assignment
    /// </summary>
    public class ManipulatorMode : MonoBehaviour
    {
        [Header("Selection Settings")]
        [Tooltip("Layer mask for selectable units")]
        [SerializeField] private LayerMask _selectableLayerMask;
        
        [Tooltip("Selection box line renderer prefab")]
        [SerializeField] private LineRenderer _selectionBoxPrefab;
        
        [Tooltip("Selected unit highlight material")]
        [SerializeField] private Material _selectionHighlightMaterial;

        [Header("Selection Visuals")]
        [Tooltip("Color for selection box")]
        [SerializeField] private Color _selectionBoxColor = Color.cyan;
        
        [Tooltip("Highlight color for selected units")]
        [SerializeField] private Color _highlightColor = Color.yellow;

        // Current selection
        private List<GameObject> _selectedUnits = new List<GameObject>();
        private bool _isBoxSelecting = false;
        private Vector3 _boxSelectStart;
        private LineRenderer _selectionBoxRenderer;

        // Camera reference
        private Camera _mainCamera;

        public IReadOnlyList<GameObject> SelectedUnits => _selectedUnits;

        private void Awake()
        {
            _mainCamera = Camera.main;
            
            // Create selection box renderer
            if (_selectionBoxPrefab != null)
            {
                _selectionBoxRenderer = Instantiate(_selectionBoxPrefab, transform);
                _selectionBoxRenderer.gameObject.SetActive(false);
                _selectionBoxRenderer.startColor = _selectionBoxColor;
                _selectionBoxRenderer.endColor = _selectionBoxColor;
            }
        }

        private void OnEnable()
        {
            Debug.Log("[ManipulatorMode] Enabled - Click to select units, Drag to box select");
        }

        private void OnDisable()
        {
            ClearSelection();
            if (_selectionBoxRenderer != null)
            {
                _selectionBoxRenderer.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            HandleSelectionInput();
        }

        /// <summary>
        /// Handle mouse input for unit selection
        /// </summary>
        private void HandleSelectionInput()
        {
            if (_mainCamera == null) return;

            // Left mouse button down - start selection
            if (Input.GetMouseButtonDown(0))
            {
                _boxSelectStart = Input.mousePosition;
                _isBoxSelecting = false;
            }

            // Left mouse button held - check for box selection
            if (Input.GetMouseButton(0))
            {
                float distance = Vector3.Distance(Input.mousePosition, _boxSelectStart);
                if (distance > 10f && !_isBoxSelecting)
                {
                    _isBoxSelecting = true;
                    if (_selectionBoxRenderer != null)
                    {
                        _selectionBoxRenderer.gameObject.SetActive(true);
                    }
                }

                if (_isBoxSelecting)
                {
                    UpdateSelectionBox();
                }
            }

            // Left mouse button up - complete selection
            if (Input.GetMouseButtonUp(0))
            {
                if (_isBoxSelecting)
                {
                    PerformBoxSelection();
                    _isBoxSelecting = false;
                    if (_selectionBoxRenderer != null)
                    {
                        _selectionBoxRenderer.gameObject.SetActive(false);
                    }
                }
                else
                {
                    PerformClickSelection();
                }
            }
        }

        /// <summary>
        /// Update visual representation of selection box
        /// </summary>
        private void UpdateSelectionBox()
        {
            if (_selectionBoxRenderer == null) return;

            Vector3 start = _boxSelectStart;
            Vector3 end = Input.mousePosition;

            // Convert to world space (on ground plane)
            Ray startRay = _mainCamera.ScreenPointToRay(start);
            Ray endRay = _mainCamera.ScreenPointToRay(end);

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(startRay, out float startDist) && 
                groundPlane.Raycast(endRay, out float endDist))
            {
                Vector3 worldStart = startRay.GetPoint(startDist);
                Vector3 worldEnd = endRay.GetPoint(endDist);

                // Create box corners
                Vector3[] corners = new Vector3[5];
                corners[0] = worldStart;
                corners[1] = new Vector3(worldEnd.x, worldStart.y, worldStart.z);
                corners[2] = worldEnd;
                corners[3] = new Vector3(worldStart.x, worldEnd.y, worldEnd.z);
                corners[4] = worldStart; // Close the box

                _selectionBoxRenderer.positionCount = 5;
                _selectionBoxRenderer.SetPositions(corners);
            }
        }

        /// <summary>
        /// Perform box selection
        /// </summary>
        private void PerformBoxSelection()
        {
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }

            // Get screen bounds
            Vector3 start = _boxSelectStart;
            Vector3 end = Input.mousePosition;

            Rect selectionRect = new Rect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Abs(start.x - end.x),
                Mathf.Abs(start.y - end.y)
            );

            // Find all selectable objects
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("NPC");
            
            foreach (GameObject unit in allUnits)
            {
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(unit.transform.position);
                if (selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
                {
                    AddToSelection(unit);
                }
            }

            Debug.Log($"[ManipulatorMode] Box selected {_selectedUnits.Count} units");
        }

        /// <summary>
        /// Perform single click selection
        /// </summary>
        private void PerformClickSelection()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _selectableLayerMask))
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    ClearSelection();
                }

                AddToSelection(hit.collider.gameObject);
                Debug.Log($"[ManipulatorMode] Selected: {hit.collider.gameObject.name}");
            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// Add unit to selection
        /// </summary>
        private void AddToSelection(GameObject unit)
        {
            if (!_selectedUnits.Contains(unit))
            {
                _selectedUnits.Add(unit);
                HighlightUnit(unit, true);
            }
        }

        /// <summary>
        /// Clear all selected units
        /// </summary>
        public void ClearSelection()
        {
            foreach (GameObject unit in _selectedUnits)
            {
                if (unit != null)
                {
                    HighlightUnit(unit, false);
                }
            }
            _selectedUnits.Clear();
        }

        /// <summary>
        /// Highlight/unhighlight a unit
        /// </summary>
        private void HighlightUnit(GameObject unit, bool highlight)
        {
            // TODO: Implement visual highlighting (outline shader, particle effect, etc.)
            // For now, just log
            // Could add a component or modify material
        }

        /// <summary>
        /// Assign decision to selected units
        /// </summary>
        public void AssignDecisionToSelected(string decisionType)
        {
            if (_selectedUnits.Count == 0)
            {
                Debug.LogWarning("[ManipulatorMode] No units selected");
                return;
            }

            foreach (GameObject unit in _selectedUnits)
            {
                if (unit != null)
                {
                    // TODO: Implement decision assignment through NPCDecisionSystem
                    Debug.Log($"[ManipulatorMode] Assigning decision '{decisionType}' to {unit.name}");
                }
            }
        }

        /// <summary>
        /// Order selected units to move to position
        /// </summary>
        public void OrderMoveTo(Vector3 position)
        {
            if (_selectedUnits.Count == 0) return;

            Debug.Log($"[ManipulatorMode] Ordering {_selectedUnits.Count} units to move to {position}");
            
            // TODO: Implement movement orders through NPC system
        }
    }
}

