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
using UnityEngine.EventSystems;
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
            // Presentation-only state, animated every frame in AnimateEnemies.
            public Vector2 BasePoint;
            public Vector2 Knock;
            public float HitTime = -10f;
            public float Phase;
            public Color BaseColor = Color.white;
            public RouteConfig Route;
            public Vector2 Stretch = Vector2.one;
            public float Size;
            public Image Aura;
            public float TrailTime;
            public float Facing = 1f;
            public bool HasOwnArt;
        }

        // Radius of the organ's ring in world units (core art is 260 px, its ring ~60% of that).
        private const float CoreRadius = 1.05f;
        // Any living defender within this distance stops an enemy (world units).
        private const float BlockRadius = 1.3f;
        private BattleAudio _audio;

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
        private AnimatedHealthBar _hudOrganHealth;
        private GameObject _resultPanel;
        private GameObject _pausePanel;
        private Text _resultText;
        private DefenderConfig _selectedDefender;
        private WaveSystem _wave;
        private int _waveIndex = -1;
        private int _tick;
        private int _enemySerial;
        private float _accumulator;
        private bool _resultSaved;
        private Image _core;
        private Image _coreGlow;
        private float _coreHitTime = -10f;
        private Vector2 _coreHitDirection = Vector2.up;
        private readonly HashSet<Image> _flashingDefenders = new HashSet<Image>();
        private static Sprite _glowSprite;
        private static Sprite _ringSprite;

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
            _audio = BattleAudio.Create(gameObject, _art, _session ? _session.Progress : null);
            _audio.PlayMusic(_art ? _art.battleMusic : null);

            _battle.State.Economy.Add(250); // Starting ATP bonus
            SetStatus("Prepare your defenses! Click a node or anywhere on the map to drop defenders.");
            RefreshStats();
            
            StartCoroutine(AutoNextWaveRoutine(6f));
        }

        private IEnumerator AutoNextWaveRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextWave();
        }

        private void BuildInterface(GameCatalog catalog)
        {
            var root = RuntimeUi.Root("PlayableBattle");

            // Fullscreen map background (no black edges)
            _arena = RuntimeUi.Rect(root, "Arena", Vector2.zero, new Vector2(1920, 1080));
            var mapSprite = _art ? _art.MapSprite(_map.Id) : null;
            if (mapSprite)
            {
                var mapImage = RuntimeUi.Image(_arena, "MapBg", Vector2.zero, new Vector2(1920, 1080), Color.white);
                mapImage.sprite = mapSprite;
                mapImage.preserveAspect = false;
            }

            // Organ Core at center (0,0) — this is where all routes converge.
            // A soft glow behind it beats with the core and shifts green → yellow → red with vitality.
            _coreGlow = RuntimeUi.Image(_arena, "OrganGlow", Vector2.zero, new Vector2(340, 340), new Color(0.35f, 1f, 0.6f, 0.3f));
            _coreGlow.sprite = GlowSprite;
            var organImage = RuntimeUi.Image(_arena, "OrganCore", Vector2.zero, new Vector2(260, 260), Color.white, true);
            var specificCore = Resources.Load<Sprite>("ImmuneWar/Cores/core_" + _map.Id);
            if (specificCore) organImage.sprite = specificCore;
            else if (_art && _art.organCard) organImage.sprite = _art.organCard;
            _core = organImage;
            // Note: Floating _organHealth bar removed; we use the main HUD bar instead.

            // Route visualization: subtle transparent lines along blood vessels
            if (_map.routes != null)
            {
                foreach (var route in _map.routes)
                {
                    if (!route || route.waypoints == null) continue;
                    for (var i = 1; i < route.waypoints.Length; i++)
                    {
                        var start = RuntimeUi.Point(route.waypoints[i - 1]);
                        var end = RuntimeUi.Point(route.waypoints[i]);
                        // Very subtle glow only - the map art already shows the blood vessels
                        RuntimeUi.Line(_arena, "RouteGlow", start, end, 28, new Color(1f, 0.3f, 0.2f, 0.12f));
                    }
                }
            }

            // Free placement: an invisible surface over the map lets the player drop defenders anywhere
            // valid (e.g. several blockers straight on a vessel). Fixed node buttons are created after it,
            // so they still receive their own clicks.
            var surface = RuntimeUi.Image(_arena, "PlacementSurface", Vector2.zero, new Vector2(1920, 1080), Color.clear);
            surface.raycastTarget = true;
            var pointer = surface.gameObject.AddComponent<ArenaPointer>();
            pointer.Clicked += local => PlaceFree(local / 76f);
            pointer.Moved += local => UpdateGhost(local / 76f);
            pointer.Exited += HideGhost;

            // Defense nodes (placement spots)
            if (_map.nodes != null)
            {
                foreach (var node in _map.nodes)
                {
                    if (!node) continue;
                    var point = RuntimeUi.Point(node.position);
                    _nodePositions[node.Id] = node.position;
                    var nodeId = node.Id;
                    var mask = node.allowedRoleMask;
                    // Node size reduced to 32x32
                    var button = RuntimeUi.Button(_arena, "Node_" + nodeId, "+", point, new Vector2(32, 32),
                        new Color(0.3f, 1f, 0.5f, 0.6f), () => Place(nodeId, mask), 22);
                    var img = button.GetComponent<Image>();
                    img.sprite = RuntimeUi.Image(_arena, "CT", new Vector2(-9999, 0), Vector2.one, Color.clear, true).sprite;
                    img.type = Image.Type.Simple;
                }
            }

            // === LEFT PANEL: Defender selector (PvZ seed packet style) ===
            var leftPanel = RuntimeUi.Image(root, "LeftPanel", new Vector2(-890, 0),
                new Vector2(140, 800), new Color(0.08f, 0.06f, 0.12f, 0.75f));
            var panelSprite = _art ? _art.panel : null;
            if (panelSprite) { leftPanel.sprite = panelSprite; leftPanel.type = UnityEngine.UI.Image.Type.Sliced; }

            var defenders = catalog.defenders;
            var shown = Math.Min(6, defenders == null ? 0 : defenders.Length);
            for (var i = 0; i < shown; i++)
            {
                var defender = defenders[i];
                if (!defender) continue;
                var yPos = 300 - i * 120;
                
                // Seed packet background
                var button = RuntimeUi.Button(root, "Defender_" + defender.Id, "",
                    new Vector2(-890, yPos), new Vector2(110, 110),
                    new Color(0.12f, 0.16f, 0.22f, 0.95f), () => SelectDefender(defender), 16);
                
                var btnImg = button.GetComponent<Image>();
                if (panelSprite) { btnImg.sprite = panelSprite; btnImg.type = Image.Type.Sliced; }
                
                var rt = button.GetComponent<RectTransform>();
                
                // Icon (larger, centered)
                if (defender.presentation && defender.presentation.icon) {
                    var iconImg = RuntimeUi.Image(rt, "Icon", new Vector2(0, 10), new Vector2(80, 80), Color.white);
                    iconImg.sprite = defender.presentation.icon;
                    iconImg.preserveAspect = true;
                } else {
                    var name = defender.Id.Replace("def_", "").ToUpperInvariant();
                    RuntimeUi.Text(rt, "Name", name, new Vector2(0, 15), new Vector2(100, 40), 16, Color.white);
                }
                
                // Cost strip at bottom
                RuntimeUi.Image(rt, "CostStrip", new Vector2(0, -40), new Vector2(110, 30), new Color(0f, 0f, 0f, 0.6f));
                RuntimeUi.Text(rt, "Cost", defender.atpCost + " ATP", new Vector2(0, -40), new Vector2(100, 24), 16, new Color(1f, 0.9f, 0.2f), TextAnchor.MiddleRight);
                
                // Shortcut number
                RuntimeUi.Text(rt, "Shortcut", (i+1).ToString(), new Vector2(-40, 35), new Vector2(30, 30), 18, new Color(0.7f, 0.7f, 0.7f));

                _defenderButtons[defender.Id] = button;
            }
            _selection = RuntimeUi.Text(root, "Selection", "",
                new Vector2(-890, -420), new Vector2(130, 30), 14, RuntimeUi.Accent);

            // === TOP HUD BAR (full width strip, y: 510) ===
            // Background strip - use a softer blue-ish tint instead of harsh black
            var topHud = RuntimeUi.Image(root, "TopHud", new Vector2(0, 510), new Vector2(1920, 56), new Color(0.12f, 0.18f, 0.28f, 0.85f));
            if (_art && _art.panel) { topHud.sprite = _art.panel; topHud.type = Image.Type.Sliced; }

            // Left: ATP
            _stats = RuntimeUi.Text(root, "Stats", "",
                new Vector2(-600, 510), new Vector2(260, 46), 26, Color.white);

            // Centre-left: ORGAN label + health bar (separated)
            RuntimeUi.Text(root, "OrganLabel", "ORGAN", new Vector2(-200, 510), new Vector2(120, 46), 22, new Color(0.8f, 0.85f, 1f));
            _hudOrganHealth = AnimatedHealthBar.Create(root, "HudOrganHealth",
                new Vector2(0, 510), new Vector2(300, 24),
                _battle.State.Vitality.Maximum, _battle.State.Vitality.Current);

            // Right: WAVE
            _waveLabel = RuntimeUi.Text(root, "Wave", "",
                new Vector2(600, 510), new Vector2(220, 46), 26, Color.white);

            // === TOP RIGHT: Action buttons ===
            
            // 1. Menu Button (opens pause panel)
            var btnSettingsSpr = Resources.Load<Sprite>("ImmuneWar/btn_settings");
            var btnMenu = RuntimeUi.Button(root, "MenuToggle", btnSettingsSpr ? "" : "M",
                new Vector2(910, 480), new Vector2(80, 80), Color.white, TogglePauseMenu, 24);
            if (btnSettingsSpr) {
                var hi = btnMenu.GetComponent<Image>();
                hi.sprite = btnSettingsSpr; hi.type = Image.Type.Simple; hi.preserveAspect = true;
            }

            // === PAUSE PANEL ===
            var pauseBox = RuntimeUi.Rect(root, "PausePanel", Vector2.zero, new Vector2(1920, 1080));
            _pausePanel = pauseBox.gameObject;
            
            // Full screen dim
            RuntimeUi.Image(pauseBox, "Dim", Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, 0.6f));
            
            // Central window
            var pWin = RuntimeUi.Image(pauseBox, "Window", Vector2.zero, new Vector2(500, 400), new Color(0.12f, 0.16f, 0.22f, 0.98f));
            if (panelSprite) { pWin.sprite = panelSprite; pWin.type = Image.Type.Sliced; }
            
            RuntimeUi.Text(pauseBox, "Title", "PAUSED", new Vector2(0, 130), new Vector2(400, 80), 48, Color.white).fontStyle = FontStyle.Bold;
            
            // Resume
            var btnResume = RuntimeUi.Button(pauseBox, "Resume", "RESUME", new Vector2(0, 30), new Vector2(280, 70), new Color(0.2f, 0.7f, 0.3f), TogglePauseMenu, 26);
            if (panelSprite) { var img = btnResume.GetComponent<Image>(); img.sprite = panelSprite; img.type = Image.Type.Sliced; }
            
            // Restart
            var btnPr = RuntimeUi.Button(pauseBox, "Restart", "RESTART", new Vector2(0, -55), new Vector2(280, 70), new Color(0.7f, 0.3f, 0.2f), Restart, 26);
            if (panelSprite) { var img = btnPr.GetComponent<Image>(); img.sprite = panelSprite; img.type = Image.Type.Sliced; }
            
            // Home
            var btnPh = RuntimeUi.Button(pauseBox, "Home", "MAIN MENU", new Vector2(0, -140), new Vector2(280, 70), new Color(0.2f, 0.5f, 0.8f), ReturnToMenu, 26);
            if (panelSprite) { var img = btnPh.GetComponent<Image>(); img.sprite = panelSprite; img.type = Image.Type.Sliced; }

            _pausePanel.SetActive(false);

            // Status text (bottom centre)

            _status = RuntimeUi.Text(root, "Status", "",
                new Vector2(0, -510), new Vector2(700, 40), 18, new Color(1f, 0.96f, 0.82f));

            // === RESULT PANEL ===
            var result = RuntimeUi.Rect(root, "ResultPanel", Vector2.zero, new Vector2(700, 450));
            _resultPanel = result.gameObject;
            
            // Background
            var resultBg = RuntimeUi.Image(result, "Shade", Vector2.zero, new Vector2(700, 450), new Color(0.08f, 0.12f, 0.2f, 0.98f));
            if (panelSprite) { resultBg.sprite = panelSprite; resultBg.type = UnityEngine.UI.Image.Type.Sliced; }
            
            // Text
            _resultText = RuntimeUi.Text(result, "ResultText", "", new Vector2(0, 100), new Vector2(600, 140), 64, Color.white);
            _resultText.fontStyle = FontStyle.Bold;
            
            // Restart Button
            var btnRe = RuntimeUi.Button(result, "PlayAgain", "PLAY AGAIN", new Vector2(-170, -100), new Vector2(260, 80), new Color(0.2f, 0.7f, 0.3f), Restart, 28);
            if (panelSprite) { var img = btnRe.GetComponent<Image>(); img.sprite = panelSprite; img.type = Image.Type.Sliced; }
            
            // Menu Button
            var btnMenu2 = RuntimeUi.Button(result, "BackToMenu", "MAIN MENU", new Vector2(170, -100), new Vector2(260, 80), new Color(0.8f, 0.5f, 0.2f), ReturnToMenu, 28);
            if (panelSprite) { var img = btnMenu2.GetComponent<Image>(); img.sprite = panelSprite; img.type = Image.Type.Sliced; }
            
            _resultPanel.SetActive(false);
        }

        public void SelectDefender(DefenderConfig defender)
        {
            _selectedDefender = defender;
            foreach (var pair in _defenderButtons)
                RuntimeUi.Skin(pair.Value, _art, UiButtonRole.Card, defender && pair.Key == defender.Id);
            if (_selection) _selection.text = defender ? defender.Id.Replace("def_", "").ToUpperInvariant() : "NO DEFENDER";
            SetStatus(defender ? "Click a node or anywhere on the map to drop " + defender.Id.Replace("def_", "") + " (e.g. on a vessel to block)." : "Select a defender.");
            _audio?.Play(_art ? _art.uiClick : null, 0.7f);
        }

        public bool Place(string nodeId, DefenderRoleMask mask)
        {
            if (!_selectedDefender || _battle.State.Phase == BattlePhase.Paused) { SetStatus("Select a defender while the battle is active."); _audio?.Play(_art ? _art.uiError : null); return false; }
            var config = _selectedDefender;
            var result = _placement.TryPlace(Guid.NewGuid().ToString("N"), config.Id, config.role, nodeId, mask, config.atpCost, config.maxHealth);
            if (!result.Accepted) { SetStatus("Cannot place: " + result.ReasonCode.Replace('_', ' ')); _audio?.Play(_art ? _art.uiError : null); return false; }
            _audio?.Play(_art ? _art.place : null, 0.8f);
            var point = RuntimeUi.Point(_nodePositions[nodeId]);
            var visual = RuntimeUi.Image(_arena, "Placed_" + result.DefenderInstanceId, point, new Vector2(80, 80), DefenderColor(config.role), true);
            if (config.presentation && config.presentation.icon)
            {
                visual.sprite = config.presentation.icon;
                visual.color = Color.white;
                visual.preserveAspect = true;
            }
            else RuntimeUi.Text(visual.transform, "PlacedLetter", config.Id.Replace("def_", "").Substring(0, 1).ToUpperInvariant(), Vector2.zero, new Vector2(50, 50), 24, Color.white);
            var controller = _art ? _art.DefenderController(config.Id) : null;
            if (controller)
            {
                var animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                _defenderAnimators[result.DefenderInstanceId] = animator;
            }
            _defenderImages[result.DefenderInstanceId] = visual;
            _defenderHealthBars[result.DefenderInstanceId] = AnimatedHealthBar.Create(visual.transform,
                "DefenderHealth", new Vector2(0f, 55f), new Vector2(76f, 10f), config.maxHealth, config.maxHealth);
            SetStatus(config.Id.Replace("def_", "") + " placed. " + _battle.State.Economy.Atp + " ATP remaining.");
            RefreshStats();
            return true;
        }

        // ---------- Free placement ----------

        private const string FreeNodePrefix = "free-";
        private int _freeNodeSerial;
        private Image _ghost;
        private Image _ghostRange;

        /// <summary>Drops the selected defender at an arbitrary map point by creating a one-off node there.</summary>
        public bool PlaceFree(Vector2 world)
        {
            if (!_selectedDefender) { SetStatus("Select a defender on the left first."); _audio?.Play(_art ? _art.uiError : null); return false; }
            if (!CanPlaceAt(world, out var reason)) { SetStatus("Cannot place here: " + reason + "."); _audio?.Play(_art ? _art.uiError : null); return false; }
            var nodeId = FreeNodePrefix + (++_freeNodeSerial).ToString("D3");
            _battle.State.Nodes[nodeId] = new DefenseNodeState(nodeId);
            _nodePositions[nodeId] = world;
            if (Place(nodeId, DefenderRoleMask.All)) { UpdateGhost(world); return true; }
            _battle.State.Nodes.Remove(nodeId);
            _nodePositions.Remove(nodeId);
            return false;
        }

        private bool CanPlaceAt(Vector2 world, out string reason)
        {
            reason = null;
            var phase = _battle.State.Phase;
            if (phase is BattlePhase.Paused or BattlePhase.Victory or BattlePhase.Defeat) reason = "battle is not active";
            else if (world.x < -10.4f || world.x > 12.2f || world.y < -6.6f || world.y > 6.1f) reason = "outside the battlefield";
            else if (world.magnitude < CoreRadius + 0.7f) reason = "too close to the organ";
            else if (_selectedDefender && _battle.State.Economy.Atp < _selectedDefender.atpCost) reason = "not enough ATP";
            else
                foreach (var defender in _battle.State.Defenders.Values)
                    if (_nodePositions.TryGetValue(defender.NodeId, out var other) && Vector2.Distance(other, world) < 0.85f)
                    { reason = "too close to another defender"; break; }
            return reason == null;
        }

        private void UpdateGhost(Vector2 world)
        {
            if (!_selectedDefender) { HideGhost(); return; }
            if (!_ghost)
            {
                _ghostRange = RuntimeUi.Image(_arena, "GhostRange", Vector2.zero, Vector2.one, Color.white);
                _ghostRange.sprite = RingSprite;
                _ghost = RuntimeUi.Image(_arena, "PlacementGhost", Vector2.zero, new Vector2(80f, 80f), Color.white, true);
                _ghost.preserveAspect = true;
            }
            var ok = CanPlaceAt(world, out _);
            var point = RuntimeUi.Point(world);
            var icon = _selectedDefender.presentation ? _selectedDefender.presentation.icon : null;
            if (icon) _ghost.sprite = icon;
            _ghost.color = ok ? new Color(0.7f, 1f, 0.75f, 0.6f) : new Color(1f, 0.35f, 0.35f, 0.55f);
            _ghost.rectTransform.anchoredPosition = point;
            // Range ring shows attack range; for non-attackers it shows the blocking radius.
            var radius = Mathf.Max(_selectedDefender.attackDamage > 0f ? _selectedDefender.range : 0f, BlockRadius) * 76f;
            _ghostRange.rectTransform.anchoredPosition = point;
            _ghostRange.rectTransform.sizeDelta = Vector2.one * (radius * 2f / 0.85f);
            _ghostRange.color = ok ? new Color(0.5f, 1f, 0.6f, 0.55f) : new Color(1f, 0.35f, 0.35f, 0.45f);
            _ghost.gameObject.SetActive(true);
            _ghostRange.gameObject.SetActive(true);
            _ghostRange.transform.SetAsLastSibling();
            _ghost.transform.SetAsLastSibling();
        }

        private void HideGhost()
        {
            if (_ghost) _ghost.gameObject.SetActive(false);
            if (_ghostRange) _ghostRange.gameObject.SetActive(false);
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
            _audio?.Play(_art ? _art.waveStart : null, 0.8f);
            RefreshStats();
        }

        public void TogglePauseMenu()
        {
            if (!_battle) return;
            if (_battle.State.Phase == BattlePhase.Running)
            {
                _battle.Pause();
                if (_pausePanel) _pausePanel.SetActive(true);
            }
            else if (_battle.State.Phase == BattlePhase.Paused)
            {
                _battle.Resume();
                if (_pausePanel) _pausePanel.SetActive(false);
            }
        }

        public void Restart() => StartCoroutine(SceneFlowService.Load("Battle"));
        public void ReturnToMenu() => StartCoroutine(SceneFlowService.Load("MainMenu"));

        private void Update()
        {
            if (!_battle || !_map) return;
            var running = _battle.State.Phase == BattlePhase.Running;
            if (running && _wave != null)
            {
                _accumulator += Time.deltaTime;
                var count = 0;
                while (_accumulator >= 1f / 30f && count++ < 5)
                {
                    _accumulator -= 1f / 30f;
                    Tick();
                }
            }
            if (running) AnimateEnemies(Time.deltaTime);
            AnimateCore();
        }

        private void Tick()
        {
            _tick++;
            _battle.Advance(1d / 30d);
            foreach (var request in _wave.Tick(_tick)) Spawn(request);
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                var blocked = false;

                if (enemy.Config.regenPerSecond > 0f && enemy.State.Health < enemy.Config.maxHealth)
                    enemy.State.Health = Mathf.Min(enemy.Config.maxHealth, enemy.State.Health + enemy.Config.regenPerSecond / 30f);

                // Check if blocked by a defender
                foreach (var defender in _battle.State.Defenders.Values)
                {
                    if (defender.Health > 0f && _nodePositions.TryGetValue(defender.NodeId, out var defenderPoint) &&
                        Vector2.Distance(defenderPoint, enemy.Follower.Position) <= BlockRadius)
                    {
                        blocked = true;
                        if (_tick >= enemy.NextContactTick)
                        {
                            defender.ReceiveDamage(Mathf.Max(2f, enemy.Config.organDamage * 0.5f * enemy.Config.biteMultiplier));
                            ShowEffect(_art ? _art.hit : null, defenderPoint, new Color(1f, 0.46f, 0.51f, 0.72f));
                            if (_defenderHealthBars.TryGetValue(defender.InstanceId, out var healthBar))
                                healthBar.SetValue(defender.Health, FindDefender(defender.ConfigId)?.maxHealth ?? 100);
                            // The enemy lunges at the defender it is biting, and the defender flashes red.
                            enemy.Knock += (RuntimeUi.Point(defenderPoint) - enemy.BasePoint).normalized * 16f;
                            if (_defenderImages.TryGetValue(defender.InstanceId, out var defenderImage) && defenderImage)
                                StartCoroutine(FlashDefender(defenderImage));
                            enemy.NextContactTick = _tick + 30;
                        }
                        break;
                    }
                }

                if (!blocked)
                {
                    if (enemy.Follower.Tick(1f / 30f))
                    {
                        enemy.BasePoint = RuntimeUi.Point(enemy.Follower.Position);
                        _battle.DamageOrgan(enemy.Config.organDamage);
                        if (_hudOrganHealth) _hudOrganHealth.SetValue(_battle.State.Vitality.Current, _battle.State.Vitality.Maximum);
                        _wave.NotifyTerminal();
                        StrikeOrgan(enemy);
                        RemoveEnemy(i, false);
                        continue;
                    }
                }

                var next = RuntimeUi.Point(enemy.Follower.Position);
                // Sheets are drawn facing right; mirror the sprite while the enemy travels left.
                if (Mathf.Abs(next.x - enemy.BasePoint.x) > 0.2f) enemy.Facing = next.x < enemy.BasePoint.x ? -1f : 1f;
                enemy.BasePoint = next;
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
                if (config.role == DefenderRole.Economy && _tick % 90 == 0)
                {
                    _battle.State.Economy.Add(5);
                    FloatText(RuntimeUi.Point(position) + new Vector2(0f, 50f), "+5", new Color(1f, 0.88f, 0.25f), 22);
                    _audio?.Play(_art ? _art.atpGain : null, 0.35f, 0.4f);
                }
                if (config.role == DefenderRole.Repair && _tick % 120 == 0 && _battle.State.Vitality.Current < _battle.State.Vitality.Maximum)
                {
                    _battle.State.Vitality.Repair(3);
                    _audio?.Play(_art ? _art.heal : null, 0.4f, 1f);
                }
                if (config.attackDamage <= 0) continue;
                var candidates = new List<EnemyState>();
                foreach (var enemy in _enemies)
                    if (!enemy.State.IsTerminal && Vector2.Distance(position, enemy.Follower.Position) <= config.range) candidates.Add(enemy.State);
                // Pick the target first so its armor can reduce the damage dealt.
                var chosen = TargetingSystem.Select(candidates);
                var hitEnemy = chosen == null ? null : _enemies.Find(x => x.State == chosen);
                var damage = config.attackDamage * (1f - (hitEnemy != null ? hitEnemy.Config.armor : 0f));
                var target = chosen == null ? null : _combat.Attack(defender, new[] { chosen }, damage, Mathf.CeilToInt(config.attackInterval * 30f));
                if (target != null)
                {
                    if (_defenderAnimators.TryGetValue(defender.InstanceId, out var animator) && animator)
                        animator.Play("Attack", 0, 0f);
                    if (hitEnemy != null) ShowAttack(defender.InstanceId, config, position, hitEnemy, damage);
                }
                if (target != null && target.IsTerminal)
                {
                    var enemyIndex = _enemies.FindIndex(x => x.State == target);
                    if (enemyIndex >= 0) KillEnemy(enemyIndex);
                }
            }
            if (_battle.State.Phase == BattlePhase.Defeat) { ShowResult(false); return; }
            if (_wave != null && _battle.State.Waves.Phase == WavePhase.AllComplete)
            {
                _wave = null;
                if (_map.waveSet != null && _waveIndex + 1 >= _map.waveSet.waves.Length)
                {
                    _battle.CompleteAllWavesForTests();
                    ShowResult(true);
                }
                else 
                {
                    SetStatus("Wave clear! Next wave approaching...");
                    StartCoroutine(AutoNextWaveRoutine(6f));
                }
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
            CreateEnemy(config, route);
            if (config.Id == "ene_super_pathogen")
            {
                SetStatus("WARNING: Super Pathogen incoming!");
                _audio?.Play(_art ? _art.bossWarning : null);
            }
        }

        private EnemyVisual CreateEnemy(EnemyConfig config, RouteConfig route)
        {
            var state = new EnemyState("enemy-" + (++_enemySerial), config.Id, route.Id, config.maxHealth);
            _battle.State.Enemies[state.InstanceId] = state;
            var look = VisualBaseId(config);
            var size = EnemySize(look) * config.visualScale;
            // Stop with the enemy's front just touching the organ ring instead of walking into the core.
            var follower = new RouteFollower(state, route.waypoints, config.moveSpeed, CoreRadius + size * 0.35f / 76f);
            var image = RuntimeUi.Image(_arena, "Enemy_" + state.InstanceId, RuntimeUi.Point(follower.Position), new Vector2(size, size), EnemyTint(look) * config.tint, true);
            if (config.presentation && config.presentation.icon)
            {
                image.sprite = config.presentation.icon;
                image.color = config.tint;
                image.preserveAspect = true;
            }
            var controller = _art ? _art.EnemyController(look) : null;
            var hasOwnArt = controller && look == config.Id && FamilyId(config.Id) != config.Id;
            // A variant whose sheet has not been imported yet borrows its family's animation (ene_virus_swift → ene_virus).
            if (!controller && _art) controller = _art.EnemyController(FamilyId(config.Id));
            if (controller)
            {
                var animator = image.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }
            var visual = new EnemyVisual
            {
                State = state, Config = config, Follower = follower, Route = route, Image = image,
                BasePoint = RuntimeUi.Point(follower.Position), Phase = _enemySerial * 1.7f, BaseColor = image.color,
                Stretch = config.stretch, Size = size, HasOwnArt = hasOwnArt
            };
            AddTraitDecor(visual);
            visual.HealthBar = AnimatedHealthBar.Create(image.transform, "EnemyHealth",
                new Vector2(0f, size * 0.5f + 13f), new Vector2(Mathf.Max(68f, size * 0.8f), 10f), config.maxHealth, config.maxHealth);
            _enemies.Add(visual);
            return visual;
        }

        private static string VisualBaseId(EnemyConfig config) => string.IsNullOrEmpty(config.visualBaseId) ? config.Id : config.visualBaseId;

        private static string FamilyId(string id)
        {
            var second = id.IndexOf('_', id.IndexOf('_') + 1);
            return second > 0 ? id.Substring(0, second) : id;
        }

        private void KillEnemy(int enemyIndex)
        {
            var dead = _enemies[enemyIndex];
            _battle.State.Economy.Add(dead.Config.atpReward);
            ShowEffect(_art ? _art.atp : null, dead.Follower.Position, RuntimeUi.Accent);
            FloatText(dead.BasePoint + new Vector2(0f, 60f), "+" + dead.Config.atpReward + " ATP", new Color(1f, 0.88f, 0.25f), 24);
            _audio?.Play(_art ? _art.enemyDeath : null, dead.Config.Id == "ene_super_pathogen" ? 1f : 0.6f);
            // Splitters break into smaller enemies that continue from the same spot on the route.
            // They are registered with the wave before the parent is released so the wave never completes early.
            if (dead.Config.splitInto && dead.Config.splitCount > 0 && dead.Route)
            {
                for (var i = 0; i < dead.Config.splitCount; i++)
                {
                    var child = CreateEnemy(dead.Config.splitInto, dead.Route);
                    child.Follower.Warp(dead.Follower.Distance - 0.35f * i);
                    child.BasePoint = RuntimeUi.Point(child.Follower.Position);
                    child.Knock = new Vector2(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-30f, 30f));
                    _battle.State.Waves.ActiveEnemies++;
                }
                FloatText(dead.BasePoint + new Vector2(0f, 95f), "SPLIT!", new Color(1f, 0.65f, 0.3f), 26);
            }
            _wave.NotifyTerminal();
            RemoveEnemy(enemyIndex, false);
            var look = VisualBaseId(dead.Config);
            StartCoroutine(EnemyBurst(dead.Image, Vector2.zero, EnemyTint(look) * dead.Config.tint, dead.Config.Id == "ene_super_pathogen" ? 28 : 12));
        }

        /// <summary>Adds a trait-specific silhouette accent so each enemy type reads differently at a glance.</summary>
        private void AddTraitDecor(EnemyVisual enemy)
        {
            var config = enemy.Config;
            Image aura = null;
            // Variants with their own sheet already show the trait in the art (armor plates, three cores),
            // so the outline ring is only drawn for variants that still reuse a family sprite.
            if (config.armor > 0f && !enemy.HasOwnArt)
            {
                aura = RuntimeUi.Image(enemy.Image.transform, "ArmorShell", Vector2.zero, Vector2.one * (enemy.Size * 1.25f), new Color(0.75f, 0.85f, 1f, 0.9f));
                aura.sprite = RingSprite;
            }
            else if (config.regenPerSecond > 0f)
            {
                aura = RuntimeUi.Image(enemy.Image.transform, "RegenAura", Vector2.zero, Vector2.one * (enemy.Size * 1.5f), new Color(0.4f, 1f, 0.5f, 0.45f));
                aura.sprite = GlowSprite;
            }
            else if (config.biteMultiplier > 1.5f)
            {
                aura = RuntimeUi.Image(enemy.Image.transform, "ToxicAura", Vector2.zero, Vector2.one * (enemy.Size * 1.4f), new Color(0.8f, 1f, 0.2f, 0.4f));
                aura.sprite = GlowSprite;
            }
            else if (config.splitInto && config.splitCount > 0 && !enemy.HasOwnArt)
            {
                aura = RuntimeUi.Image(enemy.Image.transform, "SplitRing", Vector2.zero, Vector2.one * (enemy.Size * 1.15f), new Color(1f, 0.6f, 0.25f, 0.8f));
                aura.sprite = RingSprite;
            }
            enemy.Aura = aura;
        }

        private static float EnemySize(string id) => id switch
        {
            "ene_bacteria" => 80f,
            "ene_mutant" => 88f,
            "ene_super_pathogen" => 150f,
            _ => 70f
        };

        private static Color EnemyTint(string id) => id switch
        {
            "ene_bacteria" => new Color(0.55f, 1f, 0.45f),
            "ene_mutant" => new Color(0.8f, 0.45f, 1f),
            "ene_super_pathogen" => new Color(1f, 0.35f, 0.2f),
            "ene_virus_swift" => new Color(0.4f, 0.8f, 1f),
            "ene_virus_splitter" => new Color(1f, 0.6f, 0.2f),
            "ene_bacteria_armored" => new Color(0.6f, 0.68f, 0.85f),
            "ene_bacteria_toxic" => new Color(0.8f, 1f, 0.2f),
            "ene_mutant_regen" => new Color(0.45f, 1f, 0.5f),
            _ => new Color(1f, 0.4f, 0.5f)
        };

        // ---------- Presentation: hit feedback ----------

        private void AnimateEnemies(float deltaTime)
        {
            var now = Time.time;
            var settle = 1f - Mathf.Exp(-14f * deltaTime);
            foreach (var enemy in _enemies)
            {
                if (!enemy.Image) continue;
                enemy.Knock = Vector2.Lerp(enemy.Knock, Vector2.zero, settle);
                var hit = Mathf.Clamp01(1f - (now - enemy.HitTime) / 0.18f);
                var bob = Mathf.Sin(now * 4.8f + enemy.Phase) * 5f;
                var breathe = Mathf.Sin(now * 3.6f + enemy.Phase * 1.3f) * 0.035f;
                var rect = enemy.Image.rectTransform;
                rect.anchoredPosition = enemy.BasePoint + new Vector2(0f, bob) + enemy.Knock;
                // Silhouette stretch per enemy type, plus squash on impact, wobble and a fading red flash.
                rect.localScale = new Vector3(enemy.Facing * enemy.Stretch.x * (1f + breathe + hit * 0.22f), enemy.Stretch.y * (1f + breathe - hit * 0.16f), 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((now - enemy.HitTime) * 45f) * 14f * hit);
                enemy.Image.color = Color.Lerp(enemy.BaseColor, new Color(1f, 0.3f, 0.3f), hit);
                // Counter-mirror the health bar so it always drains right-to-left.
                if (enemy.HealthBar) enemy.HealthBar.transform.localScale = new Vector3(enemy.Facing, 1f, 1f);
                if (enemy.Aura)
                {
                    var pulse = 0.5f + 0.5f * Mathf.Sin(now * 5f + enemy.Phase);
                    enemy.Aura.rectTransform.localScale = Vector3.one * (0.92f + 0.12f * pulse);
                    if (enemy.Config.armor <= 0f) enemy.Aura.rectTransform.localRotation = Quaternion.identity;
                }
                // Fast enemies leave fading after-images.
                if (enemy.Config.moveSpeed >= 0.8f && now - enemy.TrailTime > 0.06f && enemy.Image.sprite)
                {
                    enemy.TrailTime = now;
                    StartCoroutine(AfterImage(enemy.Image));
                }
            }
        }

        private IEnumerator AfterImage(Image source)
        {
            var rect = source.rectTransform;
            var ghost = RuntimeUi.Image(_arena, "AfterImage", rect.anchoredPosition, rect.sizeDelta, source.color);
            ghost.sprite = source.sprite;
            ghost.preserveAspect = true;
            ghost.rectTransform.localScale = rect.localScale;
            ghost.transform.SetSiblingIndex(Mathf.Max(0, source.transform.GetSiblingIndex()));
            var color = source.color;
            const float duration = 0.25f;
            for (var elapsed = 0f; ghost && elapsed < duration; elapsed += Time.deltaTime)
            {
                color.a = 0.45f * (1f - elapsed / duration);
                ghost.color = color;
                yield return null;
            }
            if (ghost) Destroy(ghost.gameObject);
        }

        private void ShowAttack(string defenderId, DefenderConfig config, Vector2 defenderPosition, EnemyVisual target, float damage)
        {
            var from = RuntimeUi.Point(defenderPosition);
            var to = target.BasePoint;
            var direction = (to - from).sqrMagnitude > 0.01f ? (to - from).normalized : Vector2.right;
            var melee = config.range <= 1.5f;
            var color = DefenderColor(config.role);
            if (_defenderImages.TryGetValue(defenderId, out var defenderImage) && defenderImage)
                StartCoroutine(Recoil(defenderImage.rectTransform, from, melee ? direction * 20f : -direction * 7f));
            if (!melee) StartCoroutine(Tracer(from, to, color));
            target.HitTime = Time.time;
            target.Knock += direction * (melee ? 16f : 10f);
            ShowEffect(_art ? _art.hit : null, target.Follower.Position, Color.Lerp(color, Color.white, 0.4f), 58f);
            // Armored targets show their reduced damage in steel blue.
            var armored = target.Config.armor > 0f;
            FloatText(to + new Vector2(UnityEngine.Random.Range(-16f, 16f), 42f),
                Mathf.Max(1, Mathf.RoundToInt(damage)).ToString(), armored ? new Color(0.7f, 0.82f, 1f) : new Color(1f, 0.95f, 0.6f), armored ? 22 : 26);
            var heavy = config.role is DefenderRole.Burst or DefenderRole.Blocker;
            _audio?.Play(_art ? heavy ? _art.heavyAttack : _art.attack : null, 0.45f, 0.07f);
        }

        private IEnumerator Recoil(RectTransform rect, Vector2 home, Vector2 offset)
        {
            const float duration = 0.16f;
            for (var elapsed = 0f; rect && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                rect.anchoredPosition = home + offset * Mathf.Sin(t * Mathf.PI);
                rect.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            if (!rect) yield break;
            rect.anchoredPosition = home;
            rect.localScale = Vector3.one;
        }

        private IEnumerator Tracer(Vector2 from, Vector2 to, Color color)
        {
            var delta = to - from;
            var beam = RuntimeUi.Image(_arena, "Tracer", (from + to) * 0.5f, new Vector2(delta.magnitude, 5f), color);
            beam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var bolt = RuntimeUi.Image(_arena, "Bolt", from, new Vector2(22f, 22f), Color.Lerp(color, Color.white, 0.5f));
            bolt.sprite = GlowSprite;
            const float duration = 0.14f;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                if (bolt) bolt.rectTransform.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(t * 1.6f));
                if (beam) { var c = color; c.a = 0.75f * (1f - t); beam.color = c; }
                yield return null;
            }
            if (beam) Destroy(beam.gameObject);
            if (bolt) Destroy(bolt.gameObject);
        }

        private IEnumerator FlashDefender(Image image)
        {
            if (!_flashingDefenders.Add(image)) yield break;
            var original = image.color;
            const float duration = 0.2f;
            for (var elapsed = 0f; image && elapsed < duration; elapsed += Time.deltaTime)
            {
                image.color = Color.Lerp(new Color(1f, 0.3f, 0.3f, original.a), original, elapsed / duration);
                yield return null;
            }
            if (image) image.color = original;
            _flashingDefenders.Remove(image);
        }

        /// <summary>Death (or organ strike) animation: optional lunge, then pop, spin and particle burst.</summary>
        private IEnumerator EnemyBurst(Image image, Vector2 lunge, Color tint, int particles)
        {
            if (!image) yield break;
            var animator = image.GetComponent<Animator>();
            if (animator) animator.enabled = false;
            var bar = image.GetComponentInChildren<AnimatedHealthBar>();
            if (bar) bar.gameObject.SetActive(false);
            var rect = image.rectTransform;
            var start = rect.anchoredPosition;
            if (lunge != Vector2.zero)
            {
                const float lungeTime = 0.14f;
                for (var elapsed = 0f; image && elapsed < lungeTime; elapsed += Time.deltaTime)
                {
                    var t = elapsed / lungeTime;
                    rect.anchoredPosition = start + lunge * (t * t);
                    rect.localScale = new Vector3(1f - 0.2f * t, 1f + 0.25f * t, 1f);
                    yield return null;
                }
                if (!image) yield break;
            }
            SpawnParticles(rect.anchoredPosition, tint, particles, rect.sizeDelta.x);
            const float popTime = 0.28f;
            var baseColor = image.color;
            for (var elapsed = 0f; image && elapsed < popTime; elapsed += Time.deltaTime)
            {
                var t = elapsed / popTime;
                rect.localScale = Vector3.one * (1f + 0.6f * t);
                rect.localRotation = Quaternion.Euler(0f, 0f, 120f * t);
                image.color = new Color(Mathf.Lerp(baseColor.r, 1f, 0.5f), baseColor.g * (1f - t * 0.5f), baseColor.b * (1f - t * 0.5f), 1f - t);
                yield return null;
            }
            if (image) Destroy(image.gameObject);
        }

        private void SpawnParticles(Vector2 origin, Color tint, int count, float spread)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = (i + UnityEngine.Random.value * 0.6f) / count * Mathf.PI * 2f;
                var distance = spread * UnityEngine.Random.Range(0.7f, 1.4f);
                var dot = RuntimeUi.Image(_arena, "Particle", origin, Vector2.one * UnityEngine.Random.Range(12f, 24f), tint);
                dot.sprite = GlowSprite;
                StartCoroutine(Particle(dot, origin, origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance));
            }
        }

        private static IEnumerator Particle(Image dot, Vector2 from, Vector2 to)
        {
            const float duration = 0.45f;
            var color = dot.color;
            for (var elapsed = 0f; dot && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                var eased = 1f - (1f - t) * (1f - t);
                dot.rectTransform.anchoredPosition = Vector2.Lerp(from, to, eased);
                dot.rectTransform.localScale = Vector3.one * (1f - 0.6f * t);
                color.a = 1f - t;
                dot.color = color;
                yield return null;
            }
            if (dot) Destroy(dot.gameObject);
        }

        private void FloatText(Vector2 position, string value, Color color, int fontSize)
        {
            var text = RuntimeUi.Text(_arena, "FloatText", value, position, new Vector2(160f, 50f), fontSize, color);
            text.resizeTextForBestFit = false;
            text.fontStyle = FontStyle.Bold;
            StartCoroutine(RiseAndFade(text, position));
        }

        private static IEnumerator RiseAndFade(Text text, Vector2 start)
        {
            const float duration = 0.75f;
            var color = text.color;
            for (var elapsed = 0f; text && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                text.rectTransform.anchoredPosition = start + new Vector2(0f, 55f * (1f - (1f - t) * (1f - t)));
                text.rectTransform.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(0.6f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, (t - 0.15f) / 0.85f));
                color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                text.color = color;
                yield return null;
            }
            if (text) Destroy(text.gameObject);
        }

        // ---------- Presentation: organ core ----------

        private void StrikeOrgan(EnemyVisual enemy)
        {
            var contact = enemy.BasePoint;
            _coreHitTime = Time.time;
            _coreHitDirection = contact.sqrMagnitude > 0.01f ? contact.normalized : Vector2.up;
            ShowEffect(_art ? _art.hit : null, enemy.Follower.Position, new Color(1f, 0.25f, 0.25f, 0.95f), 120f);
            _audio?.Play(_art ? _art.organHit : null, 1f, 0.1f);
            StartCoroutine(Shockwave());
            FloatText(new Vector2(0f, 165f), "-" + enemy.Config.organDamage, new Color(1f, 0.32f, 0.32f), 40);
            // The enemy dives into the ring and bursts on contact.
            StartCoroutine(EnemyBurst(enemy.Image, -_coreHitDirection * 24f, EnemyTint(enemy.Config.Id), 10));
        }

        private IEnumerator Shockwave()
        {
            var ring = RuntimeUi.Image(_arena, "CoreShockwave", Vector2.zero, new Vector2(200f, 200f), new Color(1f, 0.3f, 0.3f, 0.85f));
            ring.sprite = RingSprite;
            const float duration = 0.5f;
            for (var elapsed = 0f; ring && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 2.1f, 1f - (1f - t) * (1f - t));
                ring.color = new Color(1f, 0.3f, 0.3f, 0.85f * (1f - t));
                yield return null;
            }
            if (ring) Destroy(ring.gameObject);
        }

        private void AnimateCore()
        {
            if (!_core) return;
            var now = Time.time;
            var vitality = _battle.State.Vitality;
            var health = (float)vitality.Current / vitality.Maximum;
            // Heartbeat: a strong beat followed by a softer one; beats faster as vitality drops.
            var period = Mathf.Lerp(0.55f, 1.15f, health);
            var t = Mathf.Repeat(now, period);
            var beat = Mathf.Exp(-Mathf.Pow(t / 0.07f, 2f)) + 0.6f * Mathf.Exp(-Mathf.Pow((t - 0.2f) / 0.07f, 2f));
            var hit = Mathf.Clamp01(1f - (now - _coreHitTime) / 0.4f);
            var shake = hit > 0f ? new Vector2(Mathf.Sin(now * 90f), Mathf.Cos(now * 77f)) * (12f * hit) : Vector2.zero;
            var rect = _core.rectTransform;
            rect.anchoredPosition = shake - _coreHitDirection * (14f * hit);
            rect.localScale = Vector3.one * (1f + 0.06f * beat - 0.08f * hit);
            var danger = health < 0.35f ? 0.5f + 0.5f * Mathf.Sin(now * 8f) : 0f;
            _core.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), Mathf.Max(hit, danger * 0.45f));
            if (!_coreGlow) return;
            var glow = health > 0.6f ? new Color(0.35f, 1f, 0.6f) : health > 0.3f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.3f, 0.3f);
            glow = Color.Lerp(glow, new Color(1f, 0.2f, 0.2f), hit);
            glow.a = 0.22f + 0.3f * beat + 0.4f * hit;
            _coreGlow.color = glow;
            _coreGlow.rectTransform.localScale = Vector3.one * (1f + 0.12f * beat + 0.15f * hit);
        }

        private static Sprite GlowSprite => _glowSprite ? _glowSprite : _glowSprite = RadialSprite(false);
        private static Sprite RingSprite => _ringSprite ? _ringSprite : _ringSprite = RadialSprite(true);

        private static Sprite RadialSprite(bool ring)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    var alpha = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.1f) : Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void ShowEffect(Sprite sprite, Vector2 position, Color tint, float size = 65f)
        {
            var image = RuntimeUi.Image(_arena, "ImpactVfx", RuntimeUi.Point(position), new Vector2(size, size), tint, !sprite);
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

        private void RemoveEnemy(int index, bool destroyImage = true)
        {
            var enemy = _enemies[index];
            _battle.State.Enemies.Remove(enemy.State.InstanceId);
            if (destroyImage && enemy.Image) Destroy(enemy.Image.gameObject);
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
            // One-off nodes from free placement disappear with their defender.
            if (defender.NodeId.StartsWith(FreeNodePrefix, StringComparison.Ordinal))
            {
                _battle.State.Nodes.Remove(defender.NodeId);
                _nodePositions.Remove(defender.NodeId);
            }
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
            HideGhost();
            _audio?.StopMusic();
            _audio?.Play(_art ? victory ? _art.atpGain : _art.organHit : null);
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

        /// <summary>Forwards pointer events on the map surface as positions in arena (pixel) space.</summary>
        private sealed class ArenaPointer : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
        {
            public event Action<Vector2> Clicked;
            public event Action<Vector2> Moved;
            public event Action Exited;

            public void OnPointerClick(PointerEventData eventData) { if (ToLocal(eventData, out var local)) Clicked?.Invoke(local); }
            public void OnPointerMove(PointerEventData eventData) { if (ToLocal(eventData, out var local)) Moved?.Invoke(local); }
            public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();

            private bool ToLocal(PointerEventData eventData, out Vector2 local) =>
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out local);
        }

        private void RefreshStats()
        {
            if (!_battle || !_stats) return;
            var state = _battle.State;
            _stats.text = "ATP  " + state.Economy.Atp;
            _waveLabel.text = "WAVE " + Math.Max(0, _waveIndex + 1) + "/" + (_map.waveSet?.waves?.Length ?? 0);
            // (Floating organ health removed)
            if (_hudOrganHealth) _hudOrganHealth.SetValue(state.Vitality.Current, state.Vitality.Maximum);
        }
    }
}
