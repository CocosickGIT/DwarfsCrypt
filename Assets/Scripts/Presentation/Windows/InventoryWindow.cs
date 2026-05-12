using Core.Items;
using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Presentation.Windows.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    public class InventoryWindow : WindowBase
    {
        [Header("References")]
        [SerializeField] private Canvas _rootCanvas;
        [SerializeField] private InventoryGrid _inventoryGrid;
        [SerializeField] private EquipmentPanel _equipmentPanel;

        [Header("Controls")]
        [SerializeField] private Button _sortByTypeButton;
        [SerializeField] private Button _sortByRarityButton;
        [SerializeField] private Button _closeButton;

        public override WindowType Type => WindowType.Inventory;

        private void Awake()
        {
            BaseSlot.SetRootCanvas(_rootCanvas);
            _equipmentPanel.SetInventoryGrid(_inventoryGrid);

            _sortByTypeButton.onClick.AddListener(_inventoryGrid.SortByType);
            _sortByRarityButton.onClick.AddListener(_inventoryGrid.SortByRarity);
            _closeButton.onClick.AddListener(Close);
        }

        private void Start()
        {
            SeedTestItems();
        }

        public bool TryAddItem(InventoryItem item) => _inventoryGrid.TryAddItem(item);

        protected override void OnOpen() { }
        protected override void OnClose() { }

        private void SeedTestItems()
        {
            var collection = ItemConfigLoader.LoadFromStreamingAssets("Items/items");
            foreach (var entry in collection.Items)
                _inventoryGrid.TryAddItem(new InventoryItem(entry, entry.Quantity));
        }
    }
}
