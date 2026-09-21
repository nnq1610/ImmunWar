using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImmunWar.Editor
{
    public static class InitialProjectSetup
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/ImmunWar/Scenes/Bootstrap.unity",
            "Assets/ImmunWar/Scenes/MainMenu.unity",
            "Assets/ImmunWar/Scenes/Battle.unity"
        };

        [MenuItem("Immune War/Setup/Create Base Scenes")]
        public static void CreateBaseScenes()
        {
            Directory.CreateDirectory("Assets/ImmunWar/Scenes");
            foreach (var path in ScenePaths)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateCamera();
                var sceneName = Path.GetFileNameWithoutExtension(path);
                CreateUiRoot(sceneName);
                var root = new GameObject(sceneName + "Root");
                root.transform.SetAsFirstSibling();
                EditorSceneManager.SaveScene(scene, path);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePaths[0], true),
                new EditorBuildSettingsScene(ScenePaths[1], true),
                new EditorBuildSettingsScene(ScenePaths[2], true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("IMMUNEWAR_SETUP_OK");
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void CreateUiRoot(string sceneName)
        {
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetSiblingIndex(canvas.transform.GetSiblingIndex() + 1);

            var label = new GameObject("SceneLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 100f);
            var text = label.GetComponent<Text>();
            text.text = "IMMUNE WAR — " + sceneName;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.68f, 0.94f, 1f);
        }
    }
}
