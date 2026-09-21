using System.Collections.Generic;

namespace ImmunWar.Battle.State
{
    public enum BattlePhase { Preparing, Running, Paused, Victory, Defeat, Restarting, Exited }

    public sealed class BattleState
    {
        public string BattleId { get; }
        public string MapId { get; }
        public int Seed { get; }
        public long Tick { get; internal set; }
        public BattlePhase Phase { get; internal set; } = BattlePhase.Preparing;
        public EconomyState Economy { get; }
        public VitalityState Vitality { get; }
        public WaveState Waves { get; } = new WaveState();
        public FeverState Fever { get; } = new FeverState();
        public Dictionary<string, DefenseNodeState> Nodes { get; } = new Dictionary<string, DefenseNodeState>();
        public Dictionary<string, DefenderState> Defenders { get; } = new Dictionary<string, DefenderState>();
        public Dictionary<string, EnemyState> Enemies { get; } = new Dictionary<string, EnemyState>();

        public BattleState(string battleId, string mapId, int seed, int startingAtp, int maximumVitality)
        {
            BattleId = battleId;
            MapId = mapId;
            Seed = seed;
            Economy = new EconomyState(startingAtp);
            Vitality = new VitalityState(maximumVitality);
        }
    }
}

