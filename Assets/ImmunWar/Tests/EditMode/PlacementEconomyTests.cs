using NUnit.Framework;
using ImmunWar.Battle.Placement;
using ImmunWar.Battle.State;
using ImmunWar.Core.Config;
using ImmunWar.Economy;

namespace ImmunWar.Tests.EditMode
{
    public sealed class PlacementEconomyTests
    {
        [Test]
        public void Placement_SpendsOnceAndRejectsOccupiedOrWrongRole()
        {
            var battle = new BattleState("battle-1", "map_lung", 1, 50, 100);
            battle.Nodes.Add("node-1", new DefenseNodeState("node-1"));
            var system = new PlacementSystem(battle);
            var first = system.TryPlace("cmd-1", "def_macrophage", DefenderRole.Blocker, "node-1", DefenderRoleMask.Blocker, 25, 100);
            Assert.That(first.Accepted, Is.True);
            Assert.That(battle.Economy.Atp, Is.EqualTo(25));
            Assert.That(system.TryPlace("cmd-2", "def_tcell", DefenderRole.Damage, "node-1", DefenderRoleMask.All, 20, 60).Accepted, Is.False);
            Assert.That(battle.Economy.Atp, Is.EqualTo(25));
        }

        [Test]
        public void Economy_RejectsUnaffordableAndGeneratesIncomeOnInterval()
        {
            var state = new EconomyState(10);
            var economy = new AtpEconomySystem(state);
            Assert.That(economy.TrySpend(11, "too-expensive"), Is.False);
            economy.RegisterGenerator("energy-1", 3, 30);
            for (var tick = 1; tick <= 60; tick++) economy.Tick(tick);
            Assert.That(state.Atp, Is.EqualTo(16));
        }
    }
}

