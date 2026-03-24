using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.EventSystems;

/// <summary>
/// Singleton manager that keeps track of all CharacterProviders in the scene.
/// Allows easy access to toggle controls without FindObjectsByType every time.
/// </summary>
public class CharactersCoordinator : MMSingleton<CharactersCoordinator>
{
    [Header("Click-to-Control")]
    [SerializeField] private bool _allowClickToTakeControl = true;
    [SerializeField] private float _clickRayDistance = 1000f;

    public List<CharacterProvider> AllCharacters { get; private set; } = new List<CharacterProvider>();

    public CharacterProvider CurrentPlayer { get; private set; }
    public CharacterProvider LastControlledPlayer { get; private set; }

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleClickToTakeControl();
    }

    public void RegisterCharacter(CharacterProvider character)
    {
        if (character == null)
        {
            return;
        }

        if (!AllCharacters.Contains(character))
        {
            AllCharacters.Add(character);
        }
    }

    public void UnregisterCharacter(CharacterProvider character)
    {
        if (AllCharacters.Contains(character))
        {
            AllCharacters.Remove(character);
        }
        if (CurrentPlayer == character)
        {
            CurrentPlayer = null;
        }

        if (LastControlledPlayer == character)
        {
            LastControlledPlayer = null;
        }
    }

    /// <summary>
    /// Sets a specific character as the player, and turns all others into AI.
    /// </summary>
    public void SetPlayer(CharacterProvider newPlayer)
    {
        if (newPlayer == null)
        {
            return;
        }

        CleanupDestroyedCharacters();

        foreach (var character in AllCharacters)
        {
            if (character == null)
            {
                continue;
            }

            if (character == newPlayer)
            {
                character.SetCharacterType(MoreMountains.TopDownEngine.Character.CharacterTypes.Player);
                CurrentPlayer = character;
                LastControlledPlayer = character;
            }
            else
            {
                character.SetCharacterType(MoreMountains.TopDownEngine.Character.CharacterTypes.AI);
            }
        }
    }

    /// <summary>
    /// Turns all characters into AI. Useful when switching to Free Camera.
    /// </summary>
    public void SetAllToAI()
    {
        CleanupDestroyedCharacters();

        foreach (var character in AllCharacters)
        {
            if (character == null)
            {
                continue;
            }

            character.SetCharacterType(MoreMountains.TopDownEngine.Character.CharacterTypes.AI);
        }
        CurrentPlayer = null;
    }

    private void HandleClickToTakeControl()
    {
        if (!_allowClickToTakeControl)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!CanTakeControlFromCurrentMode())
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                return;
            }
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, _clickRayDistance))
        {
            return;
        }

        CharacterProvider provider = hit.collider.GetComponentInParent<CharacterProvider>();
        if (provider == null || provider == CurrentPlayer)
        {
            return;
        }

        provider.TakeControl();
    }

    /// <summary>
    /// Restores control to the last character that was controlled by the player.
    /// </summary>
    public bool TryRestoreLastControlled(out CharacterProvider restoredProvider)
    {
        restoredProvider = null;
        CleanupDestroyedCharacters();

        if (LastControlledPlayer == null || !AllCharacters.Contains(LastControlledPlayer))
        {
            LastControlledPlayer = null;
            return false;
        }

        SetPlayer(LastControlledPlayer);
        restoredProvider = LastControlledPlayer;
        return true;
    }

    private bool CanTakeControlFromCurrentMode()
    {
        if (!CameraManager.HasInstance || CameraManager.Instance.CurrentMode != CameraManager.CameraMode.FreeCamera)
        {
            return true;
        }

        FreeCameraStateMachine stateMachine = CameraManager.Instance.stateMachine;
        if (stateMachine == null)
        {
            return true;
        }

        return stateMachine.CurrentState == FreeCameraStateMachine.FreeCameraState.None;
    }

    private void CleanupDestroyedCharacters()
    {
        AllCharacters.RemoveAll(character => character == null);
    }
}
