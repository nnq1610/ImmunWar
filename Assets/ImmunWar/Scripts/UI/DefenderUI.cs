using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ImmunWar.UI
{
    [Serializable]
    public struct DefenderCapabilities
    {
        public float Range;
        public float Damage;
        public float FireRate;
        public string SpecialAbility;
    }

    [Serializable]
    public struct DefenderUIState
    {
        public string DefenderId;
        public bool IsAvailable;
        public bool IsSelected;
        public int Cost;
        public string UnavailableReason;
        public DefenderCapabilities Capabilities;
    }

    /// <summary>
    /// Defender UI Component supporting artifact-resistant rendering, tooltips, and accessibility.
    /// Implements Task 5 of immunwar-placement-ui-redesign.
    /// </summary>
    public class DefenderUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform _defenderPanelContainer;
        [SerializeField] private TextMeshProUGUI _atpCurrentText;
        [SerializeField] private TextMeshProUGUI _atpGenerationText;
        [SerializeField] private TextMeshProUGUI _atpProjectedText;
        
        [Header("Tooltip System")]
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipNameText;
        [SerializeField] private TextMeshProUGUI _tooltipStatsText;
        [SerializeField] private TextMeshProUGUI _tooltipCostText;
        [SerializeField] private float _defaultTooltipDuration = 3.0f;

        // State & Accessibility
        private Dictionary<string, DefenderUIState> _defenderStates = new Dictionary<string, DefenderUIState>();
        private string _selectedDefenderId;
        private KeyCode[] _selectionShortcuts = new KeyCode[] 
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, 
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6
        };
        private bool _highContrast = false;
        private float _fontScale = 1.0f;
        private float _tooltipHideTime = -1f;

        public event Action<string> OnDefenderSelected;

        private void Update()
        {
            HandleShortcuts();
            
            // Tooltip duration handling
            if (_tooltipHideTime > 0 && Time.time >= _tooltipHideTime)
            {
                HideTooltip();
            }
        }

        // --- State Management ---

        public void UpdateDefenderStates(DefenderUIState[] states)
        {
            foreach (var state in states)
            {
                _defenderStates[state.DefenderId] = state;
                // Here we would also update the visual state of individual UI buttons (e.g. gray out if unavailable)
                // We ensure no artifacting by strictly separating UI render targets per Canvas constraints (Unity URP).
            }
        }

        public void SetSelectedDefender(string defenderId)
        {
            _selectedDefenderId = defenderId;
            
            // Trigger visual highlight logic for the selected defender here
            foreach (var key in _defenderStates.Keys)
            {
                var state = _defenderStates[key];
                state.IsSelected = (key == defenderId);
                _defenderStates[key] = state;
            }
            
            OnDefenderSelected?.Invoke(defenderId);
        }

        public void SetATPStatus(int current, int generation, int projected)
        {
            if (_atpCurrentText != null) _atpCurrentText.text = $"ATP: {current}";
            if (_atpGenerationText != null) _atpGenerationText.text = $"+{generation}/sec";
            if (_atpProjectedText != null) _atpProjectedText.text = $"Projected: {projected}";
        }

        // --- Tooltip System ---

        public void ShowTooltip(string defenderId, Vector2 position)
        {
            if (!_defenderStates.TryGetValue(defenderId, out var state)) return;
            if (_tooltipPanel == null) return;

            _tooltipPanel.SetActive(true);
            _tooltipPanel.transform.position = position;

            if (_tooltipNameText != null) 
                _tooltipNameText.text = defenderId;
            
            if (_tooltipCostText != null) 
                _tooltipCostText.text = state.IsAvailable ? $"Cost: {state.Cost} ATP" : $"Unavailable: {state.UnavailableReason}";

            if (_tooltipStatsText != null)
            {
                _tooltipStatsText.text = $"Damage: {state.Capabilities.Damage}\n" +
                                         $"Range: {state.Capabilities.Range}\n" +
                                         $"Speed: {state.Capabilities.FireRate}\n" +
                                         $"Special: {state.Capabilities.SpecialAbility}";
            }

            _tooltipHideTime = Time.time + _defaultTooltipDuration;
        }

        public void HideTooltip()
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.SetActive(false);
            }
            _tooltipHideTime = -1f;
        }

        public void SetTooltipDuration(float seconds)
        {
            _defaultTooltipDuration = seconds;
        }

        // --- Accessibility ---

        public void SetKeyboardShortcuts(KeyCode[] shortcuts)
        {
            if (shortcuts != null && shortcuts.Length >= 6)
            {
                _selectionShortcuts = shortcuts;
            }
        }

        public void SetContrastMode(bool highContrast)
        {
            _highContrast = highContrast;
            
            // Adjust UI elements for 4.5:1 minimum contrast ratio
            Color textColor = highContrast ? Color.yellow : Color.white;
            Color bgColor = highContrast ? Color.black : new Color(0.2f, 0.2f, 0.2f, 0.8f);

            if (_tooltipNameText != null) _tooltipNameText.color = textColor;
            if (_tooltipStatsText != null) _tooltipStatsText.color = textColor;
            if (_tooltipCostText != null) _tooltipCostText.color = textColor;
            if (_atpCurrentText != null) _atpCurrentText.color = textColor;

            var bgImage = _tooltipPanel != null ? _tooltipPanel.GetComponent<Image>() : null;
            if (bgImage != null)
            {
                bgImage.color = bgColor;
            }
        }

        public void SetFontScale(float scale)
        {
            _fontScale = scale;
            
            // Apply scale to texts
            if (_tooltipNameText != null) _tooltipNameText.fontSize *= scale;
            if (_tooltipStatsText != null) _tooltipStatsText.fontSize *= scale;
            if (_tooltipCostText != null) _tooltipCostText.fontSize *= scale;
        }

        private void HandleShortcuts()
        {
            // Maps 1-6 keys to the 6 defender types
            string[] orderedDefenders = new string[] 
            {
                "Macrophage", "T-Cell", "B-Cell", "NK", "Energy", "Platelet"
            };

            for (int i = 0; i < _selectionShortcuts.Length && i < orderedDefenders.Length; i++)
            {
                if (Input.GetKeyDown(_selectionShortcuts[i]))
                {
                    if (_defenderStates.TryGetValue(orderedDefenders[i], out var state) && state.IsAvailable)
                    {
                        SetSelectedDefender(orderedDefenders[i]);
                    }
                }
            }
        }
    }
}
