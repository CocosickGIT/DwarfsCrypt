using System;
using System.Collections.Generic;
using DwarfsCrypt.Domain.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows.Inventory
{
    public class EquipmentSlot : BaseSlot
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _slotPlaceholderIcon; // visual hint for empty slot
        [SerializeField] private EquipmentSlotType _slotType;

        public EquipmentSlotType SlotType => _slotType;

        // Wired by EquipmentPanel
        public Action<BaseSlot, EquipmentSlot> OnDropOnEquipment;

        private static readonly HashSet<(ItemType, EquipmentSlotType)> ValidCombinations = new()
        {
            (ItemType.Weapon, EquipmentSlotType.MainHand),
            (ItemType.Shield, EquipmentSlotType.OffHand),
            (ItemType.Helmet, EquipmentSlotType.Head),
            (ItemType.Chest,  EquipmentSlotType.Chest),
            (ItemType.Legs,   EquipmentSlotType.Legs),
            (ItemType.Feet,   EquipmentSlotType.Feet),
            (ItemType.Ring,   EquipmentSlotType.Ring1),
            (ItemType.Ring,   EquipmentSlotType.Ring2),
            (ItemType.Amulet, EquipmentSlotType.Amulet),
        };

        public override bool AcceptsItem(ItemData data)
        {
            if (data == null) return false;
            return ValidCombinations.Contains((data.Type, _slotType));
        }

        // Equipment slots do not accept drops from other equipment slots
        public override void OnDrop(PointerEventData eventData)
        {
            if (DragSource is EquipmentSlot) return;
            base.OnDrop(eventData);
        }

        protected override void OnItemSet()
        {
            _icon.sprite = LoadIcon();
            _icon.color = Color.white;
            if (_slotPlaceholderIcon != null)
                _slotPlaceholderIcon.gameObject.SetActive(false);
        }

        protected override void OnSlotCleared()
        {
            _icon.sprite = null;
            _icon.color = Color.clear;
            if (_slotPlaceholderIcon != null)
                _slotPlaceholderIcon.gameObject.SetActive(true);
        }

        protected override void HandleDrop(BaseSlot source) =>
            OnDropOnEquipment?.Invoke(source, this);

        protected override Sprite GetIcon() => _icon.sprite;

        protected override void OnBeginDragInternal() =>
            _icon.color = new Color(1f, 1f, 1f, 0.3f);

        protected override void OnEndDragInternal()
        {
            if (Item != null) _icon.color = Color.white;
        }

        private Sprite LoadIcon()
        {
            if (string.IsNullOrEmpty(Item?.Data.IconPath)) return null;
            return Resources.Load<Sprite>(Item.Data.IconPath);
        }
    }
}
