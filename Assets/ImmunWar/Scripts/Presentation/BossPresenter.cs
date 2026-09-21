using ImmunWar.Combat.Boss;
using UnityEngine;

namespace ImmunWar.Presentation
{
    public sealed class BossPresenter : MonoBehaviour
    {
        public int PresentedPhase { get; private set; }
        public void Bind(BossPhaseController boss) { if (boss != null) boss.PhaseChanged += OnPhaseChanged; }
        private void OnPhaseChanged(BossPhaseEvent value) => PresentedPhase = value.PhaseIndex;
    }
}
