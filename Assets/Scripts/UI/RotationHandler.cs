using UnityEngine;
using UnityEngine.EventSystems;

namespace PocoRender.UI
{
    public class RotationHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform target;
        public CanvasController controller;
        private Quaternion startRotation;
        private bool isHovered;
        private bool isDragging;

        private void OnDisable()
        {
            isHovered = false;
            isDragging = false;
            NativeCursorUtility.Reset();
        }

        private void LateUpdate()
        {
            if (isHovered || isDragging)
            {
                NativeCursorUtility.Apply(NativeCursorShape.Rotate);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            NativeCursorUtility.Apply(NativeCursorShape.Rotate);
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            NativeCursorUtility.Apply(NativeCursorShape.Rotate);
            if (target != null) startRotation = target.rotation;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null) return;

            Vector2 dir = Input.mousePosition - target.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            target.rotation = Quaternion.Euler(0, 0, angle + 90); 
            
            if (controller != null)
            {
                controller.UpdatePositionInfo();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            if (controller != null && target != null)
            {
                if (Quaternion.Angle(startRotation, target.rotation) > 0.1f)
                {
                    controller.RecordRotation(target, startRotation, target.rotation);
                }
            }

            if (isHovered)
                NativeCursorUtility.Apply(NativeCursorShape.Rotate);
            else
                NativeCursorUtility.Reset();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            NativeCursorUtility.Apply(NativeCursorShape.Rotate);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (!isDragging)
            {
                NativeCursorUtility.Reset();
            }
        }
    }
}

