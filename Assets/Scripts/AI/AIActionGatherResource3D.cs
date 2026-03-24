using GameResources;
using MoreMountains.Tools;
using UnityEngine;

[AddComponentMenu("Room Horror/AI/Actions/AI Action Gather Resource 3D")]
public class AIActionGatherResource3D : AIAction
{
    [Header("Target")]
    [Tooltip("Optional explicit resource target. If empty, Brain.Target will be used.")]
    public ResourceNode TargetResource;

    [Tooltip("If true, this action reads the target resource from Brain.Target.")]
    public bool UseBrainTarget = true;

    [Header("Execution")]
    [Tooltip("When true, CharacterGatherer will pause the brain while the task runs. Keep false when used inside AIBrain states.")]
    public bool PauseBrainWhileGathering;

    [Tooltip("If true, exiting the state cancels the active gather task.")]
    public bool CancelTaskOnExitState = true;

    protected CharacterGatherer _characterGatherer;

    public override void Initialization()
    {
        if (!ShouldInitialize)
        {
            return;
        }

        base.Initialization();
        _characterGatherer = GetComponentInParent<CharacterGatherer>();

        if (_characterGatherer == null)
        {
            Debug.LogWarning($"{name} : AIActionGatherResource3D requires a CharacterGatherer component on the character.", this);
        }
    }

    public override void PerformAction()
    {
        if (_characterGatherer == null)
        {
            return;
        }

        ResourceNode targetResource = ResolveTargetResource();
        if (targetResource == null)
        {
            return;
        }

        if (_characterGatherer.HasTaskFor(targetResource))
        {
            ActionInProgress = true;
            return;
        }

        if (_characterGatherer.HasActiveTask)
        {
            _characterGatherer.CancelCurrentTask();
        }

        ActionInProgress = _characterGatherer.TryStartGatherTask(targetResource, PauseBrainWhileGathering);
    }

    public override void OnExitState()
    {
        base.OnExitState();

        if (CancelTaskOnExitState)
        {
            _characterGatherer?.CancelCurrentTask();
        }
    }

    protected virtual ResourceNode ResolveTargetResource()
    {
        if (!UseBrainTarget)
        {
            return TargetResource;
        }

        if (_brain.Target == null)
        {
            return TargetResource;
        }

        ResourceNode resourceNode = _brain.Target.GetComponent<ResourceNode>();
        if (resourceNode != null)
        {
            return resourceNode;
        }

        return _brain.Target.GetComponentInParent<ResourceNode>();
    }
}
