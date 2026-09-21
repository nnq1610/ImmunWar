using System;
using ImmunWar.Core.Config;
using ImmunWar.Progression;
using UnityEngine;

namespace ImmunWar.UI
{
    public readonly struct MapCardState
    {
        public string MapId { get; }
        public bool Unlocked { get; }
        public bool Completed { get; }
        public MapCardState(string mapId, bool unlocked, bool completed) { MapId = mapId; Unlocked = unlocked; Completed = completed; }
    }

    public sealed class MapSelectController : MonoBehaviour
    {
        private CampaignProgressionService _progression;
        private OrganMapConfig[] _maps = Array.Empty<OrganMapConfig>();
        public event Action<string> LaunchRequested;
        public void Initialize(CampaignProgressionService progression, OrganMapConfig[] maps) { _progression = progression; _maps = maps ?? Array.Empty<OrganMapConfig>(); }
        public MapCardState[] Cards()
        {
            var cards = new MapCardState[_maps.Length];
            for (var i = 0; i < _maps.Length; i++) cards[i] = new MapCardState(_maps[i].Id, _progression.Data.unlockedMapIds.Contains(_maps[i].Id), _progression.Data.completedMapIds.Contains(_maps[i].Id));
            return cards;
        }
        public bool SelectAndLaunch(string mapId) { if (_progression == null || !_progression.SelectMap(mapId)) return false; LaunchRequested?.Invoke(mapId); return true; }
    }
}
