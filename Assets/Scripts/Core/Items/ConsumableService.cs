using Core.Player;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Domain.Player;

namespace Core.Items
{
    /// <summary>
    /// Applies a consumable item from a <see cref="PlayerProfile"/>'s inventory to a live
    /// <see cref="Character"/>: validates, removes one from the stack, applies its restore
    /// effect, and persists the profile. Kept out of the UI/controller so the rules
    /// (don't waste a potion at full HP, must own one, must be alive) live in one place.
    /// </summary>
    public static class ConsumableService
    {
        public enum Result
        {
            Consumed,
            NotConsumable,
            NoneInInventory,
            NoEffectNeeded,
            Blocked
        }

        public static Result TryConsume(PlayerProfile profile, string itemId, Character character)
        {
            if (profile == null || character == null || string.IsNullOrEmpty(itemId))
                return Result.Blocked;

            if (character.IsDead)
                return Result.Blocked;

            if (!ItemCatalog.TryGet(itemId, out var data) || data.Type != ItemType.Consumable)
                return Result.NotConsumable;

            if (profile.CountItem(itemId) <= 0)
                return Result.NoneInInventory;

            // Don't burn a potion when its effect would do nothing (e.g. already at full HP).
            if (!HasEffect(data, character))
                return Result.NoEffectNeeded;

            if (!profile.RemoveItem(itemId, 1))
                return Result.NoneInInventory;

            if (data.RestoreHp > 0)
                character.Heal(data.RestoreHp);

            PlayerProfileService.Save();
            return Result.Consumed;
        }

        // True when at least one of the item's restore effects would change the character's state.
        private static bool HasEffect(ItemData data, Character character)
        {
            return data.RestoreHp > 0 && character.CurrentHp < character.MaxHp;
        }
    }
}
