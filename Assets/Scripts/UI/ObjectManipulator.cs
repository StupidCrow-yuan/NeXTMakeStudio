using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PocoRender.UI.Core;

namespace PocoRender.UI
{
    public class ObjectManipulator : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsLocked { get; set; } = false;
        private CanvasController canvasController;
        private RectTransform rectTransform;
        private Canvas canvas;
        private Vector2 dragStartPos;
        private GameObject dragInfoBubble;
        private Text dragInfoText;
        private bool isHovered;
        private bool isDraggingSelf;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasController = GetComponentInParent<CanvasController>();
            
            if (canvasController == null)
            {
                canvasController = Object.FindObjectOfType<CanvasController>();
            }
        }

        private void OnDisable()
        {
            isHovered = false;
            isDraggingSelf = false;
            NativeCursorUtility.Reset();
            DestroyDragInfoBubble();
        }

        private void LateUpdate()
        {
            if ((isHovered || isDraggingSelf) && canvasController != null && canvasController.CurrentSelection == gameObject)
            {
                ApplyMoveCursor();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (canvasController != null)
            {
                canvasController.SelectObject(this.gameObject);
            }
            if (canvasController != null && canvasController.CurrentSelection == gameObject)
            {
                ApplyMoveCursor();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsLocked) return;
            if (rectTransform != null) dragStartPos = rectTransform.anchoredPosition;
            if (canvasController != null && canvasController.CurrentSelection == gameObject)
            {
                isDraggingSelf = true;
                ApplyMoveCursor();
                EnsureDragInfoBubble();
                UpdateDragInfoBubble(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsLocked || rectTransform == null) return;

            Vector2 delta = eventData.delta;
            if (canvas != null)
            {
                 delta /= canvas.scaleFactor;
            }

            rectTransform.anchoredPosition += delta;
            
            if (canvasController != null)
            {
                canvasController.UpdatePositionInfo();
                canvasController.OnObjectMoved();
                UpdateDragInfoBubble(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsLocked) return;
            isDraggingSelf = false;
            if (canvasController != null && rectTransform != null)
            {
                if (Vector2.Distance(dragStartPos, rectTransform.anchoredPosition) > 0.01f)
                {
                    canvasController.RecordMove(rectTransform, dragStartPos, rectTransform.anchoredPosition);
                }
            }
            DestroyDragInfoBubble();
            if (canvasController != null && canvasController.CurrentSelection == gameObject)
                ApplyMoveCursor();
            else
                NativeCursorUtility.Reset();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsLocked) return;
            isHovered = true;
            if (canvasController != null && canvasController.CurrentSelection == gameObject)
            {
                ApplyMoveCursor();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (!eventData.dragging)
            {
                DestroyDragInfoBubble();
                NativeCursorUtility.Reset();
            }
        }

        private void EnsureDragInfoBubble()
        {
            if (dragInfoBubble != null || canvas == null) return;

            dragInfoBubble = new GameObject("DragInfoBubble", typeof(RectTransform), typeof(Image), typeof(SelectionAdornment));
            dragInfoBubble.transform.SetParent(canvas.transform, false);

            RectTransform bubbleRt = dragInfoBubble.GetComponent<RectTransform>();
            bubbleRt.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.pivot = new Vector2(0, 1);
            bubbleRt.sizeDelta = new Vector2(120, 28);

            Image bg = dragInfoBubble.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            Sprite rounded = UIFactory.CreateRoundedRectSprite(96, 48, 7);
            if (rounded != null)
            {
                bg.sprite = rounded;
                bg.type = Image.Type.Sliced;
            }
            bg.raycastTarget = false;

            var outline = dragInfoBubble.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.15f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(dragInfoBubble.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 4);
            textRt.offsetMax = new Vector2(-8, -4);

            dragInfoText = textObj.GetComponent<Text>();
            dragInfoText.font = ResolveDragInfoFont();
            dragInfoText.fontSize = 12;
            dragInfoText.color = Color.white;
            dragInfoText.alignment = TextAnchor.MiddleCenter;
            dragInfoText.raycastTarget = false;
        }

        private void UpdateDragInfoBubble(PointerEventData eventData)
        {
            if (dragInfoBubble == null || dragInfoText == null || rectTransform == null || canvasController == null)
                return;

            Vector2 userPos = GetUserPosition();
            dragInfoText.text = $"({userPos.x:F1}, {userPos.y:F1})";

            RectTransform bubbleRt = dragInfoBubble.GetComponent<RectTransform>();
            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            bubbleRt.anchoredPosition = new Vector2(localPoint.x + 18f, localPoint.y - 18f);
        }

        private Vector2 GetUserPosition()
        {
            RectTransform paper = canvasController.paper;
            float halfW = paper != null ? paper.rect.width * 0.5f : 300f;
            float halfH = paper != null ? paper.rect.height * 0.5f : 300f;
            return new Vector2(halfW - rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + halfH);
        }

        private void DestroyDragInfoBubble()
        {
            if (dragInfoBubble != null)
            {
                Destroy(dragInfoBubble);
                dragInfoBubble = null;
                dragInfoText = null;
            }
        }

        private void ApplyMoveCursor()
        {
            NativeCursorUtility.Apply(NativeCursorShape.SizeAll);
        }

        private static Font ResolveDragInfoFont()
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
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return font;
        }
    }
}

