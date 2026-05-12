using System;

namespace DwarfsCrypt.Domain.Items
{
    [Serializable]
    public class ItemEntry : ItemData
    {
        public int Quantity = 1;
    }
}
