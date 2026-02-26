using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NeXTMake.Core;
using NeXTMake.UI.Core;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.UI
{
    /// <summary>
    /// NeXTMake Studio 风格的 UI 管理器
    /// </summary>
    public class NeXTMakeStudioUIManager : MonoBehaviour
    {
        [Header("主布局容器")]
        public RectTransform mainContainer;
        public Canvas rootCanvas;

        [Header("布局组件")]
        public UVPrintStudioLayout uvPrintLayout;
        public Print3DStudioLayout print3DLayout;
        public StudioSelectionDialog selectionDialog;

        [Header("模式管理")]
        public PrintModeManager printModeManager;

        [Header("核心组件 (引用)")]
        public Model3DViewer model3DViewer;
        public Model3DController model3DController;

        // 内部状态
        private PrintMode currentMode = PrintMode.UVPrint;
        private bool isInitialized = false;

        /// <summary>
        /// 由 AutoSetup 显式调用
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            // 启动时显示选择对话框
            ShowSelectionDialog();

            // 监听窗口大小变化
            SetupWindowResizeListener();
        }

        /// <summary>
        /// 设置窗口大小变化监听器
        /// </summary>
        private void SetupWindowResizeListener()
        {
            WindowResizeHandler resizeHandler = rootCanvas.gameObject.GetComponent<WindowResizeHandler>();
            if (resizeHandler == null)
            {
                resizeHandler = rootCanvas.gameObject.AddComponent<WindowResizeHandler>();
            }

            resizeHandler.OnWindowResized += () => {
                Debug.Log("[NeXTMakeStudioUIManager] 窗口大小变化，更新布局");
                UpdateLayout();
            };
        }

        /// <summary>
        /// 更新布局以适应窗口大小变化
        /// </summary>
        private void UpdateLayout()
        {
            // 这里可以添加具体的布局更新逻辑
            // 例如，更新各个面板的大小和位置
        }

        public void ShowSelectionDialog()
        {
            Debug.Log("ShowSelectionDialog called");
            if (selectionDialog != null)
            {
                Debug.Log("SelectionDialog found, initializing");
                // 确保事件已初始化
                if (selectionDialog.OnStudioSelected == null)
                {
                    selectionDialog.OnStudioSelected = new UnityEngine.Events.UnityEvent<PrintMode>();
                    Debug.Log("OnStudioSelected event created");
                }

                // 确保 SelectionDialog 在最上层，防止被其他 Layout 遮挡
                if (selectionDialog.transform.parent != null) // Overlay
                {
                    selectionDialog.transform.parent.SetAsLastSibling();
                    Debug.Log("Set SelectionDialog parent as last sibling");
                }
                else
                {
                    selectionDialog.transform.SetAsLastSibling();
                    Debug.Log("Set SelectionDialog as last sibling");
                }

                selectionDialog.OnStudioSelected.RemoveAllListeners();
                selectionDialog.OnStudioSelected.AddListener(OnStudioSelected);
                Debug.Log("OnStudioSelected listener added");
                selectionDialog.Show();
                Debug.Log("SelectionDialog shown");
            }
            else
            {
                Debug.LogWarning("[UI] SelectionDialog not found, defaulting to UV Print.");
                OnStudioSelected(PrintMode.UVPrint);
            }
        }

        void OnStudioSelected(PrintMode mode)
        {
            Debug.Log($"OnStudioSelected called with mode: {mode}");
            currentMode = mode;
            
            if (selectionDialog != null)
            {
                selectionDialog.Hide();
                Debug.Log("SelectionDialog hidden");
            }

            // 切换布局
            ShowLayoutForMode(currentMode);
            Debug.Log($"Layout for mode {mode} shown");

            // 通知模式管理器 (如果存在)
            if (printModeManager != null)
            {
                printModeManager.SwitchMode(mode);
                Debug.Log("PrintModeManager notified of mode change");
            }
        }

        void ShowLayoutForMode(PrintMode mode)
        {
            Debug.Log($"ShowLayoutForMode called with mode: {mode}");
            if (uvPrintLayout != null)
            {
                bool uvActive = mode == PrintMode.UVPrint;
                uvPrintLayout.gameObject.SetActive(uvActive);
                Debug.Log($"UVPrintLayout set to active: {uvActive}");
            }
            else
            {
                Debug.LogWarning("uvPrintLayout is null");
            }
            if (print3DLayout != null)
            {
                bool print3DActive = mode == PrintMode.Print3D;
                print3DLayout.gameObject.SetActive(print3DActive);
                Debug.Log($"Print3DLayout set to active: {print3DActive}");
            }
            else
            {
                Debug.LogWarning("print3DLayout is null");
            }
        }

        // 占位方法：保持接口兼容
        public void ToggleLeftPanel() { }
        public void ToggleRightPanel() { }
        public void UpdateStatus(string msg) { Debug.Log($"[Status] {msg}"); }
        public PrintMode GetCurrentMode() { return currentMode; }
        public void SetCurrentMode(PrintMode mode) { OnStudioSelected(mode); }
    }
}

