using System.Collections.Generic;
using Core.Crafting;
using Core.Player;
using Core.Quests;
using DwarfsCrypt.Domain.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// The quest-giver dialog: lists quests available to take (Accept) and the player's active
    /// quests with live progress (Claim when ready). Rows are built from code like CraftWindow, so
    /// the only required editor wiring is registering the window with the WindowService; assign a
    /// scene container as the list root to place the rows, otherwise they build under this transform.
    /// All state reads/writes go through <see cref="QuestService"/> on the persistent profile.
    /// </summary>
    public class QuestWindow : WindowBase
    {
        [Header("References")]
        [Tooltip("Container the quest rows are spawned under. If empty, rows are built under this " +
                 "window's transform. A Vertical Layout Group is added automatically.")]
        [SerializeField] private Transform _questListRoot;

        [Header("Controls")]
        [SerializeField] private Button _closeButton;

        public override WindowType Type => WindowType.Quest;

        private readonly List<GameObject> _spawned = new();

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        protected override void OnOpen()
        {
            PlayerProfileService.EnsureLoaded();
            Rebuild();
        }

        private Transform ListRoot => _questListRoot != null ? _questListRoot : transform;

        private void Rebuild()
        {
            var root = ListRoot;

            foreach (var go in _spawned)
                if (go != null) Destroy(go);
            _spawned.Clear();

            EnsureVerticalLayout(root);

            var active = QuestService.GetActive();
            var available = QuestService.GetAvailable();

            AddHeader(root, "Active Quests");
            if (active.Count == 0)
                AddHeader(root, "  (none)", 13f, Color.gray);
            foreach (var progress in active)
            {
                var def = QuestCatalog.Get(progress.QuestId);
                if (def == null) continue;
                _spawned.Add(QuestRowUI.BuildActive(root, def, progress, OnClaim));
            }

            AddHeader(root, "Available Quests");
            if (available.Count == 0)
                AddHeader(root, "  (none)", 13f, Color.gray);
            foreach (var def in available)
                _spawned.Add(QuestRowUI.BuildAvailable(root, def, OnAccept));
        }

        private void OnAccept(QuestDefinition def)
        {
            if (QuestService.Accept(def.Id))
                Rebuild();
        }

        private void OnClaim(QuestDefinition def)
        {
            if (QuestService.Claim(def.Id))
                Rebuild();
        }

        private void AddHeader(Transform root, string text, float size = 18f, Color? color = null)
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(root, false);
            go.GetComponent<LayoutElement>().minHeight = size + 8f;
            var label = QuestRowUI.CreateText(go.transform, "Label", size, color ?? Color.white);
            label.fontStyle = FontStyles.Bold;
            label.text = text;
            var rt = (RectTransform)label.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _spawned.Add(go);
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
    /// A single code-built quest row: name, description, objective lines and an action button whose
    /// label/state reflects whether the quest can be accepted, is in progress, or is ready to claim.
    /// </summary>
    internal static class QuestRowUI
    {
        private static readonly Color RowColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        private static readonly Color ReadyColor = new Color(0.2f, 1f, 0f, 0.4f);
        private static readonly Color DisabledColor = new Color(1f, 0.5f, 0f, 0.25f);
        private static readonly Color AcceptColor = new Color(0.2f, 0.6f, 1f, 0.5f);

        public static GameObject BuildAvailable(Transform parent, QuestDefinition def,
                                                System.Action<QuestDefinition> onAccept)
        {
            var row = BuildShell(parent, def, ObjectiveLines(def), out var button, out var label);
            label.text = "Accept";
            button.interactable = true;
            button.image.color = AcceptColor;
            button.onClick.AddListener(() => onAccept?.Invoke(def));
            return row;
        }

        public static GameObject BuildActive(Transform parent, QuestDefinition def,
                                             QuestProgress progress, System.Action<QuestDefinition> onClaim)
        {
            bool ready = progress.State == QuestState.ReadyToClaim;
            var row = BuildShell(parent, def, ProgressLines(def, progress), out var button, out var label);

            label.text = ready ? "Claim" : "In Progress";
            button.interactable = ready;
            button.image.color = ready ? ReadyColor : DisabledColor;
            label.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            if (ready)
                button.onClick.AddListener(() => onClaim?.Invoke(def));
            return row;
        }

        private static GameObject BuildShell(Transform parent, QuestDefinition def, string objectiveText,
                                             out Button button, out TMP_Text buttonLabel)
        {
            var rowGO = new GameObject($"Quest_{def.Id}",
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGO.transform.SetParent(parent, false);
            rowGO.GetComponent<Image>().color = RowColor;
            rowGO.GetComponent<LayoutElement>().minHeight = 72f;

            var rowLayout = rowGO.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.padding = new RectOffset(8, 8, 6, 6);
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            // Text column: name (+ category tag), description, objectives.
            var textColGO = new GameObject("Text", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColGO.transform.SetParent(rowGO.transform, false);
            textColGO.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var textColLayout = textColGO.GetComponent<VerticalLayoutGroup>();
            textColLayout.childControlWidth = true;
            textColLayout.childControlHeight = true;
            textColLayout.childForceExpandHeight = false;
            textColLayout.spacing = 2f;

            var nameText = CreateText(textColGO.transform, "Name", 16f, Color.white);
            nameText.fontStyle = FontStyles.Bold;
            nameText.text = def.CategoryValue == QuestCategory.Normal
                ? def.Name
                : $"{def.Name}  [{def.CategoryValue}]";

            if (!string.IsNullOrEmpty(def.Description))
                CreateText(textColGO.transform, "Desc", 12f, Color.gray).text = def.Description;

            CreateText(textColGO.transform, "Objectives", 13f, new Color(0.85f, 0.85f, 0.7f)).text = objectiveText;

            var rewardText = CreateText(textColGO.transform, "Reward", 12f, new Color(0.7f, 0.85f, 1f));
            rewardText.text = RewardLine(def);

            // Action button.
            var buttonGO = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(rowGO.transform, false);
            var buttonLayout = buttonGO.GetComponent<LayoutElement>();
            buttonLayout.minWidth = buttonLayout.preferredWidth = 100f;
            button = buttonGO.GetComponent<Button>();
            button.targetGraphic = buttonGO.GetComponent<Image>();

            buttonLabel = CreateText(buttonGO.transform, "Label", 14f, Color.white);
            buttonLabel.alignment = TextAlignmentOptions.Center;
            var labelRT = (RectTransform)buttonLabel.transform;
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;

            return rowGO;
        }

        private static string ObjectiveLines(QuestDefinition def)
        {
            if (def.Objectives == null || def.Objectives.Count == 0) return "No objectives";

            var parts = new List<string>();
            foreach (var o in def.Objectives)
            {
                if (o == null) continue;
                parts.Add($"{Verb(o.ObjectiveType)} {TargetName(o)} x{o.Amount}");
            }
            return string.Join("\n", parts);
        }

        private static string ProgressLines(QuestDefinition def, QuestProgress progress)
        {
            if (progress.Objectives == null || progress.Objectives.Count == 0) return "No objectives";

            var parts = new List<string>();
            for (int i = 0; i < progress.Objectives.Count; i++)
            {
                var op = progress.Objectives[i];
                var def_o = def.Objectives != null && i < def.Objectives.Count ? def.Objectives[i] : null;
                int have = QuestService.CurrentAmount(op);
                string label = def_o != null ? $"{Verb(def_o.ObjectiveType)} {TargetName(def_o)}" : op.StatKey;
                parts.Add($"{label} {have}/{op.Required}");
            }
            return string.Join("\n", parts);
        }

        private static string RewardLine(QuestDefinition def)
        {
            if (def.Reward == null) return string.Empty;
            var parts = new List<string>();
            if (def.Reward.Exp > 0) parts.Add($"{def.Reward.Exp} XP");
            if (def.Reward.Gold > 0) parts.Add($"{def.Reward.Gold} gold");
            if (def.Reward.Drops != null)
                foreach (var d in def.Reward.Drops)
                    if (d != null && !string.IsNullOrEmpty(d.ItemId))
                        parts.Add(d.ItemId);
            return parts.Count > 0 ? "Reward: " + string.Join(", ", parts) : string.Empty;
        }

        private static string Verb(QuestObjectiveType type) =>
            type == QuestObjectiveType.Kill ? "Kill" : "Craft";

        private static string TargetName(QuestObjective o)
        {
            if (string.IsNullOrEmpty(o.TargetId))
                return o.ObjectiveType == QuestObjectiveType.Kill ? "enemies" : "items";

            if (o.ObjectiveType == QuestObjectiveType.Craft && BlueprintCatalog.TryGet(o.TargetId, out var bp))
                return bp.Name;

            return o.TargetId;
        }

        public static TMP_Text CreateText(Transform parent, string name, float fontSize, Color color)
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
