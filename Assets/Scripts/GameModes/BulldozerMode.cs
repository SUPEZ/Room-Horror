using UnityEngine;

namespace GameModes
{
    /// <summary>
    /// Bulldozer mode - allows destroying buildings and NPCs
    /// Handles selection and destruction with visual feedback
    /// </summary>
    public class BulldozerMode : MonoBehaviour
    {
        [Header("Destruction Settings")]
        [Tooltip("Layer mask for destroyable objects")]
        [SerializeField] private LayerMask _destroyableLayerMask;
        
        [Tooltip("Require confirmation for NPC destruction")]
        [SerializeField] private bool _requireNPCConfirmation = true;
        
        [Tooltip("Destruction particle effect")]
        [SerializeField] private GameObject _destructionEffectPrefab;

        [Header("Visual Settings")]
        [Tooltip("Highlight color for target")]
        [SerializeField] private Color _targetHighlightColor = Color.red;
        
        [Tooltip("Highlight material")]
        [SerializeField] private Material _highlightMaterial;

        // Current state
        private GameObject _currentTarget;
        private Camera _mainCamera;
        private bool _awaitingConfirmation = false;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            Debug.Log("[BulldozerMode] Enabled - Left Click to destroy, Right Click to cancel");
        }

        private void OnDisable()
        {
            ClearTarget();
            _awaitingConfirmation = false;
        }

        private void Update()
        {
            HandleTargeting();
            HandleDestruction();
        }

        /// <summary>
        /// Handle mouse targeting for destruction
        /// </summary>
        private void HandleTargeting()
        {
            if (_mainCamera == null) return;
            if (_awaitingConfirmation) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _destroyableLayerMask))
            {
                GameObject target = hit.collider.gameObject;
                
                // Check if target is destructible
                if (IsDestructible(target))
                {
                    if (_currentTarget != target)
                    {
                        ClearTarget();
                        SetTarget(target);
                    }
                }
                else
                {
                    ClearTarget();
                }
            }
            else
            {
                ClearTarget();
            }
        }

        /// <summary>
        /// Check if object can be destroyed
        /// </summary>
        private bool IsDestructible(GameObject obj)
        {
            // Check tags
            return obj.CompareTag("Building") || 
                   obj.CompareTag("NPC") || 
                   obj.CompareTag("Destructible");
        }

        /// <summary>
        /// Set current target for destruction
        /// </summary>
        private void SetTarget(GameObject target)
        {
            _currentTarget = target;
            HighlightTarget(target, true);
        }

        /// <summary>
        /// Clear current target
        /// </summary>
        private void ClearTarget()
        {
            if (_currentTarget != null)
            {
                HighlightTarget(_currentTarget, false);
                _currentTarget = null;
            }
        }

        /// <summary>
        /// Highlight target object
        /// </summary>
        private void HighlightTarget(GameObject target, bool highlight)
        {
            // TODO: Implement visual highlighting
            // Could use outline shader, change material, or add particle effect
            // For now, just log
        }

        /// <summary>
        /// Handle destruction input
        /// </summary>
        private void HandleDestruction()
        {
            // Left click to destroy
            if (Input.GetMouseButtonDown(0))
            {
                if (_currentTarget != null)
                {
                    if (_requireNPCConfirmation && _currentTarget.CompareTag("NPC"))
                    {
                        // Require confirmation for NPCs
                        if (!_awaitingConfirmation)
                        {
                            _awaitingConfirmation = true;
                            Debug.Log("[BulldozerMode] Click again to confirm NPC destruction");
                        }
                        else
                        {
                            DestroyTarget();
                            _awaitingConfirmation = false;
                        }
                    }
                    else
                    {
                        DestroyTarget();
                    }
                }
            }

            // Right click to cancel
            if (Input.GetMouseButtonDown(1))
            {
                if (_awaitingConfirmation)
                {
                    _awaitingConfirmation = false;
                    Debug.Log("[BulldozerMode] Destruction cancelled");
                }
                ClearTarget();
            }
        }

        /// <summary>
        /// Destroy current target
        /// </summary>
        private void DestroyTarget()
        {
            if (_currentTarget == null) return;

            Vector3 position = _currentTarget.transform.position;
            string name = _currentTarget.name;

            // Spawn destruction effect
            if (_destructionEffectPrefab != null)
            {
                Instantiate(_destructionEffectPrefab, position, Quaternion.identity);
            }

            // Destroy the object
            Destroy(_currentTarget);
            _currentTarget = null;

            Debug.Log($"[BulldozerMode] Destroyed: {name}");
        }

        /// <summary>
        /// Destroy all objects in area
        /// </summary>
        public void DestroyInArea(Vector3 center, float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius, _destroyableLayerMask);
            
            int destroyCount = 0;
            foreach (Collider col in colliders)
            {
                if (IsDestructible(col.gameObject))
                {
                    if (_destructionEffectPrefab != null)
                    {
                        Instantiate(_destructionEffectPrefab, col.transform.position, Quaternion.identity);
                    }
                    
                    Destroy(col.gameObject);
                    destroyCount++;
                }
            }

            Debug.Log($"[BulldozerMode] Destroyed {destroyCount} objects in area");
        }

        /// <summary>
        /// Get current target name for UI
        /// </summary>
        public string GetTargetName()
        {
            return _currentTarget != null ? _currentTarget.name : "None";
        }

        /// <summary>
        /// Check if awaiting confirmation
        /// </summary>
        public bool IsAwaitingConfirmation()
        {
            return _awaitingConfirmation;
        }
    }
}

