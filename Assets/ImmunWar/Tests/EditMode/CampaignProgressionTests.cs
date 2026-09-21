using ImmunWar.Persistence;
using ImmunWar.Progression;
using NUnit.Framework;

namespace ImmunWar.Tests.EditMode
{
    public sealed class CampaignProgressionTests
    {
        [Test]
        public void CompletionUnlocksInOrderAndRewardsOnce()
        {
            var service = new CampaignProgressionService(new[] { "map_lung", "map_brain", "map_stomach" }, SaveData.CreateDefault());
            Assert.That(service.CompleteMap("map_lung"), Is.True);
            Assert.That(service.CompleteMap("map_lung"), Is.False);
            Assert.That(service.Data.unlockedMapIds, Is.EqualTo(new[] { "map_lung", "map_brain" }));
            Assert.That(service.SelectMap("map_stomach"), Is.False);
            Assert.That(service.SelectMap("map_brain"), Is.True);
            service.CompleteMap("map_brain"); service.SelectMap("map_stomach"); service.CompleteMap("map_stomach");
            Assert.That(service.CampaignFinished, Is.True);
        }
    }
}
