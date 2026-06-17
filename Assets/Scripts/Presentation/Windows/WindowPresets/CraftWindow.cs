using System.Collections.Generic;
using Core.Crafting;
using Core.Items;
using Core.Player;
using DwarfsCrypt.Domain.Crafting;
using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Domain.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// Lists every crafting blueprint and lets the player craft from inventory materials.
    /// Rows are built from code (like InventoryGrid/RewardWindow) so the only required editor
    /// wiring is registering the window; assign a scene container as the list root if you want
    /// the rows to sit somewhere specific, otherwise they are built directly under this object.
    /// Crafting reads and writes the persistent <see cref="PlayerProfile"/> and saves on each craft.
    /// </summary>
    public class CraftWindow : WindowBase
    {
        [Header("References")]
        [Tooltip("Container the blueprint rows are spawned under. If empty, rows are built under " +
                 "this window's transform. A Vertical Layout Group is added automatically.")]
        [SerializeField] private Transform _blueprintListRoot;

        [Header("Controls")]
        [SerializeField] private Button _closeButton;

        public override WindowType Type => WindowType.Craft;

        private readonly List<BlueprintRowUI> _rows = new();

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        protected override void OnOpen()
        {
            PlayerProfileService.EnsureLoaded();
            BuildRows();
        }

        private Transform ListRoot => _blueprintListRoot != null ? _blueprintListRoot : transform;

        private void BuildRows()
        {
            var root = ListRoot;

            // Tidy only the rows we built last time (the root may also hold a close button etc.).
            foreach (var row in _rows)
                if (row?.Root != null) Destroy(row.Root);
            _rows.Clear();

            EnsureVerticalLayout(root);

            foreach (var blueprint in BlueprintCatalog.All)
            {
                if (blueprint == null) continue;
                var row = BlueprintRowUI.Build(root, blueprint, OnCraftClicked);
                _rows.Add(row);
                row.Refresh(PlayerProfileService.Current);
            }
        }

        private void OnCraftClicked(Blueprint blueprint)
        {
            var profile = PlayerProfileService.Current;
            if (!CraftingService.TryCraft(blueprint, profile))
                return;

            PlayerProfileService.Save();

            // Crafting one item can change what else is now craftable (ore -> ingot -> sword),
            // so refresh every row's affordability and counts.
            foreach (var row in _rows)
                row.Refresh(profile);
        }

        private static void EnsureVerticalLayout(Transform root)
        {
            if (!root.TryGetComponent<VerticalLayoutGroup>(out var layout))
            {
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 6f;
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
        }
    }

    /// <summary>
    /// A single code-built blueprint row: result icon + name, an ingredient-with-counts line and a
    /// Craft button that enables/disables based on whether the player can currently afford it.
    /// </summary>
    internal class BlueprintRowUI
    {
        private static readonly Color PlaceholderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color HaveColor = new Color(0.75f, 0.95f, 0.75f);
        private static readonly Color MissingColor = new Color(0.95f, 0.6f, 0.6f);

        private readonly Blueprint _blueprint;
        private readonly TMP_Text _ingredientsText;
        private readonly Button _craftButton;
        private readonly TMP_Text _craftButtonLabel;

        /// <summary>The row's root GameObject, so the window can destroy it on rebuild.</summary>
        public GameObject Root { get; }

        private BlueprintRowUI(GameObject root, Blueprint blueprint, TMP_Text ingredientsText,
                               Button craftButton, TMP_Text craftButtonLabel)
        {
            Root = root;
            _blueprint = blueprint;
            _ingredientsText = ingredientsText;
            _craftButton = craftButton;
            _craftButtonLabel = craftButtonLabel;
        }

        public static BlueprintRowUI Build(Transform parent, Blueprint blueprint,
                                           System.Action<Blueprint> onCraft)
        {
            ItemCatalog.TryGet(blueprint.ResultItemId, out var result);

            var rowGO = new GameObject($"Blueprint_{blueprint.Id}",
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGO.transform.SetParent(parent, false);
            rowGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            rowGO.GetComponent<LayoutElement>().minHeight = 56f;

            var rowLayout = rowGO.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.padding = new RectOffset(6, 6, 6, 6);
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            // Result icon.
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconGO.transform.SetParent(rowGO.transform, false);
            var iconLayout = iconGO.GetComponent<LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = 44f;
            iconLayout.minHeight = iconLayout.preferredHeight = 44f;
            var icon = iconGO.GetComponent<Image>();
            var sprite = LoadIcon(result);
            icon.sprite = sprite;
            icon.color = sprite != null ? Color.white : PlaceholderColor;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Name + ingredients text column.
            var textColGO = new GameObject("Text", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColGO.transform.SetParent(rowGO.transform, false);
            textColGO.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var textColLayout = textColGO.GetComponent<VerticalLayoutGroup>();
            textColLayout.childControlWidth = true;
            textColLayout.childControlHeight = true;
            textColLayout.childForceExpandHeight = false;
            textColLayout.spacing = 2f;

            var nameText = CreateText(textColGO.transform, "Name", 16f, Color.white);
            nameText.text = result != null ? result.Name : blueprint.Name;
            if (blueprint.ResultQuantity > 1) nameText.text += $" x{blueprint.ResultQuantity}";

            var ingredientsText = CreateText(textColGO.transform, "Ingredients", 12f, Color.gray);

            // Craft button.
            var buttonGO = new GameObject("CraftButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(rowGO.transform, false);
            var buttonLayout = buttonGO.GetComponent<LayoutElement>();
            buttonLayout.minWidth = buttonLayout.preferredWidth = 90f;
            var craftButton = buttonGO.GetComponent<Button>();
            craftButton.targetGraphic = buttonGO.GetComponent<Image>();
            craftButton.onClick.AddListener(() => onCraft?.Invoke(blueprint));

            var craftButtonLabel = CreateText(buttonGO.transform, "Label", 14f, Color.white);
            craftButtonLabel.alignment = TextAlignmentOptions.Center;
            craftButtonLabel.text = "Craft";
            var labelRT = (RectTransform)craftButtonLabel.transform;
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;

            return new BlueprintRowUI(rowGO, blueprint, ingredientsText, craftButton, craftButtonLabel);
        }

        /// <summary>Update ingredient counts and the Craft button's enabled state from the profile.</summary>
        public void Refresh(PlayerProfile profile)
        {
            bool canCraft = CraftingService.CanCraft(_blueprint, profile);

            _ingredientsText.text = BuildIngredientLine(profile, out bool anyMissing);
            _ingredientsText.color = anyMissing ? MissingColor : HaveColor;

            _craftButton.interactable = canCraft;
            _craftButton.image.color = canCraft ? new Color(0.2f, 1f, 0f, 0.4f) : new Color(1f, 0.2f, 0f, 0.4f);
            _craftButtonLabel.color = canCraft ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        private string BuildIngredientLine(PlayerProfile profile, out bool anyMissing)
        {
            anyMissing = false;
            if (_blueprint.Ingredients == null || _blueprint.Ingredients.Count == 0)
                return "No materials required";

            var parts = new List<string>();
            foreach (var ingredient in _blueprint.Ingredients)
            {
                if (ingredient == null || string.IsNullOrEmpty(ingredient.ItemId)) continue;

                int have = profile != null ? profile.CountItem(ingredient.ItemId) : 0;
                if (have < ingredient.Quantity) anyMissing = true;

                string name = ItemCatalog.TryGet(ingredient.ItemId, out var data) ? data.Name : ingredient.ItemId;
                parts.Add($"{name} {have}/{ingredient.Quantity}");
            }

            return string.Join(",  ", parts);
        }

        private static Sprite LoadIcon(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.IconPath)) return null;
            return Resources.Load<Sprite>(data.IconPath);
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
