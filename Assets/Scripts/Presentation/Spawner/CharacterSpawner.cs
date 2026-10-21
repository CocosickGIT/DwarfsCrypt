using System;
using System.Collections;
using System.Collections.Generic;
using Core.Player;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Presentation.Combat;
using DwarfsCrypt.Presentation.Enemy;
using DwarfsCrypt.Presentation.Player;
using DwarfsCrypt.Presentation.Towns;
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

        [Tooltip("Resolves the monster ids in the entered zone's roster (towns.json) to prefabs. " +
                 "Found in the scene when left empty. Without one the camps spawn whatever they were " +
                 "authored with, so a scene with no registry still works.")]
        [SerializeField] private MonsterRegistry _monsterRegistry;

        [Tooltip("Enemies instantiated per frame at scene start. Spawning every enemy in one frame " +
                 "causes a launch hitch (Instantiate + animation-clip loading); spreading them over " +
                 "frames smooths it. Higher = faster full spawn but a bigger spike. Set very high to " +
                 "spawn all at once.")]
        [SerializeField, Min(1)] private int _enemiesPerFrame = 3;

        public GameObject Player { get; private set; }
        public IReadOnlyList<GameObject> SpawnedEnemies => _spawnedEnemies;

        /// <summary>True once every camp zone has finished spawning. Enemies are instantiated over
        /// several frames, so anything watching for "the scene is empty" (e.g. ZoneClearTracker)
        /// must wait for this before treating an empty enemy list as a clear.</summary>
        public bool SpawnComplete { get; private set; }

        /// <summary>Total enemies instantiated this scene, including ones already killed and pooled.
        /// Zero after <see cref="SpawnComplete"/> means the scene has no enemies at all.</summary>
        public int TotalEnemiesSpawned { get; private set; }

        private CharacterFactory _factory;
        private readonly List<GameObject> _spawnedEnemies = new();
        // Remembers which prefab each live enemy came from, so it can be pooled by key on death.
        private readonly Dictionary<GameObject, GameObject> _enemyPrefabs = new();

        private void Awake()
        {
            _factory = GetComponent<CharacterFactory>();

            if (_monsterRegistry == null)
                _monsterRegistry = FindAnyObjectByType<MonsterRegistry>();
        }

        private void Start()
        {
            SpawnAll();
        }

        private void SpawnAll()
        {
            SpawnPlayer();
            StartCoroutine(SpawnEnemiesRoutine());
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

        /// <summary>
        /// Put the already-spawned player back on the spawn point. Used when a town is swapped in
        /// place (see <see cref="Towns.TownBuilder.Rebuild"/>): with no scene reload nothing else
        /// re-places the player, so they would arrive standing wherever the previous town's layout
        /// left them.
        /// </summary>
        public void MovePlayerToSpawn()
        {
            if (Player == null || _playerConfig == null) return;
            Player.transform.position = FirstPosition(_playerConfig);
        }

        /// <summary>Spawns all camp-zone enemies, spread across frames to avoid a scene-load spike.</summary>
        public void SpawnEnemies() => StartCoroutine(SpawnEnemiesRoutine());

        private IEnumerator SpawnEnemiesRoutine()
        {
            float luck = Player != null
                ? Player.GetComponent<CharacterComponent>()?.Character?.Attributes.GetFinal(AttributeType.Luck) ?? 0f
                : 0f;

            SpawnComplete = false;

            // The entered zone's roster, resolved once for the whole scene. Null when the zone
            // authors none (or nothing in it resolved), which leaves every camp on its own enemies.
            var roster = _monsterRegistry != null ? _monsterRegistry.BuildEntriesForCurrentRun() : null;

            int sinceYield = 0;
            foreach (var zone in _campZones)
            {
                if (zone == null) continue;

                foreach (var (prefab, configPath, position) in zone.GetSpawnData(luck, roster))
                {
                    SpawnEnemy(prefab, configPath, position, zone.transform);

                    // Amortize the Instantiate + SPUM animation-clip loading across frames.
                    if (++sinceYield >= _enemiesPerFrame)
                    {
                        sinceYield = 0;
                        yield return null;
                    }
                }
            }

            SpawnComplete = true;
        }

        private void SpawnEnemy(GameObject prefab, string configPath, Vector3 position, Transform parent)
        {
            var enemy = _factory.Spawn(prefab, configPath, position, parent);
            _spawnedEnemies.Add(enemy);
            _enemyPrefabs[enemy] = prefab;
            TotalEnemiesSpawned++;

            // Apply the active town's run modifiers to this freshly built enemy (no-op outside a run).
            EnemyRunModifiers.Apply(
                enemy.GetComponentInChildren<CharacterComponent>(),
                enemy.GetComponentInChildren<EnemyController>());

            // Pool the unit only once its death dissolve has finished playing.
            var dissolve = enemy.GetComponentInChildren<DissolveOnDeath>();
            if (dissolve != null)
                dissolve.Dissolved += HandleEnemyDissolved;
        }

        // Death animation + dissolve are done — return the corpse to the pool.
        private void HandleEnemyDissolved(DissolveOnDeath dissolve)
        {
            dissolve.Dissolved -= HandleEnemyDissolved;

            GameObject enemy = dissolve.Unit;
            if (enemy != null && _enemyPrefabs.TryGetValue(enemy, out var prefab))
            {
                _enemyPrefabs.Remove(enemy);
                ReleaseEnemy(enemy, prefab);
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
