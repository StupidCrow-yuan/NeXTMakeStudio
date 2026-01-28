using UnityEngine;
using NeXTMake.UI;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.UI
{
    /// <summary>
    /// 运行时自动创建Studio UI界面
    /// 如果场景中没有Studio UI，会自动创建
    /// </summary>
    public class StudioUIAutoSetup : MonoBehaviour
    {
        [Header("自动创建设置")]
        public bool autoCreateOnStart = true;
        public bool hideOldUI = true;
        
        void Start()
        {
            if (autoCreateOnStart)
            {
                SetupStudioUI();
            }
        }
        
        void SetupStudioUI()
        {
            // 检查是否已存在Studio UI
            StudioUIManager existingStudio = FindObjectOfType<StudioUIManager>();
            if (existingStudio != null)
            {
                Debug.Log("[StudioUIAutoSetup] Studio UI已存在，跳过创建");
                return;
            }
            
            // 查找或创建Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                UnityEngine.UI.CanvasScaler scaler = canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            
            // 删除旧的UI（如果存在）
            if (hideOldUI)
            {
                // 查找旧的Canvas和UI元素
                Canvas oldCanvas = canvas;
                if (oldCanvas != null)
                {
                    // 查找旧的MainPanel等UI元素
                    Transform oldMainPanel = oldCanvas.transform.Find("MainPanel");
                    if (oldMainPanel != null)
                    {
                        DestroyImmediate(oldMainPanel.gameObject);
                        Debug.Log("[StudioUIAutoSetup] 已删除旧的MainPanel");
                    }
                    
                    // 查找旧的按钮等UI元素（保留Canvas本身）
                    foreach (Transform child in oldCanvas.transform)
                    {
                        if (child.name != "StudioUIContainer" && 
                            child.name != "EventSystem" &&
                            child.GetComponent<MainUIManager>() == null)
                        {
                            // 检查是否是旧的UI元素
                            if (child.name.Contains("Button") || 
                                child.name.Contains("Panel") ||
                                child.name.Contains("Image") ||
                                child.name.Contains("Text"))
                            {
                                DestroyImmediate(child.gameObject);
                            }
                        }
                    }
                }
                
                // 禁用或删除旧的MainUIManager（保留组件但禁用GameObject）
                MainUIManager oldMainUI = FindObjectOfType<MainUIManager>();
                if (oldMainUI != null && oldMainUI.gameObject != null)
                {
                    // 如果MainUIManager在Canvas上，禁用Canvas下的旧UI元素
                    if (oldMainUI.transform.parent == canvas.transform)
                    {
                        oldMainUI.gameObject.SetActive(false);
                        Debug.Log("[StudioUIAutoSetup] 已禁用旧的MainUIManager GameObject");
                    }
                }
            }
            
            // 使用编辑器工具创建Studio UI
            // 注意：运行时无法使用EditorUtility，所以我们需要手动创建
            CreateStudioUIAtRuntime(canvas.transform);
        }
        
        void CreateStudioUIAtRuntime(Transform canvasTransform)
        {
            Debug.Log("[StudioUIAutoSetup] 开始创建Studio UI界面...");
            
            // 创建主容器
            GameObject mainContainer = new GameObject("StudioUIContainer");
            mainContainer.transform.SetParent(canvasTransform, false);
            RectTransform mainRect = mainContainer.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.sizeDelta = Vector2.zero;
            mainRect.anchoredPosition = Vector2.zero;
            
            StudioUIManager studioManager = mainContainer.AddComponent<StudioUIManager>();
            
            // 重要：设置mainContainer引用
            studioManager.mainContainer = mainRect;
            
            // 创建各个面板（简化版本，只创建基本结构）
            CreateBasicStudioPanels(mainContainer.transform, studioManager);
            
            // 连接MainUIManager（如果存在）
            MainUIManager mainUI = FindObjectOfType<MainUIManager>();
            if (mainUI != null)
            {
                mainUI.studioUIManager = studioManager;
                StatusBar statusBar = FindObjectOfType<StatusBar>();
                if (statusBar != null)
                {
                    mainUI.statusBar = statusBar;
                }
            }
            
            Debug.Log("[StudioUIAutoSetup] Studio UI界面创建完成！");
        }
        
        void CreateBasicStudioPanels(Transform parent, StudioUIManager manager)
        {
            try
            {
                Debug.Log("[StudioUIAutoSetup] 开始创建菜单栏...");
                // 创建菜单栏
                GameObject menuBar = CreatePanel("MenuBar", parent, 
                    new Vector2(0, 1), new Vector2(1, 1), 
                    new Vector2(0, -30), new Vector2(0, 0),
                    new Color(0.2f, 0.2f, 0.2f, 1f));
                if (menuBar != null)
                {
                    manager.menuBarContainer = menuBar.GetComponent<RectTransform>();
                    
                    // 添加MenuBar组件并创建基本按钮
                    MenuBar menuBarScript = menuBar.AddComponent<MenuBar>();
                    CreateMenuButtons(menuBar.transform, menuBarScript, manager);
                    Debug.Log("[StudioUIAutoSetup] 菜单栏创建完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] 创建菜单栏失败: {e.Message}\n{e.StackTrace}");
            }
            
            try
            {
                Debug.Log("[StudioUIAutoSetup] 开始创建工具栏...");
                // 创建工具栏
                GameObject toolBar = CreatePanel("ToolBar", parent,
                    new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(0, -70), new Vector2(0, -30),
                    new Color(0.25f, 0.25f, 0.25f, 1f));
                if (toolBar != null)
                {
                    manager.toolBarContainer = toolBar.GetComponent<RectTransform>();
                    
                    // 添加ToolBar组件并创建工具栏按钮
                    ToolBar toolBarScript = toolBar.AddComponent<ToolBar>();
                    CreateToolBarButtons(toolBar.transform, toolBarScript, manager);
                    
                    // 创建模式切换按钮
                    CreateModeSwitchButtons(toolBar.transform, manager);
                    
                    Debug.Log("[StudioUIAutoSetup] 工具栏创建完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] 创建工具栏失败: {e.Message}\n{e.StackTrace}");
            }
            
            try
            {
                Debug.Log("[StudioUIAutoSetup] 开始创建左侧面板...");
                // 创建左侧面板
                GameObject leftPanel = CreatePanel("LeftPanel", parent,
                    new Vector2(0, 0), new Vector2(0, 1),
                    new Vector2(0, 25), new Vector2(250, -70),
                    new Color(0.22f, 0.22f, 0.22f, 1f));
                if (leftPanel != null)
                {
                    manager.leftPanelContainer = leftPanel.GetComponent<RectTransform>();
                    GameObject leftContent = new GameObject("Content");
                    leftContent.transform.SetParent(leftPanel.transform, false);
                    RectTransform leftContentRect = leftContent.AddComponent<RectTransform>();
                    leftContentRect.anchorMin = Vector2.zero;
                    leftContentRect.anchorMax = Vector2.one;
                    leftContentRect.sizeDelta = Vector2.zero;
                    manager.leftPanelContent = leftContent;
                    
                    // 添加LeftPanel组件
                    LeftPanel leftPanelScript = leftPanel.AddComponent<LeftPanel>();
                    leftPanelScript.panelContainer = manager.leftPanelContainer;
                    leftPanelScript.panelContent = leftContent;
                    CreateToolButtons(leftContent.transform, leftPanelScript, manager);
                    Debug.Log("[StudioUIAutoSetup] 左侧面板创建完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] 创建左侧面板失败: {e.Message}\n{e.StackTrace}");
            }
            
            try
            {
                Debug.Log("[StudioUIAutoSetup] 开始创建右侧面板...");
                // 创建右侧面板
                GameObject rightPanel = CreatePanel("RightPanel", parent,
                    new Vector2(1, 0), new Vector2(1, 1),
                    new Vector2(-300, 25), new Vector2(0, -70),
                    new Color(0.22f, 0.22f, 0.22f, 1f));
                if (rightPanel != null)
                {
                    manager.rightPanelContainer = rightPanel.GetComponent<RectTransform>();
                    GameObject rightContent = new GameObject("Content");
                    rightContent.transform.SetParent(rightPanel.transform, false);
                    RectTransform rightContentRect = rightContent.AddComponent<RectTransform>();
                    rightContentRect.anchorMin = Vector2.zero;
                    rightContentRect.anchorMax = Vector2.one;
                    rightContentRect.sizeDelta = Vector2.zero;
                    manager.rightPanelContent = rightContent;
                    
                    // 添加RightPanel组件
                    RightPanel rightPanelScript = rightPanel.AddComponent<RightPanel>();
                    rightPanelScript.panelContainer = manager.rightPanelContainer;
                    rightPanelScript.panelContent = rightContent;
                    CreatePropertyControls(rightContent.transform, rightPanelScript, manager);
                    Debug.Log("[StudioUIAutoSetup] 右侧面板创建完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] 创建右侧面板失败: {e.Message}\n{e.StackTrace}");
            }
            
            try
            {
                Debug.Log("[StudioUIAutoSetup] 开始创建主视图...");
                // 创建主视图容器
                GameObject mainView = CreatePanel("MainView", parent,
                    new Vector2(0, 0), new Vector2(1, 1),
                    new Vector2(250, 25), new Vector2(-300, -70),
                    new Color(0.15f, 0.15f, 0.15f, 1f));
                if (mainView != null)
                {
                    manager.mainViewContainer = mainView.GetComponent<RectTransform>();
                    
                    // 创建UV打印视图容器
                    GameObject uvPrintView = CreateUVPrintView(mainView.transform);
                    manager.uvPrintViewContainer = uvPrintView;
                    
                    // 创建3D打印视图容器
                    GameObject print3DView = CreatePrint3DView(mainView.transform);
                    manager.print3DViewContainer = print3DView;
                    
                    Debug.Log("[StudioUIAutoSetup] 主视图创建完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] 创建主视图失败: {e.Message}\n{e.StackTrace}");
            }
            
            try
            {
                Debug.Log("[StudioUIAutoSetup] 开始创建状态栏...");
                // 创建状态栏
                GameObject statusBar = CreatePanel("StatusBar", parent,
                    new Vector2(0, 0), new Vector2(1, 0),
                    new Vector2(0, 0), new Vector2(0, 25),
                    new Color(0.18f, 0.18f, 0.18f, 1f));
                if (statusBar != null)
                {
                    manager.statusBarContainer = statusBar.GetComponent<RectTransform>();
                    
                    // 添加StatusBar组件
                    StatusBar statusBarScript = statusBar.AddComponent<StatusBar>();
                    
                    // 创建状态文本（统一使用Text组件，支持中文）
                    GameObject statusTextObj = CreateText("就绪", statusBar.transform);
                    UnityEngine.UI.Text statusTextComp = statusTextObj.GetComponent<UnityEngine.UI.Text>();
                    manager.statusText = statusTextComp;
#if UNITY_EDITOR || UNITY_STANDALONE
                    // StatusBar期望TextMeshPro，但我们可以传入Text，SetText方法会处理
                    statusBarScript.statusText = null; // 设为null，通过manager更新
#else
                    statusBarScript.statusText = statusTextComp;
#endif
                    
                    GameObject zoomTextObj = CreateText("缩放: 100%", statusBar.transform);
                    UnityEngine.UI.Text zoomTextComp = zoomTextObj.GetComponent<UnityEngine.UI.Text>();
                    manager.zoomText = zoomTextComp;
#if UNITY_EDITOR || UNITY_STANDALONE
                    statusBarScript.zoomText = null;
#else
                    statusBarScript.zoomText = zoomTextComp;
#endif
                    
                    GameObject posTextObj = CreateText("位置: (0, 0)", statusBar.transform);
                    UnityEngine.UI.Text posTextComp = posTextObj.GetComponent<UnityEngine.UI.Text>();
                    manager.positionText = posTextComp;
#if UNITY_EDITOR || UNITY_STANDALONE
                    statusBarScript.positionText = null;
#else
                    statusBarScript.positionText = posTextComp;
#endif
                    Debug.Log("[StudioUIAutoSetup] 状态栏创建完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] 创建状态栏失败: {e.Message}\n{e.StackTrace}");
            }
            
            // 创建模式管理器
            try
            {
                Debug.Log("[StudioUIAutoSetup] 开始创建模式管理器...");
                // 通过manager获取GameObject来添加组件
                PrintModeManager modeManager = manager.gameObject.AddComponent<PrintModeManager>();
                manager.printModeManager = modeManager;
                
                // 设置模式管理器的引用
                if (manager.uvPrintViewContainer != null && manager.print3DViewContainer != null)
                {
                    modeManager.uvPrintView = manager.uvPrintViewContainer;
                    modeManager.print3DView = manager.print3DViewContainer;
                    modeManager.imageViewer = manager.mainImageViewer;
                    modeManager.model3DViewer = manager.model3DViewer;
                    modeManager.model3DController = manager.model3DController;
                    
                    // 查找并设置面板引用
                    LeftPanel leftPanel = FindObjectOfType<LeftPanel>();
                    RightPanel rightPanel = FindObjectOfType<RightPanel>();
                    if (leftPanel != null)
                        modeManager.leftPanel = leftPanel;
                    if (rightPanel != null)
                        modeManager.rightPanel = rightPanel;
                    modeManager.studioUIManager = manager;
                }
                
                Debug.Log("[StudioUIAutoSetup] 模式管理器创建完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] 创建模式管理器失败: {e.Message}\n{e.StackTrace}");
            }
            
            Debug.Log("[StudioUIAutoSetup] 所有面板创建完成！");
        }
        
        /// <summary>
        /// 创建UV打印视图（2D图片编辑）
        /// </summary>
        GameObject CreateUVPrintView(Transform parent)
        {
            GameObject uvView = new GameObject("UVPrintView");
            uvView.transform.SetParent(parent, false);
            RectTransform uvRect = uvView.AddComponent<RectTransform>();
            uvRect.anchorMin = Vector2.zero;
            uvRect.anchorMax = Vector2.one;
            uvRect.sizeDelta = Vector2.zero;
            uvRect.anchoredPosition = Vector2.zero;
            
            // 添加 Mask 组件
            UnityEngine.UI.Mask mask = uvView.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;
            
            // 创建ScrollRect和ImageViewer
            UnityEngine.UI.ScrollRect scrollRect = uvView.AddComponent<UnityEngine.UI.ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            
            // 创建 Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(uvView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            // 添加背景图像
            UnityEngine.UI.Image viewportBg = viewport.AddComponent<UnityEngine.UI.Image>();
            viewportBg.color = Color.white;
            
            // 添加 Mask 到 viewport
            UnityEngine.UI.Mask viewportMask = viewport.AddComponent<UnityEngine.UI.Mask>();
            viewportMask.showMaskGraphic = false;
            
            scrollRect.viewport = viewportRect;
            
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.content = contentRect;
            
            GameObject imageObj = new GameObject("ImageView");
            imageObj.transform.SetParent(content.transform, false);
            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = new Vector2(800, 600);
            imageRect.anchoredPosition = Vector2.zero;
            UnityEngine.UI.RawImage rawImage = imageObj.AddComponent<UnityEngine.UI.RawImage>();
            
            // 创建默认占位图片
            CreateDefaultPlaceholderImage(rawImage);
            
            // 获取StudioUIManager并设置引用
            StudioUIManager manager = parent.GetComponentInParent<StudioUIManager>();
            if (manager != null)
            {
                manager.mainViewImage = rawImage;
                manager.mainViewScrollRect = scrollRect;
                
                ImageViewer imageViewer = uvView.AddComponent<ImageViewer>();
                imageViewer.rawImage = rawImage;
                imageViewer.scrollRect = scrollRect;
                manager.mainImageViewer = imageViewer;
            }
            
            return uvView;
        }
        
        /// <summary>
        /// 创建3D打印视图（3D模型编辑）
        /// </summary>
        GameObject CreatePrint3DView(Transform parent)
        {
            GameObject print3DView = new GameObject("Print3DView");
            print3DView.transform.SetParent(parent, false);
            RectTransform print3DRect = print3DView.AddComponent<RectTransform>();
            print3DRect.anchorMin = Vector2.zero;
            print3DRect.anchorMax = Vector2.one;
            print3DRect.sizeDelta = Vector2.zero;
            print3DRect.anchoredPosition = Vector2.zero;
            
            // 添加背景
            UnityEngine.UI.Image bg = print3DView.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            
            // 创建RawImage用于显示3D渲染结果
            GameObject imageObj = new GameObject("3DViewImage");
            imageObj.transform.SetParent(print3DView.transform, false);
            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.zero;
            imageRect.anchoredPosition = Vector2.zero;
            UnityEngine.UI.RawImage rawImage = imageObj.AddComponent<UnityEngine.UI.RawImage>();
            
            // 添加Model3DViewer组件
            Model3DViewer modelViewer = print3DView.AddComponent<Model3DViewer>();
            modelViewer.targetImage = rawImage;
            modelViewer.textureWidth = 1024;
            modelViewer.textureHeight = 1024;
            
            // 添加Model3DController组件
            Model3DController modelController = print3DView.AddComponent<Model3DController>();
            modelController.modelViewer = modelViewer;
            
            // 获取StudioUIManager并设置引用
            StudioUIManager manager = parent.GetComponentInParent<StudioUIManager>();
            if (manager != null)
            {
                manager.model3DViewer = modelViewer;
                manager.model3DController = modelController;
            }
            
            // 初始隐藏3D视图（默认显示UV视图）
            print3DView.SetActive(false);
            
            return print3DView;
        }
        
        /// <summary>
        /// 创建模式切换按钮
        /// </summary>
        void CreateModeSwitchButtons(Transform parent, StudioUIManager manager)
        {
            UnityEngine.UI.HorizontalLayoutGroup layout = parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = parent.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                layout.spacing = 5f;
                layout.padding = new RectOffset(5, 5, 2, 2);
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
            }
            
            // 创建分隔符（可选）
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(parent, false);
            RectTransform sepRect = separator.AddComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(2, 30);
            UnityEngine.UI.Image sepImg = separator.AddComponent<UnityEngine.UI.Image>();
            sepImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            // 创建UV打印模式按钮
            GameObject uvBtn = CreateButton("UV打印", parent, new Vector2(80, 30));
            UnityEngine.UI.Button uvButton = uvBtn.GetComponent<UnityEngine.UI.Button>();
            
            // 创建3D打印模式按钮
            GameObject print3DBtn = CreateButton("3D打印", parent, new Vector2(80, 30));
            UnityEngine.UI.Button print3DButton = print3DBtn.GetComponent<UnityEngine.UI.Button>();
            
            // 设置按钮颜色
            if (uvButton != null)
            {
                var colors = uvButton.colors;
                colors.normalColor = new Color(0.3f, 0.6f, 0.8f, 1f); // 蓝色
                uvButton.colors = colors;
            }
            
            if (print3DButton != null)
            {
                var colors = print3DButton.colors;
                colors.normalColor = new Color(0.8f, 0.5f, 0.3f, 1f); // 橙色
                print3DButton.colors = colors;
            }
            
            // 设置到PrintModeManager（稍后会在CreateBasicStudioPanels中创建）
            // 这里先保存引用，等PrintModeManager创建后再设置
            StartCoroutine(SetModeButtonsDelayed(uvButton, print3DButton, manager));
        }
        
        System.Collections.IEnumerator SetModeButtonsDelayed(UnityEngine.UI.Button uvBtn, UnityEngine.UI.Button print3DBtn, StudioUIManager manager)
        {
            // 等待多帧，确保PrintModeManager已创建
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            // 如果PrintModeManager还没创建，尝试查找
            PrintModeManager modeManager = manager.printModeManager;
            if (modeManager == null)
            {
                modeManager = manager.GetComponent<PrintModeManager>();
                if (modeManager == null)
                {
                    modeManager = FindObjectOfType<PrintModeManager>();
                }
            }
            
            if (modeManager != null)
            {
                modeManager.uvPrintModeButton = uvBtn;
                modeManager.print3DModeButton = print3DBtn;
                manager.printModeManager = modeManager;
                
                // 重新初始化按钮（确保事件已连接）
                modeManager.InitializeButtons();
                
                // 创建模式文本显示
                if (modeManager.modeText == null && manager.statusBarContainer != null)
                {
                    GameObject modeTextObj = CreateText("UV打印模式", manager.statusBarContainer);
                    if (modeTextObj != null)
                    {
                        modeManager.modeText = modeTextObj.GetComponent<UnityEngine.UI.Text>();
                    }
                }
                
                Debug.Log("[StudioUIAutoSetup] 模式切换按钮已连接");
            }
            else
            {
                Debug.LogError("[StudioUIAutoSetup] 未找到PrintModeManager组件！");
            }
        }
        
        void CreateMenuButtons(Transform parent, MenuBar menuBarScript, StudioUIManager manager)
        {
            try
            {
                UnityEngine.UI.HorizontalLayoutGroup layout = parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (layout == null)
                {
                    layout = parent.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    layout.spacing = 5f;
                    layout.padding = new RectOffset(5, 5, 2, 2);
                    layout.childControlWidth = false;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = true;
                }
                
                string[] menuNames = { "文件", "编辑", "视图", "工具", "帮助" };
                UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[menuNames.Length];
                for (int i = 0; i < menuNames.Length; i++)
                {
                    try
                    {
                        GameObject btn = CreateButton(menuNames[i], parent, new Vector2(60, 25));
                        if (btn != null)
                        {
                            buttons[i] = btn.GetComponent<UnityEngine.UI.Button>();
                            Debug.Log($"[StudioUIAutoSetup] 创建菜单按钮: {menuNames[i]}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[StudioUIAutoSetup] 创建菜单按钮 {menuNames[i]} 失败: {e.Message}");
                    }
                }
                
                if (buttons[0] != null) menuBarScript.fileMenuButton = buttons[0];
                if (buttons[1] != null) menuBarScript.editMenuButton = buttons[1];
                if (buttons[2] != null) menuBarScript.viewMenuButton = buttons[2];
                if (buttons[3] != null) menuBarScript.toolsMenuButton = buttons[3];
                if (buttons[4] != null) menuBarScript.helpMenuButton = buttons[4];
                
                if (buttons[0] != null) manager.fileMenuButton = buttons[0];
                if (buttons[1] != null) manager.editMenuButton = buttons[1];
                if (buttons[2] != null) manager.viewMenuButton = buttons[2];
                if (buttons[3] != null) manager.toolsMenuButton = buttons[3];
                if (buttons[4] != null) manager.helpMenuButton = buttons[4];
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] CreateMenuButtons失败: {e.Message}\n{e.StackTrace}");
            }
        }
        
        void CreateToolBarButtons(Transform parent, ToolBar toolBarScript, StudioUIManager manager)
        {
            UnityEngine.UI.HorizontalLayoutGroup layout = parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = parent.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                layout.spacing = 5f;
                layout.padding = new RectOffset(5, 5, 2, 2);
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
            }
            
            string[] toolNames = { "新建", "打开", "保存", "撤销", "重做", "设置" };
            UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[toolNames.Length];
            for (int i = 0; i < toolNames.Length; i++)
            {
                GameObject btn = CreateButton(toolNames[i], parent, new Vector2(60, 30));
                buttons[i] = btn.GetComponent<UnityEngine.UI.Button>();
            }
            
            toolBarScript.newProjectButton = buttons[0];
            toolBarScript.openProjectButton = buttons[1];
            toolBarScript.saveProjectButton = buttons[2];
            toolBarScript.undoButton = buttons[3];
            toolBarScript.redoButton = buttons[4];
            toolBarScript.settingsButton = buttons[5];
            
            manager.newProjectButton = buttons[0];
            manager.openProjectButton = buttons[1];
            manager.saveProjectButton = buttons[2];
            manager.undoButton = buttons[3];
            manager.redoButton = buttons[4];
            manager.settingsButton = buttons[5];
        }
        
        void CreateToolButtons(Transform parent, LeftPanel leftPanelScript, StudioUIManager manager)
        {
            UnityEngine.UI.VerticalLayoutGroup layout = parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = parent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.spacing = 5f;
                layout.padding = new RectOffset(5, 5, 5, 5);
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
            
            string[] toolNames = { "选择", "画笔", "橡皮擦", "形状", "文本" };
            UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[toolNames.Length];
            for (int i = 0; i < toolNames.Length; i++)
            {
                GameObject btn = CreateButton(toolNames[i], parent, new Vector2(0, 35));
                buttons[i] = btn.GetComponent<UnityEngine.UI.Button>();
            }
            
            leftPanelScript.toolSelectButton = buttons[0];
            leftPanelScript.toolBrushButton = buttons[1];
            leftPanelScript.toolEraserButton = buttons[2];
            leftPanelScript.toolShapeButton = buttons[3];
            leftPanelScript.toolTextButton = buttons[4];
            
            manager.toolSelectButton = buttons[0];
            manager.toolBrushButton = buttons[1];
            manager.toolEraserButton = buttons[2];
            manager.toolShapeButton = buttons[3];
            manager.toolTextButton = buttons[4];
        }
        
        void CreatePropertyControls(Transform parent, RightPanel rightPanelScript, StudioUIManager manager)
        {
            UnityEngine.UI.VerticalLayoutGroup layout = parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = parent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.padding = new RectOffset(10, 10, 10, 10);
            }
            
            // 创建不透明度滑块
            CreatePropertySlider(parent, "不透明度", out UnityEngine.UI.Slider opacitySlider);
            rightPanelScript.opacitySlider = opacitySlider;
            manager.opacitySlider = opacitySlider;
            
            // 创建画笔大小滑块
            CreatePropertySlider(parent, "画笔大小", out UnityEngine.UI.Slider brushSizeSlider);
            rightPanelScript.brushSizeSlider = brushSizeSlider;
            manager.brushSizeSlider = brushSizeSlider;
            
            // 创建颜色选择器
            GameObject colorBtn = CreateButton("颜色", parent, new Vector2(0, 35));
            rightPanelScript.colorPickerButton = colorBtn.GetComponent<UnityEngine.UI.Button>();
            manager.colorPickerButton = colorBtn.GetComponent<UnityEngine.UI.Button>();
            
            GameObject colorPreview = new GameObject("ColorPreview");
            colorPreview.transform.SetParent(parent.transform, false);
            RectTransform colorRect = colorPreview.AddComponent<RectTransform>();
            colorRect.sizeDelta = new Vector2(100, 30);
            UnityEngine.UI.Image colorImg = colorPreview.AddComponent<UnityEngine.UI.Image>();
            colorImg.color = Color.white;
            rightPanelScript.colorPreviewImage = colorImg;
            manager.colorPreviewImage = colorImg;
        }
        
        void CreatePropertySlider(Transform parent, string label, out UnityEngine.UI.Slider slider)
        {
            GameObject container = new GameObject($"Slider_{label}");
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);
            
            UnityEngine.UI.VerticalLayoutGroup layout = container.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.spacing = 5f;
            
            // 统一使用Text组件，支持中文
            CreateText(label, container.transform);
            
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0, 20);
            
            UnityEngine.UI.Image bg = sliderObj.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            
            // 创建填充区域
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect2 = fill.AddComponent<RectTransform>();
            fillRect2.sizeDelta = Vector2.zero;
            UnityEngine.UI.Image fillImg = fill.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            slider.fillRect = fillRect2;
            
            slider.targetGraphic = fillImg;
        }
        
        GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, 
            Vector2 offsetMin, Vector2 offsetMax, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            
            UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = bgColor;
            
            return panel;
        }
        
#if UNITY_EDITOR || UNITY_STANDALONE
        GameObject CreateTextMeshPro(string text, Transform parent)
        {
            // 统一使用 CreateText 方法，它已经配置了支持中文的字体
            return CreateText(text, parent);
        }
#endif
        
        GameObject CreateText(string text, Transform parent)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            // 统一使用 Unity Text 组件，并尝试加载支持中文的系统字体
            UnityEngine.UI.Text textComp = textObj.AddComponent<UnityEngine.UI.Text>();
            textComp.text = text;
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleLeft;
            textComp.raycastTarget = false; // 文本不需要接收射线
            
            // 尝试加载支持中文的系统字体
            Font font = null;
            
#if UNITY_EDITOR || UNITY_STANDALONE
            // Windows 平台：尝试加载微软雅黑或其他中文字体
            string[] chineseFontNames = { "msyh", "Microsoft YaHei", "SimHei", "SimSun", "KaiTi" };
            foreach (string fontName in chineseFontNames)
            {
                font = Font.CreateDynamicFontFromOSFont(fontName, 12);
                if (font != null)
                {
                    Debug.Log($"[StudioUIAutoSetup] 成功加载系统字体: {fontName}");
                    break;
                }
            }
            
            // 如果系统字体加载失败，尝试使用 LegacyRuntime
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    Debug.LogWarning("[StudioUIAutoSetup] 使用 LegacyRuntime 字体（可能不支持中文）");
                }
            }
#else
            // 其他平台使用 LegacyRuntime
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#endif
            
            if (font != null)
            {
                textComp.font = font;
            }
            else
            {
                Debug.LogError("[StudioUIAutoSetup] 无法加载任何字体，文本可能无法正确显示");
            }
            
            return textObj;
        }
        
        GameObject CreateButton(string text, Transform parent, Vector2 size)
        {
            try
            {
                GameObject btn = new GameObject($"Button_{text}");
                if (btn == null)
                {
                    Debug.LogError($"[StudioUIAutoSetup] 无法创建GameObject: Button_{text}");
                    return null;
                }
                
                btn.transform.SetParent(parent, false);
                
                RectTransform rect = btn.AddComponent<RectTransform>();
                if (rect == null)
                {
                    Debug.LogError($"[StudioUIAutoSetup] 无法添加RectTransform到: Button_{text}");
                    return btn; // 返回部分创建的按钮
                }
                
                // 设置anchor和pivot，确保按钮正确定位
                if (size.x == 0)
                {
                    // 宽度自适应
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.sizeDelta = new Vector2(0, size.y);
                }
                else
                {
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                    rect.sizeDelta = size;
                }
                rect.anchoredPosition = Vector2.zero;
                
                // 使用更明显的颜色，根据按钮类型设置不同颜色
                Color buttonColor = GetButtonColor(text);
                
                UnityEngine.UI.Image img = btn.AddComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = buttonColor;
                    img.raycastTarget = true;
                }
                
                UnityEngine.UI.Button button = btn.AddComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    UnityEngine.UI.ColorBlock colors = button.colors;
                    colors.normalColor = buttonColor;
                    colors.highlightedColor = new Color(
                        Mathf.Min(1f, buttonColor.r + 0.2f),
                        Mathf.Min(1f, buttonColor.g + 0.2f),
                        Mathf.Min(1f, buttonColor.b + 0.2f),
                        1f
                    );
                    colors.pressedColor = new Color(
                                Mathf.Max(0f, buttonColor.r - 0.1f),
                        Mathf.Max(0f, buttonColor.g - 0.1f),
                        Mathf.Max(0f, buttonColor.b - 0.1f),
                        1f
                    );
                    colors.selectedColor = buttonColor;
                    colors.fadeDuration = 0.1f;
                    button.colors = colors;
                }
                
                // 创建文本子对象（使用 Unity Text 组件，支持中文）
                try
                {
                    GameObject textObj = CreateText(text, btn.transform);
                    if (textObj != null)
                    {
                        UnityEngine.UI.Text textComp = textObj.GetComponent<UnityEngine.UI.Text>();
                        if (textComp != null)
                        {
                            textComp.alignment = TextAnchor.MiddleCenter;
                            textComp.fontSize = 14;
                            textComp.color = Color.white; // 确保文本颜色是白色
                            Debug.Log($"[StudioUIAutoSetup] 按钮 '{text}' 的文本组件已创建，文本内容: '{textComp.text}'，字体: {(textComp.font != null ? textComp.font.name : "null")}");
                        }
                        else
                        {
                            Debug.LogError($"[StudioUIAutoSetup] 按钮 '{text}' 的 Text 组件未找到");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[StudioUIAutoSetup] 按钮 '{text}' 的文本对象创建失败");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[StudioUIAutoSetup] 创建按钮文本失败 ({text}): {e.Message}\n{e.StackTrace}");
                }
                
                return btn;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIAutoSetup] CreateButton失败 ({text}): {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        Color GetButtonColor(string buttonText)
        {
            // 根据按钮文本返回不同的颜色，便于区分
            switch (buttonText)
            {
                case "文件": return new Color(0.4f, 0.5f, 0.6f, 1f); // 蓝色调
                case "编辑": return new Color(0.5f, 0.4f, 0.6f, 1f); // 紫色调
                case "视图": return new Color(0.4f, 0.6f, 0.5f, 1f); // 绿色调
                case "工具": return new Color(0.6f, 0.5f, 0.4f, 1f); // 橙色调
                case "帮助": return new Color(0.5f, 0.5f, 0.5f, 1f); // 灰色调
                case "新建": return new Color(0.3f, 0.6f, 0.3f, 1f); // 绿色
                case "打开": return new Color(0.3f, 0.5f, 0.7f, 1f); // 蓝色
                case "保存": return new Color(0.7f, 0.5f, 0.3f, 1f); // 橙色
                case "撤销": return new Color(0.6f, 0.4f, 0.4f, 1f); // 红色调
                case "重做": return new Color(0.4f, 0.6f, 0.4f, 1f); // 绿色调
                case "设置": return new Color(0.5f, 0.5f, 0.6f, 1f); // 蓝灰色调
                case "选择": return new Color(0.4f, 0.5f, 0.7f, 1f); // 蓝色
                case "画笔": return new Color(0.7f, 0.4f, 0.4f, 1f); // 红色
                case "橡皮擦": return new Color(0.6f, 0.6f, 0.4f, 1f); // 黄色调
                case "形状": return new Color(0.4f, 0.7f, 0.5f, 1f); // 绿色调
                case "文本": return new Color(0.7f, 0.5f, 0.4f, 1f); // 橙色调
                case "颜色": return new Color(0.6f, 0.4f, 0.7f, 1f); // 紫色
                case "UV打印": return new Color(0.3f, 0.6f, 0.8f, 1f); // 蓝色
                case "3D打印": return new Color(0.8f, 0.5f, 0.3f, 1f); // 橙色
                default: return new Color(0.4f, 0.4f, 0.4f, 1f); // 默认灰色
            }
        }
        
        void CreateDefaultPlaceholderImage(UnityEngine.UI.RawImage rawImage)
        {
            // 创建一个简单的占位纹理
            Texture2D placeholderTexture = new Texture2D(800, 600);
            Color bgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Color gridColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            
            // 填充背景
            Color[] pixels = new Color[800 * 600];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = bgColor;
            }
            placeholderTexture.SetPixels(pixels);
            
            // 绘制简单的网格线
            for (int x = 0; x < 800; x += 50)
            {
                for (int y = 0; y < 600; y++)
                {
                    if (x < 800 && y < 600)
                        placeholderTexture.SetPixel(x, y, gridColor);
                }
            }
            for (int y = 0; y < 600; y += 50)
            {
                for (int x = 0; x < 800; x++)
                {
                    if (x < 800 && y < 600)
                        placeholderTexture.SetPixel(x, y, gridColor);
                }
            }
            
            // 在中心绘制提示区域
            int centerX = 400;
            int centerY = 300;
            int boxWidth = 200;
            int boxHeight = 40;
            int startX = centerX - boxWidth / 2;
            int startY = centerY - boxHeight / 2;
            
            // 绘制背景框
            Color boxBgColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            Color boxBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            
            for (int x = startX; x < startX + boxWidth; x++)
            {
                for (int y = startY; y < startY + boxHeight; y++)
                {
                    if (x >= 0 && x < 800 && y >= 0 && y < 600)
                    {
                        placeholderTexture.SetPixel(x, y, boxBgColor);
                    }
                }
            }
            
            // 绘制边框
            for (int x = startX; x < startX + boxWidth; x++)
            {
                if (x >= 0 && x < 800)
                {
                    if (startY >= 0 && startY < 600)
                        placeholderTexture.SetPixel(x, startY, boxBorderColor);
                    if (startY + boxHeight - 1 >= 0 && startY + boxHeight - 1 < 600)
                        placeholderTexture.SetPixel(x, startY + boxHeight - 1, boxBorderColor);
                }
            }
            for (int y = startY; y < startY + boxHeight; y++)
            {
                if (y >= 0 && y < 600)
                {
                    if (startX >= 0 && startX < 800)
                        placeholderTexture.SetPixel(startX, y, boxBorderColor);
                    if (startX + boxWidth - 1 >= 0 && startX + boxWidth - 1 < 800)
                        placeholderTexture.SetPixel(startX + boxWidth - 1, y, boxBorderColor);
                }
            }
            
            placeholderTexture.Apply();
            rawImage.texture = placeholderTexture;
        }
    }
}

