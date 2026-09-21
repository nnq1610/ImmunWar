using System.Collections.Generic;
using ImmunWar.Battle.State;

namespace ImmunWar.Combat
{
    public static class TargetingSystem
    {
        public static EnemyState Select(IEnumerable<EnemyState> candidates)
        {
            EnemyState selected = null;
            foreach (var enemy in candidates)
            {
                if (enemy == null || enemy.IsTerminal) continue;
                if (selected == null || enemy.RouteProgress > selected.RouteProgress ||
                    enemy.RouteProgress == selected.RouteProgress && string.CompareOrdinal(enemy.InstanceId, selected.InstanceId) < 0)
                    selected = enemy;
            }
            return selected;
        }
    }
}

