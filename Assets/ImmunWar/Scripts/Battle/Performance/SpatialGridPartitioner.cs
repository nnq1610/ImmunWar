using System.Collections.Generic;
using UnityEngine;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Battle.Performance
{
    /// <summary>
    /// Spatial partitioning system to optimize grid queries (Task 9.2)
    /// </summary>
    public class SpatialGridPartitioner
    {
        private readonly int _chunkSize;
        private readonly Dictionary<Vector2Int, List<GridPosition>> _chunks;

        public SpatialGridPartitioner(int chunkSize = 4)
        {
            _chunkSize = chunkSize;
            _chunks = new Dictionary<Vector2Int, List<GridPosition>>();
        }

        public void AddPosition(GridPosition pos)
        {
            Vector2Int chunkCoord = GetChunkCoordinate(pos);
            if (!_chunks.ContainsKey(chunkCoord))
            {
                _chunks[chunkCoord] = new List<GridPosition>();
            }
            _chunks[chunkCoord].Add(pos);
        }

        public IEnumerable<GridPosition> GetPositionsInChunk(GridPosition pos)
        {
            Vector2Int chunkCoord = GetChunkCoordinate(pos);
            if (_chunks.TryGetValue(chunkCoord, out var list))
            {
                return list;
            }
            return new List<GridPosition>();
        }
        
        public IEnumerable<GridPosition> GetPositionsInRadius(GridPosition center, int radius)
        {
            var results = new List<GridPosition>();
            
            // Calculate chunk bounding box
            int minChunkX = (center.X - radius) / _chunkSize;
            int maxChunkX = (center.X + radius) / _chunkSize;
            int minChunkY = (center.Y - radius) / _chunkSize;
            int maxChunkY = (center.Y + radius) / _chunkSize;

            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                for (int cy = minChunkY; cy <= maxChunkY; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(cx, cy);
                    if (_chunks.TryGetValue(chunkCoord, out var list))
                    {
                        foreach (var pos in list)
                        {
                            int dist = Mathf.Abs(pos.X - center.X) + Mathf.Abs(pos.Y - center.Y);
                            if (dist <= radius)
                            {
                                results.Add(pos);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private Vector2Int GetChunkCoordinate(GridPosition pos)
        {
            return new Vector2Int(pos.X / _chunkSize, pos.Y / _chunkSize);
        }
    }
}
