using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DwarfsCrypt.Presentation.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameHUD _hud;
        [SerializeField] private SPUM_Prefabs _spum;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private bool _useIsometric = true;

        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 15f;
        [SerializeField] private float _dashDuration = 0.15f;
        [SerializeField] private float _dashCooldown = 1f;

        private Rigidbody2D _rb;
        private PlayerInputActions _input;

        private Vector2 _moveInput;
        private bool _dashRequested;
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector2 _dashDirection;
        private Vector2 _lastMoveDirection;

        private PlayerState _currentState;
        private Dictionary<PlayerState, int> _animationIndex = new();
        private int _facingSign = 0; // 0 = unset, 1 = right (-x scale), -1 = left (+x scale)

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = new PlayerInputActions();
            _input.Dash.performed += OnDashPerformed;
            _input.Enable();
        }

        private void Start()
        {
            if (_hud != null)
                _hud.OnDashPressed += RequestDash;

            InitSpum();
        }

        private void OnDestroy()
        {
            _input.Dash.performed -= OnDashPerformed;
            _input.Disable();

            if (_hud != null)
                _hud.OnDashPressed -= RequestDash;
        }

        private void InitSpum()
        {
            if (_spum == null)
            {
                // Search children only — never self, to avoid flipping the physics root
                for (int i = 0; i < transform.childCount; i++)
                {
                    _spum = transform.GetChild(i).GetComponentInChildren<SPUM_Prefabs>();
                    if (_spum != null) break;
                }
            }

            if (_spum == null) return;

            if (!_spum.allListsHaveItemsExist())
                _spum.PopulateAnimationLists();

            _spum.OverrideControllerInit();
            _spum._anim.applyRootMotion = false;

            foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
                _animationIndex[state] = 0;
        }

        private void Update()
        {
            if (_dashCooldownTimer > 0f)
                _dashCooldownTimer -= Time.deltaTime;

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                    _isDashing = false;

                PlayStateAnimation(PlayerState.MOVE);
                return;
            }

            GatherMoveInput();

            if (_dashRequested)
            {
                _dashRequested = false;
                TryDash();
            }

            PlayerState state = _moveInput.sqrMagnitude > 0.01f ? PlayerState.MOVE : PlayerState.IDLE;
            UpdateFacing();
            PlayStateAnimation(state);
        }

        private void FixedUpdate()
        {
            if (_isDashing)
            {
                _rb.linearVelocity = _dashDirection * _dashSpeed;
                return;
            }

            Vector2 move = _useIsometric ? ToIsometric(_moveInput) : _moveInput;
            _rb.linearVelocity = move * _moveSpeed;
        }

        private void GatherMoveInput()
        {
            Vector2 keyboard = _input.Move.ReadValue<Vector2>();
            Vector2 joystick = _hud != null ? _hud.Joystick.Direction : Vector2.zero;
            _moveInput = keyboard.sqrMagnitude >= joystick.sqrMagnitude ? keyboard : joystick;

            if (_moveInput.sqrMagnitude > 0.01f)
                _lastMoveDirection = _moveInput.normalized;
        }

        private void UpdateFacing()
        {
            if (_spum == null) return;

            float facingX = _moveInput.sqrMagnitude > 0.01f ? _moveInput.x : _lastMoveDirection.x;

            int newSign;
            if (facingX > 0f) newSign = 1;
            else if (facingX < 0f) newSign = -1;
            else return;

            if (newSign == _facingSign) return;

            _facingSign = newSign;
            float absX = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(
                _facingSign == 1 ? -absX : absX,
                transform.localScale.y,
                transform.localScale.z
            );
        }

        private void PlayStateAnimation(PlayerState state)
        {
            if (_spum == null) return;
            _currentState = state;
            _spum.PlayAnimation(_currentState, _animationIndex[_currentState]);
        }

        // Call externally to trigger one-shot states (ATTACK, DAMAGED, DEATH, etc.)
        public void PlayAnimation(PlayerState state, int index = 0)
        {
            if (_spum == null) return;
            _animationIndex[state] = index;
            _spum.PlayAnimation(state, index);
        }

        private void OnDashPerformed(InputAction.CallbackContext _) => RequestDash();

        private void RequestDash() => _dashRequested = true;

        private void TryDash()
        {
            if (_dashCooldownTimer > 0f) return;

            _dashDirection = _moveInput.sqrMagnitude > 0.01f
                ? (_useIsometric ? ToIsometric(_moveInput) : _moveInput).normalized
                : _lastMoveDirection;

            _isDashing = true;
            _dashTimer = _dashDuration;
            _dashCooldownTimer = _dashCooldown;
        }

        // Standard 2:1 isometric projection: squish Y and rotate 45°
        private static Vector2 ToIsometric(Vector2 input)
        {
            return new Vector2(input.x - input.y, (input.x + input.y) * 0.5f);
        }
    }
}
