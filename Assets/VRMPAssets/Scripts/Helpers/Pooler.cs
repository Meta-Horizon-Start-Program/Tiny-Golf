using UnityEngine;
using UnityEngine.Pool;

namespace XRMultiplayer
{
    public class Pooler : MonoBehaviour
    {
        /// <summary>
        /// The Prefab to spawn and use for pooling.
        /// </summary>
        [SerializeField, Tooltip("The Prefab to spawn and use for pooling")]
        GameObject m_SpawnPrefab;

        /// <summary>
        /// Collection checks are performed when an instance is returned back to the pool.
        /// An exception will be thrown if the instance is already in the pool.
        /// Collection checks are only performed in the Editor.
        /// </summary>
        [SerializeField, Tooltip("An exception will be thrown if the instance is already in the pool")]
        bool m_UseCollectionChecks = true;

        /// <summary>
        /// The default capacity the pool will be created with.
        /// </summary>
        [SerializeField, Tooltip("he default capacity the pool will be created with")]
        int m_DefaultCapacity = 30;

        /// <summary>
        /// The maximum size of the pool.
        /// When the pool reaches the max size then any further instances returned to the pool will be ignored and can be garbage collected.
        /// This can be used to prevent the pool growing to a very large size
        /// </summary>
        [SerializeField, Tooltip("The maximum size of the pool")]
        int m_MaxCapacity = 1000;

        /// <summary>
        /// If true, the spawned object will be parented under the transform of the Pooler.
        /// </summary>
        [SerializeField, Tooltip("Spawned objects will be parented under this Transform")]
        bool m_ParentUnderTransform = false;

        IObjectPool<GameObject> m_Pool;

        protected virtual void Start()
        {
            InitializePool();
        }

        protected void InitializePool()
        {
            m_Pool = new ObjectPool<GameObject>(CreateNewObject, OnTakeFromPool, OnReturnToPool,
            OnDestroyPoolObject, m_UseCollectionChecks, m_DefaultCapacity, m_MaxCapacity);
        }

        protected GameObject CreateNewObject()
        {
            GameObject spawnedObject = Instantiate(m_SpawnPrefab);
            if (m_ParentUnderTransform)
                spawnedObject.transform.SetParent(transform);

            return spawnedObject;
        }

        /// <summary>
        /// Called when an instance is taken from the pool.
        /// </summary>
        protected void OnTakeFromPool(GameObject go)
        {
            go.SetActive(true);
        }


        /// <summary>
        /// Called when returning an instance to the pool.
        /// </summary>
        protected void OnReturnToPool(GameObject go)
        {
            go.SetActive(false);
        }

        /// <summary>
        /// Called when returning an instance to a pool that is full, or when called <see cref="ObjectPool.Dispose"/>, or <see cref="ObjectPool.Clear"/>
        /// </summary>
        /// <param name="go"></param>
        protected void OnDestroyPoolObject(GameObject go)
        {
            Destroy(go);
        }

        /// <summary>
        /// Get an instance from the pool. If the pool is empty then a new instance will be created.
        /// </summary>
        public GameObject GetItem()
        {
            return m_Pool.Get();
        }

        /// <summary>
        /// Returns the instance back to the pool. Returning an instance to a pool that is full will cause the instance to be destroyed.
        /// </summary>
        /// <param name="item"></param>
        public void ReturnItem(GameObject item)
        {
            m_Pool.Release(item);
        }
    }
}
