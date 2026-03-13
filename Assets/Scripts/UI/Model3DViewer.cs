using UnityEngine;
using UnityEngine.UI;
using PocoRender.Core;

namespace PocoRender.UI
{
    /// <summary>
    /// 3D模型查看器，使用RenderTexture在UI上显示3D模型
    /// </summary>
    public class Model3DViewer : MonoBehaviour
    {
        [Header("渲染设置")]
        public RawImage targetImage;
        public Camera renderCamera;
        public int textureWidth = 1024;
        public int textureHeight = 1024;
        public LayerMask renderLayer = 1;

        [Header("模型设置")]
        public GameObject modelContainer;
        public Light sceneLight;

        private RenderTexture renderTexture;
        private GameObject currentModel;
        private Camera modelCamera;
        private Vector2 lastTargetRectSize = Vector2.zero;

        private float currentYaw = 0f; // Facing North from South
        private float currentPitch = 35f;
        private float initialFocusDistance = 15f;

        private int renderFramesRemaining = 0;
        private const int RENDER_BURST_FRAMES = 3;

        void Start()
        {
            InitializeRenderer();
        }
        
        void OnEnable()
        {
            // 当GameObject被激活时，确保渲染器已初始化
            if (renderTexture == null)
            {
                InitializeRenderer();
            }
            else if (targetImage != null && targetImage.texture != renderTexture)
            {
                // 确保RawImage使用正确的RenderTexture
                targetImage.texture = renderTexture;
            }
        }

        public void InitializeRenderer()
        {
            Canvas.ForceUpdateCanvases();

            // Calculate dynamic resolution based on target image or screen aspect ratio
            int width = textureWidth;
            int height = textureHeight;

            // USER REQ: Full preview should match the actual RawImage aspect (avoid horizontal stretching).
            // If RawImage rect isn't ready yet, fall back to Screen aspect.
            if (targetImage != null)
            {
                RectTransform rt = targetImage.GetComponent<RectTransform>();
                if (rt != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
                float rtW = (rt != null) ? rt.rect.width : 0f;
                float rtH = (rt != null) ? rt.rect.height : 0f;
                if (rtH <= 0f && rt != null)
                {
                    rtW = rt.sizeDelta.x;
                    rtH = rt.sizeDelta.y;
                }
                float aspect = (rtH > 0f) ? (rtW / rtH) : ((float)Screen.width / Screen.height);

                if (height > 0)
                {
                    width = Mathf.Max(1, Mathf.RoundToInt(height * aspect));
                }

                lastTargetRectSize = new Vector2(rtW, rtH);
            }

            // Guard against zero/tiny dimensions during layout transitions (e.g. window resize)
            if (width < 8 || height < 8)
            {
                width = Mathf.Max(width, 256);
                height = Mathf.Max(height, 256);
            }

            // Check if we can reuse existing RT
            if (renderTexture != null && renderTexture.IsCreated() && 
                renderTexture.width == width && renderTexture.height == height)
            {
                // Just ensure it's assigned to targetImage and return
                if (targetImage != null && targetImage.texture != renderTexture)
                {
                    targetImage.texture = renderTexture;
                }
                return; // Nothing changed, skip expensive rebuild
            }

            if (renderTexture != null) renderTexture.Release();
            renderTexture = new RenderTexture(width, height, 24);
            renderTexture.antiAliasing = 4;

            // Reuse existing camera if available; only create a new one on first init
            bool isReinit = (modelCamera != null);
            Vector3 savedCamPos = isReinit ? modelCamera.transform.position : Vector3.zero;
            Quaternion savedCamRot = isReinit ? modelCamera.transform.rotation : Quaternion.identity;
            float savedFOV = isReinit ? modelCamera.fieldOfView : 60f;

            if (modelCamera == null)
            {
                if (renderCamera != null)
                {
                    modelCamera = renderCamera;
                }
                else
                {
                    GameObject cameraObj = new GameObject("Model3DCamera");
                    cameraObj.transform.SetParent(transform);
                    modelCamera = cameraObj.AddComponent<Camera>();
                }
            }

            // Configure camera (reuses existing object)
            modelCamera.clearFlags = CameraClearFlags.SolidColor;
            modelCamera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f); 
            modelCamera.cullingMask = renderLayer;
            modelCamera.targetTexture = renderTexture;
            
            // Match camera aspect to RenderTexture to avoid stretching
            modelCamera.aspect = (height > 0) ? ((float)width / height) : ((float)Screen.width / Screen.height);
            
            modelCamera.orthographic = false;
            modelCamera.nearClipPlane = 0.1f; 
            modelCamera.farClipPlane = 1000f;

            // Restore camera transform on reinit so view is not lost
            if (isReinit)
            {
                modelCamera.transform.position = savedCamPos;
                modelCamera.transform.rotation = savedCamRot;
                modelCamera.fieldOfView = savedFOV;
            }
            else
            {
                modelCamera.fieldOfView = 60f;
            }
            
            // 创建模型容器
            if (modelContainer == null)
            {
                GameObject container = new GameObject("ModelContainer");
                container.transform.SetParent(transform);
                container.layer = GetLayerFromMask(renderLayer);
                modelContainer = container;
            }

            // 创建场景灯光：定向光作为主光源，确保画面清晰可见
            if (sceneLight == null)
            {
                GameObject lightObj = new GameObject("SceneLight");
                lightObj.transform.SetParent(modelContainer.transform);
                sceneLight = lightObj.AddComponent<Light>();
                sceneLight.type = LightType.Directional;
                sceneLight.color = Color.white;
                sceneLight.intensity = 1.2f; // 主光，保证图像色彩正常还原
                lightObj.transform.rotation = Quaternion.Euler(50f, 40f, 0f);
            }
            
            // 环境光：保持适中，让画面不会太暗，同时聚光扫过仍有可见明暗变化
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);

            // 设置目标图像
            if (targetImage != null)
            {
                targetImage.texture = renderTexture;
                Debug.Log($"[Model3DViewer] RenderTexture已设置到RawImage: {textureWidth}x{textureHeight}");
            }
            else
            {
                Debug.LogWarning("[Model3DViewer] targetImage未设置！");
            }
            
            // Disable automatic rendering — we render on demand via RequestRender()
            if (modelCamera != null)
            {
                modelCamera.enabled = false;
                Debug.Log("[Model3DViewer] 相机已设置为按需渲染模式");
            }

            // 调整相机位置以查看模型 (only on first init, not reinit)
            if (!isReinit)
            {
                if (currentModel == null)
                {
                    if (modelCamera != null)
                    {
                        modelCamera.transform.position = new Vector3(0, 0, -5);
                        modelCamera.transform.rotation = Quaternion.identity;
                    }
                }
                else
                {
                    FocusOnModel();
                }
            }
            
            RequestRender();
        }

        /// <summary>
        /// 设置要显示的3D模型
        /// </summary>
        public void SetModel(GameObject model)
        {
            if (currentModel != null)
            {
                Destroy(currentModel);
            }

            currentModel = model;
            if (currentModel != null)
            {
                Debug.Log($"[Model3DViewer] 设置模型: {model.name}");
                
                currentModel.transform.SetParent(modelContainer.transform);
                currentModel.transform.localPosition = Vector3.zero;
                currentModel.transform.localRotation = Quaternion.identity;
                currentModel.layer = GetLayerFromMask(renderLayer);

                // 递归设置所有子对象的层
                SetLayerRecursive(currentModel, GetLayerFromMask(renderLayer));

                // 等待一帧，确保模型已完全设置
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(FocusOnModelDelayed());
                }
                else
                {
                    // If not active, we can't start coroutine, but we can't focus either.
                    // When the object becomes active (OnEnable), it will initialize/focus.
                    Debug.Log("[Model3DViewer] Object inactive, skipping delayed focus. Will focus on enable.");
                }
            }
        }
        
        /// <summary>
        /// 延迟聚焦到模型，确保模型已完全加载
        /// </summary>
        System.Collections.IEnumerator FocusOnModelDelayed()
        {
            yield return new WaitForEndOfFrame();
            FocusOnModel();
        }

        /// <summary>
        /// 聚焦到模型
        /// </summary>
        public void FocusOnModel()
        {
            if (currentModel == null || modelCamera == null)
            {
                Debug.LogWarning("[Model3DViewer] FocusOnModel: currentModel或modelCamera为null");
                return;
            }

            Bounds bounds = GetModelBounds(currentModel);
            Debug.Log($"[Model3DViewer] 模型边界: center={bounds.center}, size={bounds.size}, min={bounds.min}, max={bounds.max}");
            
            // 验证模型是否完整
            int vertexCount = GetModelVertexCount(currentModel);
            int triangleCount = GetModelTriangleCount(currentModel);
            Debug.Log($"[Model3DViewer] 模型统计: 顶点数={vertexCount}, 三角形数={triangleCount}");
            
            if (bounds.size == Vector3.zero || vertexCount == 0)
            {
                Debug.LogWarning("[Model3DViewer] 模型边界大小为0或没有顶点，使用默认视图");
                // 使用默认视图
                modelCamera.transform.position = new Vector3(0, 0, -5);
                modelCamera.transform.rotation = Quaternion.identity;
                modelCamera.fieldOfView = 60f;
                return;
            }

            // 计算模型的最大尺寸
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float minSize = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z);
            Vector3 center = bounds.center;
            
            Debug.Log($"[Model3DViewer] 模型尺寸: max={maxSize}, min={minSize}, center={center}");
            
            // 计算合适相机距离 - USER REQ: Zoom out to match reference image (approx 1/3 of screen height)
            float baseDist = Mathf.Max(maxSize * 2.2f, 1.0f);
            initialFocusDistance = baseDist;
            if (initialFocusDistance < 5f) initialFocusDistance = 5f;
            
            Quaternion initialRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            modelCamera.transform.position = center + initialRotation * new Vector3(0, 0, -initialFocusDistance);
            modelCamera.transform.LookAt(center);

            // 调整视野
            float requiredFOV = Mathf.Atan2(maxSize * 0.85f, baseDist) * Mathf.Rad2Deg * 2f;
            modelCamera.fieldOfView = Mathf.Clamp(requiredFOV, 30f, 60f);
            
            Debug.Log($"[Model3DViewer] 相机设置完成 - 位置: {modelCamera.transform.position}, 距离: {initialFocusDistance:F2}, FOV: {modelCamera.fieldOfView:F2}");
            
            RequestRender();
        }
        
        /// <summary>
        /// 获取模型的顶点数量
        /// </summary>
        int GetModelVertexCount(GameObject model)
        {
            int count = 0;
            MeshFilter[] meshFilters = model.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    count += mf.sharedMesh.vertexCount;
                }
            }
            return count;
        }
        
        /// <summary>
        /// 获取模型的三角形数量
        /// </summary>
        int GetModelTriangleCount(GameObject model)
        {
            int count = 0;
            MeshFilter[] meshFilters = model.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    count += mf.sharedMesh.triangles.Length / 3;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取模型边界
        /// </summary>
        private Bounds GetModelBounds(GameObject model)
        {
            // First, try to calculate bounds from RectTransform (for UI elements)
            RectTransform[] rectTransforms = model.GetComponentsInChildren<RectTransform>();
            if (rectTransforms.Length > 0)
            {
                bool isFirst = true;
                Bounds bounds = new Bounds();
                
                foreach (RectTransform rt in rectTransforms)
                {
                    if (rt.gameObject.activeSelf)
                    {
                        // Calculate world space corners of the RectTransform
                        Vector3[] corners = new Vector3[4];
                        rt.GetWorldCorners(corners);
                        
                        if (isFirst)
                        {
                            bounds = new Bounds(corners[0], Vector3.zero);
                            isFirst = false;
                        }
                        
                        foreach (Vector3 corner in corners)
                        {
                            bounds.Encapsulate(corner);
                        }
                    }
                }
                
                if (bounds.size != Vector3.zero)
                {
                    Debug.Log($"[Model3DViewer] 从RectTransform计算边界: center={bounds.center}, size={bounds.size}");
                    return bounds;
                }
            }
            
            // 首先尝试使用MeshFilter计算边界（更准确）
            MeshFilter[] meshFilters = model.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length > 0)
            {
                bool isFirstMesh = true;
                Bounds bounds = new Bounds();
                
                foreach (MeshFilter mf in meshFilters)
                {
                    if (mf.sharedMesh != null)
                    {
                        Bounds meshBounds = mf.sharedMesh.bounds;
                        Matrix4x4 matrix = mf.transform.localToWorldMatrix;
                        
                        // 转换边界框的8个角点到世界空间
                        Vector3[] corners = new Vector3[8];
                        Vector3 center = meshBounds.center;
                        Vector3 extents = meshBounds.extents;
                        
                        corners[0] = matrix.MultiplyPoint3x4(center + new Vector3(-extents.x, -extents.y, -extents.z));
                        corners[1] = matrix.MultiplyPoint3x4(center + new Vector3(extents.x, -extents.y, -extents.z));
                        corners[2] = matrix.MultiplyPoint3x4(center + new Vector3(-extents.x, extents.y, -extents.z));
                        corners[3] = matrix.MultiplyPoint3x4(center + new Vector3(extents.x, extents.y, -extents.z));
                        corners[4] = matrix.MultiplyPoint3x4(center + new Vector3(-extents.x, -extents.y, extents.z));
                        corners[5] = matrix.MultiplyPoint3x4(center + new Vector3(extents.x, -extents.y, extents.z));
                        corners[6] = matrix.MultiplyPoint3x4(center + new Vector3(-extents.x, extents.y, extents.z));
                        corners[7] = matrix.MultiplyPoint3x4(center + new Vector3(extents.x, extents.y, extents.z));
                        
                        if (isFirstMesh)
                        {
                            bounds = new Bounds(corners[0], Vector3.zero);
                            isFirstMesh = false;
                        }
                        
                        foreach (Vector3 corner in corners)
                        {
                            bounds.Encapsulate(corner);
                        }
                    }
                }
                
                Debug.Log($"[Model3DViewer] 从MeshFilter计算边界: center={bounds.center}, size={bounds.size}, min={bounds.min}, max={bounds.max}");
                return bounds;
            }
            
            // 如果没有MeshFilter，使用Renderer
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[Model3DViewer] 模型没有Renderer或MeshFilter");
                return new Bounds();
            }

            Bounds rendererBounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds.size != Vector3.zero)
                {
                    rendererBounds.Encapsulate(renderer.bounds);
                }
            }
            
            Debug.Log($"[Model3DViewer] 从Renderer计算边界: center={rendererBounds.center}, size={rendererBounds.size}, min={rendererBounds.min}, max={rendererBounds.max}");
            return rendererBounds;
        }

        /// <summary>
        /// 从LayerMask获取层索引
        /// </summary>
        private int GetLayerFromMask(LayerMask mask)
        {
            int layer = 0;
            int maskValue = mask.value;
            while (maskValue > 1)
            {
                maskValue >>= 1;
                layer++;
            }
            return layer;
        }

        /// <summary>
        /// 递归设置层
        /// </summary>
        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 设置相机旋转
        /// </summary>
        public void SetCameraRotation(float deltaPitch, float deltaYaw)
        {
            if (modelCamera == null || currentModel == null) return;

            currentYaw += deltaYaw;
            currentPitch = Mathf.Clamp(currentPitch + deltaPitch, 5f, 85f); 

            Bounds bounds = GetModelBounds(currentModel);
            Vector3 center = bounds.center;

            float distance = Vector3.Distance(modelCamera.transform.position, center);
            if (distance < 1f) distance = initialFocusDistance; 

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            modelCamera.transform.position = center + rotation * new Vector3(0, 0, -distance);
            modelCamera.transform.LookAt(center);
            
            RequestRender();
        }

        /// <summary>
        /// 设置相机缩放
        /// </summary>
        public void SetCameraZoom(float zoom)
        {
            if (modelCamera == null || currentModel == null) return;

            Bounds bounds = GetModelBounds(currentModel);
            Vector3 center = bounds.center;

            // Use the stored initialFocusDistance as the base for 1.0 zoom
            float distance = initialFocusDistance / Mathf.Clamp(zoom, 0.1f, 10f);

            // Maintain orbital position during zoom
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            modelCamera.transform.position = center + rotation * new Vector3(0, 0, -distance);
            modelCamera.transform.LookAt(center);
            
            RequestRender();
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        public void ResetView()
        {
            currentYaw = 0f;
            currentPitch = 35f;
            FocusOnModel();
            RequestRender();
        }

        /// <summary>
        /// Request the camera to render for a few frames.
        /// Call this whenever the scene changes (rotation, zoom, model update, light move, etc.)
        /// </summary>
        public void RequestRender(int frames = -1)
        {
            renderFramesRemaining = Mathf.Max(renderFramesRemaining, frames > 0 ? frames : RENDER_BURST_FRAMES);
        }

        void LateUpdate()
        {
            // In the Unity editor, dragging layout splitters can resize the RawImage without
            // changing Screen.width/height. Watch the target rect directly so the mini preview
            // recenters itself in editor layout changes as well.
            if (targetImage != null)
            {
                RectTransform rt = targetImage.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 currentSize = rt.rect.size;
                    if (currentSize.x <= 0f || currentSize.y <= 0f)
                    {
                        currentSize = rt.sizeDelta;
                    }

                    if (currentSize.x > 1f && currentSize.y > 1f &&
                        (Mathf.Abs(currentSize.x - lastTargetRectSize.x) > 0.5f ||
                         Mathf.Abs(currentSize.y - lastTargetRectSize.y) > 0.5f))
                    {
                        lastTargetRectSize = currentSize;
                        InitializeRenderer();
                        if (currentModel != null)
                        {
                            FocusOnModel();
                        }
                        RequestRender(3);
                    }
                }
            }

            if (renderFramesRemaining > 0 && modelCamera != null && renderTexture != null)
            {
                modelCamera.Render();
                renderFramesRemaining--;
            }
        }
        
        void OnDestroy()
        {
            if (renderTexture != null)
            {
                if (modelCamera != null) modelCamera.targetTexture = null;
                renderTexture.Release();
                DestroyImmediate(renderTexture);
                renderTexture = null;
            }

            if (modelContainer != null)
            {
                DestroyImmediate(modelContainer);
                modelContainer = null;
            }

            if (modelCamera != null)
            {
                DestroyImmediate(modelCamera.gameObject);
                modelCamera = null;
            }
        }
    }
}


