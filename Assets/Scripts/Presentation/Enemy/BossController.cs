using System.Collections;
using UnityEngine;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Presentation.Combat;

namespace DwarfsCrypt.Presentation.Enemy
{
    // Boss enemy: has a heavy attack variant and enters an enrage phase below a HP threshold.
    public class BossController : EnemyController
    {
        [Header("Boss - Heavy Attack")]
        [Tooltip("Wider or stronger hitbox used for the heavy slam")]
        [SerializeField] private MeleeAttackHitbox _heavyHitbox;
        [SerializeField] private float _heavyAttackCooldown = 4f;
        [SerializeField] private float _heavyAttackDuration = 0.9f;
        [SerializeField] private float _heavyAttackDamageMultiplier = 2.5f;
        [SerializeField, Range(0f, 1f)] private float _heavyAttackChance = 0.3f;

        [Header("Boss - Enrage")]
        [Tooltip("Enrage triggers when HP drops to this fraction of max HP")]
        [SerializeField, Range(0f, 1f)] private float _enrageHpThreshold = 0.5f;
        [SerializeField] private float _enrageSpeedBonus = 2f;
        [SerializeField] private float _enrageCooldownMultiplier = 0.5f;
        [SerializeField] private float _enrageWindUpDuration = 1f;

        private bool _isEnraged;
        private bool _isEnraging;
        private float _heavyCooldownTimer;

        protected override void Start()
        {
            base.Start();
            if (_character != null)
                _character.OnDamaged += CheckEnrageThreshold;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_character != null)
                _character.OnDamaged -= CheckEnrageThreshold;
        }

        protected override void Update()
        {
            base.Update();
            if (_heavyCooldownTimer > 0f)
                _heavyCooldownTimer -= Time.deltaTime;
        }

        private void CheckEnrageThreshold(float currentHp, float maxHp)
        {
            if (_isEnraged || _isEnraging || _state == EnemyAIState.Dead) return;
            if (currentHp / maxHp <= _enrageHpThreshold)
            {
                _isEnraging = true;
                StartCoroutine(EnrageRoutine());
            }
        }

        // Brief stagger that freezes movement, then permanently buffs the boss.
        // DAMAGED animation is already fired by the base HandleDamaged handler on the same hit.
        private IEnumerator EnrageRoutine()
        {
            _desiredVelocity = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < _enrageWindUpDuration)
            {
                _rb.linearVelocity = Vector2.zero;
                elapsed += Time.deltaTime;
                yield return null;
            }

            _moveSpeed += _enrageSpeedBonus;
            _attackCooldown *= _enrageCooldownMultiplier;
            _isEnraged = true;
            _isEnraging = false;
        }

        protected override IEnumerator AttackRoutine()
        {
            bool useHeavy = _heavyHitbox != null
                && _heavyCooldownTimer <= 0f
                && (_isEnraged || Random.value < _heavyAttackChance);

            if (!useHeavy)
            {
                yield return StartCoroutine(base.AttackRoutine());
                yield break;
            }

            // Wind-up telegraph before the heavy hit
            _desiredVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.25f);

            PlayAnimation(PlayerState.ATTACK, 1);

            if (_target != null)
            {
                Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
                UpdateFacing(dir.x);
                float damage = AttributeFormulas.RollPhysicalDamage(_character.Character.Attributes)
                               * _heavyAttackDamageMultiplier;
                _heavyHitbox.PerformAttack(dir, damage, _heavyAttackDuration);
            }

            _attackCooldownTimer = _attackCooldown;
            _heavyCooldownTimer = _heavyAttackCooldown;
            yield return new WaitForSeconds(_heavyAttackDuration);

            _state = EnemyAIState.Chase;
        }
    }
}
