using UnityEngine;
using UnityEngine.EventSystems;

namespace NeXTMake.UI
{
    public class CanvasDragger : MonoBehaviour, IDragHandler
    {
        public CanvasController controller;
        private Canvas canvas;

        void Start()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (controller != null && controller.IsHandToolActive())
            {
                Vector2 delta = eventData.delta;
                if (canvas != null) delta /= canvas.scaleFactor;
                
                controller.paper.anchoredPosition += delta;
                controller.UpdateRulers();
            }
        }
    }
}


