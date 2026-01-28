using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NeXTMake.Core;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.UI
{
    /// <summary>
    /// Studio风格的UI管理器，提供类似NeXTMake Studio的界面布局
    /// </summary>
    public class StudioUIManager : MonoBehaviour
    {
        [Header("主布局容器")]
        public RectTransform mainContainer;
        
        [Header("顶部菜单栏")]
        public RectTransform menuBarContainer;
        public Button fileMenuButton;
        public Button editMenuButton;
        public Button viewMenuButton;
        public Button toolsMenuButton;
        public Button helpMenuButton;
        
        [Header("顶部工具栏")]
        public RectTransform toolBarContainer;
        public Button newProjectButton;
        public Button openProjectButton;
        public Button saveProjectButton;
        public Button undoButton;
        public Button redoButton;
        public Button settingsButton;
        
        [Header("左侧工具面板")]
        public RectTransform leftPanelContainer;
        public Button toggleLeftPanelButton;
        public GameObject leftPanelContent;
        public Button toolSelectButton;
        public Button toolBrushButton;
        public Button toolEraserButton;
        public Button toolShapeButton;
        public Button toolTextButton;
        
        [Header("中间主视图区域")]
        public RectTransform mainViewContainer;
        public ImageViewer mainImageViewer;
        public RawImage mainViewImage;
        public ScrollRect mainViewScrollRect;
        
        [Header("模式切换")]
        public PrintModeManager printModeManager;
        public GameObject uvPrintViewContainer;
        public GameObject print3DViewContainer;
        public Model3DViewer model3DViewer;
        public Model3DController model3DController;
        
        [Header("右侧属性面板")]
        public RectTransform rightPanelContainer;
        public Button toggleRightPanelButton;
        public GameObject rightPanelContent;
        public Slider opacitySlider;
        public Slider brushSizeSlider;
        public Button colorPickerButton;
        public Image colorPreviewImage;
        
        [Header("底部状态栏")]
        public RectTransform statusBarContainer;
        public Text statusText;
        public Text zoomText;
        public Text positionText;
        
        [Header("面板设置")]
        public bool leftPanelVisible = true;
        public bool rightPanelVisible = true;
        public float leftPanelWidth = 250f;
        public float rightPanelWidth = 300f;
        public float menuBarHeight = 30f;
        public float toolBarHeight = 40f;
        public float statusBarHeight = 25f;
        
        private MainUIManager mainUIManager;
        private Color currentColor = Color.white;
        private ImageLoader imageLoader;
        private ModelLoader modelLoader;
        
        void Start()
        {
            // 延迟初始化，确保所有组件都已创建
            StartCoroutine(DelayedInitialize());
        }
        
        System.Collections.IEnumerator DelayedInitialize()
        {
            // 等待一帧，确保所有GameObject都已创建
            yield return null;
            
            InitializeLayout();
            InitializeButtons();
            LoadMainUIManager();
            InitializeModeManager();
            
            // 延迟初始化模式管理器按钮（确保所有组件都已创建）
            StartCoroutine(DelayedModeManagerInit());
            
            // 自动加载 test2.jpg 图像
            LoadDefaultImage();
            
            // 尝试自动加载3D模型
            StartCoroutine(TryLoadDefault3DModelCoroutine());
        }
        
        async void LoadDefaultImage()
        {
            try
            {
                // 获取 test2.jpg 的路径
                // 在 Unity 中，Assets 文件夹的路径是 Application.dataPath
                string imagePath = Path.Combine(Application.dataPath, "Images", "test2.jpg");
                
                // 如果文件不存在，尝试其他可能的路径
                if (!File.Exists(imagePath))
                {
#if UNITY_EDITOR
                    // 在 Editor 中，Application.dataPath 已经是项目根目录下的 Assets 文件夹
                    // 所以路径应该是正确的，但如果还是找不到，尝试使用 Unity 的资源加载
                    Debug.LogWarning($"[StudioUIManager] 文件不存在，尝试的路径: {imagePath}");
                    // 尝试使用 Resources 或直接加载
                    imagePath = Path.Combine(Application.dataPath, "Images", "test2.jpg");
#else
                    // 运行时，尝试从 StreamingAssets 加载
                    imagePath = Path.Combine(Application.streamingAssetsPath, "Images", "test2.jpg");
#endif
                }
                
                if (!File.Exists(imagePath))
                {
                    Debug.LogWarning($"[StudioUIManager] 未找到 test2.jpg 文件，尝试的路径: {imagePath}");
                    Debug.LogWarning($"[StudioUIManager] Application.dataPath: {Application.dataPath}");
                    return;
                }
                
                Debug.Log($"[StudioUIManager] 找到图像文件: {imagePath}");
                UpdateStatus("正在加载图像...");
                
                // 获取或创建 ImageLoader
                if (imageLoader == null)
                {
                    imageLoader = GetComponent<ImageLoader>();
                    if (imageLoader == null)
                    {
                        imageLoader = gameObject.AddComponent<ImageLoader>();
                    }
                }
                
                // 异步加载图像
                Texture2D texture = await imageLoader.LoadImageTaskAsync(imagePath);
                
                if (texture != null)
                {
                    // 显示图像
                    SetMainImage(texture);
                    UpdateStatus($"图像已加载: test2.jpg ({texture.width} × {texture.height})");
                    UpdateZoom(1.0f);
                    UpdatePosition(Vector2.zero);
                    
                    Debug.Log($"[StudioUIManager] 图像加载成功: {texture.width} × {texture.height}");
                }
                else
                {
                    Debug.LogError("[StudioUIManager] 图像加载失败");
                    UpdateStatus("图像加载失败");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIManager] 加载图像时发生错误: {e.Message}\n{e.StackTrace}");
                UpdateStatus("加载图像时发生错误");
            }
        }
        
        void InitializeLayout()
        {
            // 如果mainContainer未分配，尝试自动设置
            if (mainContainer == null)
            {
                RectTransform rect = GetComponent<RectTransform>();
                if (rect != null)
                {
                    mainContainer = rect;
                    Debug.Log("[StudioUIManager] 自动设置mainContainer");
                }
                else
                {
                    Debug.LogError("StudioUIManager: mainContainer未分配！");
                    return;
                }
            }
            
            SetupMenuBar();
            SetupToolBar();
            SetupLeftPanel();
            SetupRightPanel();
            SetupMainView();
            SetupStatusBar();
            
            UpdateLayout();
        }
        
        void SetupMenuBar()
        {
            if (menuBarContainer != null)
            {
                menuBarContainer.anchorMin = new Vector2(0, 1);
                menuBarContainer.anchorMax = new Vector2(1, 1);
                menuBarContainer.offsetMin = new Vector2(0, -menuBarHeight);
                menuBarContainer.offsetMax = new Vector2(0, 0);
            }
        }
        
        void SetupToolBar()
        {
            if (toolBarContainer != null)
            {
                toolBarContainer.anchorMin = new Vector2(0, 1);
                toolBarContainer.anchorMax = new Vector2(1, 1);
                toolBarContainer.offsetMin = new Vector2(0, -menuBarHeight - toolBarHeight);
                toolBarContainer.offsetMax = new Vector2(0, -menuBarHeight);
            }
        }
        
        void SetupLeftPanel()
        {
            if (leftPanelContainer != null)
            {
                leftPanelContainer.anchorMin = new Vector2(0, 0);
                leftPanelContainer.anchorMax = new Vector2(0, 1);
                leftPanelContainer.offsetMin = new Vector2(0, statusBarHeight);
                leftPanelContainer.offsetMax = new Vector2(leftPanelWidth, -menuBarHeight - toolBarHeight);
                
                if (leftPanelContent != null)
                {
                    leftPanelContent.SetActive(leftPanelVisible);
                }
            }
        }
        
        void SetupRightPanel()
        {
            if (rightPanelContainer != null)
            {
                rightPanelContainer.anchorMin = new Vector2(1, 0);
                rightPanelContainer.anchorMax = new Vector2(1, 1);
                rightPanelContainer.offsetMin = new Vector2(-rightPanelWidth, statusBarHeight);
                rightPanelContainer.offsetMax = new Vector2(0, -menuBarHeight - toolBarHeight);
                
                if (rightPanelContent != null)
                {
                    rightPanelContent.SetActive(rightPanelVisible);
                }
            }
        }
        
        void SetupMainView()
        {
            if (mainViewContainer != null)
            {
                float leftOffset = leftPanelVisible ? leftPanelWidth : 0;
                float rightOffset = rightPanelVisible ? rightPanelWidth : 0;
                
                mainViewContainer.anchorMin = new Vector2(0, 0);
                mainViewContainer.anchorMax = new Vector2(1, 1);
                mainViewContainer.offsetMin = new Vector2(leftOffset, statusBarHeight);
                mainViewContainer.offsetMax = new Vector2(-rightOffset, -menuBarHeight - toolBarHeight);
            }
        }
        
        void SetupStatusBar()
        {
            if (statusBarContainer != null)
            {
                statusBarContainer.anchorMin = new Vector2(0, 0);
                statusBarContainer.anchorMax = new Vector2(1, 0);
                statusBarContainer.offsetMin = new Vector2(0, 0);
                statusBarContainer.offsetMax = new Vector2(0, statusBarHeight);
            }
        }
        
        void InitializeButtons()
        {
            // 菜单栏按钮
            if (fileMenuButton != null)
                fileMenuButton.onClick.AddListener(() => OnMenuClicked("File"));
            if (editMenuButton != null)
                editMenuButton.onClick.AddListener(() => OnMenuClicked("Edit"));
            if (viewMenuButton != null)
                viewMenuButton.onClick.AddListener(() => OnMenuClicked("View"));
            if (toolsMenuButton != null)
                toolsMenuButton.onClick.AddListener(() => OnMenuClicked("Tools"));
            if (helpMenuButton != null)
                helpMenuButton.onClick.AddListener(() => OnMenuClicked("Help"));
            
            // 工具栏按钮
            if (newProjectButton != null)
                newProjectButton.onClick.AddListener(OnNewProject);
            if (openProjectButton != null)
                openProjectButton.onClick.AddListener(OnOpenProject);
            if (saveProjectButton != null)
                saveProjectButton.onClick.AddListener(OnSaveProject);
            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndo);
            if (redoButton != null)
                redoButton.onClick.AddListener(OnRedo);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);
            
            // 左侧面板切换
            if (toggleLeftPanelButton != null)
                toggleLeftPanelButton.onClick.AddListener(ToggleLeftPanel);
            
            // 右侧面板切换
            if (toggleRightPanelButton != null)
                toggleRightPanelButton.onClick.AddListener(ToggleRightPanel);
            
            // 工具按钮
            if (toolSelectButton != null)
                toolSelectButton.onClick.AddListener(() => OnToolSelected("Select"));
            if (toolBrushButton != null)
                toolBrushButton.onClick.AddListener(() => OnToolSelected("Brush"));
            if (toolEraserButton != null)
                toolEraserButton.onClick.AddListener(() => OnToolSelected("Eraser"));
            if (toolShapeButton != null)
                toolShapeButton.onClick.AddListener(() => OnToolSelected("Shape"));
            if (toolTextButton != null)
                toolTextButton.onClick.AddListener(() => OnToolSelected("Text"));
            
            // 属性面板控件
            if (opacitySlider != null)
                opacitySlider.onValueChanged.AddListener(OnOpacityChanged);
            if (brushSizeSlider != null)
                brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
            if (colorPickerButton != null)
                colorPickerButton.onClick.AddListener(OnColorPicker);
        }
        
        void LoadMainUIManager()
        {
            mainUIManager = FindObjectOfType<MainUIManager>();
            if (mainUIManager != null && mainImageViewer != null)
            {
                // 将ImageViewer连接到MainUIManager
                mainUIManager.imageViewer = mainImageViewer;
            }
        }
        
        void InitializeModeManager()
        {
            // 获取或创建ModelLoader
            if (modelLoader == null)
            {
                modelLoader = GetComponent<ModelLoader>();
                if (modelLoader == null)
                {
                    modelLoader = gameObject.AddComponent<ModelLoader>();
                }
            }
            
            // 初始化模式管理器
            if (printModeManager == null)
            {
                printModeManager = GetComponent<PrintModeManager>();
                if (printModeManager == null)
                {
                    printModeManager = gameObject.AddComponent<PrintModeManager>();
                }
            }
            
            // 设置模式管理器的视图引用
            if (printModeManager != null)
            {
                printModeManager.uvPrintView = uvPrintViewContainer;
                printModeManager.print3DView = print3DViewContainer;
                printModeManager.imageViewer = mainImageViewer;
                printModeManager.model3DViewer = model3DViewer;
                printModeManager.model3DController = model3DController;
            }
        }
        
        System.Collections.IEnumerator DelayedModeManagerInit()
        {
            // 等待几帧，确保所有UI元素都已创建
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            // 重新初始化按钮连接
            if (printModeManager != null)
            {
                printModeManager.InitializeButtons();
                Debug.Log("[StudioUIManager] 模式管理器按钮已重新初始化");
            }
        }
        
        /// <summary>
        /// 尝试自动加载3DModels文件夹下的STL文件
        /// </summary>
        public System.Collections.IEnumerator TryLoadDefault3DModelCoroutine()
        {
            Debug.Log("[StudioUIManager] 开始尝试加载3D模型...");
            
            // 等待一帧，确保模式已切换
            yield return new WaitForEndOfFrame();
            
            // 检查是否在3D打印模式
            if (printModeManager == null || !printModeManager.Is3DPrintMode())
            {
                Debug.Log("[StudioUIManager] 当前不在3D打印模式，跳过自动加载");
                yield break;
            }
            
            // 查找3DModels文件夹下的STL文件
            string modelsPath = Path.Combine(Application.dataPath, "3DModels");
            Debug.Log($"[StudioUIManager] 查找3D模型路径: {modelsPath}");
            
            if (!Directory.Exists(modelsPath))
            {
                Debug.LogWarning($"[StudioUIManager] 3DModels文件夹不存在: {modelsPath}");
                UpdateStatus("3DModels文件夹不存在，请将3D模型文件放入Assets/3DModels文件夹");
                yield break;
            }
            
            // 查找所有支持的3D模型文件（STL、OBJ、3MF）
            string[] modelFiles = null;
            try
            {
                List<string> allFiles = new List<string>();
                allFiles.AddRange(Directory.GetFiles(modelsPath, "*.stl", SearchOption.TopDirectoryOnly));
                allFiles.AddRange(Directory.GetFiles(modelsPath, "*.obj", SearchOption.TopDirectoryOnly));
                allFiles.AddRange(Directory.GetFiles(modelsPath, "*.3mf", SearchOption.TopDirectoryOnly));
                modelFiles = allFiles.ToArray();
                Debug.Log($"[StudioUIManager] 找到 {modelFiles.Length} 个3D模型文件");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUIManager] 查找3D模型文件失败: {e.Message}");
                UpdateStatus($"查找3D模型文件失败: {e.Message}");
                yield break;
            }
            
            if (modelFiles == null || modelFiles.Length == 0)
            {
                Debug.Log($"[StudioUIManager] 3DModels文件夹下没有找到3D模型文件");
                UpdateStatus("3DModels文件夹下没有找到3D模型文件，请添加.stl/.obj/.3mf文件");
                yield break;
            }
            
            // 加载第一个模型文件
            string modelPath = modelFiles[0];
            Debug.Log($"[StudioUIManager] 准备加载3D模型文件: {Path.GetFileName(modelPath)}");
            UpdateStatus($"正在加载3D模型: {Path.GetFileName(modelPath)}...");
            
            // 检查ModelLoader
            if (modelLoader == null)
            {
                Debug.LogError("[StudioUIManager] ModelLoader未初始化！");
                UpdateStatus("ModelLoader未初始化");
                yield break;
            }
            
            // 检查Model3DViewer
            if (model3DViewer == null)
            {
                Debug.LogError("[StudioUIManager] Model3DViewer未设置！");
                UpdateStatus("Model3DViewer未设置");
                yield break;
            }
            
            // 异步加载模型
            var loadTask = modelLoader.LoadModelTaskAsync(modelPath);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }
            
            GameObject model = null;
            try
            {
                model = loadTask.Result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUIManager] 加载3D模型失败: {e.Message}\n{e.StackTrace}");
                UpdateStatus($"3D模型加载失败: {e.Message}");
                yield break;
            }
            
            if (model != null)
            {
                Debug.Log($"[StudioUIManager] 3D模型加载成功，顶点数: {GetModelVertexCount(model)}");
                
                if (model3DViewer != null)
                {
                    model3DViewer.SetModel(model);
                    Debug.Log("[StudioUIManager] 模型已设置到Model3DViewer");
                    
                    if (model3DController != null)
                    {
                        model3DController.modelObject = model;
                        Debug.Log("[StudioUIManager] 模型已设置到Model3DController");
                    }
                    
                    UpdateStatus($"3D模型已加载: {Path.GetFileName(modelPath)}");
                }
                else
                {
                    Debug.LogError("[StudioUIManager] Model3DViewer为null，无法显示模型");
                    UpdateStatus("Model3DViewer未设置");
                }
            }
            else
            {
                Debug.LogError("[StudioUIManager] 模型加载返回null");
                UpdateStatus("3D模型加载失败：返回null");
            }
        }
        
        /// <summary>
        /// 获取模型的顶点数量（用于调试）
        /// </summary>
        int GetModelVertexCount(GameObject model)
        {
            int count = 0;
            MeshFilter[] meshFilters = model.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.mesh != null)
                {
                    count += mf.mesh.vertexCount;
                }
            }
            return count;
        }
        
        
        void UpdateLayout()
        {
            SetupLeftPanel();
            SetupRightPanel();
            SetupMainView();
        }
        
        public void ToggleLeftPanel()
        {
            leftPanelVisible = !leftPanelVisible;
            if (leftPanelContent != null)
            {
                leftPanelContent.SetActive(leftPanelVisible);
            }
            UpdateLayout();
        }
        
        public void ToggleRightPanel()
        {
            rightPanelVisible = !rightPanelVisible;
            if (rightPanelContent != null)
            {
                rightPanelContent.SetActive(rightPanelVisible);
            }
            UpdateLayout();
        }
        
        void OnMenuClicked(string menuName)
        {
            try
            {
                UpdateStatus($"菜单: {menuName}");
                Debug.Log($"[StudioUI] 菜单点击: {menuName}");
                // TODO: 实现菜单功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 菜单点击处理异常: {e.Message}");
            }
        }
        
        void OnNewProject()
        {
            try
            {
                UpdateStatus("新建项目");
                Debug.Log("[StudioUI] 新建项目");
                // TODO: 实现新建项目功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 新建项目异常: {e.Message}");
            }
        }
        
        void OnOpenProject()
        {
            try
            {
                UpdateStatus("打开项目");
                Debug.Log("[StudioUI] 打开项目");
                
                // 根据当前模式打开不同的文件
                if (printModeManager != null && printModeManager.Is3DPrintMode())
                {
                    // 3D打印模式：打开3D模型
                    Open3DModel();
                }
                else
                {
                    // UV打印模式：打开图片
                    if (mainUIManager != null)
                    {
                        var method = mainUIManager.GetType().GetMethod("OnLoadImageClicked", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(mainUIManager, null);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 打开项目异常: {e.Message}");
                UpdateStatus("打开项目功能暂未实现");
            }
        }
        
        /// <summary>
        /// 打开3D模型文件夹
        /// </summary>
        public void OpenModelsFolder()
        {
            try
            {
                NeXTMake.Utils.MakerWorldDownloadHelper helper = GetComponent<NeXTMake.Utils.MakerWorldDownloadHelper>();
                if (helper == null)
                {
                    helper = gameObject.AddComponent<NeXTMake.Utils.MakerWorldDownloadHelper>();
                }
                helper.OpenModelsFolder();
                UpdateStatus("已打开3D模型文件夹");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUI] 打开文件夹失败: {e.Message}");
                UpdateStatus("打开文件夹失败");
            }
        }
        
        /// <summary>
        /// 打开3D模型文件
        /// </summary>
        async void Open3DModel()
        {
            try
            {
#if UNITY_EDITOR
                // 默认打开3DModels文件夹
                string defaultPath = Path.Combine(Application.dataPath, "3DModels");
                if (!Directory.Exists(defaultPath))
                {
                    defaultPath = Application.dataPath;
                }
                
                string path = UnityEditor.EditorUtility.OpenFilePanel("选择3D模型文件 (STL/OBJ/3MF)", defaultPath, "stl,obj,3mf");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                
                // 如果文件不在Assets/3DModels文件夹，询问是否复制
                string modelsFolder = Path.Combine(Application.dataPath, "3DModels");
                if (!path.StartsWith(modelsFolder))
                {
                    bool copy = UnityEditor.EditorUtility.DisplayDialog(
                        "复制文件",
                        "是否将文件复制到 Assets/3DModels 文件夹？\n这样下次可以自动加载。",
                        "是", "否");
                    
                    if (copy)
                    {
                        NeXTMake.Utils.MakerWorldDownloadHelper helper = GetComponent<NeXTMake.Utils.MakerWorldDownloadHelper>();
                        if (helper == null)
                        {
                            helper = gameObject.AddComponent<NeXTMake.Utils.MakerWorldDownloadHelper>();
                        }
                        string newPath = helper.CopyModelFileToFolder(path);
                        if (newPath != null)
                        {
                            path = newPath;
                            Debug.Log($"[StudioUI] 文件已复制到: {path}");
                        }
                    }
                }
                
                UpdateStatus("正在加载3D模型...");
                GameObject model = await modelLoader.LoadModelTaskAsync(path);
                
                if (model != null && model3DViewer != null)
                {
                    model3DViewer.SetModel(model);
                    if (model3DController != null)
                    {
                        model3DController.modelObject = model;
                    }
                    UpdateStatus($"3D模型已加载: {Path.GetFileName(path)}");
                    Debug.Log($"[StudioUI] 3D模型加载成功: {Path.GetFileName(path)}");
                }
                else
                {
                    UpdateStatus("3D模型加载失败");
                    Debug.LogError("[StudioUI] 3D模型加载失败：返回null");
                }
#else
                // 运行时可以使用文件对话框或直接指定路径
                UpdateStatus("运行时请使用文件路径加载模型");
                Debug.LogWarning("[StudioUI] 运行时模式，请手动指定模型路径");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StudioUI] 打开3D模型异常: {e.Message}\n{e.StackTrace}");
                UpdateStatus($"打开3D模型失败: {e.Message}");
            }
        }
        
        void OnSaveProject()
        {
            try
            {
                UpdateStatus("保存项目");
                Debug.Log("[StudioUI] 保存项目");
                // TODO: 实现保存项目功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 保存项目异常: {e.Message}");
            }
        }
        
        void OnUndo()
        {
            try
            {
                UpdateStatus("撤销");
                Debug.Log("[StudioUI] 撤销");
                // TODO: 实现撤销功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 撤销异常: {e.Message}");
            }
        }
        
        void OnRedo()
        {
            try
            {
                UpdateStatus("重做");
                Debug.Log("[StudioUI] 重做");
                // TODO: 实现重做功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 重做异常: {e.Message}");
            }
        }
        
        void OnSettings()
        {
            try
            {
                UpdateStatus("设置");
                Debug.Log("[StudioUI] 设置");
                // TODO: 实现设置功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 设置异常: {e.Message}");
            }
        }
        
        void OnToolSelected(string toolName)
        {
            try
            {
                UpdateStatus($"工具: {toolName}");
                Debug.Log($"[StudioUI] 选择工具: {toolName}");
                // TODO: 实现工具选择功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 工具选择异常: {e.Message}");
            }
        }
        
        void OnOpacityChanged(float value)
        {
            try
            {
                UpdateStatus($"不透明度: {(int)(value * 100)}%");
                // TODO: 实现不透明度调整
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 不透明度调整异常: {e.Message}");
            }
        }
        
        void OnBrushSizeChanged(float value)
        {
            try
            {
                UpdateStatus($"画笔大小: {(int)value}");
                // TODO: 实现画笔大小调整
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 画笔大小调整异常: {e.Message}");
            }
        }
        
        void OnColorPicker()
        {
            try
            {
                UpdateStatus("颜色选择器");
                Debug.Log("[StudioUI] 打开颜色选择器");
                // TODO: 实现颜色选择器
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudioUI] 颜色选择器异常: {e.Message}");
            }
        }
        
        public void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
        
        public void UpdateZoom(float zoom)
        {
            if (zoomText != null)
            {
                zoomText.text = $"缩放: {(int)(zoom * 100)}%";
            }
        }
        
        public void UpdatePosition(Vector2 position)
        {
            if (positionText != null)
            {
                positionText.text = $"位置: ({position.x:F0}, {position.y:F0})";
            }
        }
        
        public void SetMainImage(Texture2D texture)
        {
            if (mainViewImage != null && texture != null)
            {
                mainViewImage.texture = texture;
                RectTransform imageRect = mainViewImage.GetComponent<RectTransform>();
                if (imageRect != null)
                {
                    // 获取主视图容器的大小
                    RectTransform mainViewRect = mainViewContainer;
                    if (mainViewRect != null)
                    {
                        // 等待一帧，确保布局已更新
                        StartCoroutine(SetImageSizeDelayed(texture, mainViewRect, imageRect));
                    }
                    else
                    {
                        // 如果无法获取视图大小，使用原始大小
                        imageRect.sizeDelta = new Vector2(texture.width, texture.height);
                    }
                }
            }
            
            if (mainImageViewer != null)
            {
                mainImageViewer.SetImage(texture);
            }
        }
        
        System.Collections.IEnumerator SetImageSizeDelayed(Texture2D texture, RectTransform viewRect, RectTransform imageRect)
        {
            // 等待一帧，确保布局已更新
            yield return null;
            
            Vector2 viewSize = viewRect.rect.size;
            
            if (viewSize.x > 0 && viewSize.y > 0)
            {
                // 计算缩放比例，使图像适应视图大小（保持宽高比）
                float scaleX = viewSize.x / texture.width;
                float scaleY = viewSize.y / texture.height;
                float scale = Mathf.Min(scaleX, scaleY, 1.0f); // 不超过原始大小，但适应视图
                
                // 设置图像大小（适应视图，但不超过原始大小）
                imageRect.sizeDelta = new Vector2(
                    texture.width * scale,
                    texture.height * scale
                );
                
                // 确保图像居中
                imageRect.anchoredPosition = Vector2.zero;
                
                Debug.Log($"[StudioUIManager] 图像大小设置为: {imageRect.sizeDelta}, 视图大小: {viewSize}, 原始大小: {texture.width} × {texture.height}, 缩放: {scale}");
            }
            else
            {
                // 如果视图大小无效，使用原始大小
                imageRect.sizeDelta = new Vector2(texture.width, texture.height);
                Debug.LogWarning($"[StudioUIManager] 视图大小无效，使用原始图像大小: {texture.width} × {texture.height}");
            }
        }
    }
}
