using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PocoRender.UI.Core;
using PocoRender.UI.TextureEffects;

namespace PocoRender.UI
{
    public class EraserToolSession : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public CanvasController controller;
        public GameObject targetObject;

        private RectTransform targetRect;
        private Image targetImage;
        private ObjectManipulator manipulator;
        private Texture2D sourceTexture;
        private Texture2D workingTexture;
        private Sprite workingSprite;
        private Sprite originalSprite;
        private Color32[] workingPixels;
        private int texWidth, texHeight;

        private int brushSize = 20;
        private bool isPainting;
        private Vector2 lastPaintUV = new Vector2(-9999, -9999);
        private bool hasStroked;

        private GameObject cursorOverlay;
        private Image innerCircleImg;
        private Image outerCircleImg;
        private RectTransform outerCircleRt;
        private Canvas parentCanvas;
        private Camera uiCamera;

        public static EraserToolSession Create(CanvasController controller, GameObject targetObject)
        {
            GameObject root = new GameObject("EraserToolSession", typeof(RectTransform), typeof(EraserToolSession), typeof(SelectionAdornment));
            root.transform.SetParent(targetObject.transform, false);

            EraserToolSession session = root.GetComponent<EraserToolSession>();
            session.controller = controller;
            session.targetObject = targetObject;
            session.Initialize();
            return session;
        }

        public bool IsFor(GameObject obj)
        {
            return targetObject == obj;
        }

        private void Initialize()
        {
            targetRect = targetObject.GetComponent<RectTransform>();
            targetImage = targetObject.GetComponent<Image>();
            manipulator = targetObject.GetComponent<ObjectManipulator>();
            originalSprite = targetImage != null ? targetImage.sprite : null;

            RectTransform rootRt = GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Image blocker = gameObject.AddComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true;

            sourceTexture = SpriteTextureUtil.ExtractSpriteTexture(originalSprite, 0);
            if (sourceTexture == null || targetImage == null) return;

            texWidth = sourceTexture.width;
            texHeight = sourceTexture.height;

            workingTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false, true);
            workingPixels = sourceTexture.GetPixels32();
            workingTexture.SetPixels32(workingPixels);
            workingTexture.Apply();

            workingSprite = Sprite.Create(
                workingTexture,
                new Rect(0, 0, texWidth, texHeight),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

            targetImage.sprite = workingSprite;

            if (manipulator != null) manipulator.enabled = false;

            parentCanvas = targetObject.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = parentCanvas.worldCamera;

            CreateCursorOverlay();
        }

        public void SetBrushSize(int size)
        {
            brushSize = Mathf.Clamp(size, 5, 100);
            UpdateCursorSize();
        }

        public void CancelErase()
        {
            if (targetImage != null && originalSprite != null)
                targetImage.sprite = originalSprite;
            if (manipulator != null) manipulator.enabled = true;
            DestroyCursorOverlay();
            Destroy(gameObject);
        }

        public void ExitErase()
        {
            CommitStroke();
            if (manipulator != null) manipulator.enabled = true;
            DestroyCursorOverlay();
            Destroy(gameObject);
        }

        private void CommitStroke()
        {
            if (!hasStroked || targetImage == null) return;

            Sprite oldSprite = originalSprite;

            Texture2D finalTex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false, true);
            finalTex.SetPixels32(workingPixels);
            finalTex.Apply();

            Sprite newSprite = Sprite.Create(
                finalTex,
                new Rect(0, 0, texWidth, texHeight),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

            targetImage.sprite = newSprite;
            targetImage.color = Color.white;

            var cmd = new EraseCommand(
                targetImage, oldSprite, newSprite,
                () => { controller?.UpdatePositionInfo(); controller?.OnObjectMoved(); });
            controller?.RecordErase(cmd);

            originalSprite = newSprite;
            workingSprite = Sprite.Create(
                workingTexture,
                new Rect(0, 0, texWidth, texHeight),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            targetImage.sprite = workingSprite;
            hasStroked = false;
        }

        private void Update()
        {
            if (workingTexture == null || targetRect == null) return;

            UpdateCursorPosition();

            if (Input.GetMouseButtonDown(0) && IsPointerOverTarget())
            {
                isPainting = true;
                lastPaintUV = new Vector2(-9999, -9999);
                PaintAt(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && isPainting)
            {
                PaintAt(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0) && isPainting)
            {
                isPainting = false;
                lastPaintUV = new Vector2(-9999, -9999);
                CommitStroke();
            }
        }

        private bool IsPointerOverTarget()
        {
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect, Input.mousePosition, uiCamera, out localPoint))
                return false;

            Rect r = targetRect.rect;
            return r.Contains(localPoint);
        }

        private void PaintAt(Vector3 screenPos)
        {
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect, screenPos, uiCamera, out localPoint))
                return;

            Rect r = targetRect.rect;
            float u = (localPoint.x - r.x) / r.width;
            float v = (localPoint.y - r.y) / r.height;

            float texelPerPixel = (float)texWidth / r.width;
            int radius = Mathf.Max(1, Mathf.RoundToInt(brushSize * 0.5f * texelPerPixel));
            float stepDist = Mathf.Max(1f, radius * 0.3f);

            Vector2 currentUV = new Vector2(u * texWidth, v * texHeight);
            bool changed = false;

            if (lastPaintUV.x < -999)
            {
                changed = EraseDab(currentUV, radius);
            }
            else
            {
                Vector2 delta = currentUV - lastPaintUV;
                float dist = delta.magnitude;
                if (dist < 0.5f)
                {
                    changed = EraseDab(currentUV, radius);
                }
                else
                {
                    int steps = Mathf.Max(1, Mathf.CeilToInt(dist / stepDist));
                    for (int i = 0; i <= steps; i++)
                    {
                        float t = (float)i / steps;
                        Vector2 pt = Vector2.Lerp(lastPaintUV, currentUV, t);
                        changed |= EraseDab(pt, radius);
                    }
                }
            }

            lastPaintUV = currentUV;

            if (changed)
            {
                hasStroked = true;
                workingTexture.SetPixels32(workingPixels);
                workingTexture.Apply();
            }
        }

        private bool EraseDab(Vector2 center, int radius)
        {
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            int radiusSq = radius * radius;

            int minX = Mathf.Max(0, cx - radius);
            int maxX = Mathf.Min(texWidth - 1, cx + radius);
            int minY = Mathf.Max(0, cy - radius);
            int maxY = Mathf.Min(texHeight - 1, cy + radius);

            bool changed = false;
            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    int dx = px - cx;
                    int dy = py - cy;
                    int dSq = dx * dx + dy * dy;
                    if (dSq > radiusSq) continue;

                    int idx = py * texWidth + px;
                    if (workingPixels[idx].a == 0) continue;

                    float edgeFactor = Mathf.Clamp01(1f - (float)dSq / radiusSq);
                    int eraseAmount = Mathf.RoundToInt(edgeFactor * 180f);

                    int newA = workingPixels[idx].a - eraseAmount;
                    if (newA < 0) newA = 0;
                    workingPixels[idx] = new Color32(
                        workingPixels[idx].r,
                        workingPixels[idx].g,
                        workingPixels[idx].b,
                        (byte)newA);
                    changed = true;
                }
            }
            return changed;
        }

        private void CreateCursorOverlay()
        {
            cursorOverlay = new GameObject("EraserCursor", typeof(RectTransform));
            cursorOverlay.transform.SetParent(targetRect.parent, false);
            cursorOverlay.transform.SetAsLastSibling();

            RectTransform crt = cursorOverlay.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(brushSize, brushSize);

            // Outer circle - use the circle PNG directly, white tint to keep original look
            GameObject outerObj = UIFactory.CreateObject("OuterCircle", cursorOverlay);
            outerCircleRt = outerObj.GetComponent<RectTransform>();
            outerCircleRt.anchorMin = Vector2.zero; outerCircleRt.anchorMax = Vector2.one;
            outerCircleRt.offsetMin = Vector2.zero; outerCircleRt.offsetMax = Vector2.zero;
            outerCircleImg = outerObj.AddComponent<Image>();
            Sprite outerSpr = Resources.Load<Sprite>("EditIcons/p_edit__out_cicle");
            if (outerSpr != null)
            {
                outerCircleImg.sprite = outerSpr;
                outerCircleImg.type = Image.Type.Simple;
                outerCircleImg.preserveAspect = false;
            }
            outerCircleImg.color = Color.white;
            outerCircleImg.raycastTarget = false;

            // Inner circle (tiny center dot)
            GameObject innerObj = UIFactory.CreateObject("InnerCircle", cursorOverlay);
            RectTransform innerRt = innerObj.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0.5f, 0.5f); innerRt.anchorMax = new Vector2(0.5f, 0.5f);
            innerRt.sizeDelta = new Vector2(3, 3);
            innerCircleImg = innerObj.AddComponent<Image>();
            Sprite innerSpr = Resources.Load<Sprite>("EditIcons/p_edit_in_cicle");
            if (innerSpr != null)
            {
                innerCircleImg.sprite = innerSpr;
                innerCircleImg.type = Image.Type.Simple;
                innerCircleImg.preserveAspect = true;
            }
            innerCircleImg.color = Color.white;
            innerCircleImg.raycastTarget = false;

            CanvasGroup cg = cursorOverlay.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            UpdateCursorSize();
        }

        private void UpdateCursorSize()
        {
            if (cursorOverlay == null) return;
            RectTransform crt = cursorOverlay.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(brushSize, brushSize);
        }

        private void UpdateCursorPosition()
        {
            if (cursorOverlay == null || targetRect == null) return;

            RectTransform parentRt = targetRect.parent as RectTransform;
            if (parentRt == null) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRt, Input.mousePosition, uiCamera, out localPoint))
            {
                cursorOverlay.GetComponent<RectTransform>().anchoredPosition = localPoint;
            }

            bool over = IsPointerOverTarget();
            cursorOverlay.SetActive(over || isPainting);
        }

        private void DestroyCursorOverlay()
        {
            if (cursorOverlay != null)
            {
                Destroy(cursorOverlay);
                cursorOverlay = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData) { eventData.Use(); }
        public void OnPointerClick(PointerEventData eventData) { eventData.Use(); }
        public void OnBeginDrag(PointerEventData eventData) { eventData.Use(); }
        public void OnDrag(PointerEventData eventData) { eventData.Use(); }
        public void OnEndDrag(PointerEventData eventData) { eventData.Use(); }

        private void OnDestroy()
        {
            DestroyCursorOverlay();
        }
    }
}
