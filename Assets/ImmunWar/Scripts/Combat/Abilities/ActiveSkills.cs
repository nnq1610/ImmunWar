using System;
using System.Collections.Generic;
using ImmunWar.Core.Config;

namespace ImmunWar.Combat.Abilities
{
    public enum ActiveSkillKind { None, Engulf, FeverRush, AntibodyFreeze, Execute, ClotShield, AtpSurge }

    public enum SkillCheck { Ready, CoolingDown, NotEnoughAtp, Disabled, NoSkill }

    public readonly struct ActiveSkillSpec
    {
        public ActiveSkillKind Kind { get; }
        public string Name { get; }
        public int AtpCost { get; }
        public int CooldownTicks { get; }
        public ActiveSkillSpec(ActiveSkillKind kind, string name, int atpCost, int cooldownTicks)
        { Kind = kind; Name = name; AtpCost = atpCost; CooldownTicks = cooldownTicks; }
    }

    /// <summary>The tap-to-cast skill each defender role carries (30 ticks = 1 s).</summary>
    public static class ActiveSkills
    {
        public const float EngulfHealthFraction = 0.35f;
        public const float FeverRushHealthLossPerSecond = 0.02f;
        public const float AntibodyFreezeRadius = 1.5f;
        public const float ExecuteDamageMultiplier = 4f;
        public const float ExecuteKillRefund = 0.5f;
        public const float ClotShieldRadius = 2.2f;
        public const float ClotShieldHealFraction = 0.15f;
        public const int AtpSurgeAmount = 30;

        public static ActiveSkillSpec For(DefenderRole role) => role switch
        {
            DefenderRole.Blocker => new ActiveSkillSpec(ActiveSkillKind.Engulf, "ENGULF", 20, 360),
            DefenderRole.Damage => new ActiveSkillSpec(ActiveSkillKind.FeverRush, "FEVER RUSH", 15, 420),
            DefenderRole.Support => new ActiveSkillSpec(ActiveSkillKind.AntibodyFreeze, "ANTIBODY FREEZE", 25, 480),
            DefenderRole.Burst => new ActiveSkillSpec(ActiveSkillKind.Execute, "EXECUTE", 20, 300),
            DefenderRole.Repair => new ActiveSkillSpec(ActiveSkillKind.ClotShield, "CLOT SHIELD", 20, 540),
            DefenderRole.Economy => new ActiveSkillSpec(ActiveSkillKind.AtpSurge, "ATP SURGE", 0, 600),
            _ => default
        };
    }

    /// <summary>Per-defender skill cooldowns, measured on the battle tick clock.</summary>
    public sealed class SkillCooldowns
    {
        private readonly Dictionary<string, int> _readyAt = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _length = new Dictionary<string, int>();

        public int RemainingTicks(string defenderId, int tick) =>
            _readyAt.TryGetValue(defenderId, out var ready) ? Math.Max(0, ready - tick) : 0;

        /// <summary>0 right after casting, 1 when ready again.</summary>
        public float Progress(string defenderId, int tick)
        {
            var remaining = RemainingTicks(defenderId, tick);
            if (remaining <= 0) return 1f;
            return _length.TryGetValue(defenderId, out var length) && length > 0 ? 1f - (float)remaining / length : 1f;
        }

        public SkillCheck Check(string defenderId, ActiveSkillSpec spec, int tick, int atp, bool disabled)
        {
            if (spec.Kind == ActiveSkillKind.None) return SkillCheck.NoSkill;
            if (disabled) return SkillCheck.Disabled;
            if (RemainingTicks(defenderId, tick) > 0) return SkillCheck.CoolingDown;
            return atp < spec.AtpCost ? SkillCheck.NotEnoughAtp : SkillCheck.Ready;
        }

        public void Start(string defenderId, ActiveSkillSpec spec, int tick)
        {
            _readyAt[defenderId] = tick + Math.Max(1, spec.CooldownTicks);
            _length[defenderId] = Math.Max(1, spec.CooldownTicks);
        }

        /// <summary>Cuts the remaining cooldown by a fraction of its full length (e.g. NK Execute on a kill).</summary>
        public void Refund(string defenderId, float fraction, int tick)
        {
            if (!_readyAt.TryGetValue(defenderId, out var ready) || !_length.TryGetValue(defenderId, out var length)) return;
            _readyAt[defenderId] = Math.Max(tick, ready - (int)Math.Round(length * Math.Max(0f, fraction)));
        }

        public void Remove(string defenderId) { _readyAt.Remove(defenderId); _length.Remove(defenderId); }
    }
}
