using System;
using System.Collections.Generic;
using Core.Items;
using DwarfsCrypt.Domain.Items;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Windows.Inventory
{
    public class EquipmentPanel : MonoBehaviour
    {
        [SerializeField] private List<EquipmentSlot> _slots;

        private readonly Dictionary<EquipmentSlotType, EquipmentSlot> _slotMap = new();
        private InventoryGrid _inventoryGrid;

        /// <summary>Raised whenever the set of equipped items changes.</summary>
        public event Action EquipmentChanged;

        private void Awake()
        {
            foreach (var slot in _slots)
            {
                _slotMap[slot.SlotType] = slot;
                slot.OnContentChanged += () => EquipmentChanged?.Invoke();
            }
        }

        /// <summary>Sum of the stat bonuses from every currently equipped item.</summary>
        public EquipmentBonuses GetTotalBonuses()
        {
            var bonuses = new EquipmentBonuses();
            foreach (var slot in _slots)
                if (slot.Item != null)
                    bonuses.Add(slot.Item.Data);
            return bonuses;
        }

        public void SetInventoryGrid(InventoryGrid grid)
        {
            _inventoryGrid = grid;
            foreach (var slot in _slots)
                slot.OnDropOnEquipment = HandleEquipmentDrop;
        }

        public bool TryEquip(InventoryItem item, int sourceInventoryIndex)
        {
            if (item == null || !item.Data.IsEquippable) return false;

            var targetSlotType = ResolveRingSlot(item);
            if (!_slotMap.TryGetValue(targetSlotType, out var equipSlot)) return false;

            var displaced = equipSlot.Item;
            equipSlot.SetItem(item);
            _inventoryGrid.SetItemAt(sourceInventoryIndex, displaced);
            return true;
        }

        public InventoryItem Unequip(EquipmentSlotType slotType)
        {
            if (!_slotMap.TryGetValue(slotType, out var slot) || slot.Item == null) return null;
            var item = slot.Item;
            slot.Clear();
            return item;
        }

        public InventoryItem GetEquipped(EquipmentSlotType slotType) =>
            _slotMap.TryGetValue(slotType, out var slot) ? slot.Item : null;

        /// <summary>Item ids currently equipped, for persisting to the player profile.</summary>
        public List<string> GetEquippedItemIds()
        {
            var ids = new List<string>();
            foreach (var slot in _slots)
                if (slot.Item != null)
                    ids.Add(slot.Item.Data.Id);
            return ids;
        }

        /// <summary>Clear all slots and place the given saved item ids into their matching slots.</summary>
        public void LoadEquipped(IEnumerable<string> itemIds)
        {
            foreach (var slot in _slots)
                slot.Clear();

            if (itemIds == null) return;

            foreach (var id in itemIds)
            {
                if (!ItemCatalog.TryGet(id, out var data)) continue;

                var item = new InventoryItem(data);
                var slotType = ResolveRingSlot(item);
                if (_slotMap.TryGetValue(slotType, out var slot))
                    slot.SetItem(item);
            }
        }

        private void HandleEquipmentDrop(BaseSlot source, EquipmentSlot target)
        {
            if (source is not ItemSlot srcSlot) return;

            var invItem = _inventoryGrid.GetItem(srcSlot.SlotIndex);
            var currentEquipped = target.Item;

            target.SetItem(invItem);
            // Put displaced equipment back into the exact inventory slot the item came from
            _inventoryGrid.SetItemAt(srcSlot.SlotIndex, currentEquipped);
        }

        private EquipmentSlotType ResolveRingSlot(InventoryItem item)
        {
            if (item.Data.Type != ItemType.Ring) return item.Data.SlotType;

            // Prefer Ring1; fall back to Ring2
            if (_slotMap.TryGetValue(EquipmentSlotType.Ring1, out var r1) && r1.Item == null)
                return EquipmentSlotType.Ring1;
            return EquipmentSlotType.Ring2;
        }
    }
}
