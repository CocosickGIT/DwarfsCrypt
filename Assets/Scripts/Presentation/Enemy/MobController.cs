using UnityEngine;

namespace DwarfsCrypt.Presentation.Enemy
{
    // Regular mob: idles, optionally patrols between waypoints, aggros and attacks on sight.
    public class MobController : EnemyController
    {
        [Header("Patrol")]
        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private float _patrolSpeed = 1.5f;
        [SerializeField] private float _patrolWaitTime = 2f;

        private int _patrolIndex;
        private float _patrolWaitTimer;
        private bool _isWaiting;

        protected override void UpdateIdle()
        {
            _target = ScanForPlayer();
            if (_target != null)
            {
                _isWaiting = false;
                EnterState(EnemyAIState.Chase);
                return;
            }

            Patrol();
        }

        private void Patrol()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0)
            {
                _desiredVelocity = Vector2.zero;
                PlayAnimation(PlayerState.IDLE);
                return;
            }

            if (_isWaiting)
            {
                _desiredVelocity = Vector2.zero;
                PlayAnimation(PlayerState.IDLE);
                _patrolWaitTimer -= Time.deltaTime;
                if (_patrolWaitTimer <= 0f)
                {
                    _isWaiting = false;
                    _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
                }
                return;
            }

            Transform goal = _patrolPoints[_patrolIndex];
            Vector2 toGoal = (Vector2)goal.position - (Vector2)transform.position;

            if (toGoal.sqrMagnitude < 0.04f)
            {
                _isWaiting = true;
                _patrolWaitTimer = _patrolWaitTime;
                _desiredVelocity = Vector2.zero;
                return;
            }

            Vector2 dir = toGoal.normalized;
            _desiredVelocity = dir * _patrolSpeed;
            UpdateFacing(dir.x);
            PlayAnimation(PlayerState.MOVE);
        }
    }
}
