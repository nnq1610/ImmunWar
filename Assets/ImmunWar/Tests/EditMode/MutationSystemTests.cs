using NUnit.Framework;
using ImmunWar.Battle.State;
using ImmunWar.Combat;
using ImmunWar.Core;

namespace ImmunWar.Tests.EditMode
{
    public sealed class MutationSystemTests
    {
        [Test]
        public void SameSeed_SelectsSameEligibleMutationAndAppliesOnce()
        {
            var candidates = new[] { new MutationCandidate("mutation_fast", 1f, 1f, 1.5f), new MutationCandidate("mutation_tough", 2f, 2f, 1f) };
            var a = new MutationSystem(new SeededRandomSource(42)); var b = new MutationSystem(new SeededRandomSource(42));
            Assert.That(a.Select(candidates).Id, Is.EqualTo(b.Select(candidates).Id));
            var enemy = new EnemyState("enemy", "ene_virus", "route", 50);
            Assert.That(a.Apply(enemy, candidates[0]), Is.True);
            Assert.That(a.Apply(enemy, candidates[1]), Is.False);
        }
    }
}

