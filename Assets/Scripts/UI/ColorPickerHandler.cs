using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PocoRender.UI
{
    public class ColorPickerHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public RectTransform pickerCircle;
        public Image areaImage;
        public Slider hueSlider;
        public InputField hexInput;
        public System.Action<Color> onColorChanged;

        private float currentHue = 0;
        private float currentSat = 1;
        private float currentVal = 1;
        private bool isUpdatingUI = false;

        void Start()
        {
            if (hueSlider != null)
            {
                hueSlider.onValueChanged.AddListener(OnHueChanged);
            }
            if (hexInput != null)
            {
                hexInput.onEndEdit.AddListener(OnHexInputEndEdit);
            }
            // Set initial position to top-right (fully saturated/bright)
            RectTransform rt = GetComponent<RectTransform>();
            pickerCircle.anchoredPosition = new Vector2(rt.rect.width / 2f, rt.rect.height / 2f);
            UpdateGradients();
        }

        public void OnHueChanged(float value)
        {
            if (isUpdatingUI) return;
            currentHue = value;
            UpdateGradients();
            NotifyColor();
        }

        private void OnHexInputEndEdit(string hex)
        {
            if (isUpdatingUI) return;
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            {
                SetColor(color);
            }
        }

        public void SetColor(Color color)
        {
            isUpdatingUI = true;
            Color.RGBToHSV(color, out currentHue, out currentSat, out currentVal);
            
            // Update Slider
            if (hueSlider != null) hueSlider.value = currentHue;
            
            // Update Picker Circle Position
            RectTransform rt = GetComponent<RectTransform>();
            float x = (currentSat * rt.rect.width) - (rt.rect.width / 2f);
            float y = (currentVal * rt.rect.height) - (rt.rect.height / 2f);
            pickerCircle.anchoredPosition = new Vector2(x, y);

            UpdateGradients();
            UpdateHexText(color);
            onColorChanged?.Invoke(color);
            isUpdatingUI = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdatePickerPos(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdatePickerPos(eventData);
        }

        private void UpdatePickerPos(PointerEventData eventData)
        {
            Vector2 localPos;
            RectTransform rt = GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out localPos))
            {
                // Clamp within bounds
                float halfW = rt.rect.width / 2f;
                float halfH = rt.rect.height / 2f;
                localPos.x = Mathf.Clamp(localPos.x, -halfW, halfW);
                localPos.y = Mathf.Clamp(localPos.y, -halfH, halfH);
                
                pickerCircle.anchoredPosition = localPos;
                
                // Map to Sat/Val (0-1)
                currentSat = (localPos.x + halfW) / rt.rect.width;
                currentVal = (localPos.y + halfH) / rt.rect.height;
                
                NotifyColor();
            }
        }

        private void UpdateGradients()
        {
            if (areaImage == null || areaImage.sprite == null) return;
            
            Texture2D svTexture = areaImage.sprite.texture;
            if (svTexture == null) return;

            for(int y=0; y<100; y++) {
                for(int x=0; x<100; x++) {
                    svTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, x/100f, y/100f));
                }
            }
            svTexture.Apply();
        }

        private void NotifyColor()
        {
            Color c = Color.HSVToRGB(currentHue, currentSat, currentVal);
            UpdateHexText(c);
            onColorChanged?.Invoke(c);
        }

        private void UpdateHexText(Color c)
        {
            if (hexInput != null && !isUpdatingUI)
            {
                hexInput.text = ColorUtility.ToHtmlStringRGB(c);
            }
        }
    }
}


