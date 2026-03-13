using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PocoRender.UI.Core;

namespace PocoRender.UI.Modules
{
    public static class CanvasWorkspaceBuilder
    {
        public static (RectTransform, RectTransform) CreateRulers(GameObject workspace)
        {
            GameObject bRuler = UIFactory.CreateObject("BottomRuler", workspace);
            RectTransform brRect = bRuler.GetComponent<RectTransform>();
            brRect.anchorMin = new Vector2(0, 0); brRect.anchorMax = new Vector2(1, 0);
            brRect.sizeDelta = new Vector2(0, 25); brRect.anchoredPosition = new Vector2(0, 12);
            bRuler.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
            bRuler.transform.SetAsLastSibling(); 

            GameObject rRuler = UIFactory.CreateObject("RightRuler", workspace);
            RectTransform rrRect = rRuler.GetComponent<RectTransform>();
            rrRect.anchorMin = new Vector2(1, 0); rrRect.anchorMax = new Vector2(1, 1);
            rrRect.sizeDelta = new Vector2(25, 0); rrRect.anchoredPosition = new Vector2(-12, 0);
            rRuler.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
            rRuler.transform.SetAsLastSibling();

            return (brRect, rrRect);
        }

        public static void CreateBottomControls(GameObject workspace, CanvasController controller)
        {
            GameObject ctrlBar = UIFactory.CreateObject("ZoomControls", workspace);
            RectTransform cbRect = ctrlBar.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 0); cbRect.anchorMax = new Vector2(0.5f, 0);
            cbRect.sizeDelta = new Vector2(650, 40); cbRect.anchoredPosition = new Vector2(0, 80); 
            ctrlBar.AddComponent<Image>().color = new Color(1,1,1,0.95f);
            ctrlBar.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            ctrlBar.transform.SetAsLastSibling();

            HorizontalLayoutGroup hlg = ctrlBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2; hlg.padding = new RectOffset(4, 8, 5, 5); hlg.childAlignment = TextAnchor.MiddleCenter;

            // Undo / Redo buttons (leftmost, compact)
            Sprite undoSpr = Resources.Load<Sprite>("EditIcons/p_undo");
            Sprite redoSpr = Resources.Load<Sprite>("EditIcons/p_redo");

            GameObject undoBtn = UIFactory.CreateButton("", ctrlBar, Vector2.zero, new Vector2(20, 20), Color.white, Color.black);
            LayoutElement undoLe = undoBtn.AddComponent<LayoutElement>(); undoLe.minWidth = 20; undoLe.preferredWidth = 20; undoLe.minHeight = 20; undoLe.preferredHeight = 20;
            if (undoSpr != null) { Image ui = undoBtn.GetComponent<Image>(); ui.sprite = undoSpr; ui.type = Image.Type.Simple; ui.preserveAspect = true; ui.color = Color.white; }
            else { var t = undoBtn.GetComponentInChildren<Text>(); if (t != null) t.text = "\u21A9"; }
            undoBtn.GetComponent<Button>().onClick.AddListener(() => controller.Undo());
            undoBtn.AddComponent<UITooltip>().text = "Undo";

            GameObject redoBtn = UIFactory.CreateButton("", ctrlBar, Vector2.zero, new Vector2(20, 20), Color.white, Color.black);
            LayoutElement redoLe = redoBtn.AddComponent<LayoutElement>(); redoLe.minWidth = 20; redoLe.preferredWidth = 20; redoLe.minHeight = 20; redoLe.preferredHeight = 20;
            if (redoSpr != null) { Image ri = redoBtn.GetComponent<Image>(); ri.sprite = redoSpr; ri.type = Image.Type.Simple; ri.preserveAspect = true; ri.color = Color.white; }
            else { var t = redoBtn.GetComponentInChildren<Text>(); if (t != null) t.text = "\u21AA"; }
            redoBtn.GetComponent<Button>().onClick.AddListener(() => controller.Redo());
            redoBtn.AddComponent<UITooltip>().text = "Redo";

            // Separator (p_chart-v-line icon)
            Sprite vLineSpr = Resources.Load<Sprite>("EditIcons/p_chart-v-line");
            GameObject zSep = UIFactory.CreateObject("Sep", ctrlBar);
            zSep.GetComponent<RectTransform>().sizeDelta = new Vector2(8, 20);
            Image zSepImg = zSep.AddComponent<Image>();
            if (vLineSpr != null) { zSepImg.sprite = vLineSpr; zSepImg.preserveAspect = true; zSepImg.color = new Color(0.7f, 0.7f, 0.7f); }
            else { zSepImg.color = new Color(0.82f, 0.82f, 0.82f); }
            LayoutElement zSepLe = zSep.AddComponent<LayoutElement>(); zSepLe.minWidth = 8; zSepLe.preferredWidth = 8;

            UIFactory.CreateButton("-", ctrlBar, Vector2.zero, new Vector2(28, 28), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ChangeZoom(-0.2f));
            
            GameObject ddObj = UIFactory.CreateObject("ZoomDropdown", ctrlBar);
            ddObj.AddComponent<LayoutElement>().minWidth = 70;
            Image ddImg = ddObj.AddComponent<Image>(); ddImg.color = new Color(0.95f, 0.95f, 0.95f);
            Dropdown dd = ddObj.AddComponent<Dropdown>();
            dd.targetGraphic = ddImg;
            controller.zoomDropdown = dd;
            UIFactory.AddDropdownArrow(ddObj, 12f);
            
            // Dropdown Template
            GameObject template = UIFactory.CreateObject("Template", ddObj);
            template.AddComponent<CanvasGroup>();
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 1); templateRect.anchorMax = new Vector2(1, 1);
            templateRect.pivot = new Vector2(0.5f, 0); templateRect.sizeDelta = new Vector2(-10, 180);
            templateRect.anchoredPosition = new Vector2(0, 2);
            template.AddComponent<Image>().color = Color.white;
            template.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            
            ScrollRect templateSr = template.AddComponent<ScrollRect>();
            templateSr.horizontal = false;
            templateSr.vertical = true;
            templateSr.movementType = ScrollRect.MovementType.Clamped;
            templateSr.scrollSensitivity = 25;

            GameObject viewport = UIFactory.CreateObject("Viewport", template);
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            UIFactory.Stretch(vpRect);
            vpRect.offsetMax = new Vector2(-8, 0); 
            viewport.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.AddComponent<RectMask2D>();

            GameObject sbarObj = UIFactory.CreateObject("Scrollbar", template);
            RectTransform sbRect = sbarObj.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1, 0); sbRect.anchorMax = new Vector2(1, 1);
            sbRect.sizeDelta = new Vector2(4, -4);
            sbRect.anchoredPosition = new Vector2(-2, 0); 
            Image sbImg = sbarObj.AddComponent<Image>(); sbImg.color = new Color(0.98f, 0.98f, 0.98f, 0.5f);
            Scrollbar sb = sbarObj.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            
            GameObject handle = UIFactory.CreateObject("Handle", sbarObj);
            Image hImg = handle.AddComponent<Image>(); hImg.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
            sb.handleRect = handle.GetComponent<RectTransform>();
            UIFactory.Stretch(sb.handleRect);
            templateSr.verticalScrollbar = sb;

            GameObject content = UIFactory.CreateObject("Content", viewport);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 4;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            templateSr.viewport = vpRect;
            templateSr.content = contentRect;
            dd.template = templateRect;
            
            GameObject itemTemplate = UIFactory.CreateObject("Item", content);
            itemTemplate.AddComponent<LayoutElement>().minHeight = 34;
            Toggle itemToggle = itemTemplate.AddComponent<Toggle>();
            
            GameObject itemBg = UIFactory.CreateObject("ItemBackground", itemTemplate);
            UIFactory.Stretch(itemBg.GetComponent<RectTransform>());
            Image itemBgImg = itemBg.AddComponent<Image>(); itemBgImg.color = new Color(0, 0, 0, 0);
            
            GameObject itemLabelObj = new GameObject("ItemLabel", typeof(RectTransform));
            itemLabelObj.transform.SetParent(itemTemplate.transform, false);
            Text itemText = itemLabelObj.AddComponent<Text>();
            itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemText.fontSize = 13;
            itemText.color = Color.black;
            itemText.alignment = TextAnchor.MiddleLeft;
            RectTransform itRt = itemText.rectTransform;
            itRt.anchorMin = Vector2.zero; itRt.anchorMax = Vector2.one;
            itRt.offsetMin = new Vector2(30, 0); itRt.offsetMax = new Vector2(-5, 0);
            
            GameObject itemCheck = UIFactory.CreateObject("ItemCheckmark", itemTemplate);
            RectTransform checkRect = itemCheck.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 0.5f); checkRect.anchorMax = new Vector2(0, 0.5f);
            checkRect.sizeDelta = new Vector2(18, 18); checkRect.anchoredPosition = new Vector2(15, 0);
            Image checkImg = itemCheck.AddComponent<Image>();
            Sprite checkSprite = Resources.Load<Sprite>("EditIcons/p_check");
            if (checkSprite != null) { checkImg.sprite = checkSprite; checkImg.color = Color.white; checkImg.preserveAspect = true; }
            else { checkImg.color = UIFactory.COLOR_ACCENT_GREEN; }
            
            dd.itemText = itemText;
            itemToggle.targetGraphic = itemBgImg; itemToggle.graphic = checkImg;
            template.SetActive(false);

            Text label = UIFactory.CreateText("100%", ddObj, 12, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            label.rectTransform.offsetMax = new Vector2(-20, 0);
            dd.captionText = label;
            
            var options = new System.Collections.Generic.List<string>{
                "10%","25%","50%","75%","100%","125%","150%","175%","200%",
                "250%","300%","350%","400%","500%","1000%","1500%","2000%"
            };
            dd.ClearOptions();
            dd.AddOptions(options);
            dd.onValueChanged.AddListener((idx) => {
                string val = options[idx].Replace("%", "");
                if(float.TryParse(val, out float f)) controller.SetZoom(f/100f);
            });

            UIFactory.CreateButton("+", ctrlBar, Vector2.zero, new Vector2(30, 30), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ChangeZoom(0.2f));

            // Separator between zoom and hand/fit (p_chart-v-line icon)
            GameObject zSep2 = UIFactory.CreateObject("Sep2", ctrlBar);
            zSep2.GetComponent<RectTransform>().sizeDelta = new Vector2(8, 20);
            Image zSep2Img = zSep2.AddComponent<Image>();
            if (vLineSpr != null) { zSep2Img.sprite = vLineSpr; zSep2Img.preserveAspect = true; zSep2Img.color = new Color(0.7f, 0.7f, 0.7f); }
            else { zSep2Img.color = new Color(0.82f, 0.82f, 0.82f); }
            LayoutElement zSep2Le = zSep2.AddComponent<LayoutElement>(); zSep2Le.minWidth = 8; zSep2Le.preferredWidth = 8;

            Button hb = UIFactory.CreateButton("\u270B", ctrlBar, Vector2.zero, new Vector2(26, 26), Color.white, Color.black).GetComponent<Button>();
            LayoutElement hbLe = hb.gameObject.AddComponent<LayoutElement>(); hbLe.minWidth = 26; hbLe.preferredWidth = 26; hbLe.minHeight = 26; hbLe.preferredHeight = 26;
            hb.gameObject.AddComponent<UITooltip>().text = "Hand Tool";
            hb.onClick.AddListener(() => {
                controller.ToggleHandTool(!controller.IsHandToolActive());
                hb.GetComponent<Image>().color = controller.IsHandToolActive() ? new Color(0.8f, 1f, 0.8f) : Color.white;
            });

            GameObject fitBtn = UIFactory.CreateButton("Fit", ctrlBar, Vector2.zero, new Vector2(32, 26), Color.white, Color.black);
            LayoutElement fitLe = fitBtn.AddComponent<LayoutElement>(); fitLe.minWidth = 32; fitLe.preferredWidth = 32; fitLe.minHeight = 26; fitLe.preferredHeight = 26;
            fitBtn.GetComponent<Button>().onClick.AddListener(() => controller.ZoomToFit());
        }

        public static void CreateContextToolbar(GameObject workspace, CanvasController controller)
        {
            GameObject ct = UIFactory.CreateObject("ContextToolbar", workspace);
            RectTransform ctRect = ct.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0.5f, 1f); ctRect.anchorMax = new Vector2(0.5f, 1f); ctRect.pivot = new Vector2(0.5f, 1f);
            ctRect.sizeDelta = new Vector2(600, 34); ctRect.anchoredPosition = new Vector2(0, -4); 
            Image ctBg = ct.AddComponent<Image>();
            ctBg.color = Color.white;
            Sprite cardSprite = UIFactory.CreateRoundedRectSprite(128, 64, 10);
            if (cardSprite != null) { ctBg.sprite = cardSprite; ctBg.type = Image.Type.Sliced; }
            ct.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            HorizontalLayoutGroup ctHlg = ct.AddComponent<HorizontalLayoutGroup>();
            ctHlg.spacing = 3; ctHlg.padding = new RectOffset(12, 12, 3, 3); ctHlg.childAlignment = TextAnchor.MiddleCenter;
            ctHlg.childForceExpandWidth = false;

            Button cropBtn = CreateToolbarIconButton(ct, "EditIcons/p_edit_crop", "Crop");
            cropBtn.onClick.AddListener(() => controller.ToggleCropTool());

            Button eraserBtn = CreateToolbarIconButton(ct, "EditIcons/e_edit_eraser", "Eraser");
            eraserBtn.onClick.AddListener(() => controller.ToggleEraserTool());

            Button opacityBtn = CreateToolbarIconButton(ct, "EditIcons/p_edit_opacity", "Opacity");
            opacityBtn.onClick.AddListener(() => controller.ToggleOpacityTool());

            Button splitBtn = CreateToolbarIconButton(ct, "EditIcons/p_edit_cell_split", "Image Splitting");
            splitBtn.onClick.AddListener(() => controller.ToggleSplitTool());

            // Adjustment text button with icon
            Button adjBtn = CreateToolbarIconButton(ct, "EditIcons/p_curve-adjustment", "Adjustment");
            adjBtn.onClick.AddListener(() => controller.ToggleAdjustmentPanel());

            // Separator between icon tools and text tools
            GameObject sep = UIFactory.CreateObject("Separator", ct);
            sep.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 20);
            sep.AddComponent<Image>().color = new Color(0.82f, 0.82f, 0.82f);
            sep.AddComponent<LayoutElement>().minWidth = 1;

            // AI Remove (before UpScaler): icon + English label "AI Remove", applies background removal
            Button aiRemoveBtn = CreateToolbarIconButton(ct, "EditIcons/p_ai_remove", "AI Remove");
            aiRemoveBtn.onClick.AddListener(() => controller.ApplyAIRemoveBackground());

            string[] tools = { "UpScaler", "Cutout", "Outline" };
            foreach (var tool in tools)
                UIFactory.CreateButton(tool, ct, Vector2.zero, new Vector2(0, 30), Color.white, Color.black).AddComponent<LayoutElement>().flexibleWidth = 1;

            ct.SetActive(false); controller.contextToolbar = ct;
            ct.transform.SetAsLastSibling();

            GameObject cropPanel = UIFactory.CreateObject("CropOptionsPanel", workspace);
            RectTransform cpRect = cropPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.5f, 1f); cpRect.anchorMax = new Vector2(0.5f, 1f); cpRect.pivot = new Vector2(0.5f, 1f);
            cpRect.sizeDelta = new Vector2(540, 56); cpRect.anchoredPosition = new Vector2(0, -42);
            Image cpBg = cropPanel.AddComponent<Image>();
            cpBg.color = Color.white;
            if (cardSprite != null) { cpBg.sprite = cardSprite; cpBg.type = Image.Type.Sliced; }
            cropPanel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);

            HorizontalLayoutGroup cpHlg = cropPanel.AddComponent<HorizontalLayoutGroup>();
            cpHlg.spacing = 8;
            cpHlg.padding = new RectOffset(6, 6, 3, 3);
            cpHlg.childAlignment = TextAnchor.MiddleCenter;
            cpHlg.childControlWidth = false;
            cpHlg.childControlHeight = false;
            cpHlg.childForceExpandWidth = false;
            cpHlg.childForceExpandHeight = false;

            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_free", "Free", () => controller.SetCropPreset(CropPresetType.Free));
            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_1_1", "1:1", () => controller.SetCropPreset(CropPresetType.Ratio1x1));
            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_9_16", "9:16", () => controller.SetCropPreset(CropPresetType.Ratio9x16));
            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_16_9", "16:9", () => controller.SetCropPreset(CropPresetType.Ratio16x9));
            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_ellipse", "Ellipse", () => controller.SetCropPreset(CropPresetType.Ellipse));
            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_triangle", "Triangle", () => controller.SetCropPreset(CropPresetType.Triangle));
            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_star", "Star", () => controller.SetCropPreset(CropPresetType.Star));
            CreateCropPresetButton(cropPanel, "EditIcons/p_edit_heart", "Heart", () => controller.SetCropPreset(CropPresetType.Heart));

            var cancelBtn = UIFactory.CreateButton("Cancel", cropPanel, Vector2.zero, new Vector2(56, 28), Color.white, Color.black);
            cancelBtn.GetComponent<Button>().onClick.AddListener(() => controller.CancelCropTool());
            cancelBtn.GetComponentInChildren<Text>().fontSize = 10;

            var applyBtn = UIFactory.CreateButton("Apply", cropPanel, Vector2.zero, new Vector2(56, 28), new Color(0.1f, 0.1f, 0.1f), Color.white);
            applyBtn.GetComponent<Button>().onClick.AddListener(() => controller.ApplyCropTool());
            applyBtn.GetComponentInChildren<Text>().fontSize = 10;

            cropPanel.SetActive(false);
            cropPanel.transform.SetAsLastSibling();
            controller.cropOptionsPanel = cropPanel;

            CreateEraserOptionsPanel(workspace, controller, cardSprite);
        }

        private static void CreateEraserOptionsPanel(GameObject workspace, CanvasController controller, Sprite cardSprite)
        {
            GameObject eraserPanel = UIFactory.CreateObject("EraserOptionsPanel", workspace);
            RectTransform epRect = eraserPanel.GetComponent<RectTransform>();
            epRect.anchorMin = new Vector2(0.5f, 1f); epRect.anchorMax = new Vector2(0.5f, 1f); epRect.pivot = new Vector2(0.5f, 1f);
            epRect.sizeDelta = new Vector2(280, 78); epRect.anchoredPosition = new Vector2(0, -42);
            Image epBg = eraserPanel.AddComponent<Image>();
            epBg.color = Color.white;
            if (cardSprite != null) { epBg.sprite = cardSprite; epBg.type = Image.Type.Sliced; }
            eraserPanel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);

            VerticalLayoutGroup epVlg = eraserPanel.AddComponent<VerticalLayoutGroup>();
            epVlg.spacing = 4;
            epVlg.padding = new RectOffset(12, 12, 8, 6);
            epVlg.childAlignment = TextAnchor.MiddleCenter;
            epVlg.childControlWidth = true;
            epVlg.childControlHeight = false;
            epVlg.childForceExpandWidth = true;
            epVlg.childForceExpandHeight = false;

            // --- Slider row ---
            GameObject sliderRow = UIFactory.CreateObject("SliderRow", eraserPanel);
            sliderRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
            HorizontalLayoutGroup srHlg = sliderRow.AddComponent<HorizontalLayoutGroup>();
            srHlg.spacing = 6; srHlg.childAlignment = TextAnchor.MiddleCenter;
            srHlg.childControlWidth = false; srHlg.childControlHeight = false;
            srHlg.childForceExpandWidth = false; srHlg.childForceExpandHeight = false;

            // "Size" label
            GameObject labelObj = UIFactory.CreateText("Size", sliderRow, 11, new Color(0.3f, 0.3f, 0.3f), Vector2.zero, new Vector2(30, 20));
            labelObj.GetComponent<Text>().alignment = TextAnchor.MiddleRight;

            // Slider
            GameObject sliderObj = CreateEraserSlider(sliderRow);

            // Value text
            GameObject valObj = UIFactory.CreateText("20", sliderRow, 11, new Color(0.2f, 0.2f, 0.2f), Vector2.zero, new Vector2(30, 20));
            Text valText = valObj.GetComponent<Text>();
            valText.alignment = TextAnchor.MiddleCenter;

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.onValueChanged.AddListener((val) =>
            {
                int size = Mathf.RoundToInt(val);
                valText.text = size.ToString();
                controller.SetEraserBrushSize(size);
            });
            slider.value = 20f;

            // --- Button row ---
            GameObject btnRow = UIFactory.CreateObject("ButtonRow", eraserPanel);
            btnRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
            HorizontalLayoutGroup brHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            brHlg.spacing = 8; brHlg.childAlignment = TextAnchor.MiddleCenter;
            brHlg.childControlWidth = false; brHlg.childControlHeight = false;
            brHlg.childForceExpandWidth = false; brHlg.childForceExpandHeight = false;

            GameObject spacer = UIFactory.CreateObject("Spacer", btnRow);
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            var exitBtn = UIFactory.CreateButton("Exit", btnRow, Vector2.zero, new Vector2(56, 26), Color.white, Color.black);
            exitBtn.GetComponent<Button>().onClick.AddListener(() => controller.ExitEraserTool());
            exitBtn.GetComponentInChildren<Text>().fontSize = 10;

            GameObject spacer2 = UIFactory.CreateObject("Spacer2", btnRow);
            spacer2.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            spacer2.AddComponent<LayoutElement>().flexibleWidth = 1;

            eraserPanel.SetActive(false);
            eraserPanel.transform.SetAsLastSibling();
            controller.eraserOptionsPanel = eraserPanel;

            CreateOpacityOptionsPanel(workspace, controller, cardSprite);
        }

        private static void CreateOpacityOptionsPanel(GameObject workspace, CanvasController controller, Sprite cardSprite)
        {
            GameObject opacityPanel = UIFactory.CreateObject("OpacityOptionsPanel", workspace);
            RectTransform opRect = opacityPanel.GetComponent<RectTransform>();
            opRect.anchorMin = new Vector2(0.5f, 1f); opRect.anchorMax = new Vector2(0.5f, 1f); opRect.pivot = new Vector2(0.5f, 1f);
            opRect.sizeDelta = new Vector2(280, 48); opRect.anchoredPosition = new Vector2(0, -42);
            Image opBg = opacityPanel.AddComponent<Image>();
            opBg.color = Color.white;
            if (cardSprite != null) { opBg.sprite = cardSprite; opBg.type = Image.Type.Sliced; }
            opacityPanel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);

            HorizontalLayoutGroup opHlg = opacityPanel.AddComponent<HorizontalLayoutGroup>();
            opHlg.spacing = 6;
            opHlg.padding = new RectOffset(12, 12, 8, 8);
            opHlg.childAlignment = TextAnchor.MiddleCenter;
            opHlg.childControlWidth = false;
            opHlg.childControlHeight = false;
            opHlg.childForceExpandWidth = false;
            opHlg.childForceExpandHeight = false;

            GameObject labelObj = UIFactory.CreateText("Opacity", opacityPanel, 11, new Color(0.3f, 0.3f, 0.3f), Vector2.zero, new Vector2(48, 20));
            labelObj.GetComponent<Text>().alignment = TextAnchor.MiddleRight;

            GameObject sliderObj = CreateOpacitySlider(opacityPanel);

            GameObject valObj = UIFactory.CreateText("100%", opacityPanel, 11, new Color(0.2f, 0.2f, 0.2f), Vector2.zero, new Vector2(36, 20));
            Text valText = valObj.GetComponent<Text>();
            valText.alignment = TextAnchor.MiddleCenter;

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.onValueChanged.AddListener((val) =>
            {
                int pct = Mathf.RoundToInt(val);
                valText.text = pct + "%";
                controller.SetLayerOpacity(pct / 100f);
            });

            int initialPct = Mathf.RoundToInt(controller.GetCurrentLayerOpacity() * 100f);
            slider.value = initialPct;

            opacityPanel.SetActive(false);
            opacityPanel.transform.SetAsLastSibling();
            controller.opacityOptionsPanel = opacityPanel;
            controller.opacitySlider = slider;

            CreateSplitOptionsPanel(workspace, controller, cardSprite);
        }

        private static void CreateSplitOptionsPanel(GameObject workspace, CanvasController controller, Sprite cardSprite)
        {
            GameObject splitPanel = UIFactory.CreateObject("SplitOptionsPanel", workspace);
            RectTransform spRect = splitPanel.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 1f); spRect.anchorMax = new Vector2(0.5f, 1f); spRect.pivot = new Vector2(0.5f, 1f);
            spRect.sizeDelta = new Vector2(240, 260); spRect.anchoredPosition = new Vector2(0, -42);
            Image spBg = splitPanel.AddComponent<Image>();
            spBg.color = Color.white;
            if (cardSprite != null) { spBg.sprite = cardSprite; spBg.type = Image.Type.Sliced; }
            splitPanel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);

            VerticalLayoutGroup spVlg = splitPanel.AddComponent<VerticalLayoutGroup>();
            spVlg.spacing = 6;
            spVlg.padding = new RectOffset(12, 12, 10, 10);
            spVlg.childAlignment = TextAnchor.MiddleCenter;
            spVlg.childControlWidth = true;
            spVlg.childControlHeight = false;
            spVlg.childForceExpandWidth = true;
            spVlg.childForceExpandHeight = false;

            // Title
            GameObject titleObj = UIFactory.CreateText("Image Splitting", splitPanel, 13, new Color(0.15f, 0.15f, 0.15f),
                Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20);

            // --- Preset grid: Row 1 ---
            GameObject row1 = UIFactory.CreateObject("PresetRow1", splitPanel);
            row1.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 58);
            HorizontalLayoutGroup r1Hlg = row1.AddComponent<HorizontalLayoutGroup>();
            r1Hlg.spacing = 8; r1Hlg.childAlignment = TextAnchor.MiddleCenter;
            r1Hlg.childControlWidth = false; r1Hlg.childControlHeight = false;
            r1Hlg.childForceExpandWidth = false; r1Hlg.childForceExpandHeight = false;

            CreateSplitPresetButton(row1, "EditIcons/p_split_2_2", "2*2", 2, 2, controller);
            CreateSplitPresetButton(row1, "EditIcons/p_split_3_1", "3*1", 3, 1, controller);
            CreateSplitPresetButton(row1, "EditIcons/p_split2_3", "2*3", 2, 3, controller);

            // --- Preset grid: Row 2 ---
            GameObject row2 = UIFactory.CreateObject("PresetRow2", splitPanel);
            row2.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 58);
            HorizontalLayoutGroup r2Hlg = row2.AddComponent<HorizontalLayoutGroup>();
            r2Hlg.spacing = 8; r2Hlg.childAlignment = TextAnchor.MiddleCenter;
            r2Hlg.childControlWidth = false; r2Hlg.childControlHeight = false;
            r2Hlg.childForceExpandWidth = false; r2Hlg.childForceExpandHeight = false;

            CreateSplitPresetButton(row2, "EditIcons/p_split_3_3", "3*3", 3, 3, controller);
            CreateSplitPresetButton(row2, "EditIcons/p_split_1_3", "1*3", 1, 3, controller);
            CreateSplitPresetButton(row2, "EditIcons/p_split_3_2", "3*2", 3, 2, controller);

            // --- Customize row ---
            GameObject customRow = UIFactory.CreateObject("CustomRow", splitPanel);
            customRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
            HorizontalLayoutGroup crHlg = customRow.AddComponent<HorizontalLayoutGroup>();
            crHlg.spacing = 5; crHlg.childAlignment = TextAnchor.MiddleCenter;
            crHlg.childControlWidth = false; crHlg.childControlHeight = false;
            crHlg.childForceExpandWidth = false; crHlg.childForceExpandHeight = false;

            UIFactory.CreateText("Customize:", customRow, 11, new Color(0.35f, 0.35f, 0.35f), Vector2.zero, new Vector2(68, 22));

            InputField colsInput = CreateSmallInputField(customRow, "3");
            controller.splitColsInput = colsInput;
            colsInput.onValueChanged.AddListener((_) => controller.UpdateSplitInfo());

            UIFactory.CreateText("*", customRow, 13, new Color(0.3f, 0.3f, 0.3f), Vector2.zero, new Vector2(12, 22), TextAnchor.MiddleCenter);

            InputField rowsInput = CreateSmallInputField(customRow, "4");
            controller.splitRowsInput = rowsInput;
            rowsInput.onValueChanged.AddListener((_) => controller.UpdateSplitInfo());

            // --- Slice info ---
            GameObject infoObj = UIFactory.CreateText("Each Slice: --", splitPanel, 10, new Color(0.5f, 0.5f, 0.5f),
                Vector2.zero, new Vector2(0, 18), TextAnchor.MiddleLeft);
            controller.splitInfoText = infoObj.GetComponent<Text>();

            // --- OK button ---
            var okBtn = UIFactory.CreateButton("OK", splitPanel, Vector2.zero, new Vector2(0, 32), new Color(0.31f, 0.86f, 0.45f), Color.white);
            okBtn.GetComponent<Button>().onClick.AddListener(() => controller.ApplySplitTool());
            okBtn.GetComponentInChildren<Text>().fontSize = 13;
            okBtn.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

            splitPanel.SetActive(false);
            splitPanel.transform.SetAsLastSibling();
            controller.splitOptionsPanel = splitPanel;

            CreateAdjustmentPanel(workspace, controller, cardSprite);
        }

        private static void CreateAdjustmentPanel(GameObject workspace, CanvasController controller, Sprite cardSprite)
        {
            GameObject panel = UIFactory.CreateObject("AdjustmentPanel", workspace);
            RectTransform pRect = panel.GetComponent<RectTransform>();
            UIFactory.Stretch(pRect);
            panel.AddComponent<LayoutElement>().flexibleHeight = 1;

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Header row
            GameObject header = UIFactory.CreateObject("Header", panel);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
            HorizontalLayoutGroup hdrHlg = header.AddComponent<HorizontalLayoutGroup>();
            hdrHlg.childAlignment = TextAnchor.MiddleLeft;
            hdrHlg.childControlWidth = true; hdrHlg.childControlHeight = true;
            hdrHlg.childForceExpandWidth = false; hdrHlg.childForceExpandHeight = false;

            GameObject titleObj = UIFactory.CreateText("Adjustment", header, 16, new Color(0.15f, 0.15f, 0.15f),
                Vector2.zero, new Vector2(0, 28), TextAnchor.MiddleLeft, FontStyle.Bold);
            titleObj.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Close button
            GameObject closeObj = UIFactory.CreateObject("CloseBtn", header);
            LayoutElement closeLe = closeObj.AddComponent<LayoutElement>();
            closeLe.minWidth = 22; closeLe.minHeight = 22; closeLe.preferredWidth = 22; closeLe.preferredHeight = 22;
            Image closeImg = closeObj.AddComponent<Image>();
            Sprite closeSprite = Resources.Load<Sprite>("EditIcons/p_close");
            if (closeSprite != null) { closeImg.sprite = closeSprite; closeImg.preserveAspect = true; closeImg.color = Color.white; }
            else closeImg.color = new Color(0.5f, 0.5f, 0.5f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() => controller.CloseAdjustmentPanel());

            // Slider rows
            string[] paramNames = { "Brightness", "Contrast", "Saturation", "Hue", "Temperature", "Tint", "Highlights", "Shadows", "Sharpness" };
            Slider[] sliders = new Slider[paramNames.Length];
            Text[] valueTexts = new Text[paramNames.Length];

            for (int i = 0; i < paramNames.Length; i++)
            {
                int idx = i;
                GameObject row = UIFactory.CreateObject("Row_" + paramNames[i], panel);
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 34);
                HorizontalLayoutGroup rowHlg = row.AddComponent<HorizontalLayoutGroup>();
                rowHlg.spacing = 4; rowHlg.childAlignment = TextAnchor.MiddleCenter;
                rowHlg.padding = new RectOffset(2, 2, 0, 0);
                rowHlg.childControlWidth = true; rowHlg.childControlHeight = true;
                rowHlg.childForceExpandWidth = false; rowHlg.childForceExpandHeight = false;

                GameObject labelObj = UIFactory.CreateText(paramNames[i], row, 11, new Color(0.3f, 0.3f, 0.3f),
                    Vector2.zero, new Vector2(68, 24), TextAnchor.MiddleLeft);
                LayoutElement labelLe = labelObj.AddComponent<LayoutElement>();
                labelLe.minWidth = 68; labelLe.preferredWidth = 68;

                GameObject sliderObj = CreateAdjustmentSlider(row);
                LayoutElement sliderLe = sliderObj.AddComponent<LayoutElement>();
                sliderLe.flexibleWidth = 1; sliderLe.minWidth = 60; sliderLe.minHeight = 18; sliderLe.preferredHeight = 18;

                GameObject valObj = UIFactory.CreateText("0", row, 11, new Color(0.4f, 0.4f, 0.4f),
                    Vector2.zero, new Vector2(28, 24), TextAnchor.MiddleRight);
                LayoutElement valLe2 = valObj.AddComponent<LayoutElement>();
                valLe2.minWidth = 28; valLe2.preferredWidth = 28;
                Text valText = valObj.GetComponent<Text>();
                valueTexts[idx] = valText;

                Slider slider = sliderObj.GetComponent<Slider>();
                bool isSharpness = (idx == paramNames.Length - 1);
                slider.minValue = isSharpness ? 0 : -100;
                slider.maxValue = 100;
                slider.wholeNumbers = true;
                slider.value = 0;
                sliders[idx] = slider;

                slider.onValueChanged.AddListener((v) =>
                {
                    valText.text = Mathf.RoundToInt(v).ToString();
                    controller.ScheduleAdjustment();
                });
            }

            controller.adjustmentSliders = sliders;
            controller.adjustmentValueTexts = valueTexts;

            // Spacer before Restore
            GameObject restoreSpacer = UIFactory.CreateObject("RestoreSpacer", panel);
            restoreSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 8);

            // Restore button
            var restoreBtn = UIFactory.CreateButton("Restore", panel, Vector2.zero, new Vector2(0, 34), new Color(0.96f, 0.96f, 0.96f), new Color(0.25f, 0.25f, 0.25f));
            restoreBtn.GetComponentInChildren<Text>().fontSize = 13;
            restoreBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 34);
            restoreBtn.GetComponent<Button>().onClick.AddListener(() => controller.RestoreAdjustments());

            panel.SetActive(false);
            panel.transform.SetAsLastSibling();
            controller.adjustmentPanel = panel;
        }

        private static GameObject CreateAdjustmentSlider(GameObject parent)
        {
            GameObject sliderObj = UIFactory.CreateObject("AdjSlider", parent);
            sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 20);

            Sprite trackSprite = UIFactory.CreateRoundedRectSprite(64, 8, 4);

            GameObject bgTrack = UIFactory.CreateObject("Background", sliderObj);
            RectTransform bgRt = bgTrack.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f); bgRt.anchorMax = new Vector2(1, 0.5f);
            bgRt.sizeDelta = new Vector2(0, 3); bgRt.anchoredPosition = Vector2.zero;
            Image bgImg = bgTrack.AddComponent<Image>();
            bgImg.color = new Color(0.88f, 0.88f, 0.88f);
            if (trackSprite != null) { bgImg.sprite = trackSprite; bgImg.type = Image.Type.Sliced; }

            // Green center fill (from zero-point to handle)
            GameObject centerFill = UIFactory.CreateObject("CenterFill", sliderObj);
            RectTransform cfRt = centerFill.GetComponent<RectTransform>();
            cfRt.anchorMin = new Vector2(0.5f, 0.5f); cfRt.anchorMax = new Vector2(0.5f, 0.5f);
            cfRt.sizeDelta = new Vector2(0, 3); cfRt.anchoredPosition = Vector2.zero;
            Image cfImg = centerFill.AddComponent<Image>();
            cfImg.color = new Color(0.31f, 0.86f, 0.45f);
            if (trackSprite != null) { cfImg.sprite = trackSprite; cfImg.type = Image.Type.Sliced; }

            // Invisible fill (required by Slider component)
            GameObject fillArea = UIFactory.CreateObject("Fill Area", sliderObj);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.5f); faRt.anchorMax = new Vector2(1, 0.5f);
            faRt.sizeDelta = new Vector2(-14, 3);
            GameObject fill = UIFactory.CreateObject("Fill", fillArea);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1);
            fillRt.sizeDelta = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0, 0, 0, 0);

            GameObject handleArea = UIFactory.CreateObject("Handle Slide Area", sliderObj);
            RectTransform haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(7, 0); haRt.offsetMax = new Vector2(-7, 0);

            GameObject handle = UIFactory.CreateObject("Handle", handleArea);
            RectTransform hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(14, 14);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            handleImg.preserveAspect = true;
            Sprite handleSprite = UIFactory.CreateRoundedRectSprite(32, 32, 16);
            if (handleSprite != null) { handleImg.sprite = handleSprite; handleImg.type = Image.Type.Simple; }
            handle.AddComponent<Outline>().effectColor = new Color(0.31f, 0.86f, 0.45f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            AdjustmentCenterFill acf = sliderObj.AddComponent<AdjustmentCenterFill>();
            acf.slider = slider;
            acf.centerFillRect = cfRt;
            acf.trackRect = bgRt;

            return sliderObj;
        }

        private static void CreateSplitPresetButton(GameObject parent, string iconPath, string label, int cols, int rows, CanvasController controller)
        {
            GameObject item = UIFactory.CreateObject("Split_" + label, parent);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(62, 58);

            VerticalLayoutGroup vlg = item.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2; vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            GameObject btnObj = UIFactory.CreateObject("Icon", item);
            btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 36);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.96f, 0.96f, 0.96f);
            Sprite spr = Resources.Load<Sprite>(iconPath);
            if (spr != null)
            {
                btnImg.sprite = spr;
                btnImg.type = Image.Type.Simple;
                btnImg.preserveAspect = true;
                btnImg.color = Color.white;
            }
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                if (controller.splitColsInput != null) controller.splitColsInput.text = cols.ToString();
                if (controller.splitRowsInput != null) controller.splitRowsInput.text = rows.ToString();
                controller.UpdateSplitInfo();
            });

            GameObject labelObj = UIFactory.CreateText(label, item, 10, new Color(0.4f, 0.4f, 0.4f),
                Vector2.zero, new Vector2(0, 14), TextAnchor.MiddleCenter);
        }

        private static InputField CreateSmallInputField(GameObject parent, string defaultText)
        {
            GameObject obj = UIFactory.CreateObject("InputField", parent);
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(34, 22);
            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.96f, 0.96f, 0.96f);

            GameObject textObj = UIFactory.CreateText(defaultText, obj, 12, new Color(0.15f, 0.15f, 0.15f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            UIFactory.Stretch(textObj.GetComponent<RectTransform>());
            Text textComp = textObj.GetComponent<Text>();
            textComp.raycastTarget = true;

            GameObject placeholder = UIFactory.CreateText("0", obj, 12, new Color(0.7f, 0.7f, 0.7f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            UIFactory.Stretch(placeholder.GetComponent<RectTransform>());

            InputField input = obj.AddComponent<InputField>();
            input.textComponent = textComp;
            input.placeholder = placeholder.GetComponent<Text>();
            input.text = defaultText;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 2;

            return input;
        }

        private static GameObject CreateOpacitySlider(GameObject parent)
        {
            GameObject sliderObj = UIFactory.CreateObject("OpacitySlider", parent);
            sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 20);

            GameObject bgTrack = UIFactory.CreateObject("Background", sliderObj);
            RectTransform bgRt = bgTrack.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.35f); bgRt.anchorMax = new Vector2(1, 0.65f);
            bgRt.sizeDelta = Vector2.zero; bgRt.anchoredPosition = Vector2.zero;
            Image bgImg = bgTrack.AddComponent<Image>();
            bgImg.color = new Color(0.82f, 0.82f, 0.82f);
            Sprite trackSprite = UIFactory.CreateRoundedRectSprite(64, 16, 8);
            if (trackSprite != null) { bgImg.sprite = trackSprite; bgImg.type = Image.Type.Sliced; }

            GameObject fillArea = UIFactory.CreateObject("Fill Area", sliderObj);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.35f); faRt.anchorMax = new Vector2(1, 0.65f);
            faRt.offsetMin = Vector2.zero; faRt.offsetMax = Vector2.zero;

            GameObject fill = UIFactory.CreateObject("Fill", fillArea);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1);
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.31f, 0.86f, 0.45f);
            if (trackSprite != null) { fillImg.sprite = trackSprite; fillImg.type = Image.Type.Sliced; }

            GameObject handleArea = UIFactory.CreateObject("Handle Slide Area", sliderObj);
            RectTransform haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10, 0); haRt.offsetMax = new Vector2(-10, 0);

            GameObject handle = UIFactory.CreateObject("Handle", handleArea);
            RectTransform hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(20, 20);
            Image handleImg = handle.AddComponent<Image>();
            Sprite brushSpr = Resources.Load<Sprite>("EditIcons/p_brush_press");
            if (brushSpr != null)
            {
                handleImg.sprite = brushSpr;
                handleImg.type = Image.Type.Simple;
                handleImg.preserveAspect = true;
                handleImg.color = Color.white;
            }
            else
            {
                handleImg.color = new Color(0.25f, 0.25f, 0.25f);
            }

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.value = 100;

            return sliderObj;
        }

        private static GameObject CreateEraserSlider(GameObject parent)
        {
            GameObject sliderObj = UIFactory.CreateObject("BrushSizeSlider", parent);
            sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 20);

            // Background track (thin gray bar)
            GameObject bgTrack = UIFactory.CreateObject("Background", sliderObj);
            RectTransform bgRt = bgTrack.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.35f); bgRt.anchorMax = new Vector2(1, 0.65f);
            bgRt.sizeDelta = Vector2.zero; bgRt.anchoredPosition = Vector2.zero;
            Image bgImg = bgTrack.AddComponent<Image>();
            bgImg.color = new Color(0.82f, 0.82f, 0.82f);
            Sprite trackSprite = UIFactory.CreateRoundedRectSprite(64, 16, 8);
            if (trackSprite != null) { bgImg.sprite = trackSprite; bgImg.type = Image.Type.Sliced; }

            // Fill area (green)
            GameObject fillArea = UIFactory.CreateObject("Fill Area", sliderObj);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.35f); faRt.anchorMax = new Vector2(1, 0.65f);
            faRt.offsetMin = Vector2.zero; faRt.offsetMax = Vector2.zero;

            GameObject fill = UIFactory.CreateObject("Fill", fillArea);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1);
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.31f, 0.86f, 0.45f);
            if (trackSprite != null) { fillImg.sprite = trackSprite; fillImg.type = Image.Type.Sliced; }

            // Handle slide area
            GameObject handleArea = UIFactory.CreateObject("Handle Slide Area", sliderObj);
            RectTransform haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10, 0); haRt.offsetMax = new Vector2(-10, 0);

            GameObject handle = UIFactory.CreateObject("Handle", handleArea);
            RectTransform hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(20, 20);
            Image handleImg = handle.AddComponent<Image>();
            Sprite brushSpr = Resources.Load<Sprite>("EditIcons/p_brush_press");
            if (brushSpr != null)
            {
                handleImg.sprite = brushSpr;
                handleImg.type = Image.Type.Simple;
                handleImg.preserveAspect = true;
                handleImg.color = Color.white;
            }
            else
            {
                handleImg.color = new Color(0.25f, 0.25f, 0.25f);
            }

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.minValue = 5;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.value = 20;

            return sliderObj;
        }

        private static Button CreateToolbarIconButton(GameObject parent, string iconPath, string tooltipText)
        {
            GameObject buttonObj = UIFactory.CreateButton("", parent, Vector2.zero, new Vector2(38, 28), Color.white, Color.black);
            LayoutElement le = buttonObj.AddComponent<LayoutElement>();
            le.minWidth = 38;
            le.preferredWidth = 38;
            Image img = buttonObj.GetComponent<Image>();
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                Object.Destroy(buttonObj.transform.GetChild(0).gameObject);
                UIFactory.CreateText(tooltipText, buttonObj, 12, Color.black, Vector2.zero, Vector2.zero);
            }

            UITooltip tip = buttonObj.AddComponent<UITooltip>();
            tip.text = tooltipText;

            return buttonObj.GetComponent<Button>();
        }

        private static void CreateCropPresetButton(GameObject parent, string iconPath, string label, System.Action onClick)
        {
            GameObject item = UIFactory.CreateObject("CropPreset_" + label, parent);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 48);

            VerticalLayoutGroup vlg = item.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            GameObject buttonObj = UIFactory.CreateButton("", item, Vector2.zero, new Vector2(28, 22), Color.white, Color.black);
            Button button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());
            Image buttonImage = buttonObj.GetComponent<Image>();
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            if (sprite != null)
            {
                buttonImage.sprite = sprite;
                buttonImage.type = Image.Type.Simple;
                buttonImage.preserveAspect = true;
                buttonImage.color = Color.white;
            }
            else
            {
                Object.Destroy(buttonObj.transform.GetChild(0).gameObject);
                UIFactory.CreateText(label, buttonObj, 8, Color.black, Vector2.zero, Vector2.zero);
            }

            GameObject labelObj = UIFactory.CreateText(label, item, 8, new Color(0.25f, 0.25f, 0.25f), Vector2.zero, new Vector2(36, 12), TextAnchor.MiddleCenter);
            labelObj.GetComponent<Text>().raycastTarget = false;
        }

        public static void CreateLayersPanel(GameObject workspace, CanvasController controller)
        {
            GameObject container = UIFactory.CreateObject("LayersContainer", workspace);
            RectTransform cr = container.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 0); cr.anchorMax = new Vector2(0.4f, 0.5f);
            cr.offsetMin = new Vector2(20, 20); cr.offsetMax = new Vector2(0, 0);
            container.transform.SetAsLastSibling(); 
            
            GameObject list = UIFactory.CreateObject("LayersList", container);
            UIFactory.Stretch(list.GetComponent<RectTransform>());
            list.AddComponent<Image>().color = Color.white; list.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            
            ScrollRect sr = list.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            
            GameObject viewport = UIFactory.CreateObject("Viewport", list);
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            UIFactory.Stretch(vpRect); vpRect.offsetMin = new Vector2(0, 5); vpRect.offsetMax = new Vector2(0, -35);
            viewport.AddComponent<RectMask2D>();
            
            GameObject content = UIFactory.CreateObject("Content", viewport);
            RectTransform cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1); cRect.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup vlg2 = content.AddComponent<VerticalLayoutGroup>();
            vlg2.childControlHeight = true;
            vlg2.childForceExpandWidth = true;
            vlg2.spacing = 2;
            
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            sr.viewport = vpRect; sr.content = cRect;
            controller.layersListContainer = content;

            UIFactory.CreateText("Layers", list, 14, Color.black, new Vector2(0, 110), new Vector2(0, 30), TextAnchor.MiddleCenter, FontStyle.Bold);
            list.SetActive(false); 

            GameObject toggleBtn = UIFactory.CreateButton("Layers \u2630", workspace, Vector2.zero, new Vector2(100, 30), Color.white, Color.black);
            RectTransform btRt = toggleBtn.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0, 0); btRt.anchorMax = new Vector2(0, 0); btRt.pivot = Vector2.zero;
            btRt.anchoredPosition = new Vector2(20, 20);
            toggleBtn.GetComponent<Button>().onClick.AddListener(() => {
                list.SetActive(!list.activeSelf);
                if (list.activeSelf) controller.UpdateLayersPanel();
            });
            toggleBtn.transform.SetAsLastSibling();
        }

        public static void SetupGrid(GameObject parent, int count, System.Action<int> onClick, string prefix = "T", Object[] images = null)
        {
            GameObject grid = UIFactory.CreateObject("Grid", parent);
            RectTransform gridRt = grid.GetComponent<RectTransform>();
            UIFactory.Stretch(gridRt);
            
            // Allow grid to expand vertically
            gridRt.anchorMin = new Vector2(0, 1);
            gridRt.anchorMax = new Vector2(1, 1);
            gridRt.pivot = new Vector2(0.5f, 1);
            
            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(110, 110); 
            glg.spacing = new Vector2(10, 10);
            glg.padding = new RectOffset(10, 10, 10, 10);
            glg.constraint = GridLayoutGroup.Constraint.Flexible;
            
            // Add ContentSizeFitter so it expands inside a ScrollRect
            ContentSizeFitter csf = grid.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            for(int k=0; k<count; k++) {
                GameObject item = UIFactory.CreateObject(prefix+k, grid);
                int index = k; 
                Button btn = item.AddComponent<Button>();
                btn.onClick.AddListener(() => onClick(index));
                
                Image img = item.AddComponent<Image>();
                if (images != null && k < images.Length && images[k] is Sprite sp)
                {
                    img.sprite = sp;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                else
                {
                    img.color = Color.HSVToRGB((float)k/(float)count, 0.5f, 0.9f);
                }
            }
        }

        public static void AddManipulationComponents(GameObject go)
        {
            if(!go.GetComponent<CanvasGroup>()) go.AddComponent<CanvasGroup>();
            if(!go.GetComponent<BoxCollider2D>()) go.AddComponent<BoxCollider2D>().size = new Vector2(100, 100); 
            if(!go.GetComponent<ObjectManipulator>()) go.AddComponent<ObjectManipulator>();
        }
    }
}

