namespace ImmunWar.Battle.State
{
    public enum WavePhase { Waiting, Spawning, Active, Complete, AllComplete }
    public sealed class WaveState
    {
        public int WaveIndex { get; internal set; } = -1;
        public WavePhase Phase { get; internal set; } = WavePhase.Waiting;
        public int RemainingConfigured { get; internal set; }
        public int ActiveEnemies { get; internal set; }
    }
}

