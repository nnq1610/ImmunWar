namespace ImmunWar.Core
{
    public sealed class SequenceId
    {
        public long Current { get; private set; }
        public long Next() => ++Current;
        public void Reset() => Current = 0;
    }
}

