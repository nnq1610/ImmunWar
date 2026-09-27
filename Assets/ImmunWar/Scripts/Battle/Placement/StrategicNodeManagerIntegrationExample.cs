using System.Collections.Generic;
using UnityEngine;
using ImmunWar.Core.Config;

namespace ImmunWar.Battle.Placement
{
    /// <summary>
    /// Example integration class demonstrating how StrategicNodeManager works with
    /// the existing placement system to provide strategic positioning mechanics
    /// </summary>
    public class StrategicNodeManagerIntegrationExample : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private GridConfiguration _gridConfig;
        
        private GridSystem _gridSystem;
        private StrategicNodeManager _strategicNodeManager;
        private ValidationSystem _validationSystem;

        /// <summary>
        /// Initialize the placement system with strategic node management
        /// </summary>
        void Start()
        {
            if (_gridConfig == null)
            {
                Debug.LogError("Grid configuration is required for strategic node system");
                return;
            }

            InitializeSystems();
            LogStrategicNodeConfiguration();
        }

        /// <summary>
        /// Initialize all placement system components
        /// </summary>
        private void InitializeSystems()
        {
            // Initialize core grid system
            _gridSystem = new GridSystem(_gridConfig);
            
            // Initialize strategic node manager
            _strategicNodeManager = new StrategicNodeManager(_gridSystem);
            
            // Initialize validation system
            _validationSystem = new ValidationSystem(_gridSystem, _gridConfig);
            
            // Subscribe to strategic node events
            _strategicNodeManager.OnAdvantageUpdated += OnStrategicAdvantageUpdated;
            
            Debug.Log($"Placement system initialized with {_strategicNodeManager.GetTotalStrategicNodeCount()} strategic nodes");
        }

        /// <summary>
        /// Demonstrates strategic node advantage calculation
        /// </summary>
        private void LogStrategicNodeConfiguration()
        {
            Debug.Log("=== Strategic Node Configuration ===");
            
            var nodeStats = _strategicNodeManager.GetNodeStatistics();
            foreach (var kvp in nodeStats)
            {
                if (kvp.Value > 0)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value} nodes");
                }
            }
            
            // Log strategic positions and their advantages
            foreach (var position in _strategicNodeManager.GetAllStrategicNodes())
            {
                var advantage = _strategicNodeManager.GetAdvantage(position);
                var description = _strategicNodeManager.GetAdvantageDescription(position);
                var color = _strategicNodeManager.GetNodeHighlightColor(advantage.NodeType);
                
                Debug.Log($"Strategic Node at {position}: {description} (Color: {color})");
            }
        }

        /// <summary>
        /// Example method showing how to calculate placement cost with strategic node multipliers
        /// </summary>
        /// <param name="defenderId">Defender to place</param>
        /// <param name="position">Target position</param>
        /// <param name="baseCost">Base placement cost</param>
        /// <returns>Final cost including strategic node multipliers</returns>
        public int CalculatePlacementCost(string defenderId, GridPosition position, int baseCost)
        {
            var multiplier = _strategicNodeManager.GetPlacementCostMultiplier(position);
            var finalCost = Mathf.RoundToInt(baseCost * multiplier);
            
            if (multiplier > 1.0f)
            {
                Debug.Log($"Strategic placement cost: {baseCost} × {multiplier:F2} = {finalCost}");
            }
            
            return finalCost;
        }

        /// <summary>
        /// Example method showing how to validate placement including strategic node considerations
        /// </summary>
        /// <param name="defenderId">Defender to place</param>
        /// <param name="position">Target position</param>
        /// <param name="defenderRole">Role of the defender</param>
        /// <param name="availableATP">Currently available ATP</param>
        /// <returns>Validation result with strategic node information</returns>
        public string ValidateStrategicPlacement(string defenderId, GridPosition position, 
                                               DefenderRole defenderRole, int availableATP)
        {
            var results = new List<string>();
            
            // Basic validation
            if (!_gridSystem.IsValidPosition(position))
            {
                results.Add("Invalid grid position");
                return string.Join(", ", results);
            }
            
            // Strategic node role validation
            if (_strategicNodeManager.IsStrategicNode(position))
            {
                if (!_strategicNodeManager.IsRolePermitted(position, defenderRole))
                {
                    results.Add($"Role {defenderRole} not permitted at strategic position");
                }
                else
                {
                    var advantage = _strategicNodeManager.GetAdvantage(position);
                    results.Add($"Strategic advantages: {_strategicNodeManager.GetAdvantageDescription(position)}");
                    
                    // Check if defender benefits from strategic advantages
                    if (defenderRole.BenefitsFromStrategicAdvantages())
                    {
                        results.Add($"✓ {defenderRole} will benefit from strategic positioning");
                    }
                }
            }
            
            // ATP cost validation with strategic multiplier
            var baseCost = GetDefenderBaseCost(defenderId);
            var finalCost = CalculatePlacementCost(defenderId, position, baseCost);
            
            if (finalCost > availableATP)
            {
                results.Add($"Insufficient ATP: need {finalCost}, have {availableATP}");
            }
            else
            {
                results.Add($"ATP cost: {finalCost}");
            }
            
            return results.Count > 0 ? string.Join(", ", results) : "Valid placement";
        }

        /// <summary>
        /// Example ATP generation calculation for Energy Cells on strategic nodes
        /// </summary>
        /// <param name="position">Position of Energy Cell</param>
        /// <param name="baseGeneration">Base ATP generation rate</param>
        /// <returns>Modified ATP generation including strategic bonuses</returns>
        public float CalculateATPGeneration(GridPosition position, float baseGeneration)
        {
            var bonusGeneration = _strategicNodeManager.CalculateATPGenerationBonus(position, baseGeneration);
            
            if (bonusGeneration > baseGeneration)
            {
                var bonus = bonusGeneration - baseGeneration;
                Debug.Log($"Energy Cell ATP generation: {baseGeneration} + {bonus:F1} bonus = {bonusGeneration:F1}");
            }
            
            return bonusGeneration;
        }

        /// <summary>
        /// Event handler for strategic node advantage updates
        /// </summary>
        private void OnStrategicAdvantageUpdated(GridPosition position, StrategicNodeManager.StrategicNodeAdvantage advantage)
        {
            Debug.Log($"Strategic advantage updated at {position}: {advantage.Description}");
        }

        /// <summary>
        /// Example method to get base cost for different defender types
        /// This would normally come from defender configuration
        /// </summary>
        private int GetDefenderBaseCost(string defenderId)
        {
            return defenderId.ToLower() switch
            {
                "macrophage" => 100,
                "t-cell" => 150,
                "b-cell" => 120,
                "nk-cell" => 180,
                "energy-cell" => 200,
                "platelet" => 80,
                _ => 100
            };
        }

        /// <summary>
        /// Validation helper for strategic node distribution
        /// </summary>
        [ContextMenu("Validate Strategic Node Distribution")]
        public void ValidateStrategicNodes()
        {
            if (_strategicNodeManager == null)
            {
                Debug.LogError("Strategic node manager not initialized");
                return;
            }
            
            var isValid = _strategicNodeManager.ValidateStrategicNodeDistribution(8, out var errors);
            
            if (isValid)
            {
                Debug.Log("✓ Strategic node distribution is valid");
                
                var stats = _strategicNodeManager.GetNodeStatistics();
                foreach (var kvp in stats)
                {
                    if (kvp.Value > 0)
                    {
                        Debug.Log($"  {kvp.Key}: {kvp.Value} nodes");
                    }
                }
            }
            else
            {
                Debug.LogError("✗ Strategic node distribution validation failed:");
                foreach (var error in errors)
                {
                    Debug.LogError($"  - {error}");
                }
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_strategicNodeManager != null)
            {
                _strategicNodeManager.OnAdvantageUpdated -= OnStrategicAdvantageUpdated;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor visualization of strategic nodes
        /// </summary>
        void OnDrawGizmos()
        {
            if (_strategicNodeManager == null || _gridSystem == null) return;
            
            // Draw strategic nodes with colored indicators
            foreach (var position in _strategicNodeManager.GetAllStrategicNodes())
            {
                var worldPos = _gridSystem.GridToWorldPosition(position);
                var advantage = _strategicNodeManager.GetAdvantage(position);
                
                Gizmos.color = _strategicNodeManager.GetNodeHighlightColor(advantage.NodeType);
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                
                // Draw larger circle for power nodes (ATP generation)
                if (advantage.NodeType == StrategicNodeType.PowerNode)
                {
                    Gizmos.DrawWireSphere(worldPos, 0.6f);
                }
            }
            
            // Draw grid bounds
            if (_gridSystem != null)
            {
                Gizmos.color = Color.white;
                var gridCenter = new Vector3(_gridSystem.Width * 0.5f, _gridSystem.Height * 0.5f, 0);
                var gridSize = new Vector3(_gridSystem.Width, _gridSystem.Height, 0);
                Gizmos.DrawWireCube(gridCenter, gridSize);
            }
        }
#endif
    }
}