using ImmunWar.Battle.State;
using ImmunWar.Core;
using ImmunWar.Core.Config;

namespace ImmunWar.Battle.Placement
{
    public readonly struct PlacementResult
    {
        public bool Accepted { get; }
        public string ReasonCode { get; }
        public string DefenderInstanceId { get; }
        public PlacementResult(bool accepted, string reasonCode, string instanceId = null)
        { Accepted = accepted; ReasonCode = reasonCode; DefenderInstanceId = instanceId; }
    }

    public sealed class PlacementSystem
    {
        private readonly BattleState _battle;
        private readonly SequenceId _sequence = new SequenceId();
        public PlacementSystem(BattleState battle) => _battle = battle;

        public PlacementResult TryPlace(string commandId, string configId, DefenderRole role, string nodeId, DefenderRoleMask allowedRoles, int cost, float health)
        {
            if (_battle.Phase is BattlePhase.Victory or BattlePhase.Defeat or BattlePhase.Exited) return new PlacementResult(false, "battle_terminal");
            if (!_battle.Nodes.TryGetValue(nodeId, out var node)) return new PlacementResult(false, "node_unknown");
            if (node.IsOccupied) return new PlacementResult(false, "node_occupied");
            if ((allowedRoles & ConfigValidator.ToMask(role)) == 0) return new PlacementResult(false, "role_blocked");
            if (!_battle.Economy.TrySpend(cost)) return new PlacementResult(false, "insufficient_atp");
            var instanceId = "defender-" + _sequence.Next().ToString("D4");
            var defender = new DefenderState(instanceId, configId, nodeId, health);
            _battle.Defenders.Add(instanceId, defender);
            node.OccupantId = instanceId;
            return new PlacementResult(true, null, instanceId);
        }

        public bool Remove(string nodeId)
        {
            if (!_battle.Nodes.TryGetValue(nodeId, out var node) || !node.IsOccupied) return false;
            _battle.Defenders.Remove(node.OccupantId);
            node.OccupantId = null;
            return true;
        }
    }
}
