using UnityEngine;

namespace Ares.World
{
    /// <summary>Single Three.js -> Unity reflection. Three Euler rotations use XYZ order.</summary>
    public static class ReferenceSpace
    {
        public static Vector3 Position(float x, float y, float z) => new Vector3(x, y, -z);
        public static Quaternion Rotation(float x, float y, float z, float w) => new Quaternion(-x, -y, z, w);
        public static Quaternion Euler(float x, float y, float z) =>
            Quaternion.AngleAxis(-x * Mathf.Rad2Deg, Vector3.right) *
            Quaternion.AngleAxis(-y * Mathf.Rad2Deg, Vector3.up) *
            Quaternion.AngleAxis(z * Mathf.Rad2Deg, Vector3.forward);
    }
}
