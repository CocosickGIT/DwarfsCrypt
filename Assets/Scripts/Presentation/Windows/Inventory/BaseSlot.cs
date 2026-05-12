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
            if (Item == null || _rootCanvas == null) return;

            DragSource = this;
            OnBeginDragInternal();
            SpawnDragVisual(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveDragVisual(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DestroyDragVisual();
            if (DragSource == this)
            {
                OnEndDragInternal();
                DragSource = null;
            }
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
