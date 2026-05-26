using UnityEngine;

namespace Presentation.Features
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset;
        
        [Header("Dead Zone")]
        [Tooltip("Half-extents of the region the target can roam without moving the camera")]
        [SerializeField] private Vector2 _deadZoneHalfSize = new Vector2(0.8f, 0.4f);

        [Header("Look Ahead")]
        [Tooltip("Max world-units the camera leads in the movement direction")]
        [SerializeField] private float _lookAheadDistance = 1.8f;
        [Tooltip("Seconds to reach full look-ahead / return to zero")]
        [SerializeField] private float _lookAheadSmoothTime = 0.45f;

        [Header("Follow")]
        [SerializeField] private float _followSmoothTime = 0.18f;
        [SerializeField] private float _maxFollowSpeed = 25f;

        private Vector3 _followVelocity;
        private Vector2 _lookAheadVelocity;
        private Vector2 _lookAheadOffset;

        // The dead-zone anchor in world XY; camera tracks this + look-ahead.
        private Vector2 _focusPoint;
        private Vector2 _moveDir = Vector2.right;

        // Rigidbody2D on the target — velocity is stable across render frames,
        // unlike a raw position delta which is zero on frames without a physics step.
        private Rigidbody2D _targetRb;

        private void Awake()
        {
            if (_target == null) return;
            Init(_target);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector2 targetPos = _target.position + _offset;

            // --- 1. Movement direction from physics velocity (frame-rate stable) ---
            Vector2 velocity = _targetRb != null ? _targetRb.linearVelocity : Vector2.zero;
            bool    isMoving = velocity.sqrMagnitude > 0.01f;
            if (isMoving)
                _moveDir = velocity.normalized;

            // --- 2. Dead zone: push focus only when target exits the rect ---
            Vector2 toTarget = targetPos - _focusPoint;
            float pushX = Mathf.Abs(toTarget.x) > _deadZoneHalfSize.x
                ? toTarget.x - Mathf.Sign(toTarget.x) * _deadZoneHalfSize.x
                : 0f;
            float pushY = Mathf.Abs(toTarget.y) > _deadZoneHalfSize.y
                ? toTarget.y - Mathf.Sign(toTarget.y) * _deadZoneHalfSize.y
                : 0f;
            _focusPoint += new Vector2(pushX, pushY);
            bool insideDeadZone = pushX == 0f && pushY == 0f;

            // --- 3. Look-ahead only while pushing the dead zone boundary ---
            // Keeping it active inside the dead zone would drift the camera off _focusPoint.
            Vector2 desiredLookAhead = (isMoving && !insideDeadZone)
                ? _moveDir * _lookAheadDistance
                : Vector2.zero;
            _lookAheadOffset = Vector2.SmoothDamp(
                _lookAheadOffset, desiredLookAhead,
                ref _lookAheadVelocity, _lookAheadSmoothTime);

            // --- 4. Smooth camera toward focus + look-ahead, preserve Z ---
            Vector3 desired = new Vector3(
                _focusPoint.x + _lookAheadOffset.x,
                _focusPoint.y + _lookAheadOffset.y,
                transform.position.z);

            transform.position = Vector3.SmoothDamp(
                transform.position, desired,
                ref _followVelocity, _followSmoothTime, _maxFollowSpeed);

            // --- 5. Snap once settled inside dead zone ---
            // SmoothDamp asymptotically approaches its target; this kills the residual drift.
            if (insideDeadZone
                && _lookAheadOffset.sqrMagnitude < 0.0001f
                && _followVelocity.sqrMagnitude  < 0.0001f)
            {
                _lookAheadOffset   = Vector2.zero;
                _lookAheadVelocity = Vector2.zero;
                _followVelocity    = Vector3.zero;
                transform.position = new Vector3(_focusPoint.x, _focusPoint.y, transform.position.z);
            }
        }

        // Called externally (e.g. from spawner) to assign a target at runtime.
        public void SetTarget(Transform target) => Init(target);

        private void Init(Transform target)
        {
            _target    = target;
            _targetRb  = target.GetComponent<Rigidbody2D>();
            _focusPoint = target.position;
            _lookAheadOffset   = Vector2.zero;
            _lookAheadVelocity = Vector2.zero;
            _followVelocity    = Vector3.zero;
            SnapToFocus();
        }

        private void SnapToFocus()
        {
            transform.position = new Vector3(_focusPoint.x, _focusPoint.y, transform.position.z);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _target == null) return;

            // Dead zone rectangle
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.55f);
            Vector3 focusWorld = new Vector3(_focusPoint.x, _focusPoint.y, 0f);
            Gizmos.DrawWireCube(focusWorld,
                new Vector3(_deadZoneHalfSize.x * 2f, _deadZoneHalfSize.y * 2f, 0f));

            // Look-ahead target line
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
            Vector3 lookTarget = focusWorld + new Vector3(_lookAheadOffset.x, _lookAheadOffset.y, 0f);
            Gizmos.DrawLine(focusWorld, lookTarget);
            Gizmos.DrawSphere(lookTarget, 0.08f);
        }
    }
}
