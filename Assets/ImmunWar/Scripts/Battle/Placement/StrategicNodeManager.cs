using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ImmunWar.Core.Config;

namespace ImmunWar.Battle.Placement
{
    /// <summary>
    /// Manages strategic node functionality including advantage calculations, 
    /// cost multipliers, and tactical benefits for the placement system
    /// </summary>
    public class StrategicNodeManager
    {
        private readonly GridSystem _gridSystem;
        private readonly Dictionary<GridPosition, StrategicNodeAdvantage> _nodeAdvantages;
        private readonly Dictionary<StrategicNodeType, Color> _nodeHighlightColors;

        /// <summary>
        /// Event fired when strategic node advantages are updated
        /// </summary>
        public event Action<GridPosition, StrategicNodeAdvantage> OnAdvantageUpdated;

        /// <summary>
        /// Initializes the strategic node manager with grid system reference
        /// </summary>
        /// <param name="gridSystem">The grid system to manage strategic nodes for</param>
        public StrategicNodeManager(GridSystem gridSystem)
        {
            _gridSystem = gridSystem ?? throw new ArgumentNullException(nameof(gridSystem));
            _nodeAdvantages = new Dictionary<GridPosition, StrategicNodeAdvantage>();
            _nodeHighlightColors = InitializeHighlightColors();
            
            InitializeStrategicNodes();
        }

        /// <summary>
        /// Enhanced strategic node advantage structure with additional tactical benefits
        /// </summary>
        [Serializable]
        public struct StrategicNodeAdvantage
        {
            public float RangeMultiplier;
            public float DamageBonus;
            public int PathCoverage;
            public bool MultiTargeting;
            public float ATPGenerationBonus;
            public float CostMultiplier;
            public StrategicNodeType NodeType;
            public string Description;

            public StrategicNodeAdvantage(float rangeMultiplier = 1.0f, float damageBonus = 0.0f, 
                                        int pathCoverage = 1, bool multiTargeting = false, 
                                        float atpGenerationBonus = 0.0f, float costMultiplier = 1.25f,
                                        StrategicNodeType nodeType = StrategicNodeType.HighGround,
                                        string description = "")
            {
                RangeMultiplier = rangeMultiplier;
                DamageBonus = damageBonus;
                PathCoverage = pathCoverage;
                MultiTargeting = multiTargeting;
                ATPGenerationBonus = atpGenerationBonus;
                CostMultiplier = costMultiplier;
                NodeType = nodeType;
                Description = description;
            }
        }

        /// <summary>
        /// Gets the strategic advantage for a specific grid position
        /// </summary>
        /// <param name="position">Grid position to check</param>
        /// <returns>Strategic node advantage data, or default if not a strategic position</returns>
        public StrategicNodeAdvantage GetAdvantage(GridPosition position)
        {
            if (_nodeAdvantages.TryGetValue(position, out var advantage))
            {
                return advantage;
            }

            // Check if position has strategic node data in grid system
            var strategicData = _gridSystem.GetStrategicAdvantage(position);
            if (strategicData.RangeMultiplier > 0 || strategicData.DamageBonus > 0 || strategicData.ATPGenerationBonus > 0)
            {
                // Convert grid system data to manager advantage
                var node = _gridSystem.GetNode(position);
                var nodeType = GetNodeTypeFromData(strategicData);
                
                var advantage_converted = new StrategicNodeAdvantage(
                    strategicData.RangeMultiplier,
                    strategicData.DamageBonus,
                    strategicData.PathCoverage,
                    strategicData.MultiTargeting,
                    strategicData.ATPGenerationBonus,
                    GetCostMultiplierForType(nodeType),
                    nodeType,
                    strategicData.Description
                );
                
                // Cache for future lookups
                _nodeAdvantages[position] = advantage_converted;
                return advantage_converted;
            }

            return default;
        }

        /// <summary>
        /// Gets all available strategic node positions that are not currently occupied
        /// </summary>
        /// <returns>Enumerable of available strategic node positions</returns>
        public IEnumerable<GridPosition> GetAvailableStrategicNodes()
        {
            return _gridSystem.GetStrategicNodes()
                              .Where(node => !node.IsOccupied)
                              .Select(node => node.Position);
        }

        /// <summary>
        /// Gets all strategic nodes regardless of occupation status
        /// </summary>
        /// <returns>Enumerable of all strategic node positions</returns>
        public IEnumerable<GridPosition> GetAllStrategicNodes()
        {
            return _gridSystem.GetStrategicNodes().Select(node => node.Position);
        }

        /// <summary>
        /// Calculates the placement cost multiplier for a specific position
        /// </summary>
        /// <param name="position">Grid position to check</param>
        /// <returns>Cost multiplier (1.0 for standard positions, higher for strategic nodes)</returns>
        public float GetPlacementCostMultiplier(GridPosition position)
        {
            var advantage = GetAdvantage(position);
            return advantage.CostMultiplier > 0 ? advantage.CostMultiplier : 1.0f;
        }

        /// <summary>
        /// Checks if a position is a strategic node
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if the position is a strategic node</returns>
        public bool IsStrategicNode(GridPosition position)
        {
            var node = _gridSystem.GetNode(position);
            return node.Type == NodeType.Strategic;
        }

        /// <summary>
        /// Gets a human-readable description of the advantages at a position
        /// </summary>
        /// <param name="position">Position to describe</param>
        /// <returns>Formatted description string</returns>
        public string GetAdvantageDescription(GridPosition position)
        {
            var advantage = GetAdvantage(position);
            
            if (advantage.CostMultiplier <= 1.0f)
            {
                return "Standard placement position";
            }

            var benefits = new List<string>();
            
            if (advantage.RangeMultiplier > 1.0f)
                benefits.Add($"+{(advantage.RangeMultiplier - 1.0f):P0} Range");
                
            if (advantage.DamageBonus > 0.0f)
                benefits.Add($"+{advantage.DamageBonus:P0} Damage");
                
            if (advantage.PathCoverage > 1)
                benefits.Add($"Covers {advantage.PathCoverage} Paths");
                
            if (advantage.MultiTargeting)
                benefits.Add("Multi-Target");
                
            if (advantage.ATPGenerationBonus > 0.0f)
                benefits.Add($"+{advantage.ATPGenerationBonus:P0} ATP Generation");

            var description = !string.IsNullOrEmpty(advantage.Description) 
                ? advantage.Description 
                : GetDefaultDescription(advantage.NodeType);
                
            if (benefits.Count > 0)
            {
                return $"{description}: {string.Join(", ", benefits)} (Cost: +{(advantage.CostMultiplier - 1.0f):P0})";
            }
            
            return description;
        }

        /// <summary>
        /// Gets the highlight color for a specific strategic node type
        /// </summary>
        /// <param name="nodeType">Type of strategic node</param>
        /// <returns>Color to use for highlighting this node type</returns>
        public Color GetNodeHighlightColor(StrategicNodeType nodeType)
        {
            return _nodeHighlightColors.TryGetValue(nodeType, out var color) ? color : Color.white;
        }

        /// <summary>
        /// Gets the highlight color for a specific position
        /// </summary>
        /// <param name="position">Grid position</param>
        /// <returns>Highlight color for the position</returns>
        public Color GetPositionHighlightColor(GridPosition position)
        {
            var advantage = GetAdvantage(position);
            return advantage.CostMultiplier > 1.0f ? GetNodeHighlightColor(advantage.NodeType) : Color.white;
        }

        /// <summary>
        /// Calculates ATP generation bonus for Energy Cell defenders on strategic nodes
        /// </summary>
        /// <param name="position">Position where Energy Cell is placed</param>
        /// <param name="baseGeneration">Base ATP generation rate</param>
        /// <returns>Modified ATP generation rate including strategic bonuses</returns>
        public float CalculateATPGenerationBonus(GridPosition position, float baseGeneration)
        {
            var advantage = GetAdvantage(position);
            return baseGeneration * (1.0f + advantage.ATPGenerationBonus);
        }

        /// <summary>
        /// Updates strategic node advantages (useful for dynamic strategic nodes)
        /// </summary>
        /// <param name="position">Position to update</param>
        /// <param name="newAdvantage">New advantage data</param>
        public void UpdateAdvantage(GridPosition position, StrategicNodeAdvantage newAdvantage)
        {
            _nodeAdvantages[position] = newAdvantage;
            OnAdvantageUpdated?.Invoke(position, newAdvantage);
        }

        /// <summary>
        /// Gets strategic node statistics for the current grid
        /// </summary>
        /// <returns>Dictionary containing node counts by type</returns>
        public Dictionary<StrategicNodeType, int> GetNodeStatistics()
        {
            var stats = new Dictionary<StrategicNodeType, int>();
            
            // Initialize all types to 0
            foreach (StrategicNodeType nodeType in Enum.GetValues(typeof(StrategicNodeType)))
            {
                stats[nodeType] = 0;
            }
            
            // Count actual nodes
            foreach (var node in _gridSystem.GetStrategicNodes())
            {
                var advantage = GetAdvantage(node.Position);
                stats[advantage.NodeType]++;
            }
            
            return stats;
        }

        /// <summary>
        /// Validates that the grid meets strategic node requirements
        /// </summary>
        /// <param name="minimumNodes">Minimum required strategic nodes</param>
        /// <param name="errors">Output list of validation errors</param>
        /// <returns>True if validation passes</returns>
        public bool ValidateStrategicNodeDistribution(int minimumNodes, out List<string> errors)
        {
            errors = new List<string>();
            
            var totalNodes = _gridSystem.GetStrategicNodeCount();
            if (totalNodes < minimumNodes)
            {
                errors.Add($"Insufficient strategic nodes: {totalNodes} found, {minimumNodes} required");
            }
            
            var stats = GetNodeStatistics();
            
            // Ensure we have at least one of each critical type
            if (stats[StrategicNodeType.Chokepoint] == 0)
            {
                errors.Add("No chokepoint strategic nodes found - players need path control options");
            }
            
            if (stats[StrategicNodeType.PowerNode] == 0 && stats[StrategicNodeType.AmplifierNode] == 0)
            {
                errors.Add("No power or amplifier nodes found - players need economy/damage enhancement options");
            }
            
            return errors.Count == 0;
        }

        /// <summary>
        /// Initializes strategic node advantages from grid system configuration
        /// </summary>
        private void InitializeStrategicNodes()
        {
            foreach (var node in _gridSystem.GetStrategicNodes())
            {
                var strategicData = _gridSystem.GetStrategicAdvantage(node.Position);
                var nodeType = GetNodeTypeFromData(strategicData);
                
                var advantage = new StrategicNodeAdvantage(
                    strategicData.RangeMultiplier,
                    strategicData.DamageBonus,
                    strategicData.PathCoverage,
                    strategicData.MultiTargeting,
                    strategicData.ATPGenerationBonus,
                    GetCostMultiplierForType(nodeType),
                    nodeType,
                    strategicData.Description
                );
                
                _nodeAdvantages[node.Position] = advantage;
            }
        }

        /// <summary>
        /// Determines strategic node type from strategic node data
        /// </summary>
        private StrategicNodeType GetNodeTypeFromData(StrategicNodeData data)
        {
            // Prioritize based on primary characteristics
            if (data.ATPGenerationBonus > 0.0f)
                return StrategicNodeType.PowerNode;
            if (data.PathCoverage > 1 || data.MultiTargeting)
                return StrategicNodeType.Chokepoint;
            if (data.RangeMultiplier > 1.2f)
                return StrategicNodeType.HighGround;
            if (data.DamageBonus > 0.0f)
                return StrategicNodeType.AmplifierNode;
                
            return StrategicNodeType.HighGround; // Default
        }

        /// <summary>
        /// Gets the cost multiplier for a specific strategic node type
        /// </summary>
        private float GetCostMultiplierForType(StrategicNodeType nodeType)
        {
            return nodeType switch
            {
                StrategicNodeType.HighGround => 1.25f,
                StrategicNodeType.Chokepoint => 1.25f,
                StrategicNodeType.PowerNode => 1.25f,
                StrategicNodeType.AmplifierNode => 1.25f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Gets default description for strategic node types
        /// </summary>
        private string GetDefaultDescription(StrategicNodeType nodeType)
        {
            return nodeType switch
            {
                StrategicNodeType.HighGround => "Elevated Position",
                StrategicNodeType.Chokepoint => "Path Convergence",
                StrategicNodeType.PowerNode => "Energy Source",
                StrategicNodeType.AmplifierNode => "Signal Amplifier",
                _ => "Strategic Position"
            };
        }

        /// <summary>
        /// Initializes highlight colors for different strategic node types
        /// </summary>
        private Dictionary<StrategicNodeType, Color> InitializeHighlightColors()
        {
            return new Dictionary<StrategicNodeType, Color>
            {
                { StrategicNodeType.HighGround, new Color(0.2f, 0.8f, 0.2f, 0.8f) },      // Green - Range advantage
                { StrategicNodeType.Chokepoint, new Color(0.8f, 0.2f, 0.2f, 0.8f) },     // Red - Critical position
                { StrategicNodeType.PowerNode, new Color(0.2f, 0.2f, 0.8f, 0.8f) },      // Blue - ATP generation
                { StrategicNodeType.AmplifierNode, new Color(0.8f, 0.5f, 0.2f, 0.8f) }   // Orange - Damage boost
            };
        }

        /// <summary>
        /// Gets the total number of strategic nodes in the grid
        /// </summary>
        public int GetTotalStrategicNodeCount()
        {
            return _gridSystem.GetStrategicNodeCount();
        }

        /// <summary>
        /// Gets the number of available (unoccupied) strategic nodes
        /// </summary>
        public int GetAvailableStrategicNodeCount()
        {
            return GetAvailableStrategicNodes().Count();
        }

        /// <summary>
        /// Checks if a defender role is permitted at a strategic position
        /// </summary>
        /// <param name="position">Strategic node position</param>
        /// <param name="defenderRole">Defender role to check</param>
        /// <returns>True if the role is permitted at this position</returns>
        public bool IsRolePermitted(GridPosition position, DefenderRole defenderRole)
        {
            var node = _gridSystem.GetNode(position);
            if (node.Type != NodeType.Strategic)
                return true; // Non-strategic nodes allow all roles by default
                
            var roleMask = ConfigValidator.ToMask(defenderRole);
            return (node.AllowedRoles & roleMask) != DefenderRoleMask.None;
        }

        /// <summary>
        /// Gets all strategic positions that allow a specific defender role
        /// </summary>
        /// <param name="defenderRole">Defender role to check</param>
        /// <returns>Enumerable of positions that allow the specified role</returns>
        public IEnumerable<GridPosition> GetStrategicPositionsForRole(DefenderRole defenderRole)
        {
            var roleMask = ConfigValidator.ToMask(defenderRole);
            return _gridSystem.GetStrategicNodes()
                              .Where(node => (node.AllowedRoles & roleMask) != DefenderRoleMask.None)
                              .Select(node => node.Position);
        }
    }

    /// <summary>
    /// Extension methods for DefenderRole enum to work with strategic nodes
    /// </summary>
    public static class DefenderRoleExtensions
    {
        /// <summary>
        /// Checks if a defender role benefits from strategic node advantages
        /// </summary>
        public static bool BenefitsFromStrategicAdvantages(this DefenderRole role)
        {
            return role switch
            {
                DefenderRole.Damage => true,  // Benefits from damage bonuses and range
                DefenderRole.Blocker => true, // Benefits from range and multi-targeting
                DefenderRole.Support => true, // Benefits from range and coverage
                DefenderRole.Burst => true,   // Benefits from damage bonuses
                DefenderRole.Economy => true, // Benefits from ATP generation bonuses
                DefenderRole.Repair => false, // Limited benefit from strategic positions
                _ => false
            };
        }
    }
}