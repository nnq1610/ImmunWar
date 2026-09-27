using NUnit.Framework;
using ImmunWar.Core.Data;
using UnityEngine;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Tests
{
    public class SaveIntegrationTests
    {
        [Test]
        public void Property10_SaveStatePreservation_MigratesAndRecovers()
        {
            var corruptedData = default(PlacementSaveData);
            var recoveredData = SaveDataMigrationManager.Migrate(corruptedData);
            
            // Should fallback to default
            Assert.IsNotNull(recoveredData);
            Assert.AreEqual(SaveDataMigrationManager.CurrentVersion, recoveredData.Version);
            Assert.IsNotNull(recoveredData.HotkeyAssignments);

            // Test Migration
            var oldData = new PlacementSaveData { Version = 1 };
            var migratedData = SaveDataMigrationManager.Migrate(oldData);
            
            Assert.AreEqual(2, migratedData.Version);
            Assert.IsNotNull(migratedData.PlacementEfficiencyMetrics);
        }
    }
}
