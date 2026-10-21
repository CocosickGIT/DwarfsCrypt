using System.Collections.Generic;
using DwarfsCrypt.Domain.Items;
using UnityEngine;

namespace Core.Items
{
    /// <summary>
    /// In-memory lookup of every item by id, loaded once from Resources/Items/items.json.
    /// Resolves drop ids and saved inventory ids back to full <see cref="ItemData"/>.
    ///
    /// Also resolves the quality variant ids crafting produces (see <see cref="ItemQuality"/>):
    /// <c>"iron_sword@2"</c> is synthesised on first lookup from the authored <c>iron_sword</c> with
    /// its bonuses scaled, then cached alongside the authored items.
    /// </summary>
    public static class ItemCatalog
    {
        private const string ItemsPath = "Items/items";

        private static Dictionary<string, ItemData> _byId;

        public static void EnsureLoaded()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, ItemData>();
            var collection = ItemConfigLoader.LoadFromResources(ItemsPath);
            if (collection?.Items == null) return;

            foreach (var item in collection.Items)
            {
                if (item != null && !string.IsNullOrEmpty(item.Id))
                    _byId[item.Id] = item;
            }
        }

        public static bool TryGet(string id, out ItemData item)
        {
            EnsureLoaded();
            if (_byId.TryGetValue(id, out item)) return true;

            // Not authored — it may be a crafted quality variant of an authored item.
            if (!ItemQuality.TryParse(id, out string baseId, out var quality)) return false;
            if (!_byId.TryGetValue(baseId, out var baseItem)) return false;

            item = MakeQualityVariant(baseItem, quality, id);
            _byId[id] = item;
            return true;
        }

        /// <summary>
        /// Clone an authored item at a higher quality: every stat bonus scaled, the rarity raised and
        /// the smith's quality word prefixed to the name. Everything else (type, icon, stacking) is
        /// inherited, so the variant behaves exactly like the item it was forged from.
        /// </summary>
        private static ItemData MakeQualityVariant(ItemData baseItem, ItemRarity quality, string variantId)
        {
            float mult = ItemQuality.StatMultiplier(quality);

            return new ItemData
            {
                Id = variantId,
                Name = $"{ItemQuality.NamePrefix(quality)} {baseItem.Name}".Trim(),
                Description = baseItem.Description,
                Type = baseItem.Type,
                // A crafted piece is never worse than the item it is based on.
                Rarity = (ItemRarity)Mathf.Max((int)baseItem.Rarity, (int)quality),
                IconPath = baseItem.IconPath,
                IsStackable = baseItem.IsStackable,
                MaxStack = baseItem.MaxStack,
                RestoreHp = ItemQuality.ScaleBonus(baseItem.RestoreHp, mult),
                RestoreMana = ItemQuality.ScaleBonus(baseItem.RestoreMana, mult),
                BonusStr = ItemQuality.ScaleBonus(baseItem.BonusStr, mult),
                BonusDex = ItemQuality.ScaleBonus(baseItem.BonusDex, mult),
                BonusCon = ItemQuality.ScaleBonus(baseItem.BonusCon, mult),
                BonusWit = ItemQuality.ScaleBonus(baseItem.BonusWit, mult),
                BonusMen = ItemQuality.ScaleBonus(baseItem.BonusMen, mult),
                BonusLuc = ItemQuality.ScaleBonus(baseItem.BonusLuc, mult),
                BonusMaxHp = ItemQuality.ScaleBonus(baseItem.BonusMaxHp, mult),
                BonusMaxStamina = ItemQuality.ScaleBonus(baseItem.BonusMaxStamina, mult),
                BonusMaxMana = ItemQuality.ScaleBonus(baseItem.BonusMaxMana, mult)
            };
        }

        public static ItemData Get(string id)
        {
            return TryGet(id, out var item) ? item : null;
        }
    }
}
