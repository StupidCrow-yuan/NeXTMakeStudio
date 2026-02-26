using UnityEngine;
using UnityEngine.EventSystems;

namespace PocoRender.UI
{
    public class RotationHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler
    {
        public RectTransform target;
        public CanvasController controller;
        private Quaternion startRotation;

        public void OnPointerDown(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
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
            if (controller != null && target != null)
            {
                if (Quaternion.Angle(startRotation, target.rotation) > 0.1f)
                {
                    controller.RecordRotation(target, startRotation, target.rotation);
                }
            }
        }
    }
}

