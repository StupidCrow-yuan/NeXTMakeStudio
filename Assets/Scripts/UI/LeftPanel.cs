using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.UI
{
    /// <summary>
    /// 左侧工具面板组件
    /// </summary>
    public class LeftPanel : MonoBehaviour
    {
        [Header("面板容器")]
        public RectTransform panelContainer;
        public Button toggleButton;
        public GameObject panelContent;
        
        [Header("工具按钮")]
        public Button toolSelectButton;
        public Button toolBrushButton;
        public Button toolEraserButton;
        public Button toolShapeButton;
        public Button toolTextButton;
        public Button toolLineButton;
        public Button toolRectangleButton;
        public Button toolCircleButton;
        
        [Header("工具组")]
        public Transform basicToolsGroup;
        public Transform shapeToolsGroup;
        
        private string currentSelectedTool = "Select";
        private Dictionary<string, Button> toolButtons = new Dictionary<string, Button>();
        
        void Start()
        {
            InitializeTools();
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(TogglePanel);
            }
        }
        
        void InitializeTools()
        {
            RegisterTool(toolSelectButton, "Select");
            RegisterTool(toolBrushButton, "Brush");
            RegisterTool(toolEraserButton, "Eraser");
            RegisterTool(toolShapeButton, "Shape");
            RegisterTool(toolTextButton, "Text");
            RegisterTool(toolLineButton, "Line");
            RegisterTool(toolRectangleButton, "Rectangle");
            RegisterTool(toolCircleButton, "Circle");
            
            SelectTool("Select");
        }
        
        void RegisterTool(Button button, string toolName)
        {
            if (button != null)
            {
                toolButtons[toolName] = button;
                button.onClick.AddListener(() => SelectTool(toolName));
            }
        }
        
        public void SelectTool(string toolName)
        {
            // 取消之前选中的工具
            if (toolButtons.ContainsKey(currentSelectedTool))
            {
                SetToolButtonState(toolButtons[currentSelectedTool], false);
            }
            
            // 选中新工具
            currentSelectedTool = toolName;
            if (toolButtons.ContainsKey(toolName))
            {
                SetToolButtonState(toolButtons[toolName], true);
            }
            
            Debug.Log($"[LeftPanel] 选择工具: {toolName}");
            // TODO: 通知其他组件工具已更改
        }
        
        void SetToolButtonState(Button button, bool selected)
        {
            if (button == null) return;
            
            ColorBlock colors = button.colors;
            if (selected)
            {
                colors.normalColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
            }
            else
            {
                colors.normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            }
            button.colors = colors;
        }
        
        public void TogglePanel()
        {
            if (panelContent != null)
            {
                bool isActive = panelContent.activeSelf;
                panelContent.SetActive(!isActive);
                
                StudioUIManager studioUI = GetComponentInParent<StudioUIManager>();
                if (studioUI != null)
                {
                    studioUI.ToggleLeftPanel();
                }
            }
        }
        
        public string GetCurrentTool()
        {
            return currentSelectedTool;
        }
    }
}
