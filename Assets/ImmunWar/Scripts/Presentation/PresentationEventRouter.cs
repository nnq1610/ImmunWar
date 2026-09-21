using System;
using System.Collections.Generic;
using ImmunWar.Battle.Events;

namespace ImmunWar.Presentation
{
    public sealed class PresentationEventRouter : IBattleEventSink
    {
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<BattleEventType, string> _feedback = new Dictionary<BattleEventType, string>();
        public event Action<string, BattleEvent> FeedbackRequested;
        public int RoutedCount { get; private set; }
        public void Map(BattleEventType type, string feedbackId) { if (!string.IsNullOrWhiteSpace(feedbackId)) _feedback[type] = feedbackId; }
        public void Publish(BattleEvent value)
        {
            if (value == null) return; var key = $"{value.BattleId}:{value.Sequence}"; if (!_seen.Add(key)) return;
            if (!_feedback.TryGetValue(value.Type, out var feedbackId)) return; RoutedCount++; FeedbackRequested?.Invoke(feedbackId, value);
        }
        public void ResetBattle(string battleId) { _seen.RemoveWhere(key => key.StartsWith(battleId + ":", StringComparison.Ordinal)); }
    }
}
