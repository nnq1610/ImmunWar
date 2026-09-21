using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Fever")]
    public sealed class FeverConfig : GameConfig
    {
        [Min(1)] public int maximumCharge = 100;
        [Min(1)] public int durationTicks = 300;
        [Min(0f)] public float damageMultiplier = 1.5f;
        [Min(0f)] public float speedMultiplier = 1.25f;
    }
}

