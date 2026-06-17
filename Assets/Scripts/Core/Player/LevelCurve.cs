using DwarfsCrypt.Domain.Player;

namespace Core.Player
{
    /// <summary>
    /// Exp-to-level progression. Kept deliberately simple and tunable in one place.
    /// </summary>
    public static class LevelCurve
    {
        // --- Per-level growth (tune here) -----------------------------------
        private const int HpPerLevel      = 10;
        private const int StaminaPerLevel = 5;
        private const int ManaPerLevel    = 2;
        private const int StrPerLevel     = 1;
        private const int ConPerLevel     = 1;
        // --------------------------------------------------------------------

        /// <summary>Total exp required to advance FROM <paramref name="level"/> to the next level.</summary>
        public static int ExpForLevel(int level)
        {
            if (level < 1) level = 1;
            return 100 * level * level;
        }

        /// <summary>
        /// Adds exp to the profile and applies any resulting level-ups (incrementing level and
        /// growing stats). Carries leftover exp into the next level. Returns levels gained.
        /// </summary>
        public static int ApplyExp(PlayerProfile profile, int exp)
        {
            if (profile == null || exp <= 0) return 0;

            profile.Exp += exp;

            int levelsGained = 0;
            int needed = ExpForLevel(profile.Level);
            while (profile.Exp >= needed)
            {
                profile.Exp -= needed;
                profile.Level++;
                ApplyLevelUpGrowth(profile);
                levelsGained++;
                needed = ExpForLevel(profile.Level);
            }

            return levelsGained;
        }

        private static void ApplyLevelUpGrowth(PlayerProfile profile)
        {
            profile.MaxHp      += HpPerLevel;
            profile.MaxStamina += StaminaPerLevel;
            profile.MaxMana    += ManaPerLevel;

            var stats = profile.Stats;
            stats.Str += StrPerLevel;
            stats.Con += ConPerLevel;
            profile.Stats = stats; // CharacterStats is a struct — reassign after mutating
        }
    }
}
