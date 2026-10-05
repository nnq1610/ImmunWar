using System;
using ImmunWar.Battle.State;

namespace ImmunWar.Combat
{
    /// <summary>Damage-path rules for enemy skills: biofilm shields, spiked shells and one-time revival.</summary>
    public static class EnemyTraits
    {
        public const float BiofilmThreshold = 0.5f;
        public const float BiofilmShieldFraction = 0.2f;
        public const float SpikeReflectFraction = 0.1f;
        public const float ReviveHealthFraction = 0.3f;

        /// <summary>Soaks damage into a shield first; returns the damage left for health.</summary>
        public static float Absorb(ref float shield, float damage)
        {
            if (shield <= 0f || damage <= 0f) return Math.Max(0f, damage);
            var soaked = Math.Min(shield, damage);
            shield -= soaked;
            return damage - soaked;
        }

        /// <summary>Brings a just-defeated enemy back with part of its health. Executed enemies stay down.</summary>
        public static bool TryRevive(EnemyState enemy, float maxHealth, bool executed)
        {
            if (enemy == null || enemy.TerminalResult != EnemyTerminalResult.Defeated || executed) return false;
            enemy.TerminalResult = EnemyTerminalResult.None;
            enemy.Health = Math.Max(1f, maxHealth * ReviveHealthFraction);
            return true;
        }
    }
}
