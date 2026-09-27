using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ImmunWar.Core.Config;

namespace ImmunWar.Battle.Placement
{
    /// <summary>
    /// Represents a position within the placement grid using integer coordinates
    /// </summary>
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int X;
        public int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !(left == right);
        public override string ToString() => $"({X}, {Y})";

        public Vector2 ToWorldPosition(Vector2 cellSize, Vector2 gridOrigin)
        {
            return gridOrigin + new Vector2(X * cellSize.x, Y * cellSize.y);
        }
    }

    /// <summary>
    /// Represents a single node within the placement grid
    /// </summary>
    [Serializable]
    public struct GridNode
    {
        public GridPosition Position;
        public NodeType Type;
        public bool IsOccupied;
        public string OccupantId;
        public StrategicNodeData StrategyData;
        public DefenderRoleMask AllowedRoles;

        public GridNode(GridPosition position, NodeType type = NodeType.Standard, DefenderRoleMask allowedRoles = DefenderRoleMask.All)
        {
            Position = position;
            Type = type;
            IsOccupied = false;
            OccupantId = null;
            StrategyData = default;
            AllowedRoles = allowedRoles;
        }
    }

    /// <summary>
    /// Types of nodes in the placement grid
    /// </summary>
    public enum NodeType
    {
        Standard,   // Normal placement position
        Strategic,  // High-value position with advantages
        Restricted, // Limited access position
        Blocked     // Impassable position
    }

    /// <summary>
    /// Strategic advantages for strategic nodes
    /// </summary>
    [Serializable]
    public struct StrategicNodeData
    {
        public float RangeMultiplier;
        public float DamageBonus;
        public int PathCoverage;
        public bool MultiTargeting;
        public float ATPGenerationBonus;
        public string Description;

        public StrategicNodeData(float rangeMultiplier = 1.0f, float damageBonus = 0.0f, int pathCoverage = 1, 
                               bool multiTargeting = false, float atpBonus = 0.0f, string description = "")
        {
            RangeMultiplier = rangeMultiplier;
            DamageBonus = damageBonus;
            PathCoverage = pathCoverage;
            MultiTargeting = multiTargeting;
            ATPGenerationBonus = atpBonus;
            Description = description;
        }
    }

    /// <summary>
    /// Core grid system managing the 2D placement grid for ImmunWar
    /// Provides grid generation, validation, and query functionality
    /// </summary>
    public class GridSystem
    {
        private readonly GridConfiguration _config;
        private readonly Dictionary<GridPosition, GridNode> _nodes;
        private readonly HashSet<GridPosition> _validPositions;
        private readonly HashSet<GridPosition> _strategicPositions;

        public GridConfiguration Configuration => _config;
        public int Width => _config.Width;
        public int Height => _config.Height;
        public Vector2 CellSize => _config.CellSize;
        public Vector2 GridOrigin => _config.GridOrigin;

        /// <summary>
        /// Initializes the grid system with the provided configuration
        /// </summary>
        /// <param name="config">Grid configuration defining dimensions and node types</param>
        public GridSystem(GridConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _nodes = new Dictionary<GridPosition, GridNode>();
            _validPositions = new HashSet<GridPosition>();
            _strategicPositions = new HashSet<GridPosition>();
            
            GenerateGrid();
        }

        /// <summary>
        /// Generates the complete grid based on configuration
        /// </summary>
        private void GenerateGrid()
        {
            // Clear existing data
            _nodes.Clear();
            _validPositions.Clear();
            _strategicPositions.Clear();

            // Generate all standard positions first
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var position = new GridPosition(x, y);
                    var node = new GridNode(position, NodeType.Standard);
                    _nodes[position] = node;
                    _validPositions.Add(position);
                }
            }

            // Apply strategic node definitions
            foreach (var strategicDef in _config.StrategicNodes)
            {
                if (IsWithinBounds(strategicDef.Position))
                {
                    var node = _nodes[strategicDef.Position];
                    node.Type = NodeType.Strategic;
                    node.StrategyData = strategicDef.StrategyData;
                    node.AllowedRoles = strategicDef.AllowedRoles;
                    _nodes[strategicDef.Position] = node;
                    _strategicPositions.Add(strategicDef.Position);
                }
            }

            // Apply standard node definitions (overrides)
            foreach (var nodeDef in _config.StandardNodes)
            {
                if (IsWithinBounds(nodeDef.Position))
                {
                    var node = _nodes[nodeDef.Position];
                    node.Type = nodeDef.Type;
                    node.AllowedRoles = nodeDef.AllowedRoles;
                    _nodes[nodeDef.Position] = node;
                    
                    // Update valid positions based on node type
                    if (nodeDef.Type == NodeType.Blocked || nodeDef.Type == NodeType.Restricted)
                    {
                        _validPositions.Remove(nodeDef.Position);
                    }
                }
            }

            // Apply restricted zones
            foreach (var restrictedZone in _config.RestrictedZones)
            {
                foreach (var position in restrictedZone.Positions)
                {
                    if (IsWithinBounds(position) && _nodes.ContainsKey(position))
                    {
                        var node = _nodes[position];
                        node.Type = NodeType.Restricted;
                        _nodes[position] = node;
                        _validPositions.Remove(position);
                    }
                }
            }

            // Ensure minimum valid positions requirement
            if (_validPositions.Count < _config.MinimumValidPositions)
            {
                Debug.LogWarning($"Grid configuration only provides {_validPositions.Count} valid positions, " +
                               $"minimum required: {_config.MinimumValidPositions}");
            }

            // Ensure minimum strategic nodes requirement
            if (_strategicPositions.Count < _config.MinimumStrategicNodes)
            {
                Debug.LogWarning($"Grid configuration only provides {_strategicPositions.Count} strategic nodes, " +
                               $"minimum required: {_config.MinimumStrategicNodes}");
            }
        }

        /// <summary>
        /// Validates if a grid position is within the grid bounds
        /// </summary>
        public bool IsValidPosition(GridPosition position)
        {
            return IsWithinBounds(position) && _validPositions.Contains(position);
        }

        /// <summary>
        /// Checks if a position is within grid boundaries (regardless of validity)
        /// </summary>
        public bool IsWithinBounds(GridPosition position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
        }

        /// <summary>
        /// Gets the grid node at the specified position
        /// </summary>
        public GridNode GetNode(GridPosition position)
        {
            return _nodes.TryGetValue(position, out var node) ? node : default;
        }

        /// <summary>
        /// Attempts to get the grid node at the specified position
        /// </summary>
        public bool TryGetNode(GridPosition position, out GridNode node)
        {
            return _nodes.TryGetValue(position, out node);
        }

        /// <summary>
        /// Gets all nodes within a specified range of a center position
        /// </summary>
        public IEnumerable<GridNode> GetNodesInRange(GridPosition center, int range)
        {
            for (int x = center.X - range; x <= center.X + range; x++)
            {
                for (int y = center.Y - range; y <= center.Y + range; y++)
                {
                    var position = new GridPosition(x, y);
                    if (_nodes.TryGetValue(position, out var node))
                    {
                        // Calculate Manhattan distance
                        int distance = Math.Abs(x - center.X) + Math.Abs(y - center.Y);
                        if (distance <= range)
                        {
                            yield return node;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets all valid placement positions
        /// </summary>
        public IEnumerable<GridPosition> GetValidPlacementPositions()
        {
            return _validPositions.Where(pos => !_nodes[pos].IsOccupied);
        }

        /// <summary>
        /// Gets all strategic nodes
        /// </summary>
        public IEnumerable<GridNode> GetStrategicNodes()
        {
            return _strategicPositions.Select(pos => _nodes[pos]);
        }

        /// <summary>
        /// Gets strategic advantages for a given position
        /// </summary>
        public StrategicNodeData GetStrategicAdvantage(GridPosition position)
        {
            if (_strategicPositions.Contains(position) && _nodes.TryGetValue(position, out var node))
            {
                return node.StrategyData;
            }
            return default;
        }

        /// <summary>
        /// Attempts to occupy a position with the specified defender
        /// </summary>
        public bool TryOccupyPosition(GridPosition position, string defenderId)
        {
            if (!IsValidPosition(position) || !_nodes.TryGetValue(position, out var node) || node.IsOccupied)
            {
                return false;
            }

            node.IsOccupied = true;
            node.OccupantId = defenderId;
            _nodes[position] = node;
            return true;
        }

        /// <summary>
        /// Releases a previously occupied position
        /// </summary>
        public void ReleasePosition(GridPosition position)
        {
            if (_nodes.TryGetValue(position, out var node) && node.IsOccupied)
            {
                node.IsOccupied = false;
                node.OccupantId = null;
                _nodes[position] = node;
            }
        }

        /// <summary>
        /// Gets the occupant ID for a given position
        /// </summary>
        public string GetOccupantId(GridPosition position)
        {
            return _nodes.TryGetValue(position, out var node) ? node.OccupantId : null;
        }

        /// <summary>
        /// Converts world position to grid position
        /// </summary>
        public GridPosition WorldToGridPosition(Vector2 worldPosition)
        {
            var localPos = worldPosition - GridOrigin;
            int x = Mathf.RoundToInt(localPos.x / CellSize.x);
            int y = Mathf.RoundToInt(localPos.y / CellSize.y);
            return new GridPosition(x, y);
        }

        /// <summary>
        /// Converts grid position to world position
        /// </summary>
        public Vector2 GridToWorldPosition(GridPosition gridPosition)
        {
            return gridPosition.ToWorldPosition(CellSize, GridOrigin);
        }

        /// <summary>
        /// Gets total count of valid placement positions
        /// </summary>
        public int GetValidPositionCount()
        {
            return _validPositions.Count(pos => !_nodes[pos].IsOccupied);
        }

        /// <summary>
        /// Gets total count of strategic nodes
        /// </summary>
        public int GetStrategicNodeCount()
        {
            return _strategicPositions.Count;
        }
    }
}