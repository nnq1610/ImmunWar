using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Route")]
    public sealed class RouteConfig : GameConfig
    {
        public Vector2[] waypoints;
    }
}

