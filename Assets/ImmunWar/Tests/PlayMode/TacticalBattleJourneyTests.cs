using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using ImmunWar.Battle.Fever;
using ImmunWar.Battle.State;
using ImmunWar.Core.Config;
using ImmunWar.StatusEffects;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class TacticalBattleJourneyTests
    {
        [UnityTest]
        public IEnumerator InfectionCleanseAndFever_CompleteDeterministically()
        {
            var effects = new List<StatusEffectState>(); var statuses = new StatusEffectSystem();
            statuses.Apply(effects, "status_infection", "ene_bacteria", 2, 1, StatusStackPolicy.Refresh);
            Assert.That(statuses.Cleanse(effects, "status_infection"), Is.True);
            var feverState = new FeverState(); var fever = new FeverSystem(feverState, 10, 2, 1.5f, 1.2f);
            fever.AddCharge(10); Assert.That(fever.Activate(), Is.True); fever.Tick(); fever.Tick();
            Assert.That(feverState.Phase, Is.EqualTo(FeverPhase.Charging));
            yield return null;
        }
    }
}
