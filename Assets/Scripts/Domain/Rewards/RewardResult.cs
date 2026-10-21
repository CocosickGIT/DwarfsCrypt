using System.Collections.Generic;
using DwarfsCrypt.Domain.Items;

namespace DwarfsCrypt.Domain.Rewards
{
    /// <summary>
    /// A concrete, rolled outcome of a <see cref="RewardTable"/> — fixed exp/gold and the
    /// items that actually dropped after chance rolls. Also used to total a whole run.
    /// </summary>
    public class RewardResult
    {
        public int Exp;
        public int Gold;
        public List<ItemStack> Items = new List<ItemStack>();

        /// <summary>Blueprint ids this payout newly taught the player (already-known ones are dropped).</summary>
        public List<string> LearnedBlueprints = new List<string>();

        /// <summary>Fold another result into this one (used to accumulate a run total).</summary>
        public void Add(RewardResult other)
        {
            if (other == null) return;

            Exp += other.Exp;
            Gold += other.Gold;

            foreach (var stack in other.Items)
                AddItem(stack.ItemId, stack.Quantity);

            foreach (var blueprintId in other.LearnedBlueprints)
                if (!LearnedBlueprints.Contains(blueprintId))
                    LearnedBlueprints.Add(blueprintId);
        }

        public void AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;

            var existing = Items.Find(s => s.ItemId == itemId);
            if (existing != null)
                existing.Quantity += quantity;
            else
                Items.Add(new ItemStack(itemId, quantity));
        }
    }
}
