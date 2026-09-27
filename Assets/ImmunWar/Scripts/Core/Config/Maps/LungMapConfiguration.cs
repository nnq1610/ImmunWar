using System;
using UnityEngine;
using ImmunWar.Core.Config;
using ImmunWar.Battle.Placement; // For GridPosition or GridConfiguration if needed

namespace ImmunWar.Core.Config.Maps
{
    public enum AlveoliPlacementRule
    {
        Standard,
        Restricted,
        Enhanced
    }

    [Serializable]
    public class AlveoliStructure
    {
        public Vector2 Position;
        public float Radius;
        public AlveoliPlacementRule PlacementRule;
    }

    [Serializable]
    public class AirwayDefinition
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public float Width;
    }

    [Serializable]
    public class BreathingCycleEffect
    {
        public float CycleDuration;
        public float ExpansionMultiplier;
        public float PathogenSpeedModifier;
    }

    [Serializable]
    public class OxygenLevelEffect
    {
        public Vector2 Position;
        public float Radius;
        public float ATPGenerationMultiplier;
    }

    [CreateAssetMenu(menuName = "ImmunWar/Organ Maps/Lung Configuration")]
    public class LungMapConfiguration : GridConfiguration
    {
        [Header("Lung-Specific Features")]
        public AlveoliStructure[] AlveoliPositions;
        public AirwayDefinition[] BranchingAirways;
        
        [Tooltip("Required exactly 4 entry points")]
        public Vector2[] PathogenEntryPoints = new Vector2[4]; 
        
        [Header("Environmental Challenges")]
        public BreathingCycleEffect BreathingCycle;
        public OxygenLevelEffect[] OxygenZones;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (PathogenEntryPoints == null || PathogenEntryPoints.Length != 4)
            {
                Debug.LogWarning("LungMapConfiguration requires exactly 4 pathogen entry points.");
            }
        }
#endif
    }
}
