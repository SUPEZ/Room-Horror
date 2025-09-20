using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.BetterLit
{
   [ExecuteAlways]
   public class ShapeEffectorGroup : MonoBehaviour
   {
      public List<ShapeEffector> effectors = new List<ShapeEffector>();
      public List<Material> materials = new List<Material>();

      // Arrays to store matrices and effect data for up to 8 effectors
      Matrix4x4[] mtxs = new Matrix4x4[8]; // The transformation matrices
      Matrix4x4[] invMtxs = new Matrix4x4[8]; // The inverse transformation matrices
      Vector4[] data = new Vector4[8];

      private void Update()
      {
         int count = effectors.Count;

         // Limiting to 8 effectors
         if (count > 8)
         { 
            Debug.LogError("Maximum of 8 effectors per group, additional effectors ignored");
         }

         // Loop through each effector
         for (int i = 0; i < count; ++i)
         {
            // Store the localToWorldMatrix which includes non-uniform scaling
            mtxs[i] = effectors[i].transform.localToWorldMatrix;

            // Calculate the inverse of the localToWorldMatrix and store it
            invMtxs[i] = mtxs[i].inverse;

            // Store the shape type and contrast in the data vector
            data[i].x = (int)effectors[i].shape; // Shape type: Sphere, Plane, or Cube
            data[i].y = effectors[i].contrast;   // Contrast value
         }

         // Update all materials with the effector matrices and data
         foreach (var m in materials)
         {
            m.SetMatrixArray("_EffectorMtx", mtxs);     // Pass the transformation matrices
            m.SetMatrixArray("_EffectorInvMtx", invMtxs); // Pass the inverse transformation matrices
            m.SetVectorArray("_EffectorData", data);    // Pass the effector data (shape type, contrast)
            m.SetFloat("_EffectorCount", count);        // Pass the count of effectors
         }
      }
   }
}

