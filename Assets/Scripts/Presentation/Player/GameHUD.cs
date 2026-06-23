using System;
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
        [SerializeField] private Button _skill1Button;
        [SerializeField] private Button _skill2Button;
        [SerializeField] private Button _skill3Button;
        [SerializeField] private Button _interactButton;

        [Header("Menu")]
        [SerializeField] private Button _menuButton;
        [Tooltip("Window service used to open the Menu window when the menu button is pressed.")]
        [SerializeField] private WindowService _windowService;

        public override WindowType Type => WindowType.GameHUD;
        public VirtualJoystick Joystick => _joystick;
        public WindowService WindowService => _windowService;

        public event Action OnDashPressed;
        public event Action OnAttackPressed;
        public event Action OnSkill1Pressed;
        public event Action OnSkill2Pressed;
        public event Action OnSkill3Pressed;
        public event Action OnInteractPressed;
        public event Action OnMenuPressed;

        private void Awake()
        {
            _dashButton.onClick.AddListener(() => OnDashPressed?.Invoke());
            _attackButton.onClick.AddListener(() => OnAttackPressed?.Invoke());
            _skill1Button.onClick.AddListener(() => OnSkill1Pressed?.Invoke());
            _skill2Button.onClick.AddListener(() => OnSkill2Pressed?.Invoke());
            _skill3Button.onClick.AddListener(() => OnSkill3Pressed?.Invoke());
            _interactButton.onClick.AddListener(() => OnInteractPressed?.Invoke());
            _interactButton.gameObject.SetActive(false);

            if (_menuButton != null)
                _menuButton.onClick.AddListener(OpenMenu);
        }

        public void SetInteractVisible(bool visible) =>
            _interactButton.gameObject.SetActive(visible);

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
