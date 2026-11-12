#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FurnaceRhythm.Config;
using FurnaceRhythm.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FurnaceRhythm.EditorTools
{
    /// <summary>Creates the sample scene and configures its imported art.</summary>
    public static class FurnaceRhythmDayOneSceneBuilder
    {
        private const string _sceneFolder = "Assets/_Project/Scenes";
        private const string _scenePath = _sceneFolder + "/FurnaceRhythmDay1.unity";
        private const string _legacyArtFolder = "Assets/FurnaceRhythm/Art";
        private const string _artFolder = "Assets/_Project/Art";
        private const string _spritePath = _artFolder + "/hot_billet_anvil_concept.png";
        private const string _assetRegisterPath = _artFolder + "/ASSETS.md";
        private const string _presetPath = _artFolder + "/hot_billet_sprite.preset";
        private const string _tuningPath = "Assets/_Project/Config/FurnaceRhythmTuning.asset";

        /// <summary>Builds the Day 1 scene and places it first in Build Settings.</summary>
        [MenuItem("Furnace Rhythm/Build Day 1 Scene")]
        public static void BuildDayOneScene()
        {
            if (!EnsureArtAssetPath())
                return;

            string absoluteSpritePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, _spritePath);
            if (!File.Exists(absoluteSpritePath))
            {
                Debug.LogError("Furnace Rhythm sample art is missing at " + _spritePath + ". Copy the Day 1 asset into the project and try again.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureSceneFolder();
            AssetDatabase.ImportAsset(_spritePath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImport();
            Sprite workpieceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(_spritePath);
            if (workpieceSprite == null)
            {
                Debug.LogError("Unity could not import the Furnace Rhythm sample art as a Sprite: " + _spritePath);
                return;
            }

            // Texture reimports can invalidate Unity object wrappers, so load and save tuning afterward.
            LoadOrCreateTuning();
            FurnaceRhythmTuning tuning = AssetDatabase.LoadAssetAtPath<FurnaceRhythmTuning>(_tuningPath);
            if (tuning == null)
            {
                Debug.LogError("Furnace Rhythm tuning could not be loaded after creation: " + _tuningPath);
                return;
            }

            tuning.SetHotBilletSprite(workpieceSprite);
            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject managerGroup = new GameObject("--- Managers ---");
            GameObject environmentGroup = new GameObject("Environment");
            new GameObject("Characters");
            GameObject uiGroup = new GameObject("UI");

            GameObject cameraObject = new GameObject("Furnace Rhythm Camera", typeof(Camera));
            cameraObject.transform.SetParent(environmentGroup.transform, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FurnaceRhythmPalette.MidnightIndustrial;

            GameObject gameObject = new GameObject("Furnace Rhythm Day 1");
            gameObject.transform.SetParent(managerGroup.transform, false);
            FurnaceRhythmGame game = gameObject.AddComponent<FurnaceRhythmGame>();
            FurnaceRhythmTuning sceneTuning = AssetDatabase.LoadAssetAtPath<FurnaceRhythmTuning>(_tuningPath);
            if (sceneTuning == null)
            {
                Debug.LogError("Furnace Rhythm tuning could not be loaded after scene creation: " + _tuningPath);
                return;
            }

            game.SetTuning(sceneTuning);
            game.SetPresentationParent(uiGroup.transform);
            EditorUtility.SetDirty(game);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, _scenePath))
            {
                Debug.LogError("Furnace Rhythm Day 1 scene could not be saved at " + _scenePath);
                return;
            }
            AddSceneToBuildSettings(_scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Furnace Rhythm Day 1 scene created at " + _scenePath + ". Press Play, then use Space or left click to forge one workpiece.");
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder(_sceneFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
        }

        private static bool EnsureArtAssetPath()
        {
            if (!AssetDatabase.IsValidFolder(_artFolder))
            {
                if (AssetDatabase.IsValidFolder(_legacyArtFolder))
                {
                    string moveError = AssetDatabase.MoveAsset(_legacyArtFolder, _artFolder);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        Debug.LogError("Could not move the Furnace Rhythm art into " + _artFolder + ": " + moveError);
                        return false;
                    }
                }
                else
                {
                    AssetDatabase.CreateFolder("Assets/_Project", "Art");
                }
            }

            string legacySpritePath = _legacyArtFolder + "/FurnaceRhythm_HotBillet_Concept.png";
            string legacyRegisterPath = _legacyArtFolder + "/ASSETS.md";
            if (AssetDatabase.IsValidFolder(_legacyArtFolder))
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(_spritePath) == null &&
                    AssetDatabase.LoadAssetAtPath<Object>(legacySpritePath) != null)
                {
                    string moveError = AssetDatabase.MoveAsset(legacySpritePath, _artFolder + "/FurnaceRhythm_HotBillet_Concept.png");
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        Debug.LogError("Could not move the Furnace Rhythm sprite asset: " + moveError);
                        return false;
                    }
                }

                if (!File.Exists(_assetRegisterPath) && File.Exists(legacyRegisterPath))
                {
                    string moveError = AssetDatabase.MoveAsset(legacyRegisterPath, _assetRegisterPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        Debug.LogError("Could not move the Furnace Rhythm asset register: " + moveError);
                        return false;
                    }
                }
            }

            string oldSpritePath = _artFolder + "/FurnaceRhythm_HotBillet_Concept.png";
            if (AssetDatabase.LoadAssetAtPath<Object>(oldSpritePath) != null &&
                AssetDatabase.LoadAssetAtPath<Object>(_spritePath) == null)
            {
                string renameError = AssetDatabase.RenameAsset(oldSpritePath, "hot_billet_anvil_concept");
                if (!string.IsNullOrEmpty(renameError))
                {
                    Debug.LogError("Could not rename the Furnace Rhythm art asset: " + renameError);
                    return false;
                }
            }

            if (File.Exists(_assetRegisterPath))
            {
                string register = File.ReadAllText(_assetRegisterPath);
                register = register.Replace("FurnaceRhythm_HotBillet_Concept.png", "hot_billet_anvil_concept.png");
                File.WriteAllText(_assetRegisterPath, register);
            }

            return true;
        }

        private static FurnaceRhythmTuning LoadOrCreateTuning()
        {
            FurnaceRhythmTuning tuning = AssetDatabase.LoadAssetAtPath<FurnaceRhythmTuning>(_tuningPath);
            if (tuning != null)
                return tuning;

            tuning = ScriptableObject.CreateInstance<FurnaceRhythmTuning>();
            AssetDatabase.CreateAsset(tuning, _tuningPath);
            AssetDatabase.SaveAssets();
            return tuning;
        }

        private static void ConfigureSpriteImport()
        {
            AssetImporter importer = AssetImporter.GetAtPath(_spritePath);
            TextureImporter textureImporter = importer as TextureImporter;
            if (textureImporter == null)
                return;

            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(_presetPath);
            if (preset == null)
            {
                ApplyPaintedSpriteDefaults(textureImporter);
                textureImporter.SaveAndReimport();
                textureImporter = AssetImporter.GetAtPath(_spritePath) as TextureImporter;
                preset = new Preset(textureImporter);
                AssetDatabase.CreateAsset(preset, _presetPath);
                AssetDatabase.SaveAssets();
            }

            if (!preset.CanBeAppliedTo(textureImporter))
            {
                Debug.LogError("The Furnace Rhythm sprite preset cannot be applied to the billet texture.");
                return;
            }

            preset.ApplyTo(textureImporter);
            textureImporter.SaveAndReimport();
        }

        private static void ApplyPaintedSpriteDefaults(TextureImporter textureImporter)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.spritePixelsPerUnit = 100f;
            textureImporter.textureCompression = TextureImporterCompression.Compressed;
            textureImporter.compressionQuality = 50;
            textureImporter.maxTextureSize = 1024;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int index = scenes.Count - 1; index >= 0; index--)
            {
                if (scenes[index].path == path)
                    scenes.RemoveAt(index);
            }

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
