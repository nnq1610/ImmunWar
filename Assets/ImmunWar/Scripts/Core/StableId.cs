using System;

namespace ImmunWar.Core
{
    [Serializable]
    public readonly struct StableId : IEquatable<StableId>, IComparable<StableId>
    {
        public string Value { get; }

        public StableId(string value)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("Stable ID cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Equals(StableId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public int CompareTo(StableId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(StableId id) => id.Value;
    }
}

