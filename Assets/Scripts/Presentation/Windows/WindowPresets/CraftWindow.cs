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
    /// The smithing screen, split into two tabs that match the two crafting stations:
    ///
    /// <list type="bullet">
    /// <item><b>Forge</b> — always-available recipes that turn raw drops into basic materials.</item>
    /// <item><b>Workshop</b> — blueprints earned from quests and missions, where the result's quality
    /// is rolled against the player's crafting mastery.</item>
    /// </list>
    ///
    /// Rows are built from code (like InventoryGrid/RewardWindow) so the only required editor wiring
    /// is registering the window; assign a scene container as the list root if you want the contents
    /// to sit somewhere specific, otherwise everything is built directly under this object.
    /// Crafting reads and writes the persistent <see cref="PlayerProfile"/> and saves on each craft.
    /// </summary>
    public class CraftWindow : WindowBase
    {
        [Header("References")]
        [Tooltip("Container the mastery header, station tabs and blueprint rows are spawned under. " +
                 "If empty, they are built under this window's transform. A Vertical Layout Group is " +
                 "added automatically.")]
        [SerializeField] private Transform _blueprintListRoot;

        [Header("Controls")]
        [SerializeField] private Button _closeButton;

        public override WindowType Type => WindowType.Craft;

        private readonly List<BlueprintRowUI> _rows = new();

        private CraftStation _station = CraftStation.Forge;

        // Chrome built once on first open and kept across tab switches / crafts.
        private TMP_Text _masteryText;
        private TMP_Text _qualityText;
        private TMP_Text _resultText;
        private readonly Dictionary<CraftStation, Button> _tabButtons = new();

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        protected override void OnOpen()
        {
            PlayerProfileService.EnsureLoaded();
            EnsureChrome();
            SetStation(_station);
        }

        private Transform ListRoot => _blueprintListRoot != null ? _blueprintListRoot : transform;

        private void SetStation(CraftStation station)
        {
            _station = station;
            if (_resultText != null) _resultText.text = string.Empty;

            foreach (var tab in _tabButtons)
                tab.Value.image.color = tab.Key == station
                    ? new Color(0.30f, 0.45f, 0.75f, 0.9f)
                    : new Color(0.18f, 0.18f, 0.18f, 0.9f);

            BuildRows();
            RefreshHeader();
        }

        private void BuildRows()
        {
            var root = ListRoot;

            // Tidy only the rows we built last time (the root also holds the header and tabs).
            foreach (var row in _rows)
                if (row?.Root != null) Destroy(row.Root);
            _rows.Clear();

            var profile = PlayerProfileService.Current;

            // Only the recipes this town's station works: untagged ones everywhere, tagged ones as
            // the local speciality they were authored to be.
            foreach (var blueprint in BlueprintCatalog.ForStation(_station, Core.Towns.TownContext.ActiveTownId))
            {
                var row = BlueprintRowUI.Build(root, blueprint, OnCraftClicked);
                _rows.Add(row);
                row.Refresh(profile);
            }
        }

        private void OnCraftClicked(Blueprint blueprint)
        {
            var profile = PlayerProfileService.Current;
            if (!CraftingService.TryCraft(blueprint, profile, out var outcome))
                return;

            PlayerProfileService.Save();
            ShowResult(outcome);

            // Crafting one item can change what else is now craftable (ore -> ingot -> sword), and a
            // mastery level-up can unlock a gated recipe, so refresh every row and the header.
            foreach (var row in _rows)
                row.Refresh(profile);
            RefreshHeader();
        }

        private void ShowResult(CraftOutcome outcome)
        {
            if (_resultText == null) return;

            string name = ItemCatalog.TryGet(outcome.ItemId, out var data) ? data.Name : outcome.ItemId;
            string line = outcome.Quantity > 1 ? $"Crafted {name} x{outcome.Quantity}" : $"Crafted {name}";
            if (outcome.MasteryLevelsGained > 0) line += "   Mastery up!";

            _resultText.text = line;
            _resultText.color = ItemQuality.RarityTextColor(outcome.Quality);
        }

        private void RefreshHeader()
        {
            var profile = PlayerProfileService.Current;
            int level = CraftingMastery.LevelOf(profile);

            if (_masteryText != null)
                _masteryText.text = $"Crafting Mastery {level}   " +
                                    $"{profile.CraftingExp}/{CraftingMastery.ExpForLevel(level)} XP";

            if (_qualityText == null) return;

            // Forge output is deterministic, so the odds only mean something at the workshop.
            if (_station == CraftStation.Forge)
            {
                _qualityText.text = "Forge work is exact — materials always come out standard.";
                return;
            }

            var chances = CraftingMastery.QualityChances(profile);
            var parts = new List<string>(chances.Length);
            for (int i = 0; i < chances.Length; i++)
            {
                if (chances[i] < 0.005f) continue;
                parts.Add($"{QualityLabel((ItemRarity)i)} {chances[i] * 100f:0}%");
            }

            _qualityText.text = "Quality odds:  " + string.Join("   ", parts);
        }

        /// <summary>Quality tier as the smith names it. Common has no prefix word of its own.</summary>
        private static string QualityLabel(ItemRarity quality) =>
            quality == ItemRarity.Common ? "Standard" : ItemQuality.NamePrefix(quality);

        // --- Chrome construction ------------------------------------------------

        private void EnsureChrome()
        {
            if (_masteryText != null) return;

            var root = ListRoot;
            EnsureVerticalLayout(root);

            _masteryText = UIBuild.Text(root, "Mastery", 18f, Color.white);
            _qualityText = UIBuild.Text(root, "QualityOdds", 12f, new Color(0.75f, 0.8f, 0.9f));

            BuildTabBar(root);

            _resultText = UIBuild.Text(root, "CraftResult", 14f, Color.white);
        }

        private void BuildTabBar(Transform root)
        {
            var barGO = new GameObject("StationTabs",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            barGO.transform.SetParent(root, false);
            barGO.GetComponent<LayoutElement>().minHeight = 36f;

            var barLayout = barGO.GetComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 6f;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = true;

            _tabButtons[CraftStation.Forge] = BuildTab(barGO.transform, CraftStation.Forge, "Forge");
            _tabButtons[CraftStation.Workshop] = BuildTab(barGO.transform, CraftStation.Workshop, "Craft");
        }

        private Button BuildTab(Transform parent, CraftStation station, string label)
        {
            var tabGO = new GameObject($"Tab_{station}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            tabGO.transform.SetParent(parent, false);

            var button = tabGO.GetComponent<Button>();
            button.targetGraphic = tabGO.GetComponent<Image>();
            button.onClick.AddListener(() => SetStation(station));

            var text = UIBuild.Text(tabGO.transform, "Label", 16f, Color.white);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            UIBuild.Stretch((RectTransform)text.transform);

            return button;
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
    /// Craft button. The button reports why it is unusable — an unlearned blueprint, too low a
    /// mastery, or missing materials — rather than just greying out.
    /// </summary>
    internal class BlueprintRowUI
    {
        private static readonly Color PlaceholderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color HaveColor = new Color(0.75f, 0.95f, 0.75f);
        private static readonly Color MissingColor = new Color(0.95f, 0.6f, 0.6f);
        private static readonly Color LockedColor = new Color(0.8f, 0.75f, 0.5f);

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

            var nameText = UIBuild.Text(textColGO.transform, "Name", 16f, Color.white);
            nameText.text = result != null ? result.Name : blueprint.Name;
            if (blueprint.ResultQuantity > 1) nameText.text += $" x{blueprint.ResultQuantity}";

            var ingredientsText = UIBuild.Text(textColGO.transform, "Ingredients", 12f, Color.gray);

            // Craft button.
            var buttonGO = new GameObject("CraftButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(rowGO.transform, false);
            var buttonLayout = buttonGO.GetComponent<LayoutElement>();
            buttonLayout.minWidth = buttonLayout.preferredWidth = 90f;
            var craftButton = buttonGO.GetComponent<Button>();
            craftButton.targetGraphic = buttonGO.GetComponent<Image>();
            craftButton.onClick.AddListener(() => onCraft?.Invoke(blueprint));

            var craftButtonLabel = UIBuild.Text(buttonGO.transform, "Label", 14f, Color.white);
            craftButtonLabel.alignment = TextAlignmentOptions.Center;
            craftButtonLabel.text = "Craft";
            UIBuild.Stretch((RectTransform)craftButtonLabel.transform);

            return new BlueprintRowUI(rowGO, blueprint, ingredientsText, craftButton, craftButtonLabel);
        }

        /// <summary>Update the status line and the Craft button's state from the profile.</summary>
        public void Refresh(PlayerProfile profile)
        {
            bool known = CraftingService.IsKnown(_blueprint, profile);
            bool hasMastery = CraftingService.HasMastery(_blueprint, profile);
            bool canCraft = CraftingService.CanCraft(_blueprint, profile);

            if (!known)
            {
                _ingredientsText.text = "Blueprint not learned — earned from quests and missions";
                _ingredientsText.color = LockedColor;
                _craftButtonLabel.text = "Locked";
            }
            else if (!hasMastery)
            {
                _ingredientsText.text = $"Requires crafting mastery {_blueprint.RequiredMastery} " +
                                        $"(you are {CraftingMastery.LevelOf(profile)})";
                _ingredientsText.color = LockedColor;
                _craftButtonLabel.text = "Locked";
            }
            else
            {
                _ingredientsText.text = BuildIngredientLine(profile, out bool anyMissing);
                _ingredientsText.color = anyMissing ? MissingColor : HaveColor;
                _craftButtonLabel.text = "Craft";
            }

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
    }

    /// <summary>Small helpers shared by the code-built crafting UI.</summary>
    internal static class UIBuild
    {
        public static TMP_Text Text(Transform parent, string name, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Make a child fill its parent, for labels sitting inside a button.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
