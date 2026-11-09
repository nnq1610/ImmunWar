using System;

namespace ImmunWar.Battle.State
{
    public sealed class EconomyState
    {
        public const int MaxAtp = 300;
        public const int WaveBudgetMax = 90;

        public int Atp { get; private set; }
        public int WaveBudgetRemaining { get; private set; } = WaveBudgetMax;
        public int Room => Math.Max(0, MaxAtp - Atp);

        public EconomyState(int startingAtp) => Atp = Math.Clamp(startingAtp, 0, MaxAtp);

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Atp < amount) return false;
            Atp -= amount;
            return true;
        }

        public int Add(int amount)
        {
            if (amount > 0) Atp = Math.Min(MaxAtp, Atp + amount);
            return Atp;
        }

        /// <summary>Generates passive ATP from Energy Cells within the 90 ATP/wave budget and 300 ATP cap.</summary>
        public int GenerateWaveAtp(int amount)
        {
            if (amount <= 0 || WaveBudgetRemaining <= 0 || Room <= 0) return 0;
            var actual = Math.Min(amount, Math.Min(WaveBudgetRemaining, Room));
            Atp += actual;
            WaveBudgetRemaining -= actual;
            return actual;
        }

        /// <summary>ATP Surge requires full room and wave budget for the entire surge amount (20 ATP).</summary>
        public bool CanSurge(int amount = 20) => amount > 0 && WaveBudgetRemaining >= amount && Room >= amount;

        public bool TrySurge(int amount = 20)
        {
            if (!CanSurge(amount)) return false;
            Atp += amount;
            WaveBudgetRemaining -= amount;
            return true;
        }

        public void ResetWaveBudget() => WaveBudgetRemaining = WaveBudgetMax;
    }
}

