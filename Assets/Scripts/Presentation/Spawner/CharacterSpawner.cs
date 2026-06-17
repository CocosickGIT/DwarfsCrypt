using System;
using System.Collections.Generic;
using Core.Player;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Presentation.Combat;
using DwarfsCrypt.Presentation.Player;
using Presentation.Features;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Spawner
{
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(CharacterFactory))]
    public class CharacterSpawner : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private UnitSpawnConfig _playerConfig;
        [SerializeField] private GameHUD _gameHUD;
        [SerializeField] private CameraFollow _cameraFollow;

        [Header("Camp Zones")]
        [SerializeField] private List<CampSpawnZone> _campZones = new();

        public GameObject Player { get; private set; }
        public IReadOnlyList<GameObject> SpawnedEnemies => _spawnedEnemies;

        private CharacterFactory _factory;
        private readonly List<GameObject> _spawnedEnemies = new();

        private void Awake()
        {
            _factory = GetComponent<CharacterFactory>();
        }

        private void Start()
        {
            SpawnAll();
        }

        private void SpawnAll()
        {
            SpawnPlayer();
            SpawnEnemies();
        }

        private void SpawnPlayer()
        {
            if (_playerConfig?.Prefab == null) return;

            Player = _factory.Spawn(_playerConfig.Prefab, _playerConfig.ConfigPath, FirstPosition(_playerConfig), transform);

            // Drive the player from the persisted profile (level/stats/equipment) rather than the
            // static spawn-config JSON, so progression carries across runs and scenes.
            PlayerProfileService.EnsureLoaded();
            Player.GetComponentInChildren<CharacterComponent>()?.Initialize(PlayerProfileService.Current.ToCharacterConfig());

            if (_cameraFollow != null)
                _cameraFollow.SetTarget(Player.transform);

            if (_gameHUD != null)
                Player.GetComponent<PlayerController>()?.SetHUD(_gameHUD);
        }

        public void SpawnEnemies()
        {
            float luck = Player != null
                ? Player.GetComponent<CharacterComponent>()?.Character?.Attributes.GetFinal(AttributeType.Luck) ?? 0f
                : 0f;

            foreach (var zone in _campZones)
            {
                if (zone == null) continue;

                foreach (var (prefab, configPath, position) in zone.GetSpawnData(luck))
                    _spawnedEnemies.Add(_factory.Spawn(prefab, configPath, position, zone.transform));
            }
        }

        /// <summary>Release an enemy back to the pool (e.g. after a death animation finishes).</summary>
        public void ReleaseEnemy(GameObject enemy, GameObject prefab)
        {
            _spawnedEnemies.Remove(enemy);
            _factory.Release(enemy, prefab);
        }

        private static Vector3 FirstPosition(UnitSpawnConfig config)
        {
            return config.SpawnPoints is { Count: > 0 } && config.SpawnPoints[0] != null
                ? config.SpawnPoints[0].position
                : Vector3.zero;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_playerConfig.SpawnPoints[0].position,0.5f );
        }
    }
}
