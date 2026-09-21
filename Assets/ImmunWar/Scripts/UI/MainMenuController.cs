using ImmunWar.Core;
using UnityEngine;

namespace ImmunWar.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public void OpenCampaign() => StartCoroutine(SceneFlowService.Load("MainMenu"));
        public void StartSelectedBattle() => StartCoroutine(SceneFlowService.Load("Battle"));
        public void Quit() => Application.Quit();
    }
}
