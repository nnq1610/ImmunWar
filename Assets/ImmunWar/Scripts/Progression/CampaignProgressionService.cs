using System;
using System.Collections.Generic;
using System.Linq;
using ImmunWar.Persistence;

namespace ImmunWar.Progression
{
    public sealed class CampaignProgressionService
    {
        private readonly string[] _orderedMapIds;
        public SaveData Data { get; }
        public bool CampaignFinished => _orderedMapIds.Length > 0 && _orderedMapIds.All(Data.completedMapIds.Contains);

        public CampaignProgressionService(IEnumerable<string> orderedMapIds, SaveData data = null)
        {
            _orderedMapIds = (orderedMapIds ?? Array.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
            if (_orderedMapIds.Length == 0) _orderedMapIds = new[] { "map_lung" };
            Data = data ?? SaveData.CreateDefault();
            Data.unlockedMapIds ??= new List<string>(); Data.completedMapIds ??= new List<string>();
            if (!Data.unlockedMapIds.Contains(_orderedMapIds[0])) Data.unlockedMapIds.Insert(0, _orderedMapIds[0]);
            if (!Data.unlockedMapIds.Contains(Data.lastSelectedMapId)) Data.lastSelectedMapId = _orderedMapIds[0];
        }

        public bool CompleteMap(string mapId)
        {
            var index = Array.IndexOf(_orderedMapIds, mapId);
            if (index < 0 || !Data.unlockedMapIds.Contains(mapId) || Data.completedMapIds.Contains(mapId)) return false;
            Data.completedMapIds.Add(mapId);
            if (index + 1 < _orderedMapIds.Length && !Data.unlockedMapIds.Contains(_orderedMapIds[index + 1])) Data.unlockedMapIds.Add(_orderedMapIds[index + 1]);
            return true;
        }

        public bool SelectMap(string mapId)
        {
            if (string.IsNullOrWhiteSpace(mapId) || !Data.unlockedMapIds.Contains(mapId) || !_orderedMapIds.Contains(mapId)) return false;
            Data.lastSelectedMapId = mapId; return true;
        }
    }
}
