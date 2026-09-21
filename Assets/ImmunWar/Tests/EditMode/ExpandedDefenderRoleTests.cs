using NUnit.Framework;
using ImmunWar.Battle.State;
using ImmunWar.Combat.Abilities;

namespace ImmunWar.Tests.EditMode
{
    public sealed class ExpandedDefenderRoleTests
    {
        [Test]
        public void BCell_CleansesOneNegativeEffectAndSupportsTarget()
        {
            var node = new DefenseNodeState("node-1");
            node.Effects.Add(new StatusEffectState("status_infection", "enemy-1", 1, 90));
            Assert.That(new BCellAbility().Cleanse(node, "status_infection"), Is.True);
            Assert.That(node.Effects, Is.Empty);
        }

        [Test]
        public void NkCell_DealsBonusToMutatedEnemy()
        {
            var enemy = new EnemyState("enemy-1", "ene_mutant", "route", 100);
            var damage = new NkCellAbility().CalculateDamage(20, true);
            Assert.That(damage, Is.EqualTo(40));
        }

        [Test]
        public void Platelet_RepairsVitalityWithoutExceedingMaximum()
        {
            var vitality = new VitalityState(100); vitality.Damage(30);
            new PlateletAbility().Repair(vitality, 50);
            Assert.That(vitality.Current, Is.EqualTo(100));
        }
    }
}

