using System;
using System.Collections.Generic;
using Core.Towns;
using DwarfsCrypt.Domain.Towns;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Spawner
{
    /// <summary>One monster id and the prefab that realises it.</summary>
    [Serializable]
    public class MonsterPrefabEntry
    {
        [Tooltip("Id used by a zone's MonsterTable in towns.json, e.g. 'monster_skeleton'.")]
        public string MonsterId;

        [Tooltip("Unit prefab spawned for this id.")]
        public GameObject Prefab;

        [Tooltip("Resources path (no extension) of the character json used when the zone's entry " +
                 "doesn't name one of its own, e.g. 'Characters/example_enemy'.")]
        public string ConfigPath;
    }

    /// <summary>
    /// Maps the monster ids a zone's roster is authored with onto real prefabs.
    ///
    /// This exists because the two halves live in different worlds: a zone's roster is data in
    /// towns.json, but the unit prefabs are under Assets/Prefabs (not Resources), so they can only be
    /// referenced by an inspector link. The registry is that link, and nothing else — keeping it out
    /// of <see cref="CharacterSpawner"/> means it can be a prefab dropped into every zone scene and
    /// edited in one place, while each scene keeps its own camp layout and spawn points.
    ///
    /// Ids the registry doesn't know are skipped with a warning rather than spawning something wrong.
    /// </summary>
    public class MonsterRegistry : MonoBehaviour
    {
        [Tooltip("Every monster id a zone roster may name, and the prefab it spawns.")]
        [SerializeField] private List<MonsterPrefabEntry> _monsters = new();

        /// <summary>
        /// Turn the active run's zone roster into camp entries. Returns null when the run authors no
        /// roster, which tells the camp zones to spawn what they were authored with instead.
        /// </summary>
        public List<CampEnemyEntry> BuildEntriesForCurrentRun() => BuildEntries(RunContext.MonsterTable);

        /// <summary>
        /// Resolve a zone roster against this registry. Null/empty in, null out — callers treat that
        /// as "no override".
        /// </summary>
        public List<CampEnemyEntry> BuildEntries(IReadOnlyList<MonsterEntry> roster)
        {
            if (roster == null || roster.Count == 0) return null;

            var entries = new List<CampEnemyEntry>(roster.Count);
            foreach (var mob in roster)
            {
                if (mob == null || mob.Weight <= 0f) continue;

                var known = Find(mob.MonsterId);
                if (known?.Prefab == null)
                {
                    Debug.LogWarning($"[MonsterRegistry] Zone roster names '{mob.MonsterId}', which " +
                                     "has no prefab registered here; it will not spawn.", this);
                    continue;
                }

                entries.Add(new CampEnemyEntry
                {
                    Prefab = known.Prefab,
                    ConfigPath = string.IsNullOrEmpty(mob.ConfigPath) ? known.ConfigPath : mob.ConfigPath,
                    Weight = mob.Weight
                });
            }

            // Every id was unknown — fall back to the scene's own enemies rather than an empty zone,
            // which ZoneClearTracker would otherwise read as "nothing to kill".
            return entries.Count > 0 ? entries : null;
        }

        private MonsterPrefabEntry Find(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId)) return null;

            foreach (var entry in _monsters)
                if (entry != null && entry.MonsterId == monsterId)
                    return entry;

            return null;
        }
    }
}
