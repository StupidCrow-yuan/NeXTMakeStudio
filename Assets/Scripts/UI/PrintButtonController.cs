using UnityEngine;
using UnityEngine.UI;
using PocoRender.Core;
using PocoRender.Communication;

namespace PocoRender.UI
{
    /// <summary>
    /// Creates and manages the "Send to PocoStudio Print" floating button.
    /// Only visible when the app was launched from PocoStudio (has print service port).
    /// When clicked, exports the current canvas as a PNG and sends it to Qt.
    /// </summary>
    public class PrintButtonController : MonoBehaviour
    {
        private Button _printButton;
        private Text _statusText;
        private CanvasController _canvas;
        private bool _printServiceAvailable;

        void Start()
        {
            _printServiceAvailable = BuildMode.HasPrintService;

            if (!_printServiceAvailable)
            {
                Debug.Log("[PrintButton] No print service port — button hidden");
                gameObject.SetActive(false);
                return;
            }

            CreateUI();
            ConnectToPrintService();
        }

        private void CreateUI()
        {
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[PrintButton] No 'Canvas' found in scene");
                return;
            }

            // Floating button in the top-right corner
            var btnObj = new GameObject("PrintToPocoStudioBtn");
            btnObj.transform.SetParent(canvasObj.transform, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(220, 50);

            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f, 0.9f);

            _printButton = btnObj.AddComponent<Button>();
            _printButton.onClick.AddListener(OnPrintClicked);

            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.text = "发送到 PocoStudio 打印";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            // Status line below the button
            var statusObj = new GameObject("PrintStatus");
            statusObj.transform.SetParent(canvasObj.transform, false);
            var statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(1, 1);
            statusRect.anchoredPosition = new Vector2(-20, -75);
            statusRect.sizeDelta = new Vector2(220, 24);

            _statusText = statusObj.AddComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 12;
            _statusText.alignment = TextAnchor.MiddleRight;
            _statusText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private void ConnectToPrintService()
        {
            int port = BuildMode.PrintServicePort;
            bool ok = PrintClient.Instance.Connect("127.0.0.1", port);
            if (_statusText != null)
            {
                _statusText.text = ok
                    ? $"已连接打印服务 (:{port})"
                    : "打印服务连接失败";
                _statusText.color = ok
                    ? new Color(0.3f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }
        }

        private void OnPrintClicked()
        {
            if (!PrintClient.Instance.IsConnected)
            {
                ConnectToPrintService();
                if (!PrintClient.Instance.IsConnected)
                {
                    if (_statusText != null)
                        _statusText.text = "无法连接到 PocoStudio";
                    return;
                }
            }

            if (_canvas == null)
                _canvas = FindObjectOfType<CanvasController>();

            if (_canvas == null || _canvas.paper == null)
            {
                if (_statusText != null)
                    _statusText.text = "未找到画布";
                return;
            }

            ExportAndSend();
        }

        private void ExportAndSend()
        {
            var paper = _canvas.paper;
            int w = Mathf.RoundToInt(paper.sizeDelta.x);
            int h = Mathf.RoundToInt(paper.sizeDelta.y);

            // Export canvas to a temp PNG file
            string tempPath = System.IO.Path.Combine(
                Application.temporaryCachePath,
                "print_export_" + System.DateTime.Now.Ticks + ".png");

            bool exported = ExportCanvasToPng(tempPath, w, h);

            if (exported && System.IO.File.Exists(tempPath))
            {
                byte[] pngData = System.IO.File.ReadAllBytes(tempPath);
                bool sent = PrintClient.Instance.SendPrintRequestWithData(
                    "UnityProject", pngData, w, h, 300, 1);

                if (_statusText != null)
                    _statusText.text = sent ? "已发送打印请求 ✓" : "发送失败";

                // Clean up temp file
                try { System.IO.File.Delete(tempPath); } catch { }
            }
            else
            {
                // Fallback: send with canvas dimensions but no image data
                PrintClient.Instance.SendPrintRequest(
                    "UnityProject", "", w, h, 300, 1);
                if (_statusText != null)
                    _statusText.text = "已发送（无图像数据）";
            }
        }

        /// <summary>
        /// Capture the canvas area as a PNG using RenderTexture.
        /// </summary>
        private bool ExportCanvasToPng(string path, int width, int height)
        {
            try
            {
                var cam = Camera.main;
                if (cam == null) return false;

                var rt = new RenderTexture(width, height, 24);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;

                byte[] png = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, png);

                Destroy(tex);
                Destroy(rt);

                Debug.Log($"[PrintButton] Exported canvas to {path} ({png.Length} bytes)");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PrintButton] Export failed: {ex.Message}");
                return false;
            }
        }

        void OnDestroy()
        {
            PrintClient.Instance.Disconnect();
        }
    }
}
