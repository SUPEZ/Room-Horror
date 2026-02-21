using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// State machine for Free Camera mode - manages different interaction modes
/// Switches between: None, Manipulator (control NPCs), Builder (place objects), Bulldozer (destroy objects/NPCs)
/// </summary>
public class FreeCameraStateMachine : MonoBehaviour
{
    [Header("State Settings")]
    [Tooltip("Initial state when entering free camera mode")]
    [SerializeField] private FreeCameraState startState = FreeCameraState.None;

    [Header("Input Keys")]
    [Tooltip("Key to switch to None state")]
    [SerializeField] private Key noneKey = Key.Digit1;
    
    [Tooltip("Key to switch to Manipulator state")]
    [SerializeField] private Key manipulatorKey = Key.Digit2;
    
    [Tooltip("Key to switch to Builder state")]
    [SerializeField] private Key builderKey = Key.Digit3;
    
    [Tooltip("Key to switch to Bulldozer state")]
    [SerializeField] private Key bulldozerKey = Key.Digit4;

    // Current state
    public FreeCameraState CurrentState { get; private set; }

    // Events
    public event Action<FreeCameraState, FreeCameraState> OnStateChanged;
    public event Action<FreeCameraState> OnStateEnter;
    public event Action<FreeCameraState> OnStateExit;

    // State enum
    public enum FreeCameraState
    {
        None,           // No special mode active
        Manipulator,    // Manipulate NPCs and troops
        Builder,        // Build objects: houses, machines, etc.
        Bulldozer       // Destroy objects and NPCs
    }

    private void Start()
    {
        SetState(startState);
    }

    private void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Handle keyboard input for state switching
    /// </summary>
    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[noneKey].wasPressedThisFrame)
        {
            SetState(FreeCameraState.None);
        }
        else if (Keyboard.current[manipulatorKey].wasPressedThisFrame)
        {
            SetState(FreeCameraState.Manipulator);
        }
        else if (Keyboard.current[builderKey].wasPressedThisFrame)
        {
            SetState(FreeCameraState.Builder);
        }
        else if (Keyboard.current[bulldozerKey].wasPressedThisFrame)
        {
            SetState(FreeCameraState.Bulldozer);
        }
    }

    /// <summary>
    /// Set current state
    /// </summary>
    public void SetState(FreeCameraState newState)
    {
        if (CurrentState == newState) return;

        FreeCameraState previousState = CurrentState;
        
        // Exit current state
        OnStateExit?.Invoke(CurrentState);
        
        // Change state
        CurrentState = newState;
        
        // Enter new state
        OnStateEnter?.Invoke(CurrentState);
        
        // Notify listeners
        OnStateChanged?.Invoke(previousState, CurrentState);

        Debug.Log($"[FreeCameraStateMachine] State changed: {previousState} → {CurrentState}");
    }

    /// <summary>
    /// Enable state machine
    /// </summary>
    public void Enable()
    {
        enabled = true;
        SetState(startState);
        Debug.Log("[FreeCameraStateMachine] Enabled");
    }

    /// <summary>
    /// Disable state machine
    /// </summary>
    public void Disable()
    {
        SetState(FreeCameraState.None);
        enabled = false;
        Debug.Log("[FreeCameraStateMachine] Disabled");
    }

    /// <summary>
    /// Get state name for UI display
    /// </summary>
    public string GetStateName()
    {
        return CurrentState.ToString();
    }

    /// <summary>
    /// Get state color for UI display
    /// </summary>
    public Color GetStateColor()
    {
        return CurrentState switch
        {
            FreeCameraState.None => Color.white,
            FreeCameraState.Manipulator => Color.cyan,
            FreeCameraState.Builder => Color.green,
            FreeCameraState.Bulldozer => Color.red,
            _ => Color.white
        };
    }
}

