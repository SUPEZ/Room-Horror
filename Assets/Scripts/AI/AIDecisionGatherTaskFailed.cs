using GameResources;
using MoreMountains.Tools;
using UnityEngine;

[AddComponentMenu("Room Horror/AI/Decisions/AI Decision Gather Task Failed")]
public class AIDecisionGatherTaskFailed : AIDecision
{
    [Tooltip("Treat cancelled gather tasks as failures for this decision.")]
    public bool CancelledCountsAsFailure = true;

    protected CharacterGatherer _characterGatherer;
    protected float _stateEnteredAt;

    public override void Initialization()
    {
        base.Initialization();
        _characterGatherer = GetComponentInParent<CharacterGatherer>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _stateEnteredAt = Time.time;
    }

    public override bool Decide()
    {
        if (_characterGatherer == null || _characterGatherer.LastTask == null)
        {
            return false;
        }

        GatherTaskState state = _characterGatherer.LastTask.State;
        bool failed = state == GatherTaskState.Failed || (CancelledCountsAsFailure && state == GatherTaskState.Cancelled);

        return failed && _characterGatherer.LastTask.StateChangedAt >= _stateEnteredAt;
    }
}
