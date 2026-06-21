using UnityEngine;
using System;

namespace LevelGeneration
{
    public class OrientationSwitcher : MonoBehaviour
    {
        public enum Orientation
        {
            None,
            SameDirection,
            OppositeDirection,
            Perpendicular
        }

        public event Action<Orientation> OrientationChanged;

        [SerializeField] private Camera mainCamera; // optional override; falls back to Camera.main
        [SerializeField] private GameObject whenSameDirectionEnabled;
        [SerializeField] private GameObject whenOppositeDirectionEnabled;
        [SerializeField] private GameObject whenPerpendicularEnabled;
        [SerializeField, Range(0f, 180f)] private float angleTolerance = 45f;

        private Orientation currentOrientation = Orientation.None;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("OrientationSwitcher: No camera assigned and Camera.main is null; switching disabled.", this);
                }
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                return;
            }

            var camFlat = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
            var selfFlat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            if (camFlat.sqrMagnitude <= Mathf.Epsilon || selfFlat.sqrMagnitude <= Mathf.Epsilon)
            {
                SetOrientation(Orientation.None);
                return;
            }

            var angle = Vector3.Angle(camFlat, selfFlat);

            if (Mathf.Abs(angle - 0f) <= angleTolerance)
            {
                SetOrientation(Orientation.SameDirection);
                return;
            }

            if (Mathf.Abs(angle - 180f) <= angleTolerance)
            {
                SetOrientation(Orientation.OppositeDirection);
                return;
            }

            if (Mathf.Abs(angle - 90f) <= angleTolerance)
            {
                SetOrientation(Orientation.Perpendicular);
                return;
            }

            SetOrientation(Orientation.None);
        }

        private void SetOrientation(Orientation orientation)
        {
            bool same = orientation == Orientation.SameDirection;
            bool opposite = orientation == Orientation.OppositeDirection;
            bool perpendicular = orientation == Orientation.Perpendicular;

            SetActiveStates(same, opposite, perpendicular);

            if (currentOrientation == orientation)
            {
                return;
            }

            currentOrientation = orientation;
            OrientationChanged?.Invoke(currentOrientation);
        }

        private void SetActiveStates(bool same, bool opposite, bool perpendicular)
        {
            if (whenSameDirectionEnabled != null)
            {
                whenSameDirectionEnabled.SetActive(same);
            }

            if (whenOppositeDirectionEnabled != null)
            {
                whenOppositeDirectionEnabled.SetActive(opposite);
            }

            if (whenPerpendicularEnabled != null)
            {
                whenPerpendicularEnabled.SetActive(perpendicular);
            }
        }
    }
}
