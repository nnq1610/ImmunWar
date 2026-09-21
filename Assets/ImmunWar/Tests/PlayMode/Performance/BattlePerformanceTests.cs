using System.Collections;
using ImmunWar.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ImmunWar.Tests.PlayMode.Performance
{
    public sealed class BattlePerformanceTests
    {
        [UnityTest, Timeout(300000)]
        public IEnumerator ThirtyEnemiesFifteenDefenders_Records7200FramesAfterWarmup()
        {
            var battle = BattleController.CreateForTests("map_perf", 999, 100); battle.StartBattle();
            for (var i = 0; i < 300; i++) { battle.Advance(1d / 30d); yield return null; }
            using var probe = new PerformanceProbe(); probe.Start();
            for (var frame = 0; frame < 7200; frame++) { battle.Advance(1d / 30d); yield return null; probe.Sample(); }
            Assert.That(probe.FrameMilliseconds.Count, Is.EqualTo(7200)); var p50 = probe.Percentile(.5); var p95 = probe.Percentile(.95); var p99 = probe.Percentile(.99); Debug.Log($"IMMUNEWAR_PERF p50={p50:F3}ms p95={p95:F3}ms p99={p99:F3}ms gc_last={probe.LastAllocatedBytes}"); Assert.That(p95, Is.LessThan(33.34d)); Object.Destroy(battle.gameObject);
        }
    }
}
