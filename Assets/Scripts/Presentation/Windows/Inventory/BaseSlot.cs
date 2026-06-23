using DwarfsCrypt.Domain.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows.Inventory
{
    public abstract class BaseSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public static BaseSlot DragSource { get; private set; }

        private static GameObject _dragVisualGO;
        private static Canvas _rootCanvas;

        private ScrollRect _scrollRect;
        private bool _routingToScroll;

        public InventoryItem Item { get; protected set; }

        public static void SetRootCanvas(Canvas canvas) => _rootCanvas = canvas;

        // Returns true if this slot can receive the given item.
        public abstract bool AcceptsItem(ItemData item);

        protected abstract void OnItemSet();
        protected abstract void OnSlotCleared();
        protected abstract void HandleDrop(BaseSlot source);
        protected abstract Sprite GetIcon();

        public virtual void SetItem(InventoryItem item)
        {
            Item = item;
            OnItemSet();
        }

        public virtual void Clear()
        {
            Item = null;
            OnSlotCleared();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Empty slot, or the gesture runs along the scroll axis → hand the drag to the
            // ScrollRect so the list scrolls instead of trying to pick up an item.
            if (Item == null || _rootCanvas == null || IsScrollGesture(eventData))
            {
                _routingToScroll = TryRouteToScroll(eventData, ExecuteEvents.beginDragHandler);
                return;
            }

            DragSource = this;
            OnBeginDragInternal();
            SpawnDragVisual(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_routingToScroll)
            {
                TryRouteToScroll(eventData, ExecuteEvents.dragHandler);
                return;
            }
            MoveDragVisual(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_routingToScroll)
            {
                TryRouteToScroll(eventData, ExecuteEvents.endDragHandler);
                _routingToScroll = false;
                return;
            }

            DestroyDragVisual();
            if (DragSource == this)
            {
                OnEndDragInternal();
                DragSource = null;
            }
        }

        // A drag that moves mostly along the scroll axis is treated as a scroll, not an item pickup.
        private bool IsScrollGesture(PointerEventData eventData)
        {
            if (!FindScroll()) return false;
            Vector2 drag = eventData.position - eventData.pressPosition;
            return _scrollRect.vertical
                ? Mathf.Abs(drag.y) >= Mathf.Abs(drag.x)
                : Mathf.Abs(drag.x) >= Mathf.Abs(drag.y);
        }

        private bool TryRouteToScroll<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
            where T : IEventSystemHandler
        {
            if (!FindScroll()) return false;
            ExecuteEvents.Execute(_scrollRect.gameObject, eventData, handler);
            return true;
        }

        private bool FindScroll()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponentInParent<ScrollRect>();
            return _scrollRect != null;
        }

        public virtual void OnDrop(PointerEventData eventData)
        {
            if (DragSource == null || DragSource == this) return;
            if (DragSource.Item == null) return;
            if (!AcceptsItem(DragSource.Item.Data)) return;
            HandleDrop(DragSource);
        }

        protected virtual void OnBeginDragInternal() { }
        protected virtual void OnEndDragInternal() { }

        private void SpawnDragVisual(PointerEventData eventData)
        {
            _dragVisualGO = new GameObject("DragVisual", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _dragVisualGO.transform.SetParent(_rootCanvas.transform, false);
            _dragVisualGO.transform.SetAsLastSibling();

            var rt = (RectTransform)_dragVisualGO.transform;
            rt.pivot = Vector2.one * 0.5f;
            rt.anchorMin = rt.anchorMax = Vector2.one * 0.5f;
            rt.sizeDelta = ((RectTransform)transform).sizeDelta;

            var img = _dragVisualGO.GetComponent<Image>();
            img.sprite = GetIcon();
            img.raycastTarget = false;

            var cg = _dragVisualGO.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0.85f;

            MoveDragVisual(eventData);
        }

        private static void MoveDragVisual(PointerEventData eventData)
        {
            if (_dragVisualGO == null || _rootCanvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_rootCanvas.transform,
                eventData.position,
                _rootCanvas.worldCamera,
                out var localPoint);
            ((RectTransform)_dragVisualGO.transform).localPosition = localPoint;
        }

        private static void DestroyDragVisual()
        {
            if (_dragVisualGO == null) return;
            Destroy(_dragVisualGO);
            _dragVisualGO = null;
        }
    }
}
