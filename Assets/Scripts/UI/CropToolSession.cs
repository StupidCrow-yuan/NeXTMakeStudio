using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PocoRender.UI.TextureEffects;

namespace PocoRender.UI
{
    public enum CropPresetType
    {
        Free,
        Ratio1x1,
        Ratio9x16,
        Ratio16x9,
        Ellipse,
        Triangle,
        Star,
        Heart
    }

    internal enum CropShapeType
    {
        Rect,
        Ellipse,
        Triangle,
        Star,
        Heart
    }

    public class CropToolSession : MonoBehaviour
    {
        public CanvasController controller;
        public GameObject targetObject;
        public RectTransform targetRect;
        public Image targetImage;
        public RectTransform frameRect;
        public RectTransform cropImageRect;

        private Image dimOverlayGraphic;
        private Image outerBorderGraphic;
        private Image shapeOutlineGraphic;
        private Image cropImage;
        private Texture2D sourceTexture;
        private Sprite sourceSprite;
        private CropPresetType currentPreset = CropPresetType.Free;
        private CropShapeType currentShape = CropShapeType.Rect;
        private float fixedAspect;
        private ObjectManipulator manipulator;
        private Sprite cachedShapeOutline;
        private CropShapeType cachedOutlineShape = (CropShapeType)(-1);

        private const float MinFrameSize = 40f;
        private const float HandleHitSize = 28f;
        private const float HandleVisualSize = 10f;

        public static CropToolSession Create(CanvasController controller, GameObject targetObject)
        {
            GameObject root = new GameObject("CropToolSession", typeof(RectTransform), typeof(CropToolSession), typeof(SelectionAdornment));
            root.transform.SetParent(targetObject.transform, false);

            CropToolSession session = root.GetComponent<CropToolSession>();
            session.controller = controller;
            session.targetObject = targetObject;
            session.Initialize();
            return session;
        }

        public bool IsFor(GameObject obj)
        {
            return targetObject == obj;
        }

        public float FixedAspect => fixedAspect;

        private void Initialize()
        {
            targetRect = targetObject.GetComponent<RectTransform>();
            targetImage = targetObject.GetComponent<Image>();
            manipulator = targetObject.GetComponent<ObjectManipulator>();
            RectTransform rootRt = GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            sourceTexture = SpriteTextureUtil.ExtractSpriteTexture(targetImage != null ? targetImage.sprite : null, 0);
            if (sourceTexture == null || targetImage == null)
                return;

            sourceSprite = Sprite.Create(
                sourceTexture,
                new Rect(0, 0, sourceTexture.width, sourceTexture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);

            if (manipulator != null) manipulator.enabled = false;
            targetImage.enabled = false;

            CreateFrame();
            SetPreset(CropPresetType.Free);
        }

        public void SetPreset(CropPresetType preset)
        {
            currentPreset = preset;
            currentShape = CropShapeType.Rect;
            fixedAspect = 0f;

            switch (preset)
            {
                case CropPresetType.Ratio1x1:
                    fixedAspect = 1f;
                    break;
                case CropPresetType.Ratio9x16:
                    fixedAspect = 9f / 16f;
                    break;
                case CropPresetType.Ratio16x9:
                    fixedAspect = 16f / 9f;
                    break;
                case CropPresetType.Ellipse:
                    currentShape = CropShapeType.Ellipse;
                    break;
                case CropPresetType.Triangle:
                    currentShape = CropShapeType.Triangle;
                    break;
                case CropPresetType.Star:
                    currentShape = CropShapeType.Star;
                    fixedAspect = 1f;
                    break;
                case CropPresetType.Heart:
                    currentShape = CropShapeType.Heart;
                    fixedAspect = 1f;
                    break;
            }

            FitFrameToPreset();
            RefreshMaskAndPreview();
        }

        public void CancelCrop()
        {
            RestoreTarget();
            Destroy(gameObject);
        }

        public void ApplyCrop()
        {
            if (targetImage == null || frameRect == null || cropImageRect == null) return;

            Texture2D cropped = RenderMaskedCrop();
            if (cropped == null) return;

            Sprite oldSprite = targetImage.sprite;
            Vector2 oldSize = targetRect.sizeDelta;

            Sprite newSprite = Sprite.Create(
                cropped,
                new Rect(0, 0, cropped.width, cropped.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            Vector2 newSize = frameRect.sizeDelta;

            var cmd = new CropCommand(
                targetImage, targetRect,
                oldSprite, newSprite,
                oldSize, newSize,
                () => { controller?.UpdatePositionInfo(); controller?.OnObjectMoved(); });

            targetImage.sprite = newSprite;
            targetImage.preserveAspect = true;
            targetImage.color = Color.white;
            targetRect.sizeDelta = newSize;

            controller?.RecordCrop(cmd);

            RestoreTarget();
            controller?.UpdatePositionInfo();
            controller?.OnObjectMoved();
            Destroy(gameObject);
        }

        public void ResizeFrame(Vector2 size, Vector2 center)
        {
            Vector2 maxSize = targetRect.rect.size;
            size.x = Mathf.Clamp(size.x, MinFrameSize, Mathf.Max(MinFrameSize, maxSize.x));
            size.y = Mathf.Clamp(size.y, MinFrameSize, Mathf.Max(MinFrameSize, maxSize.y));

            if (fixedAspect > 0.0001f)
            {
                float widthFromHeight = size.y * fixedAspect;
                float heightFromWidth = size.x / fixedAspect;
                if (Mathf.Abs(widthFromHeight - size.x) < Mathf.Abs(heightFromWidth - size.y))
                    size.x = widthFromHeight;
                else
                    size.y = heightFromWidth;

                size.x = Mathf.Clamp(size.x, MinFrameSize, Mathf.Max(MinFrameSize, maxSize.x));
                size.y = Mathf.Clamp(size.y, MinFrameSize, Mathf.Max(MinFrameSize, maxSize.y));
            }

            frameRect.sizeDelta = size;
            frameRect.anchoredPosition = ClampFrameCenter(center, size);
            RefreshMaskAndPreview();
        }

        public void MoveUnderlyingImage(Vector2 delta)
        {
            cropImageRect.anchoredPosition += delta;
        }

        private void RestoreTarget()
        {
            if (targetImage != null) targetImage.enabled = true;
            if (manipulator != null) manipulator.enabled = true;
        }

        private void CreateFrame()
        {
            GameObject fullImageObj = new GameObject("FullImage", typeof(RectTransform), typeof(Image), typeof(CropImageDragHandler), typeof(SelectionAdornment));
            fullImageObj.transform.SetParent(transform, false);
            cropImageRect = fullImageObj.GetComponent<RectTransform>();
            cropImageRect.anchorMin = new Vector2(0.5f, 0.5f);
            cropImageRect.anchorMax = new Vector2(0.5f, 0.5f);
            cropImageRect.pivot = new Vector2(0.5f, 0.5f);
            cropImageRect.sizeDelta = GetFittedSize(targetRect.rect.size, sourceTexture.width, sourceTexture.height);
            cropImageRect.anchoredPosition = Vector2.zero;

            cropImage = fullImageObj.GetComponent<Image>();
            cropImage.sprite = sourceSprite;
            cropImage.color = Color.white;
            cropImage.preserveAspect = true;

            CropImageDragHandler dragHandler = fullImageObj.GetComponent<CropImageDragHandler>();
            dragHandler.session = this;

            GameObject overlayObj = new GameObject("DimOverlay", typeof(RectTransform), typeof(Image), typeof(SelectionAdornment));
            overlayObj.transform.SetParent(transform, false);
            RectTransform overlayRt = overlayObj.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            dimOverlayGraphic = overlayObj.GetComponent<Image>();
            dimOverlayGraphic.raycastTarget = false;

            GameObject frame = new GameObject("CropFrame", typeof(RectTransform));
            frame.transform.SetParent(transform, false);
            frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = targetRect.rect.size * 0.84f;
            frameRect.anchoredPosition = Vector2.zero;

            GameObject outerBorderObj = new GameObject("OuterBorder", typeof(RectTransform), typeof(Image), typeof(SelectionAdornment));
            outerBorderObj.transform.SetParent(frame.transform, false);
            RectTransform outerBorderRt = outerBorderObj.GetComponent<RectTransform>();
            outerBorderRt.anchorMin = Vector2.zero;
            outerBorderRt.anchorMax = Vector2.one;
            outerBorderRt.offsetMin = Vector2.zero;
            outerBorderRt.offsetMax = Vector2.zero;
            outerBorderGraphic = outerBorderObj.GetComponent<Image>();
            outerBorderGraphic.sprite = BuildRectBorderSprite();
            outerBorderGraphic.type = Image.Type.Sliced;
            outerBorderGraphic.color = new Color(0.31f, 0.86f, 0.45f, 0.78f);
            outerBorderGraphic.raycastTarget = false;

            GameObject shapeOutlineObj = new GameObject("ShapeOutline", typeof(RectTransform), typeof(Image), typeof(SelectionAdornment));
            shapeOutlineObj.transform.SetParent(frame.transform, false);
            RectTransform shapeOutlineRt = shapeOutlineObj.GetComponent<RectTransform>();
            shapeOutlineRt.anchorMin = Vector2.zero;
            shapeOutlineRt.anchorMax = Vector2.one;
            shapeOutlineRt.offsetMin = Vector2.zero;
            shapeOutlineRt.offsetMax = Vector2.zero;
            shapeOutlineGraphic = shapeOutlineObj.GetComponent<Image>();
            shapeOutlineGraphic.raycastTarget = false;

            CreateEdgeHandle("TopEdge", new Vector2(0.5f, 1f), 0, 1);
            CreateEdgeHandle("BottomEdge", new Vector2(0.5f, 0f), 0, -1);
            CreateEdgeHandle("LeftEdge", new Vector2(0f, 0.5f), -1, 0);
            CreateEdgeHandle("RightEdge", new Vector2(1f, 0.5f), 1, 0);

            CreateResizeHandle("TopLeft", new Vector2(0f, 1f), new Vector2(-1f, 1f));
            CreateResizeHandle("TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f));
            CreateResizeHandle("BottomLeft", new Vector2(0f, 0f), new Vector2(-1f, -1f));
            CreateResizeHandle("BottomRight", new Vector2(1f, 0f), new Vector2(1f, -1f));
        }

        private void CreateResizeHandle(string name, Vector2 anchor, Vector2 signs)
        {
            GameObject handle = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CropFrameResizeHandle), typeof(SelectionAdornment));
            handle.transform.SetParent(frameRect, false);

            RectTransform rt = handle.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(HandleHitSize, HandleHitSize);
            rt.anchoredPosition = new Vector2(-signs.x * HandleVisualSize * 0.5f, -signs.y * HandleVisualSize * 0.5f);

            Image img = handle.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.001f);

            GameObject visual = new GameObject("Visual", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(SelectionAdornment));
            visual.transform.SetParent(handle.transform, false);
            RectTransform visualRt = visual.GetComponent<RectTransform>();
            visualRt.anchorMin = new Vector2(0.5f, 0.5f);
            visualRt.anchorMax = new Vector2(0.5f, 0.5f);
            visualRt.pivot = new Vector2(0.5f, 0.5f);
            visualRt.sizeDelta = new Vector2(HandleVisualSize, HandleVisualSize);
            visualRt.anchoredPosition = Vector2.zero;

            Image visualImage = visual.GetComponent<Image>();
            visualImage.color = Color.white;
            visualImage.raycastTarget = false;

            Outline outline = visual.GetComponent<Outline>();
            outline.effectColor = new Color(0.31f, 0.86f, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            CropFrameResizeHandle resize = handle.GetComponent<CropFrameResizeHandle>();
            resize.session = this;
            resize.xSign = Mathf.RoundToInt(signs.x);
            resize.ySign = Mathf.RoundToInt(signs.y);
        }

        private void CreateEdgeHandle(string name, Vector2 anchor, int xSign, int ySign)
        {
            bool horizontal = (ySign != 0);
            float hitW = horizontal ? 40f : 16f;
            float hitH = horizontal ? 16f : 40f;
            float barW = horizontal ? 20f : 3f;
            float barH = horizontal ? 3f : 20f;

            GameObject handle = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CropFrameResizeHandle), typeof(SelectionAdornment));
            handle.transform.SetParent(frameRect, false);

            RectTransform rt = handle.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(hitW, hitH);
            rt.anchoredPosition = Vector2.zero;

            Image img = handle.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.001f);

            GameObject visual = new GameObject("Visual", typeof(RectTransform), typeof(Image), typeof(SelectionAdornment));
            visual.transform.SetParent(handle.transform, false);
            RectTransform visualRt = visual.GetComponent<RectTransform>();
            visualRt.anchorMin = new Vector2(0.5f, 0.5f);
            visualRt.anchorMax = new Vector2(0.5f, 0.5f);
            visualRt.pivot = new Vector2(0.5f, 0.5f);
            visualRt.sizeDelta = new Vector2(barW, barH);
            visualRt.anchoredPosition = Vector2.zero;

            Image visualImage = visual.GetComponent<Image>();
            visualImage.color = Color.white;
            visualImage.raycastTarget = false;

            CropFrameResizeHandle resize = handle.GetComponent<CropFrameResizeHandle>();
            resize.session = this;
            resize.xSign = xSign;
            resize.ySign = ySign;
        }

        public void SetHandleVisualState(Transform handleTransform, bool active)
        {
            if (handleTransform == null) return;
            Transform visual = handleTransform.Find("Visual");
            if (visual == null) return;

            Image img = visual.GetComponent<Image>();
            Outline outline = visual.GetComponent<Outline>();
            RectTransform rt = visual as RectTransform;

            if (img != null)
                img.color = active ? new Color(0.90f, 1f, 0.93f, 1f) : Color.white;
            if (outline != null)
                outline.effectColor = active ? new Color(0.20f, 0.95f, 0.48f, 1f) : new Color(0.31f, 0.86f, 0.45f, 1f);
            if (rt != null)
                rt.sizeDelta = active ? new Vector2(HandleVisualSize + 2f, HandleVisualSize + 2f) : new Vector2(HandleVisualSize, HandleVisualSize);
        }

        private void FitFrameToPreset()
        {
            Vector2 maxSize = targetRect.rect.size * 0.88f;
            Vector2 newSize = maxSize;

            if (fixedAspect > 0.0001f)
            {
                if (maxSize.x / maxSize.y > fixedAspect)
                    newSize = new Vector2(maxSize.y * fixedAspect, maxSize.y);
                else
                    newSize = new Vector2(maxSize.x, maxSize.x / fixedAspect);
            }

            frameRect.sizeDelta = newSize;
            frameRect.anchoredPosition = Vector2.zero;
        }

        private void RefreshMaskAndPreview()
        {
            Sprite overlaySprite = BuildOverlaySprite();
            dimOverlayGraphic.sprite = overlaySprite;
            dimOverlayGraphic.type = Image.Type.Simple;
            dimOverlayGraphic.color = new Color(0f, 0f, 0f, 0.58f);

            if (cachedOutlineShape != currentShape)
            {
                cachedShapeOutline = BuildShapeSprite(currentShape, true);
                cachedOutlineShape = currentShape;
            }
            shapeOutlineGraphic.sprite = cachedShapeOutline;
            shapeOutlineGraphic.type = currentShape == CropShapeType.Rect ? Image.Type.Sliced : Image.Type.Simple;
            shapeOutlineGraphic.color = new Color(0.31f, 0.86f, 0.45f, 0.92f);
        }

        private Vector2 ClampFrameCenter(Vector2 center, Vector2 size)
        {
            Vector2 halfPaper = targetRect.rect.size * 0.5f;
            Vector2 halfFrame = size * 0.5f;
            center.x = Mathf.Clamp(center.x, -halfPaper.x + halfFrame.x, halfPaper.x - halfFrame.x);
            center.y = Mathf.Clamp(center.y, -halfPaper.y + halfFrame.y, halfPaper.y - halfFrame.y);
            return center;
        }

        private Texture2D RenderMaskedCrop()
        {
            int outputWidth = Mathf.Max(1, Mathf.RoundToInt(frameRect.rect.width));
            int outputHeight = Mathf.Max(1, Mathf.RoundToInt(frameRect.rect.height));
            Texture2D tex = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false, true);

            for (int y = 0; y < outputHeight; y++)
            {
                for (int x = 0; x < outputWidth; x++)
                {
                    float nx = ((x + 0.5f) / outputWidth) * 2f - 1f;
                    float ny = ((y + 0.5f) / outputHeight) * 2f - 1f;
                    if (!IsInsideShape(currentShape, nx, ny))
                    {
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                        continue;
                    }

                    Vector2 localInFrame = new Vector2(
                        (nx * frameRect.rect.width) * 0.5f,
                        (ny * frameRect.rect.height) * 0.5f);
                    Vector2 rootLocal = frameRect.anchoredPosition + localInFrame;
                    Vector2 imageLocal = rootLocal - cropImageRect.anchoredPosition;
                    float u = imageLocal.x / cropImageRect.sizeDelta.x + 0.5f;
                    float v = imageLocal.y / cropImageRect.sizeDelta.y + 0.5f;

                    if (u < 0f || u > 1f || v < 0f || v > 1f)
                    {
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, sourceTexture.GetPixelBilinear(u, v));
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private Sprite BuildShapeSprite(CropShapeType shape, bool outlineOnly)
        {
            const int size = 512;
            const float border = 0.02f;
            const int aa = 3;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float invAa = 1f / aa;
            float invAaSq = 1f / (aa * aa);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alphaSum = 0f;
                    for (int sy = 0; sy < aa; sy++)
                    {
                        for (int sx = 0; sx < aa; sx++)
                        {
                            float u = ((x + (sx + 0.5f) * invAa) / size) * 2f - 1f;
                            float v = ((y + (sy + 0.5f) * invAa) / size) * 2f - 1f;
                            bool outer = IsInsideShape(shape, u, v);
                            bool inner = IsInsideShape(shape, u / (1f - border), v / (1f - border));
                            alphaSum += outlineOnly ? ((outer && !inner) ? 1f : 0f) : (outer ? 1f : 0f);
                        }
                    }
                    byte alphaByte = (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaSum * invAaSq) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alphaByte);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(48, 48, 48, 48));
        }

        private Sprite BuildOverlaySprite()
        {
            const int size = 256;
            const int aa = 2;
            float invAaSq = 1f / (aa * aa);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 targetSize = targetRect.rect.size;
            Vector2 frameSize = frameRect.sizeDelta;
            Vector2 frameCenter = frameRect.anchoredPosition;
            float halfFW = Mathf.Max(frameSize.x * 0.5f, 0.0001f);
            float halfFH = Mathf.Max(frameSize.y * 0.5f, 0.0001f);

            Color32[] pixels = new Color32[size * size];

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    int insideCount = 0;
                    for (int sy = 0; sy < aa; sy++)
                    {
                        for (int sx = 0; sx < aa; sx++)
                        {
                            float fx = px + (sx + 0.5f) / aa;
                            float fy = py + (sy + 0.5f) / aa;
                            float rx = (fx / size - 0.5f) * targetSize.x;
                            float ry = (fy / size - 0.5f) * targetSize.y;
                            float localX = (rx - frameCenter.x) / halfFW;
                            float localY = (ry - frameCenter.y) / halfFH;
                            if (Mathf.Abs(localX) <= 1f && Mathf.Abs(localY) <= 1f && IsInsideShape(currentShape, localX, localY))
                                insideCount++;
                        }
                    }
                    byte alpha = (byte)(255 - Mathf.RoundToInt(insideCount * invAaSq * 255f));
                    pixels[py * size + px] = new Color32(255, 255, 255, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite BuildRectBorderSprite()
        {
            const int size = 128;
            const float border = 2f;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x < border || y < border || x >= size - border || y >= size - border;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, isBorder ? 1f : 0f));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
        }

        private static bool IsInsideShape(CropShapeType shape, float x, float y)
        {
            switch (shape)
            {
                case CropShapeType.Rect:
                    return Mathf.Abs(x) <= 1f && Mathf.Abs(y) <= 1f;
                case CropShapeType.Ellipse:
                    return (x * x) + (y * y) <= 1f;
                case CropShapeType.Triangle:
                    return PointInTriangle(new Vector2(x, y), new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(0f, 1f));
                case CropShapeType.Star:
                    return PointInPolygon(new Vector2(x, y), BuildStarVertices());
                case CropShapeType.Heart:
                    float hx = x * 1.13f;
                    float hy = y * 1.12f + 0.12f;
                    float a = (hx * hx + hy * hy - 1f);
                    return (a * a * a - hx * hx * hy * hy * hy) <= 0f;
                default:
                    return true;
            }
        }

        private static Vector2[] BuildStarVertices()
        {
            Vector2[] pts = new Vector2[10];
            float outer = 1.0f;
            float inner = 0.42f;
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.Deg2Rad * (-90f + i * 36f);
                float radius = (i % 2 == 0) ? outer : inner;
                pts[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            return pts;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = Sign(p, a, b);
            float s2 = Sign(p, b, c);
            float s3 = Sign(p, c, a);
            bool hasNeg = (s1 < 0f) || (s2 < 0f) || (s3 < 0f);
            bool hasPos = (s1 > 0f) || (s2 > 0f) || (s3 > 0f);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static bool PointInPolygon(Vector2 p, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool intersect = ((polygon[i].y > p.y) != (polygon[j].y > p.y)) &&
                                 (p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y + 0.00001f) + polygon[i].x);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        private static Vector2 GetFittedSize(Vector2 bounds, float sourceWidth, float sourceHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0) return bounds;
            float scale = Mathf.Min(bounds.x / sourceWidth, bounds.y / sourceHeight);
            return new Vector2(sourceWidth * scale, sourceHeight * scale);
        }

    }

    public class CropImageDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CropToolSession session;
        private Canvas canvas;
        private bool dragging;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            NativeCursorUtility.Apply(NativeCursorShape.SizeAll);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (session == null) return;
            if (dragging)
                NativeCursorUtility.Apply(NativeCursorShape.SizeAll);
            Vector2 delta = eventData.delta;
            if (canvas != null) delta /= canvas.scaleFactor;
            session.MoveUnderlyingImage(delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            NativeCursorUtility.Reset();
        }
    }

    public class CropFrameResizeHandle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CropToolSession session;
        public int xSign;
        public int ySign;

        private RectTransform parentRect;
        private Vector2 startSize;
        private Vector2 startCenter;
        private Vector2 oppositePointParentLocal;
        private Vector2 rightAxisParent;
        private Vector2 upAxisParent;
        private Vector2 diagonalDirParent;
        private float startDiagonalLength;
        private bool hovered;
        private bool dragging;
        private bool isCorner;

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            session?.SetHandleVisualState(transform, true);
            ApplyCursor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            session?.SetHandleVisualState(transform, false);
            if (!dragging)
                NativeCursorUtility.Reset();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (session == null || session.frameRect == null) return;

            dragging = true;
            isCorner = (xSign != 0 && ySign != 0);
            parentRect = session.frameRect.parent as RectTransform;
            startSize = session.frameRect.sizeDelta;
            startCenter = session.frameRect.anchoredPosition;
            rightAxisParent = ParentAxisFromLocal(session.frameRect, parentRect, Vector2.right);
            upAxisParent = ParentAxisFromLocal(session.frameRect, parentRect, Vector2.up);

            float ox = xSign != 0 ? -xSign * startSize.x * 0.5f : 0f;
            float oy = ySign != 0 ? -ySign * startSize.y * 0.5f : 0f;
            oppositePointParentLocal = startCenter + rightAxisParent * ox + upAxisParent * oy;

            if (isCorner)
            {
                diagonalDirParent = (
                    rightAxisParent * (xSign * startSize.x) +
                    upAxisParent * (ySign * startSize.y)).normalized;
                startDiagonalLength = Mathf.Max(
                    Vector2.Distance(
                        oppositePointParentLocal,
                        startCenter
                        + rightAxisParent * (xSign * startSize.x * 0.5f)
                        + upAxisParent * (ySign * startSize.y * 0.5f)),
                    0.0001f);
            }

            session.SetHandleVisualState(transform, true);
            ApplyCursor();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (session == null || session.frameRect == null || parentRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            Vector2 fromOpposite = localPoint - oppositePointParentLocal;
            Vector2 newSize;
            Vector2 newCenter;

            if (isCorner)
            {
                float width = Mathf.Max(40f, xSign * Vector2.Dot(fromOpposite, rightAxisParent));
                float height = Mathf.Max(40f, ySign * Vector2.Dot(fromOpposite, upAxisParent));
                newSize = new Vector2(width, height);

                if (session.FixedAspect > 0.0001f)
                {
                    float scale = Vector2.Dot(fromOpposite, diagonalDirParent) / startDiagonalLength;
                    scale = Mathf.Max(scale, 0.1f);
                    newSize = startSize * scale;
                }

                newCenter = oppositePointParentLocal
                    + rightAxisParent * (xSign * newSize.x * 0.5f)
                    + upAxisParent * (ySign * newSize.y * 0.5f);
            }
            else if (xSign != 0)
            {
                float width = Mathf.Max(40f, xSign * Vector2.Dot(fromOpposite, rightAxisParent));
                float height = startSize.y;
                if (session.FixedAspect > 0.0001f)
                    height = width / session.FixedAspect;
                newSize = new Vector2(width, height);
                newCenter = oppositePointParentLocal + rightAxisParent * (xSign * newSize.x * 0.5f);
            }
            else
            {
                float height = Mathf.Max(40f, ySign * Vector2.Dot(fromOpposite, upAxisParent));
                float width = startSize.x;
                if (session.FixedAspect > 0.0001f)
                    width = height * session.FixedAspect;
                newSize = new Vector2(width, height);
                newCenter = oppositePointParentLocal + upAxisParent * (ySign * newSize.y * 0.5f);
            }

            session.ResizeFrame(newSize, newCenter);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            session?.SetHandleVisualState(transform, hovered);
            if (hovered) ApplyCursor();
            else NativeCursorUtility.Reset();
        }

        private void ApplyCursor()
        {
            if (xSign != 0 && ySign != 0)
                NativeCursorUtility.Apply(xSign != ySign ? NativeCursorShape.SizeNwSe : NativeCursorShape.SizeNeSw);
            else if (xSign != 0)
                NativeCursorUtility.Apply(NativeCursorShape.SizeWE);
            else
                NativeCursorUtility.Apply(NativeCursorShape.SizeNS);
        }

        private static Vector2 ParentAxisFromLocal(RectTransform target, RectTransform parent, Vector2 localAxis)
        {
            Vector3 world = target.TransformVector(new Vector3(localAxis.x, localAxis.y, 0f));
            Vector3 local = parent.InverseTransformVector(world);
            return new Vector2(local.x, local.y).normalized;
        }
    }
}
