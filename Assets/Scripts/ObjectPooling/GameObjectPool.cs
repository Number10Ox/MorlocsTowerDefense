using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectPooling
{
    public class GameObjectPool
    {
        private readonly Stack<GameObject> available;
        private readonly Dictionary<GameObject, IPoolable> poolableCache;
        private readonly GameObject prefab;
        private readonly Transform parent;

        public int AvailableCount => available.Count;

        public GameObjectPool(GameObject prefab, int initialCapacity, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            this.prefab = prefab;
            this.parent = parent;
            available = new Stack<GameObject>(initialCapacity);
            poolableCache = new Dictionary<GameObject, IPoolable>(initialCapacity);

            for (int i = 0; i < initialCapacity; i++)
            {
                var instance = UnityEngine.Object.Instantiate(prefab, parent);
                instance.SetActive(false);
                CachePoolable(instance);
                available.Push(instance);
            }
        }

        public GameObject Acquire(Vector3 position)
        {
            GameObject instance;
            if (available.Count > 0)
            {
                instance = available.Pop();
            }
            else
            {
                Debug.LogWarning("GameObjectPool: Pool exhausted, instantiating on demand. Consider increasing initial capacity.");
                instance = UnityEngine.Object.Instantiate(prefab, parent);
                CachePoolable(instance);
            }

            instance.transform.position = position;

            if (poolableCache.TryGetValue(instance, out IPoolable poolable))
            {
                poolable.OnPoolGet();
            }

            return instance;
        }

        public void Return(GameObject instance)
        {
            if (instance == null) return;

            if (poolableCache.TryGetValue(instance, out IPoolable poolable))
            {
                poolable.OnPoolReturn();
            }

            available.Push(instance);
        }

        public void Clear()
        {
            while (available.Count > 0)
            {
                var instance = available.Pop();
                if (instance != null)
                {
#if UNITY_EDITOR
                    UnityEngine.Object.DestroyImmediate(instance);
#else
                    UnityEngine.Object.Destroy(instance);
#endif
                }
            }

            poolableCache.Clear();
        }

        private void CachePoolable(GameObject instance)
        {
            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolableCache[instance] = poolable;
            }
        }
    }
}
