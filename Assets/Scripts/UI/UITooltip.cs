using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PocoRender.UI
{
    public class UITooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string text;
        private GameObject tooltipObj;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (tooltipObj != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            tooltipObj = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasGroup));
            tooltipObj.transform.SetParent(canvas.transform, false);
            tooltipObj.transform.SetAsLastSibling();

            RectTransform tipRt = tooltipObj.GetComponent<RectTransform>();

            Image bg = tooltipObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.92f);
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero; textRt.anchoredPosition = Vector2.zero;

            Text label = textObj.AddComponent<Text>();
            label.text = text;
            label.fontSize = 11;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null) label.font = Font.CreateDynamicFontFromOSFont("Arial", 11);

            float textWidth = label.preferredWidth;
            tipRt.sizeDelta = new Vector2(textWidth + 12, 22);

            RectTransform myRt = GetComponent<RectTransform>();
            Vector3 worldPos = myRt.position;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos);
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out localPos);
            tipRt.anchoredPosition = new Vector2(localPos.x, localPos.y - myRt.rect.height * 0.5f - 16);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipObj != null)
            {
                Destroy(tooltipObj);
                tooltipObj = null;
            }
        }

        private void OnDisable()
        {
            if (tooltipObj != null)
            {
                Destroy(tooltipObj);
                tooltipObj = null;
            }
        }
    }
}
