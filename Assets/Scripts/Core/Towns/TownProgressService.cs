using System.Collections.Generic;
using Core.Player;
using DwarfsCrypt.Domain.Player;
using DwarfsCrypt.Domain.Towns;

namespace Core.Towns
{
    /// <summary>
    /// World-map progression: which zones the player has cleared and, from that, which towns are
    /// open to travel to. Cleared zones live on the persistent <see cref="PlayerProfile"/>; unlock
    /// state is *derived* from them on every query rather than stored, so re-authoring towns.json
    /// (adding a zone, re-pointing an UnlockedBy) takes effect immediately instead of leaving a
    /// stale unlocked-list on old saves.
    ///
    /// Static to match the project's other services (RewardGranter, StatisticsService, QuestService).
    /// </summary>
    public static class TownProgressService
    {
        /// <summary>The key a cleared zone is recorded under. Scoped by town so two towns may reuse
        /// the same zone id without sharing progress.</summary>
        public static string ZoneKey(string townId, string zoneId) => townId + "/" + zoneId;

        // --- Profile-explicit API (testable, no global state) ---

        public static bool IsZoneCleared(PlayerProfile profile, string townId, string zoneId)
        {
            if (profile?.ClearedZones == null || string.IsNullOrEmpty(townId) || string.IsNullOrEmpty(zoneId))
                return false;

            return profile.ClearedZones.Contains(ZoneKey(townId, zoneId));
        }

        /// <summary>
        /// Record a zone as cleared. Returns true only the first time — callers use that to decide
        /// whether anything actually changed (e.g. whether to save or to announce a new unlock).
        /// </summary>
        public static bool MarkZoneCleared(PlayerProfile profile, string townId, string zoneId)
        {
            if (profile == null || string.IsNullOrEmpty(townId) || string.IsNullOrEmpty(zoneId))
                return false;

            profile.ClearedZones ??= new List<string>();

            string key = ZoneKey(townId, zoneId);
            if (profile.ClearedZones.Contains(key)) return false;

            profile.ClearedZones.Add(key);
            return true;
        }

        /// <summary>How many of a town's zones the player has cleared (0 when the town is unknown).</summary>
        public static int ClearedZoneCount(PlayerProfile profile, string townId)
        {
            if (!TownCatalog.TryGet(townId, out var town) || town.Zones == null) return 0;

            int count = 0;
            foreach (var zone in town.Zones)
                if (zone != null && IsZoneCleared(profile, townId, zone.Id))
                    count++;

            return count;
        }

        /// <summary>Number of zones a town offers (0 when the town is unknown or has none authored).</summary>
        public static int ZoneCount(string townId) =>
            TownCatalog.TryGet(townId, out var town) && town.Zones != null ? town.Zones.Count : 0;

        /// <summary>
        /// True when every zone of a town has been cleared. A town with no zones authored is never
        /// "cleared" — otherwise an unfinished town config would silently open everything behind it.
        /// </summary>
        public static bool IsTownCleared(PlayerProfile profile, string townId)
        {
            int total = ZoneCount(townId);
            return total > 0 && ClearedZoneCount(profile, townId) >= total;
        }

        /// <summary>
        /// True when the player may travel to a town: either it is open from the start
        /// (no <see cref="TownConfig.UnlockedBy"/>) or the town gating it has been fully cleared.
        /// Unknown town ids are locked.
        /// </summary>
        public static bool IsTownUnlocked(PlayerProfile profile, string townId)
        {
            if (!TownCatalog.TryGet(townId, out var town)) return false;
            if (string.IsNullOrEmpty(town.UnlockedBy)) return true;

            return IsTownCleared(profile, town.UnlockedBy);
        }

        /// <summary>
        /// Human-readable reason a town is still locked, for the map window's locked node
        /// (e.g. "Clear all 2 zones in Ironhold"). Empty when the town is already unlocked.
        /// </summary>
        public static string LockReason(PlayerProfile profile, string townId)
        {
            if (IsTownUnlocked(profile, townId)) return string.Empty;
            if (!TownCatalog.TryGet(townId, out var town)) return "Unknown town";

            if (!TownCatalog.TryGet(town.UnlockedBy, out var gate))
                return "Locked";

            int total = ZoneCount(gate.Id);
            int done = ClearedZoneCount(profile, gate.Id);
            return total > 0
                ? $"Clear all zones in {gate.Name} ({done}/{total})"
                : $"Clear {gate.Name}";
        }

        // --- Convenience API operating on the active profile ---

        private static PlayerProfile Active
        {
            get
            {
                PlayerProfileService.EnsureLoaded();
                return PlayerProfileService.Current;
            }
        }

        public static bool IsZoneCleared(string townId, string zoneId) =>
            IsZoneCleared(Active, townId, zoneId);

        public static int ClearedZoneCount(string townId) => ClearedZoneCount(Active, townId);

        public static bool IsTownCleared(string townId) => IsTownCleared(Active, townId);

        public static bool IsTownUnlocked(string townId) => IsTownUnlocked(Active, townId);

        public static string LockReason(string townId) => LockReason(Active, townId);

        /// <summary>
        /// Record a zone clear on the active profile and flush it to disk. Unlike kill rewards
        /// (which ride along to the next save), a clear is a progression milestone — saving here
        /// means quitting from the town screen can't lose a town unlock. Returns true when this was
        /// the zone's first clear.
        /// </summary>
        public static bool MarkZoneCleared(string townId, string zoneId)
        {
            if (!MarkZoneCleared(Active, townId, zoneId)) return false;

            PlayerProfileService.Save();
            return true;
        }

        /// <summary>
        /// Ids of the towns this clear just opened — i.e. towns gated by <paramref name="townId"/>
        /// that are unlocked now. Empty unless the clear completed the town. Used by the map/HUD to
        /// announce "New town unlocked".
        /// </summary>
        public static List<string> TownsUnlockedBy(string townId)
        {
            var unlocked = new List<string>();
            if (!IsTownCleared(townId)) return unlocked;

            foreach (var town in TownCatalog.All)
                if (town != null && town.UnlockedBy == townId)
                    unlocked.Add(town.Id);

            return unlocked;
        }
    }
}
