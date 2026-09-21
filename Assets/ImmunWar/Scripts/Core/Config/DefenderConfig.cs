using UnityEngine;

namespace ImmunWar.Core.Config
{
    public enum DefenderRole { Blocker, Damage, Support, Burst, Repair, Economy }

    [CreateAssetMenu(menuName = "Immune War/Defender")]
    public sealed class DefenderConfig : GameConfig
    {
        public string displayNameKey;
        public DefenderRole role;
        [Min(0)] public int atpCost = 25;
        [Min(1)] public int maxHealth = 100;
        [Min(0f)] public float attackDamage = 10f;
        [Min(0.01f)] public float attackInterval = 1f;
        [Min(0f)] public float range = 2f;
        public AbilityConfig ability;
        public PresentationConfig presentation;
    }
}

