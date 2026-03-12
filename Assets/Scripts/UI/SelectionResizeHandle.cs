using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PocoRender.UI.Core;

namespace PocoRender.UI
{
    public class SelectionAdornment : MonoBehaviour
    {
    }

    public class SelectionResizeHandle : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public RectTransform target;
        public CanvasController controller;
        public int xSign;
        public int ySign;

        private RectTransform parentRect;
        private Vector2 startSize;
        private Vector2 startPosition;
        private Vector2 oppositeCornerParentLocal;
        private Vector2 rightAxisParent;
        private Vector2 upAxisParent;
        private bool isDragging;
        private Canvas canvas;
        private GameObject resizeInfoBubble;
        private Text resizeInfoText;
        private Vector2 diagonalDirParent;
        private float startDiagonalLength;

        private const float MinSize = 24f;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        private void OnDisable()
        {
            isDragging = false;
            DestroyResizeInfoBubble();
            NativeCursorUtility.Reset();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ApplyCursor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!eventData.dragging && !isDragging)
                NativeCursorUtility.Reset();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (target == null) return;

            parentRect = target.parent as RectTransform;
            if (parentRect == null) return;

            startSize = target.sizeDelta;
            startPosition = target.anchoredPosition;
            isDragging = true;

            rightAxisParent = ParentAxisFromLocal(Vector2.right);
            upAxisParent = ParentAxisFromLocal(Vector2.up);

            Vector2 oppositeOffsetParent =
                rightAxisParent * (-xSign * startSize.x * 0.5f) +
                upAxisParent * (-ySign * startSize.y * 0.5f);
            oppositeCornerParentLocal = startPosition + oppositeOffsetParent;
            diagonalDirParent = (
                rightAxisParent * (xSign * startSize.x) +
                upAxisParent * (ySign * startSize.y)).normalized;
            startDiagonalLength = Mathf.Max(
                Vector2.Distance(
                    oppositeCornerParentLocal,
                    startPosition +
                    rightAxisParent * (xSign * startSize.x * 0.5f) +
                    upAxisParent * (ySign * startSize.y * 0.5f)),
                0.0001f);

            ApplyCursor();
            EnsureResizeInfoBubble();
            UpdateResizeInfoBubble(eventData, startSize);
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null || parentRect == null) return;

            Vector2 pointerParentLocal = GetPointerLocalToParent(eventData);
            Vector2 fromOpposite = pointerParentLocal - oppositeCornerParentLocal;
            float projectedDiagonalLength = Vector2.Dot(fromOpposite, diagonalDirParent);
            float uniformScale = projectedDiagonalLength / startDiagonalLength;
            float minScale = Mathf.Max(MinSize / Mathf.Max(startSize.x, 1f), MinSize / Mathf.Max(startSize.y, 1f));
            uniformScale = Mathf.Max(uniformScale, minScale);

            Vector2 newSize = startSize * uniformScale;
            Vector2 newCenter = oppositeCornerParentLocal
                + rightAxisParent * (xSign * newSize.x * 0.5f)
                + upAxisParent * (ySign * newSize.y * 0.5f);

            if ((target.sizeDelta - newSize).sqrMagnitude < 0.0001f &&
                (target.anchoredPosition - newCenter).sqrMagnitude < 0.0001f)
                return;

            target.sizeDelta = newSize;
            target.anchoredPosition = newCenter;
            UpdateResizeInfoBubble(eventData, newSize);

            if (controller != null)
            {
                controller.UpdatePositionInfo();
                controller.OnObjectMoved();
            }

            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            if (target != null && controller != null)
            {
                if (Vector2.Distance(startSize, target.sizeDelta) > 0.01f ||
                    Vector2.Distance(startPosition, target.anchoredPosition) > 0.01f)
                {
                    controller.RecordResize(
                        target,
                        startSize,
                        target.sizeDelta,
                        startPosition,
                        target.anchoredPosition);
                }
            }

            DestroyResizeInfoBubble();
            ApplyCursor();
            eventData.Use();
        }

        private Vector2 ParentAxisFromLocal(Vector2 localAxis)
        {
            Vector3 world = target.TransformVector(new Vector3(localAxis.x, localAxis.y, 0f));
            Vector3 parent = parentRect.InverseTransformVector(world);
            return new Vector2(parent.x, parent.y).normalized;
        }

        private Vector2 GetPointerLocalToParent(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            return localPoint;
        }

        private void ApplyCursor()
        {
            NativeCursorUtility.Apply(xSign == ySign
                ? NativeCursorShape.SizeNwSe
                : NativeCursorShape.SizeNeSw);
        }

        private void EnsureResizeInfoBubble()
        {
            if (resizeInfoBubble != null || canvas == null) return;

            resizeInfoBubble = new GameObject("ResizeInfoBubble", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(SelectionAdornment));
            resizeInfoBubble.transform.SetParent(canvas.transform, false);

            RectTransform bubbleRt = resizeInfoBubble.GetComponent<RectTransform>();
            bubbleRt.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.pivot = new Vector2(0, 0.5f);
            bubbleRt.sizeDelta = new Vector2(148f, 30f);

            Image bg = resizeInfoBubble.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.88f);
            Sprite rounded = UIFactory.CreateRoundedRectSprite(96, 48, 10);
            if (rounded != null)
            {
                bg.sprite = rounded;
                bg.type = Image.Type.Sliced;
            }
            bg.raycastTarget = false;

            Outline outline = resizeInfoBubble.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(resizeInfoBubble.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 4);
            textRt.offsetMax = new Vector2(-10, -4);

            resizeInfoText = textObj.GetComponent<Text>();
            resizeInfoText.font = ResolveOverlayFont();
            resizeInfoText.fontSize = 12;
            resizeInfoText.color = Color.white;
            resizeInfoText.alignment = TextAnchor.MiddleCenter;
            resizeInfoText.raycastTarget = false;
        }

        private void UpdateResizeInfoBubble(PointerEventData eventData, Vector2 size)
        {
            if (resizeInfoBubble == null || resizeInfoText == null || canvas == null) return;

            resizeInfoText.text = $"W: {size.x:F2} H: {size.y:F2}";

            RectTransform bubbleRt = resizeInfoBubble.GetComponent<RectTransform>();
            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            bubbleRt.anchoredPosition = new Vector2(localPoint.x + 14f, localPoint.y - 10f);
        }

        private void DestroyResizeInfoBubble()
        {
            if (resizeInfoBubble != null)
            {
                Destroy(resizeInfoBubble);
                resizeInfoBubble = null;
                resizeInfoText = null;
            }
        }

        private static Font ResolveOverlayFont()
        {
            Font font = Resources.Load<Font>("fonts/HarmonyOS_Sans_SC_Regular");
            if (font == null) font = Resources.Load<Font>("fonts/NanumGothic-Regular");

            if (font == null)
            {
                string[] fontNames = { "Segoe UI Symbol", "Segoe UI", "Arial", "LegacyRuntime" };
                foreach (string fontName in fontNames)
                {
                    font = Font.CreateDynamicFontFromOSFont(fontName, 12);
                    if (font != null) break;
                }
            }

            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return font;
        }
    }
}
