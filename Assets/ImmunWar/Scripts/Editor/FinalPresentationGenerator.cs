using System.IO;
using ImmunWar.Audio;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImmunWar.Editor
{
    public static class FinalPresentationGenerator
    {
        public static void Generate()
        {
            CreatePresentation("Assets/ImmunWar/Data/Presentation/BattlePresentation.asset", "presentation_battle", new Color(.15f, .8f, 1f));
            CreatePresentation("Assets/ImmunWar/Data/Presentation/BossPresentation.asset", "presentation_boss", new Color(.8f, .15f, 1f));
            CreateSettings(); CreateHud(); CreateStatus(); Compose("Assets/ImmunWar/Scenes/Battle.unity", true); Compose("Assets/ImmunWar/Scenes/MainMenu.unity", false); AssetDatabase.SaveAssets(); Debug.Log("IMMUNEWAR_FINAL_PRESENTATION_OK");
        }
        private static void CreatePresentation(string path, string id, Color color) { Directory.CreateDirectory(Path.GetDirectoryName(path)); var a=AssetDatabase.LoadAssetAtPath<PresentationConfig>(path); if(!a){a=ScriptableObject.CreateInstance<PresentationConfig>();AssetDatabase.CreateAsset(a,path);} a.SetIdForEditor(id);a.accent=color;EditorUtility.SetDirty(a); }
        private static void CreateSettings(){var root=new GameObject("SettingsPanel",typeof(RectTransform),typeof(SettingsController));Add<Slider>(root,"Music");Add<Slider>(root,"SFX");Save(root,"Assets/ImmunWar/Prefabs/UI/SettingsPanel.prefab");}
        private static void CreateHud(){var root=new GameObject("BattleHud",typeof(RectTransform),typeof(SafeAreaController),typeof(CanvasGroup));for(var i=0;i<4;i++){var t=Add<Text>(root,new[]{"ATP","Vitality","Wave","Status"}[i]);t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=t.name;t.color=Color.white;}Save(root,"Assets/ImmunWar/Prefabs/UI/BattleHud.prefab");}
        private static void CreateStatus(){var root=new GameObject("StatusIndicator",typeof(RectTransform),typeof(BattleAccessibilityPresenter));var t=Add<Text>(root,"Label");t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text="STATUS +";Save(root,"Assets/ImmunWar/Prefabs/UI/StatusIndicator.prefab");}
        private static T Add<T>(GameObject parent,string name) where T:Component{var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent.transform,false);return go.AddComponent<T>();}
        private static void Save(GameObject root,string path){Directory.CreateDirectory(Path.GetDirectoryName(path));PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);}
        private static void Compose(string path,bool battle){var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var root=GameObject.Find("PresentationRuntime")??new GameObject("PresentationRuntime");if(!root.GetComponent<AudioService>())root.AddComponent<AudioService>();if(battle){if(!root.GetComponent<BattleAccessibilityPresenter>())root.AddComponent<BattleAccessibilityPresenter>();if(!root.GetComponent<TutorialPromptController>())root.AddComponent<TutorialPromptController>();}EditorSceneManager.SaveScene(scene,path);}
    }
}
