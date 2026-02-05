using UnityEngine;
using MoreMountains.TopDownEngine;

/// <summary>
/// Manages camera modes: free camera navigation or character follow mode
/// Integrates with TopDown Engine's LevelManager
/// </summary>
public class CameraModeSwitcher : MonoBehaviour
{
    [Header("Camera Modes")]
    [Tooltip("Start in free camera mode (no character spawn)")]
    public bool startInFreeCameraMode = true;
    
    [Tooltip("Allow switching between modes at runtime")]
    public bool allowModeSwitching = true;

    [Header("References")]
    [Tooltip("The main camera (will be found automatically if null)")]
    public Camera mainCamera;
    
    [Tooltip("Free camera controller (will be found/added automatically)")]
    public FreeCameraController freeCameraController;

    [Header("Character Spawn")]
    [Tooltip("Spawn position when switching to character mode")]
    public Transform characterSpawnPoint;

    // Current mode
    public bool IsInFreeCameraMode { get; private set; }

    private void Awake()
    {
        // Find main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Setup free camera controller
        if (freeCameraController == null && mainCamera != null)
        {
            freeCameraController = mainCamera.GetComponent<FreeCameraController>();
            if (freeCameraController == null)
            {
                freeCameraController = mainCamera.gameObject.AddComponent<FreeCameraController>();
            }
        }
    }

    private void Start()
    {
        // Set initial mode
        if (startInFreeCameraMode)
        {
            EnableFreeCameraMode();
        }
        else
        {
            EnableCharacterMode();
        }

        // Set bounds from LevelManager if available
        if (LevelManager.HasInstance && freeCameraController != null)
        {
            freeCameraController.SetBounds(
                LevelManager.Instance.BoundsCollider, 
                LevelManager.Instance.BoundsCollider2D);
        }
    }

    private void Update()
    {
        // Tab key handling removed - CameraManager handles it
        // To enable standalone mode, uncomment below:
        /*
        if (allowModeSwitching && UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleMode();
            }
        }
        */
    }

    /// <summary>
    /// Toggle between free camera and character mode
    /// </summary>
    public void ToggleMode()
    {
        if (IsInFreeCameraMode)
        {
            EnableCharacterMode();
        }
        else
        {
            EnableFreeCameraMode();
        }
    }

    /// <summary>
    /// Enable free camera mode (disable character spawn/control)
    /// </summary>
    public void EnableFreeCameraMode()
    {
        IsInFreeCameraMode = true;

        // Enable free camera controller
        if (freeCameraController != null)
        {
            freeCameraController.SetControlEnabled(true);
        }

        // Stop camera from following character
        MMCameraEvent.Trigger(MMCameraEventTypes.StopFollowing);

        // Disable character control if exists
        if (LevelManager.HasInstance && LevelManager.Instance.Players != null)
        {
            foreach (var player in LevelManager.Instance.Players)
            {
                if (player != null)
                {
                    player.gameObject.SetActive(false);
                }
            }
        }

        Debug.Log("Free Camera Mode Enabled - Use WASD to move, Middle Mouse to drag");
    }

    /// <summary>
    /// Enable character mode (spawn and follow character)
    /// </summary>
    public void EnableCharacterMode()
    {
        IsInFreeCameraMode = false;

        // Disable free camera controller
        if (freeCameraController != null)
        {
            freeCameraController.SetControlEnabled(false);
        }

        // Check if we need to spawn character
        if (LevelManager.HasInstance)
        {
            // If no players exist, trigger character spawn through LevelManager
            if (LevelManager.Instance.Players == null || LevelManager.Instance.Players.Count == 0)
            {
                // This would require modifying LevelManager to support late spawn
                Debug.LogWarning("Character spawn after free camera mode not yet implemented. Restart scene to use character mode.");
                return;
            }

            // Enable existing character
            foreach (var player in LevelManager.Instance.Players)
            {
                if (player != null)
                {
                    player.gameObject.SetActive(true);
                    
                    // Optionally teleport to spawn point
                    if (characterSpawnPoint != null)
                    {
                        player.transform.position = characterSpawnPoint.position;
                    }
                }
            }

            // Start following character
            if (LevelManager.Instance.Players.Count > 0 && LevelManager.Instance.Players[0] != null)
            {
                MMCameraEvent.Trigger(MMCameraEventTypes.SetTargetCharacter, LevelManager.Instance.Players[0]);
                MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
            }
        }

        Debug.Log("Character Mode Enabled");
    }

    /// <summary>
    /// Get current camera position (useful for spawning character at camera location)
    /// </summary>
    public Vector3 GetCameraPosition()
    {
        return mainCamera != null ? mainCamera.transform.position : Vector3.zero;
    }
}

