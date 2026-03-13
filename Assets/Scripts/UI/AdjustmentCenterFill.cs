using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.UI
{
    public class AdjustmentCenterFill : MonoBehaviour
    {
        public Slider slider;
        public RectTransform centerFillRect;

        private RectTransform parentRect;

        void Start()
        {
            if (slider != null)
                slider.onValueChanged.AddListener(UpdateFill);
            parentRect = GetComponent<RectTransform>();
            UpdateFill(slider != null ? slider.value : 0);
        }

        void UpdateFill(float value)
        {
            if (centerFillRect == null || parentRect == null || slider == null) return;

            float range = slider.maxValue - slider.minValue;
            if (range <= 0) return;

            float normalized = (value - slider.minValue) / range;
            float center = (0f - slider.minValue) / range;
            float trackWidth = parentRect.rect.width;

            float left = Mathf.Min(normalized, center);
            float right = Mathf.Max(normalized, center);

            float barWidth = (right - left) * trackWidth;
            float barCenter = ((left + right) * 0.5f - 0.5f) * trackWidth;

            centerFillRect.sizeDelta = new Vector2(barWidth, 0);
            centerFillRect.anchoredPosition = new Vector2(barCenter, 0);
        }
    }
}
