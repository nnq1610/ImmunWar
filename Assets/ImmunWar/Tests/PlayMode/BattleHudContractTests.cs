using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ImmunWar.UI;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class BattleHudContractTests
    {
        [UnityTest]
        public IEnumerator HudSnapshot_ContainsAtpVitalityWaveAndResult()
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<BattleHudController>();
            hud.ApplySnapshot(new BattleHudSnapshot(75, 80, 100, 2, 5, "Running", ""));
            Assert.That(hud.LastSnapshot.Atp, Is.EqualTo(75));
            Assert.That(hud.LastSnapshot.MaximumVitality, Is.EqualTo(100));
            Assert.That(hud.LastSnapshot.WaveIndex, Is.EqualTo(2));
            Object.Destroy(go);
            yield return null;
        }
    }
}
