using System.Text;
using Core.Items;
using Core.Player;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Domain.Items;
using DwarfsCrypt.Presentation.Windows.Inventory;
using TMPro;
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
        [SerializeField] private TMP_Text _statsText;

        [Header("Controls")]
        [SerializeField] private Button _sortByTypeButton;
        [SerializeField] private Button _sortByRarityButton;
        [SerializeField] private Button _closeButton;

        public override WindowType Type => WindowType.Inventory;

        // Guards against the startup Close() (WindowService closes all windows on Awake) syncing
        // an empty grid back over the saved inventory. Only sync after a real open.
        private bool _loadedFromProfile;

        private void Awake()
        {
            BaseSlot.SetRootCanvas(_rootCanvas);
            _equipmentPanel.SetInventoryGrid(_inventoryGrid);

            _sortByTypeButton.onClick.AddListener(_inventoryGrid.SortByType);
            _sortByRarityButton.onClick.AddListener(_inventoryGrid.SortByRarity);
            _closeButton.onClick.AddListener(Close);

            // Keep the stats text in sync as gear is equipped/unequipped while the window is open.
            _equipmentPanel.EquipmentChanged += RefreshStats;
        }

        private void OnDestroy()
        {
            if (_equipmentPanel != null)
                _equipmentPanel.EquipmentChanged -= RefreshStats;
        }

        public bool TryAddItem(InventoryItem item) => _inventoryGrid.TryAddItem(item);

        // Rebuild the grid from the saved profile each time the window opens so it reflects
        // drops earned during play, then write any changes back to the profile on close.
        protected override void OnOpen() => LoadFromProfile();
        protected override void OnClose() => SyncToProfile();

        private void LoadFromProfile()
        {
            PlayerProfileService.EnsureLoaded();
            _loadedFromProfile = true;
            var profile = PlayerProfileService.Current;

            _inventoryGrid.Clear();
            foreach (var stack in profile.Inventory)
            {
                if (ItemCatalog.TryGet(stack.ItemId, out var data))
                    _inventoryGrid.TryAddItem(new InventoryItem(data, stack.Quantity));
                else
                    Debug.LogWarning($"[InventoryWindow] Unknown item id in save: {stack.ItemId}");
            }

            _equipmentPanel.LoadEquipped(profile.Equipment);

            RefreshStats();
        }

        /// <summary>
        /// Renders the player's current progression and stats as plain text, with the bonuses
        /// from currently equipped gear added on top of the profile's base stats.
        /// </summary>
        private void RefreshStats()
        {
            if (_statsText == null) return;

            var profile = PlayerProfileService.Current;
            if (profile == null) return;

            var s = profile.Stats;
            var b = _equipmentPanel.GetTotalBonuses();

            // Derived resources (HP/Stamina/Mana) use the same formulas the character itself uses
            // at spawn (e.g. MaxHp = baseMaxHp + Con * 10), so the window shows the real final value.
            // We evaluate them with and without gear so we can annotate the equipment contribution.
            var baseAttrs = BuildAttributes(s, profile.MaxHp, profile.MaxStamina, profile.MaxMana);
            var withGearStats = new CharacterStats(
                s.Str + b.Str, s.Dex + b.Dex, s.Con + b.Con,
                s.Wit + b.Wit, s.Men + b.Men, s.Luc + b.Luc);
            var gearAttrs = BuildAttributes(withGearStats,
                profile.MaxHp + b.MaxHp, profile.MaxStamina + b.MaxStamina, profile.MaxMana + b.MaxMana);

            var sb = new StringBuilder();
            sb.AppendLine($"{profile.Name}  (Lv. {profile.Level} {profile.Race})");
            sb.AppendLine($"EXP: {profile.Exp}    Gold: {profile.Gold}");
            sb.AppendLine();
            sb.AppendLine($"HP: {Resource(baseAttrs, gearAttrs, AttributeType.MaxHp)}    " +
                          $"Stamina: {Resource(baseAttrs, gearAttrs, AttributeType.MaxStamina)}    " +
                          $"Mana: {Resource(baseAttrs, gearAttrs, AttributeType.MaxMana)}");
            sb.AppendLine();
            sb.AppendLine($"STR: {Stat(s.Str, b.Str)}    DEX: {Stat(s.Dex, b.Dex)}    CON: {Stat(s.Con, b.Con)}");
            sb.Append($"WIT: {Stat(s.Wit, b.Wit)}    MEN: {Stat(s.Men, b.Men)}    LUC: {Stat(s.Luc, b.Luc)}");

            _statsText.text = sb.ToString();
        }

        private static CharacterAttributes BuildAttributes(CharacterStats stats, int maxHp, int maxStamina, int maxMana) =>
            new CharacterAttributes(stats, AttributeFormulas.Default(maxHp, maxStamina, maxMana));

        // Final derived value (incl. gear), annotating how much the equipped gear adds to it.
        private static string Resource(CharacterAttributes baseAttrs, CharacterAttributes gearAttrs, AttributeType attr)
        {
            int final = (int)gearAttrs.GetFinal(attr);
            int bonus = final - (int)baseAttrs.GetFinal(attr);
            return Format(final, bonus);
        }

        // Shows the total, annotating the equipment contribution (e.g. "12 (+2)") when non-zero.
        private static string Stat(int baseValue, int bonus) => Format(baseValue + bonus, bonus);

        private static string Format(int total, int bonus) =>
            bonus != 0 ? $"{total} ({bonus:+0;-0})" : $"{total}";

        private void SyncToProfile()
        {
            if (!_loadedFromProfile) return;

            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            profile.Inventory.Clear();
            foreach (var item in _inventoryGrid.Items)
            {
                if (item == null) continue;
                profile.Inventory.Add(new ItemStack(item.Data.Id, item.Quantity));
            }

            profile.Equipment = _equipmentPanel.GetEquippedItemIds();

            PlayerProfileService.Save();
        }
    }
}
