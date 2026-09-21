using System;
using UnityEngine;

namespace ImmunWar.Core.Config
{
    [Serializable]
    public sealed class SpawnGroup
    {
        public EnemyConfig enemy;
        public string routeId;
        [Min(1)] public int count = 1;
        [Min(1)] public int intervalTicks = 15;
    }

    [Serializable]
    public sealed class Wave
    {
        public SpawnGroup[] groups;
    }

    [CreateAssetMenu(menuName = "Immune War/Wave Set")]
    public sealed class WaveSet : GameConfig
    {
        public Wave[] waves;
    }
}

