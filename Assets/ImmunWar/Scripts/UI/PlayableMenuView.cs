using System.Collections.Generic;
using ImmunWar.Core;
using ImmunWar.Core.Config;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private Text _muteBtnText;
        private readonly Dictionary<string, Button> _mapButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Image> _mapCardImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _mapCardBorders = new Dictionary<string, Image>();

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

            // === FIXED BACKGROUND WITH HIGH-TECH VIGNETTE ===
            var bgSprite = Resources.Load<Sprite>("ImmuneWar/menu_bg");
            var bgImg = RuntimeUi.Image(root, "Background", Vector2.zero, new Vector2(1920, 1080), Color.white);
            if (bgSprite) { bgImg.sprite = bgSprite; bgImg.preserveAspect = false; }
            else bgImg.color = RuntimeUi.Background;

            // Deep cyber-overlay to make UI pop
            RuntimeUi.Image(root, "Overlay", Vector2.zero, new Vector2(1920, 1080), new Color(0.02f, 0.04f, 0.08f, 0.55f));

            // === TOP AUDIO & SETTINGS BAR (Top Left / Right) ===
            var muteBtn = RuntimeUi.Button(root, "MuteButton", "", new Vector2(820, 485), new Vector2(220, 46),
                new Color(0.08f, 0.14f, 0.22f, 0.9f), ToggleMuteAudio, 16);
            _muteBtnText = muteBtn.GetComponentInChildren<Text>();
            UpdateMuteUi();

            // Hotkey guide badge (Top Left)
            var hotkeyPill = RuntimeUi.Image(root, "HotkeyPill", new Vector2(-740, 485), new Vector2(360, 42),
                new Color(0.06f, 0.1f, 0.18f, 0.85f));
            RuntimeUi.Text(hotkeyPill.transform, "HotkeyText", "PHÍM TẮT: [1-3] Chọn màn | [SPACE] Chơi | [M] Âm thanh",
                Vector2.zero, new Vector2(350, 36), 14, new Color(0.7f, 0.85f, 1f));

            // === TITLE SECTION (Top Center) ===
            // Backplate glow
            RuntimeUi.Image(root, "TitleBackplate", new Vector2(0, 385), new Vector2(920, 120),
                new Color(0.05f, 0.12f, 0.22f, 0.85f));
            // Title border lines
            RuntimeUi.Image(root, "TitleLineTop", new Vector2(0, 445), new Vector2(900, 3), new Color(0.25f, 0.85f, 0.95f, 0.8f));
            RuntimeUi.Image(root, "TitleLineBottom", new Vector2(0, 325), new Vector2(900, 3), new Color(0.25f, 0.85f, 0.95f, 0.8f));

            var titleText = RuntimeUi.Text(root, "Title", "IMMUNE WAR", new Vector2(0, 395), new Vector2(1100, 85), 78,
                new Color(0.35f, 1f, 0.75f));
            titleText.fontStyle = FontStyle.Bold;

            RuntimeUi.Text(root, "Subtitle", "— HỆ THỐNG PHÒNG THỦ KHÁNG THỂ TẾ BÀO —", new Vector2(0, 345),
                new Vector2(900, 40), 22, new Color(1f, 0.92f, 0.7f));

            // === PROMPT BAR (y: 245) ===
            var promptPill = RuntimeUi.Image(root, "PromptPill", new Vector2(0, 245), new Vector2(800, 42),
                new Color(0.08f, 0.15f, 0.24f, 0.92f));
            RuntimeUi.Text(promptPill.transform, "PromptText", "CHỌN CHIẾN TRƯỜNG CƠ QUAN THEO TRÌNH TỰ",
                Vector2.zero, new Vector2(780, 36), 20, new Color(1f, 0.88f, 0.45f)).fontStyle = FontStyle.Bold;

            // === ORGAN CARDS (y: 35, width 280, height 330) ===
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

                    float xPos = (i - (maps.Length - 1) * 0.5f) * 370f;
                    var cardSize = new Vector2(300, 340);

                    // Outer Card Button
                    var btn = RuntimeUi.Button(root, "Map_" + mapId, "", new Vector2(xPos, 35), cardSize,
                        Color.clear, () => SelectMap(mapId), 0);
                    btn.GetComponent<Image>().color = Color.clear;
                    var cardRect = btn.GetComponent<RectTransform>();

                    // Card Outer Frame / Shadow
                    RuntimeUi.Image(cardRect, "OuterFrame", Vector2.zero, cardSize,
                        new Color(0.04f, 0.08f, 0.15f, 0.95f));

                    // Glowing Selection Border
                    var border = RuntimeUi.Image(cardRect, "BorderGlow", Vector2.zero, cardSize + new Vector2(8, 8),
                        new Color(0.2f, 0.85f, 0.95f, 0f));
                    _mapCardBorders[mapId] = border;

                    // Inner Organ Image Viewport
                    cardSprites.TryGetValue(mapId, out var cardSpr);
                    var cardImg = RuntimeUi.Image(cardRect, "CardImg", new Vector2(0, 20), new Vector2(276, 220), Color.white);
                    if (cardSpr)
                    {
                        cardImg.sprite = cardSpr;
                        cardImg.preserveAspect = true;
                    }
                    else
                    {
                        cardImg.color = mapId switch
                        {
                            "map_brain"   => new Color(0.55f, 0.4f, 0.9f),
                            "map_stomach" => new Color(0.35f, 0.75f, 0.35f),
                            _             => new Color(0.9f, 0.45f, 0.55f)
                        };
                    }

                    // Header Status Badge (Top of card)
                    var badgeColor = completed
                        ? new Color(0.12f, 0.58f, 0.28f, 0.96f)
                        : (unlocked ? new Color(0.12f, 0.36f, 0.65f, 0.96f) : new Color(0.35f, 0.12f, 0.14f, 0.96f));
                    var stageBadge = RuntimeUi.Image(cardRect, "StageBadge", new Vector2(0, 142), new Vector2(280, 36), badgeColor);
                    stageBadge.raycastTarget = false;

                    var badgeTitle = completed
                        ? "✓ ĐÃ HOÀN THÀNH"
                        : (unlocked ? ("MÀN " + (i + 1) + " — SẴN SÀNG") : ("MÀN " + (i + 1) + " — ĐÃ KHÓA"));
                    var stageTxt = RuntimeUi.Text(cardRect, "StageText", badgeTitle, new Vector2(0, 142), new Vector2(270, 30), 16,
                        completed ? new Color(0.7f, 1f, 0.75f) : (unlocked ? Color.white : new Color(1f, 0.5f, 0.5f)));
                    stageTxt.fontStyle = FontStyle.Bold;

                    // Bottom info strip (Organ title + Map info)
                    var infoStrip = RuntimeUi.Image(cardRect, "InfoStrip", new Vector2(0, -120), new Vector2(280, 68),
                        new Color(0.06f, 0.1f, 0.18f, 0.96f));
                    infoStrip.raycastTarget = false;

                    var organName = OrganDisplayName(mapId);
                    var organTxt = RuntimeUi.Text(cardRect, "OrganName", organName, new Vector2(0, -104), new Vector2(270, 30), 18,
                        new Color(1f, 0.92f, 0.6f));
                    organTxt.fontStyle = FontStyle.Bold;

                    var waveInfo = mapId switch
                    {
                        "map_brain"   => "4 Waves | 180 ATP | 2 Tuyến",
                        "map_stomach" => "4 Waves | 210 ATP | 3 Tuyến",
                        _             => "3 Waves | 150 ATP | 1 Tuyến"
                    };
                    RuntimeUi.Text(cardRect, "MapDetails", waveInfo, new Vector2(0, -134), new Vector2(270, 24), 14,
                        new Color(0.75f, 0.85f, 0.95f));

                    // Lock Overlay if not unlocked
                    if (!unlocked)
                    {
                        cardImg.color = new Color(0.2f, 0.22f, 0.28f, 0.75f);
                        var lockOverlay = RuntimeUi.Image(cardRect, "LockOverlay", Vector2.zero, cardSize,
                            new Color(0.02f, 0.04f, 0.08f, 0.82f));
                        lockOverlay.raycastTarget = false;

                        // Warning banner
                        var lockBanner = RuntimeUi.Image(cardRect, "LockBanner", new Vector2(0, 10), new Vector2(270, 110),
                            new Color(0.18f, 0.06f, 0.08f, 0.95f));
                        lockBanner.raycastTarget = false;

                        var lockIcon = RuntimeUi.Text(cardRect, "LockIcon", "⛔ KHÓA AN NINH", new Vector2(0, 42),
                            new Vector2(260, 32), 18, new Color(1f, 0.35f, 0.35f));
                        lockIcon.fontStyle = FontStyle.Bold;

                        var prevOrgan = i > 0 && maps[i - 1] ? OrganDisplayName(maps[i - 1].Id) : "MÀN TRƯỚC";
                        var lockHint = RuntimeUi.Text(cardRect, "LockedHint", "Yêu cầu phòng thủ:\nHoàn thành màn " + prevOrgan,
                            new Vector2(0, -6), new Vector2(250, 52), 14, new Color(0.92f, 0.88f, 0.88f));
                        lockHint.fontStyle = FontStyle.Normal;
                    }

                    _mapButtons[mapId] = btn;
                    _mapCardImages[mapId] = cardImg;
                    btn.interactable = unlocked;
                }
            }

            // === SELECTION BAR (y: -175) ===
            var selBar = RuntimeUi.Image(root, "SelBar", new Vector2(0, -175), new Vector2(760, 46),
                new Color(0.06f, 0.12f, 0.22f, 0.9f));
            _selectionLabel = RuntimeUi.Text(selBar.transform, "SelectedMap", "ĐANG CHỌN: " + OrganDisplayName(_selectedMap),
                Vector2.zero, new Vector2(740, 40), 22, new Color(0.35f, 1f, 0.8f));
            _selectionLabel.fontStyle = FontStyle.Bold;

            // === MAIN BUTTONS (y: -270 to -420) ===

            // 1. PLAY BUTTON (Hero Action Button)
            var playBtnContainer = RuntimeUi.Rect(root, "PlayContainer", new Vector2(0, -270), new Vector2(460, 84));
            // Outer glow border
            RuntimeUi.Image(playBtnContainer, "PlayGlow", Vector2.zero, new Vector2(468, 92),
                new Color(0.2f, 0.95f, 0.55f, 0.6f));
            var playBtn = RuntimeUi.Button(playBtnContainer, "PlayButton", "⚔ BẮT ĐẦU CHIẾN ĐẤU ⚔",
                Vector2.zero, new Vector2(460, 84), new Color(0.12f, 0.65f, 0.36f, 0.98f), Play, 28);
            var playLabel = playBtn.GetComponentInChildren<Text>();
            if (playLabel) playLabel.fontStyle = FontStyle.Bold;

            // 2. QUIT BUTTON
            var quitBtnContainer = RuntimeUi.Rect(root, "QuitContainer", new Vector2(0, -380), new Vector2(300, 56));
            RuntimeUi.Image(quitBtnContainer, "QuitGlow", Vector2.zero, new Vector2(306, 62),
                new Color(0.85f, 0.25f, 0.25f, 0.45f));
            var quitBtn = RuntimeUi.Button(quitBtnContainer, "QuitButton", "✕ THOÁT GAME",
                Vector2.zero, new Vector2(300, 56), new Color(0.55f, 0.15f, 0.18f, 0.95f), () => _menu?.Quit(), 22);
            var quitLabel = quitBtn.GetComponentInChildren<Text>();
            if (quitLabel) quitLabel.fontStyle = FontStyle.Bold;

            // 3. RESET PROGRESS (Bottom Right)
            var resetContainer = RuntimeUi.Rect(root, "ResetContainer", new Vector2(720, -485), new Vector2(320, 48));
            var resetBtn = RuntimeUi.Button(resetContainer, "ResetProgressButton", "↺ CHƠI LẠI TỪ ĐẦU (RESET)",
                Vector2.zero, new Vector2(320, 48), new Color(0.24f, 0.18f, 0.32f, 0.95f), ResetProgress, 16);
            var resetLabel = resetBtn.GetComponentInChildren<Text>();
            if (resetLabel) resetLabel.color = new Color(0.85f, 0.8f, 0.95f);

            UpdateSelectedMapArt();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Shortcut M to toggle mute
            if (kb.mKey.wasPressedThisFrame)
            {
                ToggleMuteAudio();
            }

            // Shortcut Space or Enter to play
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                Play();
            }

            // Shortcut 1, 2, 3 to select maps
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                SelectMap("map_lung");
            }
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                if (IsMapUnlocked("map_brain")) SelectMap("map_brain");
            }
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            {
                if (IsMapUnlocked("map_stomach")) SelectMap("map_stomach");
            }

            // Shortcut Escape to quit
            if (kb.escapeKey.wasPressedThisFrame)
            {
                _menu?.Quit();
            }
        }

        private void ToggleMuteAudio()
        {
            if (_audio != null)
            {
                _audio.ToggleMute();
                UpdateMuteUi();
            }
        }

        private void UpdateMuteUi()
        {
            if (_muteBtnText == null) return;
            bool muted = _audio != null && _audio.IsMuted;
            _muteBtnText.text = muted ? "🔇 ÂM THANH: TẮT [M]" : "🔊 ÂM THANH: BẬT [M]";
            _muteBtnText.color = muted ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 1f, 0.7f);
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
            if (!IsMapUnlocked(mapId))
            {
                _audio?.Play(_art ? _art.uiError : null);
                return;
            }
            if (_session != null && !_session.SelectMap(mapId))
            {
                _audio?.Play(_art ? _art.uiError : null);
                return;
            }
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
                    pair.Value.color = new Color(0.2f, 0.22f, 0.28f, 0.75f);
                    if (_mapButtons.TryGetValue(pair.Key, out var lockedBtn) && lockedBtn)
                        lockedBtn.transform.localScale = Vector3.one;
                    if (_mapCardBorders.TryGetValue(pair.Key, out var lBorder) && lBorder)
                        lBorder.color = Color.clear;
                    continue;
                }
                bool isSelected = pair.Key == _selectedMap;
                pair.Value.color = isSelected ? Color.white : new Color(0.75f, 0.75f, 0.8f, 0.88f);
                if (_mapButtons.TryGetValue(pair.Key, out var btn) && btn)
                    btn.transform.localScale = isSelected ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;

                if (_mapCardBorders.TryGetValue(pair.Key, out var border) && border)
                {
                    border.color = isSelected ? new Color(0.25f, 0.95f, 0.85f, 0.95f) : Color.clear;
                }
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
