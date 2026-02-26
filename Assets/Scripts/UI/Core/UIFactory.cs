using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.UI.Core
{
    public static class UIFactory
    {
        // Colors
        public static readonly Color COLOR_UV_BG = new Color(0.88f, 0.88f, 0.89f);
        public static readonly Color COLOR_3D_BG = new Color(0.12f, 0.12f, 0.12f);
        public static readonly Color COLOR_TEXT_DARK = new Color(0.1f, 0.1f, 0.1f);
        public static readonly Color COLOR_TEXT_LIGHT = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color COLOR_ACCENT_GREEN = new Color(0.2f, 0.8f, 0.4f);
        public static readonly Color COLOR_NAV_TEXT = new Color(0.3f, 0.3f, 0.3f);
        public static readonly Color COLOR_NAV_SELECTED = new Color(0.1f, 0.1f, 0.1f);

        // Fonts
        public static Font DefaultFont { get; set; }

        public static Canvas FindOrCreateCanvas()
        {
            Canvas c = Object.FindObjectOfType<Canvas>();
            if (c == null)
            {
                c = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // 添加窗口大小变化处理器
                if (c.gameObject.GetComponent<WindowResizeHandler>() == null)
                {
                    c.gameObject.AddComponent<WindowResizeHandler>();
                }
            }
            CanvasScaler s = c.GetComponent<CanvasScaler>();
            if (s == null) s = c.gameObject.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        public static void CleanupOldUI(Canvas canvas)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child.name.Contains("UIContainer") || child.name.Contains("Dialog") || child.name.Contains("Layout"))
                {
                    if (Application.isPlaying) Object.Destroy(child.gameObject);
                    else Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        public static GameObject CreateObject(string name, GameObject parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            if (parent != null) 
            {
                obj.transform.SetParent(parent.transform, false);
            }
            obj.transform.localScale = Vector3.one;
            return obj;
        }

        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero;
        }

        public static GameObject CreateText(string content, GameObject parent, int size, Color color, Vector2 pos, Vector2 sizeDelta, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            GameObject obj = CreateObject("Text", parent);
            RectTransform r = obj.GetComponent<RectTransform>();
            if (sizeDelta != Vector2.zero) { r.sizeDelta = sizeDelta; r.anchoredPosition = pos; }
            else Stretch(r);

            Text t = obj.AddComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false; // Disable raycast for text to allow button clicks underneath
            
            if (DefaultFont != null) t.font = DefaultFont;
            else
            {
                t.font = Resources.Load<Font>("fonts/HarmonyOS_Sans_SC_Regular");
                if (t.font == null) t.font = Resources.Load<Font>("fonts/NanumGothic-Regular");
                
                if (t.font == null)
                {
                    string[] fontNames = { "Segoe UI Symbol", "Segoe UI", "Arial", "LegacyRuntime" };
                    foreach (var fontName in fontNames)
                    {
                        Font f = Font.CreateDynamicFontFromOSFont(fontName, size);
                        if (f != null) { t.font = f; break; }
                    }
                }
                
                if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return obj;
        }

        public static GameObject CreateButton(string text, GameObject parent, Vector2 pos, Vector2 size, Color bg, Color textCol)
        {
            GameObject obj = CreateObject($"Btn_{text}", parent);
            RectTransform r = obj.GetComponent<RectTransform>();
            r.sizeDelta = size; r.anchoredPosition = pos;

            obj.AddComponent<Image>().color = bg;
            obj.AddComponent<Button>();
            
            CreateText(text, obj, 16, textCol, Vector2.zero, Vector2.zero);
            return obj;
        }
        
        public static GameObject CreateTextButton(string text, GameObject parent, int size, Color col)
        {
            GameObject obj = CreateObject(text, parent);
            obj.AddComponent<LayoutElement>().minWidth = 100;
            // Add transparent image for raycast target (clicking)
            Image img = obj.AddComponent<Image>();
            img.color = Color.clear;
            
            CreateText(text, obj, size, col, Vector2.zero, Vector2.zero);
            obj.AddComponent<Button>();
            return obj;
        }

        // Helper for selection cards
        public static GameObject CreateSelectionCard(string title, GameObject parent)
        {
            GameObject card = CreateObject(title, parent);
            Image img = card.AddComponent<Image>(); img.color = new Color(0.95f, 0.95f, 0.95f);
            RectTransform rect = card.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(300, 360);
            card.AddComponent<Button>().targetGraphic = img;

            GameObject icon = CreateObject("Icon", card);
            Image iconImg = icon.AddComponent<Image>(); iconImg.color = Color.gray;
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(200, 150); iconRect.anchoredPosition = new Vector2(0, 40);

            CreateText(title, card, 24, COLOR_TEXT_DARK, new Vector2(0, -100), new Vector2(250, 40));

            GameObject check = CreateObject("Checkmark", card);
            Image checkImg = check.AddComponent<Image>(); checkImg.color = COLOR_ACCENT_GREEN;
            RectTransform cr = check.GetComponent<RectTransform>();
            cr.anchorMin = Vector2.one; cr.anchorMax = Vector2.one;
            cr.sizeDelta = new Vector2(40, 40); cr.anchoredPosition = new Vector2(-30, -30);
            check.SetActive(false);
            return card;
        }

        public static GameObject CreateModal(string title, Vector2 size)
        {
            Canvas canvas = FindOrCreateCanvas();
            GameObject overlay = CreateObject("ModalOverlay", canvas.gameObject);
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.4f);
            overlay.AddComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));

            GameObject modal = CreateObject("ModalWindow", overlay);
            RectTransform mr = modal.GetComponent<RectTransform>();
            mr.sizeDelta = size;
            modal.AddComponent<Image>().color = Color.white;
            modal.AddComponent<Outline>().effectColor = Color.gray;
            // Prevent clicks through modal
            modal.AddComponent<Button>().interactable = false;

            // Header
            GameObject header = CreateObject("Header", modal);
            header.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            header.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
            header.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
            
            CreateText(title, header, 18, COLOR_TEXT_DARK, new Vector2(20, 0), new Vector2(0, 40), TextAnchor.MiddleLeft, FontStyle.Bold);

            GameObject closeBtn = CreateButton("X", header, new Vector2(-25, 0), new Vector2(30, 30), Color.clear, Color.black);
            closeBtn.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));
            closeBtn.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
            closeBtn.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);

            GameObject content = CreateObject("Content", modal);
            RectTransform cr = content.GetComponent<RectTransform>();
            cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
            cr.offsetMin = new Vector2(20, 20); cr.offsetMax = new Vector2(-20, -60);

            return content;
        }
    }
}


