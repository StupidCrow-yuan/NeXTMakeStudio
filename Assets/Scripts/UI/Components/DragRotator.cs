using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NeXTMake.UI
{
    public class DragRotator : MonoBehaviour, IDragHandler
    {
        public float sensitivity = 0.5f;
        public void OnDrag(PointerEventData eventData)
        {
            transform.Rotate(Vector3.up, -eventData.delta.x * sensitivity, Space.World);
            transform.Rotate(Vector3.right, eventData.delta.y * sensitivity, Space.World);
        }
    }
}

