using System;
using System.Collections.Generic;
using GameResources;
using MoreMountains.Tools;
using UnityEngine;

[AddComponentMenu("Room Horror/Managers/Character Task Manager")]
public class CharacterTaskManager : MMSingleton<CharacterTaskManager>
{
    public event Action<CharacterRoleProfile, GatherTask> GatherTaskStarted;
    public event Action<CharacterRoleProfile, GatherTask> GatherTaskCompleted;
    public event Action<CharacterRoleProfile, GatherTask> GatherTaskFailed;
    public event Action<CharacterRoleProfile, GatherTask> GatherTaskCancelled;

    public List<CharacterRoleProfile> RegisteredCharacters { get; private set; } = new List<CharacterRoleProfile>();

    private void Start()
    {
        RefreshCharactersInScene();
    }

    public void RefreshCharactersInScene()
    {
        CharacterRoleProfile[] profiles = FindObjectsByType<CharacterRoleProfile>(FindObjectsSortMode.None);
        for (int i = 0; i < profiles.Length; i++)
        {
            RegisterCharacter(profiles[i]);
        }
    }

    public void RegisterCharacter(CharacterRoleProfile character)
    {
        if (character == null)
        {
            return;
        }

        if (!RegisteredCharacters.Contains(character))
        {
            RegisteredCharacters.Add(character);
        }
    }

    public void UnregisterCharacter(CharacterRoleProfile character)
    {
        if (character == null)
        {
            return;
        }

        RegisteredCharacters.Remove(character);
    }

    public List<CharacterRoleProfile> GetCharactersByRole(CharacterRoleType role)
    {
        CleanupDestroyedCharacters();

        List<CharacterRoleProfile> result = new List<CharacterRoleProfile>();
        for (int i = 0; i < RegisteredCharacters.Count; i++)
        {
            CharacterRoleProfile character = RegisteredCharacters[i];
            if (character != null && character.Role == role)
            {
                result.Add(character);
            }
        }

        return result;
    }

    public bool TryAssignGatherTask(ResourceNode resourceNode, out CharacterRoleProfile assignedCharacter)
    {
        return TryAssignGatherTask(resourceNode, CharacterRoleType.Worker, out assignedCharacter);
    }

    public bool TryAssignGatherTask(ResourceNode resourceNode, CharacterRoleType requiredRole, out CharacterRoleProfile assignedCharacter)
    {
        assignedCharacter = null;

        if (resourceNode == null)
        {
            return false;
        }

        CleanupDestroyedCharacters();

        float bestDistanceSqr = float.MaxValue;
        for (int i = 0; i < RegisteredCharacters.Count; i++)
        {
            CharacterRoleProfile candidate = RegisteredCharacters[i];
            if (!CanAssignGatherTask(candidate, resourceNode, requiredRole))
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - resourceNode.transform.position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                assignedCharacter = candidate;
            }
        }

        if (assignedCharacter == null)
        {
            return false;
        }

        return TryAssignGatherTask(assignedCharacter, resourceNode);
    }

    public bool TryAssignGatherTask(CharacterRoleProfile character, ResourceNode resourceNode)
    {
        if (!CanAssignGatherTask(character, resourceNode, character != null ? character.Role : CharacterRoleType.None))
        {
            return false;
        }

        return character.Gatherer.TryStartGatherTask(resourceNode, false);
    }

    public bool CancelTask(CharacterRoleProfile character)
    {
        if (character == null || character.Gatherer == null || !character.Gatherer.HasActiveTask)
        {
            return false;
        }

        character.Gatherer.CancelCurrentTask();
        return true;
    }

    public string GetTaskSummary(CharacterRoleProfile character)
    {
        if (character == null)
        {
            return "No character";
        }

        if (character.Gatherer == null)
        {
            return "No gatherer";
        }

        if (!character.Gatherer.HasActiveTask)
        {
            return "Idle";
        }

        GatherTask currentTask = character.Gatherer.CurrentTask;
        if (currentTask == null || currentTask.ResourceNode == null)
        {
            return "Task active";
        }

        return $"{currentTask.State} -> {currentTask.ResourceNode.name}";
    }

    public void NotifyGatherTaskStarted(CharacterGatherer gatherer, GatherTask task)
    {
        CharacterRoleProfile profile = ResolveProfile(gatherer);
        if (profile != null)
        {
            GatherTaskStarted?.Invoke(profile, task);
        }
    }

    public void NotifyGatherTaskCompleted(CharacterGatherer gatherer, GatherTask task)
    {
        CharacterRoleProfile profile = ResolveProfile(gatherer);
        if (profile != null)
        {
            GatherTaskCompleted?.Invoke(profile, task);
        }
    }

    public void NotifyGatherTaskFailed(CharacterGatherer gatherer, GatherTask task)
    {
        CharacterRoleProfile profile = ResolveProfile(gatherer);
        if (profile != null)
        {
            GatherTaskFailed?.Invoke(profile, task);
        }
    }

    public void NotifyGatherTaskCancelled(CharacterGatherer gatherer, GatherTask task)
    {
        CharacterRoleProfile profile = ResolveProfile(gatherer);
        if (profile != null)
        {
            GatherTaskCancelled?.Invoke(profile, task);
        }
    }

    private CharacterRoleProfile ResolveProfile(CharacterGatherer gatherer)
    {
        if (gatherer == null)
        {
            return null;
        }

        CharacterRoleProfile profile = gatherer.GetComponent<CharacterRoleProfile>();
        if (profile != null)
        {
            RegisterCharacter(profile);
        }

        return profile;
    }

    private bool CanAssignGatherTask(CharacterRoleProfile character, ResourceNode resourceNode, CharacterRoleType requiredRole)
    {
        if (character == null || resourceNode == null)
        {
            return false;
        }

        if (requiredRole != CharacterRoleType.None && character.Role != requiredRole)
        {
            return false;
        }

        if (!character.IsAiControlled)
        {
            return false;
        }

        if (!character.CanAcceptTask(CharacterTaskType.GatherResources))
        {
            return false;
        }

        if (character.HasActiveTask)
        {
            return false;
        }

        return character.Gatherer != null;
    }

    private void CleanupDestroyedCharacters()
    {
        RegisteredCharacters.RemoveAll(character => character == null);
    }
}
