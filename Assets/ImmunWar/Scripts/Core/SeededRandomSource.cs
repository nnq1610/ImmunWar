using System;

namespace ImmunWar.Core
{
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;
        public SeededRandomSource(int seed) => _random = new Random(seed);
        public int Range(int minimumInclusive, int maximumExclusive) => _random.Next(minimumInclusive, maximumExclusive);
        public float Value() => (float)_random.NextDouble();
    }
}

