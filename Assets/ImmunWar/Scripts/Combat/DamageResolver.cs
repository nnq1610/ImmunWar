using System;
using ImmunWar.Battle.State;

namespace ImmunWar.Combat
{
    public readonly struct DamageResult
    {
        public bool Applied { get; } public bool Defeated { get; } public float RemainingHealth { get; }
        public DamageResult(bool applied, bool defeated, float remaining) { Applied = applied; Defeated = defeated; RemainingHealth = remaining; }
    }

    public static class DamageResolver
    {
        public static DamageResult Apply(EnemyState enemy, float amount)
        {
            if (enemy == null || enemy.IsTerminal || amount <= 0f) return new DamageResult(false, false, enemy?.Health ?? 0f);
            enemy.Health = Math.Max(0f, enemy.Health - amount);
            var defeated = enemy.Health <= 0f;
            if (defeated) enemy.TerminalResult = EnemyTerminalResult.Defeated;
            return new DamageResult(true, defeated, enemy.Health);
        }
    }
}

