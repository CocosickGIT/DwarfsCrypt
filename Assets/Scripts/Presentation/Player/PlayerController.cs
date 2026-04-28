using UnityEngine;
using UnityEngine.InputSystem;

namespace DwarfsCrypt.Presentation.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameHUD _hud;

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

        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
            
            _rb = GetComponent<Rigidbody2D>();
            _input = new PlayerInputActions();
            _input.Dash.performed += OnDashPerformed;
            _input.Enable();
        }

        private void Start()
        {
            if (_hud != null)
                _hud.OnDashPressed += RequestDash;
        }

        private void OnDestroy()
        {
            _input.Dash.performed -= OnDashPerformed;
            _input.Disable();
            // _input.Dispose();

            if (_hud != null)
                _hud.OnDashPressed -= RequestDash;
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
                return;
            }

            GatherMoveInput();

            if (_dashRequested)
            {
                _dashRequested = false;
                TryDash();
            }
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
            {
                _lastMoveDirection = _moveInput.normalized;
            }
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
