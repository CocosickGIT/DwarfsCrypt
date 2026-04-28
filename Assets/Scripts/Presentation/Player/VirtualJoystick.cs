using UnityEngine;
using UnityEngine.EventSystems;

namespace DwarfsCrypt.Presentation.Player
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField, Range(0f, 1f)] private float _deadZone = 0.1f;

        public Vector2 Direction { get; private set; }

        private float _radius;

        private void Awake()
        {
            _radius = _background.sizeDelta.x * 0.5f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return;

            Vector2 raw = localPoint / _radius;
            Vector2 clamped = Vector2.ClampMagnitude(raw, 1f);

            _handle.anchoredPosition = clamped * _radius;
            Direction = clamped.magnitude > _deadZone ? clamped : Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Direction = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
        }
    }
}
