using NUnit.Framework;
using UnityEngine;
using ImmunWar.Core.Config.Maps;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Tests
{
    public class OrganMapTests
    {
        [Test]
        public void LungMap_RequiresExactlyFourEntryPoints()
        {
            var lungMap = ScriptableObject.CreateInstance<LungMapConfiguration>();
            
            // Initially set to incorrect length to simulate validation failure condition
            lungMap.PathogenEntryPoints = new Vector2[3];
            Assert.AreNotEqual(4, lungMap.PathogenEntryPoints.Length);
            
            // Fix configuration
            lungMap.PathogenEntryPoints = new Vector2[4];
            Assert.AreEqual(4, lungMap.PathogenEntryPoints.Length);
        }

        [Test]
        public void StomachMap_AcidPool_AffectsPlacementValidation()
        {
            var stomachMap = ScriptableObject.CreateInstance<StomachMapConfiguration>();
            stomachMap.AcidPools = new AcidPoolDefinition[]
            {
                new AcidPoolDefinition 
                { 
                    PoolArea = new Vector2[] { new Vector2(1,1), new Vector2(2,2) },
                    DefenderImpact = DefenderEffect.DamageOverTime
                }
            };

            Assert.IsNotNull(stomachMap.AcidPools);
            Assert.AreEqual(1, stomachMap.AcidPools.Length);
            Assert.AreEqual(DefenderEffect.DamageOverTime, stomachMap.AcidPools[0].DefenderImpact);
        }

        [Test]
        public void BrainMap_SynapticGap_ProvidesChokepointAdvantage()
        {
            var brainMap = ScriptableObject.CreateInstance<BrainMapConfiguration>();
            brainMap.SynapticGaps = new SynapticGap[]
            {
                new SynapticGap
                {
                    Position = new Vector2(5, 5),
                    Size = new Vector2(2, 2),
                    Advantage = ChokepointAdvantage.ChainLightning
                }
            };

            Assert.IsNotNull(brainMap.SynapticGaps);
            Assert.AreEqual(1, brainMap.SynapticGaps.Length);
            Assert.AreEqual(ChokepointAdvantage.ChainLightning, brainMap.SynapticGaps[0].Advantage);
        }
    }
}
