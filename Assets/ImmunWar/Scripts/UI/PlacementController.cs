using ImmunWar.Battle.Placement;
using ImmunWar.Core.Config;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ImmunWar.UI
{
    public sealed class PlacementController : MonoBehaviour
    {
        private PlacementSystem _system;
        private string _selectedConfigId;
        private DefenderRole _selectedRole;
        private int _cost;
        private float _health;
        public bool HasSelection => !string.IsNullOrEmpty(_selectedConfigId);
        public void Initialize(PlacementSystem system) => _system = system;
        public void Select(string configId, DefenderRole role, int cost, float health) { _selectedConfigId = configId; _selectedRole = role; _cost = cost; _health = health; }
        public void Cancel() => _selectedConfigId = null;
        public PlacementResult Confirm(string nodeId, DefenderRoleMask mask)
        {
            if (_system == null || !HasSelection) return new PlacementResult(false, "selection_missing");
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return new PlacementResult(false, "pointer_over_ui");
            var result = _system.TryPlace(System.Guid.NewGuid().ToString("N"), _selectedConfigId, _selectedRole, nodeId, mask, _cost, _health);
            if (result.Accepted) Cancel();
            return result;
        }
    }
}

