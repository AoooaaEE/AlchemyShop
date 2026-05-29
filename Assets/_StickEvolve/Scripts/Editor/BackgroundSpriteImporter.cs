#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StickEvolve.EditorTools
{
    public class BackgroundSpriteImporter : AssetPostprocessor
    {
        private const string BackgroundSpritePath = "Assets/_StickEvolve/Art/lucid-origin_2D_game_background_hand-painted_painterly_style_enchanted_forest_battle_stage_si-0 (1).jpg";

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