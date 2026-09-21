using System;
using System.Collections;
using ImmunWar.Battle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmunWar.Core
{
    public sealed class WindowsPlayerSmokeDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--immunwar-smoke") < 0) return;
            var go = new GameObject("WindowsPlayerSmokeDriver"); DontDestroyOnLoad(go); go.AddComponent<WindowsPlayerSmokeDriver>().StartCoroutine(go.GetComponent<WindowsPlayerSmokeDriver>().Run());
        }
        private IEnumerator Run()
        {
            Debug.Log("IMMUNEWAR_SMOKE_LAUNCH"); yield return SceneManager.LoadSceneAsync("MainMenu"); Debug.Log("IMMUNEWAR_SMOKE_MENU"); yield return SceneManager.LoadSceneAsync("Battle");
            var battle = FindFirstObjectByType<BattleController>() ?? BattleController.CreateForTests("map_lung", 100, 100); battle.StartBattle(); battle.Pause(); battle.Resume(); battle.Restart(); Debug.Log("IMMUNEWAR_SMOKE_BATTLE_PAUSE_RESTART_OK"); yield return null; Debug.Log("IMMUNEWAR_SMOKE_EXIT_OK"); Application.Quit(0);
        }
    }
}
