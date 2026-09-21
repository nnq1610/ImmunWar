using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class BattleAccessibilityPresenter : MonoBehaviour
    {
        [SerializeField] private Image icon; [SerializeField] private Text label;
        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string> { ["infection"] = "INFECTED +", ["mutation"] = "MUTATED ◇", ["fever"] = "FEVER ▲", ["boss"] = "BOSS PHASE", ["affordable"] = "READY ✓", ["victory"] = "VICTORY ★", ["defeat"] = "DEFEAT ×" };
        public string CurrentCue { get; private set; }
        public void Show(string cue, Sprite sprite = null) { CurrentCue = Labels.TryGetValue(cue ?? "", out var value) ? value : cue; if (label) label.text = CurrentCue; if (icon) { icon.sprite = sprite; icon.enabled = sprite; } }
    }
}
