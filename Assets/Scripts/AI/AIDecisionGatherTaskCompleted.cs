using GameResources;
using MoreMountains.Tools;
using UnityEngine;

[AddComponentMenu("Room Horror/AI/Decisions/AI Decision Gather Task Completed")]
public class AIDecisionGatherTaskCompleted : AIDecision
{
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

        return _characterGatherer.LastTask.State == GatherTaskState.Completed &&
               _characterGatherer.LastTask.StateChangedAt >= _stateEnteredAt;
    }
}
