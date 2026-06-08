using System;
using UnityEngine;
using UnityEngine.UI;
using DwarfsCrypt.Presentation.Windows;

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

        public override WindowType Type => WindowType.GameHUD;
        public VirtualJoystick Joystick => _joystick;

        public event Action OnDashPressed;
        public event Action OnAttackPressed;
        public event Action OnSkill1Pressed;
        public event Action OnSkill2Pressed;
        public event Action OnSkill3Pressed;
        public event Action OnInteractPressed;

        private void Awake()
        {
            _dashButton.onClick.AddListener(() => OnDashPressed?.Invoke());
            _attackButton.onClick.AddListener(() => OnAttackPressed?.Invoke());
            _skill1Button.onClick.AddListener(() => OnSkill1Pressed?.Invoke());
            _skill2Button.onClick.AddListener(() => OnSkill2Pressed?.Invoke());
            _skill3Button.onClick.AddListener(() => OnSkill3Pressed?.Invoke());
            _interactButton.onClick.AddListener(() => OnInteractPressed?.Invoke());
            _interactButton.gameObject.SetActive(false);
        }

        public void SetInteractVisible(bool visible) =>
            _interactButton.gameObject.SetActive(visible);
    }
}
