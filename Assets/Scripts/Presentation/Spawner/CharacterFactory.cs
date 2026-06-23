using System.Collections.Generic;
using DwarfsCrypt.Presentation.Combat;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Spawner
{
    /// <summary>
    /// Pool-backed factory. Inactive instances are kept per prefab and reused on Spawn.
    /// Callers are responsible for returning dead units via Release.
    /// </summary>
    public class CharacterFactory : MonoBehaviour
    {
        // Keyed by prefab instance ID so we never confuse pools across different prefabs.
        private readonly Dictionary<int, Stack<GameObject>> _pools = new();

        /// <summary>Spawn one instance at <paramref name="position"/>, injecting config from JSON.</summary>
        public GameObject Spawn(GameObject prefab, string configPath, Vector3 position, Transform parent = null)
        {
            var go = GetOrCreate(prefab);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.SetPositionAndRotation(position, Quaternion.identity);

            if (!string.IsNullOrEmpty(configPath))
                go.GetComponentInChildren<CharacterComponent>(true)?.Initialize(configPath);

            go.SetActive(true);
            return go;
        }

        /// <summary>Return a live instance to the pool (deactivates it).</summary>
        public void Release(GameObject instance, GameObject prefab)
        {
            if (instance == null) return;
            instance.SetActive(false);
            Push(prefab.GetInstanceID(), instance);
        }

        /// <summary>
        /// Pre-instantiate <paramref name="count"/> inactive instances so future Spawns are allocation-free.
        /// Config is injected now so warm instances are ready to activate.
        /// </summary>
        public void PreWarm(GameObject prefab, string configPath, int count)
        {
            int key = prefab.GetInstanceID();
            for (int i = 0; i < count; i++)
                Push(key, CreateInactive(prefab, configPath));
        }

        private GameObject GetOrCreate(GameObject prefab)
        {
            int key = prefab.GetInstanceID();
            if (_pools.TryGetValue(key, out var stack) && stack.Count > 0)
                return stack.Pop();

            // configPath is injected later by Spawn so we pass null here
            return CreateInactive(prefab, null);
        }

        // Instantiates from an inactive copy of the prefab so Awake does not fire yet.
        // Config injection happens before the object is ever activated.
        private static GameObject CreateInactive(GameObject prefab, string configPath)
        {
            bool wasActive = prefab.activeSelf;
            if (wasActive) prefab.SetActive(false);

            var go = Instantiate(prefab);

            if (wasActive) prefab.SetActive(true);

            if (!string.IsNullOrEmpty(configPath))
                go.GetComponentInChildren<CharacterComponent>(true)?.Initialize(configPath);

            return go; // still inactive — caller activates when ready
        }

        private void Push(int key, GameObject go)
        {
            if (!_pools.ContainsKey(key)) _pools[key] = new Stack<GameObject>();
            _pools[key].Push(go);
        }
    }
}
