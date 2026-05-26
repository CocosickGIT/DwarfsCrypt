using System.Collections.Generic;
using UnityEngine;
using Presentation.Features;

namespace DwarfsCrypt.Presentation.Spawner
{
    public class CharacterSpawner : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Transform _playerSpawnPoint;

        [Header("Camera")]
        [SerializeField] private CameraFollow _cameraFollow;

        [Header("Enemies")]
        [SerializeField] private List<EnemySpawnEntry> _enemies = new();

        public GameObject Player { get; private set; }
        public IReadOnlyList<GameObject> SpawnedEnemies => _spawnedEnemies;

        private readonly List<GameObject> _spawnedEnemies = new();

        private void Start() => SpawnAll();

        public void SpawnAll()
        {
            SpawnPlayer();
            SpawnEnemies();
        }

        public void SpawnPlayer()
        {
            if (_playerPrefab == null) return;

            Vector3 pos = _playerSpawnPoint != null ? _playerSpawnPoint.position : Vector3.zero;
            Player = Instantiate(_playerPrefab, pos, Quaternion.identity);

            _cameraFollow?.SetTarget(Player.transform);
        }

        public void SpawnEnemies()
        {
            foreach (var entry in _enemies)
            {
                if (entry.Prefab == null) continue;

                Vector3 pos = entry.SpawnPoint != null ? entry.SpawnPoint.position : Vector3.zero;
                _spawnedEnemies.Add(Instantiate(entry.Prefab, pos, Quaternion.identity));
            }
        }
    }
}
