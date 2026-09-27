using System;
using System.Collections;
using ImmunWar.Battle.Placement;
using ImmunWar.Core.Config;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ImmunWar.UI
{
    /// <summary>
    /// State structure for tracking placement selection and hover states
    /// Requirements: 1.2, 4.7, 8.1
    /// </summary>
    public readonly struct PlacementState
    {
        public bool IsActive { get; }
        public string SelectedDefenderId { get; }
        public DefenderRole SelectedRole { get; }
        public GridPosition HoveredPosition { get; }
        public bool PreviewActive { get; }
        public ValidationResult CurrentValidation { get; }

        public PlacementState(bool isActive = false, string selectedDefenderId = null, 
            DefenderRole selectedRole = DefenderRole.Damage, GridPosition hoveredPosition = default, 
            bool previewActive = false, ValidationResult currentValidation = default)
        {
            IsActive = isActive;
            SelectedDefenderId = selectedDefenderId;
            SelectedRole = selectedRole;
            HoveredPosition = hoveredPosition;
            PreviewActive = previewActive;
            CurrentValidation = currentValidation;
        }

        public PlacementState WithSelection(string defenderId, DefenderRole role)
            => new PlacementState(true, defenderId, role, HoveredPosition, PreviewActive, CurrentValidation);

        public PlacementState WithHover(GridPosition position)
            => new PlacementState(IsActive, SelectedDefenderId, SelectedRole, position, true, CurrentValidation);

        public PlacementState WithValidation(ValidationResult validation)
            => new PlacementState(IsActive, SelectedDefenderId, SelectedRole, HoveredPosition, PreviewActive, validation);

        public PlacementState ClearSelection()
            => new PlacementState(false, null, DefenderRole.Damage, default, false, default);

        public PlacementState ClearPreview()
            => new PlacementState(IsActive, SelectedDefenderId, SelectedRole, default, false, default);
    }

    /// <summary>
    /// Enhanced PlacementController with grid-based mouse input and preview management
    /// Handles mouse position to grid conversion, placement validation, and visual feedback
    /// Requirements: 1.2, 4.7, 8.1
    /// </summary>
    public sealed class PlacementController : MonoBehaviour
    {
        [Header("Grid System References")]
        [SerializeField] private Camera _battleCamera;
        [SerializeField] private LayerMask _gridLayerMask = -1;
        
        [Header("Preview Configuration")]
        [SerializeField] private GameObject _previewIndicatorPrefab;
        [SerializeField] private GameObject _validPlacementIndicator;
        [SerializeField] private GameObject _invalidPlacementIndicator;
        [SerializeField] private float _previewUpdateDelay = 0.1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color _validPreviewColor = Color.green;
        [SerializeField] private Color _invalidPreviewColor = Color.red;
        [SerializeField] private Color _hoveredGridColor = Color.yellow;

        // System references
        private PlacementSystem _system;
        private GridSystem _gridSystem;
        private ValidationSystem _validationSystem;
        
        // State management
        private PlacementState _currentState;
        private GridPosition _lastMouseGridPosition;
        private bool _mouseOverGrid;
        
        // Preview indicators
        private GameObject _activePreviewIndicator;
        private GameObject _activeRangePreview;
        private Coroutine _previewUpdateCoroutine;

        // Events
        public event Action<PlacementState> StateChanged;
        public event Action<GridPosition, bool> GridPositionHovered;
        public event Action<ValidationResult> ValidationChanged;

        // Legacy compatibility properties
        public bool HasSelection => _currentState.IsActive && !string.IsNullOrEmpty(_currentState.SelectedDefenderId);
        
        private void Awake()
        {
            // Initialize camera reference if not set
            if (_battleCamera == null)
                _battleCamera = Camera.main;

            // Initialize state
            _currentState = new PlacementState();
        }

        private void Update()
        {
            HandleMouseInput();
            HandleKeyboardInput();
        }

        /// <summary>
        /// Initialize the placement controller with required systems
        /// </summary>
        public void Initialize(PlacementSystem system, GridSystem gridSystem, ValidationSystem validationSystem)
        {
            _system = system;
            _gridSystem = gridSystem;
            _validationSystem = validationSystem;
        }

        /// <summary>
        /// Legacy initialization for backwards compatibility
        /// </summary>
        public void Initialize(PlacementSystem system) => _system = system;

        /// <summary>
        /// Handles mouse input for grid interaction and placement
        /// </summary>
        private void HandleMouseInput()
        {
            if (_gridSystem == null) return;

            var mousePosition = Input.mousePosition;
            HandleMouseInput(mousePosition);
        }

        /// <summary>
        /// Main mouse input handler with screen position parameter
        /// Requirements: 1.2, 8.1
        /// </summary>
        public void HandleMouseInput(Vector2 screenPosition)
        {
            if (_gridSystem == null || _battleCamera == null) return;

            // Skip if pointer is over UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (_currentState.PreviewActive)
                {
                    HidePreview();
                }
                return;
            }

            // Convert screen position to world position
            var ray = _battleCamera.ScreenPointToRay(screenPosition);
            var worldPosition = ray.GetPoint(10f); // Assume 2D plane at distance 10

            // Convert world position to grid position
            var gridPosition = _gridSystem.WorldToGridPosition(worldPosition);
            var isValidGridPosition = _gridSystem.IsWithinBounds(gridPosition);

            // Update mouse over grid state
            _mouseOverGrid = isValidGridPosition;

            // Handle grid position changes
            if (isValidGridPosition && !gridPosition.Equals(_lastMouseGridPosition))
            {
                _lastMouseGridPosition = gridPosition;
                OnGridPositionHovered(gridPosition);
            }

            // Handle mouse clicks
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                HandleGridClick(gridPosition);
            }
            else if (Input.GetMouseButtonDown(1)) // Right click
            {
                CancelSelection();
            }
        }

        /// <summary>
        /// Handles keyboard input for grid navigation and shortcuts
        /// </summary>
        private void HandleKeyboardInput()
        {
            // Handle escape key to cancel selection
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
            }

            if (!_keyboardNavigationEnabled) return;

            // Handle grid navigation with arrow keys (Task 5.3)
            var direction = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) direction.x = -1;
            if (Input.GetKeyDown(KeyCode.RightArrow)) direction.x = 1;
            if (Input.GetKeyDown(KeyCode.UpArrow)) direction.y = 1;
            if (Input.GetKeyDown(KeyCode.DownArrow)) direction.y = -1;

            if (direction != Vector2Int.zero)
            {
                HandleGridNavigation(direction);
            }
        }

        /// <summary>
        /// Handles keyboard navigation of grid positions
        /// </summary>
        public void HandleGridNavigation(Vector2Int direction)
        {
            if (_gridSystem == null) return;

            var currentPos = _currentState.HoveredPosition;
            var newPos = new GridPosition(currentPos.X + direction.x, currentPos.Y + direction.y);

            if (_gridSystem.IsWithinBounds(newPos))
            {
                OnGridPositionHovered(newPos);
            }
        }

        /// <summary>
        /// Handles grid position clicking for placement confirmation
        /// </summary>
        private void HandleGridClick(GridPosition gridPosition)
        {
            if (!_currentState.IsActive || string.IsNullOrEmpty(_currentState.SelectedDefenderId))
                return;

            // Validate placement
            var request = new PlacementRequest(
                gridPosition,
                _currentState.SelectedDefenderId,
                _currentState.SelectedRole,
                GetPlacementCost(_currentState.SelectedDefenderId, gridPosition),
                GetCurrentATP()
            );

            var validation = _validationSystem.ValidatePlacement(request);

            if (validation.IsValid)
            {
                ConfirmPlacement(gridPosition);
            }
            else
            {
                // Show validation error feedback
                Debug.LogWarning($"Placement failed: {validation.DetailMessage}");
                // In a full implementation, would show UI error message
            }
        }

        /// <summary>
        /// Handles grid position hovering for preview updates
        /// </summary>
        private void OnGridPositionHovered(GridPosition position)
        {
            _currentState = _currentState.WithHover(position);
            UpdatePreview(position);
            GridPositionHovered?.Invoke(position, _gridSystem.IsValidPosition(position));
        }

        /// <summary>
        /// Selects a defender for placement
        /// </summary>
        public void SelectDefender(string defenderId, DefenderRole role)
        {
            _currentState = _currentState.WithSelection(defenderId, role);
            StateChanged?.Invoke(_currentState);

            if (_mouseOverGrid)
            {
                UpdatePreview(_lastMouseGridPosition);
            }
        }

        /// <summary>
        /// Legacy defender selection method for backwards compatibility
        /// </summary>
        public void Select(string configId, DefenderRole role, int cost, float health)
        {
            SelectDefender(configId, role);
        }

        /// <summary>
        /// Cancels current defender selection
        /// </summary>
        public void CancelSelection()
        {
            HidePreview();
            _currentState = _currentState.ClearSelection();
            StateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Legacy cancel method for backwards compatibility
        /// </summary>
        public void Cancel() => CancelSelection();

        /// <summary>
        /// Confirms defender placement at specified grid position
        /// </summary>
        private void ConfirmPlacement(GridPosition gridPosition)
        {
            if (_system == null || !_currentState.IsActive)
                return;

            // Convert grid position to node ID (simplified for compatibility)
            var nodeId = $"{gridPosition.X}-{gridPosition.Y}";
            
            // Get defender role mask
            var roleMask = ConfigValidator.ToMask(_currentState.SelectedRole);
            
            // Attempt placement using legacy system
            var result = _system.TryPlace(
                Guid.NewGuid().ToString("N"),
                _currentState.SelectedDefenderId,
                _currentState.SelectedRole,
                nodeId,
                roleMask,
                GetPlacementCost(_currentState.SelectedDefenderId, gridPosition),
                100f // Default health
            );

            if (result.Accepted)
            {
                // Update grid system with new occupant
                _gridSystem.TryOccupyPosition(gridPosition, result.DefenderInstanceId);
                CancelSelection();
            }
        }

        /// <summary>
        /// Legacy confirmation method for backwards compatibility
        /// </summary>
        public PlacementResult Confirm(string nodeId, DefenderRoleMask mask)
        {
            if (_system == null || !HasSelection) 
                return new PlacementResult(false, "selection_missing");
                
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) 
                return new PlacementResult(false, "pointer_over_ui");

            var result = _system.TryPlace(
                Guid.NewGuid().ToString("N"), 
                _currentState.SelectedDefenderId, 
                _currentState.SelectedRole, 
                nodeId, 
                mask, 
                GetPlacementCost(_currentState.SelectedDefenderId, default),
                100f
            );
            
            if (result.Accepted) 
                Cancel();
                
            return result;
        }

        /// <summary>
        /// Updates placement preview at specified position
        /// Requirements: 1.2
        /// </summary>
        public void UpdatePreview(GridPosition position)
        {
            if (!_currentState.IsActive || _validationSystem == null)
            {
                HidePreview();
                return;
            }

            // Validate placement at this position
            var request = new PlacementRequest(
                position,
                _currentState.SelectedDefenderId,
                _currentState.SelectedRole,
                GetPlacementCost(_currentState.SelectedDefenderId, position),
                GetCurrentATP()
            );

            var validation = _validationSystem.ValidatePlacement(request);
            _currentState = _currentState.WithValidation(validation);

            // Show preview indicator
            ShowPlacementPreview(position, validation.IsValid);
            
            // Show range preview if valid
            if (validation.IsValid)
            {
                ShowRangePreview(_currentState.SelectedDefenderId, position);
            }

            ValidationChanged?.Invoke(validation);
        }

        /// <summary>
        /// Shows placement preview indicator
        /// </summary>
        private void ShowPlacementPreview(GridPosition position, bool isValid)
        {
            // Hide existing preview
            if (_activePreviewIndicator != null)
            {
                DestroyImmediate(_activePreviewIndicator);
            }

            // Create new preview indicator
            var prefab = isValid ? _validPlacementIndicator : _invalidPlacementIndicator;
            if (prefab != null)
            {
                var worldPos = _gridSystem.GridToWorldPosition(position);
                _activePreviewIndicator = Instantiate(prefab, worldPos, Quaternion.identity);
                
                // Set color based on validity and strategic node
                var renderer = _activePreviewIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color targetColor = isValid ? _validPreviewColor : _invalidPreviewColor;
                    
                    // Task 11.2: Strategic Node visual differentiation
                    if (isValid && _gridSystem.TryGetNode(position, out var node) && node.Type == NodeType.Strategic)
                    {
                        targetColor = Color.cyan; // Example strategic node highlight color
                    }
                    
                    renderer.material.color = targetColor;
                }
            }
        }

        /// <summary>
        /// Shows defender range preview
        /// </summary>
        public void ShowRangePreview(string defenderId, GridPosition position)
        {
            // Hide existing range preview
            if (_activeRangePreview != null)
            {
                DestroyImmediate(_activeRangePreview);
            }

            // Create range preview (simplified - would show actual defender range in full implementation)
            var worldPos = _gridSystem.GridToWorldPosition(position);
            
            // For now, just create a simple circle to indicate range
            // In full implementation, would query defender configuration for actual range
            _activeRangePreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _activeRangePreview.transform.position = worldPos;
            _activeRangePreview.transform.localScale = Vector3.one * 2.0f; // Example range
            
            var renderer = _activeRangePreview.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(_validPreviewColor.r, _validPreviewColor.g, _validPreviewColor.b, 0.3f);
            }
        }

        /// <summary>
        /// Hides all preview indicators
        /// </summary>
        public void HidePreview()
        {
            if (_activePreviewIndicator != null)
            {
                DestroyImmediate(_activePreviewIndicator);
                _activePreviewIndicator = null;
            }

            if (_activeRangePreview != null)
            {
                DestroyImmediate(_activeRangePreview);
                _activeRangePreview = null;
            }

            _currentState = _currentState.ClearPreview();
        }

        /// <summary>
        /// Gets placement cost for defender at position (simplified implementation)
        /// </summary>
        private int GetPlacementCost(string defenderId, GridPosition position)
        {
            // Simplified cost calculation - would query defender configuration in full implementation
            var baseCost = 100;

            // Apply strategic node multiplier if applicable
            if (_gridSystem != null && _gridSystem.TryGetNode(position, out var node) && node.Type == NodeType.Strategic)
            {
                return (int)(baseCost * 1.25f);
            }

            return baseCost;
        }

        /// <summary>
        /// Gets current ATP amount (placeholder implementation)
        /// </summary>
        private int GetCurrentATP()
        {
            // Placeholder - would query actual ATP system in full implementation
            return 500;
        }

        /// <summary>
        /// Cleanup on component destruction
        /// </summary>
        private void OnDestroy()
        {
            HidePreview();
            
            if (_previewUpdateCoroutine != null)
            {
                StopCoroutine(_previewUpdateCoroutine);
            }
        }

        /// <summary>
        /// Get current placement state for external systems
        /// </summary>
        public PlacementState GetCurrentState() => _currentState;

        /// <summary>
        /// Check if position is currently being hovered
        /// </summary>
        public bool IsPositionHovered(GridPosition position) => 
            _currentState.PreviewActive && _currentState.HoveredPosition.Equals(position);

        // --- Accessibility (Task 5) ---
        private bool _keyboardNavigationEnabled = true;
        private bool _highContrastMode = false;
        private ColorBlindType _colorBlindType = ColorBlindType.None;

        public enum ColorBlindType { None, Protanopia, Deuteranopia, Tritanopia }

        public void SetKeyboardNavigationEnabled(bool enabled)
        {
            _keyboardNavigationEnabled = enabled;
        }

        public void SetHighContrastMode(bool enabled)
        {
            _highContrastMode = enabled;
            // Adjust preview colors for 4.5:1 contrast
            _validPreviewColor = enabled ? new Color(0f, 1f, 0f, 1f) : Color.green;
            _invalidPreviewColor = enabled ? new Color(1f, 0f, 0f, 1f) : Color.red;
            _hoveredGridColor = enabled ? new Color(1f, 1f, 0f, 1f) : Color.yellow;
            if (_activePreviewIndicator != null && _currentState.PreviewActive)
            {
                var renderer = _activePreviewIndicator.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = _currentState.CurrentValidation.IsValid ? _validPreviewColor : _invalidPreviewColor;
            }
        }

        public void SetColorBlindSupport(ColorBlindType type)
        {
            _colorBlindType = type;
            // Here we would swap patterns or indicator shapes based on color blindness
            // For example, using distinct shapes (cross vs check) in addition to colors.
        }
    }
}

