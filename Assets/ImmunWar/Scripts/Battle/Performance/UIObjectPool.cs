using System.Collections.Generic;
using UnityEngine;

namespace ImmunWar.Battle.Performance
{
    /// <summary>
    /// Object pool for UI elements like preview indicators to reduce memory allocation overhead.
    /// Implements Task 9.1
    /// </summary>
    public class UIObjectPool : MonoBehaviour
    {
        private static UIObjectPool _instance;
        public static UIObjectPool Instance => _instance;

        private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
        }

        public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
            }

            if (_pools[prefab].Count > 0)
            {
                var obj = _pools[prefab].Dequeue();
                if (obj != null)
                {
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    obj.SetActive(true);
                    return obj;
                }
            }

            return Instantiate(prefab, position, rotation);
        }

        public void ReturnObject(GameObject prefab, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
            }
            _pools[prefab].Enqueue(obj);
        }
    }
}
