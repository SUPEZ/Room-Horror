using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowardsCamera : MonoBehaviour
{
    public bool LimitX = false;
    public bool LimitZ = false;
    public bool LimitY = false;
    public bool Reverse = false;

    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 rotation = Quaternion.LookRotation(Reverse ? 
                (Camera.main.transform.position - transform.position) : 
                (transform.position - Camera.main.transform.position)).eulerAngles;

            if(LimitX)
                rotation.x = 0f;

            if(LimitZ)
                rotation.z = 0f;
            
            if(LimitY)
                rotation.y = 0f;

            transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
