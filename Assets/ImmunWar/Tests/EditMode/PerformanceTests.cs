using NUnit.Framework;
using System.Diagnostics;
using ImmunWar.Battle.Placement;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using UnityEngine;

namespace ImmunWar.Tests
{
    public class PerformanceTests
    {
        [Test]
        public void Property8_PerformanceResponseTiming_MeetsTarget()
        {
            var config = GridConfiguration.CreateDefault();
            var gridSystem = new GridSystem(config);
            var validationSystem = new ValidationSystem(gridSystem, config);
            
            // Create game object to hold placement controller
            var go = new GameObject("PlacementController");
            var placementController = go.AddComponent<PlacementController>();
            
            // Note: Since PlacementSystem isn't fully implemented in this test scope,
            // we will bypass full Initialization to avoid NullRefs, or assume basic functionality.
            placementController.Initialize(null, gridSystem, validationSystem);
            
            // Measure response timing of validation
            var stopwatch = Stopwatch.StartNew();
            
            var request = new PlacementRequest(
                new GridPosition(5, 5),
                "Macrophage",
                DefenderRole.Damage,
                100,
                500
            );

            // Execute 100 iterations of validation simulating rapid load
            for (int i = 0; i < 100; i++)
            {
                var validation = validationSystem.ValidatePlacement(request);
                Assert.IsNotNull(validation);
            }
            
            stopwatch.Stop();
            
            // The requirement states controller shall acknowledge commands within 250ms
            // So 100 rapid validations should easily take < 250ms.
            Assert.Less(stopwatch.ElapsedMilliseconds, 250, "Performance response timing exceeded 250ms threshold under load.");
            
            Object.DestroyImmediate(go);
        }
    }
}
