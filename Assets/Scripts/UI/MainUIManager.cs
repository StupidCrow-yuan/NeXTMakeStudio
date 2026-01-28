using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;
using NeXTMake.Core;
using NeXTMake.Communication;

#if UNITY_EDITOR || UNITY_STANDALONE
using TMPro; // TextMeshPro �����ռ�
#endif

namespace NeXTMake.UI
{
    public class MainUIManager : MonoBehaviour
    {
        [Header("UI References")]
        public Button loadImageButton;
        public Button processButton;
        public Image displayImage;

        // �޸���������㴴������ Text ���� TextMeshPro ѡ��
#if UNITY_EDITOR || UNITY_STANDALONE
        public TMPro.TextMeshProUGUI statusText; // ����� TextMeshPro
#else
        public Text statusText; // �������ͨ Text
#endif

        public GameObject menuPanel;
        public Slider progressBar;

        [Header("Image Viewer")]
        // �޸����ImageViewer ��Ҫ����������� GameObject
        public ImageViewer imageViewer;

        [Header("Studio UI Integration")]
        // Studio UI管理器（可选，如果存在则使用Studio界面）
        public StudioUIManager studioUIManager;
        public StatusBar statusBar;

        private Texture2D loadedTexture;
        private CppBridge cppBridge;
        private ImageLoader imageLoader;
        private bool isProcessing = false;

        void Start()
        {
            InitializeUI();
            InitializeComponents();
            
            // 检查并自动创建Studio UI（如果不存在）
            EnsureStudioUIExists();
            
            // 如果Studio UI管理器存在，建立连接
            if (studioUIManager == null)
            {
                studioUIManager = FindObjectOfType<StudioUIManager>();
            }
            
            if (statusBar == null)
            {
                statusBar = FindObjectOfType<StatusBar>();
            }
        }
        
        void EnsureStudioUIExists()
        {
            // 如果Studio UI已存在，直接返回
            if (FindObjectOfType<StudioUIManager>() != null)
            {
                return;
            }
            
            // 使用StudioUIAutoSetup自动创建
            GameObject autoSetupObj = new GameObject("StudioUIAutoSetup");
            StudioUIAutoSetup autoSetup = autoSetupObj.AddComponent<StudioUIAutoSetup>();
            autoSetup.autoCreateOnStart = true;
            autoSetup.hideOldUI = true; // 删除旧UI
        }

        void InitializeUI()
        {
            // �󶨰�ť�¼�
            if (loadImageButton != null)
            {
                loadImageButton.onClick.AddListener(OnLoadImageClicked);
                loadImageButton.interactable = true;
            }

            if (processButton != null)
            {
                processButton.onClick.AddListener(OnProcessClicked);
                processButton.interactable = false;
            }

            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(false);
            }

            UpdateStatus("Ready");
        }

        async void InitializeComponents()
        {
            imageLoader = GetComponent<ImageLoader>();
            if (imageLoader == null)
            {
                imageLoader = gameObject.AddComponent<ImageLoader>();
            }

            cppBridge = GetComponent<CppBridge>();
            if (cppBridge == null)
            {
                cppBridge = gameObject.AddComponent<CppBridge>();
            }

            // C++ Bridge初始化（异步，不阻塞UI）
            UpdateStatus("正在初始化...");
            try
            {
                bool initialized = await cppBridge.InitializeAsync();

                if (initialized)
                {
                    UpdateStatus("就绪");
                }
                else
                {
                    UpdateStatus("就绪（C++ Bridge将在需要时初始化）");
                }
            }
            catch (System.Exception e)
            {
                // C++ Bridge初始化失败不影响UI使用
                Debug.LogWarning("C++ Bridge初始化失败，UI功能仍可使用: " + e.Message);
                UpdateStatus("就绪");
            }
        }

        async void OnLoadImageClicked()
        {
            if (loadImageButton != null)
                loadImageButton.interactable = false;

            try
            {
                string imagePath = await GetImagePathAsync();

                if (!string.IsNullOrEmpty(imagePath))
                {
                    await LoadImageFromPathAsync(imagePath);
                }
            }
            finally
            {
                if (loadImageButton != null)
                    loadImageButton.interactable = true;
            }
        }

        async Task<string> GetImagePathAsync()
        {
#if UNITY_EDITOR
            // UnityEditor.EditorUtility.OpenFilePanel必须在主线程调用
            // 直接在主线程调用，不使用Task.Run
            await Task.Yield(); // 确保在Unity主线程
            return UnityEditor.EditorUtility.OpenFilePanel(
                "Select Image", 
                "", 
                "png,jpg,jpeg,bmp"
            );
#else
            string defaultPath = Path.Combine(Application.persistentDataPath, "TestImage.png");
            return File.Exists(defaultPath) ? defaultPath : null;
#endif
        }

        async Task LoadImageFromPathAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                UpdateStatus("File not found: " + imagePath);
                return;
            }

            UpdateStatus("Loading image...");

            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.value = 0f;
            }

            try
            {
                Texture2D texture = await imageLoader.LoadImageTaskAsync(imagePath);

                if (texture != null)
                {
                    loadedTexture = texture;
                    DisplayImage(texture);
                    UpdateStatus("Image loaded: " + Path.GetFileName(imagePath));
                    
                    // 更新Studio界面
                    if (studioUIManager != null)
                    {
                        studioUIManager.SetMainImage(texture);
                        studioUIManager.UpdateStatus("图像已加载: " + Path.GetFileName(imagePath));
                        studioUIManager.UpdateZoom(1.0f);
                        studioUIManager.UpdatePosition(Vector2.zero);
                    }
                    
                    // 更新状态栏
                    if (statusBar != null)
                    {
                        statusBar.UpdateStatus("图像已加载: " + Path.GetFileName(imagePath));
                        statusBar.UpdateSize(new Vector2(texture.width, texture.height));
                    }

                    if (processButton != null)
                        processButton.interactable = true;

                    _ = cppBridge.OnImageLoadedAsync(imagePath);
                }
                else
                {
                    UpdateStatus("Failed to load image");
                }
            }
            catch (System.Exception e)
            {
                UpdateStatus("Error: " + e.Message);
                Debug.LogError("Error loading image: " + e);
            }
            finally
            {
                if (progressBar != null)
                {
                    progressBar.gameObject.SetActive(false);
                }
            }
        }

        async void OnProcessClicked()
        {
            if (loadedTexture == null)
            {
                UpdateStatus("Please load an image first");
                return;
            }

            if (isProcessing)
            {
                UpdateStatus("Already processing...");
                return;
            }

            if (processButton != null)
                processButton.interactable = false;

            isProcessing = true;
            UpdateStatus("Processing...");

            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.value = 0.5f;
            }

            try
            {
                ProcessResult result = await cppBridge.ProcessImageAsync(loadedTexture);

                if (result.success)
                {
                    UpdateStatus("Processing completed successfully");
                }
                else
                {
                    UpdateStatus("Processing failed: " + result.error);
                }
            }
            catch (System.Exception e)
            {
                UpdateStatus("Error: " + e.Message);
                Debug.LogError("Error processing image: " + e);
            }
            finally
            {
                isProcessing = false;

                if (processButton != null)
                    processButton.interactable = true;

                if (progressBar != null)
                {
                    progressBar.gameObject.SetActive(false);
                }
            }
        }

        void DisplayImage(Texture2D texture)
        {
            if (displayImage != null)
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                displayImage.sprite = sprite;
                displayImage.preserveAspect = true;
            }

            if (imageViewer != null)
            {
                imageViewer.SetImage(texture);
            }
        }

        void UpdateStatus(string message)
        {
            if (statusText != null)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                // TextMeshPro
                statusText.text = message;
#else
                // ��ͨ Text
                statusText.text = message;
#endif
            }
            
            // 同步更新Studio界面和状态栏
            if (studioUIManager != null)
            {
                studioUIManager.UpdateStatus(message);
            }
            
            if (statusBar != null)
            {
                statusBar.UpdateStatus(message);
            }
            
            Debug.Log("[MainUI] " + message);
        }

        void OnDestroy()
        {
            if (cppBridge != null)
            {
                cppBridge.Cleanup();
            }
        }
    }
}