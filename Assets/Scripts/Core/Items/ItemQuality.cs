using DwarfsCrypt.Domain.Items;
using UnityEngine;

namespace Core.Items
{
    /// <summary>
    /// Quality variants of an authored item, encoded into the item id itself.
    ///
    /// A crafted item that rolls above Common is stored as <c>"{baseId}@{rarity}"</c> (e.g.
    /// <c>"iron_sword@2"</c> for a Rare iron sword). <see cref="ItemCatalog"/> resolves such an id by
    /// cloning the authored item and scaling its bonuses by <see cref="StatMultiplier"/>, so the
    /// inventory, equipment bonuses, reward screen and save file all keep working with plain item ids
    /// and need no per-instance item storage. Common quality uses the bare base id, which means every
    /// pre-existing item and save stays byte-for-byte valid.
    /// </summary>
    public static class ItemQuality
    {
        /// <summary>Separates the base id from its quality tier. Never used in authored item ids.</summary>
        public const char Separator = '@';

        /// <summary>The stored id for an item at a given quality. Common returns the base id unchanged.</summary>
        public static string MakeId(string baseItemId, ItemRarity quality)
        {
            if (string.IsNullOrEmpty(baseItemId) || quality == ItemRarity.Common) return baseItemId;
            return baseItemId + Separator + (int)quality;
        }

        /// <summary>
        /// Split a stored id into its base id and quality. Returns false for a plain authored id,
        /// in which case <paramref name="baseItemId"/> is the id itself and quality is Common.
        /// </summary>
        public static bool TryParse(string id, out string baseItemId, out ItemRarity quality)
        {
            baseItemId = id;
            quality = ItemRarity.Common;
            if (string.IsNullOrEmpty(id)) return false;

            int split = id.LastIndexOf(Separator);
            if (split <= 0 || split == id.Length - 1) return false;

            if (!int.TryParse(id.Substring(split + 1), out int tier)) return false;
            if (tier < (int)ItemRarity.Common || tier > (int)ItemRarity.Legendary) return false;

            baseItemId = id.Substring(0, split);
            quality = (ItemRarity)tier;
            return true;
        }

        /// <summary>Factor applied to every stat bonus of a crafted item at this quality.</summary>
        public static float StatMultiplier(ItemRarity quality) => quality switch
        {
            ItemRarity.Uncommon  => 1.25f,
            ItemRarity.Rare      => 1.60f,
            ItemRarity.Epic      => 2.10f,
            ItemRarity.Legendary => 2.75f,
            _                    => 1f
        };

        /// <summary>Smith's word for a quality tier, prefixed to the item name ("Masterwork Iron Sword").</summary>
        public static string NamePrefix(ItemRarity quality) => quality switch
        {
            ItemRarity.Uncommon  => "Fine",
            ItemRarity.Rare      => "Masterwork",
            ItemRarity.Epic      => "Exquisite",
            ItemRarity.Legendary => "Mythic",
            _                    => string.Empty
        };

        /// <summary>Scale a bonus, rounding up so a +1 stat still gains something at higher tiers.</summary>
        public static int ScaleBonus(int bonus, float multiplier) =>
            bonus == 0 ? 0 : Mathf.CeilToInt(bonus * multiplier);

        /// <summary>Shared rarity palette, matching the inventory slot and reward cell backgrounds.</summary>
        public static Color RarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => new Color(0.25f, 0.25f, 0.25f),
            ItemRarity.Uncommon  => new Color(0.08f, 0.40f, 0.08f),
            ItemRarity.Rare      => new Color(0.08f, 0.20f, 0.65f),
            ItemRarity.Epic      => new Color(0.40f, 0.08f, 0.60f),
            ItemRarity.Legendary => new Color(0.80f, 0.35f, 0.00f),
            _                    => new Color(0.18f, 0.18f, 0.18f)
        };

        /// <summary>Brighter variant of <see cref="RarityColor"/> for text.</summary>
        public static Color RarityTextColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => new Color(0.80f, 0.80f, 0.80f),
            ItemRarity.Uncommon  => new Color(0.40f, 0.90f, 0.40f),
            ItemRarity.Rare      => new Color(0.40f, 0.60f, 1.00f),
            ItemRarity.Epic      => new Color(0.80f, 0.45f, 1.00f),
            ItemRarity.Legendary => new Color(1.00f, 0.65f, 0.20f),
            _                    => Color.white
        };
    }
}
