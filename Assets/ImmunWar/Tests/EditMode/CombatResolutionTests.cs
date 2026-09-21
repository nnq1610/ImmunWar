using System.Collections.Generic;
using NUnit.Framework;
using ImmunWar.Battle.State;
using ImmunWar.Combat;

namespace ImmunWar.Tests.EditMode
{
    public sealed class CombatResolutionTests
    {
        [Test]
        public void Targeting_PrefersGreatestProgressThenStableId()
        {
            var enemies = new List<EnemyState>
            {
                NewEnemy("enemy-b", .7f), NewEnemy("enemy-a", .7f), NewEnemy("enemy-c", .4f)
            };
            Assert.That(TargetingSystem.Select(enemies).InstanceId, Is.EqualTo("enemy-a"));
        }

        [Test]
        public void DamageResolver_CommitsDeathAndRewardOnce()
        {
            var enemy = NewEnemy("enemy-a", .2f);
            enemy.Health = 10f;
            var first = DamageResolver.Apply(enemy, 12f);
            var second = DamageResolver.Apply(enemy, 12f);
            Assert.That(first.Defeated, Is.True);
            Assert.That(second.Applied, Is.False);
            Assert.That(enemy.TerminalResult, Is.EqualTo(EnemyTerminalResult.Defeated));
        }

        private static EnemyState NewEnemy(string id, float progress)
        {
            var enemy = new EnemyState(id, "ene_virus", "route-1", 20f); enemy.RouteProgress = progress; return enemy;
        }
    }
}

