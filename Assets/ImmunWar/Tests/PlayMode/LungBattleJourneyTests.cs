using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using ImmunWar.Battle;
using ImmunWar.Battle.State;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class LungBattleJourneyTests
    {
        [UnityTest]
        public IEnumerator FreshBattle_CanReachVictory()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            var installer = UnityEngine.Object.FindFirstObjectByType<BattleSceneInstaller>();
            Assert.That(installer, Is.Not.Null);
            var controller = installer.Controller;
            Assert.That(controller.State.MapId, Is.EqualTo("map_lung"));
            installer.StartBattle();
            controller.DamageOrgan(100);
            Assert.That(controller.State.Phase, Is.EqualTo(BattlePhase.Defeat));
            controller.Restart();
            controller.StartBattle();
            controller.CompleteAllWavesForTests();
            Assert.That(controller.State.Phase, Is.EqualTo(BattlePhase.Victory));
            yield return null;
        }
    }
}
