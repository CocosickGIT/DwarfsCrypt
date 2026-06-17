using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Crafting
{
    /// <summary>
    /// A crafting recipe: consume the listed <see cref="Ingredients"/> to produce
    /// <see cref="ResultQuantity"/> of <see cref="ResultItemId"/>.
    /// </summary>
    [Serializable]
    public class Blueprint
    {
        public string Id;
        public string Name;
        public string ResultItemId;
        public int ResultQuantity = 1;
        public List<CraftIngredient> Ingredients = new List<CraftIngredient>();
    }
}
