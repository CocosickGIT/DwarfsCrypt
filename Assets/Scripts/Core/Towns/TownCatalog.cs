using System.Collections.Generic;
using DwarfsCrypt.Domain.Towns;
using UnityEngine;

namespace Core.Towns
{
    /// <summary>
    /// In-memory lookup of every town by id, loaded once from Resources/Towns/towns.json.
    /// Mirrors <see cref="Core.Items.ItemCatalog"/>. Resolves a town id to its config (modifiers +
    /// the zones its Gate offers).
    /// </summary>
    public static class TownCatalog
    {
        private const string TownsPath = "Towns/towns";

        private static Dictionary<string, TownConfig> _byId;
        private static List<TownConfig> _all;

        public static void EnsureLoaded()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, TownConfig>();
            _all = new List<TownConfig>();

            TownsCollection collection;
            try
            {
                collection = TownConfigLoader.LoadFromResources(TownsPath);
            }
            catch (System.Exception e)
            {
                // No towns.json authored yet — run with an empty catalog (modifiers simply won't apply)
                // instead of taking down the scene.
                Debug.LogWarning($"[TownCatalog] Could not load towns config '{TownsPath}': {e.Message}");
                return;
            }

            if (collection?.Towns == null) return;

            foreach (var town in collection.Towns)
            {
                if (town == null || string.IsNullOrEmpty(town.Id)) continue;
                _byId[town.Id] = town;
                _all.Add(town);
            }
        }

        public static bool TryGet(string id, out TownConfig town)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) { town = null; return false; }
            return _byId.TryGetValue(id, out town);
        }

        public static TownConfig Get(string id) => TryGet(id, out var town) ? town : null;

        /// <summary>
        /// Resolves a town id to its hub scene name. Falls back to <see cref="DefaultTownScene"/>
        /// when the id is unknown or the town has no <c>Scene</c> authored.
        /// </summary>
        public static string SceneFor(string id)
        {
            if (TryGet(id, out var town) && !string.IsNullOrEmpty(town.Scene))
                return town.Scene;
            return DefaultTownScene;
        }

        /// <summary>
        /// The starting town's hub scene: the first town in towns.json that declares a <c>Scene</c>.
        /// Returns null (and logs) when no town declares one — a config error, since there is then no
        /// hub to return to. Callers pass the result to <see cref="Scenes.SceneLoader.Load"/>, which
        /// guards against null.
        /// </summary>
        public static string DefaultTownScene
        {
            get
            {
                EnsureLoaded();
                foreach (var town in _all)
                    if (town != null && !string.IsNullOrEmpty(town.Scene))
                        return town.Scene;

                Debug.LogError("[TownCatalog] No town in towns.json declares a 'Scene'; " +
                               "cannot resolve a town hub to load.");
                return null;
            }
        }

        /// <summary>Id of the starting town: the first town authored in towns.json (null when empty).</summary>
        public static string DefaultTownId
        {
            get
            {
                EnsureLoaded();
                return _all.Count > 0 ? _all[0].Id : null;
            }
        }

        public static IReadOnlyList<TownConfig> All
        {
            get { EnsureLoaded(); return _all; }
        }
    }
}
