using ImmunWar.Battle.State;

namespace ImmunWar.Battle.Blocking
{
    public static class BlockSystem
    {
        public static bool TryBlock(EnemyState enemy, DefenderState defender)
        {
            if (enemy == null || defender == null || enemy.IsTerminal || !string.IsNullOrEmpty(enemy.BlockedById)) return false;
            enemy.BlockedById = defender.InstanceId;
            return true;
        }
        public static void Release(EnemyState enemy, string defenderId)
        { if (enemy != null && enemy.BlockedById == defenderId) enemy.BlockedById = null; }
    }
}

