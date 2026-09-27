using UnityEngine;
using System.Collections;
using System;

namespace ImmunWar.Battle.Tutorial
{
    public enum TutorialState
    {
        NotStarted,
        Intro,
        SelectDefender,
        HoverGrid,
        PlaceDefender,
        StrategicNodeIntro,
        Completed
    }

    /// <summary>
    /// Interactive tutorial for new grid-based placement mechanics.
    /// Implements Task 12.3
    /// </summary>
    public class PlacementTutorialSystem : MonoBehaviour
    {
        private TutorialState _currentState = TutorialState.NotStarted;
        
        [Header("Tutorial UI")]
        [SerializeField] private GameObject _tutorialPanel;
        [SerializeField] private TMPro.TextMeshProUGUI _tutorialText;
        [SerializeField] private GameObject _highlightArrow;

        public event Action<TutorialState> OnTutorialStateChanged;

        public void StartTutorial()
        {
            SetState(TutorialState.Intro);
        }

        private void SetState(TutorialState newState)
        {
            _currentState = newState;
            OnTutorialStateChanged?.Invoke(_currentState);
            
            switch (_currentState)
            {
                case TutorialState.Intro:
                    ShowMessage("Welcome to the new grid placement system! Let's build your defense.");
                    Invoke(nameof(ProgressToSelection), 3f);
                    break;
                case TutorialState.SelectDefender:
                    ShowMessage("Press 1-6 or click a defender in the UI to select it.");
                    HighlightElement("DefenderUI");
                    break;
                case TutorialState.HoverGrid:
                    ShowMessage("Hover over the grid to preview your placement and range.");
                    HighlightElement("GridArea");
                    break;
                case TutorialState.PlaceDefender:
                    ShowMessage("Left click on a valid position (green) to place your defender.");
                    break;
                case TutorialState.StrategicNodeIntro:
                    ShowMessage("Notice the cyan nodes! These are Strategic Nodes offering bonuses like increased range or ATP.");
                    HighlightElement("StrategicNode");
                    Invoke(nameof(CompleteTutorial), 5f);
                    break;
                case TutorialState.Completed:
                    ShowMessage("Tutorial Completed! Good luck defending the organ.");
                    Invoke(nameof(HideTutorial), 2f);
                    break;
            }
        }

        private void ShowMessage(string message)
        {
            if (_tutorialPanel != null) _tutorialPanel.SetActive(true);
            if (_tutorialText != null) _tutorialText.text = message;
        }

        private void HighlightElement(string elementName)
        {
            // Logic to move the _highlightArrow to the specific UI element or grid position
            if (_highlightArrow != null) _highlightArrow.SetActive(true);
        }

        private void HideTutorial()
        {
            if (_tutorialPanel != null) _tutorialPanel.SetActive(false);
            if (_highlightArrow != null) _highlightArrow.SetActive(false);
        }

        // Methods to be called by external events (e.g. PlacementController)
        public void OnDefenderSelected()
        {
            if (_currentState == TutorialState.SelectDefender)
                SetState(TutorialState.HoverGrid);
        }

        public void OnGridHovered()
        {
            if (_currentState == TutorialState.HoverGrid)
                SetState(TutorialState.PlaceDefender);
        }

        public void OnDefenderPlaced()
        {
            if (_currentState == TutorialState.PlaceDefender)
                SetState(TutorialState.StrategicNodeIntro);
        }

        private void ProgressToSelection() => SetState(TutorialState.SelectDefender);
        private void CompleteTutorial() => SetState(TutorialState.Completed);
    }
}
