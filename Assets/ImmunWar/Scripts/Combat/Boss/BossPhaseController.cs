using System;
using System.Linq;
using ImmunWar.Core.Config;

namespace ImmunWar.Combat.Boss
{
    public readonly struct BossPhaseEvent
    {
        public int PhaseIndex { get; }
        public string PhaseId { get; }
        public BossPhaseEvent(int phaseIndex, string phaseId) { PhaseIndex = phaseIndex; PhaseId = phaseId; }
    }

    public sealed class BossPhaseController
    {
        private readonly BossPhaseConfig[] _phases;
        public float MaximumHealth { get; }
        public float Health { get; private set; }
        public int PhaseIndex { get; private set; }
        public int InvulnerabilityTicksRemaining { get; private set; }
        public bool IsDefeated => Health <= 0f;
        public AbilityConfig[] ActiveAbilities => _phases.Length == 0 ? Array.Empty<AbilityConfig>() : _phases[PhaseIndex].abilities ?? Array.Empty<AbilityConfig>();
        public event Action<BossPhaseEvent> PhaseChanged;
        public event Action Defeated;

        public BossPhaseController(float maximumHealth, BossPhaseConfig[] phases)
        {
            MaximumHealth = Math.Max(1f, maximumHealth); Health = MaximumHealth;
            _phases = (phases ?? Array.Empty<BossPhaseConfig>()).Where(p => p).OrderByDescending(p => p.healthThreshold).ToArray();
        }

        public float ApplyDamage(float amount)
        {
            if (IsDefeated || InvulnerabilityTicksRemaining > 0 || amount <= 0f) return 0f;
            var applied = Math.Min(amount, Health); Health -= applied;
            if (Health <= 0f) { Health = 0f; Defeated?.Invoke(); return applied; }
            while (PhaseIndex + 1 < _phases.Length && Health / MaximumHealth <= _phases[PhaseIndex + 1].healthThreshold)
            {
                PhaseIndex++; InvulnerabilityTicksRemaining = _phases[PhaseIndex].transitionInvulnerabilityTicks;
                PhaseChanged?.Invoke(new BossPhaseEvent(PhaseIndex, _phases[PhaseIndex].Id));
            }
            return applied;
        }

        public void Tick() { if (InvulnerabilityTicksRemaining > 0) InvulnerabilityTicksRemaining--; }
    }
}
