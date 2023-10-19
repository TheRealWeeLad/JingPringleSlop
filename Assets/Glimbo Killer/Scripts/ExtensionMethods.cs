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
    }
}