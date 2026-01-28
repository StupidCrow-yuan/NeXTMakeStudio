using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro;
#endif

namespace NeXTMake.UI
{
    /// <summary>
    /// 底部状态栏组件
    /// </summary>
    public class StatusBar : MonoBehaviour
    {
        [Header("状态文本")]
#if UNITY_EDITOR || UNITY_STANDALONE
        public TMPro.TextMeshProUGUI statusText;
        public TMPro.TextMeshProUGUI zoomText;
        public TMPro.TextMeshProUGUI positionText;
        public TMPro.TextMeshProUGUI sizeText;
#else
        public Text statusText;
        public Text zoomText;
        public Text positionText;
        public Text sizeText;
#endif
        
        [Header("进度条（可选）")]
        public Slider progressBar;
        public GameObject progressBarContainer;
        
        private float currentZoom = 1.0f;
        private Vector2 currentPosition = Vector2.zero;
        private Vector2 currentSize = Vector2.zero;
        
        void Start()
        {
            if (progressBarContainer != null)
            {
                progressBarContainer.SetActive(false);
            }
            
            UpdateStatus("就绪");
        }
        
        public void UpdateStatus(string message)
        {
            SetText(statusText, message);
        }
        
        public void UpdateZoom(float zoom)
        {
            currentZoom = zoom;
            SetText(zoomText, $"缩放: {(int)(zoom * 100)}%");
        }
        
        public void UpdatePosition(Vector2 position)
        {
            currentPosition = position;
            SetText(positionText, $"位置: ({position.x:F0}, {position.y:F0})");
        }
        
        public void UpdateSize(Vector2 size)
        {
            currentSize = size;
            SetText(sizeText, $"大小: {(int)size.x} × {(int)size.y}");
        }
        
        public void ShowProgress(float progress)
        {
            if (progressBarContainer != null)
            {
                progressBarContainer.SetActive(true);
            }
            
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
        }
        
        public void HideProgress()
        {
            if (progressBarContainer != null)
            {
                progressBarContainer.SetActive(false);
            }
        }
        
        void SetText(Component textComponent, string text)
        {
            if (textComponent == null) return;
            
#if UNITY_EDITOR || UNITY_STANDALONE
            // 优先尝试TextMeshPro
            TMPro.TextMeshProUGUI tmp = textComponent as TMPro.TextMeshProUGUI;
            if (tmp != null)
            {
                tmp.text = text;
                return;
            }
#endif
            // 如果不是TextMeshPro或平台不支持，使用普通Text
            Text textComp = textComponent as Text;
            if (textComp != null)
            {
                textComp.text = text;
            }
        }
    }
}
