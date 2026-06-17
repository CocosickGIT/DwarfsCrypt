using Core.Player;
using Core.Rewards;
using Core.Scenes;
using Presentation.WindowService;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    public class MenuWindow : WindowBase
    {
        [Header("References")]
        [SerializeField] private WindowService _windowService;

        [Header("Controls")]
        [Tooltip("Leaves the level: saves progress, shows the run rewards, then returns to Town.")]
        [SerializeField] private Button _exitLevelButton;
        [Tooltip("Wipes all progress back to the starter profile.")]
        [SerializeField] private Button _resetProgressButton;

        public override WindowType Type => WindowType.Menu;

        private void Awake()
        {
            if (_exitLevelButton != null)
                _exitLevelButton.onClick.AddListener(ExitLevel);
            if (_resetProgressButton != null)
                _resetProgressButton.onClick.AddListener(ResetProgress);
        }

        private void ExitLevel()
        {
            // Rewards were already applied to the profile on each kill; just persist and summarize.
            PlayerProfileService.Save();

            var rewardWindow = _windowService != null
                ? _windowService.GetWindow<RewardWindow>(WindowType.Reward)
                : null;

            if (rewardWindow != null)
            {
                rewardWindow.Closed -= OnRewardClosed;
                rewardWindow.Closed += OnRewardClosed;
                Close();
                rewardWindow.Show(RunRewards.Summary);
            }
            else
            {
                // No reward UI wired — leave directly.
                OnRewardClosed();
            }
        }

        private void OnRewardClosed()
        {
            var rewardWindow = _windowService != null
                ? _windowService.GetWindow<RewardWindow>(WindowType.Reward)
                : null;
            if (rewardWindow != null)
                rewardWindow.Closed -= OnRewardClosed;

            RunRewards.Reset();
            SceneLoader.Load(SceneNames.Town);
        }

        private void ResetProgress()
        {
            PlayerProfileService.ResetToStarter();
            Debug.Log("[MenuWindow] Player progress reset to starter profile.");
        }
    }
}
