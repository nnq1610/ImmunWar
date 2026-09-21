using System;

namespace ImmunWar.Core
{
    public sealed class FixedSimulationClock
    {
        private readonly double _step;
        private double _accumulator;
        public long Tick { get; private set; }
        public int TicksPerSecond { get; }

        public FixedSimulationClock(int ticksPerSecond = 30)
        {
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            TicksPerSecond = ticksPerSecond;
            _step = 1d / ticksPerSecond;
        }

        public int Accumulate(double unscaledDeltaSeconds)
        {
            if (unscaledDeltaSeconds < 0d) throw new ArgumentOutOfRangeException(nameof(unscaledDeltaSeconds));
            _accumulator += unscaledDeltaSeconds;
            var advanced = 0;
            const double epsilon = 1e-10;
            while (_accumulator + epsilon >= _step)
            {
                _accumulator -= _step;
                Tick++;
                advanced++;
            }
            return advanced;
        }

        public void Reset()
        {
            _accumulator = 0d;
            Tick = 0;
        }
    }
}

