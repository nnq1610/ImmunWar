using System;
using System.Collections.Generic;
using ImmunWar.Battle.State;
using ImmunWar.Core.Config;

namespace ImmunWar.StatusEffects
{
    public static class StatusIds
    {
        public const string Burn = "status_burn";
        public const string Freeze = "status_freeze";
        public const string FreezeResist = "status_freeze_resist";
        public const string Slow = "status_slow";
        public const string Stun = "status_stun";
        public const string Mark = "status_mark";
        public const string Poison = "status_poison";
        public const string Shield = "status_clot_shield";
        public const string Haste = "status_haste";
        public const string Exhausted = "status_exhausted";
        public const string Digesting = "status_digesting";
        public const string AdaptedPrefix = "status_adapted:";
    }

    public readonly struct StatusSpec
    {
        public string Id { get; }
        public int DurationTicks { get; }
        public int MaximumStacks { get; }
        public StatusStackPolicy Policy { get; }
        public StatusSpec(string id, int durationTicks, int maximumStacks, StatusStackPolicy policy)
        { Id = id; DurationTicks = durationTicks; MaximumStacks = maximumStacks; Policy = policy; }
    }

    public enum ControlOutcome { Applied, Shielded, Adapted }

    /// <summary>
    /// Combat statuses shared by enemies and defenders (30 ticks = 1 s) and the rules they impose:
    /// freeze/stun disable, slow and haste scale speed, burn/poison pulse damage, frost and fire cancel each other,
    /// and repeated freezes/stuns shorten through a decaying resistance.
    /// </summary>
    public static class CombatStatus
    {
        public const int PulseTicks = 15;
        public const float BurnDamagePerStack = 2f;
        public const float PoisonDamagePerStack = 1.5f;
        public const float SlowMoveMultiplier = 0.55f;
        public const float MarkDamageMultiplier = 1.25f;
        public const float HasteCooldownMultiplier = 0.5f;
        public const int ParalyzePoisonStacks = 3;
        private const float ResistPerStack = 0.35f;
        private const float MinimumControlFraction = 0.25f;
        private const int ResistLingerTicks = 150;

        public static readonly StatusSpec Burn = new StatusSpec(StatusIds.Burn, 90, 3, StatusStackPolicy.Stack);
        public static readonly StatusSpec Freeze = new StatusSpec(StatusIds.Freeze, 60, 1, StatusStackPolicy.Refresh);
        public static readonly StatusSpec Slow = new StatusSpec(StatusIds.Slow, 60, 1, StatusStackPolicy.Refresh);
        public static readonly StatusSpec Stun = new StatusSpec(StatusIds.Stun, 45, 1, StatusStackPolicy.Refresh);
        public static readonly StatusSpec Mark = new StatusSpec(StatusIds.Mark, 90, 1, StatusStackPolicy.Refresh);
        public static readonly StatusSpec Poison = new StatusSpec(StatusIds.Poison, 90, ParalyzePoisonStacks, StatusStackPolicy.Stack);
        public static readonly StatusSpec Shield = new StatusSpec(StatusIds.Shield, 60, 1, StatusStackPolicy.Refresh);
        public static readonly StatusSpec Haste = new StatusSpec(StatusIds.Haste, 150, 1, StatusStackPolicy.Refresh);
        public static readonly StatusSpec Exhausted = new StatusSpec(StatusIds.Exhausted, 240, 1, StatusStackPolicy.Refresh);
        public static readonly StatusSpec Digesting = new StatusSpec(StatusIds.Digesting, 90, 1, StatusStackPolicy.Refresh);

        public static StatusEffectState Find(IList<StatusEffectState> effects, string id)
        {
            for (var i = 0; i < effects.Count; i++) if (effects[i].ConfigId == id) return effects[i];
            return null;
        }

        public static bool Has(IList<StatusEffectState> effects, string id) => Find(effects, id) != null;
        public static int Stacks(IList<StatusEffectState> effects, string id) => Find(effects, id)?.Stacks ?? 0;
        public static int Remaining(IList<StatusEffectState> effects, string id) => Find(effects, id)?.RemainingTicks ?? 0;

        /// <summary>Frozen or stunned units neither move, attack nor block.</summary>
        public static bool IsDisabled(IList<StatusEffectState> effects) => Has(effects, StatusIds.Freeze) || Has(effects, StatusIds.Stun);

        /// <summary>Units encased in a clot or digesting still hold their ground but cannot attack.</summary>
        public static bool CanAttack(IList<StatusEffectState> effects) =>
            !IsDisabled(effects) && !Has(effects, StatusIds.Shield) && !Has(effects, StatusIds.Digesting);

        public static float MoveMultiplier(IList<StatusEffectState> effects) =>
            IsDisabled(effects) ? 0f : Has(effects, StatusIds.Slow) ? SlowMoveMultiplier : 1f;

        public static float DamageTakenMultiplier(IList<StatusEffectState> effects) =>
            Has(effects, StatusIds.Shield) ? 0f : Has(effects, StatusIds.Mark) ? MarkDamageMultiplier : 1f;

        public static float CooldownMultiplier(IList<StatusEffectState> effects) =>
            Has(effects, StatusIds.Haste) ? HasteCooldownMultiplier : 1f;

        /// <summary>Freeze/stun duration after the target's resistance from recent freezes/stuns.</summary>
        public static int ControlDuration(IList<StatusEffectState> effects, int baseTicks)
        {
            var fraction = Math.Max(MinimumControlFraction, 1f - ResistPerStack * Stacks(effects, StatusIds.FreezeResist));
            return Math.Max(1, (int)Math.Round(baseTicks * fraction));
        }

        public static StatusEffectState Apply(StatusEffectSystem system, IList<StatusEffectState> effects, StatusSpec spec, string sourceId, int durationTicks = 0) =>
            system.Apply(effects, spec.Id, sourceId, durationTicks > 0 ? durationTicks : spec.DurationTicks, spec.MaximumStacks, spec.Policy);

        /// <summary>
        /// Applies freeze, stun or slow. A clot shield blocks it; an adaptive target is immune to a control it was
        /// hit by within the last few seconds. Freeze puts out burning.
        /// </summary>
        public static ControlOutcome ApplyControl(StatusEffectSystem system, IList<StatusEffectState> effects, StatusSpec spec, string sourceId, bool adaptive = false, int durationTicks = 0)
        {
            if (Has(effects, StatusIds.Shield)) return ControlOutcome.Shielded;
            var adaptedId = StatusIds.AdaptedPrefix + spec.Id;
            if (adaptive && Has(effects, adaptedId)) return ControlOutcome.Adapted;
            var hard = spec.Id == StatusIds.Freeze || spec.Id == StatusIds.Stun;
            var duration = durationTicks > 0 ? durationTicks : spec.DurationTicks;
            if (hard) duration = ControlDuration(effects, duration);
            if (spec.Id == StatusIds.Freeze) system.Cleanse(effects, StatusIds.Burn);
            system.Apply(effects, spec.Id, sourceId, duration, spec.MaximumStacks, spec.Policy);
            if (hard) system.Apply(effects, StatusIds.FreezeResist, sourceId, duration + ResistLingerTicks, 2, StatusStackPolicy.Stack);
            if (adaptive) system.Apply(effects, adaptedId, sourceId, duration + ResistLingerTicks, 1, StatusStackPolicy.Refresh);
            return ControlOutcome.Applied;
        }

        /// <summary>Adds a burn stack. Fire thaws a frozen target; returns true when it did.</summary>
        public static bool ApplyBurn(StatusEffectSystem system, IList<StatusEffectState> effects, string sourceId)
        {
            var thawed = system.Cleanse(effects, StatusIds.Freeze);
            Apply(system, effects, Burn, sourceId);
            return thawed;
        }

        /// <summary>Damage dealt by burn and poison this tick; call once per tick after <see cref="StatusEffectSystem.Tick"/>.</summary>
        public static float PulseDamage(IList<StatusEffectState> effects)
        {
            var damage = 0f;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect.RemainingTicks % PulseTicks != 0) continue;
                if (effect.ConfigId == StatusIds.Burn) damage += BurnDamagePerStack * effect.Stacks;
                else if (effect.ConfigId == StatusIds.Poison) damage += PoisonDamagePerStack * effect.Stacks;
            }
            return damage;
        }
    }
}
