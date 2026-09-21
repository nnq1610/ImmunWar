using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Ability")]
    public sealed class AbilityConfig : GameConfig
    {
        public string abilityType;
        [Min(0f)] public float magnitude = 1f;
        [Min(0)] public int cooldownTicks = 30;
        public StatusEffectConfig appliedStatus;
    }
}

