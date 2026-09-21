using ImmunWar.Battle.Events;
using ImmunWar.Presentation;
using NUnit.Framework;

namespace ImmunWar.Tests.EditMode
{
    public sealed class PresentationEventRouterTests
    {
        [Test] public void MapsEachSequenceExactlyOnceAndSuppressesDuplicates() { var router = new PresentationEventRouter(); router.Map(BattleEventType.EnemyDamaged, "hit"); var calls = 0; router.FeedbackRequested += (_, __) => calls++; var value = new BattleEvent { BattleId = "battle", Sequence = 7, Type = BattleEventType.EnemyDamaged }; router.Publish(value); router.Publish(value); Assert.That(calls, Is.EqualTo(1)); Assert.That(router.RoutedCount, Is.EqualTo(1)); }
    }
}
