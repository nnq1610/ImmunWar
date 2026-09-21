using System;

namespace ImmunWar.Battle.Commands
{
    public enum BattleCommandType { SelectDefender, ConfirmPlacement, CancelPlacement, StartNextWave, ActivateFever, PauseBattle, ResumeBattle, RestartBattle, ExitBattle }

    [Serializable]
    public sealed class BattleCommand : IComparable<BattleCommand>
    {
        public string CommandId;
        public string BattleId;
        public BattleCommandType Type;
        public long TargetTick;
        public long Sequence;
        public double IssuedAtUnscaledTime;
        public string DefenderConfigId;
        public string NodeId;

        public int CompareTo(BattleCommand other)
        {
            if (other == null) return 1;
            var tick = TargetTick.CompareTo(other.TargetTick);
            return tick != 0 ? tick : Sequence.CompareTo(other.Sequence);
        }
    }

    public enum CommandDisposition { Accepted, Rejected, Duplicate }
    public readonly struct CommandResult
    {
        public CommandDisposition Disposition { get; }
        public string ReasonCode { get; }
        public CommandResult(CommandDisposition disposition, string reasonCode = null)
        { Disposition = disposition; ReasonCode = reasonCode; }
    }
}

