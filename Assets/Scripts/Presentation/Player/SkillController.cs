using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DwarfsCrypt.Presentation.Player
{
    public class SkillController : MonoBehaviour
    {
        [SerializeField] private GameHUD _hud;

        public event Action OnSkillUsed;

        private PlayerInputActions _input;

        private void Awake()
        {
            _input = new PlayerInputActions();
            _input.Skill1.performed += OnSkillPerformed;
            _input.Enable();
        }

        private void Start()
        {
            if (_hud != null)
                _hud.OnSkillPressed += UseSkill;
        }

        private void OnDestroy()
        {
            _input.Skill1.performed -= OnSkillPerformed;
            _input.Disable();

            if (_hud != null)
                _hud.OnSkillPressed -= UseSkill;
        }

        private void OnSkillPerformed(InputAction.CallbackContext _) => UseSkill();

        private void UseSkill() => OnSkillUsed?.Invoke();
    }
}
