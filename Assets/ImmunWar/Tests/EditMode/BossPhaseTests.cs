using ImmunWar.Combat.Boss;
using ImmunWar.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace ImmunWar.Tests.EditMode
{
    public sealed class BossPhaseTests
    {
        [Test]
        public void ThresholdsTransitionInOrderWithInvulnerabilityAndDeath()
        {
            var one = Phase("boss_phase_1", 1f, 0); var two = Phase("boss_phase_2", .65f, 2); var three = Phase("boss_phase_3", .3f, 1);
            var boss = new BossPhaseController(100, new[] { three, one, two }); var transitions = 0; var died = false; boss.PhaseChanged += _ => transitions++; boss.Defeated += () => died = true;
            boss.ApplyDamage(40); Assert.That(boss.PhaseIndex, Is.EqualTo(1)); Assert.That(boss.ApplyDamage(30), Is.Zero); boss.Tick(); boss.Tick(); boss.ApplyDamage(35); Assert.That(boss.PhaseIndex, Is.EqualTo(2)); boss.Tick(); boss.ApplyDamage(100);
            Assert.That(transitions, Is.EqualTo(2)); Assert.That(died, Is.True); Assert.That(boss.IsDefeated, Is.True);
            Object.DestroyImmediate(one); Object.DestroyImmediate(two); Object.DestroyImmediate(three);
        }
        private static BossPhaseConfig Phase(string id, float threshold, int invulnerability) { var phase = ScriptableObject.CreateInstance<BossPhaseConfig>(); phase.SetIdForEditor(id); phase.healthThreshold = threshold; phase.transitionInvulnerabilityTicks = invulnerability; return phase; }
    }
}
