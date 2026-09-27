using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ImmunWar.Core.Config;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Tests.EditMode
{
    /// <summary>
    /// Unit tests for StrategicNodeManager component
    /// Tests strategic node advantage calculation, cost multipliers, and validation
    /// </summary>
    [TestFixture]
    public class StrategicNodeManagerTests
    {
        private GridConfiguration _testConfig;
        private GridSystem _gridSystem;
        private StrategicNodeManager _strategicNodeManager;

        [SetUp]
        public void SetUp()
        {
            _testConfig = CreateTestGridConfiguration();
            _gridSystem = new GridSystem(_testConfig);
            _strategicNodeManager = new StrategicNodeManager(_gridSystem);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testConfig != null)
                Object.DestroyImmediate(_testConfig);
        }

        [Test]
        public void StrategicNodeManager_InitializesCorrectly()
        {
            Assert.IsNotNull(_strategicNodeManager);
            Assert.AreEqual(8, _strategicNodeManager.GetTotalStrategicNodeCount());
        }

        [Test]
        public void GetAdvantage_ReturnsCorrectAdvantageForStrategicNodes()
        {
            var highGroundPos = new GridPosition(2, 2);
            var advantage = _strategicNodeManager.GetAdvantage(highGroundPos);
            
            Assert.AreEqual(StrategicNodeType.HighGround, advantage.NodeType);
            Assert.AreEqual(1.5f, advantage.RangeMultiplier, 0.01f);
            Assert.AreEqual(1.25f, advantage.CostMultiplier, 0.01f);
        }

        [Test]
        public void GetAdvantage_ReturnsDefaultForNonStrategicNodes()
        {
            var standardPos = new GridPosition(0, 0);
            var advantage = _strategicNodeManager.GetAdvantage(standardPos);
            
            Assert.AreEqual(0.0f, advantage.RangeMultiplier);
            Assert.AreEqual(0.0f, advantage.CostMultiplier);
        }

        [Test]
        public void GetPlacementCostMultiplier_Returns125PercentForStrategicNodes()
        {
            var strategicPos = new GridPosition(2, 2);
            var multiplier = _strategicNodeManager.GetPlacementCostMultiplier(strategicPos);
            
            Assert.AreEqual(1.25f, multiplier, 0.01f);
        }

        [Test]
        public void GetPlacementCostMultiplier_ReturnsOneForStandardNodes()
        {
            var standardPos = new GridPosition(0, 0);
            var multiplier = _strategicNodeManager.GetPlacementCostMultiplier(standardPos);
            
            Assert.AreEqual(1.0f, multiplier, 0.01f);
        }

        [Test]
        public void IsStrategicNode_IdentifiesStrategicPositionsCorrectly()
        {
            var strategicPos = new GridPosition(2, 2);
            var standardPos = new GridPosition(0, 0);
            
            Assert.IsTrue(_strategicNodeManager.IsStrategicNode(strategicPos));
            Assert.IsFalse(_strategicNodeManager.IsStrategicNode(standardPos));
        }

        [Test]
        public void GetAvailableStrategicNodes_ReturnsAllNodesWhenNoneOccupied()
        {
            var availableNodes = _strategicNodeManager.GetAvailableStrategicNodes().ToList();
            
            Assert.AreEqual(8, availableNodes.Count);
        }

        [Test]
        public void GetNodeHighlightColor_ReturnsDistinctColorsForEachNodeType()
        {
            var highGroundColor = _strategicNodeManager.GetNodeHighlightColor(StrategicNodeType.HighGround);
            var chokepointColor = _strategicNodeManager.GetNodeHighlightColor(StrategicNodeType.Chokepoint);
            var powerNodeColor = _strategicNodeManager.GetNodeHighlightColor(StrategicNodeType.PowerNode);
            var amplifierColor = _strategicNodeManager.GetNodeHighlightColor(StrategicNodeType.AmplifierNode);
            
            Assert.AreNotEqual(highGroundColor, chokepointColor);
            Assert.AreNotEqual(chokepointColor, powerNodeColor);
            Assert.AreNotEqual(powerNodeColor, amplifierColor);
            Assert.AreNotEqual(amplifierColor, highGroundColor);
        }

        [Test]
        public void GetAdvantageDescription_ReturnsCorrectDescriptionForStrategicNodes()
        {
            var highGroundPos = new GridPosition(2, 2);
            var description = _strategicNodeManager.GetAdvantageDescription(highGroundPos);
            
            Assert.IsTrue(description.Contains("Range"));
            Assert.IsTrue(description.Contains("Cost"));
        }

        [Test]
        public void GetAdvantageDescription_ReturnsStandardDescriptionForNormalNodes()
        {
            var standardPos = new GridPosition(0, 0);
            var description = _strategicNodeManager.GetAdvantageDescription(standardPos);
            
            Assert.AreEqual("Standard placement position", description);
        }

        [Test]
        public void CalculateATPGenerationBonus_AppliesBonusForPowerNodes()
        {
            var powerNodePos = new GridPosition(8, 2);
            var baseGeneration = 10.0f;
            var bonusGeneration = _strategicNodeManager.CalculateATPGenerationBonus(powerNodePos, baseGeneration);
            
            // Power nodes should have ATP generation bonus
            Assert.Greater(bonusGeneration, baseGeneration);
        }

        [Test]
        public void GetNodeStatistics_CountsNodesCorrectlyByType()
        {
            var stats = _strategicNodeManager.GetNodeStatistics();
            
            Assert.AreEqual(2, stats[StrategicNodeType.HighGround]);
            Assert.AreEqual(2, stats[StrategicNodeType.Chokepoint]);
            Assert.AreEqual(2, stats[StrategicNodeType.PowerNode]);
            Assert.AreEqual(2, stats[StrategicNodeType.AmplifierNode]);
        }

        [Test]
        public void ValidateStrategicNodeDistribution_PassesWithMinimumRequirements()
        {
            var isValid = _strategicNodeManager.ValidateStrategicNodeDistribution(8, out var errors);
            
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void ValidateStrategicNodeDistribution_FailsWithInsufficientNodes()
        {
            var isValid = _strategicNodeManager.ValidateStrategicNodeDistribution(20, out var errors);
            
            Assert.IsFalse(isValid);
            Assert.Greater(errors.Count, 0);
            Assert.IsTrue(errors.Any(e => e.Contains("Insufficient strategic nodes")));
        }

        [Test]
        public void IsRolePermitted_AllowsAllRolesOnStrategicNodesByDefault()
        {
            var strategicPos = new GridPosition(2, 2);
            
            Assert.IsTrue(_strategicNodeManager.IsRolePermitted(strategicPos, DefenderRole.Blocker));
            Assert.IsTrue(_strategicNodeManager.IsRolePermitted(strategicPos, DefenderRole.Damage));
            Assert.IsTrue(_strategicNodeManager.IsRolePermitted(strategicPos, DefenderRole.Economy));
        }

        [Test]
        public void DefenderRoleExtensions_BenefitsFromStrategicAdvantages_ReturnsCorrectValues()
        {
            Assert.IsTrue(DefenderRole.Damage.BenefitsFromStrategicAdvantages());
            Assert.IsTrue(DefenderRole.Blocker.BenefitsFromStrategicAdvantages());
            Assert.IsTrue(DefenderRole.Economy.BenefitsFromStrategicAdvantages());
            Assert.IsFalse(DefenderRole.Repair.BenefitsFromStrategicAdvantages());
        }

        /// <summary>
        /// Creates a test grid configuration with strategic nodes for testing
        /// </summary>
        private GridConfiguration CreateTestGridConfiguration()
        {
            var config = ScriptableObject.CreateInstance<GridConfiguration>();
            config.Width = 12;
            config.Height = 8;
            config.CellSize = new Vector2(1.0f, 1.0f);
            config.GridOrigin = Vector2.zero;
            config.MinimumValidPositions = 40;
            config.MinimumStrategicNodes = 8;

            // Create strategic nodes matching the default config
            config.StrategicNodes = new StrategicNodeDefinition[]
            {
                new StrategicNodeDefinition(new GridPosition(2, 2), StrategicNodeType.HighGround, 1.25f, "Elevated Position"),
                new StrategicNodeDefinition(new GridPosition(5, 3), StrategicNodeType.Chokepoint, 1.25f, "Path Convergence"),
                new StrategicNodeDefinition(new GridPosition(8, 2), StrategicNodeType.PowerNode, 1.25f, "Energy Source"),
                new StrategicNodeDefinition(new GridPosition(3, 5), StrategicNodeType.AmplifierNode, 1.25f, "Signal Amplifier"),
                new StrategicNodeDefinition(new GridPosition(7, 5), StrategicNodeType.HighGround, 1.25f, "Elevated Position"),
                new StrategicNodeDefinition(new GridPosition(1, 4), StrategicNodeType.Chokepoint, 1.25f, "Defensive Position"),
                new StrategicNodeDefinition(new GridPosition(9, 4), StrategicNodeType.PowerNode, 1.25f, "Power Junction"),
                new StrategicNodeDefinition(new GridPosition(5, 1), StrategicNodeType.AmplifierNode, 1.25f, "Central Hub")
            };

            // Ensure we have exactly 8 strategic nodes as defined
            Assert.AreEqual(8, config.StrategicNodes.Length, "Test configuration should have 8 strategic nodes");
            
            return config;
        }
    }
}