using System;
using System.IO;
using Ares.Data;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    public static class WorldImportChecks
    {
        [Serializable] sealed class WorldFile { public MeshData[] meshes; }
        [Serializable] sealed class MeshData { public string name, map, bumpMap; public float[] vertices, normals, uv, colors; public int[] indices; }
        public static string Run()
        {
            const string root = "Assets/ARES";
            var file = JsonUtility.FromJson<WorldFile>(File.ReadAllText(root + "/Import/world.json"));
            int vertices = 0, textured = 0, colored = 0;
            foreach (var item in file.meshes)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "/Generated/Meshes/" + item.name + ".asset");
                Require(mesh != null, "Missing mesh " + item.name);
                var actual = mesh.vertices; var uv = mesh.uv; var colors = mesh.colors; var triangles = mesh.triangles;
                Require(actual.Length * 3 == item.vertices.Length, "Vertex count " + item.name);
                for (int i = 0; i < actual.Length; i++)
                {
                    var expected = new Vector3(item.vertices[i * 3], item.vertices[i * 3 + 1], -item.vertices[i * 3 + 2]);
                    Require(Vector3.Distance(expected, actual[i]) < .00001f, "Geometry mismatch " + item.name + "/" + i);
                    if (item.uv.Length > 0) Require(uv.Length == actual.Length && Vector2.Distance(uv[i], new Vector2(item.uv[i * 2], item.uv[i * 2 + 1])) < .00001f, "UV mismatch " + item.name);
                    if (item.colors.Length > 0) Require(colors.Length == actual.Length && Mathf.Abs(colors[i].r - item.colors[i * 3]) + Mathf.Abs(colors[i].g - item.colors[i * 3 + 1]) + Mathf.Abs(colors[i].b - item.colors[i * 3 + 2]) < .00001f, "Color mismatch " + item.name);
                }
                Require(triangles.Length == item.indices.Length, "Triangle count " + item.name);
                for (int i = 0; i < triangles.Length; i += 3) Require(triangles[i] == item.indices[i + 2] && triangles[i + 1] == item.indices[i + 1] && triangles[i + 2] == item.indices[i], "Winding mismatch " + item.name);
                vertices += actual.Length; if (item.colors.Length > 0) colored++;
                if (!string.IsNullOrEmpty(item.map)) { CheckTexture(root, item.map, true); textured++; }
                if (!string.IsNullOrEmpty(item.bumpMap)) CheckTexture(root, item.bumpMap, false);
            }
            var definition = AssetDatabase.LoadAssetAtPath<GameDefinition>(root + "/Generated/GameDefinition.asset");
            var campaign = JsonUtility.FromJson<CampaignData>(File.ReadAllText(root + "/Import/campaign.json"));
            Require(definition != null && definition.rules != null && definition.rules.marsWeatherModifier, "Stale game definition");
            Require(JsonUtility.ToJson(definition.campaign) == JsonUtility.ToJson(campaign), "Stale Sol assignments or readings");
            var shader = Shader.Find("ARES/Reference Terrain");
            Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Terrain shader failed to compile");
            return $"{file.meshes.Length} world meshes / {vertices} vertices, {textured} textured meshes, {colored} vertex-colored terrain";
        }
        static void CheckTexture(string root, string name, bool srgb)
        {
            var path = root + "/Import/" + name;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(texture != null && importer != null && importer.sRGBTexture == srgb && importer.wrapMode == TextureWrapMode.Repeat, "Texture import mismatch " + name);
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
