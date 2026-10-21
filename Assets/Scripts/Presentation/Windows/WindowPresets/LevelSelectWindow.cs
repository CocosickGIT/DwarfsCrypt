using System.Collections.Generic;
using Core.Afk;
using Core.Scenes;
using DwarfsCrypt.Domain.Towns;
using DwarfsCrypt.Presentation.Towns;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// The town's zone gate: which of the current town's zones to play, plus AFK farming on the
    /// selected zone. The zone list is built on every open from the current town's
    /// <see cref="DwarfsCrypt.Domain.Towns.TownConfig.Zones"/>, so one gate serves every town and a
    /// new town's zones are a towns.json edit — the same way <see cref="MapWindow"/> builds its town
    /// nodes. With no zone icon prefab assigned it falls back to the hand-placed <see cref="icons"/>.
    ///
    /// This is the *inside a town* half of level selection; travelling between towns is
    /// <see cref="MapWindow"/>'s job and this window deliberately doesn't offer it.
    /// </summary>
    public class LevelSelectWindow : WindowBase
    {
        [Tooltip("Fallback town id for testing this gate scene directly in the editor. At runtime the " +
                 "active town comes from TownContext (set on entry); this is only used when that is unset. " +
                 "Picked from Resources/Towns/towns.json.")]
        [TownId] [SerializeField] private string townId = "town_ironhold";

        [Header("Zones")]
        [Tooltip("Prefab instantiated once per zone of the current town. Leave empty to keep using " +
                 "the hand-placed icons below instead.")]
        [SerializeField] private LevelIcon zoneIconPrefab;

        [Tooltip("Parent the zone icons spawn under (give it a layout group). Defaults to this transform.")]
        [SerializeField] private Transform zoneListRoot;

        [Tooltip("Hand-placed level icons, used only when no Zone Icon Prefab is assigned. Their " +
                 "scenes are matched back to the town's zones so clears are still recorded.")]
        [SerializeField] private List<LevelIcon> icons = new List<LevelIcon>();

        [Tooltip("Button that enters the selected level. Stays disabled until a level is selected.")]
        [SerializeField] private Button enterButton;

        [Header("AFK Farming")]
        [Tooltip("Button that runs an AFK farming simulation on the selected level instead of entering " +
                 "it. Stays hidden until a level that supports AFK farming is selected.")]
        [SerializeField] private Button afkButton;

        [Tooltip("How many mobs a single AFK run fights through.")]
        [SerializeField, Min(1)] private int afkMobCount = 10;

        [Tooltip("Constant run duration (seconds) the fight simulation is bounded by.")]
        [SerializeField, Min(1f)] private float afkDurationSeconds = 60f;

        [Tooltip("Optional. Shows the AFK run outcome line (e.g. 'killed 7/10 mobs').")]
        [SerializeField] private TMP_Text afkResultText;

        [Tooltip("Optional. Reward screen reused to display the loot earned by an AFK run.")]
        [SerializeField] private RewardWindow rewardWindow;

        private LevelIcon selectedIcon;
        private readonly List<LevelIcon> spawnedIcons = new List<LevelIcon>();

        public override WindowType Type => WindowType.LevelSelect;

        /// <summary>The town this gate acts for: the hub's current town, or the serialized fallback
        /// when entered directly (e.g. testing the scene in isolation).</summary>
        private string ActiveTownId =>
            !string.IsNullOrEmpty(Core.Towns.TownContext.CurrentTownId)
                ? Core.Towns.TownContext.CurrentTownId
                : townId;

        private void Awake()
        {
            if (afkButton != null)
                afkButton.onClick.AddListener(StartAfkRun);
        }

        protected override void OnOpen()
        {
            // Start with no selection; the player must pick a zone before the Enter button works.
            // Cleared before rebuilding so the selection can't survive as a reference to a
            // destroyed icon.
            ClearSelection();

            // Rebuilt on every open rather than once in Awake: the hub can now swap towns without a
            // scene reload (TownBuilder.Rebuild), so the town this gate acts for may have changed
            // since the last time it was shown.
            BuildZoneIcons();
        }

        /// <summary>
        /// Fills the gate with the current town's zones. With a zone icon prefab assigned this spawns
        /// one icon per zone; without one it adopts the hand-placed icons, resolving each one's zone
        /// from its scene so clears still land on the right zone id.
        /// </summary>
        private void BuildZoneIcons()
        {
            var town = Core.Towns.TownCatalog.Get(ActiveTownId);
            var zones = town?.Zones;

            if (zoneIconPrefab == null)
            {
                foreach (var icon in icons)
                    if (icon != null)
                        icon.Init(this, ResolveZoneId(zones, icon.SceneName));
                return;
            }

            foreach (var spawned in spawnedIcons)
                if (spawned != null) Destroy(spawned.gameObject);
            spawnedIcons.Clear();

            if (zones == null || zones.Count == 0)
            {
                Debug.LogWarning($"[LevelSelectWindow] Town '{ActiveTownId}' has no zones authored in " +
                                 "towns.json; the gate is empty.", this);
                return;
            }

            var root = zoneListRoot != null ? zoneListRoot : transform;
            foreach (var zone in zones)
            {
                if (zone == null || string.IsNullOrEmpty(zone.Scene)) continue;

                // worldPositionStays: false — the UI overload otherwise rewrites localScale to
                // compensate for the canvas scale (same reason MapWindow spawns its nodes this way).
                var icon = Instantiate(zoneIconPrefab, root, false);
                icon.Bind(zone, this);
                spawnedIcons.Add(icon);
            }
        }

        /// <summary>Called by an icon when it is clicked. Highlights it and remembers it as the target.</summary>
        public void Select(LevelIcon icon)
        {
            if (selectedIcon != null)
                selectedIcon.SetSelected(false);

            selectedIcon = icon;
            selectedIcon.SetSelected(true);

            if (enterButton != null)
                enterButton.gameObject.SetActive(true);

            // AFK Run is only offered on levels that have a mob configured to farm.
            if (afkButton != null)
                afkButton.gameObject.SetActive(icon.SupportsAfk);

            if (afkResultText != null)
                afkResultText.text = string.Empty;
        }

        private void ClearSelection()
        {
            if (selectedIcon != null)
                selectedIcon.SetSelected(false);

            selectedIcon = null;

            if (enterButton != null)
                enterButton.gameObject.SetActive(false);

            if (afkButton != null)
                afkButton.gameObject.SetActive(false);

            if (afkResultText != null)
                afkResultText.text = string.Empty;
        }

        /// <summary>Wired to the EnterButton's onClick. Loads the scene bound to the selected icon.</summary>
        public void EnterSelectedZone()
        {
            if (selectedIcon == null)
            {
                Debug.LogWarning("[LevelSelectWindow] Enter pressed with no icon selected.");
                return;
            }

            // The icon carries the zone it was built from, so the clear lands on the right zone id
            // even when several zones share a scene.
            Core.Towns.RunContext.Begin(ActiveTownId, selectedIcon.SceneName, selectedIcon.ZoneId);
            SceneLoader.Load(selectedIcon.SceneName);
        }

        /// <summary>
        /// Id of the zone that loads <paramref name="sceneName"/>, or null when no zone does. Only
        /// needed for hand-placed icons, which carry a scene but no zone; icons built from the config
        /// already know their zone. Ambiguous when two zones share a scene — the first wins, which is
        /// exactly why the config-driven path is preferred.
        /// </summary>
        private static string ResolveZoneId(List<TownZone> zones, string sceneName)
        {
            if (zones == null || string.IsNullOrEmpty(sceneName)) return null;

            foreach (var zone in zones)
                if (zone != null && zone.Scene == sceneName)
                    return zone.Id;

            return null;
        }

        /// <summary>
        /// Wired to the AfkButton's onClick. Runs a fight simulation against the selected level's mob
        /// (a constant-duration run over <see cref="afkMobCount"/> mobs), applies the loot to the
        /// profile, and shows the outcome — no scene load, the player stays in town.
        /// </summary>
        public void StartAfkRun()
        {
            if (selectedIcon == null)
            {
                Debug.LogWarning("[LevelSelectWindow] AFK Run pressed with no icon selected.");
                return;
            }

            if (!selectedIcon.SupportsAfk)
            {
                Debug.LogWarning("[LevelSelectWindow] Selected level has no AFK mob configured.");
                return;
            }

            var outcome = AfkRunService.Run(selectedIcon.AfkEnemyConfigPath, afkMobCount, afkDurationSeconds);
            if (outcome == null)
                return;

            if (afkResultText != null)
            {
                var r = outcome.Result;
                string status = r.ClearedAll
                    ? "cleared the camp!"
                    : r.PlayerSurvived ? "ran out of time" : "fell in battle";
                afkResultText.text = $"AFK Run: killed {r.Kills}/{r.MobCount} mobs — {status}";
            }

            // Reuse the reward screen to show the spoils (already applied to the profile).
            if (rewardWindow != null)
                rewardWindow.Show(outcome.Rewards);
        }
    }
}
