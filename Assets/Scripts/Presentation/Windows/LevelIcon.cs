using DwarfsCrypt.Domain.Towns;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// A single selectable zone in the <see cref="LevelSelectWindow"/>: the scene it enters, the zone
    /// id a clear is recorded under, and a highlight frame shown while selected.
    ///
    /// Normally this is a prefab the window instantiates once per zone in the current town's
    /// <see cref="TownConfig.Zones"/> and fills in via <see cref="Bind"/> — nothing here looks a town
    /// up on its own, so one prefab serves every town. Icons hand-placed in a town prefab still work:
    /// the window wires them through <see cref="Init"/> instead, matching their serialized scene back
    /// to a zone so clears are still recorded.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LevelIcon : MonoBehaviour
    {
        [Tooltip("Scene to load when this icon is selected and the player enters. Must match a scene " +
                 "in Build Settings. Overwritten by Bind() when the window builds icons from the " +
                 "town's zones — set it by hand only for a hand-placed icon.")]
        [SerializeField] private string sceneName;

        [Tooltip("Object shown only while this icon is the selected one (e.g. a highlight frame/glow). Optional.")]
        [SerializeField] private GameObject selectedHighlight;

        [Header("AFK Farming")]
        [Tooltip("Resources path (no extension) of the mob this zone farms in an AFK run, e.g. " +
                 "'Characters/example_enemy'. Leave empty to hide the AFK Run button for this zone. " +
                 "Overwritten by Bind() for icons built from the town's zones.")]
        [SerializeField] private string afkEnemyConfigPath;

        [Header("Labels (optional)")]
        [Tooltip("Zone name from towns.json, e.g. 'Forest Edge'. Filled by Bind(); left alone for " +
                 "hand-placed icons that already have their own artwork.")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("Suggested level from the zone's MinLevel, e.g. 'Lv 5+'. Filled by Bind().")]
        [SerializeField] private TMP_Text minLevelText;

        private Button button;
        private LevelSelectWindow owner;

        /// <summary>
        /// Id of the town zone this icon plays, as authored in towns.json. This — not the scene name —
        /// is what a clear is recorded under, so two zones may share a scene. Null when the icon isn't
        /// one of the current town's zones; such a run can be played but never counts toward a town unlock.
        /// </summary>
        public string ZoneId { get; private set; }

        public string SceneName => sceneName;

        /// <summary>Resources path of the mob simulated in this zone's AFK run (empty = AFK disabled).</summary>
        public string AfkEnemyConfigPath => afkEnemyConfigPath;

        /// <summary>True when this zone has a mob configured for AFK farming.</summary>
        public bool SupportsAfk => !string.IsNullOrEmpty(afkEnemyConfigPath);

        /// <summary>
        /// Fill this icon in for one of the current town's zones. Safe to call again on the same
        /// instance — the click listener is rebuilt rather than stacked.
        /// </summary>
        public void Bind(TownZone zone, LevelSelectWindow window)
        {
            ZoneId = zone.Id;
            sceneName = zone.Scene;
            afkEnemyConfigPath = zone.AfkEnemyConfigPath;

            gameObject.name = string.IsNullOrEmpty(zone.Id) ? "ZoneIcon" : $"Zone_{zone.Id}";

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(zone.Name) ? zone.Id : zone.Name;

            if (minLevelText != null)
                minLevelText.text = zone.MinLevel > 0 ? $"Lv {zone.MinLevel}+" : string.Empty;

            Init(window, zone.Id);
        }

        /// <summary>
        /// Wire a hand-placed icon into the window. <paramref name="zoneId"/> is the zone the window
        /// matched to this icon's scene, or null when the scene isn't one of the town's zones.
        /// </summary>
        public void Init(LevelSelectWindow window, string zoneId = null)
        {
            owner = window;
            ZoneId = zoneId;

            button = GetComponent<Button>();
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null)
                selectedHighlight.SetActive(selected);
        }

        private void OnClicked()
        {
            owner.Select(this);

            // Drop the EventSystem's focus so the Button's own "Selected" color tint doesn't
            // act as the selection visual. The persistent selectedHighlight frame is the single
            // source of truth and stays on until another icon is clicked.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
