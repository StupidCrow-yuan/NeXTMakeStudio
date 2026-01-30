using UnityEngine;
using UnityEngine.UI;
using NeXTMake.Core;
using NeXTMake.UI;

namespace NeXTMake.UI
{
    /// <summary>
    /// 打印模式管理器，处理UV打印和3D打印模式之间的切换
    /// </summary>
    public class PrintModeManager : MonoBehaviour
    {
        [Header("模式切换")]
        public Button uvPrintModeButton;
        public Button print3DModeButton;
        public Text modeText;

        [Header("视图组件")]
        public GameObject uvPrintView;      // UV打印视图（2D图片）
        public GameObject print3DView;      // 3D打印视图（3D模型）

        [Header("UI组件")]
        public ImageViewer imageViewer;     // 2D图片查看器
        public Model3DViewer model3DViewer; // 3D模型查看器
        public Model3DController model3DController; // 3D模型控制器
        
        [Header("面板组件")]
        public LeftPanel leftPanel;          // 左侧工具面板
        public RightPanel rightPanel;        // 右侧属性面板
        public StudioUIManager studioUIManager; // Studio UI管理器

        private PrintMode currentMode = PrintMode.UVPrint;

        void Start()
        {
            // 自动查找组件引用
            if (leftPanel == null)
                leftPanel = FindObjectOfType<LeftPanel>();
            if (rightPanel == null)
                rightPanel = FindObjectOfType<RightPanel>();
            if (studioUIManager == null)
                studioUIManager = FindObjectOfType<StudioUIManager>();
            
            // 自动查找模式按钮
            if (uvPrintModeButton == null)
                uvPrintModeButton = FindObjectOfType<Button>(true);
            if (print3DModeButton == null)
                print3DModeButton = FindObjectOfType<Button>(true);
            
            InitializeButtons();
            SwitchMode(currentMode);
        }

        public void InitializeButtons()
        {
            // 先移除所有旧的监听器，避免重复添加
            if (uvPrintModeButton != null)
            {
                uvPrintModeButton.onClick.RemoveAllListeners();
                uvPrintModeButton.onClick.AddListener(() => 
                {
                    Debug.Log("[PrintModeManager] UV打印按钮被点击");
                    SwitchMode(PrintMode.UVPrint);
                });
            }
            else
            {
                Debug.LogWarning("[PrintModeManager] UV打印按钮未设置！");
            }

            if (print3DModeButton != null)
            {
                print3DModeButton.onClick.RemoveAllListeners();
                print3DModeButton.onClick.AddListener(() => 
                {
                    Debug.Log("[PrintModeManager] 3D打印按钮被点击");
                    SwitchMode(PrintMode.Print3D);
                });
            }
            else
            {
                Debug.LogWarning("[PrintModeManager] 3D打印按钮未设置！");
            }
        }

        /// <summary>
        /// 切换打印模式
        /// </summary>
        public void SwitchMode(PrintMode mode)
        {
            currentMode = mode;

            // 切换视图显示
            if (uvPrintView != null)
            {
                uvPrintView.SetActive(mode == PrintMode.UVPrint);
            }

            if (print3DView != null)
            {
                print3DView.SetActive(mode == PrintMode.Print3D);
                
                // 当切换到3D模式时，确保Model3DViewer已初始化
                if (mode == PrintMode.Print3D)
                {
                    StartCoroutine(Initialize3DViewDelayed());
                }
            }

            // 更新左侧面板工具
            UpdateLeftPanelTools(mode);

            // 更新右侧面板属性
            UpdateRightPanelProperties(mode);

            // 更新按钮状态
            UpdateButtonStates();

            // 更新模式文本
            if (modeText != null)
            {
                modeText.text = mode == PrintMode.UVPrint ? "UV打印模式" : "3D打印模式";
            }

            Debug.Log($"[PrintModeManager] 切换到模式: {mode}");
        }
        
        /// <summary>
        /// 延迟初始化3D视图，确保所有组件都已激活
        /// </summary>
        System.Collections.IEnumerator Initialize3DViewDelayed()
        {
            yield return new WaitForEndOfFrame();
            
            if (model3DViewer != null)
            {
                // 确保Model3DViewer已初始化
                if (model3DViewer.targetImage != null && model3DViewer.targetImage.texture == null)
                {
                    Debug.Log("[PrintModeManager] 重新初始化Model3DViewer");
                    // 触发OnEnable来重新初始化
                    model3DViewer.gameObject.SetActive(false);
                    yield return null;
                    model3DViewer.gameObject.SetActive(true);
                }
                
                Debug.Log($"[PrintModeManager] Model3DViewer状态 - targetImage: {model3DViewer.targetImage != null}, texture: {model3DViewer.targetImage?.texture != null}");
            }
            else
            {
                Debug.LogWarning("[PrintModeManager] model3DViewer未设置！");
            }
            
            // 尝试加载3D模型
            if (studioUIManager != null)
            {
                Debug.Log("[PrintModeManager] 尝试加载3D模型...");
                studioUIManager.StartCoroutine(studioUIManager.TryLoadDefault3DModelCoroutine());
            }
        }
        
        /// <summary>
        /// 更新左侧面板工具（根据模式显示不同的工具）
        /// </summary>
        void UpdateLeftPanelTools(PrintMode mode)
        {
            if (leftPanel == null) return;
            
            // UV打印模式：显示2D编辑工具（选择、画笔、橡皮擦、形状、文本）
            // 3D打印模式：显示3D操作工具（旋转、缩放、平移、切片等）
            // 注意：当前LeftPanel已经包含了基础工具，这里可以根据需要添加3D专用工具
            
            // 可以在这里动态显示/隐藏不同的工具组
            // 例如：在3D模式下显示切片控制等
        }
        
        /// <summary>
        /// 更新右侧面板属性（根据模式显示不同的属性）
        /// </summary>
        void UpdateRightPanelProperties(PrintMode mode)
        {
            if (rightPanel == null) return;
            
            // UV打印模式：显示图片属性（不透明度、画笔大小、颜色等）
            // 3D打印模式：显示3D模型属性（位置、旋转、缩放、切片高度等）
            
            if (mode == PrintMode.UVPrint)
            {
                // 显示2D编辑相关属性
                if (rightPanel.brushGroup != null)
                    rightPanel.brushGroup.SetActive(true);
                if (rightPanel.appearanceGroup != null)
                    rightPanel.appearanceGroup.SetActive(true);
            }
            else if (mode == PrintMode.Print3D)
            {
                // 显示3D模型相关属性
                if (rightPanel.transformGroup != null)
                    rightPanel.transformGroup.SetActive(true);
                // 可以添加3D专用属性组，如切片控制等
            }
        }

        void UpdateButtonStates()
        {
            if (uvPrintModeButton != null)
            {
                // 可以添加按钮高亮效果
                var colors = uvPrintModeButton.colors;
                colors.normalColor = currentMode == PrintMode.UVPrint ? Color.cyan : Color.white;
                uvPrintModeButton.colors = colors;
            }

            if (print3DModeButton != null)
            {
                var colors = print3DModeButton.colors;
                colors.normalColor = currentMode == PrintMode.Print3D ? Color.cyan : Color.white;
                print3DModeButton.colors = colors;
            }
        }

        /// <summary>
        /// 获取当前模式
        /// </summary>
        public PrintMode GetCurrentMode()
        {
            return currentMode;
        }

        /// <summary>
        /// 检查是否在3D打印模式
        /// </summary>
        public bool Is3DPrintMode()
        {
            return currentMode == PrintMode.Print3D;
        }

        /// <summary>
        /// 检查是否在UV打印模式
        /// </summary>
        public bool IsUVPrintMode()
        {
            return currentMode == PrintMode.UVPrint;
        }
    }
}

