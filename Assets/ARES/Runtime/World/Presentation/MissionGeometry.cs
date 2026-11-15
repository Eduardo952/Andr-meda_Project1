using System.Collections.Generic;
using Ares.World.Rover;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.World
{
    /// <summary>Closed, reusable presentation meshes. Removes zero-area cap triangles from the authoring helper.</summary>
    internal static class MissionGeometry
    {
        public static Mesh Finish(RoverMesh author, string name)
        {
            var mesh = author.Bake(name); var vertices = mesh.vertices; var source = mesh.triangles;
            var triangles = new List<int>(source.Length);
            for (int i = 0; i < source.Length; i += 3)
                if (Vector3.Cross(vertices[source[i+1]]-vertices[source[i]], vertices[source[i+2]]-vertices[source[i]]).sqrMagnitude > 1e-14f)
                { triangles.Add(source[i]); triangles.Add(source[i+1]); triangles.Add(source[i+2]); }
            if (vertices.Length < 65536) mesh.indexFormat = IndexFormat.UInt16;
            mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public static Mesh Sphere()
        {
            const int sides=20, rows=14;
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int y=0;y<=rows;y++) for(int x=0;x<=sides;x++)
            {float a=Mathf.PI*y/rows,b=2*Mathf.PI*x/sides;vertices.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)));}
            for(int y=0;y<rows;y++) for(int x=0;x<sides;x++)
            {int a=y*(sides+1)+x,b=a+sides+1;if(y>0){indices.Add(a);indices.Add(a+1);indices.Add(b);}if(y<rows-1){indices.Add(a+1);indices.Add(b+1);indices.Add(b);}}
            var mesh=new Mesh{name="Mission rounded detail / 520 triangles"};mesh.SetVertices(vertices);mesh.SetNormals(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
        }
        public static Transform Render(Transform parent,string name,Mesh mesh,Material material,Vector3 position,Vector3 scale)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go.transform;
        }
    }
}
