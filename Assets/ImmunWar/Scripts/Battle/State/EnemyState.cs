using System.Collections.Generic;

namespace ImmunWar.Battle.State
{
    public enum EnemyTerminalResult { None, Defeated, ReachedOrgan }

    public sealed class EnemyState
    {
        public string InstanceId { get; }
        public string ConfigId { get; }
        public string RouteId { get; }
        public float Health { get; internal set; }
        public float RouteProgress { get; internal set; }
        public string BlockedById { get; internal set; }
        public EnemyTerminalResult TerminalResult { get; internal set; }
        public List<StatusEffectState> Effects { get; } = new List<StatusEffectState>();
        public bool IsTerminal => TerminalResult != EnemyTerminalResult.None;
        public EnemyState(string instanceId, string configId, string routeId, float health)
        { InstanceId = instanceId; ConfigId = configId; RouteId = routeId; Health = health; }
    }
}

