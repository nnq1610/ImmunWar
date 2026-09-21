using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Mutation")]
    public sealed class MutationDefinition : GameConfig
    {
        [Min(0f)] public float weight = 1f;
        [Min(0f)] public float healthMultiplier = 1f;
        [Min(0f)] public float speedMultiplier = 1f;
        public string[] immunityStatusIds;
    }
}

