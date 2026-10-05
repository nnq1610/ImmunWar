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

        [Header("Melee movement")]
        [Tooltip("Walks out from its placement point to fight enemies inside the guard radius, then walks back.")]
        public bool mobile;
        [Min(0f)] public float moveSpeed = 1.5f;
        [Tooltip("Radius around the placement point this defender guards (world units).")]
        [Min(0f)] public float engageRadius = 2.2f;
        [Tooltip("Enemies it can hold at once; 0 = unlimited.")]
        [Min(0)] public int blockCapacity;
        public PresentationConfig presentation;
    }
}

