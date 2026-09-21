using System;
using System.IO;
using NUnit.Framework;
using ImmunWar.Persistence;

namespace ImmunWar.Tests.EditMode
{
    public sealed class SaveDataTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ImmuneWarSaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void Validator_SanitizesDuplicatesRangesAndLockedSelection()
        {
            var data = SaveData.CreateDefault();
            data.unlockedMapIds.Add("map_lung");
            data.completedMapIds.Add("map_unknown");
            data.lastSelectedMapId = "map_brain";
            data.musicVolume = 9f;
            var result = SaveDataValidator.Sanitize(data, new[] { "map_lung", "map_brain", "map_stomach" });
            CollectionAssert.AreEqual(new[] { "map_lung" }, result.unlockedMapIds);
            Assert.That(result.completedMapIds, Is.Empty);
            Assert.That(result.lastSelectedMapId, Is.EqualTo("map_lung"));
            Assert.That(result.musicVolume, Is.EqualTo(1f));
        }

        [Test]
        public void Repository_SelectsHighestValidSequenceAndRecoversCorruption()
        {
            var repository = new SaveRepository(_directory, new[] { "map_lung", "map_brain", "map_stomach" });
            var first = SaveData.CreateDefault();
            repository.Save(first);
            var second = repository.Load();
            second.unlockedMapIds.Add("map_brain");
            repository.Save(second);
            File.WriteAllText(repository.PrimaryPath, "{broken");
            var recovered = repository.Load();
            Assert.That(recovered.unlockedMapIds, Does.Contain("map_lung"));
            Assert.That(recovered.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
        }

        [Test]
        public void Migration_UpgradesLegacyEnvelopeBeforeValidation()
        {
            const string legacy = "{\"schemaVersion\":0,\"unlockedMapIds\":[\"map_lung\"],\"completedMapIds\":[],\"lastSelectedMapId\":\"map_lung\"}";
            var migrated = SaveMigration.FromJson(legacy);
            Assert.That(migrated.schemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(migrated.musicVolume, Is.EqualTo(1f));
        }
    }
}

