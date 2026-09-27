using System;
using UnityEngine;
using ImmunWar.Core.Config;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Core.Config.Maps
{
    public enum DefenderEffect
    {
        None,
        DamageOverTime,
        ReducedRange,
        IncreasedCost
    }

    public enum PathogenEffect
    {
        None,
        SpeedReduction,
        DamageOverTime,
        MutationChance
    }

    [Serializable]
    public class DigestiveTractPath
    {
        public Vector2[] Waypoints;
        public float BaseWidth;
    }

    [Serializable]
    public class AcidPoolDefinition
    {
        public Vector2[] PoolArea;
        public float AcidStrength;
        public DefenderEffect DefenderImpact;
        public PathogenEffect PathogenImpact;
    }

    [Serializable]
    public class PeristalsisEffect
    {
        public float WaveInterval;
        public float PushForce;
        public Vector2 Direction;
    }

    [Serializable]
    public class PHLevelZone
    {
        public Vector2 Position;
        public float Radius;
        public float PHLevel;
    }

    [Serializable]
    public class EnzymeActivityArea
    {
        public Vector2 Position;
        public float Radius;
        public float EnzymeConcentration;
    }

    [CreateAssetMenu(menuName = "ImmunWar/Organ Maps/Stomach Configuration")]
    public class StomachMapConfiguration : GridConfiguration
    {
        [Header("Stomach-Specific Features")]
        public DigestiveTractPath MainTract;
        public AcidPoolDefinition[] AcidPools;
        public PeristalsisEffect WaveMotion;
        
        [Header("Chemical Environment")]
        public PHLevelZone[] PHZones;
        public EnzymeActivityArea[] EnzymeZones;
    }
}
