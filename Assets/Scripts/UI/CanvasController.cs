using UnityEngine;
using UnityEngine.UI;
using PocoRender.UI.Core;
using System.Collections.Generic;
using PocoRender.UI.TextureEffects;
using PocoRender.Utils;
using System.IO;
using PocoRender.Communication;

namespace PocoRender.UI
{
    public class CanvasController : MonoBehaviour
    {
        public GameObject contextToolbar;
        
        // Position Info Fields
        public Text posXText;
        public Text posYText;
        public Text widthText;
        public Text heightText;
        public Text rotationText;
        public GameObject positionPanel; 
        public Text canvasSizeText; 
        public RectTransform bottomRuler;
        public RectTransform rightRuler;
        public GameObject layersListContainer; 
        public GameObject editorArea; // Reference to main editor area for popups
        public Image paperBackground; // For color syncing
        public GameObject craftModeContainer; // For syncing selection
        public GameObject printAreaListContainer; // Dynamic Print Area List in Global Panel
        private bool useMaterialColorAsBackground = true;

        [Header("Right Panel Context")]
        public GameObject globalInfoPanel;  // Device settings, Print Bed, etc.
        public GameObject layerInfoPanel;   // Position, Craft Mode, Ink Mode, etc. 

        [Header("Popups")]
        private GameObject activePopup;

        public void ShowInfoPopup(string title)
        {
            if (activePopup != null) Destroy(activePopup);
            activePopup = Modules.CanvasModule.CreateInfoPopup(editorArea, title);
        }

        public void ShowColorPicker()
        {
            if (activePopup != null) Destroy(activePopup);
            activePopup = Modules.CanvasModule.CreateColorPicker(editorArea);
        }

        [Header("Mini Preview")]
        public GameObject miniPreviewPanel;
        public GameObject customizePanel; // Area for uploading depth map
        public RawImage miniPreviewImage;
        public Model3DViewer miniModelViewer;
        private GameObject miniDesignStage;
        private float currentMiniZoom = 1.0f;

        // Callbacks
        public System.Action OnPreviewRequested;
        public System.Action OnPrintRequested;

        [Header("Global Print Settings (Plan A)")]
        public int printResolutionDpi = 360;
        public int printCopies = 1;
        public string printColorMode = "CMYK";
        public string printPaperSize = "A4";
        public string printMediaType = "plain";
        public bool printMirror = false;
        public bool printEnableHalftone = true;
        public bool printEnableInkOptimization = false;
        public bool printEnableSkinDetection = true;
        public bool printEnableGuidedFilter = true;
        public bool printShowInkPreview = true;

        [Header("Canvas Interaction")]
        public RectTransform paper; // The White Canvas area
        public Text zoomText;
        public Dropdown zoomDropdown;
        
        [Header("Upload Panel")]
        public GameObject uploadListContainer;
        
        private GameObject currentSelection;
        private GameObject rotationHandle;
        private Outline currentOutline;

        private float paperWidth = 600f;
        private float paperHeight = 600f;
        private float currentZoom = 1.0f;

        private Vector2 lastMousePos;
        private bool isPanning = false;
        private bool handToolActive = false;

        private CommandHistory commandHistory = new CommandHistory();
        private static readonly HashSet<string> SupportedUploadExtensions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".svg"
        };
        private static readonly string UploadSupportedFormatsText = "PDF, PNG, JPEG, GIF, BMP, TIFF, WEBP, SVG";
        private readonly Dictionary<string, string> pendingUploadRequestById = new Dictionary<string, string>();
        private QtBridgeController qtBridgeController;
        private string pendingUploadFileDialogRequestId;

        /// <summary>
        /// 当前选中的图层对象（为 null 表示未选中任何单独图层，此时视为“整张画布”）。
        /// 仅提供只读访问，外部（例如 HomeModule）可据此决定打印行为。
        /// </summary>
        public GameObject CurrentSelection => currentSelection;

        public void Undo()
        {
            commandHistory.Undo();
            UpdateLayersPanel();
            if (globalInfoPanel != null && globalInfoPanel.activeSelf) UpdatePrintAreaList();
        }

        public void Redo()
        {
            commandHistory.Redo();
            UpdateLayersPanel();
            if (globalInfoPanel != null && globalInfoPanel.activeSelf) UpdatePrintAreaList();
        }

        public void RecordMove(RectTransform rt, Vector2 oldPos, Vector2 newPos)
        {
            commandHistory.AddToHistory(new MoveCommand(rt, oldPos, newPos, UpdatePositionInfo));
        }

        public void RecordRotation(RectTransform rt, Quaternion oldRot, Quaternion newRot)
        {
            commandHistory.AddToHistory(new RotateCommand(rt, oldRot, newRot, UpdatePositionInfo));
        }

        public void RecordAdd(GameObject obj)
        {
            commandHistory.AddToHistory(new AddObjectCommand(obj, paper));
            UpdateLayersPanel();
            if (globalInfoPanel != null && globalInfoPanel.activeSelf) UpdatePrintAreaList();
        }

        public void RecordDelete(GameObject obj)
        {
            if (obj == null) return;
            commandHistory.ExecuteCommand(new DeleteObjectCommand(obj, () => { 
                UpdatePositionInfo(); 
                UpdateLayersPanel(); 
                if (globalInfoPanel != null && globalInfoPanel.activeSelf) UpdatePrintAreaList();
            }));
            UpdateLayersPanel();
            if (globalInfoPanel != null && globalInfoPanel.activeSelf) UpdatePrintAreaList();
        }

        public void SelectObject(GameObject obj)
        {
            if (currentSelection == obj) return;
            
            // Check if object is locked
            var manipulator = obj.GetComponent<ObjectManipulator>();
            if (manipulator != null && manipulator.IsLocked) return;

            // IMPORTANT: If we are clicking a new object, deselect old one first
            Deselect();
            currentSelection = obj;
            
            currentOutline = currentSelection.GetComponent<Outline>();
            if (currentOutline == null) currentOutline = currentSelection.AddComponent<Outline>();
            currentOutline.effectColor = Color.green;
            currentOutline.effectDistance = new Vector2(2, -2);
            currentOutline.enabled = true;
            
            // Fix: Ensure outline respects sprite shape if it's an Image
            Image selImg = currentSelection.GetComponent<Image>();
            if (selImg != null)
            {
                // Note: Unity's built-in Outline component always draws a rectangle for UI elements.
                // To support irregular shapes, we would need a custom shader or 3rd party asset.
                // For now, we will stick to rectangular selection as per Unity UI standard,
                // but we can adjust the RectTransform to fit the content better if needed.
            }

            if (contextToolbar != null) contextToolbar.SetActive(true);
            CreateRotationHandle();

            if (positionPanel != null) positionPanel.SetActive(true);
            if (canvasSizeText != null) canvasSizeText.gameObject.SetActive(false);
            
            // Switch to Layer Panel FIRST so children can start coroutines
            if (layerInfoPanel != null) layerInfoPanel.SetActive(true);
            if (globalInfoPanel != null) globalInfoPanel.SetActive(false);

            // Sync Craft Mode UI
            var data = obj.GetComponent<LayerData>();
            if (data == null) data = obj.AddComponent<LayerData>();
            SyncCraftModeUI(data.craftMode);

            UpdatePositionInfo();
            UpdateLayersPanel(); // Refresh selection in list
            
            // Update Mini Preview
            UpdateMiniPreview();
        }

        public void Deselect()
        {
            if (currentSelection != null)
            {
                if (currentOutline != null) currentOutline.enabled = false;
                DestroyRotationHandle();
            }

            currentSelection = null;
            if (contextToolbar != null) contextToolbar.SetActive(false);
            
            if (positionPanel != null) positionPanel.SetActive(false);
            if (canvasSizeText != null) canvasSizeText.gameObject.SetActive(true);

            // Switch to Global Panel
            if (layerInfoPanel != null) layerInfoPanel.SetActive(false);
            if (globalInfoPanel != null) 
            {
                globalInfoPanel.SetActive(true);
                UpdatePrintAreaList(); // Refresh the list when switching to global panel
            }

            UpdateLayersPanel();
        }

        public void UpdatePrintAreaList()
        {
            if (printAreaListContainer == null || paper == null) return;

            // Clear old items
            foreach (Transform child in printAreaListContainer.transform) Destroy(child.gameObject);

            // Iterate layers from bottom to top (matches sibling index 0 to N)
            // Skip inactive layers (hidden by Undo)
            for (int i = 0; i < paper.childCount; i++)
            {
                Transform layer = paper.GetChild(i);
                if (layer.name == "BGDeselector") continue;
                if (!layer.gameObject.activeSelf) continue;

                CreatePrintAreaItem(layer.gameObject);
            }
        }

        private void CreatePrintAreaItem(GameObject layerObj)
        {
            GameObject item = new GameObject("PrintAreaItem", typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(printAreaListContainer.transform, false);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 60); // Total item height
            
            item.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f); 
            item.GetComponent<Button>().onClick.AddListener(() => SelectObject(layerObj));

            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 5, 5); hlg.spacing = 15; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true; hlg.childControlHeight = true; hlg.childForceExpandWidth = true;

            // 1. Thumbnail Container (to maintain aspect ratio and size)
            GameObject thumbFrame = new GameObject("ThumbFrame", typeof(RectTransform));
            thumbFrame.transform.SetParent(item.transform, false);
            LayoutElement leFrame = thumbFrame.AddComponent<LayoutElement>();
            leFrame.minWidth = 50; leFrame.preferredWidth = 50; leFrame.minHeight = 50; leFrame.preferredHeight = 50;

            GameObject thumbObj = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbObj.transform.SetParent(thumbFrame.transform, false);
            RectTransform thumbRt = thumbObj.GetComponent<RectTransform>();
            thumbRt.sizeDelta = new Vector2(50, 50); // Fixed 50x50
            thumbRt.anchoredPosition = Vector2.zero;
            
            Image thumbImg = thumbObj.GetComponent<Image>();
            thumbImg.preserveAspect = true; // Prevent stretching
            
            // Set thumbnail color or sprite from the layer
            Image layerImg = layerObj.GetComponent<Image>();
            if (layerImg != null)
            {
                thumbImg.sprite = layerImg.sprite;
                thumbImg.color = layerImg.color;
            }
            else
            {
                thumbImg.color = Color.black;
            }

            // 2. Info Container
            GameObject info = new GameObject("Info", typeof(RectTransform));
            info.transform.SetParent(item.transform, false);
            VerticalLayoutGroup ivlg = info.AddComponent<VerticalLayoutGroup>();
            ivlg.childAlignment = TextAnchor.MiddleLeft; ivlg.spacing = 2;

            var data = layerObj.GetComponent<LayerData>();
            string craftMode = data != null ? data.craftMode : "Flat";
            string inkMode = data != null ? data.inkMode : "White > CMYK";

            Text nameTxt = UIFactory.CreateText(craftMode, info, 13, Color.black, Vector2.zero, Vector2.zero).GetComponent<Text>();
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleLeft;

            Text modeTxt = UIFactory.CreateText(inkMode, info, 11, Color.gray, Vector2.zero, Vector2.zero).GetComponent<Text>();
            modeTxt.alignment = TextAnchor.MiddleLeft;

            // 3. Right side info (e.g. Height for Raised)
            if (craftMode.Contains("Raised"))
            {
                GameObject rightInfo = new GameObject("RightInfo", typeof(RectTransform));
                rightInfo.transform.SetParent(item.transform, false);
                Text rText = UIFactory.CreateText("1mm", rightInfo, 11, Color.gray, Vector2.zero, Vector2.zero).GetComponent<Text>();
                rText.alignment = TextAnchor.MiddleRight;
                rightInfo.AddComponent<LayoutElement>().minWidth = 40;
            }
        }

        public void SetPaperColor(Color c)
        {
            if (useMaterialColorAsBackground && paperBackground != null)
            {
                paperBackground.color = c;
            }
        }

        public void SetUseMaterialColor(bool use)
        {
            useMaterialColorAsBackground = use;
        }

        public void UpdatePositionInfo()
        {
            if (currentSelection != null)
            {
                RectTransform rt = currentSelection.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // USER REQ: Bottom-right is (0,0). X increases left, Y increases up.
                    // Paper is 600x600. Center is (0,0) in Unity.
                    // Bottom-right in Unity is (300, -300).
                    float userX = 300f - rt.anchoredPosition.x;
                    float userY = rt.anchoredPosition.y + 300f;
                    
                    if(posXText) posXText.text = $"X: {userX:F1} mm";
                    if(posYText) posYText.text = $"Y: {userY:F1} mm";
                    if(widthText) widthText.text = $"W: {rt.rect.width:F1} mm";
                    if(heightText) heightText.text = $"H: {rt.rect.height:F1} mm";
                    
                    float rot = rt.localEulerAngles.z;
                    if (rot > 180) rot -= 360;
                    if(rotationText) rotationText.text = $"Rotation: {-rot:F1}°";
                }
            }
        }

        #region Zoom and Pan
        public void ChangeZoom(float delta)
        {
            SetZoom(currentZoom + delta);
        }

        public void SetZoom(float value)
        {
            currentZoom = Mathf.Clamp(value, 0.1f, 20f);
            
            if (paper != null)
            {
                paper.localScale = new Vector3(currentZoom, currentZoom, 1f);
            }
            
            if (zoomText != null) zoomText.text = $"{(currentZoom * 100):F0}%";
            if (zoomDropdown != null) zoomDropdown.captionText.text = $"{(currentZoom * 100):F0}%";

            UpdateRulers();
        }

        public void UpdateRulers()
        {
            if (bottomRuler == null || rightRuler == null || paper == null) return;

            // Clear old labels and ticks
            foreach (Transform child in bottomRuler) Destroy(child.gameObject);
            foreach (Transform child in rightRuler) Destroy(child.gameObject);

            // 1. Calculate Major Step (with labels)
            float labelPixelThreshold = 80f; 
            float[] possibleSteps = { 1, 2, 5, 10, 25, 50, 100, 200, 500, 1000, 2500, 5000 };
            float majorStep = 100;
            foreach (float s in possibleSteps)
            {
                if (s * currentZoom >= labelPixelThreshold) { majorStep = s; break; }
            }

            // 2. Calculate Minor Step (1/10 of major)
            float minorStep = majorStep / 10f;

            // --- Bottom Ruler (X) ---
            float wsWidth = bottomRuler.rect.width;
            float halfWs = wsWidth / 2f;
            float minUserX = 300f - (halfWs - paper.anchoredPosition.x) / currentZoom;
            float maxUserX = 300f - (-halfWs - paper.anchoredPosition.x) / currentZoom;
            
            float startX = Mathf.Floor(Mathf.Min(minUserX, maxUserX) / majorStep) * majorStep;
            float endX = Mathf.Ceil(Mathf.Max(minUserX, maxUserX) / majorStep) * majorStep;

            for (float x = startX; x <= endX; x += majorStep)
            {
                // Major Tick & Text
                float unityX = (300f - x) * currentZoom + paper.anchoredPosition.x;
                if (unityX >= -halfWs && unityX <= halfWs)
                {
                    DrawTick(bottomRuler, new Vector2(unityX, 6), new Vector2(1, 12)); // Major tick line
                    GameObject t = UIFactory.CreateText(x.ToString(), bottomRuler.gameObject, 7, Color.gray, new Vector2(unityX, -4), new Vector2(40, 20));
                    t.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                }

                // Minor Ticks (10 subdivisions)
                for (int i = 1; i < 10; i++)
                {
                    float sx = x + i * minorStep;
                    float usx = (300f - sx) * currentZoom + paper.anchoredPosition.x;
                    if (usx >= -halfWs && usx <= halfWs)
                    {
                        DrawTick(bottomRuler, new Vector2(usx, 9), new Vector2(1, 6)); // Minor tick line
                    }
                }
            }

            // --- Right Ruler (Y) ---
            float wsHeight = rightRuler.rect.height;
            float halfHs = wsHeight / 2f;
            float minUserY = 300f + (-halfHs - paper.anchoredPosition.y) / currentZoom;
            float maxUserY = 300f + (halfHs - paper.anchoredPosition.y) / currentZoom;

            float startY = Mathf.Floor(Mathf.Min(minUserY, maxUserY) / majorStep) * majorStep;
            float endY = Mathf.Ceil(Mathf.Max(minUserY, maxUserY) / majorStep) * majorStep;

            for (float y = startY; y <= endY; y += majorStep)
            {
                // Major Tick & Text
                float unityY = (y - 300f) * currentZoom + paper.anchoredPosition.y;
                if (unityY >= -halfHs && unityY <= halfHs)
                {
                    DrawTick(rightRuler, new Vector2(6, unityY), new Vector2(12, 1)); // Major tick line
                    GameObject t = UIFactory.CreateText(y.ToString(), rightRuler.gameObject, 7, Color.gray, new Vector2(-6, unityY), new Vector2(20, 40));
                    t.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                }

                // Minor Ticks
                for (int i = 1; i < 10; i++)
                {
                    float sy = y + i * minorStep;
                    float usy = (sy - 300f) * currentZoom + paper.anchoredPosition.y;
                    if (usy >= -halfHs && usy <= halfHs)
                    {
                        DrawTick(rightRuler, new Vector2(9, usy), new Vector2(6, 1)); // Minor tick line
                    }
                }
            }
        }

        private void DrawTick(RectTransform parent, Vector2 pos, Vector2 size)
        {
            GameObject tick = new GameObject("Tick", typeof(RectTransform), typeof(Image));
            tick.transform.SetParent(parent, false);
            RectTransform rt = tick.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            tick.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        }

        public void UpdateLayersPanel()
        {
            if (layersListContainer == null || paper == null) return;

            // Clear old items (skip title if any, but we re-draw all)
            foreach (Transform child in layersListContainer.transform) Destroy(child.gameObject);

            // Iterate all objects on paper in reverse to match visual order (top-most first).
            // Skip inactive layers (hidden by Undo) so they don't appear in the list.
            for (int i = paper.childCount - 1; i >= 0; i--)
            {
                Transform layer = paper.GetChild(i);
                if (layer.name == "BGDeselector") continue;
                if (!layer.gameObject.activeSelf) continue;

                CreateLayerItem(layer.gameObject);
            }
        }

        private void CreateLayerItem(GameObject layerObj)
        {
            GameObject item = new GameObject("LayerItem", typeof(RectTransform));
            item.transform.SetParent(layersListContainer.transform, false);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(230, 40);
            
            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(5, 5, 5, 5); hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true; hlg.childControlHeight = true; hlg.childForceExpandWidth = true;

            // Selection Highlight Image
            Image bgImg = item.AddComponent<Image>();
            bgImg.color = (currentSelection == layerObj) ? new Color(0.2f, 0.8f, 0.4f, 0.2f) : new Color(1, 1, 1, 0);
            item.AddComponent<Button>().onClick.AddListener(() => SelectObject(layerObj));

            // 1. Visibility Icon [V/H]
            GameObject eyeBtn = new GameObject("Eye", typeof(RectTransform), typeof(Image), typeof(Button));
            eyeBtn.transform.SetParent(item.transform, false);
            // CRITICAL: Prevent squashing
            LayoutElement leEye = eyeBtn.AddComponent<LayoutElement>();
            leEye.minWidth = 30; leEye.preferredWidth = 30; leEye.minHeight = 24;
            
            eyeBtn.GetComponent<Image>().color = layerObj.activeSelf ? new Color(0.2f, 0.8f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
            
            Text eyeTxt = new GameObject("Txt", typeof(RectTransform)).AddComponent<Text>();
            eyeTxt.transform.SetParent(eyeBtn.transform, false);
            UIFactory.Stretch(eyeTxt.rectTransform);
            eyeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            eyeTxt.text = layerObj.activeSelf ? "V" : "H";
            eyeTxt.alignment = TextAnchor.MiddleCenter; eyeTxt.color = Color.white; eyeTxt.fontSize = 11;
            
            eyeBtn.GetComponent<Button>().onClick.AddListener(() => {
                layerObj.SetActive(!layerObj.activeSelf);
                UpdateLayersPanel();
            });

            // 2. Layer Name
            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(item.transform, false);
            nameObj.AddComponent<LayoutElement>().flexibleWidth = 1; // Take remaining space
            Text nText = nameObj.GetComponent<Text>();
            nText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nText.text = layerObj.name;
            nText.color = Color.black; nText.fontSize = 12; nText.alignment = TextAnchor.MiddleLeft;

            // 3. Lock Icon [L/U]
            var manipulator = layerObj.GetComponent<ObjectManipulator>();
            bool isLocked = manipulator != null && manipulator.IsLocked;
            
            GameObject lockBtn = new GameObject("Lock", typeof(RectTransform), typeof(Image), typeof(Button));
            lockBtn.transform.SetParent(item.transform, false);
            // CRITICAL: Prevent squashing
            LayoutElement leLock = lockBtn.AddComponent<LayoutElement>();
            leLock.minWidth = 30; leLock.preferredWidth = 30; leLock.minHeight = 24;
            
            lockBtn.GetComponent<Image>().color = isLocked ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.4f, 0.6f, 1f);
            
            Text lockTxt = new GameObject("Txt", typeof(RectTransform)).AddComponent<Text>();
            lockTxt.transform.SetParent(lockBtn.transform, false);
            UIFactory.Stretch(lockTxt.rectTransform);
            lockTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lockTxt.text = isLocked ? "L" : "U";
            lockTxt.alignment = TextAnchor.MiddleCenter; lockTxt.color = Color.white; lockTxt.fontSize = 11;
            
            lockBtn.GetComponent<Button>().onClick.AddListener(() => {
                if (manipulator != null) {
                    manipulator.IsLocked = !manipulator.IsLocked;
                    if (manipulator.IsLocked && currentSelection == layerObj) Deselect();
                    UpdateLayersPanel();
                }
            });
        }

        public void SyncCraftModeUI(string mode)
        {
            if (craftModeContainer == null) return;
            foreach (Transform child in craftModeContainer.transform)
            {
                var outline = child.GetComponent<Outline>();
                if (outline != null)
                {
                    // Check if button text matches mode
                    var txt = child.GetComponentInChildren<Text>();
                    if (txt != null) outline.effectColor = (txt.text == mode) ? Color.green : Color.gray;
                }
            }
            OnCraftModeChanged(mode); // Ensure preview updates
        }

        public void OnUploadDepthMap()
        {
            #if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("Select Depth Map", "", "jpg,png,svg,webp");
            if (!string.IsNullOrEmpty(path))
            {
                if (currentSelection == null)
                {
                    ShowInfoPopup("请先选择一个图层再上传深度图。");
                    return;
                }

                Image img = currentSelection.GetComponent<Image>();
                if (img == null || img.sprite == null)
                {
                    ShowInfoPopup("当前图层不是图片图层，无法应用深度图。");
                    return;
                }

                // Expected size: sprite rect size in pixels
                int expectedW = Mathf.RoundToInt(img.sprite.rect.width);
                int expectedH = Mathf.RoundToInt(img.sprite.rect.height);

                byte[] data = System.IO.File.ReadAllBytes(path);
                Texture2D depthTex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                if (!depthTex.LoadImage(data))
                {
                    ShowInfoPopup("深度图加载失败。");
                    return;
                }

                // Must match current layer size
                if (depthTex.width != expectedW || depthTex.height != expectedH)
                {
                    ShowInfoPopup($"深度图尺寸不匹配：需要 {expectedW}x{expectedH}，当前 {depthTex.width}x{depthTex.height}。未应用。");
                    Destroy(depthTex);
                    return;
                }

                var ld = currentSelection.GetComponent<LayerData>();
                if (ld == null) ld = currentSelection.AddComponent<LayerData>();
                ld.customDepthMap = depthTex;
                ld.customDepthWidth = depthTex.width;
                ld.customDepthHeight = depthTex.height;

                Debug.Log("[CraftMode] Depth map applied: " + path);
                ShowInfoPopup("Depth Map 已应用: " + System.IO.Path.GetFileName(path));

                // Refresh previews if needed
                UpdateMiniPreview();
            }
            #else
            Debug.Log("[CraftMode] File browser triggered (Runtime simulation)");
            ShowInfoPopup("Opening System File Browser...");
            #endif
        }

        public static string GetUploadSupportedFormatsText()
        {
            return UploadSupportedFormatsText;
        }

        private static readonly HashSet<string> UnityNativeImageExtensions =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

        public void OnUploadCanvasAsset()
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel(
                "Upload Asset",
                "",
                "pdf,png,jpg,jpeg,gif,bmp,tif,tiff,webp,svg");
            if (string.IsNullOrEmpty(path)) return;
            ContinueUploadWithSelectedPath(path);
#else
            QtBridgeController bridge = GetQtBridgeController();
            if (bridge == null || !bridge.IsConnected)
            {
                ShowInfoPopup("Qt host not connected. Upload is available when running inside PocoStudio.");
                return;
            }
            string requestId = System.Guid.NewGuid().ToString("N");
            pendingUploadFileDialogRequestId = requestId;
            string filter = "PDF and Images (*.pdf *.png *.jpg *.jpeg *.gif *.bmp *.tif *.tiff *.webp *.svg);;All files (*)";
            if (!bridge.SendOpenFileDialogRequest(requestId, "Upload Asset", filter))
            {
                pendingUploadFileDialogRequestId = null;
                ShowInfoPopup("Failed to request file dialog from Qt host.");
            }
#endif
        }

        private void ContinueUploadWithSelectedPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension) || !SupportedUploadExtensions.Contains(extension))
            {
                ShowInfoPopup("Unsupported file type. Supported: " + UploadSupportedFormatsText);
                return;
            }

            QtBridgeController bridge = GetQtBridgeController();
            if (bridge != null && bridge.IsConnected)
            {
                bridge.OnConvertToPngResult -= OnQtConvertToPngResult;
                bridge.OnConvertToPngResult += OnQtConvertToPngResult;

                string requestId = System.Guid.NewGuid().ToString("N");
                pendingUploadRequestById[requestId] = path;
                bridge.SendConvertToPngRequest(requestId, path);
                return;
            }

            if (UnityNativeImageExtensions.Contains(extension))
            {
                LoadImageLocally(path);
                return;
            }

            Debug.LogWarning("[Upload] Qt bridge unavailable – cannot convert " + extension);
        }

        private void OnQtOpenFileDialogResult(string requestId, bool success, string filePath)
        {
            if (requestId != pendingUploadFileDialogRequestId) return;
            pendingUploadFileDialogRequestId = null;
            if (success && !string.IsNullOrEmpty(filePath))
                ContinueUploadWithSelectedPath(filePath);
        }

        private void LoadImageLocally(string filePath)
        {
            if (!TryLoadPngTextureFromFile(filePath, out Texture2D tex, out string err))
            {
                ShowInfoPopup(err);
                return;
            }
            AddUploadedListItem(filePath, tex);
            AddTextureToCanvas(filePath, tex);
        }

        private bool TryLoadPngTextureFromFile(string pngPath, out Texture2D outputTexture, out string error)
        {
            outputTexture = null;
            error = null;

            if (string.IsNullOrEmpty(pngPath) || !File.Exists(pngPath))
            {
                error = "Converted PNG file not found.";
                return false;
            }

            byte[] pngBytes;
            try
            {
                pngBytes = File.ReadAllBytes(pngPath);
            }
            catch (System.Exception ex)
            {
                error = "Failed to read file: " + ex.Message;
                return false;
            }

            // sRGB (linear=false): PNG/JPEG from file or Qt conversion are sRGB; linear=true would make display too bright/gray
            Texture2D pngTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!pngTexture.LoadImage(pngBytes))
            {
                Destroy(pngTexture);
                error = "Failed to load converted PNG.";
                return false;
            }

            outputTexture = pngTexture;
            return true;
        }

        private void AddUploadedListItem(string originalPath, Texture2D texture)
        {
            if (uploadListContainer == null || texture == null)
            {
                return;
            }
            var emptyHint = uploadListContainer.transform.Find("UploadEmptyHint");
            if (emptyHint != null) Destroy(emptyHint.gameObject);

            GameObject item = UIFactory.CreateObject("UploadItem", uploadListContainer);
            item.AddComponent<LayoutElement>().minHeight = 72;
            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(6, 6, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            GameObject thumb = UIFactory.CreateObject("Thumb", item);
            thumb.AddComponent<LayoutElement>().minWidth = 60;
            thumb.GetComponent<LayoutElement>().minHeight = 60;
            Image thumbImage = thumb.AddComponent<Image>();
            thumbImage.preserveAspect = true;
            thumbImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            GameObject info = UIFactory.CreateObject("Info", item);
            VerticalLayoutGroup ivlg = info.AddComponent<VerticalLayoutGroup>();
            ivlg.spacing = 2;
            ivlg.childAlignment = TextAnchor.MiddleLeft;
            info.AddComponent<LayoutElement>().minWidth = 180;

            string fileName = Path.GetFileName(originalPath);
            string ext = Path.GetExtension(originalPath);
            UIFactory.CreateText(fileName, info, 11, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.CreateText($"{texture.width}x{texture.height}  ({ext.ToUpperInvariant()})", info, 10, Color.gray, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Normal);
        }

        private void AddTextureToCanvas(string originalPath, Texture2D texture)
        {
            if (paper == null || texture == null)
            {
                return;
            }

            GameObject addedImg = UIFactory.CreateObject("Uploaded_" + Path.GetFileNameWithoutExtension(originalPath), paper.gameObject);
            RectTransform rt = addedImg.GetComponent<RectTransform>();
            Image image = addedImg.AddComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            image.preserveAspect = true;

            float maxSide = 260f;
            float srcW = texture.width;
            float srcH = texture.height;
            if (srcW <= 0 || srcH <= 0)
            {
                srcW = 200f;
                srcH = 200f;
            }

            float scale = Mathf.Min(maxSide / srcW, maxSide / srcH);
            if (scale > 1f) scale = 1f;
            rt.sizeDelta = new Vector2(srcW * scale, srcH * scale);
            rt.anchoredPosition = Vector2.zero;

            Modules.CanvasWorkspaceBuilder.AddManipulationComponents(addedImg);
            RecordAdd(addedImg);
            SelectObject(addedImg);
        }

        private QtBridgeController GetQtBridgeController()
        {
            if (qtBridgeController == null)
            {
                qtBridgeController = UnityEngine.Object.FindObjectOfType<QtBridgeController>();
            }
            return qtBridgeController;
        }

        private void OnQtConvertToPngResult(string requestId, bool success, string outputPngPath, string error)
        {
            if (string.IsNullOrEmpty(requestId)) return;
            if (!pendingUploadRequestById.TryGetValue(requestId, out string originalPath))
            {
                return;
            }
            pendingUploadRequestById.Remove(requestId);

            if (!success)
            {
                ShowInfoPopup(string.IsNullOrEmpty(error) ? "Convert failed." : error);
                return;
            }

            if (!TryLoadPngTextureFromFile(outputPngPath, out Texture2D pngTexture, out string loadError))
            {
                ShowInfoPopup(loadError);
                return;
            }

            AddUploadedListItem(originalPath, pngTexture);
            AddTextureToCanvas(originalPath, pngTexture);
        }

        public void ChangeMiniZoom(float delta)
        {
            currentMiniZoom = Mathf.Clamp(currentMiniZoom + delta, 0.1f, 10f);
            if (miniModelViewer != null)
            {
                miniModelViewer.SetCameraZoom(currentMiniZoom);
            }
        }

        /// <summary>
        /// Called by ObjectManipulator after dragging to update mini preview position.
        /// </summary>
        public void OnObjectMoved()
        {
            if (currentSelection != null && miniPreviewPanel != null && miniPreviewPanel.activeSelf)
            {
                UpdateMiniPreview();
            }
        }

        public void OnCraftModeChanged(string mode)
        {
            if (currentSelection != null)
            {
                var data = currentSelection.GetComponent<LayerData>();
                if (data == null) data = currentSelection.AddComponent<LayerData>();
                data.craftMode = mode;
            }

            // Mini preview for all parallax modes (including Customize Texture). Flat has no depth.
            bool hasMode = PocoRender.UI.TextureEffects.TextureModeUtil.TryParseCraftMode(mode, out var texMode);
            bool needsThumbnail = hasMode && PocoRender.UI.TextureEffects.TextureModeUtil.IsParallaxMode(texMode);
            bool needsUpload = (mode == "Customize Texture");

            if (miniPreviewPanel != null)
            {
                miniPreviewPanel.SetActive(needsThumbnail);
                if (needsThumbnail) 
                {
                    // USER REQ: Initial zoom at 1.2x (was 2.5x)
                    currentMiniZoom = 1.2f; 
                    UpdateMiniPreview();
                }
            }

            if (customizePanel != null)
            {
                customizePanel.SetActive(needsUpload);
            }
        }

        public void OnDownloadDepthImage()
        {
            if (currentSelection == null)
            {
                ShowInfoPopup("请先选择一个图层。");
                return;
            }

            var ld = currentSelection.GetComponent<LayerData>();
            string craftMode = ld != null ? ld.craftMode : null;
            if (!PocoRender.UI.TextureEffects.TextureModeUtil.TryParseCraftMode(craftMode, out var mode))
            {
                ShowInfoPopup("当前模式无深度图可导出。");
                return;
            }

            if (mode == PocoRender.UI.TextureEffects.TextureMode.Flat)
            {
                ShowInfoPopup("Flat 模式没有深度图。");
                return;
            }

            Image img = currentSelection.GetComponent<Image>();
            if (img == null || img.sprite == null)
            {
                ShowInfoPopup("当前图层不是图片图层，无法导出深度图。");
                return;
            }

            Texture2D depthTex = null;
            bool shouldDestroy = false;

            if (mode == PocoRender.UI.TextureEffects.TextureMode.CustomizeTexture && ld != null && ld.customDepthMap != null)
            {
                depthTex = ld.customDepthMap;
            }
            else
            {
                var spriteTex = PocoRender.UI.TextureEffects.SpriteTextureUtil.ExtractSpriteTexture(img.sprite, 0); // keep original size
                if (spriteTex == null)
                {
                    ShowInfoPopup("无法读取图层图片纹理。");
                    return;
                }
                depthTex = PocoRender.UI.TextureEffects.HeightMapGenerator.GenerateHeightMap(spriteTex, mode);
                shouldDestroy = true;
                Destroy(spriteTex);
            }

            if (depthTex == null)
            {
                ShowInfoPopup("深度图生成失败。");
                return;
            }

            byte[] png = depthTex.EncodeToPNG();
            if (shouldDestroy) Destroy(depthTex);

            #if UNITY_EDITOR
            string defaultName = $"DepthImage_{currentSelection.name}.png";
            string savePath = UnityEditor.EditorUtility.SaveFilePanel("Save Depth Image", "", defaultName, "png");
            if (!string.IsNullOrEmpty(savePath))
            {
                System.IO.File.WriteAllBytes(savePath, png);
                ShowInfoPopup("已保存: " + System.IO.Path.GetFileName(savePath));
            }
            #else
            // Runtime: no native file dialog without plugin; fallback to persistentDataPath
            string dir = Application.persistentDataPath;
            string path = System.IO.Path.Combine(dir, $"DepthImage_{currentSelection.name}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            System.IO.File.WriteAllBytes(path, png);
            ShowInfoPopup("已保存到: " + path);
            #endif
        }

        private void UpdateMiniPreview()
        {
            if (miniModelViewer == null) return;
            
            // Ensure preview panel is active so Model3DViewer can initialize
            if (miniPreviewPanel != null && !miniPreviewPanel.activeSelf)
            {
                miniPreviewPanel.SetActive(true);
            }

            // 1. Setup 3D Container - Find existing by name to prevent accumulation
            if (miniDesignStage != null) Object.DestroyImmediate(miniDesignStage);
            
            // Safety check for orphans in scene
            GameObject existing = GameObject.Find("MiniDesignStage");
            if (existing != null) Object.DestroyImmediate(existing);
            
            miniDesignStage = new GameObject("MiniDesignStage");
            
            GameObject container = new GameObject("Container");
            container.transform.SetParent(miniDesignStage.transform);

            // 2. Setup Paper & Content (Standard shader so it responds to lights)
            GameObject paperPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            paperPlane.transform.SetParent(container.transform);
            paperPlane.transform.localScale = new Vector3(6, 6, 1);
            paperPlane.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Material paperMat = SafeShaderHelper.CreateStandardMaterial();
            if (paperMat == null) paperMat = paperPlane.GetComponent<Renderer>().material;
            paperMat.color = Color.white;
            if (paperMat.HasProperty("_Glossiness")) paperMat.SetFloat("_Glossiness", 0.2f);
            if (paperMat.HasProperty("_Metallic")) paperMat.SetFloat("_Metallic", 0f);
            paperPlane.GetComponent<Renderer>().material = paperMat;

            // 3. Get camera and ensure it's initialized
            Camera cam = miniModelViewer.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                // Force initialization if camera doesn't exist
                if (miniModelViewer.targetImage != null)
                {
                    miniModelViewer.InitializeRenderer();
                    cam = miniModelViewer.GetComponentInChildren<Camera>();
                }
                if (cam == null)
                {
                    Debug.LogWarning("[CanvasController] Mini preview camera not found!");
                    return;
                }
            }
            
            // Ensure camera only renders the preview layer
            int previewLayer = miniModelViewer.gameObject.layer;
            miniModelViewer.renderLayer = 1 << previewLayer;
            cam.cullingMask = miniModelViewer.renderLayer;
            cam.nearClipPlane = 0.0001f;
            cam.farClipPlane = 10000f;

            GameObject worldCanvas = new GameObject("WorldCanvas");
            worldCanvas.transform.SetParent(container.transform);
            worldCanvas.transform.localRotation = Quaternion.Euler(90, 0, 0);
            worldCanvas.transform.localPosition = new Vector3(0, 0.05f, 0);
            worldCanvas.transform.localScale = Vector3.one * 0.01f;
            
            Canvas c = worldCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = cam; // Set after camera is confirmed
            worldCanvas.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1;
            
            // CRITICAL: Larger sizeDelta to prevent edge clipping
            RectTransform wcRt = worldCanvas.GetComponent<RectTransform>();
            wcRt.sizeDelta = new Vector2(650, 650); // Extra padding beyond 600mm canvas

            // Add white background as first child to ensure complete coverage
            GameObject bgObj = UIFactory.CreateObject("PaperBackground", worldCanvas);
            UIFactory.Stretch(bgObj.GetComponent<RectTransform>());
            // Fix: Use black background for transparency check, or white if simulating paper.
            // Since user reported "black background" issues with transparency, let's make sure
            // the paper background is clearly white to represent the substrate.
            bgObj.AddComponent<Image>().color = Color.white;

            // USER REQ: Only show current selection in mini preview
            if (currentSelection != null)
            {
                var ld = currentSelection.GetComponent<LayerData>();
                string craftMode = ld != null ? ld.craftMode : null;
                bool isTextureMode = TextureModeUtil.TryParseCraftMode(craftMode, out TextureMode texMode)
                                     && TextureModeUtil.IsParallaxMode(texMode);

                Image selImg = currentSelection.GetComponent<Image>();
                RectTransform selRt = currentSelection.GetComponent<RectTransform>();

                if (isTextureMode && selImg != null && selImg.sprite != null && selRt != null)
                {
                    // Build a parallax quad instead of a flat UI clone
                    GameObject meshRoot = new GameObject("MeshLayers");
                    meshRoot.transform.SetParent(container.transform, false);
                    meshRoot.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    meshRoot.transform.localPosition = new Vector3(0, 0.06f, 0);
                    meshRoot.transform.localScale = Vector3.one * 0.01f;

                    Texture2D depthOverride = null;
                    if (texMode == TextureMode.CustomizeTexture && ld != null && ld.customDepthMap != null)
                    {
                        depthOverride = ld.customDepthMap;
                    }
                    PreviewMeshBuilder.BuildImageLayerQuad(selImg, selRt, meshRoot.transform, 4f, texMode, depthOverride, 512);
                    SetLayerRecursive(meshRoot, previewLayer);
                }
                else
                {
                    // Create a lit 3D Quad instead of an unlit UI clone.
                    // UI/Default shader ignores scene lights entirely, so we use Standard shader
                    // on a 3D Quad so the spotlight sweep is visible on the content.
                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "ContentQuad";
                    quad.transform.SetParent(container.transform, false);
                    quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    // Increase Y offset to 0.1f to strictly avoid z-fighting/occlusion with Paper (0.05f)
                    quad.transform.localPosition = new Vector3(0, 0.1f, 0); 

                    // Size: convert UI pixel size to world units (paper is 6 units for 600px)
                    float worldScale = 6f / 600f;
                    float qw = selRt != null ? selRt.rect.width * worldScale : 2f;
                    float qh = selRt != null ? selRt.rect.height * worldScale : 2f;
                    quad.transform.localScale = new Vector3(qw, qh, 1f);

                    // Position offset from center (canvas Y up → preview: use +pos.y for Z so top on canvas = top in preview)
                    if (selRt != null)
                    {
                        Vector2 pos = selRt.anchoredPosition;
                        
                        quad.transform.localPosition = new Vector3(
                            pos.x * worldScale,
                            0.1f, // Match the height above
                            pos.y * worldScale 
                        );
                    }

                    Material mat = SafeShaderHelper.CreateStandardMaterial();
                    if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
                    if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.3f);
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

                    if (selImg != null && selImg.sprite != null && selImg.sprite.texture != null)
                    {
                        mat.mainTexture = selImg.sprite.texture;
                        mat.color = selImg.color;

                        // FIX: Configure Standard Shader for Transparency if texture has alpha
                        if (mat.shader.name == "Standard")
                        {
                            // Set to Fade mode
                            mat.SetFloat("_Mode", 2);
                            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            mat.SetInt("_ZWrite", 0);
                            mat.DisableKeyword("_ALPHATEST_ON");
                            mat.EnableKeyword("_ALPHABLEND_ON");
                            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            // Force RenderQueue higher than UI (3000) to ensure it draws ON TOP of the white paper background
                            mat.renderQueue = 3100;
                        }
                    }
                    else if (selImg != null)
                    {
                        // Pure color block
                        mat.color = selImg.color;
                    }
                    else
                    {
                        mat.color = Color.gray;
                    }
                    quad.GetComponent<Renderer>().material = mat;
                    SetLayerRecursive(quad, previewLayer);
                }
            }

            // 4. Lighting setup for mini preview
            // The scene uses Standard shader on white surfaces. With high ambient + directional,
            // surfaces are already at max brightness and spotlights add no visible effect.
            // Strategy: dim ambient so the base image is slightly muted, then a moving spotlight
            // brings a local area to full brightness -> visible texture detail highlight.
            Vector3 contentCenterLocal = new Vector3(0f, 0.06f, 0f);

            // Directional key light: base illumination
            GameObject keyLightObj = new GameObject("KeyLight");
            keyLightObj.transform.SetParent(miniDesignStage.transform);
            keyLightObj.transform.rotation = Quaternion.Euler(50, 30, 0);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = Color.white;
            keyLight.intensity = 0.55f;
            keyLight.cullingMask = -1; // Affect all layers
            SetLayerRecursive(keyLightObj, previewLayer);

            // Moving spotlight: aims straight down, spot moves with the light orbit.
            // Height 8, spotAngle 10° → diameter ≈ 2*8*tan(5°) ≈ 1.4 units (~1/20 of 6x6 paper)
            GameObject lightObj = new GameObject("MovingLight");
            lightObj.transform.SetParent(miniDesignStage.transform);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 25f;
            l.spotAngle = 10f;
            l.innerSpotAngle = 6f;
            l.intensity = 2.5f;
            l.color = new Color(1f, 0.99f, 0.97f);
            l.shadows = LightShadows.None;
            l.cullingMask = -1; // Affect all layers
            lightObj.transform.localPosition = new Vector3(0, 8, 0);
            lightObj.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            SetLayerRecursive(lightObj, previewLayer);
            var mle = lightObj.AddComponent<MovingLightEffect>();
            mle.surfaceY = contentCenterLocal.y;
            mle.orbitRadius = 2.0f;
            mle.orbitHeight = 8f;
            mle.orbitSpeed = 0.4f;
            mle.SetTargetViewer(miniModelViewer);

            miniModelViewer.modelContainer = miniDesignStage;
            miniModelViewer.SetModel(miniDesignStage);

            // Disable Model3DViewer's own SceneLight — it's too bright (1.2) and drowns
            // out the moving spotlight. We use our own KeyLight + MovingLight instead.
            if (miniModelViewer.sceneLight != null)
            {
                miniModelViewer.sceneLight.enabled = false;
            }

            // Low ambient so the spotlight sweep creates visible contrast
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            miniModelViewer.RequestRender(5);
        }

        public void ZoomToFit()
        {
            SetZoom(1.1f);
            if (paper != null) paper.anchoredPosition = Vector2.zero;
        }

        public void ToggleHandTool(bool active)
        {
            handToolActive = active;
        }

        public bool IsHandToolActive() => handToolActive;

        void Update()
        {
            // 1. Delete Selection
            if (currentSelection != null && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)))
            {
                RecordDelete(currentSelection);
                Deselect();
            }

            // 2. Debounced mini preview rebuild after window resize stops.
            // We only re-initialize the RenderTexture (not the whole scene) to avoid flash.
            if (resizeDebounceTimer > 0f)
            {
                resizeDebounceTimer -= Time.unscaledDeltaTime;
                if (resizeDebounceTimer <= 0f)
                {
                    resizeDebounceTimer = -1f;
                    if (miniModelViewer != null && miniPreviewPanel != null && miniPreviewPanel.activeSelf)
                    {
                        // Just refresh the RenderTexture size, keeping the scene intact
                        miniModelViewer.InitializeRenderer();
                    }
                }
            }
        }

        void Start()
        {
            // Subscribe to window resize so mini preview and 3D viewer adapt to new size
            var handler = GetComponentInParent<Canvas>()?.GetComponent<WindowResizeHandler>();
            if (handler == null) handler = FindObjectOfType<WindowResizeHandler>();
            if (handler != null)
            {
                handler.OnWindowResized += RefreshMiniPreviewOnResize;
            }
        }

        void OnEnable()
        {
            QtBridgeController bridge = GetQtBridgeController();
            if (bridge != null)
            {
                bridge.OnConvertToPngResult -= OnQtConvertToPngResult;
                bridge.OnConvertToPngResult += OnQtConvertToPngResult;
                bridge.OnOpenFileDialogResult -= OnQtOpenFileDialogResult;
                bridge.OnOpenFileDialogResult += OnQtOpenFileDialogResult;
            }

            // USER REQ: Re-initialize mini preview when returning to editor
            if (currentSelection != null)
            {
                var data = currentSelection.GetComponent<LayerData>();
                if (data != null)
                {
                    OnCraftModeChanged(data.craftMode);
                }
            }
        }

        void OnDestroy()
        {
            var handler = GetComponentInParent<Canvas>()?.GetComponent<WindowResizeHandler>();
            if (handler == null) handler = FindObjectOfType<WindowResizeHandler>();
            if (handler != null)
            {
                handler.OnWindowResized -= RefreshMiniPreviewOnResize;
            }
            var bridge = GetQtBridgeController();
            if (bridge != null)
            {
                bridge.OnOpenFileDialogResult -= OnQtOpenFileDialogResult;
            }
        }

        private float resizeDebounceTimer = -1f;
        private const float RESIZE_DEBOUNCE_DELAY = 0.4f;

        private void RefreshMiniPreviewOnResize()
        {
            if (miniPreviewPanel == null || !miniPreviewPanel.activeSelf) return;
            // Reset timer on each resize frame; rebuild only fires once resizing stops.
            resizeDebounceTimer = RESIZE_DEBOUNCE_DELAY;
        }

        void OnDisable()
        {
            QtBridgeController bridge = GetQtBridgeController();
            if (bridge != null)
            {
                bridge.OnConvertToPngResult -= OnQtConvertToPngResult;
            }
            pendingUploadRequestById.Clear();

            // Clean up mini preview objects to prevent accumulation in Hierarchy
            if (miniDesignStage != null)
            {
                Object.DestroyImmediate(miniDesignStage);
                miniDesignStage = null;
            }
            if (miniModelViewer != null)
            {
                miniModelViewer.SetModel(null);
            }
        }
        #endregion

        private void CreateRotationHandle()
        {
            if (rotationHandle != null) Destroy(rotationHandle);
            
            rotationHandle = new GameObject("RotationHandle");
            rotationHandle.transform.SetParent(currentSelection.transform, false);
            
            Image img = rotationHandle.AddComponent<Image>();
            Sprite rotateSprite = Resources.Load<Sprite>("EditIcons/p_rotate_img");
            if (rotateSprite != null)
            {
                img.sprite = rotateSprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.color = Color.green;
            }
            
            RectTransform rt = rotationHandle.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(24, 24);
            float yPos = -(currentSelection.GetComponent<RectTransform>().rect.height / 2f) - 30f;
            rt.anchoredPosition = new Vector2(0, yPos);
            
            RotationHandler handler = rotationHandle.AddComponent<RotationHandler>();
            handler.target = currentSelection.GetComponent<RectTransform>();
            handler.controller = this;
        }

        private void DestroyRotationHandle()
        {
            if (rotationHandle != null) Destroy(rotationHandle);
        }

        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }

    /// <summary>
    /// Orbiting spotlight that aims straight down. Updates at a low frequency
    /// and triggers on-demand rendering to avoid continuous GPU load.
    /// </summary>
    public class MovingLightEffect : MonoBehaviour
    {
        public float surfaceY = 0.06f;
        public float orbitRadius = 2f;
        public float orbitHeight = 12f;
        public float orbitSpeed = 0.4f;

        [Tooltip("How many times per second the light updates and requests a render")]
        public float updatesPerSecond = 8f;

        private float startTime;
        private float nextUpdateTime;
        private Model3DViewer targetViewer;

        void Start()
        {
            startTime = Time.time;
            targetViewer = GetComponentInParent<Model3DViewer>();
            if (targetViewer == null)
                targetViewer = FindObjectOfType<Model3DViewer>();
        }

        public void SetTargetViewer(Model3DViewer viewer)
        {
            targetViewer = viewer;
        }

        void Update()
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + (updatesPerSecond > 0 ? 1f / updatesPerSecond : 0.125f);

            float t = (Time.time - startTime) * orbitSpeed;
            float x = Mathf.Sin(t) * orbitRadius;
            float z = Mathf.Cos(t * 0.73f) * orbitRadius * 0.85f;
            transform.localPosition = new Vector3(x, orbitHeight, z);

            if (transform.parent != null)
            {
                Vector3 groundBelow = transform.parent.TransformPoint(new Vector3(x, surfaceY, z));
                transform.LookAt(groundBelow);
            }

            if (targetViewer != null) targetViewer.RequestRender(1);
        }
    }
}

