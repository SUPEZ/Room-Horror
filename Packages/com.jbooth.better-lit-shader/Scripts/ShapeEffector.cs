using UnityEngine;

namespace JBooth.BetterLit
{
    public class ShapeEffector : MonoBehaviour
    {
        public enum Shape
        {
            Plane,
            Sphere,
            Cube
        }

        public Shape shape = Shape.Sphere;
        [Range(0.001f, 1000)]
        public float contrast = 1;

        static Mesh sphere;
        static Mesh quad;
        static Mesh cube;

        private void OnDrawGizmosSelected()
        {
            if (sphere == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere = go.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(go);
            }
            if (quad == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad = go.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(go);
            }
            if (cube == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = go.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(go);
            }

            Gizmos.color = new Color(0, 1, 1, 0.3f);

            if (shape == Shape.Sphere)
            {
                // Apply non-uniform scaling using the GameObject's transform scale (lossyScale)
                Gizmos.DrawMesh(sphere, 0, transform.position, transform.rotation, transform.lossyScale);
            }
            else if (shape == Shape.Plane)
            {
                Vector3 worldScale = new Vector3(10 * transform.lossyScale.x, 10 * transform.lossyScale.y, 1);
                Gizmos.DrawMesh(quad, 0, transform.position, transform.rotation, worldScale);
            }
            else if (shape == Shape.Cube)
            {
                // Apply non-uniform scaling using the GameObject's transform scale (lossyScale), just like the sphere
                Gizmos.DrawMesh(cube, 0, transform.position, transform.rotation, transform.lossyScale);
            }
        }
    }
}
