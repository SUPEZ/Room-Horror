using UnityEngine;
using System.Collections.Generic;

namespace GameModes
{
    /// <summary>
    /// Builder mode - allows placing buildings and structures
    /// Handles preview placement, grid snapping, and construction validation
    /// </summary>
    public class BuilderMode : MonoBehaviour
    {
        [Header("Building Catalog")]
        [Tooltip("Available building prefabs")]
        [SerializeField] private List<GameObject> _buildingPrefabs = new List<GameObject>();
        
        [Header("Grid Settings")]
        [Tooltip("Enable grid snapping")]
        [SerializeField] private bool _useGridSnapping = true;
        
        [Tooltip("Grid size for snapping")]
        [SerializeField] private float _gridSize = 1f;

        [Header("Preview Settings")]
        [Tooltip("Material for valid placement preview")]
        [SerializeField] private Material _validPreviewMaterial;
        
        [Tooltip("Material for invalid placement preview")]
        [SerializeField] private Material _invalidPreviewMaterial;
        
        [Tooltip("Preview transparency")]
        [SerializeField, Range(0f, 1f)] private float _previewAlpha = 0.5f;

        [Header("Placement Rules")]
        [Tooltip("Layer mask for collision checking")]
        [SerializeField] private LayerMask _collisionCheckMask;
        
        [Tooltip("Minimum distance from other buildings")]
        [SerializeField] private float _minBuildingDistance = 2f;

        // Current state
        private int _selectedBuildingIndex = 0;
        private GameObject _currentPreview;
        private bool _isPlacementValid;
        private Camera _mainCamera;

        public int SelectedBuildingIndex => _selectedBuildingIndex;
        public int BuildingCount => _buildingPrefabs.Count;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (_buildingPrefabs.Count > 0)
            {
                CreatePreview(_selectedBuildingIndex);
            }
            Debug.Log("[BuilderMode] Enabled - Left Click to place, Mouse Wheel to cycle buildings");
        }

        private void OnDisable()
        {
            DestroyPreview();
        }

        private void Update()
        {
            HandleBuildingSelection();
            UpdatePreviewPosition();
            HandlePlacement();
        }

        /// <summary>
        /// Handle building type selection input
        /// </summary>
        private void HandleBuildingSelection()
        {
            if (_buildingPrefabs.Count == 0) return;

            // Mouse wheel to cycle buildings
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
            {
                int newIndex = _selectedBuildingIndex + (scroll > 0 ? 1 : -1);
                newIndex = (newIndex + _buildingPrefabs.Count) % _buildingPrefabs.Count;
                
                if (newIndex != _selectedBuildingIndex)
                {
                    SelectBuilding(newIndex);
                }
            }

            // Number keys to select specific building
            for (int i = 0; i < Mathf.Min(9, _buildingPrefabs.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectBuilding(i);
                }
            }
        }

        /// <summary>
        /// Select a building type
        /// </summary>
        public void SelectBuilding(int index)
        {
            if (index < 0 || index >= _buildingPrefabs.Count) return;

            _selectedBuildingIndex = index;
            DestroyPreview();
            CreatePreview(index);
            
            Debug.Log($"[BuilderMode] Selected building: {_buildingPrefabs[index].name}");
        }

        /// <summary>
        /// Create preview object
        /// </summary>
        private void CreatePreview(int buildingIndex)
        {
            if (buildingIndex < 0 || buildingIndex >= _buildingPrefabs.Count) return;

            _currentPreview = Instantiate(_buildingPrefabs[buildingIndex]);
            _currentPreview.name = "BuildPreview";
            
            // Disable colliders on preview
            foreach (Collider col in _currentPreview.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Make preview semi-transparent
            MakePreviewTransparent(_currentPreview);
        }

        /// <summary>
        /// Destroy current preview
        /// </summary>
        private void DestroyPreview()
        {
            if (_currentPreview != null)
            {
                Destroy(_currentPreview);
                _currentPreview = null;
            }
        }

        /// <summary>
        /// Make preview object transparent
        /// </summary>
        private void MakePreviewTransparent(GameObject preview)
        {
            foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = new Material(materials[i]);
                    Color color = mat.color;
                    color.a = _previewAlpha;
                    mat.color = color;
                    
                    // Enable transparency
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    
                    materials[i] = mat;
                }
                renderer.materials = materials;
            }
        }

        /// <summary>
        /// Update preview position to follow mouse
        /// </summary>
        private void UpdatePreviewPosition()
        {
            if (_currentPreview == null || _mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPosition = ray.GetPoint(distance);

                // Apply grid snapping
                if (_useGridSnapping)
                {
                    worldPosition.x = Mathf.Round(worldPosition.x / _gridSize) * _gridSize;
                    worldPosition.z = Mathf.Round(worldPosition.z / _gridSize) * _gridSize;
                }

                _currentPreview.transform.position = worldPosition;

                // Check placement validity
                _isPlacementValid = IsPlacementValid(worldPosition);
                UpdatePreviewMaterial(_isPlacementValid);
            }
        }

        /// <summary>
        /// Check if placement is valid at position
        /// </summary>
        private bool IsPlacementValid(Vector3 position)
        {
            // Check for collisions
            Bounds bounds = GetPreviewBounds();
            Collider[] colliders = Physics.OverlapBox(
                position + bounds.center,
                bounds.extents,
                _currentPreview.transform.rotation,
                _collisionCheckMask
            );

            if (colliders.Length > 0)
            {
                return false;
            }

            // Check minimum distance from other buildings
            GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");
            foreach (GameObject building in buildings)
            {
                if (Vector3.Distance(position, building.transform.position) < _minBuildingDistance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get bounds of preview object
        /// </summary>
        private Bounds GetPreviewBounds()
        {
            if (_currentPreview == null) return new Bounds(Vector3.zero, Vector3.one);

            Bounds bounds = new Bounds(_currentPreview.transform.position, Vector3.zero);
            foreach (Renderer renderer in _currentPreview.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Update preview material based on validity
        /// </summary>
        private void UpdatePreviewMaterial(bool isValid)
        {
            // TODO: Apply valid/invalid materials to preview
            // For now, change color
            Color targetColor = isValid ? Color.green : Color.red;
            targetColor.a = _previewAlpha;

            foreach (Renderer renderer in _currentPreview.GetComponentsInChildren<Renderer>())
            {
                foreach (Material mat in renderer.materials)
                {
                    mat.color = targetColor;
                }
            }
        }

        /// <summary>
        /// Handle placement input
        /// </summary>
        private void HandlePlacement()
        {
            // Left click to place
            if (Input.GetMouseButtonDown(0))
            {
                if (_isPlacementValid)
                {
                    PlaceBuilding();
                }
                else
                {
                    Debug.LogWarning("[BuilderMode] Cannot place building here - invalid location");
                }
            }

            // Right click to cancel/rotate
            if (Input.GetMouseButtonDown(1))
            {
                RotatePreview(90f);
            }
        }

        /// <summary>
        /// Place the building at current preview position
        /// </summary>
        private void PlaceBuilding()
        {
            if (_currentPreview == null) return;

            Vector3 position = _currentPreview.transform.position;
            Quaternion rotation = _currentPreview.transform.rotation;

            // Instantiate actual building
            GameObject building = Instantiate(_buildingPrefabs[_selectedBuildingIndex], position, rotation);
            building.tag = "Building";

            Debug.Log($"[BuilderMode] Placed building: {building.name} at {position}");

            // Recreate preview
            DestroyPreview();
            CreatePreview(_selectedBuildingIndex);
        }

        /// <summary>
        /// Rotate preview by angle
        /// </summary>
        private void RotatePreview(float angle)
        {
            if (_currentPreview != null)
            {
                _currentPreview.transform.Rotate(Vector3.up, angle);
            }
        }

        /// <summary>
        /// Get current selected building name
        /// </summary>
        public string GetSelectedBuildingName()
        {
            if (_selectedBuildingIndex >= 0 && _selectedBuildingIndex < _buildingPrefabs.Count)
            {
                return _buildingPrefabs[_selectedBuildingIndex].name;
            }
            return "None";
        }
    }
}

