using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DwarfsCrypt.Presentation.Player
{
    public class SkillController : MonoBehaviour
    {
        [SerializeField] private GameHUD _hud;

        public event Action OnSkill1Used;
        public event Action OnSkill2Used;
        public event Action OnSkill3Used;

        private PlayerInputActions _input;

        private void Awake()
        {
            _input = new PlayerInputActions();
            _input.Skill1.performed += OnSkill1Performed;
            _input.Skill2.performed += OnSkill2Performed;
            _input.Skill3.performed += OnSkill3Performed;
            _input.Enable();
        }

        private void Start()
        {
            if (_hud != null)
            {
                _hud.OnSkill1Pressed += UseSkill1;
                _hud.OnSkill2Pressed += UseSkill2;
                _hud.OnSkill3Pressed += UseSkill3;
            }
        }

        private void OnDestroy()
        {
            _input.Skill1.performed -= OnSkill1Performed;
            _input.Skill2.performed -= OnSkill2Performed;
            _input.Skill3.performed -= OnSkill3Performed;
            _input.Disable();
            // _input.Dispose();

            if (_hud != null)
            {
                _hud.OnSkill1Pressed -= UseSkill1;
                _hud.OnSkill2Pressed -= UseSkill2;
                _hud.OnSkill3Pressed -= UseSkill3;
            }
        }

        private void OnSkill1Performed(InputAction.CallbackContext _) => UseSkill1();
        private void OnSkill2Performed(InputAction.CallbackContext _) => UseSkill2();
        private void OnSkill3Performed(InputAction.CallbackContext _) => UseSkill3();

        private void UseSkill1() => OnSkill1Used?.Invoke();
        private void UseSkill2() => OnSkill2Used?.Invoke();
        private void UseSkill3() => OnSkill3Used?.Invoke();
    }
}
