using UnityEngine;
using UnityEngine.UI;

namespace NeXTMake.UI
{
    /// <summary>
    /// 顶部工具栏组件
    /// </summary>
    public class ToolBar : MonoBehaviour
    {
        [Header("工具栏按钮")]
        public Button newProjectButton;
        public Button openProjectButton;
        public Button saveProjectButton;
        public Button undoButton;
        public Button redoButton;
        public Button settingsButton;
        
        [Header("按钮图标（可选）")]
        public Sprite newProjectIcon;
        public Sprite openProjectIcon;
        public Sprite saveProjectIcon;
        public Sprite undoIcon;
        public Sprite redoIcon;
        public Sprite settingsIcon;
        
        private StudioUIManager studioUIManager;
        
        void Start()
        {
            InitializeButtons();
            studioUIManager = GetComponentInParent<StudioUIManager>();
        }
        
        void InitializeButtons()
        {
            SetupButton(newProjectButton, newProjectIcon, "新建");
            SetupButton(openProjectButton, openProjectIcon, "打开");
            SetupButton(saveProjectButton, saveProjectIcon, "保存");
            SetupButton(undoButton, undoIcon, "撤销");
            SetupButton(redoButton, redoIcon, "重做");
            SetupButton(settingsButton, settingsIcon, "设置");
        }
        
        void SetupButton(Button button, Sprite icon, string tooltip)
        {
            if (button == null) return;
            
            if (icon != null)
            {
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = icon;
                }
            }
            
            // 添加工具提示（如果需要）
            // TODO: 实现工具提示功能
        }
    }
}
