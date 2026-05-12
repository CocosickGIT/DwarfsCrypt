using System;
using DwarfsCrypt.Domain.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows.Inventory
{
    public class ItemSlot : BaseSlot
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _quantityText;

        public int SlotIndex { get; private set; }

        // Wired by InventoryGrid
        public Action<BaseSlot, ItemSlot> OnDropOnSlot;

        // Called when slot is created from code (no prefab inspector refs)
        public void Setup(Image background, Image icon, TextMeshProUGUI quantityText)
        {
            _background = background;
            _icon = icon;
            _quantityText = quantityText;
        }

        public void Initialize(int index)
        {
            SlotIndex = index;
            Clear();
        }

        public override bool AcceptsItem(ItemData data) => data != null;

        protected override void OnItemSet()
        {
            _icon.sprite = LoadIcon();
            _icon.color = Color.white;
            _quantityText.text = Item.Data.IsStackable && Item.Quantity > 1
                ? Item.Quantity.ToString()
                : string.Empty;
            ApplyRarityStyle(Item.Data.Rarity);
        }

        protected override void OnSlotCleared()
        {
            _icon.sprite = null;
            _icon.color = Color.clear;
            _quantityText.text = string.Empty;
            _background.color = new Color(0.18f, 0.18f, 0.18f);
        }

        protected override void HandleDrop(BaseSlot source) =>
            OnDropOnSlot?.Invoke(source, this);

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

        private void ApplyRarityStyle(ItemRarity rarity)
        {
            _background.color = rarity switch
            {
                ItemRarity.Common    => new Color(0.25f, 0.25f, 0.25f),
                ItemRarity.Uncommon  => new Color(0.08f, 0.40f, 0.08f),
                ItemRarity.Rare      => new Color(0.08f, 0.20f, 0.65f),
                ItemRarity.Epic      => new Color(0.40f, 0.08f, 0.60f),
                ItemRarity.Legendary => new Color(0.80f, 0.35f, 0.00f),
                _                    => new Color(0.18f, 0.18f, 0.18f)
            };
        }
    }
}
