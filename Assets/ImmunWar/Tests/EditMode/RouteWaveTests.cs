using NUnit.Framework;
using UnityEngine;
using ImmunWar.Battle.Movement;
using ImmunWar.Battle.State;
using ImmunWar.Battle.Waves;

namespace ImmunWar.Tests.EditMode
{
    public sealed class RouteWaveTests
    {
        [Test]
        public void RouteFollower_AdvancesDeterministicallyAndArrivesOnce()
        {
            var enemy = new EnemyState("enemy-1", "ene_virus", "route-1", 10);
            var route = new[] { Vector2.zero, Vector2.right, Vector2.one };
            var follower = new RouteFollower(enemy, route, 1f);
            var arrivals = 0;
            for (var i = 0; i < 100; i++) if (follower.Tick(1f / 30f)) arrivals++;
            Assert.That(enemy.RouteProgress, Is.EqualTo(1f));
            Assert.That(arrivals, Is.EqualTo(1));
        }

        [Test]
        public void WaveSystem_SpawnsInStableOrderAndCompletesAfterLastTerminalEnemy()
        {
            var state = new WaveState();
            var waves = new WaveSystem(state, new[] { new WaveSpawn("ene_virus", "route-1", 2, 1) });
            waves.StartNextWave();
            Assert.That(waves.Tick(1).Count, Is.EqualTo(1));
            Assert.That(waves.Tick(2).Count, Is.EqualTo(1));
            waves.NotifyTerminal();
            waves.NotifyTerminal();
            Assert.That(state.Phase, Is.EqualTo(WavePhase.AllComplete));
        }
    }
}

