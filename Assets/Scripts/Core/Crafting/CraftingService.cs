using System.Collections.Generic;
using Core.Items;
using Core.Statistics;
using DwarfsCrypt.Domain.Crafting;
using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Domain.Player;

namespace Core.Crafting
{
    /// <summary>
    /// Stateless crafting logic operating on a <see cref="PlayerProfile"/> (the persistent source of
    /// truth for the inventory). Checks recipe availability and ingredients, consumes materials, rolls
    /// the result's quality and grants it. Callers are responsible for saving the profile and
    /// refreshing any open UI.
    ///
    /// Two stations share this pipeline (see <see cref="CraftStation"/>): the <b>forge</b> works
    /// always-known recipes into basic materials with a fixed result, while the <b>workshop</b> works
    /// learned blueprints into equipment whose quality is rolled against <see cref="CraftingMastery"/>.
    /// </summary>
    public static class CraftingService
    {
        /// <summary>
        /// True when the recipe is available to this player at all: forge recipes always are, workshop
        /// blueprints only once learned from a quest or mission reward.
        /// </summary>
        public static bool IsKnown(Blueprint blueprint, PlayerProfile profile)
        {
            if (blueprint == null) return false;
            if (!blueprint.RequiresLearning) return true;

            return profile?.KnownBlueprints != null &&
                   profile.KnownBlueprints.Contains(blueprint.Id);
        }

        /// <summary>True when the smith's mastery level meets the recipe's requirement.</summary>
        public static bool HasMastery(Blueprint blueprint, PlayerProfile profile) =>
            blueprint != null && CraftingMastery.LevelOf(profile) >= blueprint.RequiredMastery;

        /// <summary>
        /// Teach the player a workshop blueprint. Returns false when it was already known, so callers
        /// can tell a genuinely new recipe from a duplicate reward.
        /// </summary>
        public static bool LearnBlueprint(PlayerProfile profile, string blueprintId)
        {
            if (profile == null || string.IsNullOrEmpty(blueprintId)) return false;

            profile.KnownBlueprints ??= new List<string>();
            if (profile.KnownBlueprints.Contains(blueprintId)) return false;

            profile.KnownBlueprints.Add(blueprintId);
            return true;
        }

        /// <summary>
        /// True when the recipe is known, the mastery requirement is met and the profile holds enough
        /// of every ingredient to craft once.
        /// </summary>
        public static bool CanCraft(Blueprint blueprint, PlayerProfile profile)
        {
            if (blueprint == null || profile == null) return false;
            if (!IsKnown(blueprint, profile) || !HasMastery(blueprint, profile)) return false;
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
        /// Consume the blueprint's ingredients, roll the result's quality and add it to the profile
        /// inventory. Returns false and leaves the inventory untouched when the player can't craft it.
        /// </summary>
        public static bool TryCraft(Blueprint blueprint, PlayerProfile profile, out CraftOutcome outcome)
        {
            outcome = null;
            if (!CanCraft(blueprint, profile)) return false;

            if (blueprint.Ingredients != null)
            {
                foreach (var ingredient in blueprint.Ingredients)
                {
                    if (ingredient == null || string.IsNullOrEmpty(ingredient.ItemId)) continue;
                    profile.RemoveItem(ingredient.ItemId, ingredient.Quantity);
                }
            }

            var quality = RollQuality(blueprint, profile);
            string itemId = ItemQuality.MakeId(blueprint.ResultItemId, quality);
            int resultQty = blueprint.ResultQuantity > 0 ? blueprint.ResultQuantity : 1;
            profile.AddItem(itemId, resultQty);

            int masteryExp = blueprint.MasteryExp;
            outcome = new CraftOutcome
            {
                ItemId = itemId,
                Quantity = resultQty,
                Quality = quality,
                MasteryExpGained = masteryExp,
                MasteryLevelsGained = CraftingMastery.ApplyExp(profile, masteryExp)
            };

            // Count the craft toward the statistics quests track. The caller saves the profile,
            // which also persists this counter and the mastery gain.
            StatisticsService.RecordCraft(profile, blueprint.Id);
            return true;
        }

        /// <summary>
        /// Whether this recipe's result can come out above Common. Only equippable results carry stats
        /// worth scaling, so materials (ingots, leather) always forge out plain no matter the mastery.
        /// </summary>
        public static bool RollsQuality(Blueprint blueprint)
        {
            if (blueprint == null || !blueprint.AllowQuality) return false;
            return ItemCatalog.TryGet(blueprint.ResultItemId, out var result) && result.IsEquippable;
        }

        private static ItemRarity RollQuality(Blueprint blueprint, PlayerProfile profile) =>
            RollsQuality(blueprint) ? CraftingMastery.RollQuality(profile) : ItemRarity.Common;

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
