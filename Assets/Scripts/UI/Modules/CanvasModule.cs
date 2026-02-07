using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;
using NeXTMake.UI.Core; // For UIFactory
using NeXTMake.UI; // For DragRotator

using System.Linq;
using System.Collections.Generic;

namespace NeXTMake.UI.Modules
{
    public class CanvasModule
    {
        public static void CreateCanvasEditor(GameObject parent)
        {
            GameObject editorArea = UIFactory.CreateObject("EditorArea", parent);
            UIFactory.Stretch(editorArea.GetComponent<RectTransform>()); 

            CanvasController controller = editorArea.AddComponent<CanvasController>();
            controller.editorArea = editorArea; // Assign for popups

            // 1. Workspace Container (Clips everything inside) - Middle 0.45 area
            GameObject workspace = UIFactory.CreateObject("Workspace", editorArea);
            RectTransform wsRect = workspace.GetComponent<RectTransform>();
            wsRect.anchorMin = new Vector2(0.3f, 0); wsRect.anchorMax = new Vector2(0.75f, 1);
            wsRect.offsetMin = Vector2.zero; wsRect.offsetMax = new Vector2(0, -30);
            workspace.AddComponent<Image>().color = new Color(0.93f, 0.93f, 0.94f); 

            // CRITICAL: Add Mask to clip paper when zoomed in
            workspace.AddComponent<RectMask2D>();

            // Interaction Overlay (Top level click detector for deselection)
            GameObject bgBtnObj = UIFactory.CreateObject("BGDeselector", workspace);
            UIFactory.Stretch(bgBtnObj.GetComponent<RectTransform>());
            Image bgImg = bgBtnObj.AddComponent<Image>();
            bgImg.color = Color.clear;
            bgImg.raycastTarget = true;
            Button overlayBtn = bgBtnObj.AddComponent<Button>();
            overlayBtn.onClick.AddListener(() => controller.Deselect());
            // Add Dragger for Hand Tool
            bgBtnObj.AddComponent<CanvasDragger>().controller = controller;

            // 2. Paper (The Canvas)
            GameObject paper = UIFactory.CreateObject("Paper", workspace);

            Image paperImg = paper.AddComponent<Image>();
            paperImg.color = Color.white;
            controller.paperBackground = paperImg;
            paper.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            paper.AddComponent<Button>().onClick.AddListener(() => controller.Deselect());
            // Add Dragger for Hand Tool here too
            paper.AddComponent<CanvasDragger>().controller = controller;
            RectTransform pRect = paper.GetComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(600, 600);

            controller.paper = pRect;
            
            // 3. UI Components (Must be OUTSIDE workspace or above it to not be clipped/panned)
            var (bRuler, rRuler) = CreateRulers(workspace);
            controller.bottomRuler = bRuler;
            controller.rightRuler = rRuler;

            CreateBottomControls(workspace, controller);
            CreateContextToolbar(workspace, controller);
            CreateLayersPanel(workspace, controller);
            
            // Initial ruler draw
            controller.UpdateRulers();
            
            // 4. Menu & Toolbar
            SetupLeftMenu(editorArea, pRect, controller);
            
            // 5. Right Panel
            CreateRightPanel(editorArea, controller);
        }

        private static (RectTransform, RectTransform) CreateRulers(GameObject workspace)
        {
            // Rulers stay fixed at edges of workspace
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

        private static void CreateBottomControls(GameObject workspace, CanvasController controller)
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
            ddObj.AddComponent<LayoutElement>().minWidth = 70; // Slightly narrower
            Image ddImg = ddObj.AddComponent<Image>(); ddImg.color = new Color(0.95f, 0.95f, 0.95f);
            Dropdown dd = ddObj.AddComponent<Dropdown>();
            dd.targetGraphic = ddImg;
            controller.zoomDropdown = dd;
            
            // Dropdown Template
            GameObject template = UIFactory.CreateObject("Template", ddObj);
            template.AddComponent<CanvasGroup>();
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 1); templateRect.anchorMax = new Vector2(1, 1);
            templateRect.pivot = new Vector2(0.5f, 0); templateRect.sizeDelta = new Vector2(-10, 180); // Height for 9 items
            templateRect.anchoredPosition = new Vector2(0, 2);
            template.AddComponent<Image>().color = Color.white;
            template.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f); // Lighter border
            
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

            // Scrollbar
            GameObject sbarObj = UIFactory.CreateObject("Scrollbar", template);
            RectTransform sbRect = sbarObj.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1, 0); sbRect.anchorMax = new Vector2(1, 1);
            sbRect.sizeDelta = new Vector2(4, -4); // Thin scrollbar
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
            vlg.spacing = 4; // Add spacing between items

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            templateSr.viewport = vpRect;
            templateSr.content = contentRect;
            dd.template = templateRect;
            
            GameObject itemTemplate = UIFactory.CreateObject("Item", content);
            itemTemplate.AddComponent<LayoutElement>().minHeight = 34; // Increased item height
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
            Image checkImg = itemCheck.AddComponent<Image>(); checkImg.color = UIFactory.COLOR_ACCENT_GREEN;
            
            dd.itemText = itemText;
            itemToggle.targetGraphic = itemBgImg; itemToggle.graphic = checkImg;
            template.SetActive(false);

            Text label = UIFactory.CreateText("100%", ddObj, 12, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            dd.captionText = label;
            
            List<string> options = new List<string>{
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
            
            Button hb = UIFactory.CreateButton("Hand ✋", ctrlBar, Vector2.zero, new Vector2(80, 30), Color.white, Color.black).GetComponent<Button>();
            hb.onClick.AddListener(() => {
                controller.ToggleHandTool(!controller.IsHandToolActive());
                hb.GetComponent<Image>().color = controller.IsHandToolActive() ? new Color(0.8f, 1f, 0.8f) : Color.white; // Greenish when active
            });

            UIFactory.CreateButton("Fit", ctrlBar, Vector2.zero, new Vector2(50, 30), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ZoomToFit());
        }

        private static void SetupLeftMenu(GameObject editorArea, RectTransform paper, CanvasController controller)
        {
            // Left area container - 0.3 width
            GameObject leftArea = UIFactory.CreateObject("LeftArea", editorArea);
            RectTransform laRect = leftArea.GetComponent<RectTransform>();
            laRect.anchorMin = new Vector2(0, 0); laRect.anchorMax = new Vector2(0.3f, 1);
            laRect.offsetMin = Vector2.zero; laRect.offsetMax = Vector2.zero;

            GameObject leftToolBar = UIFactory.CreateObject("LeftToolBar", leftArea);
            RectTransform ltbRect = leftToolBar.GetComponent<RectTransform>();
            // USER REQ: Left-most tool column should take ~1/6 of the left area (not fixed width)
            ltbRect.anchorMin = new Vector2(0, 0);
            ltbRect.anchorMax = new Vector2(1f / 6f, 1);
            ltbRect.offsetMin = Vector2.zero;
            ltbRect.offsetMax = Vector2.zero;
            leftToolBar.AddComponent<Image>().color = Color.white;

            VerticalLayoutGroup ltbVlg = leftToolBar.AddComponent<VerticalLayoutGroup>();
            ltbVlg.spacing = 6; ltbVlg.padding = new RectOffset(6, 6, 12, 8); ltbVlg.childAlignment = TextAnchor.UpperCenter;

            GameObject drawer = UIFactory.CreateObject("Drawer", leftArea);
            RectTransform dRect = drawer.GetComponent<RectTransform>();
            // Fill remaining space after toolbar (0.3 screen width - toolBarWidth)
            dRect.anchorMin = new Vector2(1f / 6f, 0);
            dRect.anchorMax = new Vector2(1, 1);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;
            drawer.AddComponent<Image>().color = Color.white; drawer.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            VerticalLayoutGroup dVlg = drawer.AddComponent<VerticalLayoutGroup>();
            dVlg.padding = new RectOffset(16, 16, 16, 16); dVlg.spacing = 10; dVlg.childControlHeight = false;

            // USER REQ: Divider should be between the 1/6 tool column (Upload etc.) and the Drawer
            // Put it under leftArea so it's not affected by VerticalLayoutGroup.
            GameObject divider = UIFactory.CreateObject("Divider", leftArea);
            RectTransform divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(1f / 6f, 0);
            divRt.anchorMax = new Vector2(1f / 6f, 1);
            divRt.pivot = new Vector2(0.5f, 0.5f);
            divRt.anchoredPosition = Vector2.zero;
            divRt.sizeDelta = new Vector2(2f, 0);
            Image divImg = divider.AddComponent<Image>();
            divImg.color = new Color(0.84f, 0.84f, 0.84f, 1f);
            // Ensure it's above the tool/drawer backgrounds
            divider.transform.SetAsLastSibling();

            GameObject titleTxt = UIFactory.CreateText("Templates", drawer, 20, Color.black, Vector2.zero, new Vector2(0, 32), TextAnchor.MiddleLeft, FontStyle.Bold);
            titleTxt.AddComponent<LayoutElement>().minHeight = 32;

            GameObject searchBar = UIFactory.CreateObject("Search", drawer);
            searchBar.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            var searchLe = searchBar.AddComponent<LayoutElement>();
            searchLe.minHeight = 28;
            searchLe.preferredHeight = 28;
            InputField searchInput = searchBar.AddComponent<InputField>();
            Text txt = UIFactory.CreateText("", searchBar, 12, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            searchInput.textComponent = txt;
            GameObject placeholder = UIFactory.CreateText("Q Search", searchBar, 12, new Color(0.6f, 0.6f, 0.6f), Vector2.zero, Vector2.zero);
            searchInput.placeholder = placeholder.GetComponent<Text>();
            RectTransform txtRect = txt.rectTransform; UIFactory.Stretch(txtRect); txtRect.offsetMin = new Vector2(10, 2);
            RectTransform phRect = placeholder.GetComponent<RectTransform>(); UIFactory.Stretch(phRect); phRect.offsetMin = new Vector2(10, 2);

            GameObject contentRoot = UIFactory.CreateObject("PanelContainer", drawer);
            contentRoot.AddComponent<LayoutElement>().flexibleHeight = 1;

            System.Action<string> ShowSidePanel = (type) => {
                titleTxt.GetComponent<Text>().text = type;

                foreach(Transform child in contentRoot.transform) Object.Destroy(child.gameObject);
                searchBar.SetActive(type == "Templates" || type == "Elements");
                
                switch(type) {
                    case "Templates":

                        SetupGrid(contentRoot, 6, (i) => {
                            GameObject addedImg = UIFactory.CreateObject("Design_"+i, paper.gameObject);
                            addedImg.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);
                            addedImg.AddComponent<Image>().color = Color.HSVToRGB((float)i/6f, 0.5f, 0.9f);
                            AddManipulationComponents(addedImg);

                            controller.RecordAdd(addedImg); // Record for Undo
                        }, "T");
                        break;
                    case "Text":

                        SetupGrid(contentRoot, 4, (i) => {
                            GameObject t = UIFactory.CreateText("New Text", paper.gameObject, 32, Color.black, Vector2.zero, new Vector2(200, 50));
                            AddManipulationComponents(t);
                            controller.RecordAdd(t); // Record for Undo
                        }, "Txt");
                        break;
                }
            };
            
            string[] tools = { "Upload", "Image AI", "Textures", "Templates", "Elements", "Text", "Projects" };
            string[] icons = { "\u2191", "\u25C7", "\u25A3", "\u229E", "\u25A6", "T", "\uD83D\uDCC1" }; // ↑, ◇, ▣, ⊞, ▦, T, 📁 (folder)
            for (int i = 0; i < tools.Length; i++) {
                string t = tools[i];
                string iconChar = i < icons.Length ? icons[i] : "";
                GameObject btnObj = UIFactory.CreateObject("Btn_" + t, leftToolBar);
                var btnLe = btnObj.AddComponent<LayoutElement>();
                btnLe.minHeight = 44;
                btnLe.minWidth = 0;
                Image btnImg = btnObj.AddComponent<Image>(); btnImg.color = new Color(0,0,0,0.01f);
                VerticalLayoutGroup btnVlg = btnObj.AddComponent<VerticalLayoutGroup>();
                btnVlg.spacing = 2; btnVlg.padding = new RectOffset(2, 2, 4, 4); btnVlg.childAlignment = TextAnchor.MiddleCenter; btnVlg.childControlHeight = false; btnVlg.childForceExpandHeight = false;
                if (!string.IsNullOrEmpty(iconChar)) {
                    GameObject iconObj = UIFactory.CreateText(iconChar, btnObj, 16, new Color(0.35f, 0.35f, 0.35f), Vector2.zero, new Vector2(0, 18), TextAnchor.MiddleCenter);
                    iconObj.AddComponent<LayoutElement>().minHeight = 18;
                }
                GameObject lblObj = UIFactory.CreateText(t, btnObj, 11, new Color(0.25f, 0.25f, 0.25f), Vector2.zero, new Vector2(0, 16), TextAnchor.MiddleCenter, FontStyle.Bold);
                lblObj.AddComponent<LayoutElement>().minHeight = 14;
                RectTransform lblRt = lblObj.GetComponent<RectTransform>();
                lblRt.anchorMin = new Vector2(0, 0); lblRt.anchorMax = new Vector2(1, 0); lblRt.pivot = new Vector2(0.5f, 0);
                string type = t;
                btnObj.AddComponent<Button>().onClick.AddListener(() => ShowSidePanel(type));
            }
            ShowSidePanel("Templates");
        }

        private static void CreateLayersPanel(GameObject workspace, CanvasController controller)
        {
            // Position at bottom-left of workspace area using relative position
            GameObject container = UIFactory.CreateObject("LayersContainer", workspace);
            RectTransform cr = container.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 0); cr.anchorMax = new Vector2(0.4f, 0.5f);
            cr.offsetMin = new Vector2(20, 20); cr.offsetMax = new Vector2(0, 0);
            container.transform.SetAsLastSibling(); 
            
            GameObject list = UIFactory.CreateObject("LayersList", container);
            UIFactory.Stretch(list.GetComponent<RectTransform>());
            list.AddComponent<Image>().color = Color.white; list.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            
            // Scroll Area for Layers
            ScrollRect sr = list.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            
            GameObject viewport = UIFactory.CreateObject("Viewport", list);
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            UIFactory.Stretch(vpRect); vpRect.offsetMin = new Vector2(0, 5); vpRect.offsetMax = new Vector2(0, -35); // Room for title
            viewport.AddComponent<RectMask2D>();
            
            GameObject content = UIFactory.CreateObject("Content", viewport);
            RectTransform cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1); cRect.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; // IMPORTANT: Force items to fill row width
            vlg.spacing = 2;
            
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            sr.viewport = vpRect; sr.content = cRect;
            controller.layersListContainer = content;

            UIFactory.CreateText("Layers", list, 14, Color.black, new Vector2(0, 110), new Vector2(0, 30), TextAnchor.MiddleCenter, FontStyle.Bold);
            list.SetActive(false); 

            GameObject toggleBtn = UIFactory.CreateButton("Layers ☰", workspace, Vector2.zero, new Vector2(100, 30), Color.white, Color.black);
            RectTransform btRt = toggleBtn.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0, 0); btRt.anchorMax = new Vector2(0, 0); btRt.pivot = Vector2.zero;
            btRt.anchoredPosition = new Vector2(20, 20);
            toggleBtn.GetComponent<Button>().onClick.AddListener(() => {
                list.SetActive(!list.activeSelf);
                if (list.activeSelf) controller.UpdateLayersPanel();
            });
            toggleBtn.transform.SetAsLastSibling();
        }

        private static void CreateRightPanel(GameObject editorArea, CanvasController controller)
        {
            GameObject rightPanel = UIFactory.CreateObject("RightPanel", editorArea);
            RectTransform rpRect = rightPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.75f, 0); rpRect.anchorMax = new Vector2(1, 1);
            rpRect.offsetMin = Vector2.zero; rpRect.offsetMax = Vector2.zero;
            rightPanel.AddComponent<Image>().color = Color.white;
            rightPanel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);


            // 1. Root Container for Panels (Scrollable)
            GameObject panelsRoot = UIFactory.CreateObject("PanelsRoot", rightPanel);
            RectTransform prRt = panelsRoot.GetComponent<RectTransform>();
            UIFactory.Stretch(prRt); prRt.offsetMin = new Vector2(0, 100); // Leave room for bottom buttons

            // Scroll Area for Right Panel Content
            ScrollRect mainSr = panelsRoot.AddComponent<ScrollRect>();
            mainSr.horizontal = false; mainSr.vertical = true;
            mainSr.scrollSensitivity = 60; // Increased sensitivity
            mainSr.movementType = ScrollRect.MovementType.Clamped; // Prevent bouncing jitter

            GameObject mainVp = UIFactory.CreateObject("Viewport", panelsRoot);
            UIFactory.Stretch(mainVp.GetComponent<RectTransform>());
            Image vpImg = mainVp.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0); // Transparent but raycastable
            vpImg.raycastTarget = true;
            mainVp.AddComponent<RectMask2D>();

            GameObject mainContent = UIFactory.CreateObject("MainContent", mainVp);
            RectTransform mcRt = mainContent.GetComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0, 1); mcRt.anchorMax = new Vector2(1, 1);
            mcRt.pivot = new Vector2(0.5f, 1); mcRt.sizeDelta = new Vector2(0, 0);
            mainContent.AddComponent<VerticalLayoutGroup>(); // Container for layer/global panels
            mainContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            mainSr.viewport = mainVp.GetComponent<RectTransform>(); mainSr.content = mcRt;

            // Layer Info Panel
            GameObject layerPanel = UIFactory.CreateObject("LayerInfoPanel", mainContent);
            layerPanel.AddComponent<LayoutElement>().flexibleHeight = 1;
            VerticalLayoutGroup lpVlg = layerPanel.AddComponent<VerticalLayoutGroup>();
            lpVlg.padding = new RectOffset(20, 20, 20, 20); lpVlg.spacing = 15;
            lpVlg.childControlHeight = false; // Allow children to control their own height
            lpVlg.childForceExpandHeight = false; // Don't force expand
            controller.layerInfoPanel = layerPanel;
            layerPanel.SetActive(false);

            // Global Info Panel
            GameObject globalPanel = UIFactory.CreateObject("GlobalInfoPanel", mainContent);
            globalPanel.AddComponent<LayoutElement>().flexibleHeight = 1;
            controller.globalInfoPanel = globalPanel;
            CreateGlobalInfoPanel(globalPanel, controller);

            // --- Populate Layer Info Panel ---
            UIFactory.CreateText("Position", layerPanel, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            
            GameObject posPanel = UIFactory.CreateObject("PosPanel", layerPanel);
            VerticalLayoutGroup posVlg = posPanel.AddComponent<VerticalLayoutGroup>(); posVlg.spacing = 5;
            posPanel.AddComponent<LayoutElement>().minHeight = 80;
            
            GameObject r1 = UIFactory.CreateObject("Row1", posPanel); r1.AddComponent<LayoutElement>().minHeight = 25;
            r1.AddComponent<HorizontalLayoutGroup>();
            controller.posXText = UIFactory.CreateText("X: --", r1, 13, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            controller.posYText = UIFactory.CreateText("Y: --", r1, 13, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            
            GameObject r2 = UIFactory.CreateObject("Row2", posPanel); r2.AddComponent<LayoutElement>().minHeight = 25;
            r2.AddComponent<HorizontalLayoutGroup>();
            controller.widthText = UIFactory.CreateText("W: --", r2, 13, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            controller.heightText = UIFactory.CreateText("H: --", r2, 13, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            
            GameObject r3 = UIFactory.CreateObject("Row3", posPanel); r3.AddComponent<LayoutElement>().minHeight = 25;
            r3.AddComponent<HorizontalLayoutGroup>();
            controller.rotationText = UIFactory.CreateText("Rotation: 0°", r3, 13, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            
            controller.positionPanel = posPanel;

            UIFactory.CreateText("Craft Mode", layerPanel, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject craftGrid = UIFactory.CreateObject("CraftGrid", layerPanel);
            controller.craftModeContainer = craftGrid;
            GridLayoutGroup cglg = craftGrid.AddComponent<GridLayoutGroup>();
            cglg.cellSize = new Vector2(125, 40); cglg.spacing = new Vector2(10, 10);
            cglg.constraintCount = 2;
            craftGrid.AddComponent<LayoutElement>().minHeight = 150; 

            string[] craftModes = { "Flat", "Flat Raised", "Pattern Texture", "Relief Texture", "Customize Texture" };
            foreach(var cm in craftModes) {
                GameObject btn = UIFactory.CreateButton(cm, craftGrid, Vector2.zero, new Vector2(0, 0), Color.white, Color.black);
                btn.GetComponentInChildren<Text>().fontSize = 12;
                btn.AddComponent<Outline>().effectColor = cm == "Flat" ? Color.green : Color.gray;
                
                string mode = cm;
                btn.GetComponent<Button>().onClick.AddListener(() => {
                    foreach(Transform child in craftGrid.transform) {
                        var outline = child.GetComponent<Outline>();
                        if(outline) outline.effectColor = child.name.Contains(mode) ? Color.green : Color.gray;
                    }
                    controller.OnCraftModeChanged(mode);
                });
            }

            // Mini Preview Panel - 1:1 aspect ratio
            GameObject miniPrev = UIFactory.CreateObject("MiniPreview", layerPanel);
            miniPrev.AddComponent<Image>().color = new Color(0.98f, 0.98f, 0.98f);
            miniPrev.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);

            // Ensure 1:1 aspect ratio
            AspectRatioFitter arf = miniPrev.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            arf.aspectRatio = 1.0f;

            // Layout element to control positioning in vertical layout
            LayoutElement le = miniPrev.AddComponent<LayoutElement>();
            le.preferredHeight = 220; // Initial size, will be adjusted by aspect ratio
            
            // Customize Upload Panel (Hidden by default)
            GameObject custPanel = UIFactory.CreateObject("CustomizeUpload", layerPanel);
            custPanel.AddComponent<LayoutElement>().minHeight = 80;
            custPanel.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
            custPanel.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            VerticalLayoutGroup cvlg = custPanel.AddComponent<VerticalLayoutGroup>();
            cvlg.padding = new RectOffset(10, 10, 10, 10); cvlg.spacing = 5;
            
            UIFactory.CreateText("Upload Depth Map (JPG/PNG/SVG/WebP)", custPanel, 11, Color.gray, Vector2.zero, Vector2.zero);
            Button uploadBtn = UIFactory.CreateButton("Upload ⤒", custPanel, Vector2.zero, new Vector2(0, 35), Color.white, Color.black).GetComponent<Button>();
            uploadBtn.onClick.AddListener(() => {
                controller.OnUploadDepthMap();
            });
            custPanel.SetActive(false);
            controller.customizePanel = custPanel;

            GameObject miniZoomBar = UIFactory.CreateObject("MiniZoomBar", miniPrev);
            RectTransform mzbRt = miniZoomBar.GetComponent<RectTransform>();
            mzbRt.anchorMin = new Vector2(0, 1); mzbRt.anchorMax = new Vector2(0, 1);
            mzbRt.pivot = new Vector2(0, 1); mzbRt.sizeDelta = new Vector2(80, 25); mzbRt.anchoredPosition = new Vector2(5, -5);
            miniZoomBar.AddComponent<HorizontalLayoutGroup>().spacing = 5;
            UIFactory.CreateButton("-", miniZoomBar, Vector2.zero, new Vector2(25, 20), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ChangeMiniZoom(-0.5f));
            UIFactory.CreateButton("+", miniZoomBar, Vector2.zero, new Vector2(25, 20), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ChangeMiniZoom(0.5f));

            // Depth download (bottom-left)
            GameObject depthDl = UIFactory.CreateObject("DepthDownload", miniPrev);
            RectTransform ddlRt = depthDl.GetComponent<RectTransform>();
            ddlRt.anchorMin = new Vector2(1, 0); ddlRt.anchorMax = new Vector2(1, 0);
            ddlRt.pivot = new Vector2(1, 0);
            ddlRt.sizeDelta = new Vector2(140, 24);
            ddlRt.anchoredPosition = new Vector2(-6, 6);
            Image ddlImg = depthDl.AddComponent<Image>();
            ddlImg.color = new Color(1f, 1f, 1f, 0.85f);
            Button ddlBtn = depthDl.AddComponent<Button>();
            ddlBtn.onClick.AddListener(() => controller.OnDownloadDepthImage());
            depthDl.AddComponent<Outline>().effectColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            HorizontalLayoutGroup ddlHlg = depthDl.AddComponent<HorizontalLayoutGroup>();
            ddlHlg.padding = new RectOffset(6, 6, 2, 2);
            ddlHlg.spacing = 6;
            ddlHlg.childAlignment = TextAnchor.MiddleLeft;
            ddlHlg.childControlWidth = true;
            ddlHlg.childControlHeight = true;
            ddlHlg.childForceExpandWidth = false;
            ddlHlg.childForceExpandHeight = false;
            UIFactory.CreateText("↓", depthDl, 13, new Color(0.2f, 0.2f, 0.2f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.CreateText("Depth Image", depthDl, 12, new Color(0.2f, 0.2f, 0.2f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Normal);

            GameObject mini3D = UIFactory.CreateObject("Mini3DView", miniPrev);
            UIFactory.Stretch(mini3D.GetComponent<RectTransform>());
            mini3D.GetComponent<RectTransform>().offsetMin = new Vector2(5, 5); mini3D.GetComponent<RectTransform>().offsetMax = new Vector2(-5, -30);
            RawImage miniRi = mini3D.AddComponent<RawImage>();
            miniRi.raycastTarget = true;
            
            Model3DViewer miniViewer = miniPrev.AddComponent<Model3DViewer>();
            miniViewer.targetImage = miniRi;
            miniViewer.textureHeight = 1024;
            miniViewer.textureWidth = 1024;

            Model3DController miniController = miniPrev.AddComponent<Model3DController>();
            miniController.modelViewer = miniViewer;
            miniController.enableRotation = true;
            miniController.enableZoom = true;
            miniController.enablePan = true;
            
            controller.miniPreviewPanel = miniPrev;
            controller.miniPreviewImage = miniRi;
            controller.miniModelViewer = miniViewer;
            miniPrev.SetActive(false);

            // Ink Mode
            UIFactory.CreateText("Ink Mode", layerPanel, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject dropdownObj = UIFactory.CreateObject("InkDropdown", layerPanel);
            dropdownObj.AddComponent<LayoutElement>().minHeight = 40;
            Image ddImg = dropdownObj.AddComponent<Image>(); ddImg.color = new Color(0.95f, 0.95f, 0.95f);
            Dropdown dd = dropdownObj.AddComponent<Dropdown>();
            dd.targetGraphic = ddImg;
            
            // ... (Dropdown setup remains same)
            GameObject template = UIFactory.CreateObject("Template", dropdownObj);
            // ... (Skipping some lines for brevity in search string, but will keep structure)
            template.AddComponent<CanvasGroup>();
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0); templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1); templateRect.sizeDelta = new Vector2(0, 160);
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

            Text label = UIFactory.CreateText("Select Mode...", dropdownObj, 12, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = new Vector2(10, 0);
            dd.captionText = label;

            List<string> inkOptions = new List<string> {
                "White > CMYK", "CMYK", "Gloss Varnish", "White", "CMYK > White", 
                "White > CMYK > Gloss Varnish", "Sticker"
            };
            dd.AddOptions(inkOptions);

            // --- Global Preview/Print Buttons Section (standardized) ---
            GameObject bottomRow = UIFactory.CreateObject("BottomActions", rightPanel);
            RectTransform brRt = bottomRow.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(0, 0); brRt.anchorMax = new Vector2(1, 0);
            brRt.pivot = new Vector2(0.5f, 0); brRt.sizeDelta = new Vector2(-40, 56); brRt.anchoredPosition = new Vector2(0, 18);
            
            HorizontalLayoutGroup ahlg = bottomRow.AddComponent<HorizontalLayoutGroup>();
            ahlg.spacing = 12; ahlg.padding = new RectOffset(0, 0, 0, 0); ahlg.childControlWidth = true; ahlg.childForceExpandWidth = true; ahlg.childControlHeight = true; ahlg.childForceExpandHeight = false;
            GameObject previewBtn = UIFactory.CreateButton("Preview", bottomRow, Vector2.zero, new Vector2(0, 40), Color.white, new Color(0.2f, 0.2f, 0.2f));
            previewBtn.AddComponent<LayoutElement>().minHeight = 40;
            previewBtn.AddComponent<Outline>().effectColor = new Color(0.75f, 0.75f, 0.75f); previewBtn.GetComponent<Outline>().effectDistance = new Vector2(1, 1);
            previewBtn.GetComponent<Button>().onClick.AddListener(() => controller.OnPreviewRequested?.Invoke());
            GameObject printBtn = UIFactory.CreateButton("Print", bottomRow, Vector2.zero, new Vector2(0, 40), UIFactory.COLOR_ACCENT_GREEN, Color.white);
            printBtn.AddComponent<LayoutElement>().minHeight = 40;
            printBtn.GetComponent<Button>().onClick.AddListener(() => controller.OnPrintRequested?.Invoke());
        }

        private static void CreateGlobalInfoPanel(GameObject parent, CanvasController controller)
        {
            // Removed ScrollRect here as we added one to the main RightPanel container
            GameObject content = parent;
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20); vlg.spacing = 20;
            vlg.childControlHeight = true; vlg.childForceExpandWidth = true;

            // 1. Device Info Box
            GameObject deviceBox = UIFactory.CreateObject("DeviceBox", content);
            deviceBox.AddComponent<LayoutElement>().minHeight = 80;
            deviceBox.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            HorizontalLayoutGroup dhlg = deviceBox.AddComponent<HorizontalLayoutGroup>();
            dhlg.padding = new RectOffset(10, 10, 10, 10); dhlg.spacing = 15; dhlg.childAlignment = TextAnchor.MiddleLeft;

            GameObject thumb = UIFactory.CreateObject("Thumb", deviceBox);
            thumb.AddComponent<LayoutElement>().minWidth = 60; thumb.GetComponent<LayoutElement>().minHeight = 60;
            thumb.AddComponent<Image>().color = Color.black; 

            GameObject info = UIFactory.CreateObject("Info", deviceBox);
            VerticalLayoutGroup ivlg = info.AddComponent<VerticalLayoutGroup>();
            ivlg.childAlignment = TextAnchor.MiddleLeft; ivlg.spacing = 2;
            UIFactory.CreateText("NextMake 8260", info, 14, Color.black, Vector2.zero, new Vector2(150, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.CreateText("● Disconnected", info, 12, Color.gray, Vector2.zero, new Vector2(150, 18), TextAnchor.MiddleLeft);

            // 2. Print Bed Section
            GameObject bedSection = UIFactory.CreateObject("PrintBed", content);
            VerticalLayoutGroup bvlg = bedSection.AddComponent<VerticalLayoutGroup>(); bvlg.spacing = 10;
            
            GameObject bedHeader = UIFactory.CreateObject("Header", bedSection);
            bedHeader.AddComponent<LayoutElement>().minHeight = 25;
            HorizontalLayoutGroup bhhlg = bedHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("Print Bed", bedHeader, 14, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            
            GameObject bedInfoBtn = UIFactory.CreateObject("BedInfo", bedHeader);
            bedInfoBtn.AddComponent<LayoutElement>().minWidth = 20; bedInfoBtn.GetComponent<LayoutElement>().minHeight = 20;
            Text biText = bedInfoBtn.AddComponent<Text>();
            biText.text = "ⓘ"; biText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            biText.color = Color.gray; biText.alignment = TextAnchor.MiddleCenter; biText.fontSize = 16;
            biText.raycastTarget = true;
            bedInfoBtn.AddComponent<Button>().onClick.AddListener(() => CreatePrintBedModal(controller.editorArea));

            // Real Dropdown for Print Bed
            string[] bedOptions = { "Mini Flatbed", "Standard Flatbed", "Rotary", "Roll-To-Film" };
            CreateCustomDropdown("BedDropdown", bedSection, bedOptions, 1, (idx) => {});

            // Size row
            GameObject sizeRow = UIFactory.CreateObject("SizeRow", bedSection);
            sizeRow.AddComponent<LayoutElement>().minHeight = 40;
            HorizontalLayoutGroup shlg = sizeRow.AddComponent<HorizontalLayoutGroup>(); shlg.spacing = 10;
            
            System.Action<string, string> CreateInputField = (label, val) => {
                GameObject f = UIFactory.CreateObject("Field", sizeRow);
                f.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
                HorizontalLayoutGroup fhlg = f.AddComponent<HorizontalLayoutGroup>(); fhlg.padding = new RectOffset(10, 10, 0, 0);
                UIFactory.CreateText(label, f, 12, Color.gray, Vector2.zero, new Vector2(20, 0));
                UIFactory.CreateText(val, f, 13, Color.black, Vector2.zero, new Vector2(40, 0), TextAnchor.MiddleLeft);
                UIFactory.CreateText("mm", f, 12, Color.gray, Vector2.zero, new Vector2(30, 0), TextAnchor.MiddleRight);
            };
            CreateInputField("W", "335");
            CreateInputField("H", "420");

            // 3. Design Alignment
            GameObject alignSection = UIFactory.CreateObject("Alignment", content);
            VerticalLayoutGroup avg = alignSection.AddComponent<VerticalLayoutGroup>(); avg.spacing = 10;
            
            GameObject alignHeader = UIFactory.CreateObject("Header", alignSection);
            alignHeader.AddComponent<LayoutElement>().minHeight = 25;
            HorizontalLayoutGroup ahhlg = alignHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("Design Alignment", alignHeader, 14, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            
            GameObject alignInfoBtn = UIFactory.CreateObject("AlignInfo", alignHeader);
            alignInfoBtn.AddComponent<LayoutElement>().minWidth = 20; alignInfoBtn.GetComponent<LayoutElement>().minHeight = 20;
            Text aiText = alignInfoBtn.AddComponent<Text>();
            aiText.text = "ⓘ"; aiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            aiText.color = Color.gray; aiText.alignment = TextAnchor.MiddleCenter; aiText.fontSize = 16;
            aiText.raycastTarget = true;
            alignInfoBtn.AddComponent<Button>().onClick.AddListener(() => CreateAlignmentModal(controller.editorArea));

            GameObject photoAlign = UIFactory.CreateObject("PhotoAlign", alignSection);
            photoAlign.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            VerticalLayoutGroup pavg = photoAlign.AddComponent<VerticalLayoutGroup>(); pavg.padding = new RectOffset(15, 15, 15, 15); pavg.spacing = 10;
            
            GameObject paHeader = UIFactory.CreateObject("Header", photoAlign);
            paHeader.AddComponent<LayoutElement>().minHeight = 25;
            paHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("Photo Alignment", paHeader, 13, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text paCheck = UIFactory.CreateText("✔", paHeader, 14, UIFactory.COLOR_ACCENT_GREEN, Vector2.zero, new Vector2(20, 20), TextAnchor.MiddleRight).GetComponent<Text>();

            UIFactory.CreateButton("📷 Snapshot", photoAlign, Vector2.zero, new Vector2(0, 40), new Color(0.7f, 0.9f, 0.8f), Color.white);
            UIFactory.CreateButton("📷 Assisted shot", photoAlign, Vector2.zero, new Vector2(0, 40), Color.white, Color.gray).GetComponent<Button>().interactable = false;

            GameObject zeroAlign = UIFactory.CreateObject("ZeroAlign", alignSection);
            zeroAlign.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            HorizontalLayoutGroup zhlg = zeroAlign.AddComponent<HorizontalLayoutGroup>(); zhlg.padding = new RectOffset(15, 15, 10, 10);
            UIFactory.CreateText("Zero Point Alignment", zeroAlign, 13, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text zaCheck = UIFactory.CreateText("○", zeroAlign, 18, Color.gray, Vector2.zero, new Vector2(20, 20), TextAnchor.MiddleRight).GetComponent<Text>();

            // Alignment Switch Logic
            photoAlign.AddComponent<Button>().onClick.AddListener(() => {
                paCheck.text = "✔"; paCheck.color = UIFactory.COLOR_ACCENT_GREEN;
                zaCheck.text = "○"; zaCheck.color = Color.gray;
            });
            zeroAlign.AddComponent<Button>().onClick.AddListener(() => {
                paCheck.text = "○"; paCheck.color = Color.gray;
                zaCheck.text = "✔"; zaCheck.color = UIFactory.COLOR_ACCENT_GREEN;
            });

            // 4. Material
            GameObject matSection = UIFactory.CreateObject("Material", content);
            VerticalLayoutGroup mavg = matSection.AddComponent<VerticalLayoutGroup>(); mavg.spacing = 10;
            UIFactory.CreateText("Material", matSection, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);

            GameObject matRow = UIFactory.CreateObject("MatRow", matSection);
            matRow.AddComponent<LayoutElement>().minHeight = 40;
            HorizontalLayoutGroup mhlg = matRow.AddComponent<HorizontalLayoutGroup>(); mhlg.spacing = 10;
            
            string[] materials = { 
                "Unknown", "Wood", "Acrylic", "Metal", "Drawing Board", "Plastic", 
                "Ceramics", "Cotton canvas", "Polyester canvas", "Linen canvas", 
                "Artificial leather", "Genuine leather", "Cardboard" 
            };
            CreateCustomDropdown("MatDropdown", matRow, materials, 0, (idx) => {}).AddComponent<LayoutElement>().flexibleWidth = 1;

            GameObject setColorBackBtn = UIFactory.CreateObject("SetColorBack", matRow);
            var setColorBackLe = setColorBackBtn.AddComponent<LayoutElement>();
            setColorBackLe.minWidth = 120; setColorBackLe.minHeight = 36; setColorBackLe.preferredWidth = 130; setColorBackLe.preferredHeight = 36;
            Image setColorBackBg = setColorBackBtn.AddComponent<Image>(); setColorBackBg.color = new Color(0.95f, 0.95f, 0.95f);
            Outline setColorBackOut = setColorBackBtn.AddComponent<Outline>(); setColorBackOut.effectColor = new Color(0.75f, 0.75f, 0.75f); setColorBackOut.effectDistance = new Vector2(1, 1);
            HorizontalLayoutGroup setColorBackHlg = setColorBackBtn.AddComponent<HorizontalLayoutGroup>();
            setColorBackHlg.spacing = 6; setColorBackHlg.padding = new RectOffset(8, 8, 6, 6); setColorBackHlg.childAlignment = TextAnchor.MiddleCenter; setColorBackHlg.childControlWidth = false; setColorBackHlg.childForceExpandWidth = false;
            GameObject colorSwatch = UIFactory.CreateObject("ColorSwatch", setColorBackBtn);
            Image colorBoxImg = colorSwatch.AddComponent<Image>(); colorBoxImg.color = Color.white;
            colorSwatch.AddComponent<Outline>().effectColor = new Color(0.65f, 0.65f, 0.65f);
            var swatchLe = colorSwatch.AddComponent<LayoutElement>(); swatchLe.minWidth = 20; swatchLe.minHeight = 20; swatchLe.preferredWidth = 20; swatchLe.preferredHeight = 20;
            Text setColorBackLabel = UIFactory.CreateText("Set Color Back", setColorBackBtn, 12, new Color(0.2f, 0.2f, 0.2f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter).GetComponent<Text>();
            setColorBackLabel.raycastTarget = false;
            setColorBackBtn.AddComponent<Button>().onClick.AddListener(() => CreateColorPickerModal(controller.editorArea, (c) => {
                colorBoxImg.color = c;
                controller.SetPaperColor(c);
            }));

            GameObject bgCheck = UIFactory.CreateObject("BGCheck", matSection);
            HorizontalLayoutGroup bchlg = bgCheck.AddComponent<HorizontalLayoutGroup>();
            bchlg.spacing = 8;
            // USER REQ: Prevent checkbox row from stretching into a long bar
            bchlg.childForceExpandWidth = false;
            bchlg.childControlWidth = true;
            Button syncBtn = UIFactory.CreateButton("✔", bgCheck, Vector2.zero, new Vector2(20, 20), UIFactory.COLOR_ACCENT_GREEN, Color.white).GetComponent<Button>();
            // Force fixed size for the checkbox button
            var syncLe = syncBtn.gameObject.AddComponent<LayoutElement>();
            syncLe.minWidth = 20; syncLe.preferredWidth = 20;
            syncLe.minHeight = 20; syncLe.preferredHeight = 20;
            syncBtn.onClick.AddListener(() => {
                bool currentlyOn = syncBtn.GetComponent<Image>().color == UIFactory.COLOR_ACCENT_GREEN;
                bool nextOn = !currentlyOn;
                syncBtn.GetComponent<Image>().color = nextOn ? UIFactory.COLOR_ACCENT_GREEN : Color.gray;
                syncBtn.GetComponentInChildren<Text>().text = nextOn ? "✔" : "";
                controller.SetUseMaterialColor(nextOn);
                if (nextOn) controller.SetPaperColor(colorBoxImg.color);
            });
            var bgLabel = UIFactory.CreateText("Use material color as canvas background color ⓘ", bgCheck, 11, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            // Let label take remaining space, but don't force the row to expand other children
            var bgLabelLe = bgLabel.AddComponent<LayoutElement>();
            bgLabelLe.flexibleWidth = 1;

            // 5. Quality
            GameObject qualSection = UIFactory.CreateObject("Quality", content);
            VerticalLayoutGroup qavg = qualSection.AddComponent<VerticalLayoutGroup>(); qavg.spacing = 10;
            UIFactory.CreateText("Quality", qualSection, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);

            string[] qualityOptions = { "High Quality", "Standard", "Draft" };
            CreateCustomDropdown("QualDropdown", qualSection, qualityOptions, 0, (idx) => {});

            GameObject chokeSection = UIFactory.CreateObject("Choke", content);
            chokeSection.AddComponent<LayoutElement>().minHeight = 60;
            VerticalLayoutGroup cavg = chokeSection.AddComponent<VerticalLayoutGroup>(); cavg.spacing = 5;
            
            GameObject chokeHeader = UIFactory.CreateObject("Header", chokeSection);
            chokeHeader.AddComponent<LayoutElement>().minHeight = 20;
            HorizontalLayoutGroup chhlg = chokeHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("White Underbase Choke", chokeHeader, 13, Color.gray, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            
            GameObject chokeInfoBtn = UIFactory.CreateObject("ChokeInfo", chokeHeader);
            chokeInfoBtn.AddComponent<LayoutElement>().minWidth = 20; chokeInfoBtn.GetComponent<LayoutElement>().minHeight = 20;
            Text ciText = chokeInfoBtn.AddComponent<Text>();
            ciText.text = "ⓘ"; ciText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ciText.color = Color.gray; ciText.alignment = TextAnchor.MiddleCenter; ciText.fontSize = 14;
            ciText.raycastTarget = true;
            chokeInfoBtn.AddComponent<Button>().onClick.AddListener(() => CreateChokeModal(controller.editorArea));

            Text chokeValue = UIFactory.CreateText("0.2 mm", chokeHeader, 13, Color.black, Vector2.zero, new Vector2(60, 20), TextAnchor.MiddleRight, FontStyle.Bold).GetComponent<Text>();

            GameObject sliderObj = UIFactory.CreateObject("Slider", chokeSection);
            sliderObj.AddComponent<LayoutElement>().minHeight = 24;
            UIFactory.Stretch(sliderObj.GetComponent<RectTransform>());
            
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 0.5f; slider.value = 0.2f;
            slider.onValueChanged.AddListener((v) => chokeValue.text = $"{v:F1} mm");

            GameObject track = UIFactory.CreateObject("Track", sliderObj);
            RectTransform tr = track.GetComponent<RectTransform>(); tr.anchorMin = new Vector2(0, 0.5f); tr.anchorMax = new Vector2(1, 0.5f);
            tr.sizeDelta = new Vector2(0, 6); track.AddComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
            
            GameObject fill = UIFactory.CreateObject("Fill", track);
            RectTransform fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0.4f, 1);
            fr.sizeDelta = Vector2.zero; fill.AddComponent<Image>().color = UIFactory.COLOR_ACCENT_GREEN;
            slider.fillRect = fr;

            GameObject handle = UIFactory.CreateObject("Handle", sliderObj);
            RectTransform hr = handle.GetComponent<RectTransform>(); hr.anchorMin = new Vector2(0.4f, 0.5f); hr.anchorMax = new Vector2(0.4f, 0.5f);
            hr.sizeDelta = new Vector2(16, 16); hr.pivot = new Vector2(0.5f, 0.5f);
            Image handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;
            Outline handleOut = handle.AddComponent<Outline>(); handleOut.effectColor = new Color(0.6f, 0.6f, 0.6f); handleOut.effectDistance = new Vector2(1, 1);
            slider.handleRect = hr;

            // 6. Print Area
            GameObject areaBox = UIFactory.CreateObject("AreaBox", content);
            areaBox.AddComponent<LayoutElement>().minHeight = 80;
            areaBox.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            VerticalLayoutGroup abvlg = areaBox.AddComponent<VerticalLayoutGroup>(); abvlg.padding = new RectOffset(15, 15, 10, 10); abvlg.spacing = 10;
            
            GameObject areaHeader = UIFactory.CreateObject("Header", areaBox);
            areaHeader.AddComponent<LayoutElement>().minHeight = 20;
            HorizontalLayoutGroup abhlg = areaHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("⌄ Print area", areaHeader, 13, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);

            // Container for dynamic list
            GameObject listContainer = UIFactory.CreateObject("ListContainer", areaBox);
            listContainer.AddComponent<VerticalLayoutGroup>().spacing = 5;
            listContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            controller.printAreaListContainer = listContainer;
        }

        #region Helpers for Dropdowns and Modals
        public static GameObject CreateModalPopup(GameObject root, string title)
        {
            return CreateBaseModal(root, title, new Vector2(600, 450));
        }

        public static GameObject CreateColorPicker(GameObject root)
        {
            GameObject container = new GameObject("ColorPickerContainer");
            CreateColorPickerModal(root, null);
            return container; // Dummy return as the modal handles its own overlay
        }

        private static GameObject CreateCustomDropdown(string name, GameObject parent, string[] options, int defaultIdx, System.Action<int> onValueChanged)
        {
            GameObject dropdownObj = UIFactory.CreateObject(name, parent);
            dropdownObj.AddComponent<LayoutElement>().minHeight = 40;
            Image ddImg = dropdownObj.AddComponent<Image>(); ddImg.color = new Color(0.95f, 0.95f, 0.95f);
            Dropdown dd = dropdownObj.AddComponent<Dropdown>();
            dd.targetGraphic = ddImg;
            
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
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = new Vector2(10, 0);
            dd.captionText = label;

            dd.AddOptions(options.ToList());
            dd.value = defaultIdx;
            dd.onValueChanged.AddListener((i) => onValueChanged?.Invoke(i));
            
            return dropdownObj;
        }

        private static GameObject CreateBaseModal(GameObject root, string title, Vector2 size)
        {
            GameObject overlay = UIFactory.CreateObject("ModalOverlay", root);
            UIFactory.Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            overlay.AddComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));

            GameObject panel = UIFactory.CreateObject("Panel", overlay);
            RectTransform pRt = panel.GetComponent<RectTransform>();
            pRt.sizeDelta = size;
            panel.AddComponent<Image>().color = Color.white;
            panel.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            panel.AddComponent<Button>(); // Prevent clicking through

            GameObject header = UIFactory.CreateObject("Header", panel);
            RectTransform hRt = header.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1); hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1); hRt.sizeDelta = new Vector2(0, 50); hRt.anchoredPosition = Vector2.zero;
            
            if (!string.IsNullOrEmpty(title))
            {
                Text t = UIFactory.CreateText(title, header, 18, Color.black, Vector2.zero, new Vector2(0, 50), TextAnchor.MiddleLeft, FontStyle.Bold).GetComponent<Text>();
                t.rectTransform.offsetMin = new Vector2(20, 0);
                t.raycastTarget = false;
            }
            
            GameObject closeBtn = UIFactory.CreateObject("CloseBtn", header);
            RectTransform cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1, 0.5f); cbRt.anchorMax = new Vector2(1, 0.5f);
            cbRt.sizeDelta = new Vector2(40, 40); cbRt.anchoredPosition = new Vector2(-25, 0);
            
            Text closeTxt = UIFactory.CreateText("✕", closeBtn, 20, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            closeTxt.raycastTarget = true;
            closeBtn.AddComponent<Button>().onClick.AddListener(() => Object.Destroy(overlay));

            GameObject content = UIFactory.CreateObject("Content", panel);
            RectTransform cRt = content.GetComponent<RectTransform>();
            UIFactory.Stretch(cRt); cRt.offsetMax = new Vector2(0, -50);
            
            return content;
        }

        private static void CreatePrintBedModal(GameObject root)
        {
            GameObject content = CreateBaseModal(root, "", new Vector2(800, 600));
            GameObject panel = content.transform.parent.gameObject;
            
            // Tabs Container at Top of Content
            GameObject tabs = UIFactory.CreateObject("Tabs", content);
            RectTransform tRt = tabs.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1); tRt.sizeDelta = new Vector2(0, 50); tRt.anchoredPosition = new Vector2(0, 0);
            
            HorizontalLayoutGroup thlg = tabs.AddComponent<HorizontalLayoutGroup>();
            thlg.childAlignment = TextAnchor.MiddleLeft; thlg.spacing = 30; thlg.padding = new RectOffset(40, 0, 0, 0);

            string[] bedModes = { "Standard Flatbed", "Mini Flatbed", "Rotary", "Roll-To-Film" };
            GameObject infoArea = UIFactory.CreateObject("InfoArea", content);
            UIFactory.Stretch(infoArea.GetComponent<RectTransform>()); 
            infoArea.GetComponent<RectTransform>().offsetMin = new Vector2(40, 40); 
            infoArea.GetComponent<RectTransform>().offsetMax = new Vector2(-40, -60); // Below tabs
            VerticalLayoutGroup ivlg = infoArea.AddComponent<VerticalLayoutGroup>(); ivlg.spacing = 20;

            System.Action<int> SwitchTab = (idx) => {
                for (int i = 0; i < tabs.transform.childCount; i++)
                {
                    var tabObj = tabs.transform.GetChild(i).gameObject;
                    var outline = tabObj.GetComponent<Outline>();
                    var txt = tabObj.GetComponentInChildren<Text>();
                    bool isSelected = (i == idx);
                    
                    outline.enabled = isSelected;
                    tabObj.GetComponent<Image>().color = isSelected ? new Color(0.95f, 0.95f, 0.95f) : Color.white;
                    if(txt) txt.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                }
                
                foreach(Transform child in infoArea.transform) Object.Destroy(child.gameObject);
                UIFactory.CreateText("Material Requirements", infoArea, 16, Color.black, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.CreateText("• The height of the object must not exceed 60 mm. If printing without photo measurement, the height of the object must not exceed 100 mm.\n• The surface height variation does not exceed 2 mm. Do not use objects of different heights, otherwise it will affect printing quality.", 
                    infoArea, 14, Color.gray, Vector2.zero, new Vector2(0, 80), TextAnchor.MiddleLeft);
                
                GameObject imgMock = UIFactory.CreateObject("ImageMock", infoArea);
                imgMock.AddComponent<LayoutElement>().flexibleHeight = 1;
                imgMock.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
                UIFactory.CreateText("Illustration for " + bedModes[idx], imgMock, 14, Color.gray, Vector2.zero, Vector2.zero);
            };

            for(int i=0; i<bedModes.Length; i++) {
                int idx = i;
                GameObject tObj = UIFactory.CreateObject("Tab_"+i, tabs);
                tObj.AddComponent<Image>().color = Color.white; // Add Image component
                tObj.AddComponent<LayoutElement>().minWidth = 120; tObj.AddComponent<LayoutElement>().preferredHeight = 50;
                Text t = UIFactory.CreateText(bedModes[i], tObj, 15, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
                t.raycastTarget = true;
                UIFactory.Stretch(t.rectTransform);
                tObj.AddComponent<Button>().onClick.AddListener(() => SwitchTab(idx));
                Outline outline = tObj.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(0, -2); outline.enabled = false;
            }
            SwitchTab(0);
        }

        private static void CreateAlignmentModal(GameObject root)
        {
            GameObject content = CreateBaseModal(root, "", new Vector2(700, 500));
            GameObject panel = content.transform.parent.gameObject;
            
            GameObject tabs = UIFactory.CreateObject("Tabs", content);
            RectTransform tRt = tabs.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1); tRt.sizeDelta = new Vector2(0, 50); tRt.anchoredPosition = new Vector2(0, 0);
            
            HorizontalLayoutGroup thlg = tabs.AddComponent<HorizontalLayoutGroup>();
            thlg.childAlignment = TextAnchor.MiddleLeft; thlg.spacing = 30; thlg.padding = new RectOffset(40, 0, 0, 0);

            GameObject infoArea = UIFactory.CreateObject("InfoArea", content);
            UIFactory.Stretch(infoArea.GetComponent<RectTransform>()); 
            infoArea.GetComponent<RectTransform>().offsetMin = new Vector2(40, 40); 
            infoArea.GetComponent<RectTransform>().offsetMax = new Vector2(-40, -60);
            VerticalLayoutGroup ivlg = infoArea.AddComponent<VerticalLayoutGroup>(); ivlg.spacing = 15;

            string[] modes = { "Photo Alignment", "Zero Point Alignment" };
            System.Action<int> SwitchTab = (idx) => {
                for (int i = 0; i < tabs.transform.childCount; i++)
                {
                    var tabObj = tabs.transform.GetChild(i).gameObject;
                    var outline = tabObj.GetComponent<Outline>();
                    var txt = tabObj.GetComponentInChildren<Text>();
                    bool isSelected = (i == idx);
                    
                    outline.enabled = isSelected;
                    tabObj.GetComponent<Image>().color = isSelected ? new Color(0.95f, 0.95f, 0.95f) : Color.white;
                    if(txt) txt.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                }
                
                foreach(Transform child in infoArea.transform) Object.Destroy(child.gameObject);
                UIFactory.CreateText("1/3 Introduction to " + modes[idx], infoArea, 16, Color.black, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft, FontStyle.Bold);
                
                GameObject tag = UIFactory.CreateObject("Tag", infoArea);
                tag.AddComponent<LayoutElement>().minHeight = 24; tag.AddComponent<LayoutElement>().preferredWidth = 200;
                tag.AddComponent<Image>().color = new Color(1, 0.95f, 0.9f);
                tag.AddComponent<Outline>().effectColor = new Color(1f, 0.5f, 0f);
                UIFactory.CreateText("Substrate height must be ≤ 60mm", tag, 12, new Color(1f, 0.5f, 0f), Vector2.zero, Vector2.zero).GetComponent<Text>().raycastTarget = false;

                UIFactory.CreateText(modes[idx] + ": Captures an image of the printing bed to display the substrate's actual position on the canvas. Simply drag and drop your artwork directly onto the substrate for precise visual alignment.", 
                    infoArea, 14, Color.black, Vector2.zero, new Vector2(0, 80), TextAnchor.MiddleLeft);
                
                GameObject imgMock = UIFactory.CreateObject("ImageMock", infoArea);
                imgMock.AddComponent<LayoutElement>().flexibleHeight = 1;
                imgMock.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
                UIFactory.CreateText("Alignment Preview", imgMock, 14, Color.gray, Vector2.zero, Vector2.zero);
            };

            for(int i=0; i<modes.Length; i++) {
                int idx = i;
                GameObject tObj = UIFactory.CreateObject("Tab_"+i, tabs);
                tObj.AddComponent<Image>().color = Color.white; // Add Image component
                tObj.AddComponent<LayoutElement>().minWidth = 150; tObj.AddComponent<LayoutElement>().preferredHeight = 50;
                Text t = UIFactory.CreateText(modes[i], tObj, 15, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
                t.raycastTarget = true;
                UIFactory.Stretch(t.rectTransform);
                tObj.AddComponent<Button>().onClick.AddListener(() => SwitchTab(idx));
                Outline outline = tObj.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(0, -2); outline.enabled = false;
            }
            SwitchTab(0);
        }

        private static void CreateColorPickerModal(GameObject root, System.Action<Color> onColorPicked)
        {
            GameObject content = CreateBaseModal(root, "Material Color", new Vector2(400, 550));
            GameObject panel = content.transform.parent.gameObject;
            RectTransform pRt = panel.GetComponent<RectTransform>();
            // Position near right panel left edge (Right Panel is 300 wide at right anchor)
            pRt.anchorMin = new Vector2(1, 0.5f); pRt.anchorMax = new Vector2(1, 0.5f);
            pRt.anchoredPosition = new Vector2(-520, 0); // 300 (right panel) + 200 (offset) + 20 (padding)

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>(); vlg.padding = new RectOffset(20, 20, 20, 20); vlg.spacing = 15;

            GameObject colorArea = UIFactory.CreateObject("ColorArea", content);
            colorArea.AddComponent<LayoutElement>().minHeight = 250;
            Image areaImg = colorArea.AddComponent<Image>(); 
            
            // Generate Sat/Val Gradient Texture
            Texture2D svTexture = new Texture2D(100, 100);
            for(int y=0; y<100; y++) {
                for(int x=0; x<100; x++) {
                    svTexture.SetPixel(x, y, Color.HSVToRGB(0, x/100f, y/100f));
                }
            }
            svTexture.Apply();
            areaImg.sprite = Sprite.Create(svTexture, new Rect(0,0,100,100), Vector2.zero);

            GameObject pickerCircle = UIFactory.CreateObject("Picker", colorArea);
            RectTransform pcr = pickerCircle.GetComponent<RectTransform>();
            pcr.sizeDelta = new Vector2(16, 16);
            pickerCircle.AddComponent<Image>().color = Color.white;
            pickerCircle.AddComponent<Outline>().effectColor = Color.black;

            GameObject hueSliderObj = UIFactory.CreateObject("HueSlider", content);
            hueSliderObj.AddComponent<LayoutElement>().minHeight = 24;
            Image hueImg = hueSliderObj.AddComponent<Image>();
            
            // Generate Hue Gradient Texture
            Texture2D hueTex = new Texture2D(100, 1);
            for(int i=0; i<100; i++) hueTex.SetPixel(i, 0, Color.HSVToRGB(i/100f, 1, 1));
            hueTex.Apply();
            hueImg.sprite = Sprite.Create(hueTex, new Rect(0,0,100,1), Vector2.zero);
            
            Slider hueSlider = hueSliderObj.AddComponent<Slider>();
            hueSlider.minValue = 0; hueSlider.maxValue = 1;
            
            // Add Handle to Hue Slider
            GameObject hueHandle = UIFactory.CreateObject("Handle", hueSliderObj);
            RectTransform hhRt = hueHandle.GetComponent<RectTransform>();
            hhRt.sizeDelta = new Vector2(20, 20);
            hueHandle.AddComponent<Image>().color = Color.white;
            hueHandle.AddComponent<Outline>().effectColor = Color.gray;
            hueSlider.handleRect = hhRt;

            GameObject inputs = UIFactory.CreateObject("Inputs", content);
            inputs.AddComponent<LayoutElement>().minHeight = 40;
            HorizontalLayoutGroup ihlg = inputs.AddComponent<HorizontalLayoutGroup>(); ihlg.spacing = 10;
            
            GameObject hexBox = UIFactory.CreateObject("HexBox", inputs);
            hexBox.AddComponent<LayoutElement>().minWidth = 100;
            hexBox.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
            InputField hexIn = hexBox.AddComponent<InputField>();
            Text hexTxt = UIFactory.CreateText("FFFFFF", hexBox, 14, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            hexTxt.alignment = TextAnchor.MiddleCenter;
            hexIn.textComponent = hexTxt;

            // Add Handler
            ColorPickerHandler handler = colorArea.AddComponent<ColorPickerHandler>();
            handler.pickerCircle = pcr;
            handler.areaImage = areaImg;
            handler.hueSlider = hueSlider;
            handler.hexInput = hexIn;
            handler.onColorChanged = onColorPicked;

            UIFactory.CreateText("Recommend colors", content, 13, Color.gray, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft);
            GameObject grid = UIFactory.CreateObject("Grid", content);
            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>(); glg.cellSize = new Vector2(35, 35); glg.spacing = new Vector2(8, 8);
            Color[] recommends = { Color.white, Color.black, Color.red, Color.magenta, new Color(1f, 0.5f, 0f), Color.yellow, Color.green, Color.cyan, Color.blue, Color.gray };
            foreach(var c in recommends) {
                GameObject item = UIFactory.CreateObject("Color", grid);
                item.AddComponent<Image>().color = c;
                item.AddComponent<Button>().onClick.AddListener(() => handler.SetColor(c));
            }
        }

        private static void CreateChokeModal(GameObject root)
        {
            GameObject content = CreateBaseModal(root, "White Underbase Choke", new Vector2(600, 500));
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>(); vlg.padding = new RectOffset(30, 30, 30, 30); vlg.spacing = 20;

            GameObject imgArea = UIFactory.CreateObject("Images", content);
            imgArea.AddComponent<LayoutElement>().flexibleHeight = 1;
            imgArea.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);
            HorizontalLayoutGroup ihlg = imgArea.AddComponent<HorizontalLayoutGroup>(); ihlg.padding = new RectOffset(20, 20, 20, 20); ihlg.spacing = 40;
            
            System.Action<string> CreatePumpkin = (label) => {
                GameObject p = UIFactory.CreateObject("Pumpkin", imgArea);
                VerticalLayoutGroup pvlg = p.AddComponent<VerticalLayoutGroup>(); pvlg.spacing = 10;
                GameObject icon = UIFactory.CreateObject("Icon", p);
                icon.AddComponent<LayoutElement>().flexibleHeight = 1;
                UIFactory.CreateText("🎃", icon, 80, new Color(1f, 0.5f, 0f), Vector2.zero, Vector2.zero);
                UIFactory.CreateText(label, p, 13, Color.gray, Vector2.zero, new Vector2(0, 20));
            };
            CreatePumpkin("Before");
            CreatePumpkin("After");

            UIFactory.CreateText("White Underbase Choke shrinks the white ink layer slightly compared to the CMYK layer, preventing unwanted white outlines in your finished print.", 
                content, 14, Color.gray, Vector2.zero, new Vector2(0, 60), TextAnchor.MiddleLeft);
            
            GameObject footer = UIFactory.CreateObject("Footer", content);
            footer.AddComponent<LayoutElement>().minHeight = 50;
            UIFactory.CreateButton("OK", footer, new Vector2(200, 0), new Vector2(120, 40), new Color(0.15f, 0.15f, 0.18f), Color.white)
                .GetComponent<Button>().onClick.AddListener(() => Object.Destroy(content.transform.parent.gameObject));
            footer.GetComponentInChildren<Button>().transform.localPosition = new Vector2(200, 0);
        }
        #endregion

        private static void CreateContextToolbar(GameObject workspace, CanvasController controller)
        {
            GameObject ct = UIFactory.CreateObject("ContextToolbar", workspace);
            RectTransform ctRect = ct.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0.5f, 0.5f); ctRect.anchorMax = new Vector2(0.5f, 0.5f); ctRect.pivot = new Vector2(0.5f, 0.5f);
            ctRect.sizeDelta = new Vector2(680, 50); ctRect.anchoredPosition = new Vector2(0, 340); 
            ct.AddComponent<Image>().color = Color.white; ct.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            HorizontalLayoutGroup hlg = ct.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.padding = new RectOffset(20, 20, 5, 5); hlg.childAlignment = TextAnchor.MiddleCenter;

            string[] tools = { "Crop", "Eraser", "Opacity", "Image Cutting", "UpScaler", "AI Remover", "Cutout", "Outline" };
            foreach(var tool in tools) UIFactory.CreateButton(tool, ct, Vector2.zero, new Vector2(0, 30), Color.white, Color.black).AddComponent<LayoutElement>().flexibleWidth = 1;
            ct.SetActive(false); controller.contextToolbar = ct;
            ct.transform.SetAsLastSibling();
        }

        private static void SetupGrid(GameObject parent, int count, System.Action<int> onClick, string prefix = "T") {
            GameObject grid = UIFactory.CreateObject("Grid", parent);
            UIFactory.Stretch(grid.GetComponent<RectTransform>());
            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(110, 110); glg.spacing = new Vector2(10, 10);
            for(int k=0; k<count; k++) {
                GameObject item = UIFactory.CreateObject(prefix+k, grid);
                int index = k; item.AddComponent<Button>().onClick.AddListener(() => onClick(index));
                item.AddComponent<Image>().color = Color.HSVToRGB((float)k/(float)count, 0.5f, 0.9f);
            }
        }

        private static void AddManipulationComponents(GameObject go) {
            if(!go.GetComponent<CanvasGroup>()) go.AddComponent<CanvasGroup>();

            if(!go.GetComponent<BoxCollider2D>()) go.AddComponent<BoxCollider2D>().size = new Vector2(100, 100); 
            if(!go.GetComponent<ObjectManipulator>()) go.AddComponent<ObjectManipulator>();
        }
    }
}

