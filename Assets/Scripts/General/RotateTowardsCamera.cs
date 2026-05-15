using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowardsCamera : MonoBehaviour
{
    private Vector3 _initialEulerAngles;

    [Header("Lock Axes (set rotation to 0)")]
    public bool LimitX = false;
    public bool LimitZ = false;
    public bool LimitY = false;
    public bool Reverse = false;

    [Header("Clamp X Rotation Offset")]
    public bool ClampX = false;
    [Range(-180f, 180f)] public float MinAngleX = -45f;
    [Range(-180f, 180f)] public float MaxAngleX = 45f;

    [Header("Clamp Y Rotation Offset")]
    public bool ClampY = false;
    [Range(-180f, 180f)] public float MinAngleY = -90f;
    [Range(-180f, 180f)] public float MaxAngleY = 90f;

    [Header("Clamp Z Rotation Offset")]
    public bool ClampZ = false;
    [Range(-180f, 180f)] public float MinAngleZ = -45f;
    [Range(-180f, 180f)] public float MaxAngleZ = 45f;

    private void Awake()
    {
        _initialEulerAngles = transform.eulerAngles;
    }

    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 rotation = Quaternion.LookRotation(Reverse ? 
                (Camera.main.transform.position - transform.position) : 
                (transform.position - Camera.main.transform.position)).eulerAngles;

            if (LimitX)
                rotation.x = 0f;
            else if (ClampX)
                rotation.x = ClampAngleOffset(rotation.x, _initialEulerAngles.x, MinAngleX, MaxAngleX);

            if (LimitZ)
                rotation.z = 0f;
            else if (ClampZ)
                rotation.z = ClampAngleOffset(rotation.z, _initialEulerAngles.z, MinAngleZ, MaxAngleZ);

            if (LimitY)
                rotation.y = 0f;
            else if (ClampY)
                rotation.y = ClampAngleOffset(rotation.y, _initialEulerAngles.y, MinAngleY, MaxAngleY);

            transform.rotation = Quaternion.Euler(rotation);
        }
    }

    /// <summary>
    /// Clamps the current angle offset from the initial angle to a signed range.
    /// </summary>
    private float ClampAngleOffset(float angle, float initialAngle, float min, float max)
    {
        float offset = Mathf.DeltaAngle(initialAngle, angle);
        return initialAngle + Mathf.Clamp(offset, min, max);
    }
}
