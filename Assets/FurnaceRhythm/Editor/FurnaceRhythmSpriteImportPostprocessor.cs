#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace FurnaceRhythm.EditorTools
{
    /// <summary>Applies the project's sprite preset to each texture in the Furnace Rhythm art folder.</summary>
    public sealed class FurnaceRhythmSpriteImportPostprocessor : AssetPostprocessor
    {
        private const string _artFolder = "Assets/_Project/Art/";
        private const string _presetPath = _artFolder + "hot_billet_sprite.preset";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(_artFolder, StringComparison.OrdinalIgnoreCase))
                return;

            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(_presetPath);
            if (preset != null && preset.CanBeAppliedTo(assetImporter))
            {
                preset.ApplyTo(assetImporter);
                return;
            }

            TextureImporter textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null)
                return;

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = UnityEngine.FilterMode.Bilinear;
            textureImporter.spritePixelsPerUnit = 100f;
            textureImporter.textureCompression = TextureImporterCompression.Compressed;
            textureImporter.compressionQuality = 50;
            textureImporter.maxTextureSize = 1024;
        }
    }
}
#endif
