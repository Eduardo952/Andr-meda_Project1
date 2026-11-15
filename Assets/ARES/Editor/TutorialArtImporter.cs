using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    /// <summary>Preserves generated alpha and prevents UI outlines from picking up texture fringes.</summary>
    public sealed class TutorialArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/ARES/Resources/ARES/UI/Tutorial/") && !assetPath.StartsWith("Assets/ARES/Resources/ARES/UI/Presentation/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.sRGBTexture = true; importer.isReadable = false;
            importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
