using ImmunWar.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace ImmunWar.Tests.EditMode
{
    public sealed class OrganMapConfigTests
    {
        [Test]
        public void BrainAndStomachHaveStableRoutesNodesAndWaves()
        {
            foreach (var id in new[] { "map_brain", "map_stomach" })
            {
                var route = ScriptableObject.CreateInstance<RouteConfig>(); route.SetIdForEditor(id + "_route"); route.waypoints = new[] { Vector2.left, Vector2.right };
                var node = ScriptableObject.CreateInstance<DefenseNodeConfig>(); node.SetIdForEditor(id + "_node"); node.routeId = route.Id;
                var waves = ScriptableObject.CreateInstance<WaveSet>(); waves.SetIdForEditor(id + "_waves"); waves.waves = new[] { new Wave() };
                var map = ScriptableObject.CreateInstance<OrganMapConfig>(); map.SetIdForEditor(id); map.routes = new[] { route }; map.nodes = new[] { node }; map.waveSet = waves;
                Assert.That(map.Id, Is.Not.Empty); Assert.That(map.routes[0].waypoints.Length, Is.GreaterThanOrEqualTo(2)); Assert.That(map.nodes[0].routeId, Is.EqualTo(route.Id)); Assert.That(map.waveSet, Is.Not.Null);
                Object.DestroyImmediate(map); Object.DestroyImmediate(waves); Object.DestroyImmediate(node); Object.DestroyImmediate(route);
            }
        }
    }
}
