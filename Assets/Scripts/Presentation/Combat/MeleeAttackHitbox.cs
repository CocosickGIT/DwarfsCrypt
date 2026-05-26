using System.Collections;
using UnityEngine;
using DwarfsCrypt.Domain.Characters;

namespace DwarfsCrypt.Presentation.Combat
{
    public class MeleeAttackHitbox : MonoBehaviour
    {
        [SerializeField] private float _radius = 1.5f;
        [SerializeField, Range(1f, 180f)] private float _halfAngle = 60f;

        [Tooltip("0 = hit at start of animation, 1 = hit at end")]
        [SerializeField, Range(0f, 1f)] private float _hitNormalizedTime = 0.5f;

        [SerializeField] private LayerMask _targetLayers;
        [SerializeField] private AttackSwipeVFX _swipeVFX;

        private Coroutine _activeAttack;
        private Vector2 _lastAttackDirection = Vector2.right;

        public void PerformAttack(Vector2 direction, float damage, float attackDuration)
        {
            _lastAttackDirection = direction;
            _swipeVFX?.Play(direction, _radius, _halfAngle, attackDuration);

            if (_activeAttack != null)
                StopCoroutine(_activeAttack);

            _activeAttack = StartCoroutine(AttackRoutine(direction, damage, attackDuration));
        }

        private IEnumerator AttackRoutine(Vector2 direction, float damage, float attackDuration)
        {
            yield return new WaitForSeconds(attackDuration * _hitNormalizedTime);
            ApplyHit(direction, damage);
            _activeAttack = null;
        }

        private void ApplyHit(Vector2 direction, float damage)
        {
            Vector2 origin = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, _radius, _targetLayers);

            foreach (var hit in hits)
            {
                Vector2 toTarget = (Vector2)hit.transform.position - origin;
                if (toTarget.sqrMagnitude < 0.001f) continue;

                if (Vector2.Angle(direction, toTarget) > _halfAngle) continue;

                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(damage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Vector2 dir = _lastAttackDirection.sqrMagnitude > 0.001f
                ? _lastAttackDirection
                : Vector2.right;

            Gizmos.color = new Color(1f, 0.4f, 0f, 0.9f);
            Vector3 leftEdge  = origin + (Vector3)(Rotate(dir, -_halfAngle) * _radius);
            Vector3 rightEdge = origin + (Vector3)(Rotate(dir,  _halfAngle) * _radius);
            Gizmos.DrawLine(origin, leftEdge);
            Gizmos.DrawLine(origin, rightEdge);

            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            const int Segments = 24;
            Vector3 prev = leftEdge;
            for (int i = 1; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                float angle = Mathf.Lerp(-_halfAngle, _halfAngle, t);
                Vector3 curr = origin + (Vector3)(Rotate(dir, angle) * _radius);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
