using UnityEngine;

namespace Presentation.Features
{
    [DefaultExecutionOrder(-5)]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] public Transform _target;
        [SerializeField] private Vector2 _offset;

        [Header("Dead Zone")]
        [Tooltip("Half-extents of the region the target can roam without moving the camera")]
        [SerializeField] private Vector2 _deadZoneHalfSize = new Vector2(0.8f, 0.4f);

        [Header("Follow")]
        [Tooltip("Seconds after exiting the dead zone before the camera begins moving")]
        [SerializeField] private float _followDelay = 0.3f;
        [SerializeField] private float _followSmoothTime = 0.25f;
        [SerializeField] private float _maxFollowSpeed = 20f;

        private Vector3 _followVelocity;
        private float _delayTimer;

        private void Start()
        {
            if (_target == null)
                Debug.LogWarning("CameraFollow has no target assigned.");
            else
                Init(_target);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector2 targetPos = (Vector2)_target.position + _offset;
            Vector2 toTarget  = targetPos - (Vector2)transform.position;

            bool outsideDeadZone = Mathf.Abs(toTarget.x) > _deadZoneHalfSize.x
                                || Mathf.Abs(toTarget.y) > _deadZoneHalfSize.y;

            if (outsideDeadZone)
                _delayTimer -= Time.deltaTime;
            else
                _delayTimer = _followDelay;

            // Follow once delay expires; keep moving until inertia bleeds off.
            bool shouldFollow = _delayTimer <= 0f || _followVelocity.sqrMagnitude > 0.0001f;

            if (shouldFollow)
            {
                Vector3 desired = new Vector3(targetPos.x, targetPos.y, transform.position.z);
                transform.position = Vector3.SmoothDamp(
                    transform.position, desired,
                    ref _followVelocity, _followSmoothTime, _maxFollowSpeed);
            }
            else
            {
                _followVelocity = Vector3.zero;
            }
        }

        public void SetTarget(Transform target) => Init(target);

        private void Init(Transform target)
        {
            _target         = target;
            _followVelocity = Vector3.zero;
            _delayTimer     = _followDelay;
            SnapToTarget();
        }

        private void SnapToTarget()
        {
            if (_target == null) return;
            Vector2 pos = (Vector2)_target.position + _offset;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _target == null) return;

            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.55f);
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, transform.position.y, 0f),
                new Vector3(_deadZoneHalfSize.x * 2f, _deadZoneHalfSize.y * 2f, 0f));

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            Gizmos.DrawSphere((Vector3)((Vector2)_target.position + _offset), 0.12f);
        }
    }
}
