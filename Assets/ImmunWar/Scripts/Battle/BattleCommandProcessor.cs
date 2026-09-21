using System;
using System.Collections.Generic;
using ImmunWar.Battle.Commands;
using ImmunWar.Battle.Events;
using ImmunWar.Battle.State;
using ImmunWar.Core;

namespace ImmunWar.Battle
{
    public sealed class BattleCommandQueue
    {
        private readonly List<BattleCommand> _commands = new List<BattleCommand>();
        public int Count => _commands.Count;
        public void Enqueue(BattleCommand command) { _commands.Add(command); _commands.Sort(); }
        public BattleCommand Dequeue() { var value = _commands[0]; _commands.RemoveAt(0); return value; }
        public void Clear() => _commands.Clear();
    }

    public sealed class BattleCommandProcessor
    {
        private readonly BattleState _state;
        private readonly Dictionary<string, CommandResult> _processed = new Dictionary<string, CommandResult>(StringComparer.Ordinal);
        private readonly SequenceId _eventSequence = new SequenceId();
        public event Action<BattleEvent> EventCommitted;
        public BattleCommandProcessor(BattleState state) => _state = state;

        public CommandResult Process(BattleCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.CommandId)) return new CommandResult(CommandDisposition.Rejected, "command_invalid");
            if (_processed.TryGetValue(command.CommandId, out _)) return new CommandResult(CommandDisposition.Duplicate);
            CommandResult result;
            if (!string.Equals(command.BattleId, _state.BattleId, StringComparison.Ordinal)) result = Reject("battle_mismatch");
            else result = command.Type switch
            {
                BattleCommandType.PauseBattle when _state.Phase == BattlePhase.Running => Change(BattlePhase.Paused),
                BattleCommandType.ResumeBattle when _state.Phase == BattlePhase.Paused => Change(BattlePhase.Running),
                BattleCommandType.ExitBattle when _state.Phase != BattlePhase.Exited => Change(BattlePhase.Exited),
                BattleCommandType.CancelPlacement when _state.Phase is not BattlePhase.Victory and not BattlePhase.Defeat and not BattlePhase.Exited => Accept(),
                _ => Reject("state_not_allowed")
            };
            _processed.Add(command.CommandId, result);
            if (result.Disposition == CommandDisposition.Rejected)
                Emit(BattleEventType.CommandRejected, command.CommandId, result.ReasonCode);
            return result;
        }
        private CommandResult Change(BattlePhase phase)
        {
            var previous = _state.Phase;
            _state.Phase = phase;
            Emit(BattleEventType.BattleStateChanged, phase.ToString(), previous.ToString());
            return Accept();
        }
        private void Emit(BattleEventType type, string subject, string reason)
        {
            EventCommitted?.Invoke(new BattleEvent { BattleId = _state.BattleId, Tick = _state.Tick, Sequence = _eventSequence.Next(), Type = type, SubjectId = subject, ReasonCode = reason });
        }
        private static CommandResult Accept() => new CommandResult(CommandDisposition.Accepted);
        private static CommandResult Reject(string reason) => new CommandResult(CommandDisposition.Rejected, reason);
    }
}
