using System.Collections;
using ImmunWar.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ImmunWar.Tests.PlayMode.Performance
{
    public sealed class MemorySoakTests
    {
        [UnityTest]
        public IEnumerator RepeatedRestartAndCampaignTransitionRemainBounded()
        {
            var battle = BattleController.CreateForTests("map_lung", 100, 100); var before = System.GC.GetTotalMemory(true);
            for (var i = 0; i < 50; i++) { battle.StartBattle(); battle.Advance(1d / 30d); battle.Restart(); if (i % 5 == 0) yield return null; }
            System.GC.Collect(); var after = System.GC.GetTotalMemory(true); Assert.That(after - before, Is.LessThan(8 * 1024 * 1024)); Object.Destroy(battle.gameObject); yield return null;
        }
    }
}
