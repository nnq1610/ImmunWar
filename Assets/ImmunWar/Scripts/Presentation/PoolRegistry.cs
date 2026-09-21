using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImmunWar.Presentation
{
    public sealed class PoolRegistry : MonoBehaviour
    {
        public const int EnemyCapacity = 40;
        public const int ProjectileCapacity = 96;
        public const int VfxCapacity = 64;
        private readonly Dictionary<string, object> _pools = new Dictionary<string, object>(StringComparer.Ordinal);
        public void Register<T>(string id, ComponentPool<T> pool) where T : Component => _pools.Add(id, pool);
        public ComponentPool<T> Get<T>(string id) where T : Component => _pools.TryGetValue(id, out var value) ? value as ComponentPool<T> : null;
        public void Clear() { foreach (var value in _pools.Values) value.GetType().GetMethod("Clear")?.Invoke(value, null); _pools.Clear(); }
        private void OnDestroy() => Clear();
    }
}
