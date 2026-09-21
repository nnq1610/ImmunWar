using System;
using System.Collections.Generic;
using ImmunWar.Battle.State;

namespace ImmunWar.Battle.Waves
{
    public readonly struct WaveSpawn
    {
        public string EnemyConfigId { get; } public string RouteId { get; } public int Count { get; } public int IntervalTicks { get; }
        public WaveSpawn(string enemyConfigId, string routeId, int count, int intervalTicks)
        { EnemyConfigId = enemyConfigId; RouteId = routeId; Count = Math.Max(0, count); IntervalTicks = Math.Max(1, intervalTicks); }
    }

    public readonly struct SpawnRequest
    {
        public string EnemyConfigId { get; } public string RouteId { get; } public int SpawnIndex { get; }
        public SpawnRequest(string configId, string routeId, int index) { EnemyConfigId = configId; RouteId = routeId; SpawnIndex = index; }
    }

    public sealed class WaveSystem
    {
        private readonly WaveState _state;
        private readonly WaveSpawn[] _spawns;
        private int _groupIndex;
        private int _spawnedInGroup;
        private int _spawnSequence;
        public WaveSystem(WaveState state, WaveSpawn[] spawns) { _state = state; _spawns = spawns ?? Array.Empty<WaveSpawn>(); _state.RemainingConfigured = TotalCount(); }
        public bool StartNextWave()
        {
            if (_state.Phase is WavePhase.Spawning or WavePhase.Active or WavePhase.AllComplete) return false;
            _state.WaveIndex = 0; _state.Phase = WavePhase.Spawning; return true;
        }
        public IReadOnlyList<SpawnRequest> Tick(long tick)
        {
            var result = new List<SpawnRequest>();
            if (_state.Phase != WavePhase.Spawning || _groupIndex >= _spawns.Length) return result;
            var group = _spawns[_groupIndex];
            if (tick % group.IntervalTicks == 0)
            {
                result.Add(new SpawnRequest(group.EnemyConfigId, group.RouteId, ++_spawnSequence));
                _spawnedInGroup++; _state.ActiveEnemies++; _state.RemainingConfigured--;
                if (_spawnedInGroup >= group.Count) { _groupIndex++; _spawnedInGroup = 0; }
                if (_groupIndex >= _spawns.Length) _state.Phase = _state.ActiveEnemies > 0 ? WavePhase.Active : WavePhase.AllComplete;
            }
            return result;
        }
        public void NotifyTerminal()
        {
            _state.ActiveEnemies = Math.Max(0, _state.ActiveEnemies - 1);
            if (_state.RemainingConfigured == 0 && _state.ActiveEnemies == 0) _state.Phase = WavePhase.AllComplete;
        }
        private int TotalCount() { var count = 0; foreach (var spawn in _spawns) count += spawn.Count; return count; }
    }
}
