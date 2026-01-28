using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using NeXTMake.UI;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.Editor
{
    /// <summary>
    /// Studio UI自动设置工具
    /// 在Unity编辑器中自动创建Studio风格的UI结构
    /// </summary>
    public class StudioUISetup : EditorWindow
    {
        [MenuItem("Tools/Studio UI/创建Studio界面")]
        public static void CreateStudioUI()
        {
            // 查找或创建Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                
                Debug.Log("已创建Canvas");
            }
            
            // 检查是否已存在Studio UI
            if (FindObjectOfType<StudioUIManager>() != null)
            {
                if (!EditorUtility.DisplayDialog("Studio UI已存在", 
                    "场景中已存在Studio UI结构。是否要删除旧的并重新创建？", 
                    "重新创建", "取消"))
                {
                    return;
                }
                
                // 删除旧的Studio UI
                StudioUIManager oldManager = FindObjectOfType<StudioUIManager>();
                if (oldManager != null)
                {
                    DestroyImmediate(oldManager.gameObject);
                }
            }
            
            // 创建主容器
            GameObject mainContainer = new GameObject("StudioUIContainer");
            mainContainer.transform.SetParent(canvas.transform, false);
            RectTransform mainRect = mainContainer.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.sizeDelta = Vector2.zero;
            mainRect.anchoredPosition = Vector2.zero;
            
            StudioUIManager studioManager = mainContainer.AddComponent<StudioUIManager>();
            
            // 创建菜单栏
            GameObject menuBar = CreateMenuBar(mainContainer.transform);
            studioManager.menuBarContainer = menuBar.GetComponent<RectTransform>();
            
            // 创建工具栏
            GameObject toolBar = CreateToolBar(mainContainer.transform);
            studioManager.toolBarContainer = toolBar.GetComponent<RectTransform>();
            
            // 创建左侧面板
            GameObject leftPanel = CreateLeftPanel(mainContainer.transform);
            studioManager.leftPanelContainer = leftPanel.GetComponent<RectTransform>();
            
            // 创建右侧面板
            GameObject rightPanel = CreateRightPanel(mainContainer.transform);
            studioManager.rightPanelContainer = rightPanel.GetComponent<RectTransform>();
            
            // 创建主视图
            GameObject mainView = CreateMainView(mainContainer.transform);
            studioManager.mainViewContainer = mainView.GetComponent<RectTransform>();
            
            // 创建状态栏
            GameObject statusBar = CreateStatusBar(mainContainer.transform, studioManager);
            studioManager.statusBarContainer = statusBar.GetComponent<RectTransform>();
            
            // 连接所有引用
            ConnectReferences(studioManager, menuBar, toolBar, leftPanel, rightPanel, mainView, statusBar);
            
            // 更新布局
            studioManager.SendMessage("InitializeLayout", SendMessageOptions.DontRequireReceiver);
            
            EditorUtility.DisplayDialog("完成", "Studio UI界面已创建成功！\n\n请在Inspector中检查StudioUIManager组件的设置。", "确定");
            
            Selection.activeGameObject = mainContainer;
            EditorGUIUtility.PingObject(mainContainer);
        }
        
        static GameObject CreateMenuBar(Transform parent)
        {
            GameObject menuBar = new GameObject("MenuBar");
            menuBar.transform.SetParent(parent, false);
            
            RectTransform rect = menuBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(0, -30);
            rect.offsetMax = new Vector2(0, 0);
            
            Image bg = menuBar.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            HorizontalLayoutGroup layout = menuBar.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.spacing = 5f;
            layout.padding = new RectOffset(5, 5, 2, 2);
            
            MenuBar menuBarScript = menuBar.AddComponent<MenuBar>();
            
            // 创建菜单按钮
            string[] menuNames = { "文件", "编辑", "视图", "工具", "帮助" };
            foreach (string menuName in menuNames)
            {
                GameObject btn = CreateButton(menuName, menuBar.transform);
                Button button = btn.GetComponent<Button>();
                
                switch (menuName)
                {
                    case "文件": menuBarScript.fileMenuButton = button; break;
                    case "编辑": menuBarScript.editMenuButton = button; break;
                    case "视图": menuBarScript.viewMenuButton = button; break;
                    case "工具": menuBarScript.toolsMenuButton = button; break;
                    case "帮助": menuBarScript.helpMenuButton = button; break;
                }
            }
            
            return menuBar;
        }
        
        static GameObject CreateToolBar(Transform parent)
        {
            GameObject toolBar = new GameObject("ToolBar");
            toolBar.transform.SetParent(parent, false);
            
            RectTransform rect = toolBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(0, -70);
            rect.offsetMax = new Vector2(0, -30);
            
            Image bg = toolBar.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            
            HorizontalLayoutGroup layout = toolBar.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.spacing = 5f;
            layout.padding = new RectOffset(5, 5, 2, 2);
            
            ToolBar toolBarScript = toolBar.AddComponent<ToolBar>();
            
            // 创建工具栏按钮
            string[] toolNames = { "新建", "打开", "保存", "撤销", "重做", "设置" };
            Button[] buttons = new Button[toolNames.Length];
            for (int i = 0; i < toolNames.Length; i++)
            {
                GameObject btn = CreateButton(toolNames[i], toolBar.transform);
                buttons[i] = btn.GetComponent<Button>();
            }
            
            toolBarScript.newProjectButton = buttons[0];
            toolBarScript.openProjectButton = buttons[1];
            toolBarScript.saveProjectButton = buttons[2];
            toolBarScript.undoButton = buttons[3];
            toolBarScript.redoButton = buttons[4];
            toolBarScript.settingsButton = buttons[5];
            
            return toolBar;
        }
        
        static GameObject CreateLeftPanel(Transform parent)
        {
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(parent, false);
            
            RectTransform rect = leftPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.offsetMin = new Vector2(0, 25);
            rect.offsetMax = new Vector2(250, -70);
            
            Image bg = leftPanel.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            
            VerticalLayoutGroup layout = leftPanel.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.spacing = 5f;
            layout.padding = new RectOffset(5, 5, 5, 5);
            
            LeftPanel leftPanelScript = leftPanel.AddComponent<LeftPanel>();
            leftPanelScript.panelContainer = rect;
            
            // 创建折叠按钮
            GameObject toggleBtn = CreateButton("◄", leftPanel.transform);
            leftPanelScript.toggleButton = toggleBtn.GetComponent<Button>();
            
            // 创建内容容器
            GameObject content = new GameObject("Content");
            content.transform.SetParent(leftPanel.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;
            
            leftPanelScript.panelContent = content;
            
            // 创建工具按钮
            string[] toolNames = { "选择", "画笔", "橡皮擦", "形状", "文本" };
            Button[] toolButtons = new Button[toolNames.Length];
            for (int i = 0; i < toolNames.Length; i++)
            {
                GameObject btn = CreateButton(toolNames[i], content.transform);
                toolButtons[i] = btn.GetComponent<Button>();
            }
            
            leftPanelScript.toolSelectButton = toolButtons[0];
            leftPanelScript.toolBrushButton = toolButtons[1];
            leftPanelScript.toolEraserButton = toolButtons[2];
            leftPanelScript.toolShapeButton = toolButtons[3];
            leftPanelScript.toolTextButton = toolButtons[4];
            
            return leftPanel;
        }
        
        static GameObject CreateRightPanel(Transform parent)
        {
            GameObject rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(parent, false);
            
            RectTransform rect = rightPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(-300, 25);
            rect.offsetMax = new Vector2(0, -70);
            
            Image bg = rightPanel.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            
            VerticalLayoutGroup layout = rightPanel.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            
            RightPanel rightPanelScript = rightPanel.AddComponent<RightPanel>();
            rightPanelScript.panelContainer = rect;
            
            // 创建折叠按钮
            GameObject toggleBtn = CreateButton("►", rightPanel.transform);
            rightPanelScript.toggleButton = toggleBtn.GetComponent<Button>();
            
            // 创建内容容器
            GameObject content = new GameObject("Content");
            content.transform.SetParent(rightPanel.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;
            
            rightPanelScript.panelContent = content;
            
            // 创建属性控件
            CreatePropertySlider(content.transform, "不透明度", out Slider opacitySlider);
            CreatePropertySlider(content.transform, "画笔大小", out Slider brushSizeSlider);
            CreatePropertySlider(content.transform, "旋转", out Slider rotationSlider);
            CreatePropertySlider(content.transform, "缩放", out Slider scaleSlider);
            
            rightPanelScript.opacitySlider = opacitySlider;
            rightPanelScript.brushSizeSlider = brushSizeSlider;
            rightPanelScript.rotationSlider = rotationSlider;
            rightPanelScript.scaleSlider = scaleSlider;
            
            // 创建颜色选择器
            GameObject colorBtn = CreateButton("颜色", content.transform);
            rightPanelScript.colorPickerButton = colorBtn.GetComponent<Button>();
            
            GameObject colorPreview = new GameObject("ColorPreview");
            colorPreview.transform.SetParent(content.transform, false);
            RectTransform colorRect = colorPreview.AddComponent<RectTransform>();
            colorRect.sizeDelta = new Vector2(100, 30);
            Image colorImg = colorPreview.AddComponent<Image>();
            colorImg.color = Color.white;
            rightPanelScript.colorPreviewImage = colorImg;
            
            return rightPanel;
        }
        
        static GameObject CreateMainView(Transform parent)
        {
            GameObject mainView = new GameObject("MainView");
            mainView.transform.SetParent(parent, false);
            
            RectTransform rect = mainView.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(250, 25);
            rect.offsetMax = new Vector2(-300, -70);
            
            Image bg = mainView.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            ScrollRect scrollRect = mainView.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            
            // 创建内容区域
            GameObject content = new GameObject("Content");
            content.transform.SetParent(mainView.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.content = contentRect;
            
            // 创建图像显示
            GameObject imageObj = new GameObject("ImageView");
            imageObj.transform.SetParent(content.transform, false);
            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.sizeDelta = new Vector2(800, 600);
            RawImage rawImage = imageObj.AddComponent<RawImage>();
            
            ImageViewer imageViewer = mainView.AddComponent<ImageViewer>();
            imageViewer.rawImage = rawImage;
            imageViewer.scrollRect = scrollRect;
            
            StudioUIManager manager = parent.GetComponent<StudioUIManager>();
            if (manager != null)
            {
                manager.mainViewImage = rawImage;
                manager.mainViewScrollRect = scrollRect;
                manager.mainImageViewer = imageViewer;
            }
            
            return mainView;
        }
        
        static GameObject CreateStatusBar(Transform parent, StudioUIManager manager)
        {
            GameObject statusBar = new GameObject("StatusBar");
            statusBar.transform.SetParent(parent, false);
            
            RectTransform rect = statusBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 25);
            
            Image bg = statusBar.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            
            HorizontalLayoutGroup layout = statusBar.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.spacing = 10f;
            layout.padding = new RectOffset(5, 5, 2, 2);
            
            StatusBar statusBarScript = statusBar.AddComponent<StatusBar>();
            
            // 创建状态文本（统一使用Text组件，支持中文）
            GameObject statusTextObj = CreateText("就绪", statusBar.transform);
            Text statusTextComp = statusTextObj.GetComponent<Text>();
            if (manager != null)
            {
                manager.statusText = statusTextComp;
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            statusBarScript.statusText = null; // 设为null，通过manager更新
#else
            statusBarScript.statusText = statusTextComp;
#endif
            
            GameObject zoomTextObj = CreateText("缩放: 100%", statusBar.transform);
            Text zoomTextComp = zoomTextObj.GetComponent<Text>();
            if (manager != null)
            {
                manager.zoomText = zoomTextComp;
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            statusBarScript.zoomText = null;
#else
            statusBarScript.zoomText = zoomTextComp;
#endif
            
            GameObject posTextObj = CreateText("位置: (0, 0)", statusBar.transform);
            Text posTextComp = posTextObj.GetComponent<Text>();
            if (manager != null)
            {
                manager.positionText = posTextComp;
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            statusBarScript.positionText = null;
#else
            statusBarScript.positionText = posTextComp;
#endif
            
            GameObject sizeTextObj = CreateText("大小: 0 × 0", statusBar.transform);
            Text sizeTextComp = sizeTextObj.GetComponent<Text>();
#if UNITY_EDITOR || UNITY_STANDALONE
            statusBarScript.sizeText = null;
#else
            statusBarScript.sizeText = sizeTextComp;
#endif
            
            return statusBar;
        }
        
        static GameObject CreateButton(string text, Transform parent)
        {
            GameObject btn = new GameObject($"Button_{text}");
            btn.transform.SetParent(parent, false);
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 25);
            
            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            Button button = btn.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.colors = colors;
            
            // 统一使用Text组件，支持中文
            GameObject textObj = CreateText(text, btn.transform);
            
            return btn;
        }
        
        static GameObject CreateText(string text, Transform parent)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            
            return textObj;
        }
        
        static void CreatePropertySlider(Transform parent, string label, out Slider slider)
        {
            GameObject container = new GameObject($"Slider_{label}");
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);
            
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.spacing = 5f;
            
            // 统一使用Text组件，支持中文
            GameObject labelObj = CreateText(label, container.transform);
            
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0, 20);
            
            Image bg = sliderObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            slider = sliderObj.AddComponent<Slider>();
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
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            slider.fillRect = fillRect2;
            
            slider.targetGraphic = fillImg;
        }
        
        static void ConnectReferences(StudioUIManager manager, GameObject menuBar, GameObject toolBar, 
            GameObject leftPanel, GameObject rightPanel, GameObject mainView, GameObject statusBar)
        {
            // 连接菜单栏引用
            MenuBar menuBarScript = menuBar.GetComponent<MenuBar>();
            manager.fileMenuButton = menuBarScript.fileMenuButton;
            manager.editMenuButton = menuBarScript.editMenuButton;
            manager.viewMenuButton = menuBarScript.viewMenuButton;
            manager.toolsMenuButton = menuBarScript.toolsMenuButton;
            manager.helpMenuButton = menuBarScript.helpMenuButton;
            
            // 连接工具栏引用
            ToolBar toolBarScript = toolBar.GetComponent<ToolBar>();
            manager.newProjectButton = toolBarScript.newProjectButton;
            manager.openProjectButton = toolBarScript.openProjectButton;
            manager.saveProjectButton = toolBarScript.saveProjectButton;
            manager.undoButton = toolBarScript.undoButton;
            manager.redoButton = toolBarScript.redoButton;
            manager.settingsButton = toolBarScript.settingsButton;
            
            // 连接左侧面板引用
            LeftPanel leftPanelScript = leftPanel.GetComponent<LeftPanel>();
            manager.toggleLeftPanelButton = leftPanelScript.toggleButton;
            manager.leftPanelContent = leftPanelScript.panelContent;
            manager.toolSelectButton = leftPanelScript.toolSelectButton;
            manager.toolBrushButton = leftPanelScript.toolBrushButton;
            manager.toolEraserButton = leftPanelScript.toolEraserButton;
            manager.toolShapeButton = leftPanelScript.toolShapeButton;
            manager.toolTextButton = leftPanelScript.toolTextButton;
            
            // 连接右侧面板引用
            RightPanel rightPanelScript = rightPanel.GetComponent<RightPanel>();
            manager.toggleRightPanelButton = rightPanelScript.toggleButton;
            manager.rightPanelContent = rightPanelScript.panelContent;
            manager.opacitySlider = rightPanelScript.opacitySlider;
            manager.brushSizeSlider = rightPanelScript.brushSizeSlider;
            manager.colorPickerButton = rightPanelScript.colorPickerButton;
            manager.colorPreviewImage = rightPanelScript.colorPreviewImage;
            
            // 连接状态栏引用
            StatusBar statusBarScript = statusBar.GetComponent<StatusBar>();
            // 从StatusBar获取Text组件（可能是TextMeshPro或Text）
#if UNITY_EDITOR || UNITY_STANDALONE
            if (statusBarScript.statusText != null)
            {
                // 尝试从GameObject获取Text组件
                Text textComp = statusBarScript.statusText.gameObject.GetComponent<Text>();
                if (textComp != null)
                {
                    manager.statusText = textComp;
                }
            }
            if (statusBarScript.zoomText != null)
            {
                Text textComp = statusBarScript.zoomText.gameObject.GetComponent<Text>();
                if (textComp != null)
                {
                    manager.zoomText = textComp;
                }
            }
            if (statusBarScript.positionText != null)
            {
                Text textComp = statusBarScript.positionText.gameObject.GetComponent<Text>();
                if (textComp != null)
                {
                    manager.positionText = textComp;
                }
            }
#else
            manager.statusText = statusBarScript.statusText;
            manager.zoomText = statusBarScript.zoomText;
            manager.positionText = statusBarScript.positionText;
#endif
        }
    }
}
