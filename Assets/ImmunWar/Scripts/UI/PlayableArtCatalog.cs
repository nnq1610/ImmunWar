using System;
using UnityEngine;

namespace ImmunWar.UI
{
    [Serializable]
    public sealed class UnitAnimationArt
    {
        public string unitId;
        public Sprite[] idleOrMove;
        public Sprite[] action;
        public RuntimeAnimatorController controller;
    }

    public sealed class PlayableArtCatalog : ScriptableObject
    {
        public Sprite hit;
        public Sprite pulse;
        public Sprite atp;
        public Sprite lungMap;
        public Sprite brainMap;
        public Sprite stomachMap;
        public Sprite primaryButton;
        public Sprite secondaryButton;
        public Sprite dangerButton;
        public Sprite panel;
        public Sprite organCard;
        public Sprite[] macrophageIdle;
        public Sprite[] macrophageAttack;
        public Sprite[] virusMove;
        public RuntimeAnimatorController macrophageController;
        public RuntimeAnimatorController virusController;
        public UnitAnimationArt[] defenderAnimations;
        public UnitAnimationArt[] enemyAnimations;

        public Sprite MapSprite(string mapId) => mapId switch
        {
            "map_brain" => brainMap ? brainMap : lungMap,
            "map_stomach" => stomachMap ? stomachMap : lungMap,
            _ => lungMap
        };

        public RuntimeAnimatorController DefenderController(string unitId)
        {
            if (defenderAnimations != null)
                foreach (var item in defenderAnimations)
                    if (item != null && item.unitId == unitId && item.controller) return item.controller;
            return unitId == "def_macrophage" ? macrophageController : null;
        }

        public RuntimeAnimatorController EnemyController(string unitId)
        {
            if (enemyAnimations != null)
                foreach (var item in enemyAnimations)
                    if (item != null && item.unitId == unitId && item.controller) return item.controller;
            return unitId == "ene_virus" ? virusController : null;
        }
    }
}
