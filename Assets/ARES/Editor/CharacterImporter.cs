using System;
using System.Collections.Generic;
using System.IO;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.Editor
{
    /// <summary>Imports local-space geometry and original parent/pivot transforms into reusable prefabs.</summary>
    public static class CharacterImporter
    {
        [Serializable] public sealed class FileData { public ModelData[] characters; }
        [Serializable] public sealed class ModelData { public string id; public NodeData[] nodes; public GeometryData[] meshes; }
        [Serializable] public sealed class NodeData { public string name, binding; public int parent, mesh; public float[] position, rotation, scale; }
        [Serializable] public sealed class GeometryData
        {
            public string name, color, emissive;
            public float[] vertices, normals;
            public int[] indices;
            public float glow, opacity, metalness, roughness;
            public bool transparent, unlit, depthWrite;
        }
        public const string Root = "Assets/ARES/Generated/Characters";

        public static void ImportInto(Transform world)
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var data = JsonUtility.FromJson<FileData>(File.ReadAllText("Assets/ARES/Import/characters.json"));
            foreach (var model in data.characters)
            {
                var root = new GameObject(model.id + "_character");
                try
                {
                    var transforms = new Transform[model.nodes.Length];
                    var bindings = new Dictionary<string, Transform>();
                    for (int i = 0; i < model.nodes.Length; i++)
                    {
                        var node = model.nodes[i];
                        var go = new GameObject(node.name);
                        var t = go.transform;
                        t.SetParent(node.parent < 0 ? root.transform : transforms[node.parent], false);
                        t.localPosition = ReferenceSpace.Position(node.position[0], node.position[1], node.position[2]);
                        t.localRotation = ReferenceSpace.Rotation(node.rotation[0], node.rotation[1], node.rotation[2], node.rotation[3]);
                        t.localScale = new Vector3(node.scale[0], node.scale[1], node.scale[2]);
                        transforms[i] = t;
                        if (!string.IsNullOrEmpty(node.binding)) bindings.Add(node.binding, t);
                        if (node.mesh < 0) continue;
                        var geometry = model.meshes[node.mesh];
                        go.AddComponent<MeshFilter>().sharedMesh = Mesh(geometry);
                        go.AddComponent<MeshRenderer>().sharedMaterial = Material(geometry);
                    }
                    if (model.id == "astronaut")
                    {
                        var animator = root.AddComponent<AstronautAnimator>();
                        animator.body = bindings["bodyRef"]; animator.leftArm = bindings["lArm"]; animator.rightArm = bindings["rArm"];
                        animator.leftLeg = bindings["lLeg"]; animator.rightLeg = bindings["rLeg"];
                        // Mesh raycasts match the original cameraInteractive object test, including the limbs.
                        foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                            filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    }
                    else if (model.id == "bubble")
                    {
                        root.transform.localScale = Vector3.one * .75f;
                        var animator = root.AddComponent<BubbleAnimator>();
                        animator.fish = bindings["fish"]; animator.tail = bindings["tail"]; animator.mouth = bindings["mouth"];
                        animator.fin = bindings["fin"]; animator.bubbles = bindings["bubbles"]; animator.eyes = bindings["eyes"];
                    }
                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + model.id + ".prefab");
                    PrefabUtility.InstantiatePrefab(prefab, world);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }

        static Mesh Mesh(GeometryData data)
        {
            var mesh = new Mesh { name = data.name, indexFormat = IndexFormat.UInt32 };
            var vertices = new Vector3[data.vertices.Length / 3];
            var normals = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = ReferenceSpace.Position(data.vertices[i * 3], data.vertices[i * 3 + 1], data.vertices[i * 3 + 2]);
                if (data.normals.Length == data.vertices.Length) normals[i] = ReferenceSpace.Position(data.normals[i * 3], data.normals[i * 3 + 1], data.normals[i * 3 + 2]);
            }
            mesh.vertices = vertices;
            var indices = (int[])data.indices.Clone();
            for (int i = 0; i < indices.Length; i += 3) { int swap = indices[i]; indices[i] = indices[i + 2]; indices[i + 2] = swap; }
            mesh.triangles = indices;
            if (data.normals.Length == data.vertices.Length) mesh.normals = normals; else mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return Save(mesh, Root + "/" + data.name + ".asset");
        }

        static Material Material(GeometryData data)
        {
            var material = new Material(Shader.Find(data.unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            ColorUtility.TryParseHtmlString(data.color, out var color); color.a = data.opacity;
            material.SetColor("_BaseColor", color);
            if (!data.unlit)
            {
                material.SetFloat("_Metallic", data.metalness); material.SetFloat("_Smoothness", 1 - data.roughness);
                ColorUtility.TryParseHtmlString(data.emissive, out var emission); material.SetColor("_EmissionColor", emission * data.glow);
                if (data.glow > 0) material.EnableKeyword("_EMISSION");
            }
            if (data.transparent)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", data.depthWrite ? 1 : 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            }
            return Save(material, Root + "/" + data.name + ".mat");
        }

        static T Save<T>(T value, string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null) { AssetDatabase.CreateAsset(value, path); return value; }
            EditorUtility.CopySerialized(value, existing); UnityEngine.Object.DestroyImmediate(value); return existing;
        }
    }
}
