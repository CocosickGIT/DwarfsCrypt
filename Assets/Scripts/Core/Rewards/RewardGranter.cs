using Core.Crafting;
using Core.Player;
using DwarfsCrypt.Domain.Rewards;

namespace Core.Rewards
{
    /// <summary>
    /// Entry point for granting a single kill's rewards: rolls the table, applies exp/gold/items
    /// to the active player profile immediately, and folds the outcome into the run total.
    /// </summary>
    public static class RewardGranter
    {
        /// <summary>Roll and apply a kill's rewards, folding the outcome into the run total.</summary>
        public static RewardResult GrantKill(RewardTable table) => Grant(table, foldIntoRun: true);

        /// <summary>
        /// Roll a reward table and apply exp/gold/items to the active profile. Set
        /// <paramref name="foldIntoRun"/> for kill rewards (which feed the end-of-run summary); leave
        /// it false for one-off grants like quest turn-ins that shouldn't inflate the run total.
        /// <paramref name="rewardScale"/> scales the whole payout — exp, gold, and drop chance (e.g.
        /// 0.5 for the reduced AFK payout, so items also drop about half as often).
        /// </summary>
        public static RewardResult Grant(RewardTable table, bool foldIntoRun, float rewardScale = 1f)
        {
            if (table == null) return null;

            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            var result = RewardRoller.Roll(table, rewardScale);

            LevelCurve.ApplyExp(profile, result.Exp);
            profile.Gold += result.Gold;
            foreach (var stack in result.Items)
                profile.AddItem(stack.ItemId, stack.Quantity);

            // Blueprints are taught outright rather than rolled — they are the reason a quest or
            // mission was worth running, so rewardScale doesn't apply and duplicates are ignored.
            if (table.Blueprints != null)
            {
                foreach (var blueprintId in table.Blueprints)
                    if (CraftingService.LearnBlueprint(profile, blueprintId))
                        result.LearnedBlueprints.Add(blueprintId);
            }

            if (foldIntoRun)
                RunRewards.Add(result);
            return result;
        }
    }
}
