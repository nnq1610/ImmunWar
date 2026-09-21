using System;
using System.Collections;
using System.IO;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImmunWar.Core
{
    public sealed class PlayableVisualSmokeDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "--immunwar-visual-smoke");
            if (index < 0) return;
            var output = index + 1 < args.Length ? args[index + 1] : Application.persistentDataPath;
            var go = new GameObject("PlayableVisualSmokeDriver");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayableVisualSmokeDriver>().StartCoroutine(go.GetComponent<PlayableVisualSmokeDriver>().Run(output));
        }

        private IEnumerator Run(string output)
        {
            Directory.CreateDirectory(output);
            yield return WaitForScene("MainMenu");
            yield return new WaitForEndOfFrame();
            var menu = FindFirstObjectByType<PlayableMenuView>();
            var playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
            if (!menu || !playButton) { Fail("menu controls missing"); yield break; }
            var menuPath = Path.Combine(output, "menu.png");
            ScreenCapture.CaptureScreenshot(menuPath);
            yield return new WaitForSecondsRealtime(0.5f);
            Debug.Log("IMMUNEWAR_VISUAL_MENU_OK " + menuPath);
            var args = Environment.GetCommandLineArgs();
            var mapOption = Array.IndexOf(args, "--immunwar-visual-smoke-map");
            var overrideMap = mapOption >= 0 && mapOption + 1 < args.Length;
            if (overrideMap)
            {
                var mapId = args[mapOption + 1];
                var selectedSession = FindFirstObjectByType<GameSession>();
                if (!selectedSession || !selectedSession.Catalogs.Get<OrganMapConfig>(mapId))
                { Fail("requested smoke map is missing: " + mapId); yield break; }
                if (!selectedSession.Progress.unlockedMapIds.Contains(mapId))
                    selectedSession.Progress.unlockedMapIds.Add(mapId);
                selectedSession.SelectMap(mapId);
            }
            if (overrideMap)
                FindFirstObjectByType<MainMenuController>().StartSelectedBattle();
            else
                playButton.onClick.Invoke();

            yield return WaitForScene("Battle");
            yield return new WaitForEndOfFrame();
            var battle = FindFirstObjectByType<PlayableBattleView>();
            var session = FindFirstObjectByType<GameSession>();
            if (!battle || !session) { Fail("battle controls missing"); yield break; }
            var map = session.Catalogs.Get<OrganMapConfig>(session.SelectedMapId);
            battle.SelectDefender(session.Catalogs.Catalog.defenders[0]);
            if (!battle.Place(map.nodes[0].Id, map.nodes[0].allowedRoleMask)) { Fail("placement rejected"); yield break; }
            battle.StartNextWave();
            for (var attempt = 0; attempt < 80 && battle.VisibleEnemyCount == 0; attempt++) yield return new WaitForSecondsRealtime(0.1f);
            if (battle.VisibleEnemyCount == 0) { Fail("enemy did not spawn"); yield break; }
            var progress = battle.FirstEnemyProgress;
            yield return new WaitForSecondsRealtime(0.5f);
            if (battle.FirstEnemyProgress <= progress) { Fail("enemy did not move"); yield break; }
            yield return new WaitForEndOfFrame();
            var battlePath = Path.Combine(output, "battle.png");
            ScreenCapture.CaptureScreenshot(battlePath);
            yield return new WaitForSecondsRealtime(0.5f);
            Debug.Log("IMMUNEWAR_VISUAL_BATTLE_OK " + battlePath);
            Application.Quit(0);
        }

        private static IEnumerator WaitForScene(string name)
        {
            for (var attempt = 0; attempt < 200 && SceneManager.GetActiveScene().name != name; attempt++)
                yield return new WaitForSecondsRealtime(0.05f);
        }

        private static void Fail(string reason)
        {
            Debug.LogError("IMMUNEWAR_VISUAL_FAIL " + reason);
            Application.Quit(1);
        }
    }
}
