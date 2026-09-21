using System;
using System.Collections;
using System.IO;
using ImmunWar.Core.Config;
using ImmunWar.Persistence;
using ImmunWar.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class CampaignJourneyTests
    {
        [UnityTest]
        public IEnumerator MapSelectBattleResultAndReloadPreserveUnlock()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ImmunWarJourney-" + Guid.NewGuid().ToString("N")); var ids = new[] { "map_lung", "map_brain", "map_stomach" };
            var coordinator = new CampaignSaveCoordinator(new SaveRepository(directory, ids), ids); var go = new GameObject("MapSelect"); var controller = go.AddComponent<MapSelectController>();
            var maps = new OrganMapConfig[3]; for (var i = 0; i < maps.Length; i++) { maps[i] = ScriptableObject.CreateInstance<OrganMapConfig>(); maps[i].SetIdForEditor(ids[i]); }
            controller.Initialize(coordinator.Progression, maps); Assert.That(controller.SelectAndLaunch("map_lung"), Is.True); Assert.That(coordinator.ConfirmVictory("map_lung"), Is.True); coordinator.Reload();
            Assert.That(coordinator.Progression.Data.unlockedMapIds, Does.Contain("map_brain")); Assert.That(coordinator.Progression.Data.completedMapIds, Does.Contain("map_lung"));
            foreach (var map in maps) UnityEngine.Object.Destroy(map); UnityEngine.Object.Destroy(go); yield return null; Directory.Delete(directory, true);
        }
    }
}
