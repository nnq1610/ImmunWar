using System.Collections.Generic;
using System.Linq;
using ImmunWar.Core.Config;
using ImmunWar.Economy;

namespace ImmunWar.Battle.Placement
{
    /// <summary>
    /// Validation failure reasons for placement attempts
    /// Used to provide specific feedback to players about placement restrictions
    /// </summary>
    public enum ValidationFailureReason
    {
        None,
        OutOfBounds,
        PositionOccupied,
        InsufficientATP,
        DefenderRestriction,
        DensityLimit,
        PathBlocking
    }

    /// <summary>
    /// Result of placement validation containing success state and failure details
    /// </summary>
    public readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public ValidationFailureReason Reason { get; }
        public string DetailMessage { get; }
        public IEnumerable<string> Suggestions { get; }

        public ValidationResult(bool isValid, ValidationFailureReason reason = ValidationFailureReason.None, 
            string detailMessage = null, IEnumerable<string> suggestions = null)
        {
            IsValid = isValid;
            Reason = reason;
            DetailMessage = detailMessage ?? string.Empty;
            Suggestions = suggestions ?? Enumerable.Empty<string>();
        }

        public static ValidationResult Success() => new ValidationResult(true);
        
        public static ValidationResult Failure(ValidationFailureReason reason, string message, params string[] suggestions)
            => new ValidationResult(false, reason, message, suggestions);
    }

    /// <summary>
    /// Request structure for placement validation containing all necessary information
    /// </summary>
    public readonly struct PlacementRequest
    {
        public GridPosition Position { get; }
        public string DefenderId { get; }
        public DefenderRole Role { get; }
        public int ATPCost { get; }
        public int AvailableATP { get; }

        public PlacementRequest(GridPosition position, string defenderId, DefenderRole role, int atpCost, int availableATP)
        {
            Position = position;
            DefenderId = defenderId;
            Role = role;
            ATPCost = atpCost;
            AvailableATP = availableATP;
        }
    }

    /// <summary>
    /// Comprehensive validation system for defender placement rules
    /// Enforces grid boundaries, occupancy, ATP costs, and defender-specific restrictions
    /// Requirements: 1.3, 7.1, 7.2
    /// </summary>
    public class ValidationSystem
    {
        private readonly GridSystem _gridSystem;
        private readonly GridConfiguration _configuration;

        public ValidationSystem(GridSystem gridSystem, GridConfiguration configuration)
        {
            _gridSystem = gridSystem;
            _configuration = configuration;
        }

        /// <summary>
        /// Primary validation interface - validates all placement rules
        /// Returns comprehensive result with specific failure reasons and suggestions
        /// </summary>
        public ValidationResult ValidatePlacement(PlacementRequest request)
        {
            // Check grid bounds first (fastest validation)
            if (!ValidateGridBounds(request.Position))
            {
                var nearestValid = GetNearestValidPosition(request.Position);
                return ValidationResult.Failure(
                    ValidationFailureReason.OutOfBounds,
                    $"Position {request.Position} is outside grid boundaries",
                    $"Try position {nearestValid} instead"
                );
            }

            // Check if position is occupied
            if (!ValidateOccupancy(request.Position))
            {
                var occupantId = _gridSystem.GetOccupantId(request.Position);
                var alternatives = GetNearbyAlternatives(request.Position, 2);
                return ValidationResult.Failure(
                    ValidationFailureReason.PositionOccupied,
                    $"Position {request.Position} is occupied by {occupantId}",
                    alternatives.Select(pos => $"Try {pos}").ToArray()
                );
            }

            // Check ATP cost affordability
            if (!ValidateATPCost(request.DefenderId, request.Position, request.AvailableATP))
            {
                var requiredATP = GetPlacementCost(request.DefenderId, request.Position);
                var shortfall = requiredATP - request.AvailableATP;
                return ValidationResult.Failure(
                    ValidationFailureReason.InsufficientATP,
                    $"Need {requiredATP} ATP, have {request.AvailableATP} (short {shortfall})",
                    "Wait for ATP generation", "Consider cheaper defender types"
                );
            }

            // Check defender role restrictions
            if (!ValidateDefenderRestrictions(request.DefenderId, request.Position))
            {
                var node = _gridSystem.GetNode(request.Position);
                var allowedRoles = GetAllowedRoles(node);
                return ValidationResult.Failure(
                    ValidationFailureReason.DefenderRestriction,
                    $"Defender {request.DefenderId} not allowed at {request.Position}",
                    $"Allowed roles: {allowedRoles}"
                );
            }

            // Check density limitations (Energy Cells)
            if (!ValidateDensityLimits(request.DefenderId, request.Position))
            {
                var alternatives = GetValidPositionsForDensityRule(request.DefenderId);
                return ValidationResult.Failure(
                    ValidationFailureReason.DensityLimit,
                    $"Energy Cell density limit: max 1 per 3x3 area",
                    alternatives.Take(3).Select(pos => $"Try {pos}").ToArray()
                );
            }

            // Check if placement would block critical paths
            if (!ValidatePathBlocking(request.Position))
            {
                return ValidationResult.Failure(
                    ValidationFailureReason.PathBlocking,
                    "Placement would block critical pathogen routes",
                    "This may create strategic advantages", "Confirm intentional chokepoint creation"
                );
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates that position is within grid boundaries
        /// </summary>
        public bool ValidateGridBounds(GridPosition position)
        {
            return _gridSystem.IsWithinBounds(position);
        }

        /// <summary>
        /// Validates that position is not currently occupied
        /// </summary>
        public bool ValidateOccupancy(GridPosition position)
        {
            if (!_gridSystem.TryGetNode(position, out var node))
                return false;
                
            return !node.IsOccupied;
        }

        /// <summary>
        /// Validates that player has sufficient ATP for placement cost
        /// Includes strategic node cost multipliers
        /// </summary>
        public bool ValidateATPCost(string defenderId, GridPosition position, int availableATP)
        {
            var cost = GetPlacementCost(defenderId, position);
            return availableATP >= cost;
        }

        /// <summary>
        /// Validates defender type is allowed at specified position
        /// Checks node-specific role restrictions
        /// </summary>
        public bool ValidateDefenderRestrictions(string defenderId, GridPosition position)
        {
            if (!_gridSystem.TryGetNode(position, out var node))
                return false;

            // Get defender role from ID (simplified - in real implementation would query defender config)
            var defenderRole = GetDefenderRole(defenderId);
            var allowedRoles = node.AllowedRoles;

            // Check if this defender role is permitted at this position
            var roleMask = ConfigValidator.ToMask(defenderRole);
            return (allowedRoles & roleMask) != DefenderRoleMask.None;
        }

        /// <summary>
        /// Validates density limitations for Energy Cells
        /// Prevents ATP farming by limiting Energy Cell placement density
        /// Requirements: 6.4
        /// </summary>
        public bool ValidateDensityLimits(string defenderId, GridPosition position)
        {
            // Only apply density limits to Energy Cell defenders
            if (GetDefenderRole(defenderId) != DefenderRole.Economy)
                return true;

            // Check 3x3 area around placement position
            var energyCellsInArea = 0;
            for (int x = position.X - 1; x <= position.X + 1; x++)
            {
                for (int y = position.Y - 1; y <= position.Y + 1; y++)
                {
                    var checkPos = new GridPosition(x, y);
                    if (!_gridSystem.IsWithinBounds(checkPos))
                        continue;

                    var occupantId = _gridSystem.GetOccupantId(checkPos);
                    if (!string.IsNullOrEmpty(occupantId) && GetDefenderRole(occupantId) == DefenderRole.Economy)
                    {
                        energyCellsInArea++;
                    }
                }
            }

            // Maximum 1 Energy Cell per 3x3 area
            return energyCellsInArea == 0;
        }

        /// <summary>
        /// Validates that placement won't block critical pathogen routes
        /// Warns about strategic consequences of chokepoint creation
        /// </summary>
        public bool ValidatePathBlocking(GridPosition position)
        {
            // Simplified path blocking validation
            // In full implementation, would check against PathSystem for critical route blocking
            // For now, always return true (no path blocking restrictions)
            return true;
        }

        /// <summary>
        /// Calculates placement cost including strategic node multipliers
        /// </summary>
        private int GetPlacementCost(string defenderId, GridPosition position)
        {
            // Base cost for defender type (simplified - would query DefenderConfig in real implementation)
            var baseCost = GetDefenderBaseCost(defenderId);

            // Apply strategic node cost multiplier
            if (_gridSystem.TryGetNode(position, out var node) && node.Type == NodeType.Strategic)
            {
                return (int)(baseCost * 1.25f); // 25% premium for strategic nodes
            }

            return baseCost;
        }

        /// <summary>
        /// Gets defender role from defender ID (simplified implementation)
        /// </summary>
        private DefenderRole GetDefenderRole(string defenderId)
        {
            // Simplified role mapping - in real implementation would query DefenderConfig
            return defenderId.ToLower() switch
            {
                var id when id.Contains("macrophage") => DefenderRole.Blocker,
                var id when id.Contains("tcell") => DefenderRole.Damage,
                var id when id.Contains("bcell") => DefenderRole.Support,
                var id when id.Contains("nk") => DefenderRole.Burst,
                var id when id.Contains("platelet") => DefenderRole.Repair,
                var id when id.Contains("energy") => DefenderRole.Economy,
                _ => DefenderRole.Damage
            };
        }

        /// <summary>
        /// Gets base ATP cost for defender type (simplified implementation)
        /// </summary>
        private int GetDefenderBaseCost(string defenderId)
        {
            // Simplified cost mapping - in real implementation would query DefenderConfig
            return GetDefenderRole(defenderId) switch
            {
                DefenderRole.Blocker => 75,
                DefenderRole.Damage => 100,
                DefenderRole.Support => 125,
                DefenderRole.Burst => 150,
                DefenderRole.Repair => 90,
                DefenderRole.Economy => 200,
                _ => 100
            };
        }

        /// <summary>
        /// Finds nearest valid placement position to given coordinates
        /// </summary>
        private GridPosition GetNearestValidPosition(GridPosition position)
        {
            var validPositions = _gridSystem.GetValidPlacementPositions();
            var nearest = validPositions.FirstOrDefault();
            var minDistance = float.MaxValue;

            foreach (var validPos in validPositions)
            {
                var distance = (position.X - validPos.X) * (position.X - validPos.X) + 
                              (position.Y - validPos.Y) * (position.Y - validPos.Y);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = validPos;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Gets alternative positions near the specified location
        /// </summary>
        private IEnumerable<GridPosition> GetNearbyAlternatives(GridPosition position, int radius)
        {
            var alternatives = new List<GridPosition>();

            for (int x = position.X - radius; x <= position.X + radius; x++)
            {
                for (int y = position.Y - radius; y <= position.Y + radius; y++)
                {
                    var pos = new GridPosition(x, y);
                    if (pos.Equals(position)) continue; // Skip original position
                    
                    if (_gridSystem.IsWithinBounds(pos) && ValidateOccupancy(pos))
                    {
                        alternatives.Add(pos);
                    }
                }
            }

            return alternatives;
        }

        /// <summary>
        /// Gets allowed roles for a grid node (simplified implementation)
        /// </summary>
        private string GetAllowedRoles(GridNode node)
        {
            var roles = new List<string>();
            var mask = node.AllowedRoles;

            if ((mask & DefenderRoleMask.Blocker) != 0) roles.Add("Blocker");
            if ((mask & DefenderRoleMask.Damage) != 0) roles.Add("Damage");
            if ((mask & DefenderRoleMask.Support) != 0) roles.Add("Support");
            if ((mask & DefenderRoleMask.Burst) != 0) roles.Add("Burst");
            if ((mask & DefenderRoleMask.Repair) != 0) roles.Add("Repair");
            if ((mask & DefenderRoleMask.Economy) != 0) roles.Add("Economy");

            return string.Join(", ", roles);
        }

        /// <summary>
        /// Gets valid positions for Energy Cell placement considering density rules
        /// </summary>
        private IEnumerable<GridPosition> GetValidPositionsForDensityRule(string defenderId)
        {
            if (GetDefenderRole(defenderId) != DefenderRole.Economy)
                return _gridSystem.GetValidPlacementPositions();

            var validPositions = new List<GridPosition>();
            var allPositions = _gridSystem.GetValidPlacementPositions();

            foreach (var position in allPositions)
            {
                if (ValidateOccupancy(position) && ValidateDensityLimits(defenderId, position))
                {
                    validPositions.Add(position);
                }
            }

            return validPositions;
        }
    }


}