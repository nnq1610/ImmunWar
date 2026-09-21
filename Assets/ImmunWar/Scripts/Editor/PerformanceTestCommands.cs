using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor
{
    public static class PerformanceTestCommands
    {
        [MenuItem("Immune War/Tests/Open Performance Scene")]
        public static void OpenPerformanceScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ImmunWar/Scenes/Battle.unity");
            EditorApplication.isPlaying = true;
            Debug.Log("IMMUNEWAR_PERFORMANCE_CAPTURE_READY");
        }
    }
}
