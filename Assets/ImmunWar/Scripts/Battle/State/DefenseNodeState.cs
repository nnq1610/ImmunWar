using System.Collections.Generic;

namespace ImmunWar.Battle.State
{
    public sealed class DefenseNodeState
    {
        public string Id { get; }
        public string OccupantId { get; internal set; }
        public List<StatusEffectState> Effects { get; } = new List<StatusEffectState>();
        public bool IsOccupied => !string.IsNullOrEmpty(OccupantId);
        public DefenseNodeState(string id) => Id = id;
    }
}

