using System.Linq;
using UnityEngine;
using ImmunWar.Core.Config;
using ImmunWar.UI;

namespace ImmunWar.Battle.Placement
{
    /// <summary>
    /// Integration demonstration for ValidationSystem and PlacementController
    /// Shows how the systems work together to provide placement validation and feedback
    /// Requirements: 1.3, 1.5, 7.1, 7.2
    /// </summary>
    public class PlacementIntegrationDemo : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private GridConfiguration _gridConfig;
        [SerializeField] private PlacementController _placementController;
        
        [Header("Demo Controls")]
        [SerializeField] private bool _runDemo = false;
        [SerializeField] private string _testDefenderId = "macrophage";
        [SerializeField] private DefenderRole _testDefenderRole = DefenderRole.Blocker;

        private GridSystem _gridSystem;
        private ValidationSystem _validationSystem;
        private bool _demoInitialized = false;

        private void Start()
        {
            InitializeSystems();
        }

        private void Update()
        {
            if (_runDemo && !_demoInitialized)
            {
                RunIntegrationDemo();
                _demoInitialized = true;
                _runDemo = false;
            }
        }

        /// <summary>
        /// Initialize the placement systems for demonstration
        /// </summary>
        private void InitializeSystems()
        {
            try
            {
                // Create default configuration if none provided
                if (_gridConfig == null)
                {
                    _gridConfig = GridConfiguration.CreateDefault();
                    Debug.Log("Created default grid configuration for demo");
                }

                // Initialize core systems
                _gridSystem = new GridSystem(_gridConfig);
                _validationSystem = new ValidationSystem(_gridSystem, _gridConfig);

                // Initialize placement controller if available
                if (_placementController != null)
                {
                    _placementController.Initialize(null, _gridSystem, _validationSystem);
                    Debug.Log("Placement controller initialized with grid and validation systems");
                }

                Debug.Log($"Demo systems initialized: Grid({_gridSystem.GetValidPositionCount()} positions), " +
                         $"Strategic Nodes({_gridSystem.GetStrategicNodeCount()})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize placement systems: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates the integration between ValidationSystem and PlacementController
        /// </summary>
        [ContextMenu("Run Integration Demo")]
        public void RunIntegrationDemo()
        {
            if (_gridSystem == null || _validationSystem == null)
            {
                Debug.LogError("Systems not initialized. Cannot run demo.");
                return;
            }

            Debug.Log("=== Placement System Integration Demo ===");

            // Test 1: Grid System Validation
            DemoGridSystemValidation();

            // Test 2: Validation System Rules
            DemoValidationSystemRules();

            // Test 3: PlacementController Integration
            DemoPlacementControllerIntegration();

            Debug.Log("=== Demo Complete ===");
        }

        /// <summary>
        /// Demonstrates grid system functionality
        /// </summary>
        private void DemoGridSystemValidation()
        {
            Debug.Log("--- Grid System Validation Demo ---");

            var validPositions = _gridSystem.GetValidPlacementPositions();
            var strategicNodes = _gridSystem.GetStrategicNodes();

            Debug.Log($"Grid provides {validPositions.Count()} valid positions (requirement: ≥40)");
            Debug.Log($"Grid provides {strategicNodes.Count()} strategic nodes (requirement: ≥8)");

            // Test a few specific positions
            var testPositions = new[]
            {
                new GridPosition(5, 3), // Should be valid
                new GridPosition(-1, 5), // Should be invalid (out of bounds)
                new GridPosition(2, 2)   // Should be valid
            };

            foreach (var pos in testPositions)
            {
                var isValid = _gridSystem.IsValidPosition(pos);
                var inBounds = _gridSystem.IsWithinBounds(pos);
                Debug.Log($"Position {pos}: Valid={isValid}, InBounds={inBounds}");
            }
        }

        /// <summary>
        /// Demonstrates validation system rule enforcement
        /// </summary>
        private void DemoValidationSystemRules()
        {
            Debug.Log("--- Validation System Rules Demo ---");

            // Test different validation scenarios
            var scenarios = new[]
            {
                new PlacementRequest(new GridPosition(5, 3), "macrophage", DefenderRole.Blocker, 100, 200),
                new PlacementRequest(new GridPosition(-1, 3), "tcell", DefenderRole.Damage, 100, 200), // Out of bounds
                new PlacementRequest(new GridPosition(3, 3), "energy", DefenderRole.Economy, 200, 150), // Insufficient ATP
                new PlacementRequest(new GridPosition(2, 2), "bcell", DefenderRole.Support, 125, 500)   // Should be valid
            };

            foreach (var request in scenarios)
            {
                var result = _validationSystem.ValidatePlacement(request);
                Debug.Log($"Validation for {request.DefenderId} at {request.Position}: " +
                         $"Valid={result.IsValid}, Reason={result.Reason}, Message={result.DetailMessage}");

                if (result.Suggestions.Any())
                {
                    Debug.Log($"  Suggestions: {string.Join(", ", result.Suggestions)}");
                }
            }
        }

        /// <summary>
        /// Demonstrates placement controller integration
        /// </summary>
        private void DemoPlacementControllerIntegration()
        {
            if (_placementController == null)
            {
                Debug.LogWarning("--- PlacementController not available for demo ---");
                return;
            }

            Debug.Log("--- PlacementController Integration Demo ---");

            // Test defender selection
            _placementController.SelectDefender(_testDefenderId, _testDefenderRole);
            var state = _placementController.GetCurrentState();
            Debug.Log($"Selected defender: {state.SelectedDefenderId}, Active={state.IsActive}");

            // Test preview updates at different positions
            var previewPositions = new[]
            {
                new GridPosition(4, 4),
                new GridPosition(0, 0),
                new GridPosition(7, 3)
            };

            foreach (var pos in previewPositions)
            {
                _placementController.UpdatePreview(pos);
                var updatedState = _placementController.GetCurrentState();
                
                Debug.Log($"Preview at {pos}: PreviewActive={updatedState.PreviewActive}, " +
                         $"Valid={updatedState.CurrentValidation.IsValid}");
            }

            // Test cancellation
            _placementController.CancelSelection();
            var finalState = _placementController.GetCurrentState();
            Debug.Log($"After cancellation: Active={finalState.IsActive}, Preview={finalState.PreviewActive}");
        }

        /// <summary>
        /// Demonstrates density limitation for Energy Cells
        /// Requirements: 6.4
        /// </summary>
        [ContextMenu("Demo Energy Cell Density Limits")]
        public void DemoEnergyCellDensityLimits()
        {
            if (_validationSystem == null)
            {
                Debug.LogError("Validation system not initialized");
                return;
            }

            Debug.Log("--- Energy Cell Density Limitation Demo ---");

            var centerPos = new GridPosition(5, 4);
            
            // First energy cell - should be allowed
            var firstRequest = new PlacementRequest(centerPos, "energy", DefenderRole.Economy, 200, 500);
            var firstResult = _validationSystem.ValidatePlacement(firstRequest);
            Debug.Log($"First energy cell at {centerPos}: Valid={firstResult.IsValid}");

            if (firstResult.IsValid)
            {
                // Simulate placement
                _gridSystem.TryOccupyPosition(centerPos, "energy-cell-1");

                // Second energy cell nearby - should be blocked
                var nearbyPos = new GridPosition(4, 4); // Within 3x3 area
                var secondRequest = new PlacementRequest(nearbyPos, "energy", DefenderRole.Economy, 200, 500);
                var secondResult = _validationSystem.ValidatePlacement(secondRequest);
                Debug.Log($"Second energy cell at {nearbyPos}: Valid={secondResult.IsValid}, Reason={secondResult.Reason}");

                // Third energy cell far away - should be allowed
                var farPos = new GridPosition(8, 7);
                var thirdRequest = new PlacementRequest(farPos, "energy", DefenderRole.Economy, 200, 500);
                var thirdResult = _validationSystem.ValidatePlacement(thirdRequest);
                Debug.Log($"Third energy cell at {farPos}: Valid={thirdResult.IsValid}");

                // Cleanup
                _gridSystem.ReleasePosition(centerPos);
            }
        }

        /// <summary>
        /// Creates test configuration if needed
        /// </summary>
        [ContextMenu("Create Test Configuration")]
        public void CreateTestConfiguration()
        {
            _gridConfig = GridConfiguration.CreateDefault();
            Debug.Log("Created test grid configuration");
        }

        /// <summary>
        /// Validates current grid configuration
        /// </summary>
        [ContextMenu("Validate Grid Configuration")]
        public void ValidateGridConfiguration()
        {
            if (_gridConfig == null)
            {
                Debug.LogError("No grid configuration to validate");
                return;
            }

            var isValid = _gridConfig.ValidateConfiguration(out var errors);
            
            if (isValid)
            {
                Debug.Log("Grid configuration is valid");
            }
            else
            {
                Debug.LogError($"Grid configuration has errors: {string.Join(", ", errors)}");
            }
        }

        /// <summary>
        /// Shows detailed grid statistics
        /// </summary>
        [ContextMenu("Show Grid Statistics")]
        public void ShowGridStatistics()
        {
            if (_gridSystem == null)
            {
                Debug.LogWarning("Grid system not initialized");
                return;
            }

            var validCount = _gridSystem.GetValidPositionCount();
            var strategicCount = _gridSystem.GetStrategicNodeCount();
            var totalCells = _gridConfig.Width * _gridConfig.Height;

            Debug.Log($"Grid Statistics:");
            Debug.Log($"  Dimensions: {_gridConfig.Width}x{_gridConfig.Height} ({totalCells} total cells)");
            Debug.Log($"  Valid positions: {validCount} (requirement: ≥40)");
            Debug.Log($"  Strategic nodes: {strategicCount} (requirement: ≥8)");
            Debug.Log($"  Cell size: {_gridConfig.CellSize}");
            Debug.Log($"  Origin: {_gridConfig.GridOrigin}");
        }
    }
}