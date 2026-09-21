using ImmunWar.Battle;
using UnityEngine;

namespace ImmunWar.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        private BattleController _battle;
        public void Initialize(BattleController battle) => _battle = battle;
        public void Pause() { _battle?.Pause(); if (root) root.SetActive(true); }
        public void Resume() { _battle?.Resume(); if (root) root.SetActive(false); }
        public void Restart() { _battle?.Restart(); if (root) root.SetActive(false); }
    }
}

