using System;
using System.IO;
using System.Collections.Generic;
using Ares.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.Editor
{
    public static class AresImporter
    {
        [Serializable] sealed class WorldFile { public MeshData[] meshes; }
        [Serializable] sealed class MeshData { public string name, room, layer, color, emissive; public float[] vertices, normals; public int[] indices; public float glow, opacity; public bool transparent; }
        const string Root = "Assets/ARES";
        [MenuItem("ARES/Import reference geometry and create scene")]
        public static void Import()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save your scene changes before regenerating BaseARES. No scene was replaced.");
            Directory.CreateDirectory(Root + "/Generated/Meshes"); Directory.CreateDirectory(Root + "/Generated/Materials"); Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            var definition = AssetDatabase.LoadAssetAtPath<GameDefinition>(Root + "/Generated/GameDefinition.asset");
            if (definition == null) { definition = ScriptableObject.CreateInstance<GameDefinition>(); AssetDatabase.CreateAsset(definition, Root + "/Generated/GameDefinition.asset"); }
            definition.layout = JsonUtility.FromJson<LayoutData>(File.ReadAllText(Root + "/Import/layout.json"));
            definition.campaign = JsonUtility.FromJson<CampaignData>(File.ReadAllText(Root + "/Import/campaign.json"));
            definition.world = JsonUtility.FromJson<Ares.World.WorldConfiguration>(File.ReadAllText(Root + "/Import/balance.json"));
            definition.portuguese = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Import/pt-BR.json"); definition.english = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Import/en.json"); EditorUtility.SetDirty(definition);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("Base ARES — imported reference");
            var groups = new Dictionary<string, Transform>(); var materials = new Dictionary<string, Material>();
            var source = JsonUtility.FromJson<WorldFile>(File.ReadAllText(Root + "/Import/world.json"));
            AssetDatabase.StartAssetEditing();
            try
            {
            foreach (var data in source.meshes)
            {
                string key = data.room + "_" + data.layer;
                if (!groups.TryGetValue(key, out var parent)) { parent = new GameObject(key).transform; parent.SetParent(world.transform); groups.Add(key, parent); }
                var mesh = new Mesh { name = data.name, indexFormat = IndexFormat.UInt32 };
                var vertices = new Vector3[data.vertices.Length / 3];
                for (int i = 0; i < vertices.Length; i++) vertices[i] = new Vector3(data.vertices[i * 3], data.vertices[i * 3 + 1], -data.vertices[i * 3 + 2]);
                mesh.vertices = vertices;
                // Three.js is right-handed. Mirror Z and reverse winding once.
                for (int i = 0; i < data.indices.Length; i += 3) { int temp = data.indices[i]; data.indices[i] = data.indices[i + 2]; data.indices[i + 2] = temp; }
                mesh.triangles = data.indices;
                if (data.normals.Length == data.vertices.Length) { var normals = new Vector3[vertices.Length]; for (int i = 0; i < normals.Length; i++) normals[i] = new Vector3(data.normals[i * 3], data.normals[i * 3 + 1], -data.normals[i * 3 + 2]); mesh.normals = normals; } else mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                string meshPath = Root + "/Generated/Meshes/" + data.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing != null) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; } else AssetDatabase.CreateAsset(mesh, meshPath);
                bool cutaway = data.layer == "walls" || data.layer == "roof";
                string mk = data.color + "_" + data.emissive + "_" + data.glow + "_" + data.opacity + "_" + cutaway;
                if (!materials.TryGetValue(mk, out var material))
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    ColorUtility.TryParseHtmlString(data.color, out var color); color.a = data.opacity; material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .2f);
                    ColorUtility.TryParseHtmlString(data.emissive, out var emission); material.SetColor("_EmissionColor", emission * data.glow); if (data.glow > 0) material.EnableKeyword("_EMISSION");
                    if (cutaway || data.transparent && data.opacity < 1) { material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000; }
                    string materialPath = Root + "/Generated/Materials/material_" + materials.Count + ".mat";
                    var prior = AssetDatabase.LoadAssetAtPath<Material>(materialPath); if (prior != null) { EditorUtility.CopySerialized(material, prior); UnityEngine.Object.DestroyImmediate(material); material = prior; } else AssetDatabase.CreateAsset(material, materialPath);
                    materials.Add(mk, material);
                }
                var go = new GameObject(data.name); go.transform.SetParent(parent); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            CharacterImporter.ImportInto(world.transform);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.1f; sun.color = new Color(1, .89f, .72f); sun.transform.rotation = Quaternion.Euler(48, -35, 0); sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f, .7f, 1) * .6f;
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.backgroundColor = new Color(.91f, .73f, .55f); camera.clearFlags = CameraClearFlags.SolidColor; camera.fieldOfView = 50; camera.nearClipPlane = .1f; camera.farClipPlane = 500; camera.transform.position = new Vector3(0, 8, -12); camera.transform.LookAt(new Vector3(0, 1.2f, -4)); camera.gameObject.AddComponent<AudioListener>();
            var preview = world.AddComponent<Ares.World.ReferenceExplorer>(); preview.definition = definition;
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, Root + "/Scenes/BaseARES.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "/Scenes/BaseARES.unity", true) };
            Debug.Log("ARES import complete: " + source.meshes.Length + " exact reference meshes.");
        }
    }
}

