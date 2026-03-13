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
            dVlg.padding = new RectOffset(10, 10, 16, 16); dVlg.spacing = 10;
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
                titleTxt.SetActive(type != "Upload");
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

                            // Upload button with p_upload.png icon + text in one row
                            GameObject uploadBtn = UIFactory.CreateObject("UploadBtn", uploadWrap);
                            uploadBtn.AddComponent<Image>().color = Color.white;
                            uploadBtn.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.85f, 0.85f, 0.85f);
                            Button ubBtn = uploadBtn.AddComponent<Button>();
                            ubBtn.targetGraphic = uploadBtn.GetComponent<Image>();
                            ubBtn.onClick.AddListener(() => controller.OnUploadCanvasAsset());

                            HorizontalLayoutGroup ubHlg = uploadBtn.AddComponent<HorizontalLayoutGroup>();
                            ubHlg.spacing = 6; ubHlg.childAlignment = TextAnchor.MiddleCenter;
                            ubHlg.padding = new RectOffset(8, 8, 4, 4);
                            ubHlg.childControlWidth = true; ubHlg.childControlHeight = false;
                            ubHlg.childForceExpandWidth = false;
                            LayoutElement ubLe = uploadBtn.AddComponent<LayoutElement>();
                            ubLe.minHeight = 36; ubLe.flexibleHeight = 0;

                            Sprite uploadIconSpr = Resources.Load<Sprite>("EditIcons/p_upload");
                            if (uploadIconSpr != null)
                            {
                                GameObject uploadIconObj = UIFactory.CreateObject("UploadIcon", uploadBtn);
                                uploadIconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
                                Image uiImg = uploadIconObj.AddComponent<Image>();
                                uiImg.sprite = uploadIconSpr; uiImg.preserveAspect = true; uiImg.color = Color.black;
                                LayoutElement iconLE = uploadIconObj.AddComponent<LayoutElement>();
                                iconLE.minWidth = 16; iconLE.preferredWidth = 16; iconLE.minHeight = 16;
                            }

                            GameObject uploadLabel = UIFactory.CreateText(supported, uploadBtn, 11, new Color(0.4f, 0.4f, 0.4f), Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft);
                            LayoutElement lblLE = uploadLabel.AddComponent<LayoutElement>();
                            lblLE.flexibleWidth = 1; lblLE.minHeight = 20;

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
                            GridLayoutGroup glg = listContent.AddComponent<GridLayoutGroup>();
                            glg.cellSize = new Vector2(100, 100);
                            glg.spacing = new Vector2(6, 6);
                            glg.padding = new RectOffset(4, 4, 4, 4);
                            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                            glg.constraintCount = 3;
                            glg.childAlignment = TextAnchor.UpperLeft;
                            listContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                            sr.viewport = vp.GetComponent<RectTransform>();
                            sr.content = lcRt;
                            controller.uploadListContainer = listContent;

                            // Selection action bar (bottom, hidden by default)
                            GameObject selBar = UIFactory.CreateObject("SelectionBar", uploadWrap);
                            selBar.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f, 1f);
                            LayoutElement selBarLe = selBar.AddComponent<LayoutElement>();
                            selBarLe.minHeight = 40; selBarLe.flexibleHeight = 0;
                            HorizontalLayoutGroup selHlg = selBar.AddComponent<HorizontalLayoutGroup>();
                            selHlg.spacing = 8; selHlg.padding = new RectOffset(10, 10, 5, 5);
                            selHlg.childAlignment = TextAnchor.MiddleLeft;
                            selHlg.childControlWidth = false; selHlg.childControlHeight = false;
                            selHlg.childForceExpandWidth = false;

                            // Checkbox icon in bar (click to toggle select all / deselect all)
                            Sprite barCheckSpr = Resources.Load<Sprite>("EditIcons/p_check");
                            GameObject barChkBtn = UIFactory.CreateObject("BarCheckBtn", selBar);
                            barChkBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(22, 22);
                            Image barChkImg = barChkBtn.AddComponent<Image>();
                            if (barCheckSpr != null) { barChkImg.sprite = barCheckSpr; barChkImg.preserveAspect = true; }
                            barChkImg.color = new Color(0.65f, 0.65f, 0.65f);
                            Button barChkBtnComp = barChkBtn.AddComponent<Button>();
                            barChkBtnComp.targetGraphic = barChkImg;
                            barChkBtnComp.onClick.AddListener(() => controller.SelectAllUploads());
                            controller.uploadBarCheckImage = barChkImg;

                            // Selected count text
                            GameObject selCountObj = UIFactory.CreateText("(0) Selected", selBar, 12, Color.black, Vector2.zero, new Vector2(90, 28), TextAnchor.MiddleLeft);
                            controller.uploadSelectionCountText = selCountObj.GetComponent<Text>();

                            // Flexible spacer
                            GameObject spacer = UIFactory.CreateObject("Spacer", selBar);
                            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 1);
                            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

                            // Delete button (p_delete.png icon, tooltip on hover)
                            Sprite delIconSpr = Resources.Load<Sprite>("EditIcons/p_delete");
                            GameObject delBtn = UIFactory.CreateObject("DeleteBtn", selBar);
                            delBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
                            Image delBtnImg = delBtn.AddComponent<Image>();
                            if (delIconSpr != null) { delBtnImg.sprite = delIconSpr; delBtnImg.preserveAspect = true; delBtnImg.color = Color.white; }
                            else { delBtnImg.color = new Color(0.85f, 0.2f, 0.2f); }
                            Button delBtnComp = delBtn.AddComponent<Button>();
                            delBtnComp.targetGraphic = delBtnImg;
                            delBtnComp.onClick.AddListener(() => controller.DeleteSelectedUploads());
                            delBtn.AddComponent<UITooltip>().text = "Delete";

                            // Spacer between Delete and Cancel
                            GameObject btnSpacer = UIFactory.CreateObject("BtnSpacer", selBar);
                            btnSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(8, 1);

                            // Cancel button (p_cancle.png icon only, tooltip on hover)
                            Sprite cancelIconSpr = Resources.Load<Sprite>("EditIcons/p_cancle");
                            GameObject cancelSelBtn = UIFactory.CreateObject("CancelBtn", selBar);
                            cancelSelBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
                            Image cancelBtnImg = cancelSelBtn.AddComponent<Image>();
                            if (cancelIconSpr != null) { cancelBtnImg.sprite = cancelIconSpr; cancelBtnImg.preserveAspect = true; cancelBtnImg.color = new Color(0.4f, 0.4f, 0.4f); }
                            else { cancelBtnImg.color = new Color(0.92f, 0.92f, 0.92f); }
                            Button cancelBtnComp = cancelSelBtn.AddComponent<Button>();
                            cancelBtnComp.targetGraphic = cancelBtnImg;
                            cancelBtnComp.onClick.AddListener(() => controller.CancelUploadSelection());
                            cancelSelBtn.AddComponent<UITooltip>().text = "Cancel";

                            controller.uploadSelectionBar = selBar;
                            selBar.SetActive(false);

                            controller.LoadUploadedImages();
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
                Sprite iconSprite = null;
                if (t == "Upload")
                    iconSprite = Resources.Load<Sprite>("EditIcons/p_upload image");
                else if (!string.IsNullOrEmpty(resName))
                    iconSprite = Resources.Load<Sprite>("Icons/" + resName);
                if (iconSprite != null) {
                    float iconSize = (t == "Upload") ? 36f : 22f;
                    GameObject iconObj = UIFactory.CreateObject("Icon", btnObj);
                    iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
                    Image iconImg = iconObj.AddComponent<Image>(); iconImg.sprite = iconSprite; iconImg.color = new Color(0.4f, 0.4f, 0.4f); iconImg.preserveAspect = true;
                    var iconLe = iconObj.AddComponent<LayoutElement>(); iconLe.minWidth = iconSize; iconLe.minHeight = iconSize; iconLe.preferredWidth = iconSize; iconLe.preferredHeight = iconSize;
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
            controller.leftDrawer = drawer;
        }
    }
}

