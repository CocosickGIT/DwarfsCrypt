using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Presentation.Combat;

namespace DwarfsCrypt.Presentation.Enemy
{
    // Boss with Don't Starve style telegraphed attacks. Patterns come from the character
    // JSON (CharacterConfig.BossAttacks): committing to one locks position + direction,
    // draws a TelegraphZone that fills over the wind-up, then detonates — standing outside
    // the highlighted area when it fills means the hit misses entirely.
    //
    // While a pattern is on cooldown / out of range the boss falls back to the base melee
    // attack. Below the HP threshold it enrages: faster movement, shorter cooldowns and
    // wind-ups, and EnrageOnly patterns become available.
    public class BossController : EnemyController
    {
        // Live bosses, for UI like BossHealthBarUI. Registered in OnEnable/OnDisable so
        // pooled (inactive) instances are never listed.
        public static readonly List<BossController> ActiveBosses = new();

        public CharacterComponent Character => _character;
        public bool IsEnraged => _isEnraged;

        // True while the boss is actively fighting (chasing or attacking a live target).
        // Aggro drops automatically when the player escapes past _deAggroRange or dies.
        public bool IsAggroed => _target != null
            && (_state == EnemyAIState.Chase || _state == EnemyAIState.Attack);

        [Header("Boss - Enrage")]
        [Tooltip("Enrage triggers when HP drops to this fraction of max HP")]
        [SerializeField, Range(0f, 1f)] private float _enrageHpThreshold = 0.5f;
        [SerializeField] private float _enrageSpeedBonus = 2f;
        [SerializeField] private float _enrageCooldownMultiplier = 0.5f;
        [Tooltip("Telegraph wind-ups are multiplied by this while enraged (shorter dodge window)")]
        [SerializeField, Range(0.25f, 1f)] private float _enrageWindupMultiplier = 0.75f;
        [SerializeField] private float _enrageWindUpDuration = 1f;

        private bool _isEnraged;
        private bool _isEnraging;
        private float _baseMoveSpeed;
        private float _baseAttackCooldown;

        private IReadOnlyList<BossAttackPattern> _patterns;
        private float[] _patternCooldowns;
        private BossAttackPattern _queuedPattern;
        private TelegraphZone _telegraph;
        private readonly List<int> _candidates = new();

        protected override void Awake()
        {
            base.Awake();
            // Enrage buffs _moveSpeed/_attackCooldown; cache the authored values so a
            // pooled boss comes back un-enraged with its original stats.
            _baseMoveSpeed = _moveSpeed;
            _baseAttackCooldown = _attackCooldown;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ActiveBosses.Add(this);
            _isEnraged = false;
            _isEnraging = false;
            _moveSpeed = _baseMoveSpeed;
            _attackCooldown = _baseAttackCooldown;
            _queuedPattern = null;

            RefreshPatterns();

            if (_telegraph != null)
                _telegraph.Hide();
        }

        // Patterns live on the character data, which the factory re-injects between spawns.
        // Called from OnEnable (pooled reactivation) AND Start: a scene-placed boss can hit
        // OnEnable before the child CharacterComponent's Awake has loaded the config.
        private void RefreshPatterns()
        {
            _patterns = _character != null && _character.Character != null
                ? _character.Character.BossAttacks
                : null;

            int count = _patterns?.Count ?? 0;
            if (_patternCooldowns == null || _patternCooldowns.Length != count)
                _patternCooldowns = new float[count];
            else
                Array.Clear(_patternCooldowns, 0, count);
        }

        private void OnDisable()
        {
            ActiveBosses.Remove(this);
            if (_telegraph != null)
                _telegraph.Hide();
        }

        protected override void Start()
        {
            base.Start();
            RefreshPatterns();
            _telegraph = TelegraphZone.Create(name + "_Telegraph");

            if (_character != null)
            {
                _character.OnDamaged += CheckEnrageThreshold;
                _character.OnDied += HideTelegraph;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_character != null)
            {
                _character.OnDamaged -= CheckEnrageThreshold;
                _character.OnDied -= HideTelegraph;
            }
            if (_telegraph != null)
                Destroy(_telegraph.gameObject);
        }

        protected override void Update()
        {
            base.Update();
            if (_patternCooldowns == null) return;
            for (int i = 0; i < _patternCooldowns.Length; i++)
                if (_patternCooldowns[i] > 0f)
                    _patternCooldowns[i] -= Time.deltaTime;
        }

        // Super armor: a boss that has committed to an attack (wind-up or recovery) cannot
        // be staggered out of it, otherwise the player could cancel every telegraphed
        // attack by mashing. Outside of attacks, hits stagger as usual.
        protected override void HandleDamaged(float currentHp, float maxHp)
        {
            if (_state == EnemyAIState.Attack) return;
            base.HandleDamaged(currentHp, maxHp);
        }

        // Try to commit to a telegraphed pattern before the base logic walks us into melee
        // range; base chase (and its basic attack at _attackRange) is the fallback.
        protected override void UpdateChase()
        {
            if (_target != null && _attackCooldownTimer <= 0f)
            {
                float dist = DistToTarget();
                if (dist <= _deAggroRange)
                {
                    int idx = SelectPattern(dist);
                    if (idx >= 0)
                    {
                        _queuedPattern = _patterns[idx];
                        _patternCooldowns[idx] = _queuedPattern.Cooldown
                            * (_isEnraged ? _enrageCooldownMultiplier : 1f);
                        EnterState(EnemyAIState.Attack);
                        return;
                    }
                }
            }
            base.UpdateChase();
        }

        protected override IEnumerator AttackRoutine()
        {
            BossAttackPattern pattern = _queuedPattern;
            _queuedPattern = null;

            if (pattern == null || _hitbox == null || _telegraph == null)
            {
                yield return base.AttackRoutine();
                yield break;
            }

            _desiredVelocity = Vector2.zero;

            // Lock the zone now: direction is captured at commit time and never re-aimed,
            // so the player can dodge out of the highlighted area during the wind-up.
            Vector2 dir = _target != null
                ? ((Vector2)_target.position - (Vector2)transform.position).normalized
                : Vector2.right;
            UpdateFacing(dir.x);
            PlayStateAnimation(PlayerState.IDLE);

            float windup = pattern.Windup * (_isEnraged ? _enrageWindupMultiplier : 1f);
            _telegraph.Show(transform.position, dir, pattern.Radius, pattern.HalfAngle, windup);
            yield return new WaitForSeconds(windup);

            PlayAnimation(PlayerState.ATTACK, HasAnimation(PlayerState.ATTACK, 1) ? 1 : 0);
            DamageResult damage = AttributeFormulas.RollPhysicalDamage(_character.Character.Attributes)
                                  * pattern.DamageMultiplier;
            _hitbox.PerformAttackImmediate(dir, damage, pattern.Radius, pattern.HalfAngle);

            _attackCooldownTimer = _attackCooldown;
            yield return new WaitForSeconds(pattern.Recovery);

            _state = EnemyAIState.Chase;
        }

        // Weighted pick among patterns that are off cooldown, whose range window contains
        // the target, and whose enrage requirement is met. Returns -1 when none qualify.
        private int SelectPattern(float dist)
        {
            if (_patterns == null || _patterns.Count == 0) return -1;

            _candidates.Clear();
            float totalWeight = 0f;

            for (int i = 0; i < _patterns.Count; i++)
            {
                BossAttackPattern p = _patterns[i];
                if (_patternCooldowns[i] > 0f) continue;
                if (p.EnrageOnly && !_isEnraged) continue;
                if (dist < p.MinRange) continue;
                if (p.MaxRange > 0f && dist > p.MaxRange) continue;

                _candidates.Add(i);
                totalWeight += EffectiveWeight(p);
            }

            if (_candidates.Count == 0) return -1;

            float roll = UnityEngine.Random.value * totalWeight;
            foreach (int i in _candidates)
            {
                roll -= EffectiveWeight(_patterns[i]);
                if (roll <= 0f) return i;
            }
            return _candidates[_candidates.Count - 1];
        }

        // JsonUtility leaves omitted numeric fields at 0 — treat that as weight 1.
        private static float EffectiveWeight(BossAttackPattern p) => p.Weight > 0f ? p.Weight : 1f;

        private void HideTelegraph()
        {
            if (_telegraph != null)
                _telegraph.Hide();
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
            try
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
            }
            finally
            {
                // StopAllCoroutines (death, target loss) disposes the enumerator and runs
                // this, so an interrupted wind-up can't leave _isEnraging stuck true and
                // silently disable enrage for the rest of the fight.
                _isEnraging = false;
            }
        }
    }
}
