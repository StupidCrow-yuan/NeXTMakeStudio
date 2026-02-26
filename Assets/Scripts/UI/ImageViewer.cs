using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.UI
{
    public class ImageViewer : MonoBehaviour
    {
        [Header("UI Components")]
        public RawImage rawImage;
        public ScrollRect scrollRect;
        public Slider zoomSlider;

        private Texture2D currentTexture;
        private float currentZoom = 1.0f;

        void Start()
        {
            if (zoomSlider != null)
            {
                zoomSlider.onValueChanged.AddListener(OnZoomChanged);
                zoomSlider.minValue = 0.1f;
                zoomSlider.maxValue = 5.0f;
                zoomSlider.value = 1.0f;
            }
        }

        public void SetImage(Texture2D texture)
        {
            currentTexture = texture;

            if (rawImage != null && texture != null)
            {
                rawImage.texture = texture;

                // ������С����Ӧ��ͼ
                RectTransform rectTransform = rawImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
                }
            }

            ResetView();
        }

        void ResetView()
        {
            currentZoom = 1.0f;
            if (zoomSlider != null)
            {
                zoomSlider.value = currentZoom;
            }

            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0.5f, 0.5f);
            }
        }

        void OnZoomChanged(float value)
        {
            currentZoom = value;

            if (rawImage != null)
            {
                RectTransform rectTransform = rawImage.GetComponent<RectTransform>();
                if (rectTransform != null && currentTexture != null)
                {
                    rectTransform.sizeDelta = new Vector2(
                        currentTexture.width * currentZoom,
                        currentTexture.height * currentZoom
                    );
                }
            }
        }

        void Update()
        {
            if (scrollRect != null && Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                float newZoom = currentZoom + scroll * 0.1f;
                newZoom = Mathf.Clamp(newZoom, 0.1f, 5.0f);

                if (zoomSlider != null)
                {
                    zoomSlider.value = newZoom;
                }
            }
        }
    }
}
