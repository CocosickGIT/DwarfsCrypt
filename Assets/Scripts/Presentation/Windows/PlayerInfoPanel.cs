using Core.Player;
using DwarfsCrypt.Domain.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// Town HUD element showing the player's name, level and progress toward the next level.
    /// Build the UI however you like in the editor and assign the references below; this component
    /// just fills them from the persisted <see cref="PlayerProfile"/>. All references are optional,
    /// so you can wire only the labels you care about.
    /// </summary>
    public class PlayerInfoPanel : MonoBehaviour
    {
        [Header("UI References (assign in the inspector)")]
        [Tooltip("Label that shows the player's name.")]
        [SerializeField] private TMP_Text _nameText;
        [Tooltip("Label that shows the current level, e.g. \"Level 5\".")]
        [SerializeField] private TMP_Text _levelText;
        [Tooltip("Round exp progress image. Set the Image's Type = Filled, Fill Method = Radial 360; " +
                 "its Fill Amount is set to the exp fraction (0..1).")]
        [SerializeField] private Image _expFill;

        [Header("Formatting")]
        [Tooltip("Shown when the profile has no name.")]
        [SerializeField] private string _fallbackName = "Adventurer";

        // Refresh when the panel becomes visible (returning to Town reloads the scene).
        private void OnEnable() => Refresh();

        /// <summary>Pull the latest name/level/exp from the active profile into the assigned UI.</summary>
        public void Refresh()
        {
            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;
            if (profile == null) return;

            int needed = LevelCurve.ExpForLevel(profile.Level);
            float fill = needed > 0 ? Mathf.Clamp01((float)profile.Exp / needed) : 0f;

            if (_nameText != null)
                _nameText.text = string.IsNullOrEmpty(profile.Name) ? _fallbackName : profile.Name;
            if (_levelText != null)
                _levelText.text = $"{profile.Level}";
            if (_expFill != null)
                _expFill.fillAmount = fill;
        }
    }
}
