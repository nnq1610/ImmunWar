using System;
using ImmunWar.Battle.State;

namespace ImmunWar.Battle.Fever
{
    public sealed class FeverSystem
    {
        private readonly FeverState _state; private readonly int _maximumCharge; private readonly int _durationTicks; private readonly float _damage; private readonly float _speed;
        public event Action<FeverState> Changed;
        public FeverSystem(FeverState state, int maximumCharge, int durationTicks, float damageMultiplier, float speedMultiplier)
        { _state = state; _maximumCharge = Math.Max(1, maximumCharge); _durationTicks = Math.Max(1, durationTicks); _damage = Math.Max(0f, damageMultiplier); _speed = Math.Max(0f, speedMultiplier); }
        public float DamageMultiplier => _state.Phase == FeverPhase.Active ? _damage : 1f;
        public float SpeedMultiplier => _state.Phase == FeverPhase.Active ? _speed : 1f;
        public void AddCharge(int amount)
        {
            if (_state.Phase == FeverPhase.Active || amount <= 0) return; _state.Charge = Math.Min(_maximumCharge, _state.Charge + amount); if (_state.Charge >= _maximumCharge) _state.Phase = FeverPhase.Ready; Changed?.Invoke(_state);
        }
        public bool Activate()
        {
            if (_state.Phase != FeverPhase.Ready) return false; _state.Phase = FeverPhase.Active; _state.RemainingTicks = _durationTicks; Changed?.Invoke(_state); return true;
        }
        public void Tick()
        {
            if (_state.Phase != FeverPhase.Active) return; _state.RemainingTicks--; if (_state.RemainingTicks <= 0) { _state.RemainingTicks = 0; _state.Charge = 0; _state.Phase = FeverPhase.Charging; } Changed?.Invoke(_state);
        }
    }
}

