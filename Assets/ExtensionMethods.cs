using Parabox.CSG;
using UnityEngine;

namespace ExtensionMethods
{
    public static class Vector2Extensions
    {
        public static Vector2 Rotate90Right(this Vector2 vec)
        {
            float ang = Mathf.Atan2(vec.y, vec.x);
            ang += Mathf.PI / 2;
            if (ang > 2 * Mathf.PI) ang -= 2 * Mathf.PI;
            float mag = vec.magnitude;

            return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)).normalized * mag;
        }

        public static Vector2 Rotate90Left(this Vector2 vec) {
            float ang = Mathf.Atan2(vec.y, vec.x);
            ang -= Mathf.PI / 2;
            if (ang < 0) ang += 2 * Mathf.PI;
            float mag = vec.magnitude;

            return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)).normalized * mag;
        }
    }

    public static class Vector3Extensions
    {
        /// <summary>
        /// Rotates a Vector3 by an angle on a plane, returns null if no plane;
        /// </summary>
        /// <param name="angle">the angle to rotate by in radians</param>
        /// <param name="normal">the normal of the plane on which to rotate</param>
        /// <param name="left">if true, will rotate left by angle, otherwise will rotate right</param>
        public static Vector3 RotateBy(this Vector3 vec, float angle, Vector3 normal, bool left)
        {
            if (normal == Vector3.zero) return Vector3.zero; // no plane by which to rotate

            // Find relative axes of plane
            Vector3 i = vec.normalized;
            int negative = left ? 1 : -1;
            Vector3 j = negative * Vector3.Cross(vec, normal).normalized;

            // Get projection of vec onto plane
            Vector3 offset = Vector3.Dot(vec, normal) * normal; // Distance from plane
            Vector3 proj = vec - offset;

            // Find new relative x and y components
            float magnitude = proj.magnitude;
            Vector3 x = magnitude * Mathf.Cos(angle) * i;
            Vector3 y = magnitude * Mathf.Sin(angle) * j;
            Vector3 newProj = x + y;

            // Add offset back to projection to find original rotated vector
            return newProj + offset;
        }

        /// <summary>
        /// Gets component of a vector parallel to a certain plane defined by its normal
        /// </summary>
        /// <param name="norm">parallel plane</param>
        public static Vector3 PlanarComponent(this Vector3 vec, Vector3 norm)
        {
            norm.Normalize();
            return vec - (norm * Vector3.Dot(vec, norm));
        }
    }
    
    public static class CSGExtensions
    {
        /// <summary>
        /// Perform a CSG function based on its index
        /// </summary>
        /// <param name="funcIdx"></param>
        /// <param name="lhs">object to perform function on</param>
        /// <param name="rhs">object to subtract/add/intersect</param>
        /// <returns>Result GameObject</returns>
        public static GameObject PerformCSGFunc(this GameObject lhs, GameObject rhs, CSGFunc func)
        {
            Model result;
            switch (func)
            {
                case CSGFunc.Subtract:
                    result = CSG.Subtract(lhs, rhs);
                    break;
                case CSGFunc.Union:
                    result = CSG.Union(lhs, rhs);
                    break;
                case CSGFunc.Intersect:
                    result = CSG.Intersect(lhs, rhs);
                    break;
                default:
                    Debug.LogWarning(string.Format("CSG Function {0} not found", func));
                    return null;
            }

            GameObject resultObj = new(string.Format("{0} Result", func));
            resultObj.AddComponent<MeshFilter>().sharedMesh = result.mesh;
            resultObj.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
            resultObj.AddComponent<MeshCollider>();
            resultObj.layer = lhs.layer;

            return resultObj;
        }
    }
}