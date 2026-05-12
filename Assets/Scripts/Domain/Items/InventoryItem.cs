namespace DwarfsCrypt.Domain.Items
{
    public class InventoryItem
    {
        public ItemData Data { get; }
        public int Quantity { get; private set; }

        public InventoryItem(ItemData data, int quantity = 1)
        {
            Data = data;
            Quantity = quantity;
        }

        public bool TryAddStack(int amount)
        {
            if (!Data.IsStackable) return false;
            if (Quantity + amount > Data.MaxStack) return false;
            Quantity += amount;
            return true;
        }

        public bool TryRemoveStack(int amount)
        {
            if (Quantity < amount) return false;
            Quantity -= amount;
            return true;
        }

        public void SetQuantity(int quantity) => Quantity = quantity;

        public bool IsEmpty => Quantity <= 0;
    }
}
