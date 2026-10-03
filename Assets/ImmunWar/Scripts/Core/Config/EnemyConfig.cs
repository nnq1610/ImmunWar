using UnityEngine;

namespace ImmunWar.Core.Config
{
    /// <summary>How an enemy attacks the defender blocking it (or, for ranged styles, defenders it passes).</summary>
    public enum EnemyAttackStyle
    {
        Bite,       // single lunge
        Nibble,     // fast, weak repeated bites
        Slam,       // body slam hitting every defender close by
        Whip,       // flagellum lash
        Ram,        // slow, heavy charge
        AcidSpit,   // ranged glob that poisons
        Claw,       // slashing swipe
        Drain,      // bite that heals the attacker
        SporeBurst  // wide spore cloud around the boss
    }

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

        [Header("Attack")]
        public EnemyAttackStyle attackStyle = EnemyAttackStyle.Bite;
        [Tooltip("Above 0, the enemy also attacks defenders within this range while walking (world units).")]
        [Min(0f)] public float attackRange;

        [Header("Traits")]
        [Tooltip("Fraction of incoming damage ignored (0 = none, 0.5 = half).")]
        [Range(0f, 0.9f)] public float armor;
        [Tooltip("Health restored per second while alive.")]
        [Min(0f)] public float regenPerSecond;
        [Tooltip("Damage dealt to a blocking defender per bite, relative to half of organDamage.")]
        [Min(0f)] public float biteMultiplier = 1f;
        [Tooltip("Enemy spawned at this one's position when it dies.")]
        public EnemyConfig splitInto;
        [Min(0)] public int splitCount;

        [Header("Look")]
        [Tooltip("Reuse the sprite animation of another enemy ID (empty = this enemy's own).")]
        public string visualBaseId;
        public Color tint = Color.white;
        [Min(0.2f)] public float visualScale = 1f;
        [Tooltip("Non-uniform scale that changes the silhouette, e.g. (1.3, 0.8) for a stretched fast virus.")]
        public Vector2 stretch = Vector2.one;
    }
}

