using NUnit.Framework;
using ImmunWar.Core.Config;
using ImmunWar.Battle.Placement;
using ImmunWar.UI;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace ImmunWar.Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlacementController grid-based interaction
    /// Requirements: 1.2, 4.7, 8.1
    /// </summary>
    [TestFixture]
    public class PlacementControllerTests
    {
        private GameObject _controllerGameObject;
        private PlacementController _placementController;
        private GridSystem _gridSystem;
        private ValidationSystem _validationSystem;
        private GridConfiguration _testConfig;

        [SetUp]
        public void Setup()
        {
            // Create test configuration
            _testConfig = ScriptableObject.CreateInstance<GridConfiguration>();
            _testConfig.Width = 10;
            _testConfig.Height = 8;
            _testConfig.CellSize = Vector2.one;
            _testConfig.GridOrigin = Vector2.zero;

            // Initialize systems
            _gridSystem = new GridSystem(_testConfig);
            _validationSystem = new ValidationSystem(_gridSystem, _testConfig);

            // Create PlacementController GameObject
            _controllerGameObject = new GameObject("TestPlacementController");
            _placementController = _controllerGameObject.AddComponent<PlacementController>();
            
            // Initialize with systems
            _placementController.Initialize(null, _gridSystem, _validationSystem);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerGameObject != null)
                Object.DestroyImmediate(_controllerGameObject);
                
            if (_testConfig != null)
                Object.DestroyImmediate(_testConfig);
        }

        [Test]
        public void PlacementState_InitialState_IsInactive()
        {
            // Act
            var state = _placementController.GetCurrentState();

            // Assert
            Assert.IsFalse(state.IsActive, "Initial placement state should be inactive");
            Assert.IsNull(state.SelectedDefenderId, "No defender should be selected initially");
            Assert.IsFalse(state.PreviewActive, "Preview should not be active initially");
        }

        [Test]
        public void SelectDefender_WithValidDefender_ActivatesState()
        {
            // Arrange
            var defenderId = "macrophage";
            var role = DefenderRole.Blocker;

            // Act
            _placementController.SelectDefender(defenderId, role);
            var state = _placementController.GetCurrentState();

            // Assert
            Assert.IsTrue(state.IsActive, "State should be active after defender selection");
            Assert.AreEqual(defenderId, state.SelectedDefenderId, "Selected defender ID should match");
            Assert.AreEqual(role, state.SelectedRole, "Selected role should match");
        }

        [Test]
        public void CancelSelection_WithActiveSelection_DeactivatesState()
        {
            // Arrange
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);

            // Act
            _placementController.CancelSelection();
            var state = _placementController.GetCurrentState();

            // Assert
            Assert.IsFalse(state.IsActive, "State should be inactive after cancellation");
            Assert.IsNull(state.SelectedDefenderId, "Defender selection should be cleared");
            Assert.IsFalse(state.PreviewActive, "Preview should be inactive after cancellation");
        }

        [Test]
        public void HandleGridNavigation_WithValidDirection_UpdatesHoveredPosition()
        {
            // Arrange
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);
            var initialState = _placementController.GetCurrentState();

            // Act
            _placementController.HandleGridNavigation(Vector2Int.right);
            var newState = _placementController.GetCurrentState();

            // Assert
            Assert.AreEqual(initialState.HoveredPosition.X + 1, newState.HoveredPosition.X, "Hovered position X should increase by 1");
            Assert.AreEqual(initialState.HoveredPosition.Y, newState.HoveredPosition.Y, "Hovered position Y should remain unchanged");
        }

        [Test]
        public void UpdatePreview_WithValidPosition_ShowsValidPreview()
        {
            // Arrange
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);
            var validPosition = new GridPosition(5, 3);

            // Act
            _placementController.UpdatePreview(validPosition);
            var state = _placementController.GetCurrentState();

            // Assert
            Assert.IsTrue(state.PreviewActive, "Preview should be active for valid position");
            Assert.AreEqual(validPosition, state.HoveredPosition, "Hovered position should match preview position");
        }

        [Test]
        public void UpdatePreview_WithInvalidPosition_ShowsInvalidPreview()
        {
            // Arrange
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);
            var invalidPosition = new GridPosition(-1, 3); // Out of bounds

            // Act
            _placementController.UpdatePreview(invalidPosition);
            var state = _placementController.GetCurrentState();

            // Assert
            Assert.IsFalse(state.CurrentValidation.IsValid, "Validation should indicate invalid position");
            Assert.AreEqual(ValidationFailureReason.OutOfBounds, state.CurrentValidation.Reason, "Should indicate out of bounds failure");
        }

        [Test]
        public void IsPositionHovered_WithHoveredPosition_ReturnsTrue()
        {
            // Arrange
            var position = new GridPosition(3, 3);
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);
            _placementController.UpdatePreview(position);

            // Act
            var isHovered = _placementController.IsPositionHovered(position);

            // Assert
            Assert.IsTrue(isHovered, "Position should be reported as hovered when it matches current hover state");
        }

        [Test]
        public void IsPositionHovered_WithNonHoveredPosition_ReturnsFalse()
        {
            // Arrange
            var hoveredPosition = new GridPosition(3, 3);
            var otherPosition = new GridPosition(5, 5);
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);
            _placementController.UpdatePreview(hoveredPosition);

            // Act
            var isHovered = _placementController.IsPositionHovered(otherPosition);

            // Assert
            Assert.IsFalse(isHovered, "Non-hovered position should return false");
        }

        [Test]
        public void HandleMouseInput_WithScreenPosition_ProcessesCorrectly()
        {
            // Arrange
            var screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => _placementController.HandleMouseInput(screenPosition), 
                "Mouse input handling should not throw exceptions with valid screen position");
        }

        [Test]
        public void PlacementState_WithSelection_CreatesCorrectState()
        {
            // Arrange
            var defenderId = "tcell";
            var role = DefenderRole.Damage;

            // Act
            var state = new PlacementState().WithSelection(defenderId, role);

            // Assert
            Assert.IsTrue(state.IsActive, "State with selection should be active");
            Assert.AreEqual(defenderId, state.SelectedDefenderId, "Defender ID should be set");
            Assert.AreEqual(role, state.SelectedRole, "Role should be set");
        }

        [Test]
        public void PlacementState_WithHover_UpdatesHoveredPosition()
        {
            // Arrange
            var position = new GridPosition(7, 4);
            var initialState = new PlacementState(true, "defender", DefenderRole.Damage);

            // Act
            var state = initialState.WithHover(position);

            // Assert
            Assert.IsTrue(state.PreviewActive, "Preview should be active when hovering");
            Assert.AreEqual(position, state.HoveredPosition, "Hovered position should be updated");
        }

        [Test]
        public void PlacementState_ClearSelection_ResetsState()
        {
            // Arrange
            var activeState = new PlacementState(true, "defender", DefenderRole.Damage, new GridPosition(3, 3), true);

            // Act
            var clearedState = activeState.ClearSelection();

            // Assert
            Assert.IsFalse(clearedState.IsActive, "Cleared state should be inactive");
            Assert.IsNull(clearedState.SelectedDefenderId, "Defender ID should be null");
            Assert.IsFalse(clearedState.PreviewActive, "Preview should be inactive");
        }

        [Test]
        public void PlacementState_ClearPreview_KeepsSelectionActive()
        {
            // Arrange
            var stateWithPreview = new PlacementState(true, "defender", DefenderRole.Damage, new GridPosition(3, 3), true);

            // Act
            var clearedPreviewState = stateWithPreview.ClearPreview();

            // Assert
            Assert.IsTrue(clearedPreviewState.IsActive, "Selection should remain active");
            Assert.AreEqual("defender", clearedPreviewState.SelectedDefenderId, "Defender ID should be preserved");
            Assert.IsFalse(clearedPreviewState.PreviewActive, "Preview should be cleared");
        }

        [Test]
        public void LegacyCompatibility_HasSelection_WorksWithNewState()
        {
            // Arrange & Act
            _placementController.SelectDefender("macrophage", DefenderRole.Blocker);

            // Assert
            Assert.IsTrue(_placementController.HasSelection, "Legacy HasSelection property should work with new state system");
        }

        [Test]
        public void LegacyCompatibility_Select_WorksWithNewSystem()
        {
            // Arrange
            var configId = "legacy-defender";
            var role = DefenderRole.Support;
            var cost = 150;
            var health = 100f;

            // Act
            _placementController.Select(configId, role, cost, health);

            // Assert
            Assert.IsTrue(_placementController.HasSelection, "Legacy Select method should activate new state system");
            var state = _placementController.GetCurrentState();
            Assert.AreEqual(configId, state.SelectedDefenderId, "Legacy selection should set defender ID in new state");
            Assert.AreEqual(role, state.SelectedRole, "Legacy selection should set role in new state");
        }
    }
}