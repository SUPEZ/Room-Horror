using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Free camera controller for navigating the level without character spawn
/// Allows WASD/Arrow keys movement and mouse drag with bounds checking
/// </summary>
public class FreeCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed when using keyboard (units per second)")]
    public float keyboardMoveSpeed = 10f;
    
    [Tooltip("Movement speed when using mouse drag")]
    public float mouseDragSpeed = 0.5f;
    
    [Tooltip("Smoothing factor for camera movement (0 = instant, 1 = very smooth)")]
    [Range(0f, 0.99f)]
    public float movementSmoothing = 0.85f;

    [Header("Bounds")]
    [Tooltip("Limit camera movement to these bounds (leave null for no limits)")]
    public Collider boundsCollider;
    
    [Tooltip("Limit camera movement to these bounds 2D (leave null for no limits)")]
    public Collider2D boundsCollider2D;
    
    [Tooltip("Padding from the bounds edges")]
    public float boundsPadding = 1f;

    [Header("Input")]
    [Tooltip("Enable camera control")]
    public bool enableControl = true;
    
    [Tooltip("Mouse button for dragging (0=Left, 1=Right, 2=Middle)")]
    public int dragMouseButton = 2; // Middle mouse by default

    // Private variables
    private Vector3 _targetPosition;
    private Vector3 _velocity;
    private Vector3 _lastMousePosition;
    private bool _isDragging;
    private Bounds _bounds;
    private bool _hasBounds;

    private void Start()
    {
        _targetPosition = transform.position;
        UpdateBounds();
    }

    private void Update()
    {
        if (!enableControl) return;

        HandleKeyboardInput();
        HandleMouseDrag();
        ApplyMovement();
    }

    /// <summary>
    /// Handle WASD and Arrow keys input
    /// </summary>
    private void HandleKeyboardInput()
    {
        Vector2 input = Vector2.zero;

        // Keyboard input
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
            Vector3 movement = new Vector3(input.x, 0f, input.y) * (keyboardMoveSpeed * Time.deltaTime);
            _targetPosition += movement;
        }
    }

    /// <summary>
    /// Handle mouse drag input
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
                // Convert screen space to world space movement
                Vector3 movement = new Vector3(-delta.x, 0f, -delta.y) * (mouseDragSpeed * Time.deltaTime);
                _targetPosition += movement;
                _lastMousePosition = currentMousePosition;
            }
        }
        else
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// Apply smoothed movement with bounds checking
    /// </summary>
    private void ApplyMovement()
    {
        // Clamp target position to bounds
        if (_hasBounds)
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, 
                _bounds.min.x + boundsPadding, 
                _bounds.max.x - boundsPadding);
            
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, 
                _bounds.min.z + boundsPadding, 
                _bounds.max.z - boundsPadding);
        }

        // Smooth movement
        if (movementSmoothing > 0f)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                _targetPosition, 
                ref _velocity, 
                movementSmoothing);
        }
        else
        {
            transform.position = _targetPosition;
        }
    }

    /// <summary>
    /// Update bounds from colliders
    /// </summary>
    public void UpdateBounds()
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
                new Vector3(bounds2D.center.x, transform.position.y, bounds2D.center.y),
                new Vector3(bounds2D.size.x, 0f, bounds2D.size.y)
            );
            _hasBounds = true;
        }
        else
        {
            _hasBounds = false;
        }
    }

    /// <summary>
    /// Set bounds from LevelManager
    /// </summary>
    public void SetBounds(Collider bounds3D, Collider2D bounds2D)
    {
        boundsCollider = bounds3D;
        boundsCollider2D = bounds2D;
        UpdateBounds();
    }

    /// <summary>
    /// Enable or disable camera control
    /// </summary>
    public void SetControlEnabled(bool isEnabled)
    {
        enableControl = isEnabled;
        _isDragging = false;
    }

    /// <summary>
    /// Teleport camera to position instantly
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        _targetPosition = position;
        _velocity = Vector3.zero;
    }
}

