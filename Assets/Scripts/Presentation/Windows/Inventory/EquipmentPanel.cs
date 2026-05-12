using System.Collections.Generic;
using DwarfsCrypt.Domain.Items;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Windows.Inventory
{
    public class EquipmentPanel : MonoBehaviour
    {
        [SerializeField] private List<EquipmentSlot> _slots;

        private readonly Dictionary<EquipmentSlotType, EquipmentSlot> _slotMap = new();
        private InventoryGrid _inventoryGrid;

        private void Awake()
        {
            foreach (var slot in _slots)
                _slotMap[slot.SlotType] = slot;
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
