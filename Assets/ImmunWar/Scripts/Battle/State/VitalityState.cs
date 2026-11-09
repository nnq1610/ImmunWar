using System;

namespace ImmunWar.Battle.State
{
    public sealed class VitalityState
    {
        public const int WaveRepairBudgetMax = 12;

        public int Maximum { get; }
        public int Current { get; private set; }
        public int WaveRepairRemaining { get; private set; } = WaveRepairBudgetMax;
        public bool IsDepleted => Current == 0;

        public VitalityState(int maximum) { Maximum = Math.Max(1, maximum); Current = Maximum; }
        public int Damage(int amount) { if (amount > 0) Current = Math.Max(0, Current - amount); return Current; }
        public int Repair(int amount) { if (amount > 0) Current = Math.Min(Maximum, Current + amount); return Current; }

        /// <summary>Repairs Organ Vitality up to the 12 Vitality/wave shared cap across all Platelets.</summary>
        public int RepairWave(int amount)
        {
            if (amount <= 0 || Current >= Maximum || WaveRepairRemaining <= 0) return 0;
            var missing = Maximum - Current;
            var actual = Math.Min(amount, Math.Min(missing, WaveRepairRemaining));
            Current += actual;
            WaveRepairRemaining -= actual;
            return actual;
        }

        public void ResetWaveRepairBudget() => WaveRepairRemaining = WaveRepairBudgetMax;
    }
}

