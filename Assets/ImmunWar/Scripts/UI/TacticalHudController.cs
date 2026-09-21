using ImmunWar.Battle.Fever;
using ImmunWar.Battle.State;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class TacticalHudController : MonoBehaviour
    {
        [SerializeField] private Slider feverMeter;
        [SerializeField] private Button feverButton;
        [SerializeField] private Text statusLabel;
        private FeverSystem _fever;
        private int _maximumCharge;
        public FeverPhase LastPhase { get; private set; }
        public void Initialize(FeverSystem fever, int maximumCharge) { _fever = fever; _maximumCharge = Mathf.Max(1, maximumCharge); _fever.Changed += Refresh; }
        public void ActivateFever() { _fever?.Activate(); }
        public void ShowStatus(string text) { if (statusLabel) statusLabel.text = text; }
        private void Refresh(FeverState state)
        {
            LastPhase = state.Phase; if (feverMeter) feverMeter.value = state.Charge / (float)_maximumCharge; if (feverButton) feverButton.interactable = state.Phase == FeverPhase.Ready;
        }
        private void OnDestroy() { if (_fever != null) _fever.Changed -= Refresh; }
    }
}

