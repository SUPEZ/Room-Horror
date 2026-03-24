using UnityEngine;
using MoreMountains.TopDownEngine;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;
using UnityEngine.EventSystems;

/// <summary>
/// Component to easily switch a Character between Player and AI control at runtime.
/// Allows clicking on any character to take control of it.
/// </summary>
[RequireComponent(typeof(Character))]
[AddComponentMenu("TopDown Engine/Character/Core/Character Provider")]
public class CharacterProvider : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The PlayerID to assign when this character becomes a Player")]
    public string PlayerID = "Player1";

    [Tooltip("Should the camera automatically follow this character when becoming a Player?")]
    public bool AutoSetCameraTarget = true;

    protected Character _character;
    protected AIBrain _brain;

    public Character ControlledCharacter => _character;

    protected virtual void Awake()
    {
        _character = GetComponent<Character>();
        _brain = GetComponent<AIBrain>();

        // Ensure coordinator exists before registering to avoid execution-order issues.
        CharactersCoordinator.Instance.RegisterCharacter(this);
    }

    protected virtual void OnDestroy()
    {
        if (CharactersCoordinator.HasInstance)
        {
            CharactersCoordinator.Instance.UnregisterCharacter(this);
        }
    }

    protected virtual void Start()
    {
        // Optional Initialization logic here
    }

    /// <summary>
    /// Toggles the character between AI and Player
    /// </summary>
    public virtual void ToggleCharacterType()
    {
        if (_character.CharacterType == Character.CharacterTypes.Player)
        {
            SetCharacterType(Character.CharacterTypes.AI);
        }
        else
        {
            SetCharacterType(Character.CharacterTypes.Player);
        }
    }

    /// <summary>
    /// Sets the character type to the specified type, enabling/disabling the brain and input manager accordingly.
    /// </summary>
    /// <param name="newType"></param>
    public virtual void SetCharacterType(Character.CharacterTypes newType)
    {
        if (newType == Character.CharacterTypes.Player)
        {
            _character.CharacterType = Character.CharacterTypes.Player;
            _character.SetPlayerID(PlayerID);
            _character.SetInputManager();
            SetManualInputEnabled(true);

            // Disable AI Brain
            if (_brain != null)
            {
                _brain.BrainActive = false;
                _brain.enabled = false;
            }

            // If camera should switch to follow this new player
            if (AutoSetCameraTarget)
            {
                MMCameraEvent.Trigger(MMCameraEventTypes.SetTargetCharacter, _character);
                MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
                MMSetFeedbackRangeCenterEvent.Trigger(this.transform);
            }
        }
        else // AI
        {
            _character.CharacterType = Character.CharacterTypes.AI;
            _character.SetPlayerID("");
            // SetInputManager() will remove the input manager and reset inputs when PlayerID is empty
            _character.SetInputManager();
            SetManualInputEnabled(false);
            if (_character.LinkedInputManager == null)
            {
                _character.ResetInput();
            }

            // Enable AI Brain
            if (_brain != null)
            {
                _brain.BrainActive = true;
                _brain.enabled = true;
                _brain.Owner = this.gameObject;
                _brain.ResetBrain();
            }
        }
    }

    /// <summary>
    /// Allows to click on the character to take control of it. 
    /// Requires a collider on this object or its children and Mouse Events enabled (Physics Raycaster on Camera).
    /// </summary>
    protected virtual void OnMouseDown()
    {
        if (!CanTakeControlFromCurrentMode())
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (_character.CharacterType == Character.CharacterTypes.AI)
        {
            TakeControl();
        }
    }

    /// <summary>
    /// Public method to take control of this specific character, turning all other Players into AIs.
    /// </summary>
    public virtual void TakeControl()
    {
        if (_character != null && _character.CharacterType == Character.CharacterTypes.Player)
        {
            return;
        }

        if (CharactersCoordinator.HasInstance)
        {
            CharactersCoordinator.Instance.SetPlayer(this);
        }
        
        // Switch camera mode to CharacterFollow if it's currently on FreeCamera
        if (CameraManager.HasInstance && CameraManager.Instance.CurrentMode == CameraManager.CameraMode.FreeCamera)
        {
            CameraManager.Instance.SetMode(CameraManager.CameraMode.CharacterFollow);
        }
    }

    /// <summary>
    /// Enables manual player input or AI/script-driven movement for CharacterMovement ability.
    /// </summary>
    protected virtual void SetManualInputEnabled(bool enabled)
    {
        if (_character == null)
        {
            return;
        }

        var movementAbility = _character.FindAbility<CharacterMovement>();
        if (movementAbility != null)
        {
            movementAbility.InputAuthorized = enabled;
            movementAbility.ScriptDrivenInput = !enabled;
        }
    }

    /// <summary>
    /// Allows taking control only when free-camera tools are not actively using left click.
    /// </summary>
    protected virtual bool CanTakeControlFromCurrentMode()
    {
        if (!CameraManager.HasInstance || CameraManager.Instance.CurrentMode != CameraManager.CameraMode.FreeCamera)
        {
            return true;
        }

        var stateMachine = CameraManager.Instance.stateMachine;
        if (stateMachine == null)
        {
            return true;
        }

        return stateMachine.CurrentState == FreeCameraStateMachine.FreeCameraState.None;
    }
}
