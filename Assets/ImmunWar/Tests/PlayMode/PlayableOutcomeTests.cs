using System.Collections;
using ImmunWar.Battle.State;
using ImmunWar.Core;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class PlayableOutcomeTests
    {
        private static IEnumerator OpenBattleWithoutSavedSession()
        {
            var session = Object.FindFirstObjectByType<GameSession>();
            if (session) { Object.Destroy(session.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoDefenders_WaveEndsInDefeat()
        {
            yield return OpenBattleWithoutSavedSession();
            var view = Object.FindFirstObjectByType<PlayableBattleView>();
            Assert.That(view, Is.Not.Null);
            view.StartNextWave();
            view.AdvanceSimulationTicksForTests(2400);
            Assert.That(view.Controller.State.Phase, Is.EqualTo(BattlePhase.Defeat));
            Assert.That(GameObject.Find("ResultPanel"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator FourTCells_CanDefendAllLungWaves()
        {
            yield return OpenBattleWithoutSavedSession();
            var view = Object.FindFirstObjectByType<PlayableBattleView>();
            var catalog = Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            var map = catalog.maps[0];
            var macrophage = catalog.defenders[0];
            var tCell = catalog.defenders[1];
            Assert.That(tCell.Id, Is.EqualTo("def_tcell"));
            Assert.That(view.Controller.State.Economy.Atp, Is.EqualTo(150));
            view.SelectDefender(tCell);
            for (var i = 0; i < 4; i++) Assert.That(view.Place(map.nodes[i].Id, map.nodes[i].allowedRoleMask), Is.True);
            view.SelectDefender(macrophage);
            Assert.That(view.Place(map.nodes[4].Id, map.nodes[4].allowedRoleMask), Is.True);
            for (var w = 0; w < 3; w++)
            {
                view.StartNextWave();
                view.AdvanceSimulationTicksForTests(3000);
            }
            Assert.That(view.Controller.State.Phase, Is.EqualTo(BattlePhase.Victory));
            Assert.That(GameObject.Find("ResultPanel"), Is.Not.Null);
        }
    }
}
