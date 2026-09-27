using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ImmunWar.Core.Config;

namespace ImmunWar.Battle.Placement.Tests
{
    /// <summary>
    /// Unit tests for the GridSystem component
    /// Validates core grid functionality and requirements compliance
    /// </summary>
    public class GridSystemTests
    {
        private GridConfiguration _testConfig;
        private GridSystem _gridSystem;

        [SetUp]
        public void SetUp()
        {
            // Create a test configuration with known properties
            _testConfig = GridConfiguration.CreateDefault();
            _gridSystem = new GridSystem(_testConfig);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testConfig != null)
            {
                Object.DestroyImmediate(_testConfig);
            }
        }

        [Test]
        public void GridSystem_InitializesCorrectly()
        {
            // Arrange & Act done in SetUp

            // Assert
            Assert.AreEqual(12, _gridSystem.Width);
            Assert.AreEqual(8, _gridSystem.Height);
            Assert.AreEqual(Vector2.one, _gridSystem.CellSize);
            Assert.AreEqual(Vector2.zero, _gridSystem.GridOrigin);
        }

        [Test]
        public void GridSystem_ProvidesMinimumValidPositions()
        {
            // Act
            var validPositions = _gridSystem.GetValidPlacementPositions().ToArray();

            // Assert - Requirements 1.1: THE Placement_Grid SHALL provide at least 40 valid placement positions
            Assert.GreaterOrEqual(validPositions.Length, 40, 
                "Grid must provide at least 40 valid placement positions as per requirement 1.1");
        }

        [Test]
        public void GridSystem_ProvidesMinimumStrategicNodes()
        {
            // Act
            var strategicNodes = _gridSystem.GetStrategicNodes().ToArray();

            // Assert - Requirements 2.1: THE Placement_Grid SHALL include at least 8 Strategic_Nodes per Organ_Map
            Assert.GreaterOrEqual(strategicNodes.Length, 8, 
                "Grid must provide at least 8 strategic nodes as per requirement 2.1");
        }

        [Test]
        public void GridPosition_EqualsWorksCorrectly()
        {
            // Arrange
            var pos1 = new GridPosition(3, 5);
            var pos2 = new GridPosition(3, 5);
            var pos3 = new GridPosition(4, 5);

            // Act & Assert
            Assert.AreEqual(pos1, pos2);
            Assert.AreNotEqual(pos1, pos3);
            Assert.IsTrue(pos1 == pos2);
            Assert.IsTrue(pos1 != pos3);
        }

        [Test]
        public void GridSystem_ValidatesPositionBounds()
        {
            // Arrange
            var validPos = new GridPosition(5, 3);
            var invalidPosX = new GridPosition(-1, 3);
            var invalidPosY = new GridPosition(5, 10);

            // Act & Assert
            Assert.IsTrue(_gridSystem.IsWithinBounds(validPos));
            Assert.IsFalse(_gridSystem.IsWithinBounds(invalidPosX));
            Assert.IsFalse(_gridSystem.IsWithinBounds(invalidPosY));
        }

        [Test]
        public void GridSystem_HandlesOccupancyCorrectly()
        {
            // Arrange
            var position = new GridPosition(5, 3);
            var defenderId = "test-defender-001";

            // Act & Assert - Initially unoccupied
            Assert.IsTrue(_gridSystem.TryOccupyPosition(position, defenderId));
            Assert.AreEqual(defenderId, _gridSystem.GetOccupantId(position));

            // Cannot occupy same position twice
            Assert.IsFalse(_gridSystem.TryOccupyPosition(position, "another-defender"));

            // Can release and re-occupy
            _gridSystem.ReleasePosition(position);
            Assert.IsNull(_gridSystem.GetOccupantId(position));
            Assert.IsTrue(_gridSystem.TryOccupyPosition(position, "new-defender"));
        }

        [Test]
        public void GridSystem_ConvertsBetweenWorldAndGridCoordinates()
        {
            // Arrange
            var gridPos = new GridPosition(3, 2);
            var expectedWorldPos = new Vector2(3.0f, 2.0f); // With cell size 1,1 and origin 0,0

            // Act
            var worldPos = _gridSystem.GridToWorldPosition(gridPos);
            var convertedBack = _gridSystem.WorldToGridPosition(worldPos);

            // Assert
            Assert.AreEqual(expectedWorldPos, worldPos);
            Assert.AreEqual(gridPos, convertedBack);
        }

        [Test]
        public void GridSystem_ReturnsNodesInRange()
        {
            // Arrange
            var centerPos = new GridPosition(5, 4);
            var range = 2;

            // Act
            var nodesInRange = _gridSystem.GetNodesInRange(centerPos, range).ToArray();

            // Assert
            Assert.Greater(nodesInRange.Length, 0);
            
            // Verify all returned nodes are within range
            foreach (var node in nodesInRange)
            {
                int manhattanDistance = Mathf.Abs(node.Position.X - centerPos.X) + 
                                      Mathf.Abs(node.Position.Y - centerPos.Y);
                Assert.LessOrEqual(manhattanDistance, range);
            }
        }

        [Test]
        public void GridSystem_ReturnsStrategicNodeAdvantages()
        {
            // Act
            var strategicNodes = _gridSystem.GetStrategicNodes().ToArray();

            // Assert
            Assert.Greater(strategicNodes.Length, 0, "Should have strategic nodes");
            
            foreach (var node in strategicNodes)
            {
                var advantage = _gridSystem.GetStrategicAdvantage(node.Position);
                
                // Strategic nodes should have some form of advantage
                Assert.IsTrue(
                    advantage.RangeMultiplier > 1.0f || 
                    advantage.DamageBonus > 0.0f || 
                    advantage.PathCoverage > 1 || 
                    advantage.ATPGenerationBonus > 0.0f ||
                    advantage.MultiTargeting,
                    "Strategic nodes must provide tactical advantages"
                );
            }
        }

        [Test]
        public void GridConfiguration_ValidatesCorrectly()
        {
            // Act
            bool isValid = _testConfig.ValidateConfiguration(out var errors);

            // Assert
            Assert.IsTrue(isValid, $"Default configuration should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void GridConfiguration_DetectsInvalidDimensions()
        {
            // Arrange
            _testConfig.Width = 5; // Below minimum of 8
            _testConfig.Height = 4; // Below minimum of 6

            // Act
            bool isValid = _testConfig.ValidateConfiguration(out var errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.Contains("Grid dimensions too small for meaningful gameplay", errors);
        }

        [Test]
        public void GridSystem_TracksValidPositionCount()
        {
            // Act
            var initialCount = _gridSystem.GetValidPositionCount();
            var testPosition = new GridPosition(2, 2);
            
            _gridSystem.TryOccupyPosition(testPosition, "test-defender");
            var afterOccupyCount = _gridSystem.GetValidPositionCount();
            
            _gridSystem.ReleasePosition(testPosition);
            var afterReleaseCount = _gridSystem.GetValidPositionCount();

            // Assert
            Assert.AreEqual(initialCount - 1, afterOccupyCount);
            Assert.AreEqual(initialCount, afterReleaseCount);
        }
    }
}

#if UNITY_EDITOR
namespace ImmunWar.Battle.Placement.Editor
{
    using UnityEditor;

    /// <summary>
    /// Editor utilities for GridSystem development and debugging
    /// </summary>
    public static class GridSystemEditorUtils
    {
        [MenuItem("ImmunWar/Grid System/Create Test Configuration")]
        public static void CreateTestGridConfiguration()
        {
            var config = GridConfiguration.CreateDefault();
            config.SetIdForEditor("test-grid-config");

            var path = "Assets/ImmunWar/Data/Configs/TestGridConfig.asset";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = config;
            EditorUtility.FocusProjectWindow();
            
            Debug.Log($"Created test grid configuration at {path}");
        }

        [MenuItem("ImmunWar/Grid System/Validate All Grid Configurations")]
        public static void ValidateAllGridConfigurations()
        {
            var configs = Resources.FindObjectsOfTypeAll<GridConfiguration>();
            
            foreach (var config in configs)
            {
                if (config.ValidateConfiguration(out var errors))
                {
                    Debug.Log($"✓ Grid configuration '{config.name}' is valid");
                }
                else
                {
                    Debug.LogError($"✗ Grid configuration '{config.name}' has errors:\n- {string.Join("\n- ", errors)}", config);
                }
            }
        }
    }
}
#endif