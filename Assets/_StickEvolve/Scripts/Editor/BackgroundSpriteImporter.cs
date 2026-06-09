#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StickEvolve.EditorTools
{
    public class BackgroundSpriteImporter : AssetPostprocessor
    {
        private const string BackgroundSpritePath = "Assets/_StickEvolve/Resources/Art/forest_bg.png";

        private void OnPreprocessTexture()
        {
            if (assetPath != BackgroundSpritePath) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
        }
    }
}
#endif