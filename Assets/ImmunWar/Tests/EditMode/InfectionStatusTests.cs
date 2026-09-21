using System.Collections.Generic;
using NUnit.Framework;
using ImmunWar.Battle.State;
using ImmunWar.Core.Config;
using ImmunWar.StatusEffects;

namespace ImmunWar.Tests.EditMode
{
    public sealed class InfectionStatusTests
    {
        [Test]
        public void Infection_StacksToLimitRefreshesAndExpires()
        {
            var effects = new List<StatusEffectState>();
            var system = new StatusEffectSystem();
            system.Apply(effects, "status_infection", "enemy", 3, 2, StatusStackPolicy.Stack);
            system.Apply(effects, "status_infection", "enemy", 3, 2, StatusStackPolicy.Stack);
            system.Apply(effects, "status_infection", "enemy", 3, 2, StatusStackPolicy.Stack);
            Assert.That(effects[0].Stacks, Is.EqualTo(2));
            system.Tick(effects); system.Tick(effects); system.Tick(effects);
            Assert.That(effects, Is.Empty);
        }

        [Test]
        public void Cleanse_RemovesMatchingEffectAndEmitsChange()
        {
            var effects = new List<StatusEffectState> { new StatusEffectState("status_infection", "enemy", 1, 30) };
            var system = new StatusEffectSystem(); var changes = 0; system.Changed += _ => changes++;
            Assert.That(system.Cleanse(effects, "status_infection"), Is.True);
            Assert.That(changes, Is.EqualTo(1));
        }
    }
}

