using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minigame
{
    public class ObjectPooler : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject[] prefabs;
            public int size;

            public GameObject GetPrefab()
            {
                return prefabs[Random.Range(0, prefabs.Length)];
            }
        }

        [SerializeField] private Transform poolParent;
        public List<Pool> pools;
        public Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

        public static ObjectPooler Instance;

        private void Awake()
        {
            Instance = this;
            if (!poolParent)
                poolParent = transform;

            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.GetPrefab(), poolParent);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(pool.tag, objectPool);
            }
        }

        public void AddPool(PoolObjectData _data)
        {
            Pool p = new Pool();
            p.tag = _data.tag;
            p.prefabs = _data.prefabs.ToArray();
            p.size = _data.poolSize;

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < p.size; i++)
            {
                GameObject obj = Instantiate(p.GetPrefab(), poolParent);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(p.tag, objectPool);
        }


        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
                return null;
            }

            GameObject objectToSpawn = poolDictionary[tag].Dequeue();

            if (!CheckCollisions(objectToSpawn, position))
            {
                objectToSpawn.SetActive(true);
                objectToSpawn.transform.position = position;
                objectToSpawn.transform.rotation = rotation;
                objectToSpawn.transform.parent = poolParent;
            }
            else
            {
                Debug.LogWarning($"Failed to spawn and transform object {tag}!", objectToSpawn);
            }

            poolDictionary[tag].Enqueue(objectToSpawn);

            return objectToSpawn;
        }

        private bool CheckCollisions(GameObject m_go, Vector3 m_newPos)
        {
            Collider[] hitColliders = new Collider[5];
            BoxCollider boxCollider = m_go.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                int numColliders = Physics.OverlapBoxNonAlloc((boxCollider.transform.position + m_newPos) + (boxCollider.transform.rotation * boxCollider.center),
                    Vector3.Scale((boxCollider.size * 0.5f), boxCollider.transform.lossyScale),
                    hitColliders,
                    m_go.transform.rotation);
                for (int i = 0; i < numColliders; i++)
                {
                    if (hitColliders[i].gameObject.GetComponent<MovingObject>())
                    {
                        if (hitColliders[i].CompareTag(gameObject.tag))
                        {
                            if (hitColliders[i].gameObject.GetComponent<BoxCollider>())
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}