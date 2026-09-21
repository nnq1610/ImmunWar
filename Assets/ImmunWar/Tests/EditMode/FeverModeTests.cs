using NUnit.Framework;
using ImmunWar.Battle.Fever;
using ImmunWar.Battle.State;

namespace ImmunWar.Tests.EditMode
{
    public sealed class FeverModeTests
    {
        [Test]
        public void Fever_ChargesActivatesExpiresAndReturnsToCharging()
        {
            var state = new FeverState(); var fever = new FeverSystem(state, 100, 3, 1.5f, 1.2f);
            fever.AddCharge(100); Assert.That(state.Phase, Is.EqualTo(FeverPhase.Ready));
            Assert.That(fever.Activate(), Is.True); Assert.That(fever.DamageMultiplier, Is.EqualTo(1.5f));
            fever.Tick(); fever.Tick(); fever.Tick();
            Assert.That(state.Phase, Is.EqualTo(FeverPhase.Charging)); Assert.That(state.Charge, Is.Zero);
        }
    }
}

