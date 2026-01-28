using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.UI
{
    /// <summary>
    /// 右侧属性面板组件
    /// </summary>
    public class RightPanel : MonoBehaviour
    {
        [Header("面板容器")]
        public RectTransform panelContainer;
        public Button toggleButton;
        public GameObject panelContent;
        
        [Header("属性控件")]
        public Slider opacitySlider;
        public Slider brushSizeSlider;
        public Slider rotationSlider;
        public Slider scaleSlider;
        
        [Header("颜色选择")]
        public Button colorPickerButton;
        public Image colorPreviewImage;
        
        [Header("文本标签")]
#if UNITY_EDITOR || UNITY_STANDALONE
        public TMPro.TextMeshProUGUI opacityLabel;
        public TMPro.TextMeshProUGUI brushSizeLabel;
        public TMPro.TextMeshProUGUI rotationLabel;
        public TMPro.TextMeshProUGUI scaleLabel;
#else
        public Text opacityLabel;
        public Text brushSizeLabel;
        public Text rotationLabel;
        public Text scaleLabel;
#endif
        
        [Header("属性组")]
        public GameObject transformGroup;
        public GameObject appearanceGroup;
        public GameObject brushGroup;
        
        private Color currentColor = Color.white;
        private float currentOpacity = 1.0f;
        private float currentBrushSize = 10.0f;
        
        void Start()
        {
            InitializeControls();
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(TogglePanel);
            }
        }
        
        void InitializeControls()
        {
            // 不透明度滑块
            if (opacitySlider != null)
            {
                opacitySlider.minValue = 0f;
                opacitySlider.maxValue = 1f;
                opacitySlider.value = currentOpacity;
                opacitySlider.onValueChanged.AddListener(OnOpacityChanged);
            }
            
            // 画笔大小滑块
            if (brushSizeSlider != null)
            {
                brushSizeSlider.minValue = 1f;
                brushSizeSlider.maxValue = 100f;
                brushSizeSlider.value = currentBrushSize;
                brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
            }
            
            // 旋转滑块
            if (rotationSlider != null)
            {
                rotationSlider.minValue = 0f;
                rotationSlider.maxValue = 360f;
                rotationSlider.value = 0f;
                rotationSlider.onValueChanged.AddListener(OnRotationChanged);
            }
            
            // 缩放滑块
            if (scaleSlider != null)
            {
                scaleSlider.minValue = 0.1f;
                scaleSlider.maxValue = 5f;
                scaleSlider.value = 1f;
                scaleSlider.onValueChanged.AddListener(OnScaleChanged);
            }
            
            // 颜色选择按钮
            if (colorPickerButton != null)
            {
                colorPickerButton.onClick.AddListener(OnColorPickerClicked);
            }
            
            // 初始化颜色预览
            if (colorPreviewImage != null)
            {
                colorPreviewImage.color = currentColor;
            }
            
            UpdateLabels();
        }
        
        void OnOpacityChanged(float value)
        {
            try
            {
                currentOpacity = value;
                UpdateLabel(opacityLabel, $"不透明度: {(int)(value * 100)}%");
                Debug.Log($"[RightPanel] 不透明度已更改为: {(int)(value * 100)}%");
                // TODO: 应用不透明度到当前对象
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RightPanel] 不透明度调整异常: {e.Message}");
            }
        }
        
        void OnBrushSizeChanged(float value)
        {
            try
            {
                currentBrushSize = value;
                UpdateLabel(brushSizeLabel, $"画笔大小: {(int)value}px");
                Debug.Log($"[RightPanel] 画笔大小已更改为: {(int)value}px");
                // TODO: 应用画笔大小
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RightPanel] 画笔大小调整异常: {e.Message}");
            }
        }
        
        void OnRotationChanged(float value)
        {
            try
            {
                UpdateLabel(rotationLabel, $"旋转: {(int)value}°");
                Debug.Log($"[RightPanel] 旋转角度已更改为: {(int)value}°");
                // TODO: 应用旋转
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RightPanel] 旋转调整异常: {e.Message}");
            }
        }
        
        void OnScaleChanged(float value)
        {
            try
            {
                UpdateLabel(scaleLabel, $"缩放: {value:F2}x");
                Debug.Log($"[RightPanel] 缩放比例已更改为: {value:F2}x");
                // TODO: 应用缩放
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RightPanel] 缩放调整异常: {e.Message}");
            }
        }
        
        void OnColorPickerClicked()
        {
            try
            {
                Debug.Log("[RightPanel] 打开颜色选择器");
                // TODO: 实现颜色选择器对话框
                // 临时：使用简单的颜色选择
                ShowSimpleColorPicker();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RightPanel] 颜色选择器异常: {e.Message}");
            }
        }
        
        void ShowSimpleColorPicker()
        {
            try
            {
                // 简单的颜色选择实现（占位符）
                // TODO: 替换为更完整的颜色选择器
                Color newColor = new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    1f
                );
                SetColor(newColor);
                Debug.Log($"[RightPanel] 颜色已更改为: R={newColor.r:F2}, G={newColor.g:F2}, B={newColor.b:F2}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RightPanel] 颜色选择异常: {e.Message}");
            }
        }
        
        public void SetColor(Color color)
        {
            currentColor = color;
            if (colorPreviewImage != null)
            {
                colorPreviewImage.color = currentColor;
            }
            // TODO: 应用颜色到当前对象
        }
        
        public Color GetColor()
        {
            return currentColor;
        }
        
        public float GetOpacity()
        {
            return currentOpacity;
        }
        
        public float GetBrushSize()
        {
            return currentBrushSize;
        }
        
        void UpdateLabels()
        {
            if (opacitySlider != null)
                UpdateLabel(opacityLabel, $"不透明度: {(int)(opacitySlider.value * 100)}%");
            if (brushSizeSlider != null)
                UpdateLabel(brushSizeLabel, $"画笔大小: {(int)brushSizeSlider.value}px");
            if (rotationSlider != null)
                UpdateLabel(rotationLabel, $"旋转: {(int)rotationSlider.value}°");
            if (scaleSlider != null)
                UpdateLabel(scaleLabel, $"缩放: {scaleSlider.value:F2}x");
        }
        
        void UpdateLabel(Component label, string text)
        {
            if (label == null) return;
            
#if UNITY_EDITOR || UNITY_STANDALONE
            if (label is TMPro.TextMeshProUGUI tmp)
            {
                tmp.text = text;
            }
#else
            if (label is Text textComponent)
            {
                textComponent.text = text;
            }
#endif
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
                    studioUI.ToggleRightPanel();
                }
            }
        }
        
        public void ShowTransformGroup(bool show)
        {
            if (transformGroup != null)
                transformGroup.SetActive(show);
        }
        
        public void ShowAppearanceGroup(bool show)
        {
            if (appearanceGroup != null)
                appearanceGroup.SetActive(show);
        }
        
        public void ShowBrushGroup(bool show)
        {
            if (brushGroup != null)
                brushGroup.SetActive(show);
        }
    }
}
