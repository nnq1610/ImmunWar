namespace ImmunWar.Battle.State
{
    public enum FeverPhase { Charging, Ready, Active, Cooldown }
    public sealed class FeverState
    {
        public FeverPhase Phase { get; internal set; } = FeverPhase.Charging;
        public int Charge { get; internal set; }
        public int RemainingTicks { get; internal set; }
    }
}

