using System.Collections.Generic;
using DwarfsCrypt.Domain.Towns;

namespace Core.Towns
{
    /// <summary>
    /// Carries the active run's town context across the scene load, the same way
    /// <see cref="Core.Rewards.RunRewards"/> carries the reward total. The Gate calls
    /// <see cref="Begin"/> right before loading a game scene, snapshotting the town's modifiers;
    /// the game scene reads <see cref="Modifiers"/> when spawning enemies. Cleared on return to town.
    /// </summary>
    public static class RunContext
    {
        private static readonly List<TownModifier> _modifiers = new();
        private static readonly List<MonsterEntry> _monsterTable = new();

        /// <summary>Id of the town whose Gate started this run (null if none).</summary>
        public static string TownId { get; private set; }

        /// <summary>Id of the town zone this run entered (null when the caller didn't name one).
        /// ZoneClearTracker needs it to record which zone was cleared, so a run started without it
        /// can never count toward a town unlock.</summary>
        public static string ZoneId { get; private set; }

        /// <summary>Scene this run entered.</summary>
        public static string SceneName { get; private set; }

        /// <summary>Hub scene of the town this run started from — where the player returns on
        /// exit/death (null when no run is active).</summary>
        public static string TownScene { get; private set; }

        /// <summary>The active town's modifiers for this run (empty when no run is active).</summary>
        public static IReadOnlyList<TownModifier> Modifiers => _modifiers;

        /// <summary>
        /// The entered zone's mob roster (empty when the zone authors none, or when no run is active).
        /// Empty means the game scene spawns whatever its camp zones were authored with.
        /// </summary>
        public static IReadOnlyList<MonsterEntry> MonsterTable => _monsterTable;

        /// <summary>
        /// Snapshot the given town's modifiers for the run entering <paramref name="sceneName"/>.
        /// Resolves the town from <see cref="TownCatalog"/>; unknown ids simply carry no modifiers.
        /// <paramref name="zoneId"/> is the town zone being entered — omit it only for runs that
        /// aren't a zone (they then can't be recorded as cleared).
        /// </summary>
        public static void Begin(string townId, string sceneName, string zoneId = null)
        {
            TownId = townId;
            ZoneId = zoneId;
            SceneName = sceneName;
            TownScene = TownCatalog.SceneFor(townId);

            _modifiers.Clear();
            _monsterTable.Clear();

            if (!TownCatalog.TryGet(townId, out var town)) return;

            if (town.Modifiers != null)
                _modifiers.AddRange(town.Modifiers);

            var zone = FindZone(town, zoneId);
            if (zone?.MonsterTable != null)
                _monsterTable.AddRange(zone.MonsterTable);
        }

        private static TownZone FindZone(TownConfig town, string zoneId)
        {
            if (town.Zones == null || string.IsNullOrEmpty(zoneId)) return null;

            foreach (var zone in town.Zones)
                if (zone != null && zone.Id == zoneId)
                    return zone;

            return null;
        }

        /// <summary>
        /// The town hub scene to return to: this run's town when one is active, otherwise the
        /// default starting town. Call this <em>before</em> <see cref="Clear"/>.
        /// </summary>
        public static string ResolveTownScene() =>
            !string.IsNullOrEmpty(TownScene) ? TownScene : TownCatalog.DefaultTownScene;

        /// <summary>Clear the run context (e.g. on return to town).</summary>
        public static void Clear()
        {
            TownId = null;
            ZoneId = null;
            SceneName = null;
            TownScene = null;
            _modifiers.Clear();
            _monsterTable.Clear();
        }
    }
}
