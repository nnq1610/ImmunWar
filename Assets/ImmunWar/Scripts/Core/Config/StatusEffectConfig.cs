using UnityEngine;

namespace ImmunWar.Core.Config
{
    public enum StatusStackPolicy { Replace, Refresh, Stack }

    [CreateAssetMenu(menuName = "Immune War/Status Effect")]
    public sealed class StatusEffectConfig : GameConfig
    {
        [Min(1)] public int durationTicks = 90;
        [Min(1)] public int maximumStacks = 1;
        public StatusStackPolicy stackPolicy = StatusStackPolicy.Refresh;
        public float damageMultiplier = 1f;
        public float speedMultiplier = 1f;
    }
}

