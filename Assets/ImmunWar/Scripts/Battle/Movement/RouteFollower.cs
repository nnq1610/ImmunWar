using System;
using UnityEngine;
using ImmunWar.Battle.State;

namespace ImmunWar.Battle.Movement
{
    public sealed class RouteFollower
    {
        private readonly EnemyState _enemy;
        private readonly Vector2[] _waypoints;
        private readonly float _speed;
        private readonly float _totalLength;
        private readonly float _travelLength;
        private float _distance;
        private bool _arrivalReported;
        public Vector2 Position { get; private set; }
        public Vector2 Destination => _waypoints[^1];
        public float Distance => _distance;

        /// <summary>Places the follower at a distance along the route, e.g. for enemies split off a parent.</summary>
        public void Warp(float distance)
        {
            _distance = Math.Max(0f, Math.Min(_travelLength, distance));
            _enemy.RouteProgress = _travelLength <= 0f ? 1f : _distance / _travelLength;
            Position = Evaluate(_distance);
        }

        /// <param name="endInset">Distance before the last waypoint at which the enemy counts as arrived,
        /// so it stops at the edge of the organ instead of walking into its centre.</param>
        public RouteFollower(EnemyState enemy, Vector2[] waypoints, float speed, float endInset = 0f)
        {
            _enemy = enemy;
            _waypoints = waypoints ?? throw new ArgumentNullException(nameof(waypoints));
            if (_waypoints.Length < 2) throw new ArgumentException("A route needs at least two waypoints.", nameof(waypoints));
            _speed = Math.Max(0f, speed);
            for (var i = 1; i < _waypoints.Length; i++) _totalLength += Vector2.Distance(_waypoints[i - 1], _waypoints[i]);
            _travelLength = Math.Max(0f, _totalLength - Math.Max(0f, endInset));
            Position = _waypoints[0];
        }

        public bool Tick(float seconds)
        {
            if (_enemy.IsTerminal || !string.IsNullOrEmpty(_enemy.BlockedById)) return false;
            _distance = Math.Min(_travelLength, _distance + _speed * Math.Max(0f, seconds));
            _enemy.RouteProgress = _travelLength <= 0f ? 1f : _distance / _travelLength;
            Position = Evaluate(_distance);
            if (_enemy.RouteProgress < 1f || _arrivalReported) return false;
            _arrivalReported = true;
            _enemy.TerminalResult = EnemyTerminalResult.ReachedOrgan;
            return true;
        }

        private Vector2 Evaluate(float targetDistance)
        {
            var traversed = 0f;
            for (var i = 1; i < _waypoints.Length; i++)
            {
                var length = Vector2.Distance(_waypoints[i - 1], _waypoints[i]);
                if (traversed + length >= targetDistance) return Vector2.Lerp(_waypoints[i - 1], _waypoints[i], length <= 0f ? 1f : (targetDistance - traversed) / length);
                traversed += length;
            }
            return _waypoints[^1];
        }
    }
}

