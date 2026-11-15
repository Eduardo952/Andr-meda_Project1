using System;
using System.IO;
using Ares.World;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    /// <summary>Updates an existing import without rebuilding the scene or discarding scene edits.</summary>
    [InitializeOnLoad]
    public static class CelestialPrefabRepair
    {
        const string Request = "Temp/ares-celestial-repair.request";
        static CelestialPrefabRepair() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(Request);
            try { Repair(); File.WriteAllText("Logs/ares-celestial-repair.txt", "Updated sky prefab: distant initial positions, sky layer, no shadows. Celestial shader compiled without errors."); }
            catch (Exception error) { File.WriteAllText("Logs/ares-celestial-repair.txt", error.ToString()); Debug.LogException(error); }
        }
        [MenuItem("ARES/Import/Repair celestial prefab")]
        public static void Repair()
        {
            const string path = CharacterImporter.Root + "/sky.prefab";
            var shader = Resources.Load<Shader>("ARES/Effects/Celestial");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Celestial shader is missing or has compilation errors.");
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ReferenceCelestials.PrepareImportedSky(root.transform);
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var material = renderer.sharedMaterial;
                    if (material == null) throw new InvalidOperationException("Missing celestial material: " + renderer.name);
                    material.shader = shader;
                    material.name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(material));
                    material.DisableKeyword("_EMISSION");
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssetIfDirty(material);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
