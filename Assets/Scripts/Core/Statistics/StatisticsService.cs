using Core.Player;
using DwarfsCrypt.Domain.Player;
using DwarfsCrypt.Domain.Statistics;

namespace Core.Statistics
{
    /// <summary>
    /// Lifetime statistics tracker. Counters live on the persistent <see cref="PlayerProfile"/>
    /// (so they survive scene loads and saves) but, like the kill-reward loop, are only flushed to
    /// disk when the profile is next saved (level exit, quest accept/claim, craft) — not on every
    /// kill. Quests read these counters to measure progress against an accept-time baseline.
    ///
    /// Static to match the project's other services (RewardGranter, CraftingService).
    /// </summary>
    public static class StatisticsService
    {
        // --- Profile-explicit API (testable, no global state) ---

        public static int Get(PlayerProfile profile, string key)
        {
            if (profile?.Statistics == null || string.IsNullOrEmpty(key)) return 0;

            var entry = profile.Statistics.Find(s => s != null && s.Key == key);
            return entry?.Value ?? 0;
        }

        public static void Add(PlayerProfile profile, string key, int amount)
        {
            if (profile == null || string.IsNullOrEmpty(key) || amount == 0) return;

            profile.Statistics ??= new System.Collections.Generic.List<StatEntry>();

            var entry = profile.Statistics.Find(s => s != null && s.Key == key);
            if (entry != null)
                entry.Value += amount;
            else
                profile.Statistics.Add(new StatEntry(key, amount));
        }

        // --- Convenience API operating on the active profile ---

        public static int Get(string key)
        {
            PlayerProfileService.EnsureLoaded();
            return Get(PlayerProfileService.Current, key);
        }

        public static void Add(string key, int amount)
        {
            PlayerProfileService.EnsureLoaded();
            Add(PlayerProfileService.Current, key, amount);
        }

        /// <summary>Record one kill, incrementing both the global total and the per-enemy counter.</summary>
        public static void RecordKill(string enemyName)
        {
            Add(StatKeys.Kill(null), 1);
            if (!string.IsNullOrEmpty(enemyName))
                Add(StatKeys.Kill(enemyName), 1);
        }

        /// <summary>Record one craft, incrementing both the global total and the per-blueprint counter.</summary>
        public static void RecordCraft(PlayerProfile profile, string blueprintId)
        {
            Add(profile, StatKeys.Craft(null), 1);
            if (!string.IsNullOrEmpty(blueprintId))
                Add(profile, StatKeys.Craft(blueprintId), 1);
        }
    }
}
