using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.UI
{
    public class AdjustmentCenterFill : MonoBehaviour
    {
        public Slider slider;
        public RectTransform centerFillRect;
        public RectTransform trackRect;

        private RectTransform _cachedTrack;

        void Start()
        {
            if (slider != null)
                slider.onValueChanged.AddListener(UpdateFill);
            _cachedTrack = trackRect != null ? trackRect : GetComponent<RectTransform>();
        }

        void LateUpdate()
        {
            UpdateFill(slider != null ? slider.value : 0);
        }

        void UpdateFill(float value)
        {
            if (centerFillRect == null || slider == null) return;
            if (_cachedTrack == null) _cachedTrack = trackRect != null ? trackRect : GetComponent<RectTransform>();
            if (_cachedTrack == null) return;

            float trackWidth = _cachedTrack.rect.width;
            if (trackWidth <= 0f) return;

            float range = slider.maxValue - slider.minValue;
            if (range <= 0f) return;

            float normalized = (value - slider.minValue) / range;
            float zeroPoint = (0f - slider.minValue) / range;

            // For sliders starting at 0 (like Sharpness), fill from left
            if (slider.minValue >= 0f)
            {
                float barWidth = normalized * trackWidth;
                centerFillRect.anchorMin = new Vector2(0f, 0.5f);
                centerFillRect.anchorMax = new Vector2(0f, 0.5f);
                centerFillRect.sizeDelta = new Vector2(Mathf.Max(0f, barWidth), 3);
                centerFillRect.anchoredPosition = new Vector2(barWidth * 0.5f, 0);
                return;
            }

            // For sliders with negative range (-100 to 100), fill from center (zero point)
            float left = Mathf.Min(normalized, zeroPoint);
            float right = Mathf.Max(normalized, zeroPoint);

            float pixelLeft = left * trackWidth;
            float pixelRight = right * trackWidth;
            float barW = pixelRight - pixelLeft;
            float barCenterX = (pixelLeft + pixelRight) * 0.5f - trackWidth * 0.5f;

            centerFillRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerFillRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerFillRect.sizeDelta = new Vector2(Mathf.Max(0f, barW), 3);
            centerFillRect.anchoredPosition = new Vector2(barCenterX, 0);
        }
    }
}
