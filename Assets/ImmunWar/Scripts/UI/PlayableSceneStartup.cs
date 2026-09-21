using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmunWar.UI
{
    internal static class PlayableSceneStartup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
                new GameObject("PlayableMenuRuntime").AddComponent<PlayableMenuView>();
            else if (scene.name == "Battle")
                new GameObject("PlayableBattleRuntime").AddComponent<PlayableBattleView>();
        }
    }
}
