using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Battle.Movement
{
    public enum RouteType
    {
        Primary,
        Secondary,
        Alternate,
        Boss
    }

    public enum ChokepointType
    {
        Minor,
        Major,
        Critical
    }

    [Serializable]
    public struct PathSegment
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public float Length;
        public float ComplexityMultiplier;

        public PathSegment(Vector2 start, Vector2 end, float complexity = 1.0f)
        {
            StartPoint = start;
            EndPoint = end;
            Length = Vector2.Distance(start, end);
            ComplexityMultiplier = complexity;
        }
    }

    [Serializable]
    public struct PathRoute
    {
        public string RouteId;
        public Vector2[] Waypoints;
        public RouteType Type;
        public float BaseSpeed;
        public PathSegment[] Segments;
    }

    [Serializable]
    public struct Chokepoint
    {
        public Vector2 Position;
        public string[] ConvergingRoutes;
        public float InfluenceRadius;
        public ChokepointType Type;
    }

    /// <summary>
    /// Manages multiple pathogen routes and strategic chokepoint analysis.
    /// Implements Task 4 of the immunwar-placement-ui-redesign feature.
    /// </summary>
    public class PathSystem
    {
        private readonly Dictionary<string, PathRoute> _routes;
        private readonly List<Chokepoint> _chokepoints;
        private readonly GridSystem _gridSystem;
        private int _spawnDistributionCounter = 0;

        public PathSystem(GridSystem gridSystem, IEnumerable<PathRoute> routes, IEnumerable<Chokepoint> chokepoints)
        {
            _gridSystem = gridSystem ?? throw new ArgumentNullException(nameof(gridSystem));
            _routes = new Dictionary<string, PathRoute>();
            _chokepoints = new List<Chokepoint>();

            if (routes != null)
            {
                foreach (var route in routes)
                {
                    _routes[route.RouteId] = route;
                }
            }

            if (chokepoints != null)
            {
                _chokepoints.AddRange(chokepoints);
            }
        }

        /// <summary>
        /// Gets all active pathogen routes.
        /// </summary>
        public IEnumerable<PathRoute> GetActiveRoutes()
        {
            return _routes.Values;
        }

        /// <summary>
        /// Retrieves a specific route by ID.
        /// </summary>
        public PathRoute GetRoute(string routeId)
        {
            return _routes.TryGetValue(routeId, out var route) ? route : default;
        }

        /// <summary>
        /// Calculates the exact world position on a route given a normalized progress [0, 1].
        /// Applies timing-based mechanics depending on segment complexity.
        /// </summary>
        public Vector2 GetRoutePosition(string routeId, float normalizedProgress)
        {
            if (!_routes.TryGetValue(routeId, out var route) || route.Segments == null || route.Segments.Length == 0)
                return Vector2.zero;

            normalizedProgress = Mathf.Clamp01(normalizedProgress);
            
            float totalLength = route.Segments.Sum(s => s.Length * s.ComplexityMultiplier);
            float targetDistance = totalLength * normalizedProgress;
            float currentDistance = 0f;

            foreach (var segment in route.Segments)
            {
                float adjustedLength = segment.Length * segment.ComplexityMultiplier;
                if (currentDistance + adjustedLength >= targetDistance)
                {
                    float segmentProgress = (targetDistance - currentDistance) / adjustedLength;
                    return Vector2.Lerp(segment.StartPoint, segment.EndPoint, segmentProgress);
                }
                currentDistance += adjustedLength;
            }

            // Fallback to last point
            return route.Segments.Last().EndPoint;
        }

        /// <summary>
        /// Distributes pathogens across multiple routes based on round-robin logic or enemy type matching.
        /// </summary>
        public string GetNextRouteForPathogen(string enemyType)
        {
            if (_routes.Count == 0) return null;

            // Simple distribution logic: boss types prioritize Boss routes
            if (enemyType != null && enemyType.Contains("Boss"))
            {
                var bossRoute = _routes.Values.FirstOrDefault(r => r.Type == RouteType.Boss);
                if (bossRoute.RouteId != null)
                    return bossRoute.RouteId;
            }

            // Regular distribution logic
            var standardRoutes = _routes.Values.Where(r => r.Type != RouteType.Boss).ToList();
            if (standardRoutes.Count == 0) standardRoutes = _routes.Values.ToList();

            var route = standardRoutes[_spawnDistributionCounter % standardRoutes.Count];
            _spawnDistributionCounter++;
            return route.RouteId;
        }

        /// <summary>
        /// Gets all registered chokepoints where multiple routes converge.
        /// </summary>
        public IEnumerable<Chokepoint> GetChokepoints()
        {
            return _chokepoints;
        }

        /// <summary>
        /// Calculates route coverage for a given position and range.
        /// </summary>
        public IEnumerable<string> GetRoutesInRange(Vector2 position, float range)
        {
            var routesInRange = new HashSet<string>();
            float rangeSqr = range * range;

            foreach (var route in _routes.Values)
            {
                if (route.Segments == null) continue;
                
                foreach (var segment in route.Segments)
                {
                    float distSqr = SqrDistanceToSegment(position, segment.StartPoint, segment.EndPoint);
                    if (distSqr <= rangeSqr)
                    {
                        routesInRange.Add(route.RouteId);
                        break;
                    }
                }
            }

            return routesInRange;
        }

        /// <summary>
        /// Identifies if a position is highly strategic for defender placement 
        /// (e.g. covers multiple routes or a major chokepoint).
        /// </summary>
        public bool IsStrategicPosition(GridPosition gridPos)
        {
            Vector2 worldPos = _gridSystem.GridToWorldPosition(gridPos);
            float checkRange = _gridSystem.CellSize.x * 2.5f; 
            
            var coveredRoutes = GetRoutesInRange(worldPos, checkRange);
            if (coveredRoutes.Count() >= 2)
            {
                return true;
            }

            foreach (var cp in _chokepoints)
            {
                if (Vector2.Distance(worldPos, cp.Position) <= cp.InfluenceRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private float SqrDistanceToSegment(Vector2 pt, Vector2 p1, Vector2 p2)
        {
            Vector2 v = p2 - p1;
            Vector2 w = pt - p1;

            float c1 = Vector2.Dot(w, v);
            if (c1 <= 0)
                return Vector2.SqrMagnitude(pt - p1);

            float c2 = Vector2.Dot(v, v);
            if (c2 <= c1)
                return Vector2.SqrMagnitude(pt - p2);

            float b = c1 / c2;
            Vector2 pb = p1 + b * v;
            return Vector2.SqrMagnitude(pt - pb);
        }
    }
}
