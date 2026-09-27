using System.Linq;
using UnityEngine;
using ImmunWar.Core.Config;

namespace ImmunWar.Battle.Placement
{
    /// <summary>
    /// Runtime validation component for GridSystem requirements
    /// Verifies that the implementation meets specification requirements
    /// </summary>
    public class GridSystemValidation : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private GridConfiguration _testConfig;
        
        [Header("Validation Results")]
        [SerializeField] private bool _requirementsMet;
        [SerializeField] private string[] _validationMessages;

        /// <summary>
        /// Validates all grid system requirements
        /// </summary>
        [ContextMenu("Validate Grid System Requirements")]
        public void ValidateRequirements()
        {
            var messages = new System.Collections.Generic.List<string>();
            bool allPassed = true;

            // Create test configuration if none provided
            if (_testConfig == null)
            {
                _testConfig = GridConfiguration.CreateDefault();
                messages.Add("Using default test configuration");
            }

            // Initialize grid system
            var gridSystem = new GridSystem(_testConfig);

            // Requirement 1.1: Grid provides at least 40 valid placement positions
            var validPositions = gridSystem.GetValidPlacementPositions().Count();
            if (validPositions >= 40)
            {
                messages.Add($"✓ Requirement 1.1: Grid provides {validPositions} valid positions (≥40 required)");
            }
            else
            {
                messages.Add($"✗ Requirement 1.1: Grid only provides {validPositions} valid positions (<40 required)");
                allPassed = false;
            }

            // Requirement 1.4: Grid supports different cell types
            var strategicNodes = gridSystem.GetStrategicNodes().Count();
            if (strategicNodes >= 8)
            {
                messages.Add($"✓ Requirement 1.4: Grid provides {strategicNodes} strategic nodes (≥8 required)");
            }
            else
            {
                messages.Add($"✗ Requirement 1.4: Grid only provides {strategicNodes} strategic nodes (<8 required)");
                allPassed = false;
            }

            // Test grid bounds validation
            bool boundsValidation = TestBoundsValidation(gridSystem);
            if (boundsValidation)
            {
                messages.Add("✓ Grid bounds validation working correctly");
            }
            else
            {
                messages.Add("✗ Grid bounds validation failed");
                allPassed = false;
            }

            // Test position query methods
            bool queryMethods = TestQueryMethods(gridSystem);
            if (queryMethods)
            {
                messages.Add("✓ Position query methods working correctly");
            }
            else
            {
                messages.Add("✗ Position query methods failed");
                allPassed = false;
            }

            // Test occupancy management
            bool occupancyManagement = TestOccupancyManagement(gridSystem);
            if (occupancyManagement)
            {
                messages.Add("✓ Occupancy management working correctly");
            }
            else
            {
                messages.Add("✗ Occupancy management failed");
                allPassed = false;
            }

            // Test coordinate conversion
            bool coordinateConversion = TestCoordinateConversion(gridSystem);
            if (coordinateConversion)
            {
                messages.Add("✓ Coordinate conversion working correctly");
            }
            else
            {
                messages.Add("✗ Coordinate conversion failed");
                allPassed = false;
            }

            // Configuration validation
            bool configValid = _testConfig.ValidateConfiguration(out var configErrors);
            if (configValid)
            {
                messages.Add("✓ Grid configuration is valid");
            }
            else
            {
                messages.Add($"✗ Grid configuration has errors: {string.Join(", ", configErrors)}");
                allPassed = false;
            }

            _requirementsMet = allPassed;
            _validationMessages = messages.ToArray();

            // Log results
            foreach (var message in messages)
            {
                if (message.StartsWith("✓"))
                    Debug.Log(message);
                else if (message.StartsWith("✗"))
                    Debug.LogError(message);
                else
                    Debug.LogWarning(message);
            }

            if (allPassed)
            {
                Debug.Log("<color=green><b>All GridSystem requirements validated successfully!</b></color>");
            }
            else
            {
                Debug.LogError("<color=red><b>Some GridSystem requirements failed validation!</b></color>");
            }
        }

        private bool TestBoundsValidation(GridSystem gridSystem)
        {
            try
            {
                // Test valid positions
                var validPos = new GridPosition(5, 3);
                if (!gridSystem.IsWithinBounds(validPos)) return false;

                // Test invalid positions
                var invalidX = new GridPosition(-1, 3);
                var invalidY = new GridPosition(5, 20);
                if (gridSystem.IsWithinBounds(invalidX) || gridSystem.IsWithinBounds(invalidY)) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TestQueryMethods(GridSystem gridSystem)
        {
            try
            {
                // Test node retrieval
                var pos = new GridPosition(2, 2);
                var node = gridSystem.GetNode(pos);
                if (node.Position != pos) return false;

                // Test range queries
                var nodesInRange = gridSystem.GetNodesInRange(pos, 2);
                if (nodesInRange == null) return false;

                // Test strategic node queries
                var strategicNodes = gridSystem.GetStrategicNodes();
                if (strategicNodes == null) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TestOccupancyManagement(GridSystem gridSystem)
        {
            try
            {
                var pos = new GridPosition(3, 3);
                var defenderId = "test-defender";

                // Test occupation
                if (!gridSystem.TryOccupyPosition(pos, defenderId)) return false;
                if (gridSystem.GetOccupantId(pos) != defenderId) return false;

                // Test double occupation prevention
                if (gridSystem.TryOccupyPosition(pos, "another-defender")) return false;

                // Test release
                gridSystem.ReleasePosition(pos);
                if (gridSystem.GetOccupantId(pos) != null) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TestCoordinateConversion(GridSystem gridSystem)
        {
            try
            {
                var gridPos = new GridPosition(4, 3);
                var worldPos = gridSystem.GridToWorldPosition(gridPos);
                var convertedBack = gridSystem.WorldToGridPosition(worldPos);

                return gridPos.Equals(convertedBack);
            }
            catch
            {
                return false;
            }
        }

        private void Start()
        {
            // Auto-validate on start in development builds
            if (Debug.isDebugBuild)
            {
                Invoke(nameof(ValidateRequirements), 1.0f);
            }
        }
    }
}