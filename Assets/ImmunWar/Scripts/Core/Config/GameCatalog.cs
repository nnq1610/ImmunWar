using System.Collections.Generic;
using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public DefenderConfig[] defenders;
        public EnemyConfig[] enemies;
        public OrganMapConfig[] maps;
        public StatusEffectConfig[] statusEffects;
        public MutationDefinition[] mutations;
        public FeverConfig fever;

        public IEnumerable<GameConfig> AllConfigs()
        {
            if (defenders != null) foreach (var value in defenders) yield return value;
            if (enemies != null) foreach (var value in enemies) yield return value;
            if (maps != null) foreach (var value in maps) yield return value;
            if (statusEffects != null) foreach (var value in statusEffects) yield return value;
            if (mutations != null) foreach (var value in mutations) yield return value;
            if (fever != null) yield return fever;
        }
    }
}

