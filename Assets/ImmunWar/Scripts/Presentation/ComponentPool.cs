using System;
using UnityEngine;
using UnityEngine.Pool;

namespace ImmunWar.Presentation
{
    public interface IPoolResettable { void ResetForPool(); }

    public sealed class ComponentPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<T> _pool;

        public ComponentPool(T prefab, Transform parent, int defaultCapacity = 8, int maximumSize = 128)
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _pool = new ObjectPool<T>(Create, OnGet, OnRelease, OnDestroy, true, defaultCapacity, maximumSize);
        }

        public T Get() => _pool.Get();
        public void Release(T value) { if (value) _pool.Release(value); }
        public void Clear() => _pool.Clear();
        public int CountInactive => _pool.CountInactive;
        public void Prewarm(int count)
        {
            count = Mathf.Max(0, count); var values = new T[count];
            for (var i = 0; i < count; i++) values[i] = _pool.Get();
            for (var i = 0; i < count; i++) _pool.Release(values[i]);
        }

        private T Create() => UnityEngine.Object.Instantiate(_prefab, _parent);
        private static void OnGet(T value) => value.gameObject.SetActive(true);
        private static void OnRelease(T value)
        {
            if (value is IPoolResettable resettable) resettable.ResetForPool();
            value.gameObject.SetActive(false);
        }
        private static void OnDestroy(T value) { if (value) UnityEngine.Object.Destroy(value.gameObject); }
    }
}
