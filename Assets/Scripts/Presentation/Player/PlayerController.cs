using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Presentation.Combat;
using DwarfsCrypt.Presentation.Windows;
using Core.Player;
using Core.Rewards;
using Core.Scenes;

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

        [Header("Attack")]
        [SerializeField] private float _attackCooldown = 0.8f;
        [SerializeField] private float _attackDuration = 0.5f;
        [SerializeField] private MeleeAttackHitbox _hitbox;
        [SerializeField] private CharacterComponent _character;

        [Header("Death")]
        [Tooltip("Seconds to let the death animation play before the reward summary appears.")]
        [SerializeField] private float _deathScreenDelay = 1.5f;

        [Header("Dash Phasing")]
        [Tooltip("Layers the player stops colliding with while dashing (e.g. Enemy + Environment). " +
                 "Do NOT include the Boundary layer, so the map walls still block a dashing player.")]
        [SerializeField] private LayerMask _dashPhaseLayers;

        private Rigidbody2D _rb;
        private Collider2D _collider;
        private LayerMask _baseExcludeLayers;
        private PlayerInputActions _input;

        private Vector2 _moveInput;
        private bool _dashRequested;
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector2 _dashDirection;
        private Vector2 _lastMoveDirection = Vector2.left;

        private bool _attackRequested;
        private bool _isAttacking;
        private bool _isDead;
        private float _attackCooldownTimer;
        private float _attackDurationTimer;
        private float _hitStaggerTimer;

        private PlayerState _currentState;
        private Dictionary<PlayerState, int> _animationIndex = new();
        private int _facingSign = 0; // 0 = unset, 1 = right (-x scale), -1 = left (+x scale)

        /// <summary>Called by CharacterSpawner before Start to bind the HUD.</summary>
        public void SetHUD(GameHUD hud) => _hud = hud;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            if (_collider != null) _baseExcludeLayers = _collider.excludeLayers;
            _input = new PlayerInputActions();
            _input.Dash.performed += OnDashPerformed;
            _input.Enable();
        }

        private void Start()
        {
            if (_hud == null)
                _hud = FindObjectsByType<GameHUD>(FindObjectsSortMode.None)[0];

            _hud.OnDashPressed += RequestDash;
            _hud.OnAttackPressed += RequestAttack;

            if (_character != null)
            {
                _character.OnDamaged += OnCharacterDamaged;
                _character.OnDied += HandleDied;
            }

            InitSpum();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Dash.performed -= OnDashPerformed;
                _input.Disable();
            }

            if (_hud != null)
            {
                _hud.OnDashPressed -= RequestDash;
                _hud.OnAttackPressed -= RequestAttack;
            }

            if (_character != null)
            {
                _character.OnDamaged -= OnCharacterDamaged;
                _character.OnDied -= HandleDied;
            }
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

            // Pre-load every clip into the override controller at init time.
            // SPUM's PlayAnimation reassigns the same clip reference at runtime, and Unity
            // skips the animator rebind when the reference hasn't changed — preventing the
            // position reset (snap to 0,0) that the rebind causes.
            foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
            {
                string key = state.ToString();
                if (_spum.StateAnimationPairs.TryGetValue(key, out var list) && list.Count > 0)
                    _spum.OverrideController[key] = list[0];
            }
        }

        private void Update()
        {
            if (_isDead) return;

            if (_dashCooldownTimer > 0f)
                _dashCooldownTimer -= Time.deltaTime;

            if (_attackCooldownTimer > 0f)
                _attackCooldownTimer -= Time.deltaTime;

            if (_hitStaggerTimer > 0f)
            {
                _hitStaggerTimer -= Time.deltaTime;
                _dashRequested = false;
                _attackRequested = false;
                return;
            }

            if (_isAttacking)
            {
                _attackDurationTimer -= Time.deltaTime;
                if (_attackDurationTimer <= 0f)
                    _isAttacking = false;
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                    SetDashing(false);

                PlayStateAnimation(PlayerState.MOVE);
                return;
            }

            GatherMoveInput();

            if (_dashRequested)
            {
                _dashRequested = false;
                TryDash();
            }

            if (_attackRequested)
            {
                _attackRequested = false;
                TryAttack();
            }

            if (_isAttacking) return;

            PlayerState state = _moveInput.sqrMagnitude > 0.01f ? PlayerState.MOVE : PlayerState.IDLE;
            UpdateFacing();
            PlayStateAnimation(state);
        }
        
        private void FixedUpdate()
        {
            if (_isDead)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (_hitStaggerTimer > 0f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

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
            if (_spum == null || _isAttacking) return;

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

        private void OnCharacterDamaged(float _currentHp, float _maxHp)
        {
            if (_isDead) return;
            PlayAnimation(PlayerState.DAMAGED);
        }

        private void HandleDied()
        {
            if (_isDead) return;
            _isDead = true;

            // Freeze the player and cancel any pending actions, then play the death animation.
            _rb.linearVelocity = Vector2.zero;
            _dashRequested = false;
            _attackRequested = false;
            _isAttacking = false;
            SetDashing(false);

            // Stop enemies from targeting/hitting the corpse, and hide the gameplay HUD.
            if (_collider != null) _collider.enabled = false;
            if (_hud != null) _hud.Hide();

            PlayAnimation(PlayerState.DEATH);

            StartCoroutine(ShowRewardsAfterDeath());
        }

        // Let the death animation play out, then surface the run reward summary.
        private IEnumerator ShowRewardsAfterDeath()
        {
            yield return new WaitForSeconds(_deathScreenDelay);

            // Rewards were applied to the profile on each kill; just persist and summarize.
            PlayerProfileService.Save();

            var rewardWindow = GetRewardWindow();
            if (rewardWindow != null)
            {
                rewardWindow.Closed -= OnDeathRewardClosed;
                rewardWindow.Closed += OnDeathRewardClosed;
                rewardWindow.Show(RunRewards.Summary);
            }
            else
            {
                // No reward UI wired — return to Town directly.
                OnDeathRewardClosed();
            }
        }

        private void OnDeathRewardClosed()
        {
            var rewardWindow = GetRewardWindow();
            if (rewardWindow != null)
                rewardWindow.Closed -= OnDeathRewardClosed;

            RunRewards.Reset();
            SceneLoader.Load(SceneNames.Town);
        }

        private RewardWindow GetRewardWindow()
        {
            var windowService = _hud != null ? _hud.WindowService : null;
            return windowService != null
                ? windowService.GetWindow<RewardWindow>(WindowType.Reward)
                : null;
        }

        private void OnDashPerformed(InputAction.CallbackContext _) => RequestDash();

        private void RequestDash() => _dashRequested = true;

        private void RequestAttack() => _attackRequested = true;

        private void TryAttack()
        {
            if (_attackCooldownTimer > 0f || _isAttacking || _isDashing) return;

            _attackCooldownTimer = _attackCooldown;
            _isAttacking = true;
            _attackDurationTimer = _attackDuration;
            PlayAnimation(PlayerState.ATTACK, _animationIndex[PlayerState.ATTACK]);

            if (_hitbox != null && _character != null)
            {
                Vector2 dir = _useIsometric
                    ? ToIsometric(_lastMoveDirection).normalized
                    : _lastMoveDirection;
                DamageResult damage = AttributeFormulas.RollPhysicalDamage(_character.Character.Attributes);

                _hitbox.PerformAttack(dir, damage, _attackDuration);
            }
        }

        private void TryDash()
        {
            if (_dashCooldownTimer > 0f) return;

            _dashDirection = _moveInput.sqrMagnitude > 0.01f
                ? (_useIsometric ? ToIsometric(_moveInput) : _moveInput).normalized
                : _lastMoveDirection;

            SetDashing(true);
            _dashTimer = _dashDuration;
            _dashCooldownTimer = _dashCooldown;
        }

        // Toggles the dash state. While dashing, the collider excludes the phase layers
        // (e.g. Enemy + Environment) so the player passes through them, but keeps colliding
        // with every other layer — crucially the Boundary walls — so it can never dash out of the map.
        private void SetDashing(bool dashing)
        {
            _isDashing = dashing;
            if (_collider == null) return;

            int exclude = _baseExcludeLayers;
            if (dashing) exclude |= _dashPhaseLayers;
            _collider.excludeLayers = exclude;
        }

        // Standard 2:1 isometric projection: squish Y and rotate 45°
        private static Vector2 ToIsometric(Vector2 input)
        {
            return new Vector2(input.x - input.y, (input.x + input.y) * 0.5f);
        }
    }
}
