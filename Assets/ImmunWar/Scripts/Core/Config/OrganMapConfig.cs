using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Organ Map")]
    public sealed class OrganMapConfig : GameConfig
    {
        public string displayNameKey;
        public string nextMapId;
        [Min(0)] public int startingAtp = 100;
        [Min(1)] public int maximumVitality = 100;
        public RouteConfig[] routes;
        public DefenseNodeConfig[] nodes;
        public WaveSet waveSet;
        public PresentationConfig presentation;
    }
}

