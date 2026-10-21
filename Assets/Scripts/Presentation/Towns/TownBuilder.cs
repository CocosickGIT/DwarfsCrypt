using Core.Towns;
using DwarfsCrypt.Presentation.Spawner;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Towns
{
    /// <summary>
    /// Builds the swappable visual/content layer of a town hub at runtime from its
    /// <see cref="TownConfig"/>. The town scene holds only the shared machinery (player, camera, HUD,
    /// windows); this instantiates the current town's Environment prefab — background art, colliders,
    /// NPC placements — so one scene serves every town. Which town is current comes from
    /// <see cref="TownContext"/>.
    ///
    /// Because the environment is the only town-specific thing in the hub, travelling between two
    /// towns that share a scene doesn't need a scene load at all — see <see cref="Rebuild"/>.
    /// </summary>
    public class TownBuilder : MonoBehaviour
    {
        [Tooltip("Parent the town's Environment prefab spawns under. Defaults to this transform.")]
        [SerializeField] private Transform _environmentRoot;

        [Tooltip("Town to build when entering this scene directly in the editor (TownContext unset). " +
                 "Leave empty to fall back to the first town in towns.json. Ignored during normal play, " +
                 "where the entered town comes from TownContext.")]
        [TownId] [SerializeField] private string _editorFallbackTownId;

        /// <summary>
        /// The builder in the loaded hub scene, for callers that want to swap towns in place
        /// (<see cref="Rebuild"/>). Null while no hub scene is loaded.
        /// </summary>
        public static TownBuilder Active { get; private set; }

        /// <summary>Id of the town whose environment is currently spawned (null before the first build).</summary>
        public string BuiltTownId { get; private set; }

        private GameObject _environment;

        private void Awake()
        {
            if (_environmentRoot == null) _environmentRoot = transform;
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void Start()
        {
            // Normal play: the entered town is in TownContext. Bare-scene playtest: fall back to the
            // serialized id, then the default town, so the hub still builds instead of coming up
            // empty. Set (not Enter) — a playtest fallback must not be written to the player's save.
            if (string.IsNullOrEmpty(TownContext.CurrentTownId))
                TownContext.Set(string.IsNullOrEmpty(_editorFallbackTownId)
                    ? TownCatalog.DefaultTownId
                    : _editorFallbackTownId);

            Build();
        }

        /// <summary>
        /// Swap the hub over to <see cref="TownContext"/>'s current town without reloading the scene:
        /// tears down the spawned environment, builds the new one, and moves the player to the hub's
        /// arrival point (nothing else would re-place them, so they would otherwise stand wherever
        /// the previous town's layout left them). No-op when the town hasn't actually changed.
        /// </summary>
        public void Rebuild()
        {
            if (TownContext.CurrentTownId == BuiltTownId) return;

            if (_environment != null)
            {
                // Destroy is deferred to the end of the frame; deactivating first keeps the old
                // town's art and colliders from overlapping the new one for that frame.
                _environment.SetActive(false);
                Destroy(_environment);
                _environment = null;
            }

            Build();

            var spawner = FindAnyObjectByType<CharacterSpawner>();
            if (spawner != null)
                spawner.MovePlayerToSpawn();
        }

        private void Build()
        {
            BuiltTownId = TownContext.CurrentTownId;

            var town = TownContext.Current;
            if (town == null)
            {
                Debug.LogWarning("[TownBuilder] No town could be resolved (towns.json empty?); " +
                                 "showing the bare scene.");
                return;
            }

            if (string.IsNullOrEmpty(town.EnvironmentPrefab))
            {
                Debug.LogWarning($"[TownBuilder] Town '{town.Id}' has no EnvironmentPrefab; " +
                                 "showing the bare scene.");
                return;
            }

            var prefab = Resources.Load<GameObject>(town.EnvironmentPrefab);
            if (prefab == null)
            {
                Debug.LogError($"[TownBuilder] Environment prefab not found in Resources: " +
                               $"'{town.EnvironmentPrefab}' (town '{town.Id}').");
                return;
            }

            _environment = Instantiate(prefab, _environmentRoot);
        }
    }
}
