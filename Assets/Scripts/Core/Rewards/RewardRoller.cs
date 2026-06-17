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
        public static RewardResult Roll(RewardTable table)
        {
            var result = new RewardResult();
            if (table == null) return result;

            result.Exp = table.Exp;
            result.Gold = table.Gold;

            if (table.Drops == null) return result;

            foreach (var drop in table.Drops)
            {
                if (drop == null || string.IsNullOrEmpty(drop.ItemId)) continue;
                if (Random.value > drop.Chance) continue;

                int min = Mathf.Max(1, drop.MinQuantity);
                int max = Mathf.Max(min, drop.MaxQuantity);
                int qty = Random.Range(min, max + 1);

                result.AddItem(drop.ItemId, qty);
            }

            return result;
        }
    }
}
