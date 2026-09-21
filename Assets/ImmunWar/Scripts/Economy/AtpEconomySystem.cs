using System;
using System.Collections.Generic;
using ImmunWar.Battle.State;

namespace ImmunWar.Economy
{
    public sealed class AtpEconomySystem
    {
        private readonly EconomyState _state;
        private readonly List<Generator> _generators = new List<Generator>();
        public event Action<int, int, string> AtpChanged;
        public AtpEconomySystem(EconomyState state) => _state = state;
        public bool TrySpend(int amount, string reason)
        {
            var previous = _state.Atp;
            if (!_state.TrySpend(amount)) return false;
            AtpChanged?.Invoke(previous, _state.Atp, reason);
            return true;
        }
        public void Add(int amount, string reason)
        {
            var previous = _state.Atp; _state.Add(amount); if (_state.Atp != previous) AtpChanged?.Invoke(previous, _state.Atp, reason);
        }
        public void RegisterGenerator(string instanceId, int amount, int intervalTicks) => _generators.Add(new Generator(instanceId, amount, Math.Max(1, intervalTicks)));
        public void RemoveGenerator(string instanceId) => _generators.RemoveAll(x => x.Id == instanceId);
        public void Tick(long tick)
        {
            foreach (var generator in _generators)
                if (tick > 0 && tick % generator.Interval == 0) Add(generator.Amount, generator.Id);
        }
        private readonly struct Generator
        {
            public string Id { get; } public int Amount { get; } public int Interval { get; }
            public Generator(string id, int amount, int interval) { Id = id; Amount = amount; Interval = interval; }
        }
    }
}

