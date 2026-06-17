using System;
using System.Collections.Generic;
using Core.Items;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Domain.Items;

namespace DwarfsCrypt.Domain.Player
{
    /// <summary>
    /// The persisted player progression: level/exp/gold, base stats, equipment and inventory.
    /// Saved to persistentDataPath and seeded from a read-only starter profile in StreamingAssets.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string Name;
        public string Race;
        public int Level = 1;
        public int Exp;
        public int Gold;
        public CharacterStats Stats;
        public int MaxHp;
        public int MaxStamina;
        public int MaxMana;
        public List<string> Equipment = new List<string>();   // item ids
        public List<ItemStack> Inventory = new List<ItemStack>();

        /// <summary>
        /// Build the runtime <see cref="CharacterConfig"/> the spawner uses for the player.
        /// Equipment bonuses are folded into the base stats/resources so the spawned character's
        /// final attributes (e.g. MaxHp) match what the inventory window displays.
        /// </summary>
        public CharacterConfig ToCharacterConfig()
        {
            var bonuses = GetEquipmentBonuses();
            var stats = new CharacterStats(
                Stats.Str + bonuses.Str,
                Stats.Dex + bonuses.Dex,
                Stats.Con + bonuses.Con,
                Stats.Wit + bonuses.Wit,
                Stats.Men + bonuses.Men,
                Stats.Luc + bonuses.Luc);

            return new CharacterConfig
            {
                Name = Name,
                Race = Race,
                Level = Level,
                Stats = stats,
                MaxHp = MaxHp + bonuses.MaxHp,
                MaxStamina = MaxStamina + bonuses.MaxStamina,
                MaxMana = MaxMana + bonuses.MaxMana,
                Equipment = Equipment != null ? new List<string>(Equipment) : new List<string>(),
                Rewards = null
            };
        }

        /// <summary>Sum of the stat bonuses from every currently equipped item id.</summary>
        public EquipmentBonuses GetEquipmentBonuses()
        {
            var bonuses = new EquipmentBonuses();
            if (Equipment == null) return bonuses;

            foreach (var id in Equipment)
                if (ItemCatalog.TryGet(id, out var data))
                    bonuses.Add(data);

            return bonuses;
        }

        /// <summary>Add an item to the inventory, stacking onto an existing entry of the same id.</summary>
        public void AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;

            Inventory ??= new List<ItemStack>();

            var existing = Inventory.Find(s => s.ItemId == itemId);
            if (existing != null)
                existing.Quantity += quantity;
            else
                Inventory.Add(new ItemStack(itemId, quantity));
        }

        /// <summary>Total quantity of an item held across all inventory stacks.</summary>
        public int CountItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || Inventory == null) return 0;

            int total = 0;
            foreach (var stack in Inventory)
                if (stack != null && stack.ItemId == itemId)
                    total += stack.Quantity;

            return total;
        }

        /// <summary>
        /// Remove <paramref name="quantity"/> of an item, draining across stacks and dropping any
        /// emptied entries. Returns false (and changes nothing) if the player doesn't have enough.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            if (CountItem(itemId) < quantity) return false;

            int remaining = quantity;
            for (int i = Inventory.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = Inventory[i];
                if (stack == null || stack.ItemId != itemId) continue;

                int take = Math.Min(stack.Quantity, remaining);
                stack.Quantity -= take;
                remaining -= take;

                if (stack.Quantity <= 0)
                    Inventory.RemoveAt(i);
            }

            return true;
        }
    }
}
