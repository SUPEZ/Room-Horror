using System;
using UnityEngine;

namespace GameResources
{
    [Serializable]
    public class GatherTask
    {
        public GatherTask(ResourceNode resourceNode, ResourceApproachSlot reservedSlot, Transform worker)
        {
            ResourceNode = resourceNode;
            ReservedSlot = reservedSlot;
            Worker = worker;
            State = GatherTaskState.None;
            StateChangedAt = Time.time;
        }

        public ResourceNode ResourceNode { get; }
        public ResourceApproachSlot ReservedSlot { get; }
        public Transform Worker { get; }
        public GatherTaskState State { get; private set; }
        public float StateChangedAt { get; private set; }
        public float GatheringStartedAt { get; private set; }
        public int GatheredAmount { get; private set; }
        public string FailureReason { get; private set; }

        public bool IsTerminal =>
            State == GatherTaskState.Completed ||
            State == GatherTaskState.Failed ||
            State == GatherTaskState.Cancelled;

        public float TimeInCurrentState => Time.time - StateChangedAt;

        public Vector3 SlotWorldPosition =>
            ReservedSlot != null && ResourceNode != null
                ? ReservedSlot.GetWorldPosition(ResourceNode.transform)
                : Vector3.zero;

        public void MarkMovingToSlot()
        {
            SetState(GatherTaskState.MovingToSlot);
        }

        public void BeginGathering()
        {
            SetState(GatherTaskState.Gathering);
            GatheringStartedAt = Time.time;
        }

        public void Complete(int gatheredAmount)
        {
            GatheredAmount = gatheredAmount;
            FailureReason = null;
            SetState(GatherTaskState.Completed);
        }

        public void Fail(string reason)
        {
            GatheredAmount = 0;
            FailureReason = reason;
            SetState(GatherTaskState.Failed);
        }

        public void Cancel()
        {
            GatheredAmount = 0;
            FailureReason = null;
            SetState(GatherTaskState.Cancelled);
        }

        private void SetState(GatherTaskState newState)
        {
            State = newState;
            StateChangedAt = Time.time;
        }
    }
}
