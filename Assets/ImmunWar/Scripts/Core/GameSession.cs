using ImmunWar.Core.Config;
using ImmunWar.Persistence;
using UnityEngine;

namespace ImmunWar.Core
{
    public sealed class GameSession : MonoBehaviour
    {
        public CatalogLoader Catalogs { get; private set; }
        public SaveRepository Saves { get; private set; }
        public SettingsRepository Settings { get; private set; }
        public SaveData Progress { get; private set; }
        public string SelectedMapId { get; private set; } = "map_lung";

        public void Initialize(GameCatalog catalog, string saveDirectory = null)
        {
            Catalogs = new CatalogLoader(catalog);
            var mapIds = catalog.maps == null ? new[] { "map_lung" } : System.Array.ConvertAll(catalog.maps, x => x.Id);
            Saves = new SaveRepository(saveDirectory ?? Application.persistentDataPath, mapIds);
            Settings = new SettingsRepository(Saves);
            Progress = Saves.Load();
            SelectedMapId = Progress.lastSelectedMapId;
            DontDestroyOnLoad(gameObject);
        }

        public bool SelectMap(string mapId)
        {
            if (Progress == null || !Progress.unlockedMapIds.Contains(mapId)) return false;
            SelectedMapId = mapId;
            return true;
        }

        public void ReplaceProgress(SaveData data) => Progress = data;
    }
}

