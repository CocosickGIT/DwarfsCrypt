using System.Collections.Generic;
using Core.Afk;
using Core.Scenes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    public class LevelSelectWindow : WindowBase
    {
        [Tooltip("All level icons, in display order. Icon 0 -> its scene, icon 1 -> its scene, etc.")]
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

        public override WindowType Type => WindowType.LevelSelect;

        private void Awake()
        {
            foreach (var icon in icons)
                icon.Init(this);

            if (afkButton != null)
                afkButton.onClick.AddListener(StartAfkRun);
        }

        protected override void OnOpen()
        {
            // Start with no selection; the player must pick a level before the Enter button works.
            ClearSelection();
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

            SceneLoader.Load(selectedIcon.SceneName);
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
