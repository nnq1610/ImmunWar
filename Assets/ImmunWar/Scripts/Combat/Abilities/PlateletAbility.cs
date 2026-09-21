using ImmunWar.Battle.State;

namespace ImmunWar.Combat.Abilities
{
    public sealed class PlateletAbility
    {
        public int Repair(VitalityState vitality, int amount) => vitality?.Repair(amount) ?? 0;
        public float SlowMultiplier => .65f;
    }
}
