using System;

namespace DwarfsCrypt.Domain.Items
{
    /// <summary>
    /// A lightweight, serializable reference to an item by id plus a quantity.
    /// Used both for rolled drops and for the persisted player inventory.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        public string ItemId;
        public int Quantity = 1;

        public ItemStack() { }

        public ItemStack(string itemId, int quantity = 1)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}
