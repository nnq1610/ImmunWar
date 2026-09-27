using NUnit.Framework;
using UnityEngine;
using ImmunWar.Battle.Placement;
using ImmunWar.Core.Config;
using ImmunWar.UI;

namespace ImmunWar.Tests
{
    public class PolishTests
    {
        [Test]
        public void Property2_PreviewIndicatorConsistency()
        {
            var config = GridConfiguration.CreateDefault();
            var gridSystem = new GridSystem(config);
            var validationSystem = new ValidationSystem(gridSystem, config);
            
            var go = new GameObject();
            var placementController = go.AddComponent<PlacementController>();
            placementController.Initialize(null, gridSystem, validationSystem);
            
            placementController.SelectDefender("TestDefender", DefenderRole.Damage);
            
            var validPos = new GridPosition(0, 0);
            placementController.UpdatePreview(validPos);
            
            var state = placementController.GetCurrentState();
            Assert.IsTrue(state.PreviewActive);
            
            placementController.HidePreview();
            state = placementController.GetCurrentState();
            Assert.IsFalse(state.PreviewActive);
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Property3_ValidationErrorAccuracy()
        {
            var config = GridConfiguration.CreateDefault();
            var gridSystem = new GridSystem(config);
            var validationSystem = new ValidationSystem(gridSystem, config);

            // Test Out of Bounds
            var request = new PlacementRequest(new GridPosition(-1, -1), "Test", DefenderRole.Damage, 100, 500);
            var result = validationSystem.ValidatePlacement(request);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(ValidationFailureReason.OutOfBounds, result.Reason);
            
            // Test Insufficient ATP
            request = new PlacementRequest(new GridPosition(0, 0), "Test", DefenderRole.Damage, 600, 500);
            result = validationSystem.ValidatePlacement(request);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(ValidationFailureReason.InsufficientATP, result.Reason);
        }
    }
}
