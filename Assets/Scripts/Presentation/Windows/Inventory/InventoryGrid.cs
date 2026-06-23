using System;
using System.Collections.Generic;
using System.Linq;
using DwarfsCrypt.Domain.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows.Inventory
{
    public class InventoryGrid : MonoBehaviour
    {
        [SerializeField] private int _baseSlots = 24;
        [SerializeField] private int _expandAmount = 6;

        private readonly List<ItemSlot> _slots = new();
        private readonly List<InventoryItem> _items = new();

        public int Capacity => _slots.Count;
        public IReadOnlyList<InventoryItem> Items => _items;

        //debug
        [SerializeField] private bool _expand = false;
        private void Awake() => GrowSlots(_baseSlots);

        private void Update()
        {
            //debug only
            if (_expand == true)
            {
                Expand();
                _expand = false;
            }
        }

        public bool TryAddItem(InventoryItem item)
        {
            if (item == null) return false;

            if (item.Data.IsStackable)
            {
                foreach (var slot in _slots)
                {
                    if (slot.Item?.Data.Id == item.Data.Id && slot.Item.TryAddStack(item.Quantity))
                    {
                        slot.SetItem(slot.Item); // refresh visual
                        return true;
                    }
                }
            }

            int emptyIndex = FindEmptyIndex();
            if (emptyIndex == -1)
            {
                Expand();
                emptyIndex = FindEmptyIndex();
            }

            SetItemAt(emptyIndex, item);
            return true;
        }

        public bool RemoveItem(int slotIndex)
        {
            if (!IsValidIndex(slotIndex) || _items[slotIndex] == null) return false;
            SetItemAt(slotIndex, null);
            return true;
        }

        public InventoryItem GetItem(int slotIndex) =>
            IsValidIndex(slotIndex) ? _items[slotIndex] : null;

        public void SetItemAt(int slotIndex, InventoryItem item)
        {
            if (!IsValidIndex(slotIndex)) return;
            _items[slotIndex] = item;
            if (item != null)
                _slots[slotIndex].SetItem(item);
            else
                _slots[slotIndex].Clear();
        }

        public void Expand()
        {
            GrowSlots(_items.Count + _expandAmount);
        }

        /// <summary>Empty every slot (keeps the slot objects, just clears their items).</summary>
        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
                SetItemAt(i, null);
        }

        public void SortByType()
        {
            var sorted = _items
                .Where(i => i != null)
                .OrderBy(i => (int)i.Data.Type)
                .ThenByDescending(i => (int)i.Data.Rarity)
                .ToList();
            ApplySorted(sorted);
        }

        public void SortByRarity()
        {
            var sorted = _items
                .Where(i => i != null)
                .OrderByDescending(i => (int)i.Data.Rarity)
                .ThenBy(i => (int)i.Data.Type)
                .ToList();
            ApplySorted(sorted);
        }

        // Called by ItemSlot when an item is dropped onto it
        internal void HandleDrop(BaseSlot source, ItemSlot target)
        {
            if (source is ItemSlot srcSlot)
            {
                SwapInventorySlots(srcSlot, target);
            }
            else if (source is EquipmentSlot equipSlot)
            {
                // Unequip the dragged item into the inventory.
                var equipItem = equipSlot.Item;
                if (equipItem == null) return;

                var invItem = _items[target.SlotIndex];

                // Swap only when the displaced inventory item can legally go into the
                // equipment slot. Otherwise the dragged item lands in the inventory and the
                // target item stays put (it can't be force-pushed into an incompatible slot).
                if (invItem == null || equipSlot.AcceptsItem(invItem.Data))
                {
                    SetItemAt(target.SlotIndex, equipItem);

                    if (invItem != null)
                        equipSlot.SetItem(invItem);
                    else
                        equipSlot.Clear();
                }
                else
                {
                    equipSlot.Clear();
                    TryAddItem(equipItem);
                }
            }
        }

        private void SwapInventorySlots(ItemSlot a, ItemSlot b)
        {
            var tmp = _items[a.SlotIndex];
            _items[a.SlotIndex] = _items[b.SlotIndex];
            _items[b.SlotIndex] = tmp;

            if (_items[a.SlotIndex] != null) a.SetItem(_items[a.SlotIndex]); else a.Clear();
            if (_items[b.SlotIndex] != null) b.SetItem(_items[b.SlotIndex]); else b.Clear();
        }

        private void ApplySorted(List<InventoryItem> sorted)
        {
            for (int i = 0; i < _items.Count; i++)
                _items[i] = i < sorted.Count ? sorted[i] : null;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_items[i] != null) _slots[i].SetItem(_items[i]); else _slots[i].Clear();
            }
        }

        private void GrowSlots(int targetCount)
        {
            while (_slots.Count < targetCount)
            {
                var slot = BuildSlotObject(_slots.Count);
                _slots.Add(slot);
                _items.Add(null);
            }
        }

        private ItemSlot BuildSlotObject(int index)
        {
            var go = new GameObject($"Slot_{index}", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.18f);

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(go.transform, false);
            var iconRT = (RectTransform)iconGO.transform;
            iconRT.anchorMin = new Vector2(0.05f, 0.05f);
            iconRT.anchorMax = new Vector2(0.95f, 0.95f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            var icon = iconGO.AddComponent<Image>();
            icon.color = Color.clear;
            icon.raycastTarget = false;

            var textGO = new GameObject("Quantity", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textRT = (RectTransform)textGO.transform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;
            var qty = textGO.AddComponent<TextMeshProUGUI>();
            qty.fontSize = 11;
            qty.color = Color.black;
            qty.alignment = TextAlignmentOptions.BottomRight;
            qty.raycastTarget = false;

            var slot = go.AddComponent<ItemSlot>();
            slot.Setup(bg, icon, qty);
            slot.Initialize(index);
            slot.OnDropOnSlot = HandleDrop;
            return slot;
        }

        private int FindEmptyIndex() => _items.FindIndex(i => i == null);
        private bool IsValidIndex(int i) => i >= 0 && i < _items.Count;
    }
}
