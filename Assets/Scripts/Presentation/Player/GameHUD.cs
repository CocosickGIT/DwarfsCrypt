using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DwarfsCrypt.Presentation.Windows;
using Presentation.WindowService;

namespace DwarfsCrypt.Presentation.Player
{
    public class GameHUD : WindowBase
    {
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private Button _dashButton;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _skillButton;
        [SerializeField] private Button _interactButton;

        [Header("Consumables")]
        [Tooltip("Dedicated button that consumes one health potion.")]
        [SerializeField] private Button _healthPotionButton;
        [Tooltip("Optional label showing how many health potions are left (e.g. \"x5\").")]
        [SerializeField] private TMP_Text _healthPotionCountLabel;

        [Header("Cooldown overlays")]
        [Tooltip("Buff-icon style cooldown view for the dash button.")]
        [SerializeField] private CooldownButtonView _dashCooldownView;
        [Tooltip("Cooldown view for the skill button.")]
        [SerializeField] private CooldownButtonView _skillCooldownView;

        [Header("Skill mana cost")]
        [Tooltip("Optional label showing the skill's mana cost (e.g. \"10\"). Hidden for free skills.")]
        [SerializeField] private TMP_Text _skillManaLabel;

        [Header("Menu")]
        [SerializeField] private Button _menuButton;
        [Tooltip("Window service used to open the Menu window when the menu button is pressed.")]
        [SerializeField] private WindowService _windowService;

        public override WindowType Type => WindowType.GameHUD;
        public VirtualJoystick Joystick => _joystick;
        public WindowService WindowService => _windowService;

        public event Action OnDashPressed;
        public event Action OnAttackPressed;
        public event Action OnSkillPressed;
        public event Action OnInteractPressed;
        public event Action OnHealthPotionPressed;
        public event Action OnMenuPressed;

        private void Awake()
        {
            _dashButton.onClick.AddListener(() => OnDashPressed?.Invoke());
            _attackButton.onClick.AddListener(() => OnAttackPressed?.Invoke());
            _skillButton.onClick.AddListener(() => OnSkillPressed?.Invoke());
            _interactButton.onClick.AddListener(() => OnInteractPressed?.Invoke());
            _interactButton.gameObject.SetActive(false);

            if (_healthPotionButton != null)
                _healthPotionButton.onClick.AddListener(() => OnHealthPotionPressed?.Invoke());

            if (_menuButton != null)
                _menuButton.onClick.AddListener(OpenMenu);
        }

        public void SetInteractVisible(bool visible) =>
            _interactButton.gameObject.SetActive(visible);

        /// <summary>Start the dash button's cooldown sweep (icon dims, fill goes 0 → 1).</summary>
        public void StartDashCooldown(float duration) =>
            _dashCooldownView?.Play(duration);

        /// <summary>Start the skill button's cooldown sweep (icon dims, fill goes 0 → 1).</summary>
        public void StartSkillCooldown(float duration) =>
            _skillCooldownView?.Play(duration);

        /// <summary>
        /// Set the skill's mana-cost label. A cost of 0 or less hides the label (free skills).
        /// </summary>
        public void SetSkillManaCost(int cost)
        {
            if (_skillManaLabel == null) return;

            _skillManaLabel.gameObject.SetActive(cost > 0);
            if (cost > 0) _skillManaLabel.text = cost.ToString();
        }

        /// <summary>
        /// Refresh the health-potion button: updates the count label and disables the button
        /// (greyed/non-interactable) when the player has none left.
        /// </summary>
        public void SetHealthPotionCount(int count)
        {
            if (_healthPotionCountLabel != null)
                _healthPotionCountLabel.text = $"x{count}";

            if (_healthPotionButton != null)
                _healthPotionButton.interactable = count > 0;
        }

        /// <summary>
        /// Hide the whole on-screen HUD. The GameHUD component sits on an empty logic object whose
        /// parent (the HUD canvas) holds the joystick/buttons as siblings, so closing this object
        /// alone leaves the visuals on screen — deactivate the canvas root instead.
        /// </summary>
        public void Hide()
        {
            var root = transform.parent != null ? transform.parent.gameObject : gameObject;
            root.SetActive(false);
        }

        private void OpenMenu()
        {
            OnMenuPressed?.Invoke();

            if (_windowService != null)
                _windowService.Open(WindowType.Menu);
            else
                Debug.LogWarning("[GameHUD] Menu button pressed but no WindowService is assigned.");
        }
    }
}
