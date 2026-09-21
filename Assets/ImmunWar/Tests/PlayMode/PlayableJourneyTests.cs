using System.Collections;
using ImmunWar.Battle.State;
using ImmunWar.Core;
using ImmunWar.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class PlayableJourneyTests
    {
        [UnityTest]
        public IEnumerator BootstrapMenuPlayPlacementAndWaveAreVisible()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            for (var attempt = 0; attempt < 200 && SceneManager.GetActiveScene().name != "MainMenu"; attempt++)
                yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            yield return null;
            var session = Object.FindFirstObjectByType<GameSession>();
            Assert.That(session, Is.Not.Null);
            var menu = Object.FindFirstObjectByType<PlayableMenuView>();
            Assert.That(menu, Is.Not.Null);
            var playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
            Assert.That(playButton, Is.Not.Null);
            Assert.That(playButton.interactable, Is.True);
            Assert.That(GameObject.Find("LungBackdrop")?.GetComponent<Image>()?.sprite, Is.Not.Null);
            Assert.That(playButton.GetComponent<Image>().sprite, Is.Not.Null);
            var art = Resources.Load<PlayableArtCatalog>("ImmuneWar/PlayableArtCatalog");
            Assert.That(art, Is.Not.Null);
            Assert.That(art.brainMap, Is.Not.Null);
            Assert.That(art.stomachMap, Is.Not.Null);
            foreach (var defenderConfig in session.Catalogs.Catalog.defenders)
                Assert.That(art.DefenderController(defenderConfig.Id), Is.Not.Null, defenderConfig.Id + " has no animation");
            foreach (var enemyConfig in session.Catalogs.Catalog.enemies)
                Assert.That(art.EnemyController(enemyConfig.Id), Is.Not.Null, enemyConfig.Id + " has no animation");
            playButton.onClick.Invoke();

            for (var attempt = 0; attempt < 200 && SceneManager.GetActiveScene().name != "Battle"; attempt++)
                yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
            yield return null;
            var battle = Object.FindFirstObjectByType<PlayableBattleView>();
            Assert.That(battle, Is.Not.Null);
            Assert.That(GameObject.Find("Arena"), Is.Not.Null);
            Assert.That(GameObject.Find("LungMap")?.GetComponent<Image>()?.sprite, Is.Not.Null);
            Assert.That(GameObject.Find("StartWave"), Is.Not.Null);
            foreach (var buttonName in new[] { "StartWave", "Pause", "Restart", "Menu" })
                Assert.That(GameObject.Find(buttonName)?.GetComponent<Image>()?.sprite, Is.Not.Null, buttonName + " has no themed sprite");
            var defender = session.Catalogs.Catalog.defenders[0];
            battle.SelectDefender(defender);
            var node = session.Catalogs.Get<ImmunWar.Core.Config.OrganMapConfig>("map_lung").nodes[0];
            Assert.That(battle.Place(node.Id, node.allowedRoleMask), Is.True);
            Assert.That(battle.Controller.State.Defenders.Count, Is.EqualTo(1));
            Assert.That(GameObject.Find("Placed_defender-0001"), Is.Not.Null);
            Assert.That(GameObject.Find("Placed_defender-0001").GetComponent<Animator>()?.runtimeAnimatorController, Is.Not.Null);
            Assert.That(GameObject.Find("Placed_defender-0001").GetComponentInChildren<AnimatedHealthBar>(), Is.Not.Null);
            var defenderImage = GameObject.Find("Placed_defender-0001").GetComponent<Image>();
            var defenderFrame = defenderImage.sprite;
            for (var attempt = 0; attempt < 12 && defenderImage.sprite == defenderFrame; attempt++)
                yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(defenderImage.sprite, Is.Not.EqualTo(defenderFrame), "Macrophage animation did not advance its sprite.");
            GameObject.Find("StartWave").GetComponent<Button>().onClick.Invoke();
            Assert.That(battle.Controller.State.Phase, Is.EqualTo(BattlePhase.Running));
            for (var attempt = 0; attempt < 60 && battle.VisibleEnemyCount == 0; attempt++) yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(battle.VisibleEnemyCount, Is.GreaterThan(0));
            Assert.That(GameObject.Find("Enemy_enemy-1"), Is.Not.Null);
            Assert.That(GameObject.Find("Enemy_enemy-1").GetComponent<Animator>()?.runtimeAnimatorController, Is.Not.Null);
            Assert.That(GameObject.Find("Enemy_enemy-1").GetComponentInChildren<AnimatedHealthBar>(), Is.Not.Null);
            var enemyImage = GameObject.Find("Enemy_enemy-1").GetComponent<Image>();
            var enemyFrame = enemyImage.sprite;
            for (var attempt = 0; attempt < 12 && enemyImage && enemyImage.sprite == enemyFrame; attempt++)
                yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(enemyImage && enemyImage.sprite != enemyFrame, Is.True, "Virus animation did not advance its sprite.");
            var progress = battle.FirstEnemyProgress;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(battle.FirstEnemyProgress, Is.GreaterThan(progress));
            GameObject.Find("Pause").GetComponent<Button>().onClick.Invoke();
            Assert.That(battle.Controller.State.Phase, Is.EqualTo(BattlePhase.Paused));
            GameObject.Find("Pause").GetComponent<Button>().onClick.Invoke();
            Assert.That(battle.Controller.State.Phase, Is.EqualTo(BattlePhase.Running));
        }

        [UnityTest]
        public IEnumerator HealthBarAnimatesDamageAndDelayedTrail()
        {
            var parent = new GameObject("HealthBarTestRoot", typeof(RectTransform));
            var bar = AnimatedHealthBar.Create(parent.transform, "TestHealth", Vector2.zero,
                new Vector2(200f, 20f), 100f, 100f);
            bar.SetValue(25f, 100f);
            Assert.That(bar.TargetFraction, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(bar.DisplayedFraction, Is.EqualTo(1f).Within(0.001f));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(bar.DisplayedFraction, Is.LessThan(1f));
            Assert.That(bar.DisplayedFraction, Is.GreaterThan(0.25f));
            var fill = bar.transform.Find("HealthFill")?.GetComponent<RectTransform>();
            var trail = bar.transform.Find("DamageTrail")?.GetComponent<RectTransform>();
            Assert.That(fill, Is.Not.Null);
            Assert.That(trail, Is.Not.Null);
            Assert.That(trail.sizeDelta.x, Is.GreaterThan(fill.sizeDelta.x));
            Object.Destroy(parent);
        }
    }
}
