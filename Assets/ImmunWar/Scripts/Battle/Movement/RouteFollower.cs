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
        private float _distance;
        private bool _arrivalReported;
        public Vector2 Position { get; private set; }

        public RouteFollower(EnemyState enemy, Vector2[] waypoints, float speed)
        {
            _enemy = enemy;
            _waypoints = waypoints ?? throw new ArgumentNullException(nameof(waypoints));
            if (_waypoints.Length < 2) throw new ArgumentException("A route needs at least two waypoints.", nameof(waypoints));
            _speed = Math.Max(0f, speed);
            for (var i = 1; i < _waypoints.Length; i++) _totalLength += Vector2.Distance(_waypoints[i - 1], _waypoints[i]);
            Position = _waypoints[0];
        }

        public bool Tick(float seconds)
        {
            if (_enemy.IsTerminal || !string.IsNullOrEmpty(_enemy.BlockedById)) return false;
            _distance = Math.Min(_totalLength, _distance + _speed * Math.Max(0f, seconds));
            _enemy.RouteProgress = _totalLength <= 0f ? 1f : _distance / _totalLength;
            Position = Evaluate(_enemy.RouteProgress);
            if (_enemy.RouteProgress < 1f || _arrivalReported) return false;
            _arrivalReported = true;
            _enemy.TerminalResult = EnemyTerminalResult.ReachedOrgan;
            return true;
        }

        private Vector2 Evaluate(float normalized)
        {
            var targetDistance = normalized * _totalLength;
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

