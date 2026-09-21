using System.Collections.Generic;
using ImmunWar.Progression;

namespace ImmunWar.Persistence
{
    public sealed class CampaignSaveCoordinator
    {
        private readonly SaveRepository _repository;
        private readonly IReadOnlyList<string> _mapIds;
        public CampaignProgressionService Progression { get; private set; }
        public CampaignSaveCoordinator(SaveRepository repository, IReadOnlyList<string> mapIds) { _repository = repository; _mapIds = mapIds; Reload(); }
        public void Reload() => Progression = new CampaignProgressionService(_mapIds, _repository.Load());
        public bool ConfirmVictory(string mapId) { var changed = Progression.CompleteMap(mapId); if (changed) _repository.Save(Progression.Data); return changed; }
        public bool SelectAndSave(string mapId) { if (!Progression.SelectMap(mapId)) return false; _repository.Save(Progression.Data); return true; }
    }
}
