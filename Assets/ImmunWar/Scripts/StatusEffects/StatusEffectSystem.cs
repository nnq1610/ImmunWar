using System;
using System.Collections.Generic;
using ImmunWar.Battle.State;
using ImmunWar.Core.Config;

namespace ImmunWar.StatusEffects
{
    public readonly struct StatusChange
    {
        public string ConfigId { get; } public int PreviousStacks { get; } public int CurrentStacks { get; }
        public StatusChange(string id, int previous, int current) { ConfigId = id; PreviousStacks = previous; CurrentStacks = current; }
    }

    public sealed class StatusEffectSystem
    {
        public event Action<StatusChange> Changed;
        public StatusEffectState Apply(IList<StatusEffectState> effects, string configId, string sourceId, int durationTicks, int maximumStacks, StatusStackPolicy policy)
        {
            StatusEffectState existing = null;
            for (var i = 0; i < effects.Count; i++) if (effects[i].ConfigId == configId) { existing = effects[i]; break; }
            if (existing == null)
            {
                existing = new StatusEffectState(configId, sourceId, 1, Math.Max(1, durationTicks)); effects.Add(existing); Changed?.Invoke(new StatusChange(configId, 0, 1)); return existing;
            }
            var previous = existing.Stacks;
            if (policy == StatusStackPolicy.Replace) existing.Stacks = 1;
            else if (policy == StatusStackPolicy.Stack) existing.Stacks = Math.Min(Math.Max(1, maximumStacks), existing.Stacks + 1);
            existing.RemainingTicks = Math.Max(1, durationTicks);
            Changed?.Invoke(new StatusChange(configId, previous, existing.Stacks)); return existing;
        }

        public void Tick(IList<StatusEffectState> effects)
        {
            for (var i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i]; effect.RemainingTicks--;
                if (effect.RemainingTicks > 0) continue;
                effects.RemoveAt(i); Changed?.Invoke(new StatusChange(effect.ConfigId, effect.Stacks, 0));
            }
        }

        public bool Cleanse(IList<StatusEffectState> effects, string configId)
        {
            for (var i = 0; i < effects.Count; i++) if (effects[i].ConfigId == configId) { var stacks = effects[i].Stacks; effects.RemoveAt(i); Changed?.Invoke(new StatusChange(configId, stacks, 0)); return true; }
            return false;
        }
    }
}

