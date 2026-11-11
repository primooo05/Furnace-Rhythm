#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FurnaceRhythm;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FurnaceRhythm.EditorTools
{
    public static class FurnaceRhythmDayOneSceneBuilder
    {
        private const string ScenePath = "Assets/FurnaceRhythm/Scenes/FurnaceRhythmDay1.unity";
        private const string SpritePath = "Assets/FurnaceRhythm/Art/FurnaceRhythm_HotBillet_Concept.png";

        [MenuItem("Furnace Rhythm/Build Day 1 Scene")]
        public static void BuildDayOneScene()
        {
            string absoluteSpritePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, SpritePath);
            if (!File.Exists(absoluteSpritePath))
            {
                Debug.LogError("Furnace Rhythm sample art is missing at " + SpritePath + ". Copy the Day 1 asset into the project and try again.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImport();
            Sprite workpieceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (workpieceSprite == null)
            {
                Debug.LogError("Unity could not import the Furnace Rhythm sample art as a Sprite: " + SpritePath);
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Furnace Rhythm Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FurnaceRhythmPalette.MidnightIndustrial;

            GameObject gameObject = new GameObject("Furnace Rhythm Day 1");
            FurnaceRhythmGame game = gameObject.AddComponent<FurnaceRhythmGame>();
            game.SetHotBilletSprite(workpieceSprite);
            EditorUtility.SetDirty(game);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Furnace Rhythm Day 1 scene could not be saved at " + ScenePath);
                return;
            }
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Furnace Rhythm Day 1 scene created at " + ScenePath + ". Press Play, then use Space or left click to forge one workpiece.");
        }

        private static void ConfigureSpriteImport()
        {
            AssetImporter importer = AssetImporter.GetAtPath(SpritePath);
            TextureImporter textureImporter = importer as TextureImporter;
            if (textureImporter == null)
                return;

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.spritePixelsPerUnit = 100f;
            textureImporter.maxTextureSize = 2048;
            textureImporter.SaveAndReimport();
        }

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = scenes.Count - 1; i >= 0; i--)
            {
                if (scenes[i].path == path)
                    scenes.RemoveAt(i);
            }

            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
