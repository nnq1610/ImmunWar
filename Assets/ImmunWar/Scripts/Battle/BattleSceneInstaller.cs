using ImmunWar.Battle.Placement;
using ImmunWar.Battle.Fever;
using ImmunWar.Core;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using ImmunWar.StatusEffects;
using UnityEngine;

namespace ImmunWar.Battle
{
    public sealed class BattleSceneInstaller : MonoBehaviour
    {
        [SerializeField] private OrganMapConfig fallbackMap;
        [SerializeField] private BattleHudController hud;
        [SerializeField] private PlacementController placement;
        [SerializeField] private PauseMenuController pause;
        public BattleController Controller { get; private set; }
        public StatusEffectSystem Statuses { get; private set; }
        public InfectionSystem Infections { get; private set; }
        public FeverSystem Fever { get; private set; }

        private void Awake()
        {
            var session = FindFirstObjectByType<GameSession>();
            var map = session?.Catalogs?.Get<OrganMapConfig>(session.SelectedMapId) ?? fallbackMap;
            if (!map)
            {
                var catalog = Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
                if (catalog && catalog.maps != null && catalog.maps.Length > 0) map = catalog.maps[0];
            }
            var mapId = map ? map.Id : "map_lung";
            Controller = gameObject.AddComponent<BattleController>();
            Controller.Configure(mapId, map ? map.startingAtp : 100, map ? map.maximumVitality : 100, 94721);
            var state = Controller.State;
            Statuses = new StatusEffectSystem();
            Infections = new InfectionSystem(Statuses);
            var feverConfig = session?.Catalogs?.Catalog?.fever;
            Fever = new FeverSystem(state.Fever, feverConfig ? feverConfig.maximumCharge : 100, feverConfig ? feverConfig.durationTicks : 300, feverConfig ? feverConfig.damageMultiplier : 1.5f, feverConfig ? feverConfig.speedMultiplier : 1.2f);
            if (map?.nodes != null) foreach (var node in map.nodes) if (node) state.Nodes[node.Id] = new State.DefenseNodeState(node.Id);
            placement?.Initialize(new PlacementSystem(state));
            pause?.Initialize(Controller);
            RefreshHud();
        }

        public void StartBattle() { Controller.StartBattle(); RefreshHud(); }
        public void RefreshHud()
        {
            if (!hud || Controller?.State == null) return;
            var state = Controller.State;
            hud.ApplySnapshot(new BattleHudSnapshot(state.Economy.Atp, state.Vitality.Current, state.Vitality.Maximum, state.Waves.WaveIndex + 1, 1, state.Waves.Phase.ToString(), state.Phase is State.BattlePhase.Victory or State.BattlePhase.Defeat ? state.Phase.ToString() : string.Empty));
        }
    }
}
