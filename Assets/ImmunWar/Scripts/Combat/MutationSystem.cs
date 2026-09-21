using System;
using System.Collections.Generic;
using ImmunWar.Battle.State;
using ImmunWar.Core;

namespace ImmunWar.Combat
{
    public readonly struct MutationCandidate
    {
        public string Id { get; } public float Weight { get; } public float HealthMultiplier { get; } public float SpeedMultiplier { get; }
        public MutationCandidate(string id, float weight, float healthMultiplier, float speedMultiplier) { Id = id; Weight = Math.Max(0f, weight); HealthMultiplier = Math.Max(0f, healthMultiplier); SpeedMultiplier = Math.Max(0f, speedMultiplier); }
    }

    public sealed class MutationSystem
    {
        private readonly IRandomSource _random;
        private readonly Dictionary<string, MutationCandidate> _applied = new Dictionary<string, MutationCandidate>(StringComparer.Ordinal);
        public event Action<EnemyState, MutationCandidate> Mutated;
        public MutationSystem(IRandomSource random) => _random = random;
        public MutationCandidate Select(IReadOnlyList<MutationCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0) return default;
            var total = 0f; for (var i = 0; i < candidates.Count; i++) total += candidates[i].Weight;
            if (total <= 0f) return default;
            var value = _random.Value() * total;
            for (var i = 0; i < candidates.Count; i++) { value -= candidates[i].Weight; if (value <= 0f) return candidates[i]; }
            return candidates[candidates.Count - 1];
        }
        public bool Apply(EnemyState enemy, MutationCandidate candidate)
        {
            if (enemy == null || string.IsNullOrEmpty(candidate.Id) || _applied.ContainsKey(enemy.InstanceId)) return false;
            enemy.Health *= candidate.HealthMultiplier; _applied.Add(enemy.InstanceId, candidate); Mutated?.Invoke(enemy, candidate); return true;
        }
        public bool IsMutated(string enemyId) => _applied.ContainsKey(enemyId);
    }
}

