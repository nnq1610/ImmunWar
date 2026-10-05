using System.Collections.Generic;
using NUnit.Framework;
using ImmunWar.Battle.State;
using ImmunWar.StatusEffects;

namespace ImmunWar.Tests.EditMode
{
    public sealed class CombatStatusTests
    {
        private readonly StatusEffectSystem _system = new StatusEffectSystem();

        [Test]
        public void Freeze_DisablesUnitUntilItExpires()
        {
            var effects = new List<StatusEffectState>();
            Assert.That(CombatStatus.ApplyControl(_system, effects, CombatStatus.Freeze, "bcell", false, 3), Is.EqualTo(ControlOutcome.Applied));
            Assert.That(CombatStatus.IsDisabled(effects), Is.True);
            Assert.That(CombatStatus.CanAttack(effects), Is.False);
            Assert.That(CombatStatus.MoveMultiplier(effects), Is.EqualTo(0f));
            for (var i = 0; i < 3; i++) _system.Tick(effects);
            Assert.That(CombatStatus.IsDisabled(effects), Is.False);
            Assert.That(CombatStatus.MoveMultiplier(effects), Is.EqualTo(1f));
        }

        [Test]
        public void RepeatedFreezes_GetShorter()
        {
            var effects = new List<StatusEffectState>();
            CombatStatus.ApplyControl(_system, effects, CombatStatus.Freeze, "a");
            Assert.That(CombatStatus.Remaining(effects, StatusIds.Freeze), Is.EqualTo(60));
            CombatStatus.ApplyControl(_system, effects, CombatStatus.Freeze, "a");
            Assert.That(CombatStatus.Remaining(effects, StatusIds.Freeze), Is.EqualTo(39));
            CombatStatus.ApplyControl(_system, effects, CombatStatus.Freeze, "a");
            Assert.That(CombatStatus.Remaining(effects, StatusIds.Freeze), Is.EqualTo(18));
        }

        [Test]
        public void FrostPutsOutFire_AndFireThawsIce()
        {
            var effects = new List<StatusEffectState>();
            CombatStatus.ApplyBurn(_system, effects, "tcell");
            CombatStatus.ApplyControl(_system, effects, CombatStatus.Freeze, "bcell");
            Assert.That(CombatStatus.Has(effects, StatusIds.Burn), Is.False);
            Assert.That(CombatStatus.ApplyBurn(_system, effects, "tcell"), Is.True);
            Assert.That(CombatStatus.Has(effects, StatusIds.Freeze), Is.False);
            Assert.That(CombatStatus.Has(effects, StatusIds.Burn), Is.True);
        }

        [Test]
        public void Burn_StacksToThreeAndPulsesEveryHalfSecond()
        {
            var effects = new List<StatusEffectState>();
            for (var i = 0; i < 5; i++) CombatStatus.ApplyBurn(_system, effects, "tcell");
            Assert.That(CombatStatus.Stacks(effects, StatusIds.Burn), Is.EqualTo(3));
            var total = 0f;
            var pulses = 0;
            for (var tick = 0; tick < CombatStatus.Burn.DurationTicks; tick++)
            {
                _system.Tick(effects);
                var damage = CombatStatus.PulseDamage(effects);
                if (damage > 0f) pulses++;
                total += damage;
            }
            Assert.That(pulses, Is.EqualTo(5));
            Assert.That(total, Is.EqualTo(5 * 3 * CombatStatus.BurnDamagePerStack).Within(0.001f));
            Assert.That(effects, Is.Empty);
        }

        [Test]
        public void ClotShield_BlocksControlAndDamage()
        {
            var effects = new List<StatusEffectState>();
            CombatStatus.Apply(_system, effects, CombatStatus.Shield, "platelet");
            Assert.That(CombatStatus.ApplyControl(_system, effects, CombatStatus.Freeze, "boss"), Is.EqualTo(ControlOutcome.Shielded));
            Assert.That(CombatStatus.IsDisabled(effects), Is.False);
            Assert.That(CombatStatus.DamageTakenMultiplier(effects), Is.EqualTo(0f));
            Assert.That(CombatStatus.CanAttack(effects), Is.False);
        }

        [Test]
        public void AdaptiveTarget_ResistsTheSameControlRightAfter()
        {
            var effects = new List<StatusEffectState>();
            Assert.That(CombatStatus.ApplyControl(_system, effects, CombatStatus.Slow, "platelet", true), Is.EqualTo(ControlOutcome.Applied));
            Assert.That(CombatStatus.ApplyControl(_system, effects, CombatStatus.Slow, "platelet", true), Is.EqualTo(ControlOutcome.Adapted));
            Assert.That(CombatStatus.ApplyControl(_system, effects, CombatStatus.Freeze, "bcell", true), Is.EqualTo(ControlOutcome.Applied));
        }

        [Test]
        public void SlowMarkAndHaste_ScaleTheirStats()
        {
            var effects = new List<StatusEffectState>();
            CombatStatus.ApplyControl(_system, effects, CombatStatus.Slow, "platelet");
            CombatStatus.Apply(_system, effects, CombatStatus.Mark, "bcell");
            CombatStatus.Apply(_system, effects, CombatStatus.Haste, "tcell");
            Assert.That(CombatStatus.MoveMultiplier(effects), Is.EqualTo(CombatStatus.SlowMoveMultiplier));
            Assert.That(CombatStatus.DamageTakenMultiplier(effects), Is.EqualTo(CombatStatus.MarkDamageMultiplier));
            Assert.That(CombatStatus.CooldownMultiplier(effects), Is.EqualTo(CombatStatus.HasteCooldownMultiplier));
        }
    }
}
