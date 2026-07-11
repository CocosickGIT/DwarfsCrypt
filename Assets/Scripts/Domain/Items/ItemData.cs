using System;

namespace DwarfsCrypt.Domain.Items
{
    [Serializable]
    public class ItemData
    {
        public string Id;
        public string Name;
        public string Description;
        public ItemType Type;
        public ItemRarity Rarity;
        public string IconPath; // Resources-relative path for Resources.Load<Sprite>
        public bool IsStackable;
        public int MaxStack = 1;

        // Restored when this item is consumed (Type == Consumable). Zero = no effect of that kind.
        public int RestoreHp;
        public int RestoreMana;

        // Stat bonuses applied when equipped
        public int BonusStr;
        public int BonusDex;
        public int BonusCon;
        public int BonusWit;
        public int BonusMen;
        public int BonusLuc;
        public int BonusMaxHp;
        public int BonusMaxStamina;
        public int BonusMaxMana;

        public EquipmentSlotType SlotType => Type switch
        {
            ItemType.Helmet  => EquipmentSlotType.Head,
            ItemType.Chest   => EquipmentSlotType.Chest,
            ItemType.Legs    => EquipmentSlotType.Legs,
            ItemType.Feet    => EquipmentSlotType.Feet,
            ItemType.Weapon  => EquipmentSlotType.MainHand,
            ItemType.Shield  => EquipmentSlotType.OffHand,
            ItemType.Ring    => EquipmentSlotType.Ring1, // resolved to Ring1 or Ring2 at equip time
            ItemType.Amulet  => EquipmentSlotType.Amulet,
            _                => EquipmentSlotType.None
        };

        public bool IsEquippable => SlotType != EquipmentSlotType.None;
    }
}
