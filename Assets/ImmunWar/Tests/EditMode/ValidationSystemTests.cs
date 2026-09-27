using NUnit.Framework;
using ImmunWar.Core.Config;
using ImmunWar.Battle.Placement;
using UnityEngine;
using System.Linq;

namespace ImmunWar.Tests.EditMode
{
    /// <summary>
    /// Unit tests for ValidationSystem placement rule enforcement
    /// Requirements: 1.3, 7.1
    /// </summary>
    [TestFixture]
    public class ValidationSystemTests
    {
        private GridSystem _gridSystem;
        private GridConfiguration _testConfig;
        private ValidationSystem _validationSystem;

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
        }

        [TearDown]
        public void TearDown()
        {
            if (_testConfig != null)
                Object.DestroyImmediate(_testConfig);
        }

        [Test]
        public void ValidateGridBounds_WithValidPosition_ReturnsTrue()
        {
            // Arrange
            var validPosition = new GridPosition(5, 3);

            // Act
            var result = _validationSystem.ValidateGridBounds(validPosition);

            // Assert
            Assert.IsTrue(result, "Valid position should pass bounds validation");
        }

        [Test]
        public void ValidateGridBounds_WithInvalidPosition_ReturnsFalse()
        {
            // Arrange
            var invalidPosition = new GridPosition(-1, 5);

            // Act
            var result = _validationSystem.ValidateGridBounds(invalidPosition);

            // Assert
            Assert.IsFalse(result, "Invalid position should fail bounds validation");
        }

        [Test]
        public void ValidateOccupancy_WithEmptyPosition_ReturnsTrue()
        {
            // Arrange
            var emptyPosition = new GridPosition(3, 2);

            // Act
            var result = _validationSystem.ValidateOccupancy(emptyPosition);

            // Assert
            Assert.IsTrue(result, "Empty position should pass occupancy validation");
        }

        [Test]
        public void ValidateOccupancy_WithOccupiedPosition_ReturnsFalse()
        {
            // Arrange
            var position = new GridPosition(4, 4);
            _gridSystem.TryOccupyPosition(position, "test-defender");

            // Act
            var result = _validationSystem.ValidateOccupancy(position);

            // Assert
            Assert.IsFalse(result, "Occupied position should fail occupancy validation");
        }

        [Test]
        public void ValidateATPCost_WithSufficientATP_ReturnsTrue()
        {
            // Arrange
            var defenderId = "macrophage";
            var position = new GridPosition(2, 2);
            var availableATP = 200;

            // Act
            var result = _validationSystem.ValidateATPCost(defenderId, position, availableATP);

            // Assert
            Assert.IsTrue(result, "Sufficient ATP should pass cost validation");
        }

        [Test]
        public void ValidateATPCost_WithInsufficientATP_ReturnsFalse()
        {
            // Arrange
            var defenderId = "energy";
            var position = new GridPosition(2, 2);
            var availableATP = 50; // Energy cells cost 200

            // Act
            var result = _validationSystem.ValidateATPCost(defenderId, position, availableATP);

            // Assert
            Assert.IsFalse(result, "Insufficient ATP should fail cost validation");
        }

        [Test]
        public void ValidateDensityLimits_WithNonEnergyCell_ReturnsTrue()
        {
            // Arrange
            var defenderId = "macrophage"; // Not an energy cell
            var position = new GridPosition(5, 5);

            // Act
            var result = _validationSystem.ValidateDensityLimits(defenderId, position);

            // Assert
            Assert.IsTrue(result, "Non-energy cell defenders should not have density restrictions");
        }

        [Test]
        public void ValidateDensityLimits_WithEnergyCell_NoNearbyEnergyCells_ReturnsTrue()
        {
            // Arrange
            var defenderId = "energy";
            var position = new GridPosition(5, 5);

            // Act
            var result = _validationSystem.ValidateDensityLimits(defenderId, position);

            // Assert
            Assert.IsTrue(result, "Energy cell placement should be allowed when no other energy cells are nearby");
        }

        [Test]
        public void ValidateDensityLimits_WithEnergyCell_NearbyEnergyCell_ReturnsFalse()
        {
            // Arrange
            var defenderId = "energy";
            var position = new GridPosition(5, 5);
            var nearbyPosition = new GridPosition(4, 4); // Within 3x3 area
            
            // Place an energy cell nearby
            _gridSystem.TryOccupyPosition(nearbyPosition, "existing-energy");

            // Act
            var result = _validationSystem.ValidateDensityLimits(defenderId, position);

            // Assert
            Assert.IsFalse(result, "Energy cell placement should be blocked when another energy cell is within 3x3 area");
        }

        [Test]
        public void ValidatePlacement_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new PlacementRequest(
                new GridPosition(3, 3),
                "macrophage",
                DefenderRole.Blocker,
                100,
                200
            );

            // Act
            var result = _validationSystem.ValidatePlacement(request);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid placement request should succeed");
            Assert.AreEqual(ValidationFailureReason.None, result.Reason);
        }

        [Test]
        public void ValidatePlacement_WithOutOfBoundsPosition_ReturnsFailure()
        {
            // Arrange
            var request = new PlacementRequest(
                new GridPosition(-1, 3),
                "macrophage",
                DefenderRole.Blocker,
                100,
                200
            );

            // Act
            var result = _validationSystem.ValidatePlacement(request);

            // Assert
            Assert.IsFalse(result.IsValid, "Out of bounds placement should fail");
            Assert.AreEqual(ValidationFailureReason.OutOfBounds, result.Reason);
            Assert.IsNotEmpty(result.DetailMessage);
        }

        [Test]
        public void ValidatePlacement_WithOccupiedPosition_ReturnsFailure()
        {
            // Arrange
            var position = new GridPosition(2, 2);
            _gridSystem.TryOccupyPosition(position, "existing-defender");

            var request = new PlacementRequest(
                position,
                "macrophage",
                DefenderRole.Blocker,
                100,
                200
            );

            // Act
            var result = _validationSystem.ValidatePlacement(request);

            // Assert
            Assert.IsFalse(result.IsValid, "Occupied position placement should fail");
            Assert.AreEqual(ValidationFailureReason.PositionOccupied, result.Reason);
            Assert.IsNotEmpty(result.DetailMessage);
        }

        [Test]
        public void ValidatePlacement_WithInsufficientATP_ReturnsFailure()
        {
            // Arrange
            var request = new PlacementRequest(
                new GridPosition(3, 3),
                "energy", // Costs 200
                DefenderRole.Economy,
                200,
                100 // Insufficient ATP
            );

            // Act
            var result = _validationSystem.ValidatePlacement(request);

            // Assert
            Assert.IsFalse(result.IsValid, "Insufficient ATP placement should fail");
            Assert.AreEqual(ValidationFailureReason.InsufficientATP, result.Reason);
            Assert.IsNotEmpty(result.DetailMessage);
        }

        [Test]
        public void ConfigValidator_ToMask_ConvertsRolesCorrectly()
        {
            // Test all defender roles convert to correct masks
            Assert.AreEqual(DefenderRoleMask.Blocker, ConfigValidator.ToMask(DefenderRole.Blocker));
            Assert.AreEqual(DefenderRoleMask.Damage, ConfigValidator.ToMask(DefenderRole.Damage));
            Assert.AreEqual(DefenderRoleMask.Support, ConfigValidator.ToMask(DefenderRole.Support));
            Assert.AreEqual(DefenderRoleMask.Burst, ConfigValidator.ToMask(DefenderRole.Burst));
            Assert.AreEqual(DefenderRoleMask.Repair, ConfigValidator.ToMask(DefenderRole.Repair));
            Assert.AreEqual(DefenderRoleMask.Economy, ConfigValidator.ToMask(DefenderRole.Economy));
        }

        [Test]
        public void ValidationResult_Success_CreatesValidResult()
        {
            // Act
            var result = ValidationResult.Success();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(ValidationFailureReason.None, result.Reason);
            Assert.IsEmpty(result.DetailMessage);
        }

        [Test]
        public void ValidationResult_Failure_CreatesInvalidResultWithDetails()
        {
            // Arrange
            var reason = ValidationFailureReason.InsufficientATP;
            var message = "Not enough ATP";
            var suggestions = new[] { "Wait for generation", "Use cheaper defender" };

            // Act
            var result = ValidationResult.Failure(reason, message, suggestions);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(reason, result.Reason);
            Assert.AreEqual(message, result.DetailMessage);
            Assert.AreEqual(suggestions.Length, result.Suggestions.Count());
        }
    }
}