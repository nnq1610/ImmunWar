using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using ImmunWar.Battle;
using ImmunWar.Battle.State;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class PauseRestartTests
    {
        [UnityTest]
        public IEnumerator PauseResumeRestart_CreatesCleanAttempt()
        {
            var controller = BattleController.CreateForTests("map_lung", 100, 100);
            controller.StartBattle(); controller.Pause(); controller.Resume();
            var oldId = controller.State.BattleId;
            controller.State.Defenders.Add("old-defender", new DefenderState("old-defender", "def_tcell", "node-1", 10));
            controller.Pause(); controller.Restart();
            Assert.That(controller.State.BattleId, Is.Not.EqualTo(oldId));
            Assert.That(controller.State.Phase, Is.EqualTo(BattlePhase.Preparing));
            Assert.That(controller.State.Defenders, Is.Empty);
            yield return null;
        }
    }
}
