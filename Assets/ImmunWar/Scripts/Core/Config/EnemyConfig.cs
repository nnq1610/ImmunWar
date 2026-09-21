using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Enemy")]
    public sealed class EnemyConfig : GameConfig
    {
        public string displayNameKey;
        [Min(1)] public int maxHealth = 40;
        [Min(0.01f)] public float moveSpeed = 1f;
        [Min(0)] public int organDamage = 5;
        [Min(0)] public int atpReward = 5;
        public MutationDefinition[] mutations;
        public BossPhaseConfig[] bossPhases;
        public PresentationConfig presentation;
    }
}

