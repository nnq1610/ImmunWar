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
            // Optional "--immunwar-visual-smoke-lineup": one of each defender (fixed nodes, then free spots on routes)
            // so every cell skill shows up in the captures.
            if (Array.IndexOf(args, "--immunwar-visual-smoke-lineup") >= 0)
            {
                var defenders = session.Catalogs.Catalog.defenders;
                battle.Controller.State.Economy.Add(2000);
                for (var i = 1; i < map.nodes.Length; i++)
                {
                    battle.SelectDefender(defenders[i % defenders.Length]);
                    battle.Place(map.nodes[i].Id, map.nodes[i].allowedRoleMask);
                }
                for (var i = map.nodes.Length; i < defenders.Length + 2; i++)
                {
                    var route = map.routes[i % map.routes.Length].waypoints;
                    battle.SelectDefender(defenders[i % defenders.Length]);
                    battle.PlaceFree(Vector2.Lerp(route[1], route[2], 0.5f));
                }
            }
            // Optional "--immunwar-visual-smoke-wave K": start at wave K (1-based) to see later enemy types.
            var waveOption = Array.IndexOf(args, "--immunwar-visual-smoke-wave");
            if (waveOption >= 0 && waveOption + 1 < args.Length && int.TryParse(args[waveOption + 1], out var startWave))
                battle.SkipToWaveForSmoke(startWave);
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
            // Optional: "--immunwar-visual-smoke-shots N" captures N more frames, 4 s apart, so later enemy types are visible.
            var shotsOption = Array.IndexOf(args, "--immunwar-visual-smoke-shots");
            var extraShots = shotsOption >= 0 && shotsOption + 1 < args.Length && int.TryParse(args[shotsOption + 1], out var parsed) ? parsed : 0;
            for (var shot = 1; shot <= extraShots; shot++)
            {
                yield return new WaitForSecondsRealtime(4f);
                yield return new WaitForEndOfFrame();
                var shotPath = Path.Combine(output, "battle_" + shot + ".png");
                ScreenCapture.CaptureScreenshot(shotPath);
                Debug.Log("IMMUNEWAR_VISUAL_SHOT " + shotPath + " enemies=" + battle.VisibleEnemyCount);
            }
            yield return new WaitForSecondsRealtime(0.5f);
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
