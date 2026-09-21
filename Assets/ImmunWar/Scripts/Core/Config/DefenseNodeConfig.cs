using System;
using UnityEngine;

namespace ImmunWar.Core.Config
{
    [Flags]
    public enum DefenderRoleMask { None = 0, Blocker = 1, Damage = 2, Support = 4, Burst = 8, Repair = 16, Economy = 32, All = 63 }

    [CreateAssetMenu(menuName = "Immune War/Defense Node")]
    public sealed class DefenseNodeConfig : GameConfig
    {
        public Vector2 position;
        public DefenderRoleMask allowedRoleMask = DefenderRoleMask.All;
        public string routeId;
        [Min(0f)] public float routeProgress;
    }
}

