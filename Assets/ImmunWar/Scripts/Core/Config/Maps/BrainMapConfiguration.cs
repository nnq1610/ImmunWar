using System;
using UnityEngine;
using ImmunWar.Core.Config;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Core.Config.Maps
{
    public enum SynapticType
    {
        Excitatory,
        Inhibitory,
        Modulatory
    }

    public enum ChokepointAdvantage
    {
        None,
        DamageBoost,
        SlowEffect,
        ChainLightning
    }

    [Serializable]
    public class NeuralPathway
    {
        public Vector2[] Nodes;
        public float SignalSpeed;
    }

    [Serializable]
    public class SynapticGap
    {
        public Vector2 Position;
        public Vector2 Size;
        public SynapticType Type;
        public ChokepointAdvantage Advantage;
    }

    [Serializable]
    public class BrainRegion
    {
        public string RegionName;
        public Vector2[] Boundaries;
        public DefenderRoleMask RoleBonus;
    }

    [Serializable]
    public class NeuralActivityPattern
    {
        public float ActivationInterval;
        public float Duration;
        public float IntensityMultiplier;
    }

    [Serializable]
    public class SynapticPlasticity
    {
        public bool AllowDynamicPathing;
        public float AdaptationRate;
    }

    [CreateAssetMenu(menuName = "ImmunWar/Organ Maps/Brain Configuration")]
    public class BrainMapConfiguration : GridConfiguration
    {
        [Header("Brain-Specific Features")]
        public NeuralPathway[] NeuralNetworks;
        public SynapticGap[] SynapticGaps;
        public BrainRegion[] FunctionalRegions;
        
        [Header("Neural Activity")]
        public NeuralActivityPattern[] ActivityPatterns;
        public SynapticPlasticity PlasticityRules;
    }
}
