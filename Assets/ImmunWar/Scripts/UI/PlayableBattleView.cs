using System;
using System.Collections;
using System.Collections.Generic;
using ImmunWar.Battle;
using ImmunWar.Battle.Movement;
using ImmunWar.Battle.Placement;
using ImmunWar.Battle.State;
using ImmunWar.Battle.Waves;
using ImmunWar.Combat;
using ImmunWar.Core;
using ImmunWar.Core.Config;
using ImmunWar.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class PlayableBattleView : MonoBehaviour
    {
        private sealed class EnemyVisual
        {
            public EnemyState State;
            public EnemyConfig Config;
            public RouteFollower Follower;
            public Image Image;
            public AnimatedHealthBar HealthBar;
            public int NextContactTick;
        }

        private BattleSceneInstaller _installer;
        private BattleController _battle;
        private OrganMapConfig _map;
        private GameSession _session;
        private PlacementSystem _placement;
        private PlayableArtCatalog _art;
        private readonly CombatSystem _combat = new CombatSystem();
        private readonly List<EnemyVisual> _enemies = new List<EnemyVisual>();
        private readonly Dictionary<string, Animator> _defenderAnimators = new Dictionary<string, Animator>();
        private readonly Dictionary<string, Image> _defenderImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, AnimatedHealthBar> _defenderHealthBars = new Dictionary<string, AnimatedHealthBar>();
        private readonly Dictionary<string, Button> _defenderButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Vector2> _nodePositions = new Dictionary<string, Vector2>();
        private RectTransform _arena;
        private Text _status;
        private Text _stats;
        private Text _waveLabel;
        private Text _selection;
        private AnimatedHealthBar _organHealth;
        private AnimatedHealthBar _hudOrganHealth;
        private GameObject _resultPanel;
        private Text _resultText;
        private DefenderConfig _selectedDefender;
        private WaveSystem _wave;
        private int _waveIndex = -1;
        private int _tick;
        private int _enemySerial;
        private float _accumulator;
        private bool _resultSaved;

        public BattleController Controller => _battle;
        public int VisibleEnemyCount => _enemies.Count;
        public float FirstEnemyProgress => _enemies.Count == 0 ? 0f : _enemies[0].State.RouteProgress;

        public void AdvanceSimulationTicksForTests(int count)
        {
            for (var i = 0; i < count && _battle && _battle.State.Phase == BattlePhase.Running && _wave != null; i++) Tick();
        }

        private void Start()
        {
            _installer = FindFirstObjectByType<BattleSceneInstaller>();
            _session = FindFirstObjectByType<GameSession>();
            if (!_installer || !_installer.Controller)
            {
                Debug.LogError("Playable battle cannot start: BattleSceneInstaller is missing.");
                return;
            }
            _battle = _installer.Controller;
            var catalog = _session ? _session.Catalogs.Catalog : Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            _map = catalog ? _session ? _session.Catalogs.Get<OrganMapConfig>(_session.SelectedMapId) : catalog.maps[0] : null;
            if (!_map)
            {
                Debug.LogError("Playable battle cannot start: map catalog is missing.");
                return;
            }
            _placement = new PlacementSystem(_battle.State);
            _art = Resources.Load<PlayableArtCatalog>("ImmuneWar/PlayableArtCatalog");
            BuildInterface(catalog);
            SetStatus("Select a defender, place it on a glowing node, then start the wave.");
            RefreshStats();
        }

        private void BuildInterface(GameCatalog catalog)
        {
            var root = RuntimeUi.Root("PlayableBattle");
            RuntimeUi.Image(root, "Background", Vector2.zero, new Vector2(1920, 1080), RuntimeUi.Background);
            RuntimeUi.Image(root, "TopPanel", new Vector2(0, 470), new Vector2(1920, 140), RuntimeUi.Panel);
            RuntimeUi.Text(root, "MapTitle", _map.Id.Replace("map_", "").ToUpperInvariant() + " DEFENSE", new Vector2(-710, 470), new Vector2(440, 85), 41, RuntimeUi.Accent, TextAnchor.MiddleLeft);
            _stats = RuntimeUi.Text(root, "Stats", "", new Vector2(-90, 470), new Vector2(800, 80), 30, Color.white);
            _hudOrganHealth = AnimatedHealthBar.Create(root, "HudOrganHealth", new Vector2(-90, 417),
                new Vector2(440, 18), _battle.State.Vitality.Maximum, _battle.State.Vitality.Current);
            _waveLabel = RuntimeUi.Text(root, "Wave", "", new Vector2(700, 470), new Vector2(400, 80), 28, Color.white);

            RuntimeUi.Image(root, "ArenaPanel", new Vector2(0, 20), new Vector2(1640, 690), new Color(0.045f, 0.115f, 0.17f));
            _arena = RuntimeUi.Rect(root, "Arena", new Vector2(0, 20), new Vector2(1640, 690));
            var mapSprite = _art ? _art.MapSprite(_map.Id) : null;
            if (mapSprite)
            {
                var mapImage = RuntimeUi.Image(_arena, "LungMap", Vector2.zero, new Vector2(1640, 690), new Color(1f, 1f, 1f, 0.72f));
                mapImage.sprite = mapSprite;
            }
            var organImage = RuntimeUi.Image(_arena, "Organ", new Vector2(665, 0), new Vector2(180, 136),
                new Color(0.2f, 0.8f, 0.75f, 0.8f), true);
            if (_art && _art.organCard)
            {
                organImage.sprite = _art.organCard;
                organImage.color = Color.white;
                organImage.preserveAspect = true;
            }
            RuntimeUi.Text(_arena, "OrganLabel", "ORGAN", new Vector2(665, -95), new Vector2(220, 40), 23, RuntimeUi.Accent);
            _organHealth = AnimatedHealthBar.Create(_arena, "OrganHealth", new Vector2(665, 97),
                new Vector2(170, 18), _battle.State.Vitality.Maximum, _battle.State.Vitality.Current);
            if (_map.routes != null)
            {
                foreach (var route in _map.routes)
                {
                    if (!route || route.waypoints == null) continue;
                    for (var i = 1; i < route.waypoints.Length; i++)
                    {
                        var start = RuntimeUi.Point(route.waypoints[i - 1]);
                        var end = RuntimeUi.Point(route.waypoints[i]);
                        RuntimeUi.Line(_arena, "RouteGlow", start, end, 54, new Color(0.09f, 0.27f, 0.32f));
                        RuntimeUi.Line(_arena, "RouteCore", start, end, 14, new Color(0.21f, 0.61f, 0.59f));
                    }
                }
            }
            if (_map.nodes != null)
            {
                foreach (var node in _map.nodes)
                {
                    if (!node) continue;
                    var point = RuntimeUi.Point(node.position);
                    _nodePositions[node.Id] = node.position;
                    var nodeId = node.Id;
                    var mask = node.allowedRoleMask;
                    var button = RuntimeUi.Button(_arena, "Node_" + nodeId, "+", point, new Vector2(90, 90), new Color(0.13f, 0.44f, 0.44f), () => Place(nodeId, mask), 47);
                    button.GetComponent<Image>().sprite = RuntimeUi.Image(_arena, "CircleTemplate", new Vector2(-2000, -2000), new Vector2(1, 1), Color.clear, true).sprite;
                }
            }

            RuntimeUi.Image(root, "BottomPanel", new Vector2(0, -445), new Vector2(1920, 190), RuntimeUi.Panel);
            _status = RuntimeUi.Text(root, "Status", "", new Vector2(0, -355), new Vector2(1700, 55), 24, new Color(0.82f, 0.92f, 0.94f));
            var defenders = catalog.defenders;
            var shown = Math.Min(6, defenders == null ? 0 : defenders.Length);
            for (var i = 0; i < shown; i++)
            {
                var defender = defenders[i];
                if (!defender) continue;
                var name = defender.Id.Replace("def_", "").ToUpperInvariant();
                var caption = name + "\n" + defender.atpCost + " ATP";
                var button = RuntimeUi.Button(root, "Defender_" + defender.Id, caption, new Vector2(-790 + i * 264, -445), new Vector2(245, 115), new Color(0.1f, 0.34f, 0.38f), () => SelectDefender(defender), 23);
                RuntimeUi.Skin(button, _art, UiButtonRole.Card);
                _defenderButtons[defender.Id] = button;
            }
            _selection = RuntimeUi.Text(root, "Selection", "NO DEFENDER", new Vector2(785, -442), new Vector2(300, 95), 23, RuntimeUi.Accent);
            RuntimeUi.Skin(RuntimeUi.Button(root, "StartWave", "START WAVE", new Vector2(-610, 365), new Vector2(260, 70), new Color(0.12f, 0.48f, 0.38f), StartNextWave, 25), _art, UiButtonRole.Primary);
            RuntimeUi.Skin(RuntimeUi.Button(root, "Pause", "PAUSE", new Vector2(-320, 365), new Vector2(220, 70), new Color(0.2f, 0.34f, 0.45f), TogglePause, 25), _art, UiButtonRole.Secondary);
            RuntimeUi.Skin(RuntimeUi.Button(root, "Restart", "RESTART", new Vector2(-70, 365), new Vector2(220, 70), new Color(0.2f, 0.34f, 0.45f), Restart, 25), _art, UiButtonRole.Danger);
            RuntimeUi.Skin(RuntimeUi.Button(root, "Menu", "MENU", new Vector2(180, 365), new Vector2(220, 70), new Color(0.2f, 0.34f, 0.45f), ReturnToMenu, 25), _art, UiButtonRole.Secondary);
            var result = RuntimeUi.Rect(root, "ResultPanel", Vector2.zero, new Vector2(900, 470));
            _resultPanel = result.gameObject;
            RuntimeUi.Image(result, "Shade", Vector2.zero, new Vector2(900, 470), new Color(0.035f, 0.12f, 0.17f, 0.97f));
            _resultText = RuntimeUi.Text(result, "ResultText", "", new Vector2(0, 100), new Vector2(780, 125), 52, RuntimeUi.Accent);
            RuntimeUi.Skin(RuntimeUi.Button(result, "PlayAgain", "PLAY AGAIN", new Vector2(-205, -100), new Vector2(320, 90), new Color(0.12f, 0.48f, 0.38f), Restart, 30), _art, UiButtonRole.Primary);
            RuntimeUi.Skin(RuntimeUi.Button(result, "BackToMenu", "MENU", new Vector2(205, -100), new Vector2(320, 90), new Color(0.2f, 0.34f, 0.45f), ReturnToMenu, 30), _art, UiButtonRole.Secondary);
            _resultPanel.SetActive(false);
        }

        public void SelectDefender(DefenderConfig defender)
        {
            _selectedDefender = defender;
            foreach (var pair in _defenderButtons)
                RuntimeUi.Skin(pair.Value, _art, UiButtonRole.Card, defender && pair.Key == defender.Id);
            if (_selection) _selection.text = defender ? defender.Id.Replace("def_", "").ToUpperInvariant() : "NO DEFENDER";
            SetStatus(defender ? "Click a glowing node to place " + defender.Id.Replace("def_", "") + "." : "Select a defender.");
        }

        public bool Place(string nodeId, DefenderRoleMask mask)
        {
            if (!_selectedDefender || _battle.State.Phase == BattlePhase.Paused) { SetStatus("Select a defender while the battle is active."); return false; }
            var config = _selectedDefender;
            var result = _placement.TryPlace(Guid.NewGuid().ToString("N"), config.Id, config.role, nodeId, mask, config.atpCost, config.maxHealth);
            if (!result.Accepted) { SetStatus("Cannot place: " + result.ReasonCode.Replace('_', ' ')); return false; }
            var point = RuntimeUi.Point(_nodePositions[nodeId]);
            var visual = RuntimeUi.Image(_arena, "Placed_" + result.DefenderInstanceId, point, new Vector2(120, 120), DefenderColor(config.role), true);
            if (config.presentation && config.presentation.icon)
            {
                visual.sprite = config.presentation.icon;
                visual.color = Color.white;
                visual.preserveAspect = true;
            }
            else RuntimeUi.Text(_arena, "PlacedLetter", config.Id.Replace("def_", "").Substring(0, 1).ToUpperInvariant(), point, new Vector2(72, 72), 33, Color.white);
            var controller = _art ? _art.DefenderController(config.Id) : null;
            if (controller)
            {
                var animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                _defenderAnimators[result.DefenderInstanceId] = animator;
            }
            _defenderImages[result.DefenderInstanceId] = visual;
            _defenderHealthBars[result.DefenderInstanceId] = AnimatedHealthBar.Create(visual.transform,
                "DefenderHealth", new Vector2(0f, 79f), new Vector2(112f, 15f), config.maxHealth, config.maxHealth);
            SetStatus(config.Id.Replace("def_", "") + " placed. " + _battle.State.Economy.Atp + " ATP remaining.");
            RefreshStats();
            return true;
        }

        private static Color DefenderColor(DefenderRole role) => role switch
        {
            DefenderRole.Damage => new Color(0.29f, 0.65f, 1f),
            DefenderRole.Economy => new Color(1f, 0.78f, 0.29f),
            DefenderRole.Repair => new Color(0.53f, 0.9f, 0.53f),
            _ => new Color(0.26f, 0.87f, 0.7f)
        };

        public void StartNextWave()
        {
            if (_battle.State.Phase == BattlePhase.Paused || _battle.State.Phase is BattlePhase.Victory or BattlePhase.Defeat || _wave != null) return;
            if (_map.waveSet?.waves == null || _waveIndex + 1 >= _map.waveSet.waves.Length) return;
            _waveIndex++;
            var groups = _map.waveSet.waves[_waveIndex].groups;
            var spawns = new List<WaveSpawn>();
            if (groups != null) foreach (var group in groups) if (group?.enemy) spawns.Add(new WaveSpawn(group.enemy.Id, group.routeId, group.count, group.intervalTicks));
            _wave = new WaveSystem(_battle.State.Waves, spawns.ToArray());
            _battle.State.Waves.Phase = WavePhase.Waiting;
            _wave.StartNextWave();
            _battle.State.Waves.WaveIndex = _waveIndex;
            if (_battle.State.Phase == BattlePhase.Preparing) _installer.StartBattle();
            _tick = 0;
            SetStatus("Wave " + (_waveIndex + 1) + " incoming! Defend the organ.");
            RefreshStats();
        }

        public void TogglePause()
        {
            if (_battle.State.Phase == BattlePhase.Running) { _battle.Pause(); SetStatus("Paused. Press PAUSE again to resume."); }
            else if (_battle.State.Phase == BattlePhase.Paused) { _battle.Resume(); SetStatus("Battle resumed."); }
        }

        public void Restart() => StartCoroutine(SceneFlowService.Load("Battle"));
        public void ReturnToMenu() => StartCoroutine(SceneFlowService.Load("MainMenu"));

        private void Update()
        {
            if (!_battle || !_map || _battle.State.Phase != BattlePhase.Running || _wave == null) return;
            _accumulator += Time.deltaTime;
            var count = 0;
            while (_accumulator >= 1f / 30f && count++ < 5)
            {
                _accumulator -= 1f / 30f;
                Tick();
            }
        }

        private void Tick()
        {
            _tick++;
            _battle.Advance(1d / 30d);
            foreach (var request in _wave.Tick(_tick)) Spawn(request);
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                if (enemy.Follower.Tick(1f / 30f))
                {
                    _battle.DamageOrgan(enemy.Config.organDamage);
                    _organHealth.SetValue(_battle.State.Vitality.Current, _battle.State.Vitality.Maximum);
                    _hudOrganHealth.SetValue(_battle.State.Vitality.Current, _battle.State.Vitality.Maximum);
                    _wave.NotifyTerminal();
                    RemoveEnemy(i);
                    continue;
                }
                enemy.Image.rectTransform.anchoredPosition = RuntimeUi.Point(enemy.Follower.Position) + new Vector2(0, Mathf.Sin((_tick + i * 11) * 0.16f) * 5f);
                enemy.Image.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin((_tick + i * 13) * 0.12f) * 0.035f);
                if (_tick >= enemy.NextContactTick)
                {
                    foreach (var defender in _battle.State.Defenders.Values)
                    {
                        if (defender.Health <= 0f || !_nodePositions.TryGetValue(defender.NodeId, out var defenderPoint) ||
                            Vector2.Distance(defenderPoint, enemy.Follower.Position) > 1.3f) continue;
                        defender.ReceiveDamage(Mathf.Max(2f, enemy.Config.organDamage * 0.5f));
                        ShowEffect(_art ? _art.hit : null, defenderPoint, new Color(1f, 0.46f, 0.51f, 0.72f));
                        if (_defenderHealthBars.TryGetValue(defender.InstanceId, out var healthBar))
                            healthBar.SetValue(defender.Health, FindDefender(defender.ConfigId)?.maxHealth ?? 100);
                        enemy.NextContactTick = _tick + 30;
                        break;
                    }
                }
            }
            var defeatedDefenders = new List<string>();
            foreach (var defender in _battle.State.Defenders.Values)
                if (defender.Health <= 0f) defeatedDefenders.Add(defender.InstanceId);
            foreach (var id in defeatedDefenders) RemoveDefender(id);
            foreach (var defender in _battle.State.Defenders.Values)
            {
                CombatSystem.TickCooldown(defender);
                var config = FindDefender(defender.ConfigId);
                if (!config || !_nodePositions.TryGetValue(defender.NodeId, out var position)) continue;
                if (config.role == DefenderRole.Economy && _tick % 90 == 0) _battle.State.Economy.Add(5);
                if (config.role == DefenderRole.Repair && _tick % 120 == 0) _battle.State.Vitality.Repair(3);
                if (config.attackDamage <= 0) continue;
                var candidates = new List<EnemyState>();
                foreach (var enemy in _enemies)
                    if (!enemy.State.IsTerminal && Vector2.Distance(position, enemy.Follower.Position) <= config.range) candidates.Add(enemy.State);
                var target = _combat.Attack(defender, candidates, config.attackDamage, Mathf.CeilToInt(config.attackInterval * 30f));
                if (target != null)
                {
                    if (_defenderAnimators.TryGetValue(defender.InstanceId, out var animator) && animator)
                        animator.Play("Attack", 0, 0f);
                    var hitEnemy = _enemies.Find(x => x.State == target);
                    if (hitEnemy != null) ShowEffect(_art ? _art.hit : null, hitEnemy.Follower.Position, new Color(0.65f, 1f, 0.9f));
                }
                if (target != null && target.IsTerminal)
                {
                    var enemyIndex = _enemies.FindIndex(x => x.State == target);
                    if (enemyIndex >= 0)
                    {
                        _battle.State.Economy.Add(_enemies[enemyIndex].Config.atpReward);
                        ShowEffect(_art ? _art.atp : null, _enemies[enemyIndex].Follower.Position, RuntimeUi.Accent);
                        _wave.NotifyTerminal();
                        RemoveEnemy(enemyIndex);
                    }
                }
            }
            if (_battle.State.Phase == BattlePhase.Defeat) { ShowResult(false); return; }
            if (_wave != null && _battle.State.Waves.Phase == WavePhase.AllComplete)
            {
                _wave = null;
                if (_waveIndex + 1 >= _map.waveSet.waves.Length)
                {
                    _battle.CompleteAllWavesForTests();
                    ShowResult(true);
                }
                else SetStatus("Wave clear! Place more defenders, then press START WAVE.");
            }
            if (_tick % 10 == 0) RefreshStats();
            foreach (var enemy in _enemies)
                enemy.HealthBar.SetValue(enemy.State.Health, enemy.Config.maxHealth);
        }

        private void Spawn(SpawnRequest request)
        {
            var config = _session ? _session.Catalogs.Get<EnemyConfig>(request.EnemyConfigId) : FindEnemy(request.EnemyConfigId);
            RouteConfig route = null;
            if (_map.routes != null) foreach (var item in _map.routes) if (item && item.Id == request.RouteId) { route = item; break; }
            if (!config || !route || route.waypoints == null || route.waypoints.Length < 2) return;
            var state = new EnemyState("enemy-" + (++_enemySerial), config.Id, route.Id, config.maxHealth);
            _battle.State.Enemies[state.InstanceId] = state;
            var follower = new RouteFollower(state, route.waypoints, config.moveSpeed);
            var image = RuntimeUi.Image(_arena, "Enemy_" + state.InstanceId, RuntimeUi.Point(follower.Position), new Vector2(110, 110), new Color(0.94f, 0.29f, 0.42f), true);
            if (config.presentation && config.presentation.icon)
            {
                image.sprite = config.presentation.icon;
                image.color = Color.white;
                image.preserveAspect = true;
            }
            var controller = _art ? _art.EnemyController(config.Id) : null;
            if (controller)
            {
                var animator = image.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }
            var health = AnimatedHealthBar.Create(image.transform, "EnemyHealth",
                new Vector2(0f, 70f), new Vector2(112f, 15f), config.maxHealth, config.maxHealth);
            _enemies.Add(new EnemyVisual { State = state, Config = config, Follower = follower, Image = image, HealthBar = health });
        }

        private void ShowEffect(Sprite sprite, Vector2 position, Color tint)
        {
            var image = RuntimeUi.Image(_arena, "ImpactVfx", RuntimeUi.Point(position), new Vector2(105, 105), tint, !sprite);
            if (sprite) { image.sprite = sprite; image.preserveAspect = true; }
            StartCoroutine(FadeEffect(image));
        }

        private static IEnumerator FadeEffect(Image image)
        {
            const float duration = 0.35f;
            var elapsed = 0f;
            while (image && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                image.rectTransform.localScale = Vector3.one * (0.65f + t * 0.8f);
                var color = image.color;
                color.a = 1f - t;
                image.color = color;
                yield return null;
            }
            if (image) Destroy(image.gameObject);
        }

        private void RemoveEnemy(int index)
        {
            var enemy = _enemies[index];
            _battle.State.Enemies.Remove(enemy.State.InstanceId);
            if (enemy.Image) Destroy(enemy.Image.gameObject);
            _enemies.RemoveAt(index);
        }

        private void RemoveDefender(string id)
        {
            if (!_battle.State.Defenders.TryGetValue(id, out var defender)) return;
            if (_defenderImages.TryGetValue(id, out var image) && image)
            {
                ShowEffect(_art ? _art.hit : null, _nodePositions[defender.NodeId], new Color(1f, 0.4f, 0.43f));
                Destroy(image.gameObject);
            }
            _placement.Remove(defender.NodeId);
            _defenderAnimators.Remove(id);
            _defenderImages.Remove(id);
            _defenderHealthBars.Remove(id);
        }

        private DefenderConfig FindDefender(string id)
        {
            var catalog = _session ? _session.Catalogs.Catalog : Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            if (catalog?.defenders != null) foreach (var config in catalog.defenders) if (config && config.Id == id) return config;
            return null;
        }

        private EnemyConfig FindEnemy(string id)
        {
            var catalog = Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            if (catalog?.enemies != null) foreach (var config in catalog.enemies) if (config && config.Id == id) return config;
            return null;
        }

        private void ShowResult(bool victory)
        {
            if (_resultPanel.activeSelf) return;
            _resultPanel.SetActive(true);
            _resultText.text = victory ? "ORGAN DEFENDED" : "ORGAN OVERRUN";
            if (victory && !_resultSaved && _session != null)
            {
                _resultSaved = true;
                var maps = _session.Catalogs.Catalog.maps;
                var ids = Array.ConvertAll(maps, x => x.Id);
                var progress = new CampaignProgressionService(ids, _session.Progress);
                if (progress.CompleteMap(_map.Id)) _session.Saves.Save(progress.Data);
            }
            RefreshStats();
        }

        private void SetStatus(string text) { if (_status) _status.text = text; }

        private void RefreshStats()
        {
            if (!_battle || !_stats) return;
            var state = _battle.State;
            _stats.text = "ATP  " + state.Economy.Atp + "       ORGAN  " + state.Vitality.Current + "/" + state.Vitality.Maximum;
            _waveLabel.text = "WAVE " + Math.Max(0, _waveIndex + 1) + "/" + (_map.waveSet?.waves?.Length ?? 0);
            if (_organHealth) _organHealth.SetValue(state.Vitality.Current, state.Vitality.Maximum);
            if (_hudOrganHealth) _hudOrganHealth.SetValue(state.Vitality.Current, state.Vitality.Maximum);
        }
    }
}
