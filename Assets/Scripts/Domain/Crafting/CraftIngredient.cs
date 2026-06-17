using System;

namespace DwarfsCrypt.Domain.Crafting
{
    /// <summary>
    /// One required material for a <see cref="Blueprint"/>: an item id and how many of it
    /// the recipe consumes. Authored in StreamingAssets/Crafting/blueprints.json.
    /// </summary>
    [Serializable]
    public class CraftIngredient
    {
        public string ItemId;
        public int Quantity = 1;
    }
}
