using UnityEngine;

namespace Ares.World
{
    /// <summary>Cached, two-sided triangle/segment query without adding physics colliders.</summary>
    public sealed class MeshSightline
    {
        readonly Vector3[] vertices;
        readonly int[] triangles;
        public MeshSightline(Mesh mesh) { vertices = mesh.vertices; triangles = mesh.triangles; }
        public bool Intersects(Transform transform, Vector3 from, Vector3 to)
        {
            Vector3 origin = transform.InverseTransformPoint(from);
            Vector3 direction = transform.InverseTransformPoint(to) - origin;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], edge1 = vertices[triangles[i + 1]] - a, edge2 = vertices[triangles[i + 2]] - a;
                Vector3 cross = Vector3.Cross(direction, edge2);
                float determinant = Vector3.Dot(edge1, cross);
                if (Mathf.Abs(determinant) < .000001f) continue;
                float inverse = 1 / determinant;
                Vector3 offset = origin - a;
                float u = Vector3.Dot(offset, cross) * inverse;
                if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(offset, edge1);
                float v = Vector3.Dot(direction, q) * inverse;
                if (v < 0 || u + v > 1) continue;
                float fraction = Vector3.Dot(edge2, q) * inverse;
                if (fraction > 0 && fraction < 1) return true;
            }
            return false;
        }
    }
}
