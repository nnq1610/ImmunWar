using System.Collections.Generic;
using ImmunWar.Core;
using ImmunWar.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class PlayableMenuView : MonoBehaviour
    {
        private GameSession _session;
        private MainMenuController _menu;
        private string _selectedMap = "map_lung";
        private Text _selectionLabel;
        private PlayableArtCatalog _art;
        private BattleAudio _audio;
        private readonly Dictionary<string, Button> _mapButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Image> _mapCardImages = new Dictionary<string, Image>();

        private void Start()
        {
            var catalog = Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            _session = FindFirstObjectByType<GameSession>();
            if (!_session && catalog)
            {
                var sessionGo = new GameObject("GameSession");
                _session = sessionGo.AddComponent<GameSession>();
                _session.Initialize(catalog);
            }
            else if (_session && _session.Catalogs != null && _session.Catalogs.Catalog)
            {
                catalog = _session.Catalogs.Catalog;
            }
            _menu = FindFirstObjectByType<MainMenuController>();
            if (!_menu) _menu = gameObject.AddComponent<MainMenuController>();
            _art = Resources.Load<PlayableArtCatalog>("ImmuneWar/PlayableArtCatalog");
            _audio = BattleAudio.Create(gameObject, _art, _session ? _session.Progress : null);
            _audio.PlayMusic(_art ? _art.menuMusic : null);
            if (_session != null && !string.IsNullOrEmpty(_session.SelectedMapId) && IsMapUnlocked(_session.SelectedMapId))
                _selectedMap = _session.SelectedMapId;
            else
                _selectedMap = "map_lung";
            var root = RuntimeUi.Root("PlayableMenu");

            // === FIXED BACKGROUND ===
            var bgSprite = Resources.Load<Sprite>("ImmuneWar/menu_bg");
            var bgImg = RuntimeUi.Image(root, "Background", Vector2.zero, new Vector2(1920, 1080), Color.white);
            if (bgSprite) { bgImg.sprite = bgSprite; bgImg.preserveAspect = false; }
            else bgImg.color = RuntimeUi.Background;

            // Dark overlay for readability
            RuntimeUi.Image(root, "Overlay", Vector2.zero, new Vector2(1920, 1080), new Color(0f, 0f, 0.05f, 0.42f));

            // === TITLE (top, y: 340-410) ===
            RuntimeUi.Image(root, "TitleBg", new Vector2(0, 375), new Vector2(820, 120), new Color(0.12f, 0.18f, 0.28f, 0.65f));
            RuntimeUi.Text(root, "Title", "IMMUNE WAR", new Vector2(0, 385), new Vector2(1100, 130), 88, RuntimeUi.Accent);
            RuntimeUi.Text(root, "Subtitle", "DEFEND THE BODY", new Vector2(0, 315), new Vector2(900, 55), 30, new Color(1f, 0.96f, 0.82f, 0.95f));

            // === CHOOSE LABEL (y: 230) — well above cards ===
            RuntimeUi.Text(root, "Prompt", "— CHỌN MÀN CHƠI THEO TRÌNH TỰ (PHỔI → NÃO → DẠ DÀY) —", new Vector2(0, 235), new Vector2(1100, 50), 26, new Color(0.95f, 0.88f, 0.55f));

            // === ORGAN CARDS (y: 40, height 260 → spans -90 to +170) ===
            var maps = catalog ? catalog.maps : null;
            if (maps != null)
            {
                var cardSprites = new Dictionary<string, Sprite>
                {
                    { "map_lung",    Resources.Load<Sprite>("ImmuneWar/organ_card_lung")    },
                    { "map_brain",   Resources.Load<Sprite>("ImmuneWar/organ_card_brain")   },
                    { "map_stomach", Resources.Load<Sprite>("ImmuneWar/organ_card_stomach") },
                };

                for (var i = 0; i < maps.Length; i++)
                {
                    var map = maps[i];
                    if (!map) continue;
                    var mapId = map.Id;
                    var unlocked = IsMapUnlocked(mapId);
                    var completed = IsMapCompleted(mapId);

                    float xPos = (i - (maps.Length - 1) * 0.5f) * 320f;
                    var cardSize = new Vector2(260, 260); // square cards
                    var btn = RuntimeUi.Button(root, "Map_" + mapId, "", new Vector2(xPos, 40), cardSize,
                        Color.clear, () => SelectMap(mapId), 0);
                    btn.GetComponent<Image>().color = Color.clear;

                    var cardRect = btn.GetComponent<RectTransform>();
                    cardSprites.TryGetValue(mapId, out var cardSpr);
                    var cardImg = RuntimeUi.Image(cardRect, "CardImg", Vector2.zero, cardSize, Color.white);
                    if (cardSpr) { cardImg.sprite = cardSpr; cardImg.preserveAspect = true; }
                    else
                    {
                        var fc = mapId switch
                        {
                            "map_brain"   => new Color(0.55f, 0.4f, 0.9f),
                            "map_stomach" => new Color(0.35f, 0.75f, 0.35f),
                            _             => new Color(0.9f, 0.45f, 0.55f)
                        };
                        cardImg.color = fc;
                        RuntimeUi.Text(cardRect, "CardText", mapId.Replace("map_", "").ToUpperInvariant(),
                            Vector2.zero, cardSize, 36, Color.white);
                    }

                    // Stage number badge at top of card
                    var stageBadge = RuntimeUi.Image(cardRect, "StageBadge", new Vector2(0, 112), new Vector2(220, 32),
                        completed ? new Color(0.12f, 0.55f, 0.25f, 0.92f) : new Color(0.08f, 0.12f, 0.2f, 0.88f));
                    stageBadge.raycastTarget = false;
                    var stageTitle = completed
                        ? "MÀN " + (i + 1) + " — ĐÃ HOÀN THÀNH"
                        : "MÀN " + (i + 1) + ": " + OrganDisplayName(mapId);
                    var stageTxt = RuntimeUi.Text(cardRect, "StageText", stageTitle, new Vector2(0, 112), new Vector2(216, 30), 15,
                        completed ? new Color(0.65f, 1f, 0.75f) : new Color(0.95f, 0.92f, 0.75f));
                    stageTxt.fontStyle = FontStyle.Bold;

                    if (!unlocked)
                    {
                        cardImg.color = new Color(0.22f, 0.22f, 0.26f, 0.85f);
                        var lockOverlay = RuntimeUi.Image(cardRect, "LockOverlay", Vector2.zero, cardSize, new Color(0.02f, 0.03f, 0.07f, 0.72f));
                        lockOverlay.raycastTarget = false;
                        var lockBanner = RuntimeUi.Image(cardRect, "LockBanner", new Vector2(0, -10), new Vector2(244, 88), new Color(0.16f, 0.06f, 0.08f, 0.95f));
                        lockBanner.raycastTarget = false;
                        var lockTitle = RuntimeUi.Text(cardRect, "Locked", "CHƯA MỞ KHÓA", new Vector2(0, 8), new Vector2(236, 36), 22, new Color(1f, 0.38f, 0.38f));
                        lockTitle.fontStyle = FontStyle.Bold;
                        var prevOrgan = i > 0 && maps[i - 1] ? OrganDisplayName(maps[i - 1].Id) : "MÀN TRƯỚC";
                        RuntimeUi.Text(cardRect, "LockedHint", "Qua màn " + prevOrgan + " để mở", new Vector2(0, -24), new Vector2(236, 32), 16, new Color(0.92f, 0.92f, 0.92f));
                    }

                    _mapButtons[mapId] = btn;
                    _mapCardImages[mapId] = cardImg;
                    btn.interactable = unlocked;
                }
            }

            // === SELECTION LABEL (y: -145) ===
            _selectionLabel = RuntimeUi.Text(root, "SelectedMap", "ĐANG CHỌN: " + OrganDisplayName(_selectedMap),
                new Vector2(0, -145), new Vector2(800, 50), 26, RuntimeUi.Accent);

            // === PLAY BUTTON (y: -240) ===
            var play = RuntimeUi.Button(root, "PlayButton", "", new Vector2(0, -240), new Vector2(340, 120), Color.white, Play, 48);
            RuntimeUi.Skin(play, _art, UiButtonRole.Primary);
            // Ensure play button doesn't squish by preserving aspect
            var pi = play.GetComponent<Image>();
            pi.type = Image.Type.Simple;
            pi.preserveAspect = true;

            // === QUIT BUTTON — plain red, no sprite dependency (y: -380) ===
            var quitSprite = Resources.Load<Sprite>("ImmuneWar/btn_quit");
            var quit = RuntimeUi.Button(root, "QuitButton", quitSprite ? "" : "QUIT", new Vector2(0, -380), new Vector2(300, 110),
                quitSprite ? Color.white : new Color(0.78f, 0.18f, 0.18f), () => _menu?.Quit(), 32);
            var quitImg = quit.GetComponent<Image>();
            if (quitSprite)
            {
                quitImg.sprite = quitSprite;
                quitImg.type = Image.Type.Simple;
                quitImg.preserveAspect = true;
            }
            else
            {
                quitImg.sprite = null;
                var qc = quit.colors;
                qc.normalColor      = new Color(0.78f, 0.18f, 0.18f);
                qc.highlightedColor = new Color(0.9f,  0.3f,  0.3f);
                qc.pressedColor     = new Color(0.55f, 0.1f,  0.1f);
                quit.colors = qc;
            }

            // === RESET PROGRESS BUTTON (bottom right, allows resetting to Lung-only anytime) ===
            RuntimeUi.Button(root, "ResetProgressButton", "CHƠI MỚI (RESET TIẾN ĐỘ)", new Vector2(720, -450), new Vector2(340, 60),
                new Color(0.28f, 0.22f, 0.36f, 0.92f), ResetProgress, 20);

            UpdateSelectedMapArt();
        }

        private bool IsMapUnlocked(string mapId) =>
            _session != null && _session.Progress != null && _session.Progress.unlockedMapIds != null
                ? _session.Progress.unlockedMapIds.Contains(mapId)
                : mapId == "map_lung";

        private bool IsMapCompleted(string mapId) =>
            _session != null && _session.Progress != null && _session.Progress.completedMapIds != null
            && _session.Progress.completedMapIds.Contains(mapId);

        private static string OrganDisplayName(string mapId) => mapId switch
        {
            "map_lung" => "PHỔI (LUNG)",
            "map_brain" => "NÃO BỘ (BRAIN)",
            "map_stomach" => "DẠ DÀY (STOMACH)",
            _ => mapId.Replace("map_", "").ToUpperInvariant()
        };

        private void ResetProgress()
        {
            _audio?.Play(_art ? _art.uiClick : null);
            _session?.ResetProgress();
            StartCoroutine(SceneFlowService.Load("MainMenu"));
        }

        private void SelectMap(string mapId)
        {
            if (_session != null && !_session.SelectMap(mapId)) { _audio?.Play(_art ? _art.uiError : null); return; }
            _audio?.Play(_art ? _art.uiClick : null);
            _selectedMap = mapId;
            if (_selectionLabel) _selectionLabel.text = "ĐANG CHỌN: " + OrganDisplayName(mapId);
            UpdateSelectedMapArt();
        }

        private void UpdateSelectedMapArt()
        {
            foreach (var pair in _mapCardImages)
            {
                if (!pair.Value) continue;
                var unlocked = IsMapUnlocked(pair.Key);
                if (!unlocked)
                {
                    pair.Value.color = new Color(0.22f, 0.22f, 0.26f, 0.85f);
                    if (_mapButtons.TryGetValue(pair.Key, out var lockedBtn) && lockedBtn)
                        lockedBtn.transform.localScale = Vector3.one;
                    continue;
                }
                bool isSelected = pair.Key == _selectedMap;
                pair.Value.color = isSelected ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.82f);
                if (_mapButtons.TryGetValue(pair.Key, out var btn) && btn)
                    btn.transform.localScale = isSelected ? new Vector3(1.1f, 1.1f, 1f) : Vector3.one;
            }
        }

        public void Play()
        {
            _audio?.Play(_art ? _art.uiClick : null);
            if (_session != null) _session.SelectMap(_selectedMap);
            if (_menu) _menu.StartSelectedBattle();
        }
    }
}
