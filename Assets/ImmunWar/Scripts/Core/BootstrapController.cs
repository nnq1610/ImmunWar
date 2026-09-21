using ImmunWar.Core.Config;
using UnityEngine;

namespace ImmunWar.Core
{
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private string firstScene = "MainMenu";

        private void Awake()
        {
            var sessionObject = new GameObject("GameSession");
            var session = sessionObject.AddComponent<GameSession>();
            session.Initialize(catalog);
            StartCoroutine(SceneFlowService.Load(firstScene));
        }
    }
}

