using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class TutorialPromptController : MonoBehaviour
    {
        [SerializeField] private Text label;
        private readonly HashSet<string> _shown = new HashSet<string>();
        public bool ShowOnce(string id, string copy) { if (string.IsNullOrWhiteSpace(id) || !_shown.Add(id)) return false; if (label) label.text = copy; gameObject.SetActive(true); return true; }
        public void Dismiss() => gameObject.SetActive(false);
    }
}
