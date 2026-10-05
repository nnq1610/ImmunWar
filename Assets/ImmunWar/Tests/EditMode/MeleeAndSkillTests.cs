using System.Collections.Generic;
using NUnit.Framework;
using ImmunWar.Battle.Movement;
using ImmunWar.Battle.State;
using ImmunWar.Combat;
using ImmunWar.Combat.Abilities;
using ImmunWar.Core.Config;
using UnityEngine;

namespace ImmunWar.Tests.EditMode
{
    public sealed class MeleeEngagementTests
    {
        [Test]
        public void ChooseTarget_PicksEnemyClosestToHomeInsideGuardRadius()
        {
            var enemies = new List<EngageCandidate>
            {
                new EngageCandidate("far", new Vector2(3f, 0f), null),
                new EngageCandidate("near", new Vector2(1f, 0f), null),
                new EngageCandidate("mid", new Vector2(0f, 1.5f), null)
            };
            Assert.That(MeleeEngagement.ChooseTarget("mac", Vector2.zero, 2f, null, enemies), Is.EqualTo("near"));
        }

        [Test]
        public void ChooseTarget_PrefersEnemiesNotHeldByOthers()
        {
            var enemies = new List<EngageCandidate>
            {
                new EngageCandidate("taken", new Vector2(0.5f, 0f), "other"),
                new EngageCandidate("free", new Vector2(1.5f, 0f), null)
            };
            Assert.That(MeleeEngagement.ChooseTarget("mac", Vector2.zero, 2f, null, enemies), Is.EqualTo("free"));
        }

        [Test]
        public void ChooseTarget_KeepsChaseWithinLeashThenLetsGo()
        {
            var justOutside = new List<EngageCandidate> { new EngageCandidate("e", new Vector2(2.3f, 0f), null) };
            Assert.That(MeleeEngagement.ChooseTarget("mac", Vector2.zero, 2f, "e", justOutside), Is.EqualTo("e"));
            Assert.That(MeleeEngagement.ChooseTarget("mac", Vector2.zero, 2f, null, justOutside), Is.Null);
            var gone = new List<EngageCandidate> { new EngageCandidate("e", new Vector2(2f + MeleeEngagement.LeashSlack + 0.1f, 0f), null) };
            Assert.That(MeleeEngagement.ChooseTarget("mac", Vector2.zero, 2f, "e", gone), Is.Null);
        }

        [Test]
        public void Goal_StandsNextToTargetOrReturnsHome()
        {
            var home = new Vector2(1f, 1f);
            Assert.That(MeleeEngagement.Goal(home, Vector2.zero, null), Is.EqualTo(home));
            var goal = MeleeEngagement.Goal(home, Vector2.zero, new Vector2(2f, 0f));
            Assert.That(Vector2.Distance(goal, new Vector2(2f, 0f)), Is.EqualTo(MeleeEngagement.ContactDistance).Within(0.001f));
            Assert.That(goal.x, Is.LessThan(2f), "the defender stops on its own side of the enemy");
        }

        [Test]
        public void Step_MovesAtSpeedWithoutOvershooting()
        {
            Assert.That(MeleeEngagement.Step(Vector2.zero, new Vector2(10f, 0f), 3f, 0.5f).x, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(MeleeEngagement.Step(Vector2.zero, new Vector2(1f, 0f), 3f, 1f), Is.EqualTo(new Vector2(1f, 0f)));
        }

        [Test]
        public void Capacity_ZeroMeansUnlimited()
        {
            Assert.That(MeleeEngagement.HasCapacity(50, 0), Is.True);
            Assert.That(MeleeEngagement.HasCapacity(2, 3), Is.True);
            Assert.That(MeleeEngagement.HasCapacity(3, 3), Is.False);
        }
    }

    public sealed class ActiveSkillTests
    {
        [Test]
        public void EveryRole_HasAPricedSkillWithCooldown()
        {
            foreach (DefenderRole role in System.Enum.GetValues(typeof(DefenderRole)))
            {
                var spec = ActiveSkills.For(role);
                Assert.That(spec.Kind, Is.Not.EqualTo(ActiveSkillKind.None), role.ToString());
                Assert.That(spec.CooldownTicks, Is.GreaterThan(0), role.ToString());
                Assert.That(spec.AtpCost, Is.GreaterThanOrEqualTo(0), role.ToString());
            }
        }

        [Test]
        public void Cooldown_BlocksRecastUntilItRunsOut()
        {
            var cooldowns = new SkillCooldowns();
            var spec = ActiveSkills.For(DefenderRole.Support);
            Assert.That(cooldowns.Check("b", spec, 0, 100, false), Is.EqualTo(SkillCheck.Ready));
            Assert.That(cooldowns.Check("b", spec, 0, spec.AtpCost - 1, false), Is.EqualTo(SkillCheck.NotEnoughAtp));
            Assert.That(cooldowns.Check("b", spec, 0, 100, true), Is.EqualTo(SkillCheck.Disabled));
            cooldowns.Start("b", spec, 10);
            Assert.That(cooldowns.Check("b", spec, 11, 100, false), Is.EqualTo(SkillCheck.CoolingDown));
            Assert.That(cooldowns.Progress("b", 10), Is.EqualTo(0f));
            Assert.That(cooldowns.Check("b", spec, 10 + spec.CooldownTicks, 100, false), Is.EqualTo(SkillCheck.Ready));
            Assert.That(cooldowns.Progress("b", 10 + spec.CooldownTicks), Is.EqualTo(1f));
        }

        [Test]
        public void Refund_CutsRemainingCooldownByFractionOfItsLength()
        {
            var cooldowns = new SkillCooldowns();
            var spec = ActiveSkills.For(DefenderRole.Burst);
            cooldowns.Start("nk", spec, 0);
            cooldowns.Refund("nk", 0.5f, 0);
            Assert.That(cooldowns.RemainingTicks("nk", 0), Is.EqualTo(spec.CooldownTicks / 2));
            cooldowns.Refund("nk", 1f, 0);
            Assert.That(cooldowns.RemainingTicks("nk", 0), Is.EqualTo(0));
        }
    }

    public sealed class EnemyTraitTests
    {
        [Test]
        public void Absorb_SoaksDamageIntoShieldFirst()
        {
            var shield = 10f;
            Assert.That(EnemyTraits.Absorb(ref shield, 4f), Is.EqualTo(0f));
            Assert.That(shield, Is.EqualTo(6f));
            Assert.That(EnemyTraits.Absorb(ref shield, 10f), Is.EqualTo(4f));
            Assert.That(shield, Is.EqualTo(0f));
        }

        [Test]
        public void Revive_RestoresPartOfHealthUnlessExecuted()
        {
            var enemy = new EnemyState("e1", "ene_mutant_regen", "r", 50f);
            DamageResolver.Apply(enemy, 60f);
            Assert.That(EnemyTraits.TryRevive(enemy, 50f, true), Is.False);
            Assert.That(enemy.IsTerminal, Is.True);

            var other = new EnemyState("e2", "ene_mutant_regen", "r", 50f);
            DamageResolver.Apply(other, 60f);
            Assert.That(EnemyTraits.TryRevive(other, 50f, false), Is.True);
            Assert.That(other.IsTerminal, Is.False);
            Assert.That(other.Health, Is.EqualTo(50f * EnemyTraits.ReviveHealthFraction));
        }
    }
}
