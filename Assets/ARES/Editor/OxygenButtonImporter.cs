using UnityEditor;

namespace Ares.Editor
{
    /// <summary>Keep the generated UI plate crisp and its alpha intact; only this asset is affected.</summary>
    public sealed class OxygenButtonImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/ARES/Resources/ARES/UI/Oxygen/button-cream-v1.png") return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
