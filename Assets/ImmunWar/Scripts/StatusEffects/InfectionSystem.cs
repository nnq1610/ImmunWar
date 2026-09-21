using System;
using ImmunWar.Battle.State;
using ImmunWar.Core.Config;

namespace ImmunWar.StatusEffects
{
    public sealed class InfectionSystem
    {
        private readonly StatusEffectSystem _statuses;
        public event Action<string, int, int, string> InfectionChanged;
        public InfectionSystem(StatusEffectSystem statuses)
        {
            _statuses = statuses;
            _statuses.Changed += change => { if (change.ConfigId == "status_infection") InfectionChanged?.Invoke(null, change.PreviousStacks, change.CurrentStacks, null); };
        }
        public void Infect(DefenseNodeState node, string sourceId, int durationTicks, int maximumStacks = 3) => _statuses.Apply(node.Effects, "status_infection", sourceId, durationTicks, maximumStacks, StatusStackPolicy.Stack);
        public bool Cleanse(DefenseNodeState node) => _statuses.Cleanse(node.Effects, "status_infection");
        public float DefenderDamageMultiplier(DefenseNodeState node) => node.Effects.Exists(x => x.ConfigId == "status_infection") ? .75f : 1f;
    }
}

