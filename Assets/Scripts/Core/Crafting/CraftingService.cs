using System.Collections.Generic;
using DwarfsCrypt.Domain.Crafting;
using DwarfsCrypt.Domain.Player;

namespace Core.Crafting
{
    /// <summary>
    /// Stateless crafting logic operating on a <see cref="PlayerProfile"/> (the persistent source of
    /// truth for the inventory). Checks ingredient availability, consumes materials and grants the
    /// result. Callers are responsible for saving the profile and refreshing any open UI.
    /// </summary>
    public static class CraftingService
    {
        /// <summary>True when the profile holds enough of every ingredient to craft once.</summary>
        public static bool CanCraft(Blueprint blueprint, PlayerProfile profile)
        {
            if (blueprint == null || profile == null) return false;
            if (blueprint.Ingredients == null) return true;

            foreach (var ingredient in blueprint.Ingredients)
            {
                if (ingredient == null || string.IsNullOrEmpty(ingredient.ItemId)) continue;
                if (profile.CountItem(ingredient.ItemId) < ingredient.Quantity)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Consume the blueprint's ingredients and add its result to the profile inventory.
        /// Returns false and leaves the inventory untouched when the player can't afford it.
        /// </summary>
        public static bool TryCraft(Blueprint blueprint, PlayerProfile profile)
        {
            if (!CanCraft(blueprint, profile)) return false;

            if (blueprint.Ingredients != null)
            {
                foreach (var ingredient in blueprint.Ingredients)
                {
                    if (ingredient == null || string.IsNullOrEmpty(ingredient.ItemId)) continue;
                    profile.RemoveItem(ingredient.ItemId, ingredient.Quantity);
                }
            }

            int resultQty = blueprint.ResultQuantity > 0 ? blueprint.ResultQuantity : 1;
            profile.AddItem(blueprint.ResultItemId, resultQty);
            return true;
        }

        /// <summary>
        /// Ingredients the player is short on, as (itemId, have, need) tuples. Empty when craftable.
        /// Useful for UI feedback.
        /// </summary>
        public static List<(string ItemId, int Have, int Need)> GetMissingIngredients(
            Blueprint blueprint, PlayerProfile profile)
        {
            var missing = new List<(string, int, int)>();
            if (blueprint?.Ingredients == null || profile == null) return missing;

            foreach (var ingredient in blueprint.Ingredients)
            {
                if (ingredient == null || string.IsNullOrEmpty(ingredient.ItemId)) continue;
                int have = profile.CountItem(ingredient.ItemId);
                if (have < ingredient.Quantity)
                    missing.Add((ingredient.ItemId, have, ingredient.Quantity));
            }

            return missing;
        }
    }
}
