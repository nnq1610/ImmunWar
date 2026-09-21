using System.Collections.Generic;

namespace ImmunWar.Battle.State
{
    public sealed class DefenderState
    {
        public string InstanceId { get; }
        public string ConfigId { get; }
        public string NodeId { get; }
        public float Health { get; internal set; }
        public int CooldownTicks { get; internal set; }
        public List<StatusEffectState> Effects { get; } = new List<StatusEffectState>();
        public DefenderState(string instanceId, string configId, string nodeId, float health)
        { InstanceId = instanceId; ConfigId = configId; NodeId = nodeId; Health = health; }

        public float ReceiveDamage(float amount)
        {
            if (amount > 0f) Health = System.Math.Max(0f, Health - amount);
            return Health;
        }
    }
}
