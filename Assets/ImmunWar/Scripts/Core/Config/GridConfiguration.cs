using System;
using System.Linq;
using UnityEngine;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Core.Config
{
    /// <summary>
    /// Defines validation rules for grid placement
    /// </summary>
    [Serializable]
    public struct PlacementRule
    {
        public string RuleName;
        public DefenderRoleMask AffectedRoles;
        public bool RequiresValidation;
        public string Description;

        public PlacementRule(string name, DefenderRoleMask roles, bool validation = true, string desc = "")
        {
            RuleName = name;
            AffectedRoles = roles;
            RequiresValidation = validation;
            Description = desc;
        }
    }

    /// <summary>
    /// Defines standard grid node properties
    /// </summary>
    [Serializable]
    public struct GridNodeDefinition
    {
        public GridPosition Position;
        public NodeType Type;
        public DefenderRoleMask AllowedRoles;
        public PlacementRestriction[] Restrictions;

        public GridNodeDefinition(GridPosition position, NodeType type = NodeType.Standard, 
                                DefenderRoleMask allowedRoles = DefenderRoleMask.All)
        {
            Position = position;
            Type = type;
            AllowedRoles = allowedRoles;
            Restrictions = Array.Empty<PlacementRestriction>();
        }
    }

    /// <summary>
    /// Defines strategic node properties and advantages
    /// </summary>
    [Serializable]
    public struct StrategicNodeDefinition
    {
        public GridPosition Position;
        public StrategicNodeType Type;
        [Range(1.0f, 2.0f)] public float CostMultiplier;
        public StrategicNodeData StrategyData;
        public DefenderRoleMask AllowedRoles;
        public string Description;

        public StrategicNodeDefinition(GridPosition position, StrategicNodeType type, 
                                     float costMultiplier = 1.25f, string description = "")
        {
            Position = position;
            Type = type;
            CostMultiplier = costMultiplier;
            StrategyData = GetDefaultAdvantages(type);
            AllowedRoles = DefenderRoleMask.All;
            Description = description;
        }

        private static StrategicNodeData GetDefaultAdvantages(StrategicNodeType type)
        {
            return type switch
            {
                StrategicNodeType.HighGround => new StrategicNodeData(1.5f, 0.0f, 1, false, 0.0f, "Increased Range"),
                StrategicNodeType.Chokepoint => new StrategicNodeData(1.0f, 0.2f, 2, true, 0.0f, "Multi-Path Coverage"),
                StrategicNodeType.PowerNode => new StrategicNodeData(1.0f, 0.0f, 1, false, 0.5f, "ATP Generation Bonus"),
                StrategicNodeType.AmplifierNode => new StrategicNodeData(1.2f, 0.3f, 1, false, 0.0f, "Damage Amplifier"),
                _ => new StrategicNodeData()
            };
        }
    }

    /// <summary>
    /// Types of strategic nodes with different tactical advantages
    /// </summary>
    public enum StrategicNodeType
    {
        HighGround,      // Increased range and vision
        Chokepoint,      // Multi-path coverage and control
        PowerNode,       // ATP generation bonus
        AmplifierNode    // Damage and effect amplification
    }

    /// <summary>
    /// Defines placement restrictions for specific areas
    /// </summary>
    [Serializable]
    public struct PlacementRestriction
    {
        public RestrictionType Type;
        public DefenderRoleMask AffectedRoles;
        public string Reason;

        public PlacementRestriction(RestrictionType type, DefenderRoleMask roles, string reason)
        {
            Type = type;
            AffectedRoles = roles;
            Reason = reason;
        }
    }

    /// <summary>
    /// Types of placement restrictions
    /// </summary>
    public enum RestrictionType
    {
        None,           // No restrictions
        RoleBlocked,    // Specific roles blocked
        DensityLimit,   // Maximum density restriction
        Environmental,  // Environmental hazard
        Anatomical      // Anatomical constraint
    }

    /// <summary>
    /// Defines restricted zones within the grid
    /// </summary>
    [Serializable]
    public struct RestrictedZoneDefinition
    {
        public GridPosition[] Positions;
        public RestrictionType Type;
        public string Reason;
        public DefenderRoleMask AffectedRoles;

        public RestrictedZoneDefinition(GridPosition[] positions, RestrictionType type, 
                                      string reason, DefenderRoleMask affectedRoles = DefenderRoleMask.All)
        {
            Positions = positions ?? Array.Empty<GridPosition>();
            Type = type;
            Reason = reason;
            AffectedRoles = affectedRoles;
        }
    }

    /// <summary>
    /// ScriptableObject configuration for grid-based placement system
    /// Defines grid dimensions, node types, and placement rules
    /// </summary>
    [CreateAssetMenu(menuName = "Immune War/Grid Configuration", fileName = "GridConfig")]
    public class GridConfiguration : GameConfig
    {
        [Header("Grid Dimensions")]
        [Tooltip("Width of the placement grid (default: 12)")]
        [Range(8, 20)] public int Width = 12;
        
        [Tooltip("Height of the placement grid (default: 8)")]
        [Range(6, 16)] public int Height = 8;
        
        [Tooltip("Size of each grid cell in world units")]
        public Vector2 CellSize = new Vector2(1.0f, 1.0f);
        
        [Tooltip("World position origin of the grid")]
        public Vector2 GridOrigin = Vector2.zero;

        [Header("Node Definitions")]
        [Tooltip("Standard node configurations")]
        public GridNodeDefinition[] StandardNodes = Array.Empty<GridNodeDefinition>();
        
        [Tooltip("Strategic node configurations with special advantages")]
        public StrategicNodeDefinition[] StrategicNodes = Array.Empty<StrategicNodeDefinition>();
        
        [Tooltip("Restricted zones where placement is limited or forbidden")]
        public RestrictedZoneDefinition[] RestrictedZones = Array.Empty<RestrictedZoneDefinition>();

        [Header("Validation Rules")]
        [Tooltip("Placement rules for validation system")]
        public PlacementRule[] PlacementRules = Array.Empty<PlacementRule>();
        
        [Tooltip("Minimum number of valid placement positions required")]
        [Range(30, 100)] public int MinimumValidPositions = 40;
        
        [Tooltip("Minimum number of strategic nodes required")]
        [Range(6, 20)] public int MinimumStrategicNodes = 8;

        [Header("Performance Settings")]
        [Tooltip("Enable spatial partitioning for large grids")]
        public bool UseSpatialPartitioning = true;
        
        [Tooltip("Maximum grid size before performance warnings")]
        public int MaxRecommendedSize = 200;

        /// <summary>
        /// Gets the total grid capacity
        /// </summary>
        public int TotalGridSize => Width * Height;

        /// <summary>
        /// Validates the grid configuration for consistency and requirements
        /// </summary>
        public bool ValidateConfiguration(out string[] errors)
        {
            var errorList = new System.Collections.Generic.List<string>();

            // Check minimum dimensions
            if (Width < 8 || Height < 6)
                errorList.Add("Grid dimensions too small for meaningful gameplay");

            // Check total size
            if (TotalGridSize > MaxRecommendedSize)
                errorList.Add($"Grid size {TotalGridSize} exceeds recommended maximum {MaxRecommendedSize}");

            // Validate strategic node positions
            foreach (var strategicNode in StrategicNodes)
            {
                if (strategicNode.Position.X >= Width || strategicNode.Position.Y >= Height ||
                    strategicNode.Position.X < 0 || strategicNode.Position.Y < 0)
                {
                    errorList.Add($"Strategic node at {strategicNode.Position} is outside grid bounds");
                }
            }

            // Validate standard node positions
            foreach (var standardNode in StandardNodes)
            {
                if (standardNode.Position.X >= Width || standardNode.Position.Y >= Height ||
                    standardNode.Position.X < 0 || standardNode.Position.Y < 0)
                {
                    errorList.Add($"Standard node at {standardNode.Position} is outside grid bounds");
                }
            }

            // Check for position conflicts
            var allPositions = new System.Collections.Generic.HashSet<GridPosition>();
            foreach (var node in StrategicNodes)
            {
                if (!allPositions.Add(node.Position))
                    errorList.Add($"Multiple nodes defined at position {node.Position}");
            }

            // Estimate valid positions (simplified check)
            int estimatedValid = TotalGridSize - RestrictedZones.Sum(zone => zone.Positions?.Length ?? 0);
            if (estimatedValid < MinimumValidPositions)
                errorList.Add($"Estimated valid positions ({estimatedValid}) below minimum requirement ({MinimumValidPositions})");

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        /// <summary>
        /// Creates a default grid configuration suitable for testing
        /// </summary>
        public static GridConfiguration CreateDefault()
        {
            var config = CreateInstance<GridConfiguration>();
            config.Width = 12;
            config.Height = 8;
            config.CellSize = new Vector2(1.0f, 1.0f);
            config.GridOrigin = Vector2.zero;
            config.MinimumValidPositions = 40;
            config.MinimumStrategicNodes = 8;

            // Create some default strategic nodes
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

            // Create basic placement rules
            config.PlacementRules = new PlacementRule[]
            {
                new PlacementRule("Standard Placement", DefenderRoleMask.All, true, "Basic placement validation"),
                new PlacementRule("Economy Density", DefenderRoleMask.Economy, true, "Limit economy cell density"),
                new PlacementRule("Strategic Premium", DefenderRoleMask.All, true, "Strategic node cost multiplier")
            };

            return config;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor validation to ensure configuration is valid when saved
        /// </summary>
        private void OnValidate()
        {
            if (ValidateConfiguration(out var errors))
            {
                // Configuration is valid
                return;
            }

            // Log validation errors in editor
            foreach (var error in errors)
            {
                Debug.LogWarning($"Grid Configuration Validation: {error}", this);
            }
        }

        /// <summary>
        /// Generates a preview of valid positions for the Scene view
        /// </summary>
        public GridPosition[] GetValidPositionsPreview()
        {
            var validPositions = new System.Collections.Generic.List<GridPosition>();
            
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var pos = new GridPosition(x, y);
                    bool isRestricted = false;
                    
                    // Check if position is in any restricted zone
                    foreach (var zone in RestrictedZones)
                    {
                        if (zone.Positions != null && System.Array.Exists(zone.Positions, p => p.Equals(pos)))
                        {
                            isRestricted = true;
                            break;
                        }
                    }
                    
                    if (!isRestricted)
                        validPositions.Add(pos);
                }
            }
            
            return validPositions.ToArray();
        }
#endif
    }
}