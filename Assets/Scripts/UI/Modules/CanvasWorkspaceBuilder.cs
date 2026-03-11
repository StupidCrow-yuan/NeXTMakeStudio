using UnityEngine;
using UnityEngine.UI;
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
            hlg.spacing = 10; hlg.padding = new RectOffset(15, 15, 5, 5); hlg.childAlignment = TextAnchor.MiddleCenter;

            UIFactory.CreateButton("-", ctrlBar, Vector2.zero, new Vector2(30, 30), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ChangeZoom(-0.2f));
            
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
            
            Button hb = UIFactory.CreateButton("Hand \u270B", ctrlBar, Vector2.zero, new Vector2(80, 30), Color.white, Color.black).GetComponent<Button>();
            hb.onClick.AddListener(() => {
                controller.ToggleHandTool(!controller.IsHandToolActive());
                hb.GetComponent<Image>().color = controller.IsHandToolActive() ? new Color(0.8f, 1f, 0.8f) : Color.white;
            });

            UIFactory.CreateButton("Fit", ctrlBar, Vector2.zero, new Vector2(50, 30), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ZoomToFit());
        }

        public static void CreateContextToolbar(GameObject workspace, CanvasController controller)
        {
            GameObject ct = UIFactory.CreateObject("ContextToolbar", workspace);
            RectTransform ctRect = ct.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0.5f, 0.5f); ctRect.anchorMax = new Vector2(0.5f, 0.5f); ctRect.pivot = new Vector2(0.5f, 0.5f);
            ctRect.sizeDelta = new Vector2(680, 50); ctRect.anchoredPosition = new Vector2(0, 340); 
            ct.AddComponent<Image>().color = Color.white; ct.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            HorizontalLayoutGroup ctHlg = ct.AddComponent<HorizontalLayoutGroup>();
            ctHlg.spacing = 10; ctHlg.padding = new RectOffset(20, 20, 5, 5); ctHlg.childAlignment = TextAnchor.MiddleCenter;

            string[] tools = { "Crop", "Eraser", "Opacity", "Image Cutting", "UpScaler", "AI Remover", "Cutout", "Outline" };
            foreach(var tool in tools) UIFactory.CreateButton(tool, ct, Vector2.zero, new Vector2(0, 30), Color.white, Color.black).AddComponent<LayoutElement>().flexibleWidth = 1;
            ct.SetActive(false); controller.contextToolbar = ct;
            ct.transform.SetAsLastSibling();
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

