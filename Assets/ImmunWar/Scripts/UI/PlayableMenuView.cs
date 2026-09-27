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
        private readonly Dictionary<string, Button> _mapButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Image> _mapCardImages = new Dictionary<string, Image>();

        private void Start()
        {
            _session = FindFirstObjectByType<GameSession>();
            _menu = FindFirstObjectByType<MainMenuController>();
            _art = Resources.Load<PlayableArtCatalog>("ImmuneWar/PlayableArtCatalog");
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
            RuntimeUi.Text(root, "Prompt", "— CHOOSE AN ORGAN —", new Vector2(0, 235), new Vector2(900, 50), 26, new Color(0.95f, 0.88f, 0.55f));

            // === ORGAN CARDS (y: 40, height 260 → spans -90 to +170) ===
            var catalog = _session ? _session.Catalogs.Catalog : Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
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
                    var unlocked = _session == null || _session.Progress.unlockedMapIds.Contains(map.Id);
                    var mapId = map.Id;

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

                    if (!unlocked)
                    {
                        cardImg.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
                        RuntimeUi.Text(cardRect, "Locked", "LOCKED", new Vector2(0, -100), new Vector2(240, 40), 22, new Color(1f, 0.3f, 0.3f));
                    }

                    _mapButtons[mapId] = btn;
                    _mapCardImages[mapId] = cardImg;
                    btn.interactable = unlocked;
                }
            }

            // === SELECTION LABEL (y: -145) ===
            _selectionLabel = RuntimeUi.Text(root, "SelectedMap", "SELECTED: LUNG",
                new Vector2(0, -145), new Vector2(800, 50), 26, RuntimeUi.Accent);

            // === PLAY BUTTON (y: -240) ===
            var play = RuntimeUi.Button(root, "PlayButton", "", new Vector2(0, -240), new Vector2(340, 120), Color.white, Play, 48);
            RuntimeUi.Skin(play, _art, UiButtonRole.Primary);
            // Ensure play button doesn't squish by preserving aspect
            var pi = play.GetComponent<Image>();
            pi.type = Image.Type.Simple;
            pi.preserveAspect = true;

            // === QUIT BUTTON — plain red, no sprite dependency (y: -360) ===
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

            UpdateSelectedMapArt();
        }

        private void SelectMap(string mapId)
        {
            if (_session != null && !_session.SelectMap(mapId)) return;
            _selectedMap = mapId;
            if (_selectionLabel) _selectionLabel.text = "SELECTED: " + mapId.Replace("map_", "").ToUpperInvariant();
            UpdateSelectedMapArt();
        }

        private void UpdateSelectedMapArt()
        {
            foreach (var pair in _mapCardImages)
            {
                if (!pair.Value) continue;
                bool isSelected = pair.Key == _selectedMap;
                pair.Value.color = isSelected ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.82f);
                if (_mapButtons.TryGetValue(pair.Key, out var btn) && btn)
                    btn.transform.localScale = isSelected ? new Vector3(1.1f, 1.1f, 1f) : Vector3.one;
            }
        }

        public void Play()
        {
            if (_session != null) _session.SelectMap(_selectedMap);
            if (_menu) _menu.StartSelectedBattle();
        }
    }
}

