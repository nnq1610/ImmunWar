using ImmunWar.Battle.Events;
using ImmunWar.Audio;
using UnityEngine;

namespace ImmunWar.Presentation
{
    public sealed class P0BattleFeedback : MonoBehaviour, IBattleEventSink
    {
        [SerializeField] private SfxService sfx;
        public void Publish(BattleEvent battleEvent)
        {
            if (battleEvent == null) return;
            if (battleEvent.Type == BattleEventType.EnemyDefeated) sfx?.Play("AUD006");
            else if (battleEvent.Type == BattleEventType.EnemyDamaged) sfx?.Play("AUD005");
        }
    }
}
