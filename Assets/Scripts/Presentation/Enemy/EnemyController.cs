using System;
using System.Collections;
using UnityEngine;
using Core.Rewards;
using Core.Statistics;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Presentation.Combat;

namespace DwarfsCrypt.Presentation.Enemy
{
    public enum EnemyAIState { Idle, Chase, Attack, Dead }

    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected CharacterComponent _character;
        [SerializeField] protected MeleeAttackHitbox _hitbox;
        [SerializeField] protected SPUM_Prefabs _spum;

        [Header("Detection")]
        [SerializeField] protected float _aggroRange = 8f;
        [SerializeField] protected float _deAggroRange = 32f;
        [SerializeField] protected LayerMask _playerLayer;

        [Header("Combat")]
        [SerializeField] protected float _attackRange = 1.2f;

        [Tooltip("Enemy stops approaching at this fraction of attack range, so it doesn't shove the player. 0.7 = stop at 70% of attack range.")]
        [SerializeField, Range(0.1f, 1f)] protected float _stopDistanceFactor = 0.7f;

        [SerializeField] protected float _attackCooldown = 1.5f;
        [SerializeField] protected float _attackDuration = 0.5f;

        [Header("Movement")]
        [SerializeField] protected float _moveSpeed = 3f;

        [Header("Hit Stop")]
        [SerializeField] private float _hitStaggerDuration = 0.15f;

        protected Rigidbody2D _rb;
        protected Transform _target;
        protected CharacterComponent _targetCharacter;
        protected EnemyAIState _state = EnemyAIState.Idle;
        protected float _attackCooldownTimer;
        protected float _hitStaggerTimer;
        protected Vector2 _desiredVelocity;

        private PlayerState _currentState;
        private System.Collections.Generic.Dictionary<PlayerState, int> _animationIndex = new();
        private int _facingSign;

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        // Reset combat/AI state on every (re)activation so a unit pulled from the pool
        // doesn't come back stuck in its previous Dead state.
        protected virtual void OnEnable()
        {
            _state = EnemyAIState.Idle;
            _target = null;
            _targetCharacter = null;
            _attackCooldownTimer = 0f;
            _hitStaggerTimer = 0f;
            _desiredVelocity = Vector2.zero;
        }

        protected virtual void Start()
        {
            InitSpum();

            if (_character != null)
            {
                _character.OnDied    += HandleDied;
                _character.OnDamaged += HandleDamaged;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_character != null)
            {
                _character.OnDied    -= HandleDied;
                _character.OnDamaged -= HandleDamaged;
            }
        }

        private void InitSpum()
        {
            if (_spum == null)
            {
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

        protected virtual void Update()
        {
            if (_state == EnemyAIState.Dead) return;

            // Stop chasing/attacking the moment the target dies (e.g. the player), so a dead
            // target is never pursued or hit.
            if (_targetCharacter != null && _targetCharacter.IsDead)
            {
                DropTarget();
                return;
            }

            if (_attackCooldownTimer > 0f)
                _attackCooldownTimer -= Time.deltaTime;

            if (_hitStaggerTimer > 0f)
            {
                _hitStaggerTimer -= Time.deltaTime;
                return;
            }

            UpdateAI();
        }

        protected virtual void FixedUpdate()
        {
            if (_state == EnemyAIState.Dead || _state == EnemyAIState.Attack || _hitStaggerTimer > 0f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }
            _rb.linearVelocity = _desiredVelocity;
        }

        protected virtual void UpdateAI()
        {
            switch (_state)
            {
                case EnemyAIState.Idle:   UpdateIdle();   break;
                case EnemyAIState.Chase:  UpdateChase();  break;
                case EnemyAIState.Attack: break;
            }
        }

        protected virtual void UpdateIdle()
        {
            // _desiredVelocity = Vector2.zero;
            PlayStateAnimation(PlayerState.IDLE);

            _target = ScanForPlayer();
            if (_target != null)
            {
                // The hit collider is on the physics root while CharacterComponent lives on a
                // child (see MeleeAttackHitbox.ResolveDamageable) — search children, not parents.
                _targetCharacter = _target.GetComponentInChildren<CharacterComponent>();
                EnterState(EnemyAIState.Chase);
            }
        }

        // Forget the current target and return to a calm idle state. Also stops any
        // in-flight attack coroutine so a queued hit can't land after the target is gone.
        protected void DropTarget()
        {
            StopAllCoroutines();
            _target = null;
            _targetCharacter = null;
            _desiredVelocity = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            _state = EnemyAIState.Idle;
            PlayStateAnimation(PlayerState.IDLE);
        }

        protected virtual void UpdateChase()
        {
            float dist = DistToTarget();

            if (_target == null || dist > _deAggroRange)
            {
                _target = null;
                _desiredVelocity = Vector2.zero;
                EnterState(EnemyAIState.Idle);
                return;
            }

            Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            UpdateFacing(dir.x);

            // Approach only until 70% of attack range, then hold position so the enemy
            // attacks from a distance instead of walking into and shoving the player.
            float stopDistance = _attackRange * _stopDistanceFactor;
            if (dist > stopDistance)
            {
                _desiredVelocity = dir * _moveSpeed;
                PlayStateAnimation(PlayerState.MOVE);
            }
            else
            {
                _desiredVelocity = Vector2.zero;
                PlayStateAnimation(PlayerState.IDLE);
            }

            if (dist <= _attackRange && _attackCooldownTimer <= 0f)
                EnterState(EnemyAIState.Attack);
        }

        protected void EnterState(EnemyAIState newState)
        {
            _state = newState;

            if (newState == EnemyAIState.Attack)
                StartCoroutine(AttackRoutine());
        }

        protected virtual IEnumerator AttackRoutine()
        {
            _desiredVelocity = Vector2.zero;
            PlayAnimation(PlayerState.ATTACK);

            if (_hitbox != null && _target != null)
            {
                Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
                UpdateFacing(dir.x);
                DamageResult damage = AttributeFormulas.RollPhysicalDamage(_character.Character.Attributes);
                _hitbox.PerformAttack(dir, damage, _attackDuration);
            }

            _attackCooldownTimer = _attackCooldown;
            yield return new WaitForSeconds(_attackDuration);

            _state = EnemyAIState.Chase;
        }

        // Virtual so bosses can grant themselves super armor (no interrupt) during committed attacks.
        protected virtual void HandleDamaged(float _, float __)
        {
            if (_state == EnemyAIState.Dead) return;

            _hitStaggerTimer = _hitStaggerDuration;

            if (_state == EnemyAIState.Attack)
            {
                StopAllCoroutines();
                _state = EnemyAIState.Chase;
            }

            PlayAnimation(PlayerState.DAMAGED);
        }

        private void HandleDied()
        {
            _state = EnemyAIState.Dead;
            _rb.linearVelocity = Vector2.zero;
            StopAllCoroutines();
            PlayAnimation(PlayerState.DEATH);

            // Grant this kill's rewards (exp/gold/item drops) to the player immediately,
            // and count it toward the kill statistics that quests track.
            if (_character != null && _character.Character != null)
            {
                RewardGranter.GrantKill(_character.Character.Rewards);
                StatisticsService.RecordKill(_character.Character.Name);
            }
        }

        protected Transform ScanForPlayer()
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, _aggroRange, _playerLayer);
            return hit != null ? hit.transform : null;
        }

        protected float DistToTarget()
        {
            return _target != null
                ? Vector2.Distance(transform.position, _target.position)
                : float.MaxValue;
        }

        // For looping states (IDLE, MOVE) — safe to call every frame, mirrors PlayerController.
        protected void PlayStateAnimation(PlayerState state)
        {
            if (_spum == null) return;
            int idx = _animationIndex[state];
            if (!HasAnimation(state, idx)) return;
            _currentState = state;
            _spum.PlayAnimation(_currentState, idx);
            _spum._anim.applyRootMotion = false;
        }

        // For one-shot states (ATTACK, DAMAGED, DEATH) — stores variant index, mirrors PlayerController.
        protected void PlayAnimation(PlayerState state, int index = 0)
        {
            if (_spum == null || !HasAnimation(state, index)) return;
            _animationIndex[state] = index;
            _spum.PlayAnimation(state, index);
            _spum._anim.applyRootMotion = false;
        }

        // Guard: prevents IndexOutOfRangeException when an animation list isn't populated,
        // which would otherwise leave the animator in a partial state and cause position resets.
        protected bool HasAnimation(PlayerState state, int index) =>
            _spum.StateAnimationPairs.TryGetValue(state.ToString(), out var list) && index < list.Count;

        protected void UpdateFacing(float dirX)
        {
            if (_spum == null) return;

            int newSign;
            if (dirX > 0.01f)       newSign = 1;
            else if (dirX < -0.01f) newSign = -1;
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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _aggroRange);
            Gizmos.color = new Color(1f, 0.35f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            Gizmos.color = new Color(0.35f, 0.6f, 1f);
            Gizmos.DrawWireSphere(transform.position, _attackRange * _stopDistanceFactor);
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, _deAggroRange);
        }
#endif
    }
}
