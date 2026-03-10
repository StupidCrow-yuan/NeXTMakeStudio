using UnityEngine;
using UnityEngine.UI;
using PocoRender.UI.Core;

namespace PocoRender.UI.Modules
{
    public static class CanvasLeftPanelBuilder
    {
        public static void SetupLeftMenu(GameObject editorArea, RectTransform paper, CanvasController controller)
        {
            GameObject leftArea = UIFactory.CreateObject("LeftArea", editorArea);
            RectTransform laRect = leftArea.GetComponent<RectTransform>();
            laRect.anchorMin = new Vector2(0, 0); laRect.anchorMax = new Vector2(0.3f, 1);
            laRect.offsetMin = Vector2.zero; laRect.offsetMax = Vector2.zero;

            GameObject leftToolBar = UIFactory.CreateObject("LeftToolBar", leftArea);
            RectTransform ltbRect = leftToolBar.GetComponent<RectTransform>();
            ltbRect.anchorMin = new Vector2(0, 0);
            ltbRect.anchorMax = new Vector2(1f / 6f, 1);
            ltbRect.offsetMin = Vector2.zero;
            ltbRect.offsetMax = Vector2.zero;
            leftToolBar.AddComponent<Image>().color = Color.white;

            VerticalLayoutGroup ltbVlg = leftToolBar.AddComponent<VerticalLayoutGroup>();
            ltbVlg.spacing = 6; ltbVlg.padding = new RectOffset(6, 6, 12, 8); ltbVlg.childAlignment = TextAnchor.UpperCenter;

            GameObject drawer = UIFactory.CreateObject("Drawer", leftArea);
            RectTransform dRect = drawer.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(1f / 6f, 0);
            dRect.anchorMax = new Vector2(1, 1);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;
            drawer.AddComponent<Image>().color = Color.white; drawer.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            VerticalLayoutGroup dVlg = drawer.AddComponent<VerticalLayoutGroup>();
            dVlg.padding = new RectOffset(16, 16, 16, 16); dVlg.spacing = 10;
            dVlg.childControlHeight = true; dVlg.childForceExpandHeight = false;
            dVlg.childControlWidth = true; dVlg.childForceExpandWidth = true;

            GameObject divider = UIFactory.CreateObject("Divider", leftArea);
            RectTransform divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(1f / 6f, 0);
            divRt.anchorMax = new Vector2(1f / 6f, 1);
            divRt.pivot = new Vector2(0.5f, 0.5f);
            divRt.anchoredPosition = Vector2.zero;
            divRt.sizeDelta = new Vector2(2f, 0);
            divider.AddComponent<Image>().color = new Color(0.84f, 0.84f, 0.84f, 1f);
            divider.transform.SetAsLastSibling();

            GameObject titleTxt = UIFactory.CreateText("Templates", drawer, 20, Color.black, Vector2.zero, new Vector2(0, 32), TextAnchor.MiddleLeft, FontStyle.Bold);
            var titleLe = titleTxt.AddComponent<LayoutElement>();
            titleLe.minHeight = 32; titleLe.flexibleHeight = 0;

            GameObject searchBar = UIFactory.CreateObject("Search", drawer);
            searchBar.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            var searchLe = searchBar.AddComponent<LayoutElement>();
            searchLe.minHeight = 28; searchLe.preferredHeight = 28; searchLe.flexibleHeight = 0;
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
                    case "Upload":
                        {
                            GameObject uploadWrap = UIFactory.CreateObject("UploadWrap", contentRoot);
                            UIFactory.Stretch(uploadWrap.GetComponent<RectTransform>());
                            VerticalLayoutGroup uvlg = uploadWrap.AddComponent<VerticalLayoutGroup>();
                            uvlg.spacing = 6;
                            uvlg.padding = new RectOffset(0, 0, 0, 0);
                            uvlg.childAlignment = TextAnchor.UpperLeft;
                            uvlg.childControlHeight = true;
                            uvlg.childControlWidth = true;
                            uvlg.childForceExpandHeight = false;

                            string supported = CanvasController.GetUploadSupportedFormatsText();
                            GameObject uploadBtn = UIFactory.CreateButton($"Upload ({supported})", uploadWrap, Vector2.zero, new Vector2(0, 36), Color.white, Color.black);
                            LayoutElement btnLe = uploadBtn.AddComponent<LayoutElement>();
                            btnLe.minHeight = 36;
                            btnLe.flexibleHeight = 0;
                            uploadBtn.GetComponent<Button>().onClick.AddListener(() => controller.OnUploadCanvasAsset());

                            GameObject listBg = UIFactory.CreateObject("UploadListBg", uploadWrap);
                            listBg.AddComponent<Image>().color = new Color(0.97f, 0.97f, 0.97f, 1f);
                            LayoutElement listBgLe = listBg.AddComponent<LayoutElement>();
                            listBgLe.flexibleHeight = 1;

                            ScrollRect sr = listBg.AddComponent<ScrollRect>();
                            sr.horizontal = false;
                            sr.vertical = true;
                            sr.scrollSensitivity = 30f;
                            sr.movementType = ScrollRect.MovementType.Clamped;

                            GameObject vp = UIFactory.CreateObject("Viewport", listBg);
                            UIFactory.Stretch(vp.GetComponent<RectTransform>());
                            vp.GetComponent<RectTransform>().offsetMin = new Vector2(4, 4);
                            vp.GetComponent<RectTransform>().offsetMax = new Vector2(-4, -4);
                            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0);
                            vp.AddComponent<RectMask2D>();

                            GameObject listContent = UIFactory.CreateObject("Content", vp);
                            RectTransform lcRt = listContent.GetComponent<RectTransform>();
                            lcRt.anchorMin = new Vector2(0, 1);
                            lcRt.anchorMax = new Vector2(1, 1);
                            lcRt.pivot = new Vector2(0.5f, 1);
                            lcRt.sizeDelta = new Vector2(0, 0);
                            VerticalLayoutGroup lvlg = listContent.AddComponent<VerticalLayoutGroup>();
                            lvlg.spacing = 6;
                            lvlg.padding = new RectOffset(4, 4, 4, 4);
                            lvlg.childControlHeight = true;
                            lvlg.childControlWidth = true;
                            lvlg.childForceExpandHeight = false;
                            listContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                            UIFactory.CreateText("No uploads yet.", listContent, 11, Color.gray, Vector2.zero, new Vector2(0, 22), TextAnchor.MiddleLeft, FontStyle.Normal).name = "UploadEmptyHint";

                            sr.viewport = vp.GetComponent<RectTransform>();
                            sr.content = lcRt;
                            controller.uploadListContainer = listContent;
                        }
                        break;
                    case "Templates":
                        // Automatically load all images from Resources/CanVas/Templates
                        Object[] templateImages = Resources.LoadAll("CanVas/Templates", typeof(Sprite));
                        if (templateImages != null && templateImages.Length > 0)
                        {
                            CanvasWorkspaceBuilder.SetupGrid(contentRoot, templateImages.Length, (i) => {
                                Sprite sp = templateImages[i] as Sprite;
                                GameObject addedImg = UIFactory.CreateObject("Design_" + i, paper.gameObject);
                                addedImg.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);
                                Image imgComp = addedImg.AddComponent<Image>();
                                imgComp.sprite = sp;
                                imgComp.color = Color.white;
                                imgComp.preserveAspect = true;
                                CanvasWorkspaceBuilder.AddManipulationComponents(addedImg);
                                controller.RecordAdd(addedImg);
                            }, "T", templateImages); // Pass images to SetupGrid to show thumbnails
                        }
                        else
                        {
                            // Fallback if no images found
                            CanvasWorkspaceBuilder.SetupGrid(contentRoot, 6, (i) => {
                                GameObject addedImg = UIFactory.CreateObject("Design_" + i, paper.gameObject);
                                addedImg.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);
                                addedImg.AddComponent<Image>().color = Color.HSVToRGB((float)i / 6f, 0.5f, 0.9f);
                                CanvasWorkspaceBuilder.AddManipulationComponents(addedImg);
                                controller.RecordAdd(addedImg);
                            }, "T");
                        }
                        break;
                    case "Text":
                        CanvasWorkspaceBuilder.SetupGrid(contentRoot, 4, (i) => {
                            GameObject t = UIFactory.CreateText("New Text", paper.gameObject, 32, Color.black, Vector2.zero, new Vector2(200, 50));
                            CanvasWorkspaceBuilder.AddManipulationComponents(t);
                            controller.RecordAdd(t);
                        }, "Txt");
                        break;
                }
            };
            
            string[] tools = { "Upload", "Image AI", "Textures", "Templates", "Elements", "Text", "Projects" };
            string[] iconNames = { "Upload", "ImageAI", "Textures", "Templates", "Elements", "Text", "Projects" };
            string[] iconChars = { "\u2191", "\u25C7", "\u25A3", "\u229E", "\u25A6", "T", "\uD83D\uDCC1" };
            for (int i = 0; i < tools.Length; i++) {
                string t = tools[i];
                string resName = i < iconNames.Length ? iconNames[i] : "";
                string iconChar = i < iconChars.Length ? iconChars[i] : "";
                GameObject btnObj = UIFactory.CreateObject("Btn_" + t, leftToolBar);
                var btnLe = btnObj.AddComponent<LayoutElement>();
                btnLe.minHeight = 44; btnLe.minWidth = 0;
                btnObj.AddComponent<Image>().color = new Color(0,0,0,0.01f);
                VerticalLayoutGroup btnVlg = btnObj.AddComponent<VerticalLayoutGroup>();
                btnVlg.spacing = 2; btnVlg.padding = new RectOffset(2, 2, 4, 4); btnVlg.childAlignment = TextAnchor.MiddleCenter; btnVlg.childControlHeight = false; btnVlg.childForceExpandHeight = false;
                Sprite iconSprite = !string.IsNullOrEmpty(resName) ? Resources.Load<Sprite>("Icons/" + resName) : null;
                if (iconSprite != null) {
                    GameObject iconObj = UIFactory.CreateObject("Icon", btnObj);
                    Image iconImg = iconObj.AddComponent<Image>(); iconImg.sprite = iconSprite; iconImg.color = new Color(0.4f, 0.4f, 0.4f);
                    var iconLe = iconObj.AddComponent<LayoutElement>(); iconLe.minWidth = 22; iconLe.minHeight = 22; iconLe.preferredWidth = 22; iconLe.preferredHeight = 22;
                } else if (!string.IsNullOrEmpty(iconChar)) {
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
    }
}

