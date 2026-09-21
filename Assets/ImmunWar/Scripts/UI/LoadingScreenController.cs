using ImmunWar.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider progress;
        [SerializeField] private Text errorLabel;
        private void OnEnable() { SceneFlowService.ProgressChanged += OnProgress; SceneFlowService.LoadFailed += OnFailed; }
        private void OnDisable() { SceneFlowService.ProgressChanged -= OnProgress; SceneFlowService.LoadFailed -= OnFailed; }
        private void OnProgress(string _, float value) { if (root) root.SetActive(value < 1f); if (progress) progress.value = value; }
        private void OnFailed(string error) { if (root) root.SetActive(true); if (errorLabel) errorLabel.text = error; }
    }
}
