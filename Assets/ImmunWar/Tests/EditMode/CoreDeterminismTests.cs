using System.Collections.Generic;
using NUnit.Framework;
using ImmunWar.Core;

namespace ImmunWar.Tests.EditMode
{
    public sealed class CoreDeterminismTests
    {
        [Test]
        public void StableId_RejectsEmptyAndNormalizesWhitespace()
        {
            Assert.That(new StableId("  map_lung ").Value, Is.EqualTo("map_lung"));
            Assert.Throws<System.ArgumentException>(() => new StableId(" "));
        }

        [Test]
        public void FixedClock_AdvancesAtThirtyHertzWithoutDroppingRemainder()
        {
            var clock = new FixedSimulationClock(30);
            Assert.That(clock.Accumulate(0.1), Is.EqualTo(3));
            Assert.That(clock.Tick, Is.EqualTo(3));
            Assert.That(clock.Accumulate(1.0 / 60.0), Is.EqualTo(0));
            Assert.That(clock.Accumulate(1.0 / 60.0), Is.EqualTo(1));
        }

        [Test]
        public void SeededRandom_ReplaysIdenticalSequence()
        {
            var first = new SeededRandomSource(94721);
            var second = new SeededRandomSource(94721);
            var a = new List<int>();
            var b = new List<int>();
            for (var i = 0; i < 64; i++)
            {
                a.Add(first.Range(0, 10000));
                b.Add(second.Range(0, 10000));
            }
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void SequenceId_IsMonotonicAndResettable()
        {
            var sequence = new SequenceId();
            Assert.That(sequence.Next(), Is.EqualTo(1));
            Assert.That(sequence.Next(), Is.EqualTo(2));
            sequence.Reset();
            Assert.That(sequence.Next(), Is.EqualTo(1));
        }
    }
}

