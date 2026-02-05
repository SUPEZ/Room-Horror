using UnityEngine;
using MoreMountains.TopDownEngine;
using MoreMountains.Tools;
using UnityEngine.InputSystem;

/// <summary>
/// Camera Manager - controls two camera modes:
/// 1. Free Camera - camera follows invisible target object controlled by WASD/Mouse
/// 2. Character Mode - camera follows player character controlled by TopDown Engine
/// Switch modes with F4
/// </summary>
public class CameraManager : MMSingleton<CameraManager>
{
    [Header("Camera Mode Settings")]
    [Tooltip("Camera mode to start with")]
    public CameraMode startMode = CameraMode.FreeCamera;
    
    [Header("Free Camera Settings")]
    [Tooltip("Movement speed for invisible target (units per second)")]
    public float targetMoveSpeed = 10f;
    
    [Tooltip("Mouse drag speed for invisible target")]
    public float mouseDragSpeed = 0.5f;
    
    [Tooltip("Mouse button for dragging (0=Left, 1=Right, 2=Middle)")]
    public int dragMouseButton = 2;

    [Header("Bounds")]
    [Tooltip("Limit movement to these bounds (optional)")]
    public Collider boundsCollider;
    
    [Tooltip("Limit movement to these bounds 2D (optional)")]
    public Collider2D boundsCollider2D;
    
    [Tooltip("Padding from bounds edges")]
    public float boundsPadding = 1f;

    [Header("References")]
    [Tooltip("Main camera (auto-detected if null)")]
    public Camera mainCamera;

    // Current state
    public CameraMode CurrentMode { get; private set; }
    public bool IsFreeCameraActive => CurrentMode == CameraMode.FreeCamera;
    public bool IsCharacterModeActive => CurrentMode == CameraMode.CharacterFollow;

    // Internal components
    private GameObject _invisibleTarget;
    private Bounds _bounds;
    private bool _hasBounds;
    private Vector3 _lastMousePosition;
    private bool _isDragging;

    // Enums
    public enum CameraMode
    {
        FreeCamera,
        CharacterFollow
    }

    protected override void Awake()
    {
        base.Awake();
        InitializeCamera();
        CreateInvisibleTarget();
        UpdateBounds();
    }

    private void Start()
    {
        // Wait for LevelManager to spawn character, then set mode
        StartCoroutine(InitializeAfterPlayerSpawn());
    }

    /// <summary>
    /// Wait for player to spawn before setting camera mode
    /// </summary>
    private System.Collections.IEnumerator InitializeAfterPlayerSpawn()
    {
        // Wait for LevelManager to be ready and player to spawn
        while (!LevelManager.HasInstance || 
               LevelManager.Instance.Players == null || 
               LevelManager.Instance.Players.Count == 0)
        {
            yield return null; // Wait one frame
        }

        // Wait one more frame to ensure player is fully initialized
        yield return null;

        // Now set the camera mode
        SetMode(startMode);
        
        Debug.Log("[CameraManager] Initialized after player spawn. Mode: " + startMode);
    }

    private void Update()
    {
        HandleModeSwitch();
        
        if (CurrentMode == CameraMode.FreeCamera)
        {
            HandleFreeCameraInput();
        }
    }

    /// <summary>
    /// Initialize camera reference
    /// </summary>
    private void InitializeCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[CameraManager] Main camera not found!");
            }
        }
    }

    /// <summary>
    /// Create invisible target object for free camera mode
    /// </summary>
    private void CreateInvisibleTarget()
    {
        _invisibleTarget = new GameObject("CameraTarget_Invisible");
        _invisibleTarget.transform.position = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        
        // Make it invisible but keep in hierarchy for debugging
        var meshRenderer = _invisibleTarget.AddComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        
        Debug.Log("[CameraManager] Invisible camera target created at: " + _invisibleTarget.transform.position);
    }

    /// <summary>
    /// Update bounds from colliders
    /// </summary>
    private void UpdateBounds()
    {
        if (boundsCollider != null)
        {
            _bounds = boundsCollider.bounds;
            _hasBounds = true;
        }
        else if (boundsCollider2D != null)
        {
            Bounds bounds2D = boundsCollider2D.bounds;
            _bounds = new Bounds(
                new Vector3(bounds2D.center.x, _invisibleTarget.transform.position.y, bounds2D.center.y),
                new Vector3(bounds2D.size.x, 0f, bounds2D.size.y)
            );
            _hasBounds = true;
        }
        else if (LevelManager.HasInstance)
        {
            // Auto-detect bounds from LevelManager
            if (LevelManager.Instance.BoundsCollider != null)
            {
                _bounds = LevelManager.Instance.BoundsCollider.bounds;
                _hasBounds = true;
                Debug.Log("[CameraManager] Bounds detected from LevelManager (3D)");
            }
            else if (LevelManager.Instance.BoundsCollider2D != null)
            {
                Bounds bounds2D = LevelManager.Instance.BoundsCollider2D.bounds;
                _bounds = new Bounds(
                    new Vector3(bounds2D.center.x, _invisibleTarget.transform.position.y, bounds2D.center.y),
                    new Vector3(bounds2D.size.x, 0f, bounds2D.size.y)
                );
                _hasBounds = true;
                Debug.Log("[CameraManager] Bounds detected from LevelManager (2D)");
            }
        }
        else
        {
            _hasBounds = false;
            Debug.LogWarning("[CameraManager] No bounds detected - camera movement unlimited");
        }
    }

    /// <summary>
    /// Handle F4 mode switching
    /// </summary>
    private void HandleModeSwitch()
    {
        if (Keyboard.current == null) return;
        
        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            ToggleMode();
        }
    }

    /// <summary>
    /// Handle input for free camera mode (WASD and mouse drag)
    /// </summary>
    private void HandleFreeCameraInput()
    {
        if (_invisibleTarget == null) return;

        // WASD movement
        Vector2 input = Vector2.zero;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                input.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                input.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                input.x += 1f;
        }

        if (input.sqrMagnitude > 0f)
        {
            input.Normalize();
            Vector3 movement = new Vector3(input.x, 0f, input.y) * (targetMoveSpeed * Time.deltaTime);
            _invisibleTarget.transform.position += movement;
        }

        // Mouse drag
        HandleMouseDrag();

        // Apply bounds
        if (_hasBounds)
        {
            Vector3 pos = _invisibleTarget.transform.position;
            pos.x = Mathf.Clamp(pos.x, _bounds.min.x + boundsPadding, _bounds.max.x - boundsPadding);
            pos.z = Mathf.Clamp(pos.z, _bounds.min.z + boundsPadding, _bounds.max.z - boundsPadding);
            _invisibleTarget.transform.position = pos;
        }
    }

    /// <summary>
    /// Handle mouse drag for invisible target
    /// </summary>
    private void HandleMouseDrag()
    {
        if (Mouse.current == null) return;

        bool dragButtonPressed = false;
        
        if (dragMouseButton == 0)
            dragButtonPressed = Mouse.current.leftButton.isPressed;
        else if (dragMouseButton == 1)
            dragButtonPressed = Mouse.current.rightButton.isPressed;
        else if (dragMouseButton == 2)
            dragButtonPressed = Mouse.current.middleButton.isPressed;

        if (dragButtonPressed)
        {
            Vector3 currentMousePosition = Mouse.current.position.ReadValue();
            
            if (!_isDragging)
            {
                _isDragging = true;
                _lastMousePosition = currentMousePosition;
            }
            else
            {
                Vector3 delta = currentMousePosition - _lastMousePosition;
                Vector3 movement = new Vector3(-delta.x, 0f, -delta.y) * (mouseDragSpeed * Time.deltaTime);
                _invisibleTarget.transform.position += movement;
                _lastMousePosition = currentMousePosition;
            }
        }
        else
        {
            _isDragging = false;
        }
    }

    // ========== PUBLIC API ==========

    /// <summary>
    /// Toggle between camera modes
    /// </summary>
    public void ToggleMode()
    {
        if (CurrentMode == CameraMode.FreeCamera)
        {
            SetMode(CameraMode.CharacterFollow);
        }
        else
        {
            SetMode(CameraMode.FreeCamera);
        }
    }

    /// <summary>
    /// Set camera mode
    /// </summary>
    public void SetMode(CameraMode mode)
    {
        CurrentMode = mode;

        switch (mode)
        {
            case CameraMode.FreeCamera:
                EnableFreeCameraMode();
                break;
            case CameraMode.CharacterFollow:
                EnableCharacterMode();
                break;
        }

        Debug.Log($"[CameraManager] Mode changed to: {mode}");
    }

    /// <summary>
    /// Enable free camera mode - camera follows invisible target
    /// </summary>
    private void EnableFreeCameraMode()
    {
        if (_invisibleTarget == null)
        {
            CreateInvisibleTarget();
        }

        // Position invisible target at current camera position
        if (mainCamera != null)
        {
            Vector3 camPos = mainCamera.transform.position;
            _invisibleTarget.transform.position = new Vector3(camPos.x, _invisibleTarget.transform.position.y, camPos.z);
        }

        // Stop camera following character
        MMCameraEvent.Trigger(MMCameraEventTypes.StopFollowing);

        // Disable character input
        if (LevelManager.HasInstance && LevelManager.Instance.Players != null)
        {
            foreach (var player in LevelManager.Instance.Players)
            {
                if (player != null)
                {
                    // Disable character movement abilities
                    var movementAbility = player.FindAbility<CharacterMovement>();
                    if (movementAbility != null)
                    {
                        movementAbility.enabled = false;
                    }
                    
                    // Disable all input-related abilities
                    var characterAbilities = player.GetComponents<CharacterAbility>();
                    foreach (var ability in characterAbilities)
                    {
                        if (ability is CharacterMovement || 
                            ability is CharacterOrientation3D || 
                            ability is CharacterOrientation2D)
                        {
                            ability.enabled = false;
                        }
                    }
                }
            }
        }

        Debug.Log("[CameraManager] Free Camera Mode ACTIVE - WASD to move, Middle Mouse to drag, F4 to switch to Character Mode");
    }

    /// <summary>
    /// Enable character mode - camera follows player
    /// </summary>
    private void EnableCharacterMode()
    {
        // Enable character input
        if (LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
        {
            var player = LevelManager.Instance.Players[0];
            
            if (player != null)
            {
                // Enable character abilities
                var characterAbilities = player.GetComponents<CharacterAbility>();
                foreach (var ability in characterAbilities)
                {
                    ability.enabled = true;
                }

                // Set camera to follow character
                MMCameraEvent.Trigger(MMCameraEventTypes.SetTargetCharacter, player);
                MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
                
                Debug.Log("[CameraManager] Character Mode ACTIVE - Control character with TopDown Engine, F4 to switch to Free Camera");
            }
        }
        else
        {
            Debug.LogWarning("[CameraManager] No player found for character mode!");
        }
    }

    /// <summary>
    /// LateUpdate to manually move camera to follow invisible target in free camera mode
    /// </summary>
    private void LateUpdate()
    {
        if (CurrentMode == CameraMode.FreeCamera && _invisibleTarget != null && mainCamera != null)
        {
            // Move camera to follow invisible target (keep same Y offset)
            Vector3 targetPos = _invisibleTarget.transform.position;
            Vector3 currentPos = mainCamera.transform.position;
            
            // Only update X and Z, keep Y (height) the same
            mainCamera.transform.position = new Vector3(targetPos.x, currentPos.y, targetPos.z);
        }
    }

    /// <summary>
    /// Get invisible target position
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return _invisibleTarget != null ? _invisibleTarget.transform.position : Vector3.zero;
    }

    /// <summary>
    /// Teleport invisible target to position
    /// </summary>
    public void TeleportTargetTo(Vector3 position)
    {
        if (_invisibleTarget != null)
        {
            _invisibleTarget.transform.position = position;
        }
    }

    /// <summary>
    /// Set movement speed
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        targetMoveSpeed = speed;
    }

    private void OnDestroy()
    {
        if (_invisibleTarget != null)
        {
            Destroy(_invisibleTarget);
        }
    }
}

