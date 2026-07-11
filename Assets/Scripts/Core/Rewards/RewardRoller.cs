using DwarfsCrypt.Domain.Rewards;
using UnityEngine;

namespace Core.Rewards
{
    /// <summary>
    /// Turns an authored <see cref="RewardTable"/> into a concrete <see cref="RewardResult"/>
    /// by rolling each drop's chance and quantity range.
    /// </summary>
    public static class RewardRoller
    {
        /// <summary>
        /// Roll a reward table. <paramref name="rewardScale"/> scales the whole payout — exp, gold,
        /// and each drop's chance (e.g. 0.5 for the reduced AFK payout, so items also drop about half
        /// as often). Scaling chance rather than quantity avoids single-item drops flooring to zero.
        /// </summary>
        public static RewardResult Roll(RewardTable table, float rewardScale = 1f)
        {
            var result = new RewardResult();
            if (table == null) return result;

            result.Exp = Mathf.Max(0, Mathf.RoundToInt(table.Exp * rewardScale));
            result.Gold = Mathf.Max(0, Mathf.RoundToInt(table.Gold * rewardScale));

            if (table.Drops == null) return result;

            foreach (var drop in table.Drops)
            {
                if (drop == null || string.IsNullOrEmpty(drop.ItemId)) continue;
                if (Random.value > drop.Chance * rewardScale) continue;

                int min = Mathf.Max(1, drop.MinQuantity);
                int max = Mathf.Max(min, drop.MaxQuantity);
                int qty = Random.Range(min, max + 1);

                result.AddItem(drop.ItemId, qty);
            }

            return result;
        }
    }
}
