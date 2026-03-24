using UnityEngine;

namespace GameResources
{
    [System.Serializable]
    public class ResourceApproachSlot
    {
        [SerializeField] private int _index;
        [SerializeField] private Vector3 _localPosition;

        private Transform _reservedBy;

        public int Index => _index;
        public Vector3 LocalPosition => _localPosition;
        public bool IsReserved => _reservedBy != null;
        public Transform ReservedBy => _reservedBy;

        public void Configure(int index, Vector3 localPosition)
        {
            _index = index;
            _localPosition = localPosition;
            _reservedBy = null;
        }

        public Vector3 GetWorldPosition(Transform resourceTransform)
        {
            return resourceTransform.TransformPoint(_localPosition);
        }

        public bool IsReservedBy(Transform worker)
        {
            return _reservedBy == worker;
        }

        public bool TryReserve(Transform worker)
        {
            if (worker == null)
            {
                return false;
            }

            if (_reservedBy != null && _reservedBy != worker)
            {
                return false;
            }

            _reservedBy = worker;
            return true;
        }

        public bool Release(Transform worker)
        {
            if (worker == null || _reservedBy != worker)
            {
                return false;
            }

            _reservedBy = null;
            return true;
        }

        public void ClearReservation()
        {
            _reservedBy = null;
        }
    }
}
