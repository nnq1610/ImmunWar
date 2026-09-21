using System;

namespace ImmunWar.Battle.State
{
    public sealed class EconomyState
    {
        public int Atp { get; private set; }
        public EconomyState(int startingAtp) => Atp = Math.Max(0, startingAtp);
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Atp < amount) return false;
            Atp -= amount;
            return true;
        }
        public int Add(int amount) { if (amount > 0) Atp += amount; return Atp; }
    }
}

