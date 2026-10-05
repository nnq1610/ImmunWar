using System.Collections.Generic;
using UnityEngine;

namespace ImmunWar.Battle.Movement
{
    public readonly struct EngageCandidate
    {
        public string Id { get; }
        public Vector2 Position { get; }
        /// <summary>Defender currently holding this enemy, or null.</summary>
        public string BlockedById { get; }
        public EngageCandidate(string id, Vector2 position, string blockedById) { Id = id; Position = position; BlockedById = blockedById; }
    }

    /// <summary>
    /// Mobile melee defenders guard a circle around their placement point: they walk out to the enemy closest to
    /// home, hold it in melee, and walk back once the circle is clear.
    /// </summary>
    public static class MeleeEngagement
    {
        /// <summary>Gap kept between a mobile defender and the enemy it fights (world units).</summary>
        public const float ContactDistance = 0.6f;
        /// <summary>A chased enemy is dropped once it is this far outside the guard circle.</summary>
        public const float LeashSlack = 0.6f;

        public static string ChooseTarget(string defenderId, Vector2 home, float engageRadius, string currentTargetId, IReadOnlyList<EngageCandidate> enemies)
        {
            string best = null;
            var bestScore = float.MaxValue;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var fromHome = Vector2.Distance(home, enemy.Position);
                if (enemy.Id == currentTargetId && fromHome <= engageRadius + LeashSlack) return enemy.Id;
                if (fromHome > engageRadius) continue;
                // Enemies already held by another defender are a last resort.
                var taken = !string.IsNullOrEmpty(enemy.BlockedById) && enemy.BlockedById != defenderId;
                var score = fromHome + (taken ? 100f : 0f);
                if (score < bestScore || score == bestScore && string.CompareOrdinal(enemy.Id, best) < 0) { best = enemy.Id; bestScore = score; }
            }
            return best;
        }

        /// <summary>Where to stand: next to the target on the side facing the defender, or back home.</summary>
        public static Vector2 Goal(Vector2 home, Vector2 position, Vector2? target)
        {
            if (!target.HasValue) return home;
            var offset = position - target.Value;
            var direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : (home - target.Value).sqrMagnitude > 0.0001f ? (home - target.Value).normalized : Vector2.left;
            return target.Value + direction * ContactDistance;
        }

        public static Vector2 Step(Vector2 position, Vector2 goal, float speed, float seconds) =>
            Vector2.MoveTowards(position, goal, Mathf.Max(0f, speed) * Mathf.Max(0f, seconds));

        /// <summary>A capacity of 0 or less means the defender can hold any number of enemies.</summary>
        public static bool HasCapacity(int held, int capacity) => capacity <= 0 || held < capacity;
    }
}
