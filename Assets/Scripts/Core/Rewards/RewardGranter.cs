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
        public static RewardResult GrantKill(RewardTable table)
        {
            if (table == null) return null;

            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            var result = RewardRoller.Roll(table);

            LevelCurve.ApplyExp(profile, result.Exp);
            profile.Gold += result.Gold;
            foreach (var stack in result.Items)
                profile.AddItem(stack.ItemId, stack.Quantity);

            RunRewards.Add(result);
            return result;
        }
    }
}
