using System;
using System.Collections.Generic;
using Core.Player;
using Core.Scenes;
using Core.Towns;
using DwarfsCrypt.Domain.Towns;
using DwarfsCrypt.Presentation.Towns;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// The world map: where the player travels *between* towns. Opened from inside a town, it shows
    /// the town the player is standing in plus the towns they can travel to, each as a node at its
    /// authored <see cref="TownConfig.MapPosition"/> labelled with its name and mob level range.
    /// Selecting a node and pressing Travel swaps the hub over to that town.
    ///
    /// It deliberately stops there. Which zone to play is a decision made *inside* a town (see
    /// <see cref="LevelSelectWindow"/>), and the town's bonuses apply while the player is in it —
    /// neither belongs on a screen about getting from one town to another.
    ///
    /// A town node is a prefab (<see cref="MapTownNodeView"/>) so its background, icon, name and
    /// level range are authored in the editor; the window only feeds it data. With no prefab
    /// assigned it falls back to a plain node built in code. Which towns are reachable comes from
    /// <see cref="TownProgressService"/>, which derives it from the zones the player has cleared.
    /// </summary>
    public class MapWindow : WindowBase
    {
        [Header("Map")]
        [Tooltip("Container the town nodes are placed in — normally the map image's RectTransform. " +
                 "Node positions are normalized to this rect. Falls back to this window's transform.")]
        [SerializeField] private RectTransform _mapRoot;

        [Tooltip("Optional. Image the map artwork is drawn on. Usually the same object as Map Root.")]
        [SerializeField] private Image _mapBackground;

        [Tooltip("Optional. Map artwork assigned to Map Background when the window opens. Leave " +
                 "empty to keep whatever sprite the image already has.")]
        [SerializeField] private Sprite _mapBackgroundSprite;

        [Tooltip("Also show towns that can't be travelled to yet, greyed out with the requirement " +
                 "that unlocks them. Off by default: the map is about where you can go.")]
        [SerializeField] private bool _showLockedTowns;

        [Header("Town nodes")]
        [Tooltip("Prefab instantiated once per town. Leave empty to build plain nodes in code.")]
        [SerializeField] private MapTownNodeView _townNodePrefab;

        [Tooltip("Per-town icons, matched by town id. Towns not listed here use the default icon.")]
        [SerializeField] private List<MapTownIcon> _townIcons = new();

        [Tooltip("Icon used for any town without an entry above. Optional — the node hides its icon " +
                 "slot when there is no sprite at all.")]
        [SerializeField] private Sprite _defaultTownIcon;

        [Tooltip("Size of a town node, in pixels. Used only for code-built nodes; a node prefab " +
                 "keeps its own size.")]
        [SerializeField] private Vector2 _nodeSize = new Vector2(200f, 88f);

        [Tooltip("Thickness of the line drawn between a town and the town that unlocks it.")]
        [SerializeField, Min(1f)] private float _lineThickness = 3f;

        [Header("Selected town")]
        [Tooltip("Label for the selected town's name. Optional.")]
        [SerializeField] private TMP_Text _townTitle;

        [Tooltip("Label for the selected town's mob level range. Optional.")]
        [SerializeField] private TMP_Text _townSubtitle;

        [Header("Controls")]
        [Tooltip("Travels to the selected town. Hidden for the town the player is already in and " +
                 "for towns that can't be reached yet.")]
        [SerializeField] private Button _travelButton;

        [SerializeField] private Button _closeButton;

        public override WindowType Type => WindowType.Map;

        private readonly List<GameObject> _spawnedLines = new();
        private readonly Dictionary<string, MapTownNodeView> _nodesByTown = new();

        private string _selectedTownId;

        /// <summary>The town the player is standing in.</summary>
        private static string CurrentTownId => TownContext.ActiveTownId;

        /// <summary>
        /// Rect the map is laid out in. Null when no Map Root is assigned and this window's own
        /// object isn't a RectTransform — a window object is often a plain Transform holding a
        /// Canvas, and there is no map to build in that case.
        /// </summary>
        private RectTransform MapRoot => _mapRoot != null ? _mapRoot : transform as RectTransform;

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);

            if (_travelButton != null)
                _travelButton.onClick.AddListener(TravelToSelectedTown);
        }

        protected override void OnOpen()
        {
            PlayerProfileService.EnsureLoaded();

            if (_mapBackground != null && _mapBackgroundSprite != null)
                _mapBackground.sprite = _mapBackgroundSprite;

            BuildMap();
            SelectTown(CurrentTownId);
        }

        // --- Map ---

        private void BuildMap()
        {
            foreach (var go in _spawnedLines)
                if (go != null) Destroy(go);
            _spawnedLines.Clear();

            foreach (var node in _nodesByTown.Values)
                if (node != null) Destroy(node.gameObject);
            _nodesByTown.Clear();

            var mapRoot = MapRoot;
            if (mapRoot == null)
            {
                Debug.LogWarning("[MapWindow] No Map Root assigned and this window's object is not a " +
                                 "RectTransform; the map cannot be built.", this);
                return;
            }

            var towns = TownCatalog.All;

            // Lines are drawn first so they sit behind the node buttons in sibling order, and the
            // canvas must be up to date before then: line positions are read in pixels off the map
            // rect, which is still zero-sized on the frame the window is enabled.
            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < towns.Count; i++)
            {
                var town = towns[i];
                if (town == null || string.IsNullOrEmpty(town.UnlockedBy)) continue;
                if (!IsVisible(town)) continue;
                if (!TownCatalog.TryGet(town.UnlockedBy, out var from) || !IsVisible(from)) continue;

                var line = MapUI.BuildLink(mapRoot, NormalizedPosition(from), NormalizedPosition(town),
                                           mapRoot.rect.size, _lineThickness,
                                           TownProgressService.IsTownUnlocked(town.Id));
                _spawnedLines.Add(line);
            }

            for (int i = 0; i < towns.Count; i++)
            {
                var town = towns[i];
                if (town == null || string.IsNullOrEmpty(town.Id) || !IsVisible(town)) continue;

                var node = SpawnNode(mapRoot, NormalizedPosition(town));
                node.Bind(town, IconFor(town.Id), StateOf(town), SelectTown);
                _nodesByTown[town.Id] = node;
            }
        }

        /// <summary>
        /// Whether a town appears on the map at all. The map answers "where can I go", so by default
        /// that is the town the player is in plus the ones they can travel to.
        /// </summary>
        private bool IsVisible(TownConfig town) =>
            _showLockedTowns || TownProgressService.IsTownUnlocked(town.Id);

        /// <summary>Instantiates the node prefab — or builds a plain one — and anchors it on the map.</summary>
        private MapTownNodeView SpawnNode(RectTransform mapRoot, Vector2 normalized)
        {
            MapTownNodeView node;
            Vector2 size;

            if (_townNodePrefab != null)
            {
                // worldPositionStays: false — the UI overload otherwise rewrites localScale to
                // compensate for the canvas scale.
                node = Instantiate(_townNodePrefab, mapRoot, false);

                // Keep the size the prefab was authored at; re-anchoring below would otherwise
                // reinterpret its sizeDelta as stretch offsets and collapse it.
                size = ((RectTransform)node.transform).rect.size;
                if (size.x <= 0f || size.y <= 0f) size = _nodeSize;
            }
            else
            {
                node = MapUI.BuildFallbackNode(mapRoot, _nodeSize);
                size = _nodeSize;
            }

            var rt = (RectTransform)node.transform;
            rt.anchorMin = rt.anchorMax = normalized;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            return node;
        }

        private static MapTownState StateOf(TownConfig town)
        {
            if (!TownProgressService.IsTownUnlocked(town.Id)) return MapTownState.Locked;

            return town.Id == CurrentTownId ? MapTownState.Current : MapTownState.Unlocked;
        }

        private Sprite IconFor(string townId)
        {
            foreach (var entry in _townIcons)
                if (entry != null && entry.TownId == townId && entry.Icon != null)
                    return entry.Icon;

            return _defaultTownIcon;
        }

        /// <summary>
        /// A town's normalized map position, falling back to an evenly spaced slot along the middle
        /// of the map when none is authored — an unpositioned town still shows up and is clickable.
        /// </summary>
        private static Vector2 NormalizedPosition(TownConfig town)
        {
            // JsonUtility materialises missing [Serializable] fields instead of leaving them null,
            // so an unauthored MapPosition arrives as (0,0) — treat that exact corner as "unset".
            if (town.MapPosition != null && (town.MapPosition.X != 0f || town.MapPosition.Y != 0f))
                return new Vector2(town.MapPosition.X, town.MapPosition.Y);

            var towns = TownCatalog.All;
            int index = 0;
            for (int i = 0; i < towns.Count; i++)
                if (towns[i] == town) { index = i; break; }

            return new Vector2((index + 1f) / (towns.Count + 1f), 0.5f);
        }

        // --- Selection ---

        private void SelectTown(string townId)
        {
            _selectedTownId = townId;

            foreach (var pair in _nodesByTown)
                pair.Value.SetSelected(pair.Key == townId);

            var town = TownCatalog.Get(townId);
            bool unlocked = town != null && TownProgressService.IsTownUnlocked(townId);

            if (_townTitle != null)
                _townTitle.text = town != null ? town.Name : "—";

            if (_townSubtitle != null)
                _townSubtitle.text = town == null ? string.Empty
                    : unlocked ? MapTownNodeView.LevelRangeLabel(town)
                    : TownProgressService.LockReason(townId);

            // Travel is offered only to a reachable town the player isn't already standing in.
            if (_travelButton != null)
                _travelButton.gameObject.SetActive(unlocked && townId != CurrentTownId);
        }

        // --- Actions ---

        /// <summary>Travels to the selected town: swaps the hub's town and reloads its scene.</summary>
        public void TravelToSelectedTown()
        {
            var town = TownCatalog.Get(_selectedTownId);
            if (town == null)
            {
                Debug.LogWarning("[MapWindow] Travel pressed with no town selected.");
                return;
            }

            if (!TownProgressService.IsTownUnlocked(town.Id))
            {
                Debug.LogWarning($"[MapWindow] Travel to locked town '{town.Id}' ignored.");
                return;
            }

            // Travelling is not a run — clear any stale run context so the hub returns to this town.
            RunContext.Clear();
            TownContext.Enter(town.Id);

            // The environment prefab is the only town-specific thing in the hub, and towns share one
            // scene by default — so reloading it would tear down the player, HUD and every window
            // just to swap that prefab. Swap it in place instead, and only load when the destination
            // town actually declares a different scene.
            string scene = TownCatalog.SceneFor(town.Id);
            if (TownBuilder.Active != null && scene == SceneManager.GetActiveScene().name)
            {
                TownBuilder.Active.Rebuild();
                Close();
                return;
            }

            SceneLoader.Load(scene);
        }
    }

    /// <summary>One town's map icon, assigned in the inspector and matched by town id.</summary>
    [Serializable]
    public class MapTownIcon
    {
        [TownId] public string TownId;
        public Sprite Icon;
    }

    /// <summary>
    /// Code-built pieces of the map: the lines between towns, and the fallback town node used when
    /// no node prefab is assigned. Text creation is shared with the quest window's row factory.
    /// </summary>
    internal static class MapUI
    {
        private static readonly Color LinkOpenColor = new Color(0.8f, 0.75f, 0.5f, 0.7f);
        private static readonly Color LinkLockedColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);

        /// <summary>
        /// A bare town node for projects that haven't authored a node prefab yet: a tinted panel with
        /// an icon slot and the name/level-range labels the prefab would otherwise supply.
        /// </summary>
        public static MapTownNodeView BuildFallbackNode(RectTransform mapRoot, Vector2 size)
        {
            var go = new GameObject("TownNode",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(mapRoot, false);
            ((RectTransform)go.transform).sizeDelta = size;

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconGO.transform.SetParent(go.transform, false);
            var iconLayout = iconGO.GetComponent<LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = Mathf.Max(16f, size.y - 8f);
            var icon = iconGO.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var textCol = new GameObject("Text",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textCol.transform.SetParent(go.transform, false);
            textCol.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var textLayout = textCol.GetComponent<VerticalLayoutGroup>();
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandHeight = false;
            textLayout.childAlignment = TextAnchor.MiddleLeft;

            var nameText = QuestRowUI.CreateText(textCol.transform, "Name", 14f, Color.white);
            nameText.fontStyle = FontStyles.Bold;

            var levelText = QuestRowUI.CreateText(textCol.transform, "LevelRange", 11f, new Color(0.8f, 0.8f, 0.8f));
            var lockText = QuestRowUI.CreateText(textCol.transform, "LockReason", 11f, new Color(1f, 0.8f, 0.4f));

            var view = go.AddComponent<MapTownNodeView>();
            view.InitializeRuntime(go.GetComponent<Image>(), icon, nameText, levelText, lockText,
                                   go.GetComponent<Button>());
            return view;
        }

        /// <summary>
        /// A straight line between two normalized map points, used to show which town unlocks which.
        /// Sized in pixels off <paramref name="mapSize"/> because a rotated rect can't be expressed
        /// with anchors alone.
        /// </summary>
        public static GameObject BuildLink(RectTransform mapRoot, Vector2 fromNormalized,
                                           Vector2 toNormalized, Vector2 mapSize, float thickness,
                                           bool open)
        {
            var go = new GameObject("MapLink", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(mapRoot, false);

            var image = go.GetComponent<Image>();
            image.color = open ? LinkOpenColor : LinkLockedColor;
            image.raycastTarget = false;

            Vector2 from = Vector2.Scale(fromNormalized, mapSize);
            Vector2 to = Vector2.Scale(toNormalized, mapSize);
            Vector2 delta = to - from;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = (fromNormalized + toNormalized) * 0.5f;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(delta.magnitude, thickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            return go;
        }
    }
}
