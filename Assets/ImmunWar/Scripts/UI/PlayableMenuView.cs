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
        private Image _backdrop;
        private readonly Dictionary<string, Button> _mapButtons = new Dictionary<string, Button>();

        private void Start()
        {
            _session = FindFirstObjectByType<GameSession>();
            _menu = FindFirstObjectByType<MainMenuController>();
            _art = Resources.Load<PlayableArtCatalog>("ImmuneWar/PlayableArtCatalog");
            var root = RuntimeUi.Root("PlayableMenu");
            RuntimeUi.Image(root, "Background", Vector2.zero, new Vector2(1920, 1080), RuntimeUi.Background);
            if (_art && _art.lungMap)
            {
                _backdrop = RuntimeUi.Image(root, "LungBackdrop", Vector2.zero, new Vector2(1920, 1080), new Color(1f, 1f, 1f, 0.34f));
                _backdrop.sprite = _art.lungMap;
            }
            RuntimeUi.Image(root, "LeftGlow", new Vector2(-760, 300), new Vector2(510, 510), new Color(0.08f, 0.45f, 0.42f, 0.22f), true);
            RuntimeUi.Image(root, "RightGlow", new Vector2(760, -330), new Vector2(650, 650), new Color(0.1f, 0.3f, 0.52f, 0.18f), true);
            RuntimeUi.Text(root, "Title", "IMMUNE WAR", new Vector2(0, 345), new Vector2(1100, 130), 90, RuntimeUi.Accent);
            RuntimeUi.Text(root, "Subtitle", "DEFEND THE BODY", new Vector2(0, 260), new Vector2(900, 60), 33, Color.white);
            RuntimeUi.Text(root, "Prompt", "CHOOSE AN ORGAN", new Vector2(0, 145), new Vector2(800, 55), 27, new Color(0.65f, 0.81f, 0.84f));
            var catalog = _session ? _session.Catalogs.Catalog : Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            var maps = catalog ? catalog.maps : null;
            if (maps != null)
            {
                for (var i = 0; i < maps.Length; i++)
                {
                    var map = maps[i];
                    if (!map) continue;
                    var unlocked = _session == null || _session.Progress.unlockedMapIds.Contains(map.Id);
                    var mapId = map.Id;
                    var caption = map.Id.Replace("map_", "").ToUpperInvariant() + (unlocked ? "" : "  LOCKED");
                    var button = RuntimeUi.Button(root, "Map_" + mapId, caption, new Vector2((i - (maps.Length - 1) * 0.5f) * 360f, 35), new Vector2(330, 110), unlocked ? RuntimeUi.Panel : new Color(0.13f, 0.17f, 0.2f), () => SelectMap(mapId), 30);
                    RuntimeUi.Skin(button, _art, unlocked ? UiButtonRole.Secondary : UiButtonRole.Locked);
                    _mapButtons[mapId] = button;
                    button.interactable = unlocked;
                }
            }
            _selectionLabel = RuntimeUi.Text(root, "SelectedMap", "SELECTED: LUNG", new Vector2(0, -105), new Vector2(800, 55), 26, RuntimeUi.Accent);
            var play = RuntimeUi.Button(root, "PlayButton", "PLAY", new Vector2(0, -220), new Vector2(420, 100), new Color(0.1f, 0.5f, 0.43f), Play, 45);
            RuntimeUi.Skin(play, _art, UiButtonRole.Primary);
            var quit = RuntimeUi.Button(root, "QuitButton", "QUIT", new Vector2(0, -350), new Vector2(270, 68), new Color(0.17f, 0.23f, 0.31f), () => _menu?.Quit(), 27);
            RuntimeUi.Skin(quit, _art, UiButtonRole.Secondary);
            UpdateSelectedMapArt();
        }

        private void SelectMap(string mapId)
        {
            if (_session != null && !_session.SelectMap(mapId)) return;
            _selectedMap = mapId;
            if (_selectionLabel) _selectionLabel.text = "SELECTED: " + mapId.Replace("map_", "").ToUpperInvariant();
            UpdateSelectedMapArt();
        }

        private Sprite MapSprite(string mapId) => _art ? mapId switch
        {
            "map_brain" => _art.brainMap ? _art.brainMap : _art.lungMap,
            "map_stomach" => _art.stomachMap ? _art.stomachMap : _art.lungMap,
            _ => _art.lungMap
        } : null;

        private void UpdateSelectedMapArt()
        {
            if (_backdrop) _backdrop.sprite = MapSprite(_selectedMap);
            foreach (var pair in _mapButtons)
                if (pair.Value && pair.Value.interactable)
                    RuntimeUi.Skin(pair.Value, _art,
                        pair.Key == _selectedMap ? UiButtonRole.Primary : UiButtonRole.Secondary,
                        pair.Key == _selectedMap);
        }

        public void Play()
        {
            if (_session != null) _session.SelectMap(_selectedMap);
            if (_menu) _menu.StartSelectedBattle();
        }
    }
}
