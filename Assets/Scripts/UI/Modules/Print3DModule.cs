using UnityEngine;
using UnityEngine.UI;
using PocoRender.UI.Core;
using PocoRender.UI; // For PocoRenderStudioUIManager, Print3DStudioLayout

namespace PocoRender.UI.Modules
{
    public class Print3DModule
    {
        public static void CreatePrint3DLayout(GameObject parent, PocoRenderStudioUIManager manager)
        {
            GameObject layoutObj = UIFactory.CreateObject("Print3DStudioLayout", parent);
            UIFactory.Stretch(layoutObj.GetComponent<RectTransform>());
            layoutObj.AddComponent<Image>().color = UIFactory.COLOR_3D_BG;

            Print3DStudioLayout layout = layoutObj.AddComponent<Print3DStudioLayout>();
            layout.mainContainer = layoutObj.GetComponent<RectTransform>();
            manager.print3DLayout = layout;

            // Function Bar (top of window — no separate menu row, Qt handles menus)
            GameObject funcBar = UIFactory.CreateObject("FunctionBar", layoutObj);
            RectTransform frRect = funcBar.GetComponent<RectTransform>();
            frRect.anchorMin = new Vector2(0, 1); frRect.anchorMax = new Vector2(1, 1);
            frRect.pivot = new Vector2(0.5f, 1);
            frRect.sizeDelta = new Vector2(0, 50); frRect.anchoredPosition = Vector2.zero;
            funcBar.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            layout.secondRow = frRect;

            GameObject funcTabs = UIFactory.CreateObject("FuncTabs", funcBar);
            HorizontalLayoutGroup ftlg = funcTabs.AddComponent<HorizontalLayoutGroup>();
            ftlg.spacing = 20; ftlg.padding = new RectOffset(20, 0, 0, 0); ftlg.childAlignment = TextAnchor.MiddleLeft;
            RectTransform ftRect = funcTabs.GetComponent<RectTransform>();
            ftRect.anchorMin = new Vector2(0, 0); ftRect.anchorMax = new Vector2(0.6f, 1);
            ftRect.offsetMin = Vector2.zero; ftRect.offsetMax = Vector2.zero;

            UIFactory.CreateTextButton("HOME", funcTabs, 12, UIFactory.COLOR_ACCENT_GREEN);
            
            GameObject sep = UIFactory.CreateObject("Sep", funcTabs);
            sep.AddComponent<LayoutElement>().minWidth = 20;

            Button btnExplore = UIFactory.CreateButton("MODEL", funcTabs, Vector2.zero, new Vector2(60, 40), Color.clear, UIFactory.COLOR_TEXT_LIGHT).GetComponent<Button>();
            Button btnSlice = UIFactory.CreateButton("SLICE", funcTabs, Vector2.zero, new Vector2(60, 40), Color.clear, UIFactory.COLOR_TEXT_LIGHT).GetComponent<Button>();
            Button btnDevices = UIFactory.CreateButton("DEVICES", funcTabs, Vector2.zero, new Vector2(70, 40), Color.clear, UIFactory.COLOR_TEXT_LIGHT).GetComponent<Button>();

            GameObject rightActions = UIFactory.CreateObject("RightActions", funcBar);
            HorizontalLayoutGroup ralg = rightActions.AddComponent<HorizontalLayoutGroup>();
            ralg.spacing = 15; ralg.childAlignment = TextAnchor.MiddleRight; ralg.padding = new RectOffset(0, 20, 0, 0);
            RectTransform raRect = rightActions.GetComponent<RectTransform>();
            raRect.anchorMin = new Vector2(0.6f, 0); raRect.anchorMax = new Vector2(1, 1);
            raRect.offsetMin = Vector2.zero; raRect.offsetMax = Vector2.zero;

            GameObject switcher = UIFactory.CreateObject("Switcher3D", rightActions);
            switcher.AddComponent<LayoutElement>().minWidth = 160; switcher.GetComponent<LayoutElement>().minHeight = 36;
            Image swImg = switcher.AddComponent<Image>(); swImg.color = new Color(0.15f, 0.15f, 0.15f);
            Button swBtn = switcher.AddComponent<Button>();
            swBtn.onClick.AddListener(() => {
                Debug.Log("Switcher 3D Clicked");
                manager.ShowSelectionDialog();
            });
            UIFactory.CreateText("3D Print Studio v", switcher, 14, UIFactory.COLOR_TEXT_LIGHT, Vector2.zero, Vector2.zero);

            UIFactory.CreateTextButton("NOTIF", rightActions, 10, Color.gray);
            UIFactory.CreateTextButton("HELP", rightActions, 10, Color.gray);


            // 3. Content Area
            GameObject sidebar = UIFactory.CreateObject("Sidebar", layoutObj);
            RectTransform sr = sidebar.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(0, 1);
            sr.pivot = new Vector2(0, 1);
            sr.sizeDelta = new Vector2(260, 0); sr.anchoredPosition = new Vector2(0, -50);
            sr.offsetMax = new Vector2(260, -50);
            sidebar.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

            VerticalLayoutGroup vlg = sidebar.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 30, 30); vlg.spacing = 10;
            UIFactory.CreateText("Make It Real", sidebar, 20, Color.white, Vector2.zero, new Vector2(0, 40), TextAnchor.MiddleLeft, FontStyle.Bold);
            
            string[] sItems = { "Home", "3D Paint", "City Print", "Model Database", "Rewards", "Store" };
            foreach(var s in sItems)
            {
                GameObject item = UIFactory.CreateObject(s, sidebar);
                item.AddComponent<LayoutElement>().minHeight = 36;
                UIFactory.CreateText(s, item, 14, Color.gray, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            }
            UIFactory.CreateButton("+ Create", sidebar, Vector2.zero, new Vector2(0, 40), Color.clear, Color.white);

            GameObject contentContainer = UIFactory.CreateObject("ContentContainer", layoutObj);
            RectTransform cr = contentContainer.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 0); cr.anchorMax = new Vector2(1, 1);
            cr.offsetMin = new Vector2(260, 0); cr.offsetMax = new Vector2(0, -50);

            GameObject lang = UIFactory.CreateObject("Lang", contentContainer);
            RectTransform lr = lang.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(1, 1); lr.anchorMax = new Vector2(1, 1);
            lr.sizeDelta = new Vector2(100, 30); lr.anchoredPosition = new Vector2(-20, -20);
            UIFactory.CreateText("English(US) v", lang, 12, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleRight);

            GameObject exploreView = UIFactory.CreateObject("ExploreView", contentContainer);
            UIFactory.Stretch(exploreView.GetComponent<RectTransform>());
            VerticalLayoutGroup evlg = exploreView.AddComponent<VerticalLayoutGroup>();
            evlg.padding = new RectOffset(40, 40, 60, 40); evlg.spacing = 30;

            UIFactory.CreateText("Create Stunning 3D Models with AI", exploreView, 24, Color.white, Vector2.zero, new Vector2(0, 40), TextAnchor.MiddleLeft, FontStyle.Bold);
            
            GameObject cards = UIFactory.CreateObject("BigCards", exploreView);
            cards.AddComponent<LayoutElement>().minHeight = 220;
            HorizontalLayoutGroup chlg = cards.AddComponent<HorizontalLayoutGroup>(); chlg.spacing = 30;
            CreateBigCard3D("3D Paint", new Color(0.4f, 0.8f, 0.3f), cards);
            CreateBigCard3D("City Print", new Color(0.3f, 0.5f, 0.9f), cards);

            GameObject sliceView = UIFactory.CreateObject("SliceView", contentContainer);
            UIFactory.Stretch(sliceView.GetComponent<RectTransform>());
            UIFactory.CreateText("Slicing Interface Placeholder", sliceView, 32, Color.gray, Vector2.zero, Vector2.zero);
            sliceView.SetActive(false);

            GameObject devicesView = UIFactory.CreateObject("DevicesView", contentContainer);
            UIFactory.Stretch(devicesView.GetComponent<RectTransform>());
            UIFactory.CreateText("Device Manager Placeholder", devicesView, 32, Color.gray, Vector2.zero, Vector2.zero);
            devicesView.SetActive(false);

            layout.sidebar = sr;
            layout.contentArea = cr;
            layout.exploreView = exploreView;
            layout.sliceView = sliceView;
            layout.devicesView = devicesView;
            layout.btnExplore = btnExplore;
            layout.btnSlice = btnSlice;
            layout.btnDevices = btnDevices;

            btnExplore.onClick.AddListener(() => layout.SwitchTab("Explore"));
            btnSlice.onClick.AddListener(() => layout.SwitchTab("Slice"));
            btnDevices.onClick.AddListener(() => layout.SwitchTab("Devices"));

            layout.Hide();
        }

        private static void CreateBigCard3D(string title, Color color, GameObject parent)
        {
            GameObject card = UIFactory.CreateObject(title, parent);
            card.AddComponent<LayoutElement>().flexibleWidth = 1;
            card.AddComponent<Image>().color = color;
            
            UIFactory.CreateText(title, card, 32, UIFactory.COLOR_TEXT_DARK, new Vector2(30, -30), new Vector2(300, 50), TextAnchor.UpperLeft, FontStyle.Bold);
        }
    }
}



