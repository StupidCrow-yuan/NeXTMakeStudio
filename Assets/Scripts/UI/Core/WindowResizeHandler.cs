using UnityEngine;

namespace PocoRender.UI.Core
{
    /// <summary>
    /// 窗口大小变化处理器，用于监听窗口大小变化事件并触发布局更新
    /// </summary>
    public class WindowResizeHandler : MonoBehaviour
    {
        /// <summary>
        /// 窗口大小变化时的回调事件
        /// </summary>
        public System.Action OnWindowResized;

        private Vector2 lastWindowSize;

        private void Start()
        {
            // 初始化窗口大小
            lastWindowSize = new Vector2(Screen.width, Screen.height);
        }

        private void Update()
        {
            // 检查窗口大小是否变化
            Vector2 currentWindowSize = new Vector2(Screen.width, Screen.height);
            if (currentWindowSize != lastWindowSize)
            {
                // 窗口大小变化，触发回调
                OnWindowResized?.Invoke();
                lastWindowSize = currentWindowSize;
            }
        }
    }
}
