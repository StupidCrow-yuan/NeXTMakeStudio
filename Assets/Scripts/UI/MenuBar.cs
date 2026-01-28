using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.UI
{
    /// <summary>
    /// 顶部菜单栏组件
    /// </summary>
    public class MenuBar : MonoBehaviour
    {
        [Header("菜单项")]
        public Button fileMenuButton;
        public Button editMenuButton;
        public Button viewMenuButton;
        public Button toolsMenuButton;
        public Button helpMenuButton;
        
        [Header("下拉菜单预制件")]
        public GameObject dropdownMenuPrefab;
        
        private Dictionary<string, GameObject> activeMenus = new Dictionary<string, GameObject>();
        private Transform canvasTransform;
        
        void Start()
        {
            InitializeMenus();
            canvasTransform = GetComponentInParent<Canvas>()?.transform;
        }
        
        void InitializeMenus()
        {
            if (fileMenuButton != null)
                fileMenuButton.onClick.AddListener(() => ToggleMenu("File"));
            if (editMenuButton != null)
                editMenuButton.onClick.AddListener(() => ToggleMenu("Edit"));
            if (viewMenuButton != null)
                viewMenuButton.onClick.AddListener(() => ToggleMenu("View"));
            if (toolsMenuButton != null)
                toolsMenuButton.onClick.AddListener(() => ToggleMenu("Tools"));
            if (helpMenuButton != null)
                helpMenuButton.onClick.AddListener(() => ToggleMenu("Help"));
        }
        
        void ToggleMenu(string menuName)
        {
            try
            {
                if (activeMenus.ContainsKey(menuName) && activeMenus[menuName] != null)
                {
                    CloseMenu(menuName);
                }
                else
                {
                    // 简化：只显示日志，不创建下拉菜单（避免复杂UI创建）
                    Debug.Log($"[MenuBar] 菜单点击: {menuName}");
                    CloseAllMenus();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MenuBar] 菜单切换异常: {e.Message}");
            }
        }
        
        void OpenMenu(string menuName)
        {
            // 关闭其他菜单
            CloseAllMenus();
            
            // 创建菜单项
            GameObject menu = CreateMenuItems(menuName);
            if (menu != null)
            {
                activeMenus[menuName] = menu;
            }
        }
        
        void CloseMenu(string menuName)
        {
            if (activeMenus.ContainsKey(menuName))
            {
                if (activeMenus[menuName] != null)
                {
                    Destroy(activeMenus[menuName]);
                }
                activeMenus.Remove(menuName);
            }
        }
        
        void CloseAllMenus()
        {
            foreach (var menu in activeMenus.Values)
            {
                if (menu != null)
                {
                    Destroy(menu);
                }
            }
            activeMenus.Clear();
        }
        
        GameObject CreateMenuItems(string menuName)
        {
            if (canvasTransform == null) return null;
            
            GameObject menuContainer = new GameObject($"{menuName}Menu");
            menuContainer.transform.SetParent(canvasTransform, false);
            
            RectTransform rectTransform = menuContainer.AddComponent<RectTransform>();
            Image bg = menuContainer.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            
            VerticalLayoutGroup layout = menuContainer.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.spacing = 2f;
            layout.padding = new RectOffset(2, 2, 2, 2);
            
            ContentSizeFitter fitter = menuContainer.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // 根据菜单名称创建不同的菜单项
            string[] menuItems = GetMenuItems(menuName);
            foreach (string item in menuItems)
            {
                CreateMenuItem(menuContainer.transform, item, menuName);
            }
            
            // 设置菜单位置（在按钮下方）
            Button button = GetButtonForMenu(menuName);
            if (button != null)
            {
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                rectTransform.position = new Vector3(
                    buttonRect.position.x,
                    buttonRect.position.y - buttonRect.rect.height,
                    0
                );
            }
            
            return menuContainer;
        }
        
        void CreateMenuItem(Transform parent, string itemName, string menuName)
        {
            GameObject item = new GameObject($"MenuItem_{itemName}");
            item.transform.SetParent(parent, false);
            
            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 25);
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            
            Button button = item.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.colors = colors;
            
            string action = $"{menuName}_{itemName}";
            button.onClick.AddListener(() => OnMenuItemClicked(action));
            
#if UNITY_EDITOR || UNITY_STANDALONE
            TMPro.TextMeshProUGUI text = item.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = itemName;
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TMPro.TextAlignmentOptions.Left;
#else
            Text text = item.AddComponent<Text>();
            text.text = itemName;
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
#endif
        }
        
        string[] GetMenuItems(string menuName)
        {
            switch (menuName)
            {
                case "File":
                    return new string[] { "新建", "打开", "保存", "另存为", "退出" };
                case "Edit":
                    return new string[] { "撤销", "重做", "复制", "粘贴", "删除" };
                case "View":
                    return new string[] { "缩放", "全屏", "显示网格", "显示标尺" };
                case "Tools":
                    return new string[] { "选择工具", "画笔工具", "橡皮擦", "形状工具" };
                case "Help":
                    return new string[] { "帮助", "关于", "快捷键" };
                default:
                    return new string[0];
            }
        }
        
        Button GetButtonForMenu(string menuName)
        {
            switch (menuName)
            {
                case "File": return fileMenuButton;
                case "Edit": return editMenuButton;
                case "View": return viewMenuButton;
                case "Tools": return toolsMenuButton;
                case "Help": return helpMenuButton;
                default: return null;
            }
        }
        
        void OnMenuItemClicked(string action)
        {
            try
            {
                Debug.Log($"[MenuBar] 菜单项点击: {action}");
                CloseAllMenus();
                
                // 默认占位符：显示操作提示
                string[] parts = action.Split('_');
                if (parts.Length >= 2)
                {
                    string menuName = parts[0];
                    string itemName = parts[1];
                    Debug.Log($"[MenuBar] 执行操作: {menuName} -> {itemName}");
                }
                
                // TODO: 实现具体的菜单项功能
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MenuBar] 菜单项点击处理异常: {e.Message}");
            }
        }
        
        void Update()
        {
            // 点击外部关闭菜单
            if (Input.GetMouseButtonDown(0))
            {
                bool clickedOnMenu = false;
                foreach (var menu in activeMenus.Values)
                {
                    if (menu != null && RectTransformUtility.RectangleContainsScreenPoint(
                        menu.GetComponent<RectTransform>(), Input.mousePosition))
                    {
                        clickedOnMenu = true;
                        break;
                    }
                }
                
                if (!clickedOnMenu)
                {
                    CloseAllMenus();
                }
            }
        }
    }
}
