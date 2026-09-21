using ImmunWar.Battle.State;
using ImmunWar.StatusEffects;

namespace ImmunWar.Combat.Abilities
{
    public sealed class BCellAbility
    {
        private readonly StatusEffectSystem _statuses = new StatusEffectSystem();
        public bool Cleanse(DefenseNodeState node, string statusId) => node != null && _statuses.Cleanse(node.Effects, statusId);
    }
}

