using System;

namespace ImmunWar.Battle.Events
{
    public enum BattleEventType
    {
        BattleStateChanged, CommandRejected, PlacementPreviewChanged, DefenderPlaced, AtpChanged,
        VitalityChanged, EnemySpawned, EnemyDamaged, EnemyDefeated, EnemyReachedOrgan, WaveChanged,
        InfectionChanged, MutationChanged, FeverChanged, BossPhaseChanged, BattleEnded
    }

    [Serializable]
    public sealed class BattleEvent
    {
        public string BattleId;
        public long Tick;
        public long Sequence;
        public BattleEventType Type;
        public string SubjectId;
        public string ReasonCode;
        public int PreviousInt;
        public int CurrentInt;
        public float Amount;
        public string Payload;
    }

    public interface IBattleEventSink
    {
        void Publish(BattleEvent battleEvent);
    }
}
