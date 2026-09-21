namespace ImmunWar.Battle.State
{
    public sealed class StatusEffectState
    {
        public string ConfigId { get; }
        public string SourceId { get; }
        public int Stacks { get; internal set; }
        public int RemainingTicks { get; internal set; }
        public StatusEffectState(string configId, string sourceId, int stacks, int remainingTicks)
        { ConfigId = configId; SourceId = sourceId; Stacks = stacks; RemainingTicks = remainingTicks; }
    }
}
