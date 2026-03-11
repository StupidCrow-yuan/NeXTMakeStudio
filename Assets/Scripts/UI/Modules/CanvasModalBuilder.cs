using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using PocoRender.UI.Core;

namespace PocoRender.UI.Modules
{
    /// <summary>
    /// All modal popups and reusable dropdown builder for the canvas editor.
    /// Extracted from CanvasModule for maintainability.
    /// </summary>
    public static class CanvasModalBuilder
    {
        public static GameObject CreateModalPopup(GameObject root, string title)
        {
            return CreateBaseModal(root, title, new Vector2(600, 450));
        }

        public static GameObject CreateInfoPopup(GameObject root, string message)
        {
            GameObject overlay = UIFactory.CreateObject("ModalOverlay", root);
            UIFactory.Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);
            overlay.AddComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));

            GameObject panel = UIFactory.CreateObject("InfoPanel", overlay);
            RectTransform pRt = panel.GetComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(380, 160);
            panel.AddComponent<Image>().color = Color.white;
            panel.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            panel.AddComponent<Button>();

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 16);
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            Text msgText = UIFactory.CreateText(message ?? "", panel, 15, Color.black,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter).GetComponent<Text>();
            LayoutElement msgLe = msgText.gameObject.AddComponent<LayoutElement>();
            msgLe.flexibleHeight = 1;

            GameObject okBtn = UIFactory.CreateButton("OK", panel, Vector2.zero,
                new Vector2(100, 34), new Color(0.15f, 0.15f, 0.18f), Color.white);
            okBtn.AddComponent<LayoutElement>().minHeight = 34;
            okBtn.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));

            return overlay;
        }

        public static GameObject CreateColorPicker(GameObject root)
        {
            GameObject container = new GameObject("ColorPickerContainer");
            CreateColorPickerModal(root, null);
            return container;
        }

        public static GameObject CreateCustomDropdown(string name, GameObject parent, string[] options, int defaultIdx, System.Action<int> onValueChanged)
        {
            GameObject dropdownObj = UIFactory.CreateObject(name, parent);
            dropdownObj.AddComponent<LayoutElement>().minHeight = 40;
            Image ddImg = dropdownObj.AddComponent<Image>(); ddImg.color = new Color(0.95f, 0.95f, 0.95f);
            Dropdown dd = dropdownObj.AddComponent<Dropdown>();
            dd.targetGraphic = ddImg;
            UIFactory.AddDropdownArrow(dropdownObj, 14f);
            
            GameObject template = UIFactory.CreateObject("Template", dropdownObj);
            template.AddComponent<CanvasGroup>();
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0); templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1); templateRect.sizeDelta = new Vector2(0, 200);
            templateRect.anchoredPosition = new Vector2(0, 2);
            template.AddComponent<Image>().color = Color.white;
            template.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            
            ScrollRect templateSr = template.AddComponent<ScrollRect>();
            templateSr.horizontal = false; templateSr.vertical = true;
            templateSr.movementType = ScrollRect.MovementType.Clamped;
            templateSr.scrollSensitivity = 25;

            GameObject vp = UIFactory.CreateObject("Viewport", template);
            UIFactory.Stretch(vp.GetComponent<RectTransform>());
            vp.GetComponent<RectTransform>().offsetMax = new Vector2(-8, 0);
            vp.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            vp.AddComponent<RectMask2D>();

            GameObject sbar = UIFactory.CreateObject("Scrollbar", template);
            RectTransform sbr = sbar.GetComponent<RectTransform>();
            sbr.anchorMin = new Vector2(1, 0); sbr.anchorMax = new Vector2(1, 1);
            sbr.sizeDelta = new Vector2(4, -4); sbr.anchoredPosition = new Vector2(-2, 0);
            sbar.AddComponent<Image>().color = new Color(0.98f, 0.98f, 0.98f, 0.5f);
            Scrollbar sbc = sbar.AddComponent<Scrollbar>();
            sbc.direction = Scrollbar.Direction.BottomToTop;
            GameObject h = UIFactory.CreateObject("Handle", sbar);
            h.AddComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
            sbc.handleRect = h.GetComponent<RectTransform>(); UIFactory.Stretch(sbc.handleRect);
            templateSr.verticalScrollbar = sbc;

            GameObject content = UIFactory.CreateObject("Content", vp);
            RectTransform cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1); cRect.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            templateSr.viewport = vp.GetComponent<RectTransform>(); templateSr.content = cRect;
            dd.template = templateRect;
            
            GameObject itemTemplate = UIFactory.CreateObject("Item", content);
            itemTemplate.AddComponent<LayoutElement>().minHeight = 32;
            Toggle itemToggle = itemTemplate.AddComponent<Toggle>();
            GameObject itemBg = UIFactory.CreateObject("ItemBackground", itemTemplate);
            UIFactory.Stretch(itemBg.GetComponent<RectTransform>());
            Image itemBgImg = itemBg.AddComponent<Image>(); itemBgImg.color = new Color(0, 0, 0, 0);
            GameObject itemLabelObj = new GameObject("ItemLabel", typeof(RectTransform));
            itemLabelObj.transform.SetParent(itemTemplate.transform, false);
            Text itemText = itemLabelObj.AddComponent<Text>();
            itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemText.fontSize = 13; itemText.color = Color.black; itemText.alignment = TextAnchor.MiddleLeft;
            RectTransform itRt = itemText.rectTransform;
            itRt.anchorMin = Vector2.zero; itRt.anchorMax = Vector2.one; itRt.offsetMin = new Vector2(35, 0); itRt.offsetMax = Vector2.zero;
            GameObject itemCheck = UIFactory.CreateObject("ItemCheckmark", itemTemplate);
            itemCheck.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f); itemCheck.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
            itemCheck.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20); itemCheck.GetComponent<RectTransform>().anchoredPosition = new Vector2(15, 0);
            itemCheck.AddComponent<Image>().color = UIFactory.COLOR_ACCENT_GREEN;
            dd.itemText = itemText; itemToggle.targetGraphic = itemBgImg; itemToggle.graphic = itemCheck.GetComponent<Image>();
            template.SetActive(false);

            Text label = UIFactory.CreateText(options[defaultIdx], dropdownObj, 12, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10, 0);
            label.rectTransform.offsetMax = new Vector2(-28, 0);
            dd.captionText = label;
            dd.AddOptions(options.ToList());
            dd.value = defaultIdx;
            dd.onValueChanged.AddListener((i) => onValueChanged?.Invoke(i));
            return dropdownObj;
        }

        public static GameObject CreateBaseModal(GameObject root, string title, Vector2 size)
        {
            GameObject overlay = UIFactory.CreateObject("ModalOverlay", root);
            UIFactory.Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            overlay.AddComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));

            GameObject panel = UIFactory.CreateObject("Panel", overlay);
            RectTransform pRt = panel.GetComponent<RectTransform>(); pRt.sizeDelta = size;
            panel.AddComponent<Image>().color = Color.white;
            panel.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            panel.AddComponent<Button>();

            GameObject header = UIFactory.CreateObject("Header", panel);
            RectTransform hRt = header.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1); hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1); hRt.sizeDelta = new Vector2(0, 50); hRt.anchoredPosition = Vector2.zero;
            if (!string.IsNullOrEmpty(title)) {
                Text t = UIFactory.CreateText(title, header, 18, Color.black, Vector2.zero, new Vector2(0, 50), TextAnchor.MiddleLeft, FontStyle.Bold).GetComponent<Text>();
                t.rectTransform.offsetMin = new Vector2(20, 0); t.raycastTarget = false;
            }
            GameObject closeBtn = UIFactory.CreateObject("CloseBtn", header);
            RectTransform cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1, 0.5f); cbRt.anchorMax = new Vector2(1, 0.5f);
            cbRt.sizeDelta = new Vector2(40, 40); cbRt.anchoredPosition = new Vector2(-25, 0);
            Text closeTxt = UIFactory.CreateText("\u2715", closeBtn, 20, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            closeTxt.raycastTarget = true;
            closeBtn.AddComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));

            GameObject contentObj = UIFactory.CreateObject("Content", panel);
            RectTransform cRt = contentObj.GetComponent<RectTransform>();
            UIFactory.Stretch(cRt); cRt.offsetMax = new Vector2(0, -50);
            return contentObj;
        }

        public static void CreatePrintBedModal(GameObject root)
        {
            GameObject content = CreateBaseModal(root, "", new Vector2(800, 600));
            GameObject tabs = UIFactory.CreateObject("Tabs", content);
            RectTransform tRt = tabs.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1); tRt.sizeDelta = new Vector2(0, 50);
            HorizontalLayoutGroup thlg = tabs.AddComponent<HorizontalLayoutGroup>();
            thlg.childAlignment = TextAnchor.MiddleLeft; thlg.spacing = 30; thlg.padding = new RectOffset(40, 0, 0, 0);
            string[] bedModes = { "Standard Flatbed", "Mini Flatbed", "Rotary", "Roll-To-Film" };
            GameObject infoArea = UIFactory.CreateObject("InfoArea", content);
            UIFactory.Stretch(infoArea.GetComponent<RectTransform>());
            infoArea.GetComponent<RectTransform>().offsetMin = new Vector2(40, 40);
            infoArea.GetComponent<RectTransform>().offsetMax = new Vector2(-40, -60);
            infoArea.AddComponent<VerticalLayoutGroup>().spacing = 20;
            System.Action<int> SwitchTab = (idx) => {
                for (int i = 0; i < tabs.transform.childCount; i++) { var tabObj = tabs.transform.GetChild(i).gameObject; tabObj.GetComponent<Outline>().enabled = (i == idx); tabObj.GetComponent<Image>().color = (i == idx) ? new Color(0.95f, 0.95f, 0.95f) : Color.white; var txt = tabObj.GetComponentInChildren<Text>(); if(txt) txt.fontStyle = (i == idx) ? FontStyle.Bold : FontStyle.Normal; }
                foreach(Transform child in infoArea.transform) Object.Destroy(child.gameObject);
                UIFactory.CreateText("Material Requirements", infoArea, 16, Color.black, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.CreateText("\u2022 The height of the object must not exceed 60 mm.\n\u2022 The surface height variation does not exceed 2 mm.", infoArea, 14, Color.gray, Vector2.zero, new Vector2(0, 80), TextAnchor.MiddleLeft);
                GameObject imgMock = UIFactory.CreateObject("ImageMock", infoArea); imgMock.AddComponent<LayoutElement>().flexibleHeight = 1; imgMock.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
                UIFactory.CreateText("Illustration for " + bedModes[idx], imgMock, 14, Color.gray, Vector2.zero, Vector2.zero);
            };
            for(int i=0; i<bedModes.Length; i++) { int idx = i; GameObject tObj = UIFactory.CreateObject("Tab_"+i, tabs); tObj.AddComponent<Image>().color = Color.white; tObj.AddComponent<LayoutElement>().minWidth = 120; UIFactory.CreateText(bedModes[i], tObj, 15, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>().raycastTarget = true; UIFactory.Stretch(tObj.transform.GetChild(0).GetComponent<RectTransform>()); tObj.AddComponent<Button>().onClick.AddListener(() => SwitchTab(idx)); Outline outline = tObj.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(0, -2); outline.enabled = false; }
            SwitchTab(0);
        }

        public static void CreateAlignmentModal(GameObject root)
        {
            GameObject content = CreateBaseModal(root, "", new Vector2(700, 500));
            GameObject tabs = UIFactory.CreateObject("Tabs", content);
            RectTransform tRt = tabs.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1); tRt.sizeDelta = new Vector2(0, 50);
            HorizontalLayoutGroup thlg = tabs.AddComponent<HorizontalLayoutGroup>();
            thlg.childAlignment = TextAnchor.MiddleLeft; thlg.spacing = 30; thlg.padding = new RectOffset(40, 0, 0, 0);
            GameObject infoArea = UIFactory.CreateObject("InfoArea", content);
            UIFactory.Stretch(infoArea.GetComponent<RectTransform>());
            infoArea.GetComponent<RectTransform>().offsetMin = new Vector2(40, 40);
            infoArea.GetComponent<RectTransform>().offsetMax = new Vector2(-40, -60);
            infoArea.AddComponent<VerticalLayoutGroup>().spacing = 15;
            string[] modes = { "Photo Alignment", "Zero Point Alignment" };
            System.Action<int> SwitchTab = (idx) => {
                for (int i = 0; i < tabs.transform.childCount; i++) { var tabObj = tabs.transform.GetChild(i).gameObject; tabObj.GetComponent<Outline>().enabled = (i == idx); tabObj.GetComponent<Image>().color = (i == idx) ? new Color(0.95f, 0.95f, 0.95f) : Color.white; var txt = tabObj.GetComponentInChildren<Text>(); if(txt) txt.fontStyle = (i == idx) ? FontStyle.Bold : FontStyle.Normal; }
                foreach(Transform child in infoArea.transform) Object.Destroy(child.gameObject);
                UIFactory.CreateText("1/3 Introduction to " + modes[idx], infoArea, 16, Color.black, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.CreateText(modes[idx] + ": Captures an image of the printing bed for precise visual alignment.", infoArea, 14, Color.black, Vector2.zero, new Vector2(0, 80), TextAnchor.MiddleLeft);
            };
            for(int i=0; i<modes.Length; i++) { int idx = i; GameObject tObj = UIFactory.CreateObject("Tab_"+i, tabs); tObj.AddComponent<Image>().color = Color.white; tObj.AddComponent<LayoutElement>().minWidth = 150; UIFactory.CreateText(modes[i], tObj, 15, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>().raycastTarget = true; UIFactory.Stretch(tObj.transform.GetChild(0).GetComponent<RectTransform>()); tObj.AddComponent<Button>().onClick.AddListener(() => SwitchTab(idx)); Outline outline = tObj.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(0, -2); outline.enabled = false; }
            SwitchTab(0);
        }

        public static void CreateColorPickerModal(GameObject root, System.Action<Color> onColorPicked)
        {
            GameObject content = CreateBaseModal(root, "Material Color", new Vector2(400, 550));
            RectTransform pRt = content.transform.parent.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(1, 0.5f); pRt.anchorMax = new Vector2(1, 0.5f);
            pRt.anchoredPosition = new Vector2(-520, 0);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>(); vlg.padding = new RectOffset(20, 20, 20, 20); vlg.spacing = 15;

            GameObject colorArea = UIFactory.CreateObject("ColorArea", content);
            colorArea.AddComponent<LayoutElement>().minHeight = 250;
            Image areaImg = colorArea.AddComponent<Image>();
            Texture2D svTexture = new Texture2D(100, 100);
            for(int y=0; y<100; y++) for(int x=0; x<100; x++) svTexture.SetPixel(x, y, Color.HSVToRGB(0, x/100f, y/100f));
            svTexture.Apply();
            areaImg.sprite = Sprite.Create(svTexture, new Rect(0,0,100,100), Vector2.zero);
            GameObject pickerCircle = UIFactory.CreateObject("Picker", colorArea);
            pickerCircle.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
            pickerCircle.AddComponent<Image>().color = Color.white; pickerCircle.AddComponent<Outline>().effectColor = Color.black;

            GameObject hueSliderObj = UIFactory.CreateObject("HueSlider", content);
            hueSliderObj.AddComponent<LayoutElement>().minHeight = 24;
            Image hueImg = hueSliderObj.AddComponent<Image>();
            Texture2D hueTex = new Texture2D(100, 1);
            for(int i=0; i<100; i++) hueTex.SetPixel(i, 0, Color.HSVToRGB(i/100f, 1, 1));
            hueTex.Apply();
            hueImg.sprite = Sprite.Create(hueTex, new Rect(0,0,100,1), Vector2.zero);
            Slider hueSlider = hueSliderObj.AddComponent<Slider>(); hueSlider.minValue = 0; hueSlider.maxValue = 1;
            GameObject hueHandle = UIFactory.CreateObject("Handle", hueSliderObj);
            hueHandle.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            hueHandle.AddComponent<Image>().color = Color.white; hueHandle.AddComponent<Outline>().effectColor = Color.gray;
            hueSlider.handleRect = hueHandle.GetComponent<RectTransform>();

            GameObject inputs = UIFactory.CreateObject("Inputs", content);
            inputs.AddComponent<LayoutElement>().minHeight = 40;
            inputs.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            GameObject hexBox = UIFactory.CreateObject("HexBox", inputs);
            hexBox.AddComponent<LayoutElement>().minWidth = 100;
            hexBox.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
            InputField hexIn = hexBox.AddComponent<InputField>();
            Text hexTxt = UIFactory.CreateText("FFFFFF", hexBox, 14, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            hexTxt.alignment = TextAnchor.MiddleCenter; hexIn.textComponent = hexTxt;

            ColorPickerHandler handler = colorArea.AddComponent<ColorPickerHandler>();
            handler.pickerCircle = pickerCircle.GetComponent<RectTransform>(); handler.areaImage = areaImg;
            handler.hueSlider = hueSlider; handler.hexInput = hexIn; handler.onColorChanged = onColorPicked;

            UIFactory.CreateText("Recommend colors", content, 13, Color.gray, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft);
            GameObject grid = UIFactory.CreateObject("Grid", content);
            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>(); glg.cellSize = new Vector2(35, 35); glg.spacing = new Vector2(8, 8);
            Color[] recommends = { Color.white, Color.black, Color.red, Color.magenta, new Color(1f, 0.5f, 0f), Color.yellow, Color.green, Color.cyan, Color.blue, Color.gray };
            foreach(var c in recommends) { GameObject item = UIFactory.CreateObject("Color", grid); item.AddComponent<Image>().color = c; item.AddComponent<Button>().onClick.AddListener(() => handler.SetColor(c)); }
        }

        public static void CreateChokeModal(GameObject root)
        {
            GameObject content = CreateBaseModal(root, "White Underbase Choke", new Vector2(600, 500));
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>(); vlg.padding = new RectOffset(30, 30, 30, 30); vlg.spacing = 20;
            GameObject imgArea = UIFactory.CreateObject("Images", content);
            imgArea.AddComponent<LayoutElement>().flexibleHeight = 1;
            imgArea.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);
            HorizontalLayoutGroup ihlg = imgArea.AddComponent<HorizontalLayoutGroup>(); ihlg.padding = new RectOffset(20, 20, 20, 20); ihlg.spacing = 40;
            System.Action<string> CreatePumpkin = (label) => { GameObject p = UIFactory.CreateObject("Pumpkin", imgArea); VerticalLayoutGroup pvlg = p.AddComponent<VerticalLayoutGroup>(); pvlg.spacing = 10; GameObject icon = UIFactory.CreateObject("Icon", p); icon.AddComponent<LayoutElement>().flexibleHeight = 1; UIFactory.CreateText("\uD83C\uDF83", icon, 80, new Color(1f, 0.5f, 0f), Vector2.zero, Vector2.zero); UIFactory.CreateText(label, p, 13, Color.gray, Vector2.zero, new Vector2(0, 20)); };
            CreatePumpkin("Before"); CreatePumpkin("After");
            UIFactory.CreateText("White Underbase Choke shrinks the white ink layer slightly compared to the CMYK layer, preventing unwanted white outlines.", content, 14, Color.gray, Vector2.zero, new Vector2(0, 60), TextAnchor.MiddleLeft);
            GameObject footer = UIFactory.CreateObject("Footer", content); footer.AddComponent<LayoutElement>().minHeight = 50;
            UIFactory.CreateButton("OK", footer, new Vector2(200, 0), new Vector2(120, 40), new Color(0.15f, 0.15f, 0.18f), Color.white).GetComponent<Button>().onClick.AddListener(() => Object.Destroy(content.transform.parent.gameObject));
        }
    }
}

