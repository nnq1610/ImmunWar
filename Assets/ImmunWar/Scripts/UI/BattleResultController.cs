using System;
using ImmunWar.Core;
using ImmunWar.Battle.State;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class BattleResultController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text label;
        private bool _shown;
        private BattlePhase _result;
        public event Action<BattlePhase> ResultConfirmed;
        public void Show(BattlePhase result)
        {
            if (_shown || result is not BattlePhase.Victory and not BattlePhase.Defeat) return;
            _shown = true; _result = result; if (root) root.SetActive(true); if (label) label.text = result == BattlePhase.Victory ? "Organ Defended" : "Organ Overrun";
        }
        public void ConfirmAndReturnToMapSelect() { if (!_shown) return; ResultConfirmed?.Invoke(_result); StartCoroutine(SceneFlowService.Load("MainMenu")); }
        public void ResetView() { _shown = false; _result = BattlePhase.Preparing; if (root) root.SetActive(false); }
    }
}
