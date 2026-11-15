using System.Collections.Generic;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>Small mesh authoring toolkit. Flat bevel normals and lathed profiles, no runtime primitive objects.</summary>
    public sealed class RoverMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly List<Vector2> uv = new List<Vector2>();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
            triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            triangles.Add(n); triangles.Add(n + 2); triangles.Add(n + 3);
        }
        public void Box(Vector3 center, Vector3 size, Quaternion rotation, float bevel = .08f)
        {
            // Three octagonal rings create chamfered corners and a sloped shoulder.
            float x = size.x * .5f, z = size.z * .5f, h = size.y * .5f;
            float b = Mathf.Min(bevel, Mathf.Min(x, Mathf.Min(z, h)) * .75f);
            var rings = new Vector3[4][];
            for (int r = 0; r < 4; r++)
            {
                float inset = r == 0 || r == 3 ? b : 0;
                float rx = x - inset, rz = z - inset;
                float y = r == 0 ? -h : r == 1 ? -h + b : r == 2 ? h - b : h;
                float corner = Mathf.Max(.001f, b);
                rings[r] = new[] { new Vector3(-rx+corner,y,-rz), new Vector3(rx-corner,y,-rz),
                    new Vector3(rx,y,-rz+corner), new Vector3(rx,y,rz-corner), new Vector3(rx-corner,y,rz),
                    new Vector3(-rx+corner,y,rz), new Vector3(-rx,y,rz-corner), new Vector3(-rx,y,-rz+corner) };
                for (int i = 0; i < 8; i++) rings[r][i] = center + rotation * rings[r][i];
            }
            for (int r = 0; r < 3; r++) for (int i = 0; i < 8; i++)
            { int j = (i + 1) % 8; Quad(rings[r][i], rings[r+1][i], rings[r+1][j], rings[r][j]); }
            for (int i = 1; i < 7; i++)
            {
                Quad(rings[0][0], rings[0][i], rings[0][i+1], rings[0][i+1]);
                Quad(rings[3][0], rings[3][i+1], rings[3][i], rings[3][i]);
            }
        }
        public void Tube(Vector3 a, Vector3 b, float radius, int sides = 12)
        {
            var rotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
            Lathe((a+b)*.5f, rotation, new[] { new Vector2(-(b-a).magnitude*.5f, 0),
                new Vector2(-(b-a).magnitude*.5f, radius), new Vector2((b-a).magnitude*.5f, radius), new Vector2((b-a).magnitude*.5f, 0) }, sides);
        }
        // Profile is (axial X, radius). Wheels rotate about local X.
        public void Lathe(Vector3 center, Quaternion rotation, Vector2[] profile, int segments = 32)
        {
            for (int p = 0; p < profile.Length - 1; p++) for (int s = 0; s < segments; s++)
            {
                float a = s * Mathf.PI * 2 / segments, b = (s+1) * Mathf.PI * 2 / segments;
                Vector3 Point(Vector2 v, float angle) => center + rotation * new Vector3(v.x, Mathf.Cos(angle)*v.y, Mathf.Sin(angle)*v.y);
                Quad(Point(profile[p],a), Point(profile[p],b), Point(profile[p+1],b), Point(profile[p+1],a));
            }
        }
        public Mesh Bake(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
