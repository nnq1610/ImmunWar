namespace ImmunWar.Combat.Abilities
{
    public sealed class NkCellAbility { public float CalculateDamage(float baseDamage, bool targetMutated) => baseDamage * (targetMutated ? 2f : 1f); }
}

