using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Boss Phase")]
    public sealed class BossPhaseConfig : GameConfig
    {
        [Range(0f, 1f)] public float healthThreshold = 1f;
        public AbilityConfig[] abilities;
        [Min(0)] public int transitionInvulnerabilityTicks = 15;
    }
}

