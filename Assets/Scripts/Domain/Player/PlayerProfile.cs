using System;
using System.Collections.Generic;
using Core.Items;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Domain.Quests;
using DwarfsCrypt.Domain.Statistics;

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

        // World-map progression: keys of the zones the player has cleared at least once, in the
        // "townId/zoneId" form produced by TownProgressService. A town unlocks once every zone of
        // the town that gates it (TownConfig.UnlockedBy) appears here, so unlock state is always
        // derived from this one list rather than stored separately and kept in sync.
        public List<string> ClearedZones = new List<string>();

        // Id of the town the player is currently standing in, written on travel so that quitting in
        // a town and loading the slot again resumes there instead of in the starting town. Empty or
        // an unknown id (e.g. a town removed from towns.json) resolves back to the starting town.
        public string CurrentTownId;

        // Lifetime statistic counters (kills, crafts, ...) and the player's quest log.
        // Quests measure progress against a baseline snapshotted from Statistics at accept-time.
        public List<StatEntry> Statistics = new List<StatEntry>();
        public List<QuestProgress> QuestLog = new List<QuestProgress>();

        // Smithing progression. Mastery is its own experience track (see CraftingMastery) and decides
        // the quality tier rolled at the workshop. KnownBlueprints holds the ids of the workshop
        // recipes the player has learned from quest/mission blueprint rewards; forge recipes are
        // common knowledge and are never listed here.
        public int CraftingMastery = 1;
        public int CraftingExp;
        public List<string> KnownBlueprints = new List<string>();

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
