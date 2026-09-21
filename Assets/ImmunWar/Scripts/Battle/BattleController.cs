using System;
using ImmunWar.Battle.State;
using ImmunWar.Core;
using UnityEngine;

namespace ImmunWar.Battle
{
    public sealed class BattleController : MonoBehaviour
    {
        private int _startingAtp;
        private int _maximumVitality;
        private string _mapId;
        private int _attempt;
        private readonly FixedSimulationClock _clock = new FixedSimulationClock(30);
        public BattleState State { get; private set; }
        public event Action<BattlePhase, BattlePhase> PhaseChanged;

        public static BattleController CreateForTests(string mapId, int startingAtp, int maximumVitality)
        {
            var go = new GameObject("BattleController");
            var controller = go.AddComponent<BattleController>();
            controller.Configure(mapId, startingAtp, maximumVitality, 1);
            return controller;
        }

        public void Configure(string mapId, int startingAtp, int maximumVitality, int seed)
        {
            _mapId = mapId; _startingAtp = startingAtp; _maximumVitality = maximumVitality; _attempt++;
            State = new BattleState($"{mapId}-attempt-{_attempt:D3}", mapId, seed, startingAtp, maximumVitality);
            _clock.Reset();
        }

        public void StartBattle() => ChangePhase(BattlePhase.Running);
        public void Pause() { if (State.Phase == BattlePhase.Running) ChangePhase(BattlePhase.Paused); }
        public void Resume() { if (State.Phase == BattlePhase.Paused) ChangePhase(BattlePhase.Running); }
        public void Restart() { var seed = State.Seed; Configure(_mapId, _startingAtp, _maximumVitality, seed); }
        public void CompleteAllWavesForTests() { State.Waves.Phase = WavePhase.AllComplete; ChangePhase(BattlePhase.Victory); }
        public void DamageOrgan(int amount)
        {
            if (State.Phase != BattlePhase.Running) return;
            State.Vitality.Damage(amount);
            if (State.Vitality.IsDepleted) ChangePhase(BattlePhase.Defeat);
        }
        public void Advance(double seconds)
        {
            if (State.Phase != BattlePhase.Running) return;
            var ticks = _clock.Accumulate(seconds);
            if (ticks > 0) State.Tick = _clock.Tick;
        }
        private void ChangePhase(BattlePhase next)
        {
            if (State == null || State.Phase == next) return;
            var previous = State.Phase; State.Phase = next; PhaseChanged?.Invoke(previous, next);
        }
        private void OnDestroy() => PhaseChanged = null;
    }
}
