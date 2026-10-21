using System;
using Core.Towns;
using DwarfsCrypt.Domain.Towns;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Towns
{
    /// <summary>How a town reads on the world map. Drives the node's tint and its optional
    /// locked/current decorations.</summary>
    public enum MapTownState
    {
        Locked,
        Unlocked,
        Current
    }

    /// <summary>
    /// One town node on the world map. Put this on a prefab, assign whichever pieces that prefab
    /// actually has — every reference is optional, a node with only a name label works — and hand the
    /// prefab to the map window. The window instantiates one per town in towns.json and calls
    /// <see cref="Bind"/>; nothing here looks a town up on its own, so the same prefab serves every
    /// town.
    ///
    /// When the map window has no prefab assigned it builds a plain node in code and wires it through
    /// <see cref="InitializeRuntime"/>, so the window only ever deals with this one component.
    /// </summary>
    public class MapTownNodeView : MonoBehaviour
    {
        [Header("Art")]
        [Tooltip("Background/frame of the node. Tinted by state when 'Tint Background' is on.")]
        [SerializeField] private Image _background;

        [Tooltip("The town's icon. Hidden automatically when the map window has no sprite for this town.")]
        [SerializeField] private Image _icon;

        [Tooltip("Optional. Shown only while the town is locked, e.g. a padlock or a dark scrim.")]
        [SerializeField] private GameObject _lockedOverlay;

        [Tooltip("Optional. Shown only on the town the player is standing in, e.g. a 'you are here' pin.")]
        [SerializeField] private GameObject _currentMarker;

        [Tooltip("Optional. Shown only while this node is the selected one. When left empty the " +
                 "selection instead brightens the background tint.")]
        [SerializeField] private GameObject _selectionHighlight;

        [Header("Labels")]
        [Tooltip("Town name, e.g. 'Ironhold'.")]
        [SerializeField] private TMP_Text _nameText;

        [Tooltip("Mob level range from the town's LevelBand, e.g. 'Lv 1-15'. Blank when unauthored.")]
        [SerializeField] private TMP_Text _levelRangeText;

        [Tooltip("Optional. What the player must do to reach this town, e.g. 'Clear all zones in " +
                 "Ironhold (1/2)'. Hidden for towns that can already be travelled to.")]
        [SerializeField] private TMP_Text _lockReasonText;

        [Header("Input")]
        [Tooltip("Button that selects this town. Found on this object when left empty.")]
        [SerializeField] private Button _button;

        [Header("State tint")]
        [Tooltip("Tint the background by state. Turn this off for a prefab whose art already says " +
                 "everything — the node then relies on the locked overlay / current marker instead.")]
        [SerializeField] private bool _tintBackground = true;

        [SerializeField] private Color _currentColor = new Color(0.15f, 0.5f, 0.2f, 0.95f);
        [SerializeField] private Color _unlockedColor = new Color(0.15f, 0.3f, 0.55f, 0.95f);
        [SerializeField] private Color _lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        [Tooltip("Multiplied into the icon and label colours while the town is locked, so a locked " +
                 "node reads as dimmed without throwing away the colours authored on the prefab.")]
        [SerializeField] private Color _lockedTint = new Color(1f, 1f, 1f, 0.55f);

        [Tooltip("How much brighter the selected node's background gets (ignored when a Selection " +
                 "Highlight object is assigned).")]
        [SerializeField, Range(0f, 1f)] private float _selectedBrightness = 0.2f;

        /// <summary>Id of the town this node was last bound to (null before the first bind).</summary>
        public string TownId { get; private set; }

        private Color _stateColor;
        private Color _baseIconColor = Color.white;
        private Color _baseNameColor = Color.white;
        private Color _baseLevelColor = Color.white;
        private Color _baseLockReasonColor = Color.white;
        private bool _baseColorsCached;

        /// <summary>
        /// Fill the node in for one town. Safe to call again on the same instance — the click
        /// listener is rebuilt rather than stacked, so reopening the map doesn't multiply selections.
        /// </summary>
        /// <param name="icon">Sprite for this town, or null to hide the icon slot.</param>
        public void Bind(TownConfig town, Sprite icon, MapTownState state, Action<string> onClick)
        {
            CacheBaseColors();

            TownId = town != null ? town.Id : null;
            gameObject.name = string.IsNullOrEmpty(TownId) ? "TownNode" : $"Town_{TownId}";

            bool locked = state == MapTownState.Locked;
            Color tint = locked ? _lockedTint : Color.white;

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.color = _baseIconColor * tint;
                // Deactivated rather than just blanked so an icon slot inside a layout group
                // collapses instead of leaving a hole for a town with no sprite.
                _icon.gameObject.SetActive(icon != null);
            }

            Apply(_nameText, town != null ? town.Name : "—", _baseNameColor * tint);
            Apply(_levelRangeText, LevelRangeLabel(town), _baseLevelColor * tint);

            // The unlock requirement is the only thing a locked node adds; for a town the player can
            // already travel to there is nothing to say, so the slot collapses.
            if (_lockReasonText != null)
            {
                _lockReasonText.text = locked && town != null
                    ? TownProgressService.LockReason(town.Id)
                    : string.Empty;
                _lockReasonText.color = _baseLockReasonColor;
                _lockReasonText.gameObject.SetActive(locked);
            }

            if (_lockedOverlay != null) _lockedOverlay.SetActive(locked);
            if (_currentMarker != null) _currentMarker.SetActive(state == MapTownState.Current);

            _stateColor = state == MapTownState.Current ? _currentColor
                        : state == MapTownState.Unlocked ? _unlockedColor
                        : _lockedColor;

            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
            {
                if (_background != null) _button.targetGraphic = _background;

                string id = TownId;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onClick?.Invoke(id));
            }

            SetSelected(false);
        }

        /// <summary>Highlights this node as the one whose zones the window is listing.</summary>
        public void SetSelected(bool selected)
        {
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(selected);

            if (!_tintBackground || _background == null) return;

            // With a highlight object the background stays on its plain state colour. Without one,
            // selection brightens that colour rather than replacing it, so a locked node still
            // reads as locked while selected.
            bool brighten = selected && _selectionHighlight == null;

            _background.color = brighten
                ? new Color(_stateColor.r + _selectedBrightness,
                            _stateColor.g + _selectedBrightness,
                            _stateColor.b + _selectedBrightness,
                            _stateColor.a)
                : _stateColor;
        }

        /// <summary>Level band as authored in towns.json, e.g. "Lv 1-15". Empty when unauthored.</summary>
        public static string LevelRangeLabel(TownConfig town) =>
            town?.LevelBand != null ? $"Lv {town.LevelBand.Min}-{town.LevelBand.Max}" : string.Empty;

        /// <summary>
        /// Wires up a node built in code (the map window's fallback when no prefab is assigned).
        /// Separate from the serialized fields because AddComponent runs Awake before the caller can
        /// hand over its references.
        /// </summary>
        internal void InitializeRuntime(Image background, Image icon, TMP_Text name,
                                        TMP_Text levelRange, TMP_Text lockReason, Button button)
        {
            _background = background;
            _icon = icon;
            _nameText = name;
            _levelRangeText = levelRange;
            _lockReasonText = lockReason;
            _button = button;
            _tintBackground = true;
            _baseColorsCached = false;
        }

        private void CacheBaseColors()
        {
            if (_baseColorsCached) return;
            _baseColorsCached = true;

            if (_icon != null) _baseIconColor = _icon.color;
            if (_nameText != null) _baseNameColor = _nameText.color;
            if (_levelRangeText != null) _baseLevelColor = _levelRangeText.color;
            if (_lockReasonText != null) _baseLockReasonColor = _lockReasonText.color;
        }

        private static void Apply(TMP_Text label, string text, Color color)
        {
            if (label == null) return;

            label.text = text;
            label.color = color;
        }
    }
}
