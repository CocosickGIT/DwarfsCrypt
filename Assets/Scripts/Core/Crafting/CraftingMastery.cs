using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Domain.Player;
using UnityEngine;

namespace Core.Crafting
{
    /// <summary>
    /// The player's smithing skill: its own experience track (separate from the character level) and
    /// the quality odds it buys at the workshop. Mirrors <c>LevelCurve</c> — all the tuning lives here.
    ///
    /// Quality is rolled as a cascade of upgrade steps: a craft starts at Common and tries to step up
    /// one tier at a time, each step less likely than the last. The chance of any step succeeding is
    /// driven by a saturating skill score, so early mastery levels feel generous while Legendary stays
    /// a genuine rarity no amount of grinding makes routine.
    /// </summary>
    public static class CraftingMastery
    {
        // --- Tuning ---------------------------------------------------------
        // Mastery exp needed to advance FROM a level; gentler than the character curve.
        private const int ExpBase = 50;

        // Luck contributes to the smithing score at this rate (mastery level counts 1:1).
        private const float LucWeight = 0.5f;

        // Half-saturation point: at score == SkillSoftCap the upgrade rolls are at half strength.
        private const float SkillSoftCap = 25f;

        // Per-step ceilings for the upgrade cascade: Common->Uncommon->Rare->Epic->Legendary.
        // Their product caps Legendary at ~2.6% for a perfect smith, and a realistic late-game score
        // of ~40 lands it near 0.4% — a chase result, not something a grind makes routine.
        private static readonly float[] StepCeilings = { 0.90f, 0.60f, 0.32f, 0.15f };
        // --------------------------------------------------------------------

        /// <summary>Mastery exp required to advance FROM <paramref name="level"/> to the next.</summary>
        public static int ExpForLevel(int level)
        {
            if (level < 1) level = 1;
            return ExpBase * level * level;
        }

        /// <summary>The profile's mastery level, defaulting old saves (which stored 0) to 1.</summary>
        public static int LevelOf(PlayerProfile profile) =>
            profile == null ? 1 : Mathf.Max(1, profile.CraftingMastery);

        /// <summary>
        /// Add mastery exp and apply any resulting level-ups, carrying leftover exp into the next
        /// level. Returns the number of levels gained.
        /// </summary>
        public static int ApplyExp(PlayerProfile profile, int exp)
        {
            if (profile == null || exp <= 0) return 0;

            profile.CraftingMastery = LevelOf(profile);
            profile.CraftingExp += exp;

            int levelsGained = 0;
            int needed = ExpForLevel(profile.CraftingMastery);
            while (profile.CraftingExp >= needed)
            {
                profile.CraftingExp -= needed;
                profile.CraftingMastery++;
                levelsGained++;
                needed = ExpForLevel(profile.CraftingMastery);
            }

            return levelsGained;
        }

        /// <summary>
        /// Probability of each quality tier for a craft by this profile, indexed by
        /// <see cref="ItemRarity"/>. Sums to 1. Used both for the roll and to show the odds in the UI.
        /// </summary>
        public static float[] QualityChances(PlayerProfile profile)
        {
            var chances = new float[StepCeilings.Length + 1];

            // Everything not upgraded away stays at the current tier; what is upgraded rolls on.
            float remaining = 1f;
            for (int step = 0; step < StepCeilings.Length; step++)
            {
                float upgrade = remaining * StepChance(profile, step);
                chances[step] = remaining - upgrade;
                remaining = upgrade;
            }
            chances[StepCeilings.Length] = remaining;

            return chances;
        }

        /// <summary>Roll one craft's quality against this profile's mastery.</summary>
        public static ItemRarity RollQuality(PlayerProfile profile)
        {
            for (int step = 0; step < StepCeilings.Length; step++)
                if (Random.value >= StepChance(profile, step))
                    return (ItemRarity)step;

            return (ItemRarity)StepCeilings.Length;
        }

        /// <summary>Chance of clearing one upgrade step of the cascade.</summary>
        private static float StepChance(PlayerProfile profile, int step)
        {
            float score = SkillScore(profile);
            float saturation = score / (score + SkillSoftCap);
            return StepCeilings[step] * saturation;
        }

        /// <summary>Mastery level plus a smaller contribution from the smith's luck.</summary>
        private static float SkillScore(PlayerProfile profile)
        {
            if (profile == null) return 0f;
            return LevelOf(profile) + Mathf.Max(0, profile.Stats.Luc) * LucWeight;
        }
    }
}
