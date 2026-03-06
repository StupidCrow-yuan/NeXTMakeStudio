using UnityEngine;
using UnityEngine.UI;
using PocoRender.UI.Core;
using PocoRender.Core;
using PocoRender.Communication;
using System.Collections.Generic;
using PocoRender.UI; // For PocoRenderStudioUIManager, UVPrintStudioLayout
using PocoRender.UI.TextureEffects;
using PocoRender.Utils;

namespace PocoRender.UI.Modules
{
    public class HomeModule
    {
        // NOTE: Preview background should come from the 3D grid floor (same scene), not a separate UI grid.
        private GameObject filters; // Shared between ProjectsView and DetailView toggling
        private GameObject detailView; // Reference to detail view
        private CanvasController activeController; // Active editor controller
        private GameObject currentActiveCanvas; // Current active canvas view
        private GameObject previewView; 
        private GameObject subToolbar;
        private GameObject previewLayersPanel; // Left panel in preview
        private GameObject previewLayersList; // Content for preview layers

        public void CreateUVPrintLayout(GameObject parent, PocoRenderStudioUIManager manager, System.Action<Color?> addCanvasCallback)
        {
            GameObject layoutObj = UIFactory.CreateObject("UVPrintStudioLayout", parent);
            UIFactory.Stretch(layoutObj.GetComponent<RectTransform>());
            layoutObj.AddComponent<Image>().color = UIFactory.COLOR_UV_BG;

            UVPrintStudioLayout layout = layoutObj.AddComponent<UVPrintStudioLayout>();
            layout.mainContainer = layoutObj.GetComponent<RectTransform>();

            // Assign to manager immediately so ShowLayoutForMode works even if
            // later sections throw an exception during UI construction.
            manager.uvPrintLayout = layout;

            // Global Tab Bar (top of window — no separate menu row needed, Qt handles View/Settings/Help/Account)
            GameObject tabBar = UIFactory.CreateObject("GlobalTabBar", layoutObj);
            RectTransform tbRect = tabBar.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0, 1); tbRect.anchorMax = new Vector2(1, 1);
            tbRect.pivot = new Vector2(0.5f, 1); tbRect.sizeDelta = new Vector2(0, 50); tbRect.anchoredPosition = Vector2.zero;
            tabBar.AddComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f);

            // Sub-Toolbar (Secondary Bar)
            subToolbar = UIFactory.CreateObject("SubToolbar", layoutObj);
            RectTransform subRect = subToolbar.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 1); subRect.anchorMax = new Vector2(1, 1);
            subRect.pivot = new Vector2(0.5f, 1); subRect.sizeDelta = new Vector2(0, 50); subRect.anchoredPosition = new Vector2(0, -50);
            subToolbar.AddComponent<Image>().color = Color.white;
            subToolbar.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            subToolbar.SetActive(false); // Only visible in editor

            // Left side: Aligned to Left Panel (Width: 350px)
            GameObject subLeft = UIFactory.CreateObject("SubLeft", subToolbar);
            RectTransform slRect = subLeft.GetComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0, 0); slRect.anchorMax = new Vector2(0, 1); 
            slRect.sizeDelta = new Vector2(350, 0); slRect.anchoredPosition = Vector2.zero; slRect.pivot = new Vector2(0, 0.5f);
            HorizontalLayoutGroup slhlg = subLeft.AddComponent<HorizontalLayoutGroup>();
            slhlg.spacing = 15; slhlg.padding = new RectOffset(20, 20, 0, 0); slhlg.childAlignment = TextAnchor.MiddleLeft; slhlg.childControlWidth = false;

            UIFactory.CreateText("Untitled Design", subLeft, 14, Color.black, Vector2.zero, new Vector2(150, 30), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.CreateButton("↶ Undo", subLeft, Vector2.zero, new Vector2(70, 30), new Color(0.95f, 0.95f, 0.95f), Color.black).GetComponent<Button>().onClick.AddListener(() => activeController?.Undo());
            UIFactory.CreateButton("↷ Redo", subLeft, Vector2.zero, new Vector2(70, 30), new Color(0.95f, 0.95f, 0.95f), Color.black).GetComponent<Button>().onClick.AddListener(() => activeController?.Redo());

            // Middle side: Saved status (Center Area)
            GameObject subMid = UIFactory.CreateObject("SubMid", subToolbar);
            RectTransform smRect = subMid.GetComponent<RectTransform>();
            smRect.anchorMin = Vector2.zero; smRect.anchorMax = Vector2.one;
            smRect.offsetMin = new Vector2(350, 0); smRect.offsetMax = new Vector2(-300, 0);
            HorizontalLayoutGroup smhlg = subMid.AddComponent<HorizontalLayoutGroup>();
            smhlg.spacing = 10; smhlg.childAlignment = TextAnchor.MiddleCenter;
            UIFactory.CreateText("☁ Saved 10:45 AM", subMid, 12, Color.gray, Vector2.zero, Vector2.zero);

            // Right side: Aligned to Right Panel (Width: 300px)
            GameObject subRight = UIFactory.CreateObject("SubRight", subToolbar);
            RectTransform srRect = subRight.GetComponent<RectTransform>();
            srRect.anchorMin = new Vector2(1, 0); srRect.anchorMax = new Vector2(1, 1); 
            srRect.sizeDelta = new Vector2(300, 0); srRect.anchoredPosition = Vector2.zero; srRect.pivot = new Vector2(1, 0.5f);
            HorizontalLayoutGroup srhlg = subRight.AddComponent<HorizontalLayoutGroup>();
            srhlg.spacing = 15; srhlg.padding = new RectOffset(20, 20, 0, 0); srhlg.childAlignment = TextAnchor.MiddleRight; srhlg.childControlWidth = false;

            UIFactory.CreateButton("Download ↓", subRight, Vector2.zero, new Vector2(100, 32), new Color(0.95f, 0.95f, 0.95f), Color.black);
            UIFactory.CreateButton("Publish", subRight, Vector2.zero, new Vector2(80, 32), Color.black, Color.white);

            GameObject tabsContainer = UIFactory.CreateObject("TabsContainer", tabBar);
            HorizontalLayoutGroup tclg = tabsContainer.AddComponent<HorizontalLayoutGroup>();
            tclg.spacing = 5; tclg.padding = new RectOffset(20, 0, 0, 0); tclg.childAlignment = TextAnchor.MiddleLeft;
            tclg.childControlWidth = true; tclg.childControlHeight = true; tclg.childForceExpandWidth = false; tclg.childForceExpandHeight = false;
            RectTransform tcRect = tabsContainer.GetComponent<RectTransform>();
            tcRect.anchorMin = Vector2.zero; tcRect.anchorMax = new Vector2(0.6f, 1);
            tcRect.offsetMin = Vector2.zero; tcRect.offsetMax = Vector2.zero;

            GameObject centerInfo = UIFactory.CreateObject("CenterInfo", tabBar);
            RectTransform ciRect = centerInfo.GetComponent<RectTransform>();
            ciRect.anchorMin = new Vector2(0.6f, 0); ciRect.anchorMax = new Vector2(0.8f, 1);
            ciRect.offsetMin = Vector2.zero; ciRect.offsetMax = Vector2.zero;
            centerInfo.SetActive(false);

            GameObject rightHome = UIFactory.CreateObject("RightHome", tabBar); 
            HorizontalLayoutGroup rhlg = rightHome.AddComponent<HorizontalLayoutGroup>();
            rhlg.spacing = 15; rhlg.padding = new RectOffset(0, 20, 0, 0); rhlg.childAlignment = TextAnchor.MiddleRight;
            rhlg.childControlWidth = true; rhlg.childControlHeight = true; rhlg.childForceExpandWidth = false;
            RectTransform rhRect = rightHome.GetComponent<RectTransform>();
            rhRect.anchorMin = new Vector2(1, 0); rhRect.anchorMax = new Vector2(1, 1); rhRect.pivot = new Vector2(1, 0.5f);
            rhRect.sizeDelta = new Vector2(400, 0); rhRect.anchoredPosition = Vector2.zero;

            UIFactory.CreateTextButton("REFRESH", rightHome, 12, UIFactory.COLOR_TEXT_DARK);
            GameObject switcher = UIFactory.CreateObject("Switcher", rightHome);
            switcher.AddComponent<LayoutElement>().minWidth = 160; switcher.GetComponent<LayoutElement>().minHeight = 36;
            switcher.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
            switcher.AddComponent<Button>().onClick.AddListener(manager.ShowSelectionDialog);
            UIFactory.CreateText("UV Print Studio v", switcher, 14, UIFactory.COLOR_TEXT_DARK, Vector2.zero, Vector2.zero);
            UIFactory.CreateTextButton("NOTIF", rightHome, 12, UIFactory.COLOR_TEXT_DARK); UIFactory.CreateTextButton("HELP", rightHome, 12, UIFactory.COLOR_TEXT_DARK);

            GameObject rightCanvas = UIFactory.CreateObject("RightCanvas", tabBar); 
            HorizontalLayoutGroup rclg = rightCanvas.AddComponent<HorizontalLayoutGroup>();
            rclg.spacing = 15; rclg.padding = new RectOffset(0, 20, 0, 0); rclg.childAlignment = TextAnchor.MiddleRight;
            rclg.childControlWidth = true; rclg.childControlHeight = true; rclg.childForceExpandWidth = false;
            RectTransform rcRect = rightCanvas.GetComponent<RectTransform>();
            rcRect.anchorMin = new Vector2(1, 0); rcRect.anchorMax = new Vector2(1, 1); rcRect.pivot = new Vector2(1, 0.5f);
            rcRect.sizeDelta = new Vector2(400, 0); rcRect.anchoredPosition = Vector2.zero;
            
            // Mirroring the home page right-side elements
            UIFactory.CreateTextButton("REFRESH", rightCanvas, 12, UIFactory.COLOR_TEXT_DARK);
            GameObject switcherEditor = UIFactory.CreateObject("SwitcherEditor", rightCanvas);
            switcherEditor.AddComponent<LayoutElement>().minWidth = 160; switcherEditor.GetComponent<LayoutElement>().minHeight = 36;
            switcherEditor.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
            UIFactory.CreateText("UV Print Studio v", switcherEditor, 14, UIFactory.COLOR_TEXT_DARK, Vector2.zero, Vector2.zero);
            
            UIFactory.CreateTextButton("NOTIF", rightCanvas, 12, UIFactory.COLOR_TEXT_DARK); 
            UIFactory.CreateTextButton("HELP", rightCanvas, 12, UIFactory.COLOR_TEXT_DARK);
            rightCanvas.SetActive(false);

            GameObject viewContainer = UIFactory.CreateObject("ViewContainer", layoutObj);
            RectTransform vcRect = viewContainer.GetComponent<RectTransform>();
            vcRect.anchorMin = Vector2.zero; vcRect.anchorMax = Vector2.one;
            vcRect.offsetMin = Vector2.zero; vcRect.offsetMax = new Vector2(0, -100); 
            
            // --- 3D Preview View (Full Screen) ---
            previewView = UIFactory.CreateObject("PreviewView", viewContainer);
            UIFactory.Stretch(previewView.GetComponent<RectTransform>());
            // Keep a simple dark UI backdrop; the actual grid is rendered in 3D (GridFloor)
            previewView.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f);

            // 3D Rendering Area (RawImage to display the RenderTexture)
            GameObject rawImageObj = UIFactory.CreateObject("3DRenderer", previewView);
            UIFactory.Stretch(rawImageObj.GetComponent<RectTransform>());
            
            RawImage previewRawImage = rawImageObj.AddComponent<RawImage>();
            previewRawImage.raycastTarget = true;
            
            Model3DViewer modelViewer = previewView.AddComponent<Model3DViewer>();
            modelViewer.targetImage = previewRawImage;
            modelViewer.textureHeight = 1080; // High quality height, width will be calculated
            
            Model3DController modelController = previewView.AddComponent<Model3DController>();
            modelController.modelViewer = modelViewer;

            // 3D Scene Container - Find existing to prevent accumulation
            GameObject sceneContainer = GameObject.Find("Preview3DScene");
            if (sceneContainer != null) Object.DestroyImmediate(sceneContainer);
            sceneContainer = new GameObject("Preview3DScene");
            modelViewer.modelContainer = sceneContainer;
            
            try
            {
                Create3DGrid(sceneContainer);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HomeModule] Create3DGrid failed (non-fatal): {ex.Message}");
            }

            GameObject lightObj = new GameObject("PreviewLight");
            lightObj.transform.SetParent(sceneContainer.transform);
            lightObj.transform.rotation = Quaternion.Euler(50, 30, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = Color.white;
            l.intensity = 0.5f;

            // Back Button (Top Left) - White circle with arrow
            GameObject backBtnObj = UIFactory.CreateObject("BackBtn", previewView);
            RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 1); backRt.anchorMax = new Vector2(0, 1);
            backRt.sizeDelta = new Vector2(40, 40); backRt.anchoredPosition = new Vector2(30, -30);
            backBtnObj.AddComponent<Image>().color = Color.white;
            backBtnObj.AddComponent<Outline>().effectColor = Color.gray;
            UIFactory.CreateText("←", backBtnObj, 24, Color.black, Vector2.zero, Vector2.zero);
            backBtnObj.AddComponent<Button>().onClick.AddListener(() => {
                // USER REQ: Clean up preview objects to prevent lag/accumulation
                if (modelViewer != null && modelViewer.modelContainer != null)
                {
                    // Destroy only the DesignStage content, keep scene setup if needed, 
                    // or clear entirely. Here we clear DesignStage specifically.
                    foreach (Transform child in modelViewer.modelContainer.transform)
                    {
                        if (child.name == "DesignStage") Object.DestroyImmediate(child.gameObject);
                    }
                    modelViewer.SetModel(null);
                }

                previewView.SetActive(false);
                subToolbar.SetActive(true);
                if (currentActiveCanvas != null) currentActiveCanvas.SetActive(true);
            });

            // Left Layers Panel
            previewLayersPanel = UIFactory.CreateObject("PreviewLayers", previewView);
            RectTransform plpRt = previewLayersPanel.GetComponent<RectTransform>();
            plpRt.anchorMin = new Vector2(0, 0); plpRt.anchorMax = new Vector2(0, 1);
            plpRt.sizeDelta = new Vector2(240, -150); plpRt.anchoredPosition = new Vector2(10, 0); plpRt.pivot = new Vector2(0, 0.5f);
            previewLayersPanel.AddComponent<Image>().color = new Color(1, 1, 1, 0.95f);
            previewLayersPanel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            
            UIFactory.CreateText("Layers", previewLayersPanel, 14, Color.black, new Vector2(0, 110), new Vector2(0, 30), TextAnchor.MiddleCenter, FontStyle.Bold);
            
            GameObject plpScroll = UIFactory.CreateObject("Scroll", previewLayersPanel);
            UIFactory.Stretch(plpScroll.GetComponent<RectTransform>());
            plpScroll.GetComponent<RectTransform>().offsetMin = new Vector2(0, 10); plpScroll.GetComponent<RectTransform>().offsetMax = new Vector2(0, -40);
            ScrollRect plpSr = plpScroll.AddComponent<ScrollRect>();
            plpSr.horizontal = false; plpSr.vertical = true;
            
            GameObject plpVp = UIFactory.CreateObject("VP", plpScroll);
            UIFactory.Stretch(plpVp.GetComponent<RectTransform>());
            plpVp.AddComponent<RectMask2D>();
            
            previewLayersList = UIFactory.CreateObject("Content", plpVp);
            RectTransform pllRt = previewLayersList.GetComponent<RectTransform>();
            pllRt.anchorMin = new Vector2(0, 1); pllRt.anchorMax = new Vector2(1, 1); pllRt.pivot = new Vector2(0.5f, 1);
            pllRt.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup pllVlg = previewLayersList.AddComponent<VerticalLayoutGroup>();
            pllVlg.padding = new RectOffset(5, 5, 5, 5); pllVlg.spacing = 5;
            pllVlg.childControlHeight = true; pllVlg.childForceExpandWidth = true;
            previewLayersList.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            plpSr.content = pllRt; plpSr.viewport = plpVp.GetComponent<RectTransform>();

            // Layers Button (Bottom Left)
            GameObject toggleLayersBtn = UIFactory.CreateButton("Layers", previewView, new Vector2(0, 0), new Vector2(100, 36), Color.white, Color.black);
            RectTransform tlbRt = toggleLayersBtn.GetComponent<RectTransform>();
            tlbRt.anchorMin = Vector2.zero; tlbRt.anchorMax = Vector2.zero; tlbRt.pivot = Vector2.zero;
            tlbRt.anchoredPosition = new Vector2(20, 20);
            toggleLayersBtn.GetComponent<Button>().onClick.AddListener(() => previewLayersPanel.SetActive(!previewLayersPanel.activeSelf));

            previewView.SetActive(false);

            // Home View & Dynamic Canvas Logic
            GameObject homeView = UIFactory.CreateObject("HomeView", viewContainer);
            UIFactory.Stretch(homeView.GetComponent<RectTransform>());
            
            System.Action ResetTabs = () => {
                foreach(Transform t in tabsContainer.transform) {
                    var img = t.GetComponent<Image>();
                    if(img) img.color = Color.clear; 
                }
            };

            GameObject homeTab = UIFactory.CreateTextButton("⌂ HOME", tabsContainer, 14, UIFactory.COLOR_TEXT_DARK);
            LayoutElement hle = homeTab.GetComponent<LayoutElement>(); hle.minWidth = 80; hle.minHeight = 36;
            Button homeBtn = homeTab.GetComponent<Button>();
            
            System.Action SwitchToHome = () => {
                ResetTabs(); homeTab.GetComponent<Image>().color = Color.white; 
                homeView.SetActive(true); centerInfo.SetActive(false);
                rightHome.SetActive(true); rightCanvas.SetActive(false);
                subToolbar.SetActive(false); // Hide sub toolbar in home
                vcRect.offsetMax = new Vector2(0, -50); // Restore height
                activeController = null; // No active controller in home
                currentActiveCanvas = null;
                foreach(Transform t in viewContainer.transform) if (t.name.StartsWith("CanvasView")) t.gameObject.SetActive(false);
            };
            homeBtn.onClick.AddListener(() => SwitchToHome());

            int canvasCount = 0;
            
            // Define AddCanvas Action
            System.Action<Color?> AddNewCanvas = (importColor) => {
                canvasCount++;
                string canvasName = $"Canvas {canvasCount}";
                
                // Switch context UI
                subToolbar.SetActive(true); // Show sub toolbar in editor
                vcRect.offsetMax = new Vector2(0, -100); // Shrink view to fit subtoolbar
                
                // 1. Create View
                GameObject cv = UIFactory.CreateObject($"CanvasView_{canvasCount}", viewContainer);
                UIFactory.Stretch(cv.GetComponent<RectTransform>());
                CanvasModule.CreateCanvasEditor(cv);
                activeController = cv.GetComponentInChildren<CanvasController>(); // Set active controller
                currentActiveCanvas = cv;
                
                if (activeController != null) {
                    activeController.OnPreviewRequested = () => {
                        cv.SetActive(false);
                        subToolbar.SetActive(false);
                        previewView.SetActive(true);
                        
                        // 1. Sync Layers to Preview Panel
                        SyncPreviewLayers(cv);
                        
                        // 2. Setup 3D Design and Controller
                        Setup3DDesign(cv, modelViewer);
                        modelController.modelObject = modelViewer.modelContainer.transform.Find("DesignStage")?.gameObject;
                        modelController.ResetTransform();
                    };
                    activeController.OnPrintRequested = () => {
                        if (BuildMode.HasPrintService)
                        {
                            SendCanvasToPrint(activeController, canvasName);
                        }
                        else
                        {
                            Debug.Log("Print initiated for " + canvasName +
                                      " (no print service — standalone mode)");
                        }
                    };
                }
                
                if (importColor.HasValue)
                {
                    Transform paper = cv.transform.Find("EditorArea/Workspace/Paper");
                    if (paper != null)
                    {
                        GameObject addedImg = UIFactory.CreateObject("ImportedDesign", paper.gameObject);
                        RectTransform air = addedImg.GetComponent<RectTransform>();
                        air.sizeDelta = new Vector2(300, 300); 
                        air.anchoredPosition = Vector2.zero;
                        
                        // Try to load UV texture
                        if (importColor.Value == new Color(0.8f, 0.4f, 0.2f)) {
                            Texture2D uvTex = Resources.Load<Texture2D>("UVImages/img1/uv") ?? Resources.Load<Texture2D>("UVImages/img1/uv.jpg");
                            if (uvTex != null) {
                                Image imgComp = addedImg.AddComponent<Image>();
                                imgComp.sprite = Sprite.Create(uvTex, new Rect(0, 0, uvTex.width, uvTex.height), new Vector2(0.5f, 0.5f));
                                addedImg.AddComponent<RectTransform>();
                                addedImg.AddComponent<CanvasRenderer>();
                                addedImg.AddComponent<CanvasGroup>();
                                addedImg.AddComponent<BoxCollider2D>();
                                addedImg.AddComponent<ObjectManipulator>();
                                activeController.RecordAdd(addedImg); // Record imported design
                            } else {
                                addedImg.AddComponent<Image>().color = importColor.Value;
                                addedImg.AddComponent<ObjectManipulator>();
                                activeController.RecordAdd(addedImg); // Record imported design
                            }
                        } else {
                            addedImg.AddComponent<Image>().color = importColor.Value;
                            addedImg.AddComponent<ObjectManipulator>();
                            activeController.RecordAdd(addedImg); // Record imported design
                        }
                    }
                }

                // 2. Create Tab
                GameObject newTab = UIFactory.CreateTextButton($"◳ {canvasName}", tabsContainer, 12, UIFactory.COLOR_TEXT_DARK);
                newTab.transform.SetSiblingIndex(tabsContainer.transform.childCount - 2); 
                LayoutElement le = newTab.GetComponent<LayoutElement>(); le.minWidth = 120; le.minHeight = 36;
                
                GameObject closeBtn = UIFactory.CreateText("x", newTab, 10, Color.gray, new Vector2(40, 0), new Vector2(20, 20));
                closeBtn.GetComponent<Text>().raycastTarget = true; 
                Button cBtn = closeBtn.AddComponent<Button>();
                cBtn.onClick.AddListener(() => {
                    Object.Destroy(newTab);
                    Object.Destroy(cv);
                    if(cv.activeSelf) SwitchToHome();
                });

                Button tabBtn = newTab.GetComponent<Button>();
                tabBtn.onClick.AddListener(() => {
                    ResetTabs(); newTab.GetComponent<Image>().color = Color.white;
                    homeView.SetActive(false); centerInfo.SetActive(true);
                    rightHome.SetActive(false); rightCanvas.SetActive(true);
                    subToolbar.SetActive(true); // Show sub toolbar
                    vcRect.offsetMax = new Vector2(0, -100); 
                    foreach(Transform t in viewContainer.transform) if (t.name.StartsWith("CanvasView")) t.gameObject.SetActive(false);
                    cv.SetActive(true);
                    activeController = cv.GetComponentInChildren<CanvasController>(); // Switch active controller
                    currentActiveCanvas = cv;
                });
                tabBtn.onClick.Invoke();
            };
            
            // Delegate AddNewCanvas to HomeViewContent if needed, or pass the wrapped action
            // But wait, CreateProjectsView is called inside CreateHomeViewContent.
            // And CreateProjectsView needs the addCanvasCallback which we passed in.
            // But the internal AddNewCanvas logic is what we want to use. 
            // So we should wrap it.
            
            // Wait, the "New Design" button in HomeViewContent calls AddNewCanvas(null).
            // The "Customize" button in DetailView calls AddNewCanvas(color).
            // So we need to pass AddNewCanvas to CreateHomeViewContent.

            CreateHomeViewContent(homeView, AddNewCanvas);

            GameObject plusBtn = UIFactory.CreateTextButton("+", tabsContainer, 20, UIFactory.COLOR_TEXT_DARK);
            LayoutElement ple = plusBtn.GetComponent<LayoutElement>(); ple.minWidth = 40; ple.minHeight = 36;
            plusBtn.GetComponent<Button>().onClick.AddListener(() => AddNewCanvas(null));

            // In embedded mode Qt hosts the Home page; Unity should show only the Canvas editor.
            // Create one canvas and switch to it so that when Qt sends new_project/open_project we are already on the editor.
            if (BuildMode.IsEmbeddedMode)
            {
                AddNewCanvas(null);
                homeTab.SetActive(false);
            }
            else
            {
                SwitchToHome();
            }
            layout.Hide();

            // If external callback was provided (e.g. from tests), we can hook it, but here we define the logic.
        }

        private void CreateHomeViewContent(GameObject parent, System.Action<Color?> addCanvasCallback)
        {
            // NavBar
            GameObject navBar = UIFactory.CreateObject("NavBar", parent);
            RectTransform navRect = navBar.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0, 1); navRect.anchorMax = new Vector2(1, 1);
            navRect.pivot = new Vector2(0.5f, 1); navRect.sizeDelta = new Vector2(0, 60); navRect.anchoredPosition = new Vector2(0, 0);
            navBar.AddComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f);

            GameObject logoObj = UIFactory.CreateText("Make It Real (Beta)", navBar, 20, UIFactory.COLOR_TEXT_DARK, Vector2.zero, new Vector2(10, 40), TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform logoRect = logoObj.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0, 0.5f); logoRect.anchorMax = new Vector2(0.2f, 0.5f); logoRect.anchoredPosition = new Vector2(20, 0);

            GameObject links = UIFactory.CreateObject("Links", navBar);
            HorizontalLayoutGroup linkGroup = links.AddComponent<HorizontalLayoutGroup>();
            linkGroup.spacing = 30; linkGroup.childAlignment = TextAnchor.MiddleCenter;
            RectTransform linkRect = links.GetComponent<RectTransform>();
            linkRect.anchorMin = new Vector2(0.2f, 0); linkRect.anchorMax = new Vector2(0.7f, 1); 
            linkRect.offsetMin = Vector2.zero; linkRect.offsetMax = Vector2.zero;

            GameObject navRight = UIFactory.CreateObject("NavRight", navBar);
            HorizontalLayoutGroup nrlg = navRight.AddComponent<HorizontalLayoutGroup>();
            nrlg.spacing = 15; nrlg.childAlignment = TextAnchor.MiddleRight; nrlg.padding = new RectOffset(0, 30, 0, 0);
            RectTransform nrRect = navRight.GetComponent<RectTransform>();
            nrRect.anchorMin = new Vector2(0.7f, 0); nrRect.anchorMax = new Vector2(1, 1); 
            nrRect.offsetMin = Vector2.zero; nrRect.offsetMax = Vector2.zero;
            UIFactory.CreateTextButton("SEARCH", navRight, 12, UIFactory.COLOR_TEXT_DARK);
            UIFactory.CreateButton("Publish", navRight, Vector2.zero, new Vector2(80, 32), Color.white, UIFactory.COLOR_TEXT_DARK).AddComponent<Outline>().effectColor = Color.black;
            
            Button newDesignBtn = UIFactory.CreateButton("New Design ▼", navRight, Vector2.zero, new Vector2(120, 32), Color.black, Color.white).GetComponent<Button>();
            newDesignBtn.onClick.AddListener(() => addCanvasCallback(null));

            GameObject avatar = UIFactory.CreateObject("Avatar", navRight);
            avatar.AddComponent<LayoutElement>().minWidth = 32; avatar.GetComponent<LayoutElement>().minHeight = 32;
            avatar.AddComponent<Image>().color = new Color(1f, 0.4f, 0f);
            UIFactory.CreateText("?", avatar, 16, Color.white, Vector2.zero, Vector2.zero);

            // Content Container
            GameObject contentArea = UIFactory.CreateObject("HomeContentArea", parent);
            RectTransform caRect = contentArea.GetComponent<RectTransform>();
            caRect.anchorMin = Vector2.zero; caRect.anchorMax = Vector2.one;
            caRect.offsetMin = Vector2.zero; caRect.offsetMax = new Vector2(0, -60);

            GameObject projectsView = UIFactory.CreateObject("ProjectsView", contentArea); UIFactory.Stretch(projectsView.GetComponent<RectTransform>());
            CreateProjectsView(projectsView, addCanvasCallback);
            
            GameObject creativeLabView = UIFactory.CreateObject("CreativeLabView", contentArea); UIFactory.Stretch(creativeLabView.GetComponent<RectTransform>());
            CreateCreativeLabView(creativeLabView);
            creativeLabView.SetActive(false);
            
            System.Action<string> OnNavClick = (name) => {
                projectsView.SetActive(name == "Projects");
                creativeLabView.SetActive(name == "Creative Lab");
            };

            string[] navItems = { "Projects", "Creative Lab", "Academy", "Campaigns", "Subscription" };
            foreach (var item in navItems) 
            {
                GameObject btn = UIFactory.CreateTextButton(item, links, 14, UIFactory.COLOR_NAV_TEXT);
                btn.GetComponent<Button>().onClick.AddListener(() => OnNavClick(item));
            }
        }

        private void CreateProjectsView(GameObject parent, System.Action<Color?> addCanvasCallback)
        {
            // Left Sidebar
            GameObject sidebar = UIFactory.CreateObject("LeftSidebar", parent);
            RectTransform sr = sidebar.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(0, 1);
            sr.sizeDelta = new Vector2(220, 0); sr.pivot = new Vector2(0, 0.5f);
            sidebar.AddComponent<Image>().color = Color.white;

            VerticalLayoutGroup svlg = sidebar.AddComponent<VerticalLayoutGroup>();
            svlg.padding = new RectOffset(20, 10, 20, 0); svlg.spacing = 10;
            
            UIFactory.CreateText("Content Type", sidebar, 14, Color.gray, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft);
            UIFactory.CreateTextButton("⊞ Projects", sidebar, 14, UIFactory.COLOR_TEXT_DARK).GetComponent<LayoutElement>().minHeight = 30;
            UIFactory.CreateTextButton("✎ Designs", sidebar, 14, Color.gray).GetComponent<LayoutElement>().minHeight = 30;
            
            UIFactory.CreateText("Category", sidebar, 14, Color.gray, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft);
            string[] cats = {"All", "Gifts", "Blended Crafts", "Home & Living", "Art Decor", "Digital Accessories"};
            foreach(var c in cats) UIFactory.CreateTextButton(c, sidebar, 14, c == "All" ? UIFactory.COLOR_ACCENT_GREEN : UIFactory.COLOR_TEXT_DARK).GetComponent<LayoutElement>().minHeight = 30;

            // Main Content
            GameObject main = UIFactory.CreateObject("MainGrid", parent);
            RectTransform mr = main.GetComponent<RectTransform>();
            mr.anchorMin = Vector2.zero; mr.anchorMax = Vector2.one;
            mr.offsetMin = new Vector2(240, 0); mr.offsetMax = Vector2.zero; 
            
            main.transform.SetAsLastSibling(); 
            
            // Filters
            filters = UIFactory.CreateObject("Filters", main);
            RectTransform fr = filters.GetComponent<RectTransform>();
            fr.anchorMin = new Vector2(0, 1); fr.anchorMax = new Vector2(1, 1);
            fr.sizeDelta = new Vector2(0, 50); fr.anchoredPosition = new Vector2(0, 0);
            HorizontalLayoutGroup fhlg = filters.AddComponent<HorizontalLayoutGroup>();
            fhlg.padding = new RectOffset(20, 20, 10, 10); fhlg.spacing = 15; fhlg.childAlignment = TextAnchor.MiddleLeft;
            string[] fs = {"Themes ▼", "Print Mode ▼", "Materials ▼", "Trending ▼"};
            foreach(var f in fs) UIFactory.CreateButton(f, filters, Vector2.zero, new Vector2(100, 30), Color.white, UIFactory.COLOR_TEXT_DARK).AddComponent<Outline>().effectColor = new Color(0.9f,0.9f,0.9f);

            // Scroll
            GameObject scroll = UIFactory.CreateObject("Scroll", main);
            RectTransform scr = scroll.GetComponent<RectTransform>();
            scr.anchorMin = Vector2.zero; scr.anchorMax = Vector2.one;
            scr.offsetMax = new Vector2(0, -50);
            
            ScrollRect srect = scroll.AddComponent<ScrollRect>();
            srect.movementType = ScrollRect.MovementType.Clamped; 
            srect.horizontal = false; 
            srect.vertical = true;
            GameObject vp = UIFactory.CreateObject("Viewport", scroll); UIFactory.Stretch(vp.GetComponent<RectTransform>()); vp.AddComponent<Image>().color = new Color(1,1,1,0.01f); vp.AddComponent<RectMask2D>();
            GameObject content = UIFactory.CreateObject("Content", vp);
            RectTransform cr = content.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(0.5f, 1);
            
            GridLayoutGroup glg = content.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(280, 340); glg.spacing = new Vector2(20, 20); 
            glg.padding = new RectOffset(100, 20, 20, 20);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            float screenWidth = Screen.width;
            int columns = screenWidth > 1920 ? 5 : 4;
            glg.constraintCount = columns;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            srect.viewport = vp.GetComponent<RectTransform>(); srect.content = cr;

            // Project Detail View
            detailView = UIFactory.CreateObject("DetailView", parent);
            UIFactory.Stretch(detailView.GetComponent<RectTransform>());
            detailView.SetActive(false);
            
            Dictionary<int, ProjectData> projectData = new Dictionary<int, ProjectData>();
            
            DetailViewModule.CreateProjectDetailView(detailView, () => detailView.SetActive(false), addCanvasCallback, projectData);

            for(int i=0; i<12; i++) {
                GameObject card = UIFactory.CreateObject($"Proj_{i}", content);
                card.AddComponent<Image>().color = Color.white;
                GameObject img = UIFactory.CreateObject("Img", card);
                RectTransform ir = img.GetComponent<RectTransform>();
                ir.anchorMin = new Vector2(0, 0.3f); ir.anchorMax = Vector2.one; ir.offsetMin=new Vector2(10,0); ir.offsetMax=new Vector2(-10,-10);
                
                Image imgComp = img.AddComponent<Image>();
                ProjectData data = new ProjectData();
                
                if (i == 0) {
                    Texture2D bgTex = Resources.Load<Texture2D>("UVImages/img1/bg");
                    Texture2D uvTex = Resources.Load<Texture2D>("UVImages/img1/uv") ?? Resources.Load<Texture2D>("UVImages/img1/uv.jpg");
                    Texture2D combinedTex = Resources.Load<Texture2D>("UVImages/img1/combined");
                    
                    if (bgTex != null) {
                        imgComp.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
                        data.bgTexture = bgTex;
                    } else {
                        imgComp.color = new Color(0.3f, 0.5f, 0.8f);
                    }
                    
                    data.bgTexture = bgTex;
                    data.uvTexture = uvTex;
                    data.combinedTexture = combinedTex != null ? combinedTex : bgTex;
                } else {
                    Color cardColor = Color.HSVToRGB((float)i/12f, 0.6f, 0.8f);
                    imgComp.color = cardColor;
                    data.fallbackColor = cardColor;
                }
                
                projectData[i] = data;
                
                UIFactory.CreateText("Project Title", card, 14, UIFactory.COLOR_TEXT_DARK, new Vector2(20, -110), new Vector2(150, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
                Button btn = card.AddComponent<Button>();
                int cardIndex = i; 
                btn.onClick.AddListener(() => {
                    filters.SetActive(false);
                    detailView.GetComponent<ProjectDetailViewUpdater>()?.UpdateWithProjectData(projectData[cardIndex]);
                    detailView.SetActive(true);
                });
            }
        }

        private void CreateCreativeLabView(GameObject parent)
        {
            GameObject title = UIFactory.CreateText("Texture AI", parent, 24, UIFactory.COLOR_TEXT_DARK, new Vector2(40, -40), new Vector2(200, 40), TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform tr = title.GetComponent<RectTransform>(); tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(0, 1);

            GameObject grid = UIFactory.CreateObject("Grid", parent);
            RectTransform gr = grid.GetComponent<RectTransform>();
            gr.anchorMin = Vector2.zero; gr.anchorMax = Vector2.one; gr.offsetMax = new Vector2(0, -80);
            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(300, 200); glg.spacing = new Vector2(30, 30); glg.padding = new RectOffset(40, 40, 40, 40);

            string[] items = {"Relief Pet Magnet", "Relief Portrait Magnet", "Relief Architecture", "Relief Still Life"};
            foreach(var it in items) {
                GameObject card = UIFactory.CreateObject(it, grid);
                card.AddComponent<Image>().color = Color.white;
                UIFactory.CreateText(it, card, 16, UIFactory.COLOR_TEXT_DARK, new Vector2(0, -60), new Vector2(200, 30));
                UIFactory.CreateButton("✦ try it", card, new Vector2(80, -60), new Vector2(80, 30), Color.white, UIFactory.COLOR_TEXT_DARK).AddComponent<Outline>().effectColor = Color.gray;
            }
        }

        private void Create3DGrid(GameObject parent)
        {
            GameObject gridFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gridFloor.name = "GridFloor";
            gridFloor.transform.SetParent(parent.transform);
            gridFloor.transform.localScale = new Vector3(50, 1, 50);
            gridFloor.transform.localPosition = Vector3.zero;

            Texture2D gridTex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++) {
                for (int x = 0; x < 64; x++) {
                    bool isGridLine = (x == 0 || y == 0);
                    gridTex.SetPixel(x, y, isGridLine ? new Color(0.35f, 0.35f, 0.38f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f));
                }
            }
            gridTex.wrapMode = TextureWrapMode.Repeat;
            gridTex.Apply();

            // Shader.Find("Standard") returns null in builds if not referenced by any material in the project.
            // Use the primitive's existing material as a base (always available).
            Renderer rend = gridFloor.GetComponent<Renderer>();
            Material gridMat = rend.material; // copy of default-diffuse
            gridMat.mainTexture = gridTex;
            gridMat.mainTextureScale = new Vector2(400, 400);
            if (gridMat.HasProperty("_Glossiness"))
                gridMat.SetFloat("_Glossiness", 0f);
        }

        private void SyncPreviewLayers(GameObject canvasView)
        {
            foreach (Transform child in previewLayersList.transform) Object.Destroy(child.gameObject);
            
            Transform paper = canvasView.transform.Find("EditorArea/Workspace/Paper");
            if (paper == null) return;

            foreach (Transform t in paper)
            {
                GameObject item = UIFactory.CreateObject("LayerItem", previewLayersList);
                item.AddComponent<LayoutElement>().minHeight = 35;
                HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(10, 10, 5, 5); hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;

                // Determine Layer Type Name
                string layerTypeName = "Layer";
                if (t.GetComponent<Text>()) layerTypeName = "Text Layer";
                else if (t.GetComponent<Image>()) layerTypeName = "Image Layer";
                if (t.name.Contains("Design")) layerTypeName = "Vector Layer";

                // Icon (Placeholder)
                GameObject icon = UIFactory.CreateObject("Icon", item);
                icon.AddComponent<LayoutElement>().minWidth = 20; icon.GetComponent<LayoutElement>().minHeight = 20;
                icon.AddComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);

                UIFactory.CreateText(layerTypeName, item, 12, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            }
        }

        private void Setup3DDesign(GameObject canvasView, Model3DViewer viewer)
        {
            if (viewer.modelContainer == null) return;
            
            // USER REQ: Clear ALL design objects immediately to avoid accumulation/lag
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in viewer.modelContainer.transform) 
            {
                // Destroy everything except established grid/floor if they are special
                // But usually better to clear and rebuild for consistency
                toDestroy.Add(child.gameObject);
            }
            foreach (var gd in toDestroy) Object.DestroyImmediate(gd);
            
            // Re-create Grid and Light if they were destroyed
            Create3DGrid(viewer.modelContainer);
            
            // Lighting for Preview: directional base + tight moving spotlight (~1/20 of image)
            GameObject keyLightObj = new GameObject("PreviewKeyLight");
            keyLightObj.transform.SetParent(viewer.modelContainer.transform);
            keyLightObj.transform.rotation = Quaternion.Euler(50, 30, 0);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = Color.white;
            keyLight.intensity = 0.6f;

            // Moving spotlight: tight cone aiming straight down, spot moves with orbit
            GameObject lightObj = new GameObject("PreviewLight");
            lightObj.transform.SetParent(viewer.modelContainer.transform);
            lightObj.transform.localPosition = new Vector3(0, 14, 0);
            lightObj.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 5f;
            l.innerSpotAngle = 3f;
            l.range = 30f;
            l.intensity = 1.8f;
            l.color = new Color(1f, 0.99f, 0.97f);
            l.shadows = LightShadows.None;
            var mle = lightObj.AddComponent<PocoRender.UI.MovingLightEffect>();
            mle.surfaceY = 0.06f;
            mle.orbitRadius = 2.0f;
            mle.orbitHeight = 14f;
            mle.orbitSpeed = 0.35f;

            // Moderate ambient for Preview
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            // 1. Root container for the design
            GameObject designStage = new GameObject("DesignStage");
            designStage.transform.SetParent(viewer.modelContainer.transform);
            designStage.transform.localPosition = Vector3.zero;
            designStage.transform.localRotation = Quaternion.identity;
            
            // 2. The white background "Paper"
            GameObject paperPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            paperPlane.name = "PaperPlane";
            paperPlane.transform.SetParent(designStage.transform);
            paperPlane.transform.localScale = new Vector3(6, 6, 1); 
            paperPlane.transform.localPosition = new Vector3(0, 0.01f, 0); // Base level
            paperPlane.transform.localRotation = Quaternion.Euler(90, 0, 0); 
            
            Shader uiShader = SafeShaderHelper.GetUIDefaultShader();
            Material paperMat = uiShader != null ? new Material(uiShader) : paperPlane.GetComponent<Renderer>().material;
            paperMat.color = Color.white;
            paperPlane.GetComponent<Renderer>().material = paperMat;

            // 3. World Space Canvas
            GameObject worldCanvasObj = new GameObject("WorldCanvas");
            worldCanvasObj.transform.SetParent(designStage.transform);
            worldCanvasObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            // INCREASED OFFSET: 0.05 instead of 0.01 to avoid Z-fighting with multiple layers
            worldCanvasObj.transform.localPosition = new Vector3(0, 0.06f, 0); 
            worldCanvasObj.transform.localScale = Vector3.one * 0.01f; 

            Canvas c = worldCanvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            // CRITICAL: Assign the render camera so UI knows how to project itself
            c.worldCamera = viewer.GetComponentInChildren<Camera>();
            
            worldCanvasObj.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1;
            
            RectTransform rc = worldCanvasObj.GetComponent<RectTransform>();
            rc.sizeDelta = new Vector2(600, 600);

            // Mesh root for 2.5D texture modes (parallax quads)
            GameObject meshRoot = new GameObject("MeshLayers");
            meshRoot.transform.SetParent(designStage.transform, false);
            meshRoot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            meshRoot.transform.localPosition = new Vector3(0, 0.06f, 0);
            meshRoot.transform.localScale = Vector3.one * 0.01f;

            // Copy design content
            Transform paper = canvasView.transform.Find("EditorArea/Workspace/Paper");
            if (paper != null)
            {
                float zUi = 0f;
                foreach (Transform child in paper)
                {
                    // USER REQ: Full preview should show all layers
                    // Skip the background deselector and clone everything else that is active
                        if (child.name != "BGDeselector")
                        {
                            var ld = child.GetComponent<LayerData>();
                            string craftMode = ld != null ? ld.craftMode : null;
                            bool isTextureMode = TextureModeUtil.TryParseCraftMode(craftMode, out TextureMode texMode)
                                                 && TextureModeUtil.IsParallaxMode(texMode);

                            Image img = child.GetComponent<Image>();
                            RectTransform rt = child.GetComponent<RectTransform>();

                            if (isTextureMode && img != null && img.sprite != null && rt != null)
                            {
                                // 2.5D Parallax quad for texture modes
                                Texture2D depthOverride = null;
                                if (texMode == TextureMode.CustomizeTexture && ld != null && ld.customDepthMap != null)
                                {
                                    depthOverride = ld.customDepthMap;
                                }
                                var quad = PreviewMeshBuilder.BuildImageLayerQuad(img, rt, meshRoot.transform, zUi, texMode, depthOverride, 512);
                                if (quad != null) SetLayerRecursiveStatic(quad, viewer.gameObject.layer);
                            }
                            else
                            {
                                // Fallback: keep old flat UI clone
                                GameObject copy = Object.Instantiate(child.gameObject, worldCanvasObj.transform);
                                copy.SetActive(true);

                                if (copy.GetComponent<Outline>()) Object.Destroy(copy.GetComponent<Outline>());
                                if (copy.GetComponent<ObjectManipulator>()) Object.Destroy(copy.GetComponent<ObjectManipulator>());
                                if (copy.GetComponent<BoxCollider2D>()) Object.Destroy(copy.GetComponent<BoxCollider2D>());
                                if (copy.GetComponent<CanvasGroup>()) Object.Destroy(copy.GetComponent<CanvasGroup>());

                                Transform rotHandle = copy.transform.Find("RotationHandle");
                                if (rotHandle != null) Object.Destroy(rotHandle.gameObject);

                                SetLayerRecursiveStatic(copy, viewer.gameObject.layer);
                            }

                            zUi += 4f; // ~0.04 units after 0.01 scale
                        }
                }
            }
            
            viewer.SetModel(designStage);

            // Disable Model3DViewer's own SceneLight so our custom lights control the scene
            if (viewer.sceneLight != null)
            {
                viewer.sceneLight.enabled = false;
            }

            // Low ambient so spotlight creates visible contrast
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        private void SendCanvasToPrint(CanvasController controller, string projectName)
        {
            if (controller == null || controller.paper == null) return;

            if (!PrintClient.Instance.IsConnected)
                PrintClient.Instance.Connect("127.0.0.1", BuildMode.PrintServicePort);

            if (!PrintClient.Instance.IsConnected)
            {
                Debug.LogWarning("[HomeModule] Cannot connect to PocoStudio print service");
                return;
            }

            try
            {
                // USER REQ:
                // - 如果当前选中了某个图层，仅打印该图层（不包含其它图层和旋转手柄等控件）。
                // - 如果当前未选中任何单独图层（即点击在画布空白处），则打印所有可见图层的混合结果。
                Texture2D composite = null;

                var selected = controller.CurrentSelection;
                bool hasSingleSelection =
                    selected != null &&
                    controller.paper != null &&
                    // 必须是真正在画布上的可编辑图层（带 ObjectManipulator），
                    // 而不是 BGDeselector、背景节点或其它辅助控件
                    selected.transform.IsChildOf(controller.paper) &&
                    selected.name != "BGDeselector" &&
                    selected.GetComponent<ObjectManipulator>() != null;

                if (hasSingleSelection)
                {
                    // 构造一个临时的“虚拟画布”，尺寸与原始 paper 相同，但只包含当前选中的图层。
                    // 这样可以在不影响场景的前提下，让 CapturePaperFlat 只渲染该图层。
                    GameObject tempPaper = new GameObject("_TempPaperForPrint");
                    RectTransform tempRt = tempPaper.AddComponent<RectTransform>();

                    // 保持与原始 paper 一致的尺寸，锚点居中即可。
                    tempRt.sizeDelta = controller.paper.rect.size;
                    tempRt.pivot = controller.paper.pivot;
                    tempRt.anchorMin = new Vector2(0.5f, 0.5f);
                    tempRt.anchorMax = new Vector2(0.5f, 0.5f);
                    tempRt.anchoredPosition = Vector2.zero;

                    // 在临时画布下克隆当前选中的图层（包含其内部所有可见子节点）。
                    GameObject singleLayerClone = Object.Instantiate(selected, tempPaper.transform);
                    singleLayerClone.SetActive(true);

                    composite = CapturePaperFlat(tempRt);

                    Object.DestroyImmediate(tempPaper);
                }
                else
                {
                    // 未选中具体图层时，视为“整张画布”打印：渲染 paper 下所有可见子节点。
                    composite = CapturePaperFlat(controller.paper);
                }

                if (composite == null)
                {
                    Debug.LogError("[HomeModule] Paper capture returned null");
                    return;
                }

                byte[] png = composite.EncodeToPNG();
                int w = composite.width;
                int h = composite.height;
                Object.Destroy(composite);

                var printOptions = new PrintClient.PrintOptions
                {
                    dpi = Mathf.Max(72, controller.printResolutionDpi),
                    copies = Mathf.Max(1, controller.printCopies),
                    paper_size = string.IsNullOrEmpty(controller.printPaperSize) ? "A4" : controller.printPaperSize,
                    color_profile = string.IsNullOrEmpty(controller.printColorMode) ? "CMYK" : controller.printColorMode,
                    media_type = string.IsNullOrEmpty(controller.printMediaType) ? "plain" : controller.printMediaType,
                    mirror_print = controller.printMirror,
                    color_mode = string.IsNullOrEmpty(controller.printColorMode) ? "CMYK" : controller.printColorMode,
                    enable_halftone = controller.printEnableHalftone,
                    enable_ink_optimization = controller.printEnableInkOptimization,
                    enable_skin_detection = controller.printEnableSkinDetection,
                    enable_guided_filter = controller.printEnableGuidedFilter,
                    show_ink_preview = controller.printShowInkPreview
                };

                bool sent = PrintClient.Instance.SendPrintRequestWithData(
                    projectName, png, w, h, printOptions);
                Debug.Log($"[HomeModule] Print sent: {sent}  {w}x{h}  {png.Length} bytes");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HomeModule] Print export failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Render the paper and ALL its child layers to a flat 2D Texture2D by
        /// cloning the paper hierarchy into a temporary ScreenSpaceCamera canvas
        /// that targets a RenderTexture. This captures every visible layer as a
        /// properly composited flat image (not a 3D perspective view).
        /// </summary>
        private static Texture2D CapturePaperFlat(RectTransform paper)
        {
            // Use 2x the UI size for sharper output (can be tuned)
            int outputWidth  = Mathf.RoundToInt(paper.rect.width)  * 2;
            int outputHeight = Mathf.RoundToInt(paper.rect.height) * 2;
            if (outputWidth <= 0 || outputHeight <= 0) return null;

            const int captureLayer = 31; // unused layer for isolation

            // 1. Temporary Camera (orthographic, white background)
            GameObject camObj = new GameObject("_CaptureCam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = outputHeight / 2f;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = Color.white;
            cam.cullingMask      = 1 << captureLayer;
            cam.enabled          = false; // manual render only

            // 2. Temporary Canvas (ScreenSpaceCamera → renders to camera's RT)
            GameObject canvasObj = new GameObject("_CaptureCanvas");
            Canvas canvas       = canvasObj.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera  = cam;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode  = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.layer = captureLayer;

            // 3. Clone the paper into the temp canvas
            GameObject clone = Object.Instantiate(paper.gameObject, canvas.transform);
            SetLayerRecursiveStatic(clone, captureLayer);

            // Disable any scripts that would interfere during the capture frame
            foreach (var mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is UnityEngine.UI.Image || mb is UnityEngine.UI.RawImage ||
                    mb is UnityEngine.UI.Graphic || mb is CanvasRenderer ||
                    mb is RectTransform || mb is CanvasGroup)
                    continue;
                mb.enabled = false;
            }

            // 移除旋转手柄图标，避免在导出的打印图中看到编辑控制点
            var rotationHandlers = clone.GetComponentsInChildren<RotationHandler>(true);
            foreach (var handler in rotationHandlers)
            {
                if (handler != null)
                {
                    Object.DestroyImmediate(handler.gameObject);
                }
            }

            // Reset transform so the clone fills the canvas correctly
            RectTransform cloneRT  = clone.GetComponent<RectTransform>();
            cloneRT.anchorMin      = new Vector2(0.5f, 0.5f);
            cloneRT.anchorMax      = new Vector2(0.5f, 0.5f);
            cloneRT.pivot          = new Vector2(0.5f, 0.5f);
            cloneRT.anchoredPosition = Vector2.zero;
            cloneRT.localScale     = Vector3.one;
            cloneRT.localRotation  = Quaternion.identity;
            cloneRT.sizeDelta      = new Vector2(outputWidth, outputHeight);

            // Scale children proportionally
            float scaleX = (float)outputWidth  / paper.rect.width;
            float scaleY = (float)outputHeight / paper.rect.height;
            for (int i = 0; i < cloneRT.childCount; i++)
            {
                RectTransform childRT = cloneRT.GetChild(i) as RectTransform;
                if (childRT == null) continue;
                childRT.anchoredPosition *= new Vector2(scaleX, scaleY);
                childRT.sizeDelta        *= new Vector2(scaleX, scaleY);
            }

            Canvas.ForceUpdateCanvases();

            // 4. Render to RenderTexture
            RenderTexture rt = new RenderTexture(outputWidth, outputHeight, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            // 5. Read pixels
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, outputWidth, outputHeight), 0, 0);
            tex.Apply();

            // 6. Cleanup
            RenderTexture.active = null;
            cam.targetTexture    = null;
            Object.DestroyImmediate(clone);
            Object.DestroyImmediate(canvasObj);
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(rt);

            return tex;
        }

        private static void SetLayerRecursiveStatic(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursiveStatic(child.gameObject, layer);
        }

    
    }
}



