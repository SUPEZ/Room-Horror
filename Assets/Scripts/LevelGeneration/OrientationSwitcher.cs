using UnityEngine;

namespace LevelGeneration
{
    public class OrientationSwitcher : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera; // optional override; falls back to Camera.main
        [SerializeField] private GameObject whenSameDirectionEnabled;
        [SerializeField] private GameObject whenOppositeDirectionEnabled;
        [SerializeField] private GameObject whenPerpendicularEnabled;
        [SerializeField, Range(0f, 180f)] private float angleTolerance = 45f;

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
                SetActiveStates(false, false, false);
                return;
            }

            var angle = Vector3.Angle(camFlat, selfFlat);

            if (Mathf.Abs(angle - 0f) <= angleTolerance)
            {
                SetActiveStates(true, false, false); // same direction
                return;
            }

            if (Mathf.Abs(angle - 180f) <= angleTolerance)
            {
                SetActiveStates(false, true, false); // opposite direction
                return;
            }

            if (Mathf.Abs(angle - 90f) <= angleTolerance)
            {
                SetActiveStates(false, false, true); // perpendicular
                return;
            }

            SetActiveStates(false, false, false); // other angles
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
