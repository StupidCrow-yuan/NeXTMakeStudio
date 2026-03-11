using UnityEngine;
using UnityEngine.EventSystems;

namespace PocoRender.UI
{
    public class ObjectManipulator : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler
    {
        public bool IsLocked { get; set; } = false;
        private CanvasController canvasController;
        private RectTransform rectTransform;
        private Canvas canvas;
        private Vector2 dragStartPos;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasController = GetComponentInParent<CanvasController>();
            
            if (canvasController == null)
            {
                canvasController = Object.FindObjectOfType<CanvasController>();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (canvasController != null)
            {
                canvasController.SelectObject(this.gameObject);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (rectTransform != null) dragStartPos = rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsLocked || rectTransform == null) return;

            Vector2 delta = eventData.delta;
            if (canvas != null)
            {
                 delta /= canvas.scaleFactor;
            }

            rectTransform.anchoredPosition += delta;
            
            if (canvasController != null)
            {
                canvasController.UpdatePositionInfo();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (canvasController != null && rectTransform != null)
            {
                if (Vector2.Distance(dragStartPos, rectTransform.anchoredPosition) > 0.01f)
                {
                    canvasController.RecordMove(rectTransform, dragStartPos, rectTransform.anchoredPosition);
                    canvasController.OnObjectMoved();
                }
            }
        }
    }
}

