using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NeXTMake.UI
{
    /// <summary>
    /// 3D模型控制器，处理模型的旋转、缩放、平移、切片等操作
    /// </summary>
    public class Model3DController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
    {
        [Header("控制目标")]
        public Model3DViewer modelViewer;
        public GameObject modelObject;

        [Header("旋转控制")]
        public bool enableRotation = true;
        public float rotationSpeed = 2f;
        private bool isRotating = false;
        private Vector2 lastMousePosition;

        [Header("缩放控制")]
        public bool enableZoom = true;
        public float zoomSpeed = 0.5f; // USER REQ: Better zoom increment
        public float minZoom = 0.1f;
        public float maxZoom = 10f;
        private float currentZoom = 1f;

        [Header("平移控制")]
        public bool enablePan = true;
        public float panSpeed = 0.01f;
        private bool isPanning = false;

        [Header("切片控制")]
        public bool enableSlicing = false;
        public Slider sliceSlider;
        public GameObject slicePlane;
        private float sliceHeight = 0f;
        private Material sliceMaterial;

        private bool isMouseOver = false;
        private ScrollRect parentScrollRect;

        void Start()
        {
            if (modelViewer == null)
            {
                modelViewer = FindObjectOfType<Model3DViewer>();
            }

            if (sliceSlider != null)
            {
                sliceSlider.onValueChanged.AddListener(OnSliceValueChanged);
            }

            // 创建切片材质
            CreateSliceMaterial();

            // USER REQ: Get parent scroll rect
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isMouseOver = true;
            if (parentScrollRect != null) parentScrollRect.enabled = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isMouseOver = false;
            if (parentScrollRect != null) parentScrollRect.enabled = true;
        }

        // USER REQ: Explicitly handle scroll events to block them from bubbling up to ScrollRect
        public void OnScroll(PointerEventData eventData)
        {
            if (enableZoom)
            {
                // eventData.scrollDelta.y is standard Unity scroll input
                float scroll = eventData.scrollDelta.y;
                if (Mathf.Abs(scroll) > 0.0001f)
                {
                    float factor = 1.0f + (scroll > 0 ? 0.1f : -0.1f);
                    currentZoom = Mathf.Clamp(currentZoom * factor, minZoom, maxZoom);
                    ZoomModel(currentZoom);
                }
                
                // IMPORTANT: In IScrollHandler, simply handling it prevents propagation 
                // to parent ScrollRects in most cases.
            }
        }

        void Update()
        {
            HandleInput();
        }

        void HandleInput()
        {
            // 鼠标左键旋转
            if (enableRotation)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // 只有点击在 3D 视图区域内才开始旋转
                    if (isMouseOver)
                    {
                        isRotating = true;
                        lastMousePosition = Input.mousePosition;
                    }
                }
                
                if (Input.GetMouseButtonUp(0))
                {
                    isRotating = false;
                }

                if (isRotating && Input.GetMouseButton(0))
                {
                    Vector2 currentPos = Input.mousePosition;
                    Vector2 delta = currentPos - lastMousePosition;
                    
                    if (delta.sqrMagnitude > 0.001f)
                    {
                        // 降低旋转步长，提高稳定性
                        RotateModel(delta.x * rotationSpeed * 0.1f, delta.y * rotationSpeed * 0.1f);
                        lastMousePosition = currentPos;
                    }
                }
                else
                {
                    isRotating = false;
                }
            }

            // 鼠标中键平移
            if (enablePan && Input.GetMouseButtonDown(2))
            {
                if (isMouseOver)
                {
                    isPanning = true;
                    lastMousePosition = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(2))
            {
                isPanning = false;
            }

            if (isPanning && enablePan)
            {
                Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;
                PanModel(delta.x * panSpeed, delta.y * panSpeed);
                lastMousePosition = Input.mousePosition;
            }

            // REMOVED: Global scroll wheel detection from Update to prevent interference
        }

        /// <summary>
        /// 旋转模型
        /// </summary>
        public void RotateModel(float deltaX, float deltaY)
        {
            // 修正：在预览模式下，我们通过旋转相机来“围绕桌面看”
            if (modelViewer != null)
            {
                // deltaY 控制俯仰，deltaX 控制左右旋转
                modelViewer.SetCameraRotation(-deltaY, deltaX);
            }
            else if (modelObject != null)
            {
                // 旋转物体本身（用于其他 3D 场景）
                modelObject.transform.Rotate(Vector3.up, -deltaX, Space.World);
                modelObject.transform.Rotate(Vector3.right, deltaY, Space.Self);
            }
        }

        /// <summary>
        /// 缩放模型
        /// </summary>
        public void ZoomModel(float zoom)
        {
            currentZoom = zoom;
            if (modelViewer != null)
            {
                modelViewer.SetCameraZoom(zoom);
            }
            else if (modelObject != null)
            {
                modelObject.transform.localScale = Vector3.one * zoom;
            }
        }

        /// <summary>
        /// 平移模型
        /// </summary>
        public void PanModel(float deltaX, float deltaY)
        {
            if (modelObject != null)
            {
                Vector3 position = modelObject.transform.position;
                position.x += deltaX;
                position.y += deltaY;
                modelObject.transform.position = position;
            }
        }

        /// <summary>
        /// 重置模型变换
        /// </summary>
        public void ResetTransform()
        {
            if (modelObject != null)
            {
                modelObject.transform.position = Vector3.zero;
                modelObject.transform.rotation = Quaternion.identity;
                modelObject.transform.localScale = Vector3.one;
            }

            currentZoom = 1f;
            if (modelViewer != null)
            {
                modelViewer.ResetView();
            }
        }

        /// <summary>
        /// 切片功能
        /// </summary>
        public void SetSliceHeight(float height)
        {
            sliceHeight = height;
            UpdateSlicePlane();
        }

        void OnSliceValueChanged(float value)
        {
            SetSliceHeight(value);
        }

        void UpdateSlicePlane()
        {
            if (!enableSlicing || modelObject == null) return;

            if (slicePlane == null)
            {
                // 创建切片平面
                slicePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                slicePlane.name = "SlicePlane";
                slicePlane.transform.SetParent(modelObject.transform);
                slicePlane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                // 设置切片材质
                Renderer renderer = slicePlane.GetComponent<Renderer>();
                if (renderer != null && sliceMaterial != null)
                {
                    renderer.material = sliceMaterial;
                }
            }

            // 获取模型边界
            Bounds bounds = GetModelBounds(modelObject);
            float modelHeight = bounds.size.y;
            float minY = bounds.min.y;

            // 设置切片平面位置
            slicePlane.transform.localPosition = new Vector3(0, minY + modelHeight * sliceHeight, 0);
            slicePlane.transform.localScale = new Vector3(bounds.size.x, 1, bounds.size.z) * 0.1f;

            // 应用切片着色器（需要自定义着色器实现）
            ApplySliceShader(modelObject, sliceHeight);
        }

        /// <summary>
        /// 应用切片着色器
        /// </summary>
        void ApplySliceShader(GameObject model, float height)
        {
            // 这里需要实现切片着色器逻辑
            // 可以使用Shader来实现模型切片效果
            // 或者使用Mesh操作来裁剪模型
            // TODO: 实现切片着色器
        }

        /// <summary>
        /// 获取模型边界
        /// </summary>
        Bounds GetModelBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds();

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// 创建切片材质
        /// </summary>
        void CreateSliceMaterial()
        {
            sliceMaterial = new Material(Shader.Find("Standard"));
            sliceMaterial.color = new Color(1f, 0.5f, 0f, 0.5f); // 半透明橙色
            sliceMaterial.SetFloat("_Mode", 3); // 透明模式
            sliceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sliceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sliceMaterial.SetInt("_ZWrite", 0);
            sliceMaterial.DisableKeyword("_ALPHATEST_ON");
            sliceMaterial.EnableKeyword("_ALPHABLEND_ON");
            sliceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            sliceMaterial.renderQueue = 3000;
        }

        /// <summary>
        /// 启用/禁用切片
        /// </summary>
        public void SetSlicingEnabled(bool enabled)
        {
            enableSlicing = enabled;
            if (slicePlane != null)
            {
                slicePlane.SetActive(enabled);
            }
        }
    }
}

