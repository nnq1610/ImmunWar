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
        public IEnumerator NoDefenders_TwoWavesEndInDefeat()
        {
            yield return OpenBattleWithoutSavedSession();
            var view = Object.FindFirstObjectByType<PlayableBattleView>();
            Assert.That(view, Is.Not.Null);
            view.StartNextWave();
            view.AdvanceSimulationTicksForTests(1800);
            Assert.That(view.Controller.State.Phase, Is.EqualTo(BattlePhase.Running));
            view.StartNextWave();
            view.AdvanceSimulationTicksForTests(1800);
            Assert.That(view.Controller.State.Phase, Is.EqualTo(BattlePhase.Defeat));
            Assert.That(GameObject.Find("ResultPanel"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator FourTCells_CanDefendBothWaves()
        {
            yield return OpenBattleWithoutSavedSession();
            var view = Object.FindFirstObjectByType<PlayableBattleView>();
            var catalog = Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            var map = catalog.maps[0];
            var tCell = catalog.defenders[1];
            Assert.That(tCell.Id, Is.EqualTo("def_tcell"));
            view.SelectDefender(tCell);
            for (var i = 0; i < 4; i++) Assert.That(view.Place(map.nodes[i].Id, map.nodes[i].allowedRoleMask), Is.True);
            view.StartNextWave();
            view.AdvanceSimulationTicksForTests(1800);
            Assert.That(view.Controller.State.Phase, Is.EqualTo(BattlePhase.Running));
            view.StartNextWave();
            view.AdvanceSimulationTicksForTests(1800);
            Assert.That(view.Controller.State.Phase, Is.EqualTo(BattlePhase.Victory));
            Assert.That(GameObject.Find("ResultPanel"), Is.Not.Null);
        }
    }
}
