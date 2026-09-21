using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor.AssetProvenance
{
    public sealed class ImmuneWarAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/ImmunWar/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            // Batch sprite sheets are configured by BatchArtImporter after their first import.
            if (!assetPath.Contains("/Art/Batch/")) importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.spritePixelsPerUnit = assetPath.Contains("/UI/") ? 100f : 128f;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/ImmunWar/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = assetPath.Contains("/SFX/");
            importer.loadInBackground = assetPath.Contains("/Music/");
        }
    }
}
