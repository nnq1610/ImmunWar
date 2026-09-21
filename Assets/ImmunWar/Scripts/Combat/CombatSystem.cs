using System;
using System.Collections.Generic;
using ImmunWar.Battle.State;

namespace ImmunWar.Combat
{
    public sealed class CombatSystem
    {
        public event Action<EnemyState, float> EnemyDamaged;
        public event Action<EnemyState> EnemyDefeated;

        public EnemyState Attack(DefenderState attacker, IEnumerable<EnemyState> candidates, float damage, int cooldownTicks)
        {
            if (attacker == null || attacker.Health <= 0f || attacker.CooldownTicks > 0) return null;
            var target = TargetingSystem.Select(candidates);
            if (target == null) return null;
            var result = DamageResolver.Apply(target, damage);
            if (!result.Applied) return null;
            attacker.CooldownTicks = Math.Max(1, cooldownTicks);
            EnemyDamaged?.Invoke(target, damage);
            if (result.Defeated) EnemyDefeated?.Invoke(target);
            return target;
        }

        public static void TickCooldown(DefenderState defender)
        { if (defender != null && defender.CooldownTicks > 0) defender.CooldownTicks--; }
    }
}

