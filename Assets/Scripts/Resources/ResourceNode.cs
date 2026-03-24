using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GameResources
{
    /// <summary>
    /// Base world resource that can be gathered by characters.
    /// Later steps will add AI task integration around this node.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Room Horror/Resources/Resource Node")]
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField] private ResourceType _resourceType = ResourceType.Wood;
        [SerializeField] [Min(1)] private int _startingAmount = 100;
        [SerializeField] [Min(1)] private int _yieldPerCycle = 5;
        [SerializeField] [Min(0.1f)] private float _gatherDuration = 3f;

        [Header("Access")]
        [SerializeField] [Min(1)] private int _maxWorkers = 1;
        [SerializeField] [Min(1)] private int _slotCount = 8;
        [SerializeField] [Min(0.5f)] private float _approachRadius = 2.5f;
        [SerializeField] [Min(0.1f)] private float _stopDistance = 1.25f;
        [SerializeField] [Min(0.1f)] private float _navMeshSampleDistance = 1.5f;

        [Header("Lifecycle")]
        [SerializeField] private bool _deactivateWhenDepleted = true;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private bool _drawSlotLabels;

        [Header("Generated Slots")]
        [SerializeField] private List<ResourceApproachSlot> _approachSlots = new List<ResourceApproachSlot>();

        private int _currentAmount;
        private int _currentWorkers;

        public ResourceType Type => _resourceType;
        public int StartingAmount => _startingAmount;
        public int CurrentAmount => _currentAmount;
        public int YieldPerCycle => _yieldPerCycle;
        public float GatherDuration => _gatherDuration;
        public int MaxWorkers => _maxWorkers;
        public int CurrentWorkers => _currentWorkers;
        public int SlotCount => _slotCount;
        public float ApproachRadius => _approachRadius;
        public float StopDistance => _stopDistance;
        public IReadOnlyList<ResourceApproachSlot> ApproachSlots => _approachSlots;
        public bool IsDepleted => _currentAmount <= 0;
        public bool HasWorkerCapacity => _currentWorkers < _maxWorkers;
        public bool CanBeGathered => !IsDepleted;

        public event Action<ResourceNode> Depleted;
        public event Action<ResourceNode, int> AmountChanged;

        private void Awake()
        {
            ResetNode();
            RegenerateApproachSlots();
        }

        private void OnDisable()
        {
            ClearAllReservations();
        }

        public void ResetNode()
        {
            _currentAmount = Mathf.Max(0, _startingAmount);
            ClearAllReservations();
        }

        public void RegenerateApproachSlots()
        {
            EnsureSlotListSize();

            float angleStep = 360f / _slotCount;
            for (int i = 0; i < _slotCount; i++)
            {
                float angleRadians = angleStep * i * Mathf.Deg2Rad;
                Vector3 radialOffset = new Vector3(Mathf.Cos(angleRadians), 0f, Mathf.Sin(angleRadians)) * _approachRadius;
                Vector3 desiredWorldPosition = transform.position + radialOffset;
                Vector3 slotWorldPosition = SampleSlotPosition(desiredWorldPosition);
                Vector3 slotLocalPosition = transform.InverseTransformPoint(slotWorldPosition);

                _approachSlots[i].Configure(i, slotLocalPosition);
            }

            _currentWorkers = 0;
        }

        public bool TryReserveClosestSlot(Transform worker, Vector3 fromPosition, out ResourceApproachSlot reservedSlot)
        {
            reservedSlot = null;

            if (worker == null || !CanBeGathered)
            {
                return false;
            }

            ResourceApproachSlot existingReservation = GetReservedSlot(worker);
            if (existingReservation != null)
            {
                reservedSlot = existingReservation;
                return true;
            }

            if (!HasWorkerCapacity)
            {
                return false;
            }

            float bestDistanceSqr = float.MaxValue;
            ResourceApproachSlot bestSlot = null;

            for (int i = 0; i < _approachSlots.Count; i++)
            {
                ResourceApproachSlot slot = _approachSlots[i];
                if (slot.IsReserved)
                {
                    continue;
                }

                float distanceSqr = (slot.GetWorldPosition(transform) - fromPosition).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestSlot = slot;
                }
            }

            if (bestSlot == null || !bestSlot.TryReserve(worker))
            {
                return false;
            }

            _currentWorkers++;
            reservedSlot = bestSlot;
            return true;
        }

        public bool TryGetReservedSlotPosition(Transform worker, out Vector3 slotPosition)
        {
            slotPosition = transform.position;

            ResourceApproachSlot reservedSlot = GetReservedSlot(worker);
            if (reservedSlot == null)
            {
                return false;
            }

            slotPosition = reservedSlot.GetWorldPosition(transform);
            return true;
        }

        public bool ReleaseSlot(Transform worker)
        {
            if (worker == null)
            {
                return false;
            }

            ResourceApproachSlot reservedSlot = GetReservedSlot(worker);
            if (reservedSlot == null)
            {
                return false;
            }

            if (!reservedSlot.Release(worker))
            {
                return false;
            }

            _currentWorkers = Mathf.Max(0, _currentWorkers - 1);
            return true;
        }

        public bool TryGatherCycle(out int gatheredAmount)
        {
            return TryGather(_yieldPerCycle, out gatheredAmount);
        }

        public bool TryGather(int requestedAmount, out int gatheredAmount)
        {
            gatheredAmount = 0;

            if (requestedAmount <= 0 || IsDepleted)
            {
                return false;
            }

            gatheredAmount = Mathf.Min(requestedAmount, _currentAmount);
            _currentAmount -= gatheredAmount;
            AmountChanged?.Invoke(this, _currentAmount);

            if (_currentAmount <= 0)
            {
                _currentAmount = 0;
                Depleted?.Invoke(this);

                if (_deactivateWhenDepleted)
                {
                    gameObject.SetActive(false);
                }
            }

            return gatheredAmount > 0;
        }

        private ResourceApproachSlot GetReservedSlot(Transform worker)
        {
            for (int i = 0; i < _approachSlots.Count; i++)
            {
                if (_approachSlots[i].IsReservedBy(worker))
                {
                    return _approachSlots[i];
                }
            }

            return null;
        }

        private void ClearAllReservations()
        {
            for (int i = 0; i < _approachSlots.Count; i++)
            {
                _approachSlots[i].ClearReservation();
            }

            _currentWorkers = 0;
        }

        private void EnsureSlotListSize()
        {
            _slotCount = Mathf.Max(_slotCount, _maxWorkers);

            while (_approachSlots.Count < _slotCount)
            {
                _approachSlots.Add(new ResourceApproachSlot());
            }

            if (_approachSlots.Count > _slotCount)
            {
                _approachSlots.RemoveRange(_slotCount, _approachSlots.Count - _slotCount);
            }
        }

        private Vector3 SampleSlotPosition(Vector3 desiredWorldPosition)
        {
            if (NavMesh.SamplePosition(desiredWorldPosition, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return desiredWorldPosition;
        }

        private void OnValidate()
        {
            _startingAmount = Mathf.Max(1, _startingAmount);
            _yieldPerCycle = Mathf.Max(1, _yieldPerCycle);
            _gatherDuration = Mathf.Max(0.1f, _gatherDuration);
            _maxWorkers = Mathf.Max(1, _maxWorkers);
            _slotCount = Mathf.Max(_maxWorkers, _slotCount);
            _approachRadius = Mathf.Max(0.5f, _approachRadius);
            _stopDistance = Mathf.Clamp(_stopDistance, 0.1f, _approachRadius);
            _navMeshSampleDistance = Mathf.Max(0.1f, _navMeshSampleDistance);

            RegenerateApproachSlots();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _approachRadius);

            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _stopDistance);

            for (int i = 0; i < _approachSlots.Count; i++)
            {
                ResourceApproachSlot slot = _approachSlots[i];
                Vector3 slotPosition = slot.GetWorldPosition(transform);

                Gizmos.color = slot.IsReserved ? new Color(1f, 0.25f, 0.25f, 0.9f) : new Color(0.25f, 0.8f, 1f, 0.9f);
                Gizmos.DrawSphere(slotPosition, 0.15f);

#if UNITY_EDITOR
                if (_drawSlotLabels)
                {
                    UnityEditor.Handles.Label(slotPosition + Vector3.up * 0.25f, $"Slot {slot.Index}");
                }
#endif
            }
        }
    }
}
