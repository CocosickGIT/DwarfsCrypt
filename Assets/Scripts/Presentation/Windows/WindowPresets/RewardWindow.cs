using System;
using Core.Items;
using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Domain.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    public class RewardWindow : WindowBase
    {
        [Header("Summary")]
        [SerializeField] private TMP_Text _expText;
        [SerializeField] private TMP_Text _goldText;

        [Header("Item list")]
        [Tooltip("Container the item cells are spawned under. Put a Grid Layout Group on it with " +
                 "Constraint = Fixed Row Count, Constraint Count = 1 for a single row of cells.")]
        [SerializeField] private Transform _itemListRoot;
        [Tooltip("Optional. If left empty, cells are built from code (rarity background + icon + quantity), " +
                 "just like inventory slots. Assign a RewardItemRow prefab only to override the look.")]
        [SerializeField] private RewardItemRow _itemRowPrefab;

        [Header("Controls")]
        [SerializeField] private Button _closeButton;

        public override WindowType Type => WindowType.Reward;

        /// <summary>Raised when the player dismisses the reward screen.</summary>
        public event Action Closed;

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        /// <summary>Populate the screen with a run total and open it.</summary>
        public void Show(RewardResult summary)
        {
            summary ??= new RewardResult();

            if (_expText != null) _expText.text = $"EXP +{summary.Exp}";
            if (_goldText != null) _goldText.text = $"Gold +{summary.Gold}";

            BuildItemRows(summary);
            Open();
        }

        protected override void OnClose() => Closed?.Invoke();

        private void BuildItemRows(RewardResult summary)
        {
            if (_itemListRoot == null) return;

            // The list root must be a scene object. If it points at a Project asset (a prefab),
            // Unity can't parent spawned rows under it — re-assign it to the in-scene container.
            if (_itemListRoot.gameObject.scene.IsValid() == false)
            {
                Debug.LogError("[RewardWindow] 'Item List Root' is a Project asset, not a scene object. " +
                               "Assign the in-scene container (a child of the RewardWindow in the Hierarchy).");
                return;
            }

            for (int i = _itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(_itemListRoot.GetChild(i).gameObject);

            foreach (var stack in summary.Items)
            {
                ItemCatalog.TryGet(stack.ItemId, out var data);

                if (_itemRowPrefab != null)
                {
                    // Instantiate unparented, then parent into the scene container (worldPositionStays:false).
                    var row = Instantiate(_itemRowPrefab);
                    row.transform.SetParent(_itemListRoot, false);
                    row.Set(data, stack.Quantity);
                }
                else
                {
                    BuildCodeCell(data, stack.Quantity);
                }
            }
        }

        // Builds a square cell from code (no prefab needed), mirroring InventoryGrid.BuildSlotObject:
        // a rarity-colored background, an inset icon, and a quantity overlay. A Grid Layout Group on
        // the list root sizes/arranges these into a row.
        private void BuildCodeCell(ItemData data, int quantity)
        {
            var cell = new GameObject("RewardCell", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(_itemListRoot, false);
            cell.GetComponent<Image>().color = RarityColor(data?.Rarity ?? ItemRarity.Common);

            // Icon — inset inside the cell like the inventory slot.
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(cell.transform, false);
            var iconRT = (RectTransform)iconGO.transform;
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

            var icon = iconGO.GetComponent<Image>();
            var sprite = LoadIcon(data);
            icon.sprite = sprite;
            // With no sprite a UI Image draws a solid quad — show a visible gray placeholder box.
            icon.color = sprite != null ? Color.white : PlaceholderColor;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Quantity overlay (bottom-right) — shown only when stacking, like the inventory slot.
            var qtyGO = new GameObject("Quantity", typeof(RectTransform), typeof(TextMeshProUGUI));
            qtyGO.transform.SetParent(cell.transform, false);
            var qtyRT = (RectTransform)qtyGO.transform;
            qtyRT.anchorMin = Vector2.zero;
            qtyRT.anchorMax = Vector2.one;
            qtyRT.offsetMin = new Vector2(2f, 2f);
            qtyRT.offsetMax = new Vector2(-2f, -2f);

            var qty = qtyGO.GetComponent<TextMeshProUGUI>();
            qty.text = quantity > 1 ? quantity.ToString() : string.Empty;
            qty.fontSize = 16f;
            qty.color = Color.white;
            qty.alignment = TextAlignmentOptions.BottomRight;
            qty.raycastTarget = false;
        }

        // Shown in the icon box when an item has no sprite yet (placeholder, not transparent).
        private static readonly Color PlaceholderColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private static Sprite LoadIcon(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.IconPath)) return null;
            return Resources.Load<Sprite>(data.IconPath);
        }

        // Same palette as the inventory slot (ItemSlot.ApplyRarityStyle).
        private static Color RarityColor(ItemRarity rarity) => rarity switch
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
