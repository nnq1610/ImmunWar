using NUnit.Framework;
using ImmunWar.Battle;
using ImmunWar.Battle.Commands;
using ImmunWar.Battle.State;

namespace ImmunWar.Tests.EditMode
{
    public sealed class BattleCommandContractTests
    {
        [Test]
        public void DuplicateCommand_ReturnsOriginalDispositionWithoutApplyingTwice()
        {
            var battle = new BattleState("battle-1", "map_lung", 1, 100, 100);
            battle.Phase = BattlePhase.Running;
            var processor = new BattleCommandProcessor(battle);
            var command = new BattleCommand { CommandId = "cmd-1", BattleId = battle.BattleId, Type = BattleCommandType.PauseBattle, TargetTick = 1, Sequence = 1 };
            Assert.That(processor.Process(command).Disposition, Is.EqualTo(CommandDisposition.Accepted));
            Assert.That(processor.Process(command).Disposition, Is.EqualTo(CommandDisposition.Duplicate));
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Paused));
        }

        [Test]
        public void Commands_AreSortedByTickThenSequence()
        {
            var queue = new BattleCommandQueue();
            queue.Enqueue(new BattleCommand { CommandId = "b", TargetTick = 2, Sequence = 1 });
            queue.Enqueue(new BattleCommand { CommandId = "a", TargetTick = 1, Sequence = 2 });
            Assert.That(queue.Dequeue().CommandId, Is.EqualTo("a"));
        }
    }
}

