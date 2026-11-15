using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    /// <summary>Generated source images remain intact; Unity imports a small UI-sized copy.</summary>
    public sealed class HudIconImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/ARES/Resources/ARES/UI/HudIcons/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.sRGBTexture = true; importer.isReadable = false;
            importer.maxTextureSize = 256; importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
