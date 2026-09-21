using System;
using System.IO;
using ImmunWar.Persistence;
using NUnit.Framework;

namespace ImmunWar.Tests.EditMode
{
    public sealed class CampaignSaveIntegrationTests
    {
        private string _directory;
        [SetUp] public void Setup() => _directory = Path.Combine(Path.GetTempPath(), "ImmunWarCampaign-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

        [Test]
        public void VictoryPersistsAndCorruptPrimaryRecoversFromBackup()
        {
            var ids = new[] { "map_lung", "map_brain", "map_stomach" }; var repository = new SaveRepository(_directory, ids); var coordinator = new CampaignSaveCoordinator(repository, ids);
            Assert.That(coordinator.ConfirmVictory("map_lung"), Is.True); coordinator.SelectAndSave("map_brain");
            var reloaded = new CampaignSaveCoordinator(repository, ids); Assert.That(reloaded.Progression.Data.completedMapIds, Does.Contain("map_lung")); Assert.That(reloaded.Progression.Data.lastSelectedMapId, Is.EqualTo("map_brain"));
            File.WriteAllText(repository.PrimaryPath, "{corrupt"); reloaded.Reload(); Assert.That(reloaded.Progression.Data.unlockedMapIds, Does.Contain("map_brain"));
        }
    }
}
