using System;

namespace ImmunWar.Battle.State
{
    public sealed class VitalityState
    {
        public int Maximum { get; }
        public int Current { get; private set; }
        public bool IsDepleted => Current == 0;
        public VitalityState(int maximum) { Maximum = Math.Max(1, maximum); Current = Maximum; }
        public int Damage(int amount) { if (amount > 0) Current = Math.Max(0, Current - amount); return Current; }
        public int Repair(int amount) { if (amount > 0) Current = Math.Min(Maximum, Current + amount); return Current; }
    }
}

