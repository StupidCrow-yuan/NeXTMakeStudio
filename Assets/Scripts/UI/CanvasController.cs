using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using PocoRender.UI.Core;
using PocoRender.UI.TextureEffects;
using PocoRender.Utils;
using PocoRender.Communication;

namespace PocoRender.UI
{
    public class CanvasController : MonoBehaviour
    {
        public GameObject contextToolbar;
        public GameObject cropOptionsPanel;
        public GameObject eraserOptionsPanel;
        public GameObject opacityOptionsPanel;
        public Slider opacitySlider;
        public GameObject splitOptionsPanel;
        public InputField splitColsInput;
        public InputField splitRowsInput;
        public Text splitInfoText;
        public GameObject adjustmentPanel;
        public Slider[] adjustmentSliders;
        public Text[] adjustmentValueTexts;
        private Texture2D adjustmentOriginalTexture;
        private Sprite adjustmentOriginalSprite;
        private float adjustmentDebounceTimer = -1f;
        private const float ADJUSTMENT_DEBOUNCE = 0.12f;
        public GameObject leftDrawer;
        private string lastDrawerPanel = "Templates";
        
        // Position Info Fields (editable)
        public InputField posXInput;
        public InputField posYInput;
        public InputField widthInput;
        public InputField heightInput;
        public InputField rotationInput;
        private bool isUpdatingPositionUI = false;
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
        public GameObject uploadSelectionBar;
        public Text uploadSelectionCountText;
        public Image uploadBarCheckImage;
        private List<string> uploadedImagePaths = new List<string>();
        private HashSet<string> uploadSelectedPaths = new HashSet<string>();
        private static string UploadCachePath => Path.Combine(Application.persistentDataPath, "UploadCache");
        private static string UploadManifestPath => Path.Combine(Application.persistentDataPath, "UploadCache", "manifest.txt");
        
        private GameObject currentSelection;
        private GameObject rotationHandle;
        private GameObject selectionFrame;
        private Outline currentOutline;
        private CropToolSession activeCropSession;
        private EraserToolSession activeEraserSession;

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

        public void RecordResize(RectTransform rt,
                                 Vector2 oldSize, Vector2 newSize,
                                 Vector2 oldPos, Vector2 newPos)
        {
            commandHistory.AddToHistory(new ResizeCommand(
                rt, oldSize, newSize, oldPos, newPos,
                () =>
                {
                    UpdatePositionInfo();
                    OnObjectMoved();
                }));
        }

        public void RecordCrop(CropCommand cmd)
        {
            commandHistory.AddToHistory(cmd);
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
            if (currentOutline != null) currentOutline.enabled = false;
            
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
            CreateSelectionFrame();
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
            UpdateLayersPanel();
        }

        public void Deselect()
        {
            if (activeCropSession != null)
            {
                activeCropSession.CancelCrop();
                activeCropSession = null;
            }
            if (cropOptionsPanel != null) cropOptionsPanel.SetActive(false);

            if (activeEraserSession != null)
            {
                activeEraserSession.ExitErase();
                activeEraserSession = null;
            }
            if (eraserOptionsPanel != null) eraserOptionsPanel.SetActive(false);
            if (opacityOptionsPanel != null) opacityOptionsPanel.SetActive(false);
            if (splitOptionsPanel != null) splitOptionsPanel.SetActive(false);
            if (adjustmentPanel != null && adjustmentPanel.activeSelf) CloseAdjustmentPanel();

            if (currentSelection != null)
            {
                if (currentOutline != null) currentOutline.enabled = false;
                DestroySelectionFrame();
                DestroyRotationHandle();
            }

            currentSelection = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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

        public void ToggleCropTool()
        {
            if (currentSelection == null)
            {
                ShowInfoPopup("Select an image layer first");
                return;
            }

            Image selectedImage = currentSelection.GetComponent<Image>();
            if (selectedImage == null || selectedImage.sprite == null)
            {
                ShowInfoPopup("Crop only works on image layers");
                return;
            }

            if (activeCropSession != null && activeCropSession.IsFor(currentSelection))
            {
                bool nextState = cropOptionsPanel == null || !cropOptionsPanel.activeSelf;
                if (cropOptionsPanel != null) cropOptionsPanel.SetActive(nextState);
                if (!nextState)
                {
                    CancelCropTool();
                }
                return;
            }

            if (activeEraserSession != null)
                CancelEraserTool();
            if (opacityOptionsPanel != null) opacityOptionsPanel.SetActive(false);
            if (splitOptionsPanel != null) splitOptionsPanel.SetActive(false);

            StartCropTool();
        }

        public void SetCropPreset(CropPresetType preset)
        {
            if (activeCropSession == null)
            {
                StartCropTool();
            }

            activeCropSession?.SetPreset(preset);
        }

        public void ApplyCropTool()
        {
            if (activeCropSession == null) return;
            activeCropSession.ApplyCrop();
            activeCropSession = null;
            if (cropOptionsPanel != null) cropOptionsPanel.SetActive(false);
            if (currentSelection != null)
            {
                CreateSelectionFrame();
                CreateRotationHandle();
                UpdatePositionInfo();
                UpdateLayersPanel();
            }
        }

        public void CancelCropTool()
        {
            if (activeCropSession != null)
            {
                activeCropSession.CancelCrop();
                activeCropSession = null;
            }

            if (cropOptionsPanel != null) cropOptionsPanel.SetActive(false);
            if (currentSelection != null)
            {
                CreateSelectionFrame();
                CreateRotationHandle();
                UpdatePositionInfo();
            }
        }

        private void StartCropTool()
        {
            if (activeCropSession != null)
            {
                activeCropSession.CancelCrop();
                activeCropSession = null;
            }

            DestroySelectionFrame();
            DestroyRotationHandle();

            activeCropSession = CropToolSession.Create(this, currentSelection);
            activeCropSession.SetPreset(CropPresetType.Free);
            if (cropOptionsPanel != null) cropOptionsPanel.SetActive(true);
        }

        // ---- Eraser Tool ----

        public void ToggleEraserTool()
        {
            if (currentSelection == null)
            {
                ShowInfoPopup("Select an image layer first");
                return;
            }

            Image selectedImage = currentSelection.GetComponent<Image>();
            if (selectedImage == null || selectedImage.sprite == null)
            {
                ShowInfoPopup("Selected layer has no image");
                return;
            }

            if (activeEraserSession != null && activeEraserSession.IsFor(currentSelection))
            {
                ExitEraserTool();
                return;
            }

            if (activeCropSession != null)
                CancelCropTool();
            if (opacityOptionsPanel != null) opacityOptionsPanel.SetActive(false);
            if (splitOptionsPanel != null) splitOptionsPanel.SetActive(false);

            StartEraserTool();
        }

        public void SetEraserBrushSize(int size)
        {
            if (activeEraserSession != null)
                activeEraserSession.SetBrushSize(size);
        }

        public void ExitEraserTool()
        {
            if (activeEraserSession != null)
            {
                activeEraserSession.ExitErase();
                activeEraserSession = null;
            }
            if (eraserOptionsPanel != null) eraserOptionsPanel.SetActive(false);
            if (currentSelection != null)
            {
                CreateSelectionFrame();
                CreateRotationHandle();
                UpdatePositionInfo();
                UpdateLayersPanel();
            }
        }

        public void CancelEraserTool()
        {
            if (activeEraserSession != null)
            {
                activeEraserSession.CancelErase();
                activeEraserSession = null;
            }

            if (eraserOptionsPanel != null) eraserOptionsPanel.SetActive(false);
            if (currentSelection != null)
            {
                CreateSelectionFrame();
                CreateRotationHandle();
                UpdatePositionInfo();
            }
        }

        private void StartEraserTool()
        {
            if (activeEraserSession != null)
            {
                activeEraserSession.CancelErase();
                activeEraserSession = null;
            }

            DestroySelectionFrame();
            DestroyRotationHandle();

            activeEraserSession = EraserToolSession.Create(this, currentSelection);
            if (eraserOptionsPanel != null) eraserOptionsPanel.SetActive(true);
        }

        public void RecordErase(EraseCommand cmd)
        {
            commandHistory.AddToHistory(cmd);
        }

        // ---- Opacity Tool ----

        public void ToggleOpacityTool()
        {
            if (currentSelection == null)
            {
                ShowInfoPopup("Select an image layer first");
                return;
            }

            if (opacityOptionsPanel == null) return;

            bool show = !opacityOptionsPanel.activeSelf;

            if (cropOptionsPanel != null) cropOptionsPanel.SetActive(false);
            if (eraserOptionsPanel != null) eraserOptionsPanel.SetActive(false);
            if (splitOptionsPanel != null) splitOptionsPanel.SetActive(false);

            opacityOptionsPanel.SetActive(show);

            if (show && opacitySlider != null)
            {
                opacitySlider.value = Mathf.RoundToInt(GetCurrentLayerOpacity() * 100f);
            }
        }

        public float GetCurrentLayerOpacity()
        {
            if (currentSelection == null) return 1f;
            Image img = currentSelection.GetComponent<Image>();
            if (img == null) return 1f;
            return img.color.a;
        }

        public void SetLayerOpacity(float alpha)
        {
            if (currentSelection == null) return;
            Image img = currentSelection.GetComponent<Image>();
            if (img == null) return;

            Color c = img.color;
            c.a = Mathf.Clamp01(alpha);
            img.color = c;
        }

        // ---- Image Split Tool ----

        public void ToggleSplitTool()
        {
            if (currentSelection == null)
            {
                ShowInfoPopup("Select an image layer first");
                return;
            }

            if (splitOptionsPanel == null) return;
            bool show = !splitOptionsPanel.activeSelf;

            if (activeCropSession != null) CancelCropTool();
            if (activeEraserSession != null) { ExitEraserTool(); }
            if (cropOptionsPanel != null) cropOptionsPanel.SetActive(false);
            if (eraserOptionsPanel != null) eraserOptionsPanel.SetActive(false);
            if (opacityOptionsPanel != null) opacityOptionsPanel.SetActive(false);

            splitOptionsPanel.SetActive(show);
            if (show) UpdateSplitInfo();
        }

        public void UpdateSplitInfo()
        {
            if (splitInfoText == null || currentSelection == null) return;
            int cols = ParseSplitInput(splitColsInput, 2);
            int rows = ParseSplitInput(splitRowsInput, 2);
            if (cols < 1 || rows < 1) { splitInfoText.text = "Each Slice: --"; return; }

            RectTransform selRt = currentSelection.GetComponent<RectTransform>();
            if (selRt == null) { splitInfoText.text = "Each Slice: --"; return; }

            float sliceW = selRt.sizeDelta.x / cols;
            float sliceH = selRt.sizeDelta.y / rows;
            splitInfoText.text = $"Each Slice: {sliceW:F1}(W) x {sliceH:F1}(H)";
        }

        public void ApplySplitTool()
        {
            if (currentSelection == null) return;
            int cols = ParseSplitInput(splitColsInput, 2);
            int rows = ParseSplitInput(splitRowsInput, 2);
            if (cols < 1 || rows < 1 || (cols == 1 && rows == 1)) return;

            Image srcImage = currentSelection.GetComponent<Image>();
            if (srcImage == null || srcImage.sprite == null) return;

            RectTransform srcRt = currentSelection.GetComponent<RectTransform>();
            Vector2 srcSize = srcRt.sizeDelta;
            Vector2 srcPos = srcRt.anchoredPosition;

            Texture2D srcTex = TextureEffects.SpriteTextureUtil.ExtractSpriteTexture(srcImage.sprite, 0);
            if (srcTex == null) return;

            float gap = 6f;
            float totalGapX = (cols - 1) * gap;
            float totalGapY = (rows - 1) * gap;
            float pieceW = (srcSize.x - totalGapX) / cols;
            float pieceH = (srcSize.y - totalGapY) / rows;

            GameObject sourceObj = currentSelection;
            Deselect();
            sourceObj.SetActive(false);

            var pieces = new System.Collections.Generic.List<GameObject>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int texX = Mathf.RoundToInt((float)c / cols * srcTex.width);
                    int texX2 = Mathf.RoundToInt((float)(c + 1) / cols * srcTex.width);
                    int texY = Mathf.RoundToInt((float)(rows - 1 - r) / rows * srcTex.height);
                    int texY2 = Mathf.RoundToInt((float)(rows - r) / rows * srcTex.height);
                    int tw = Mathf.Max(1, texX2 - texX);
                    int th = Mathf.Max(1, texY2 - texY);

                    Color[] pixels = srcTex.GetPixels(texX, texY, tw, th);
                    Texture2D pieceTex = new Texture2D(tw, th, TextureFormat.RGBA32, false, true);
                    pieceTex.SetPixels(pixels);
                    pieceTex.Apply();

                    Sprite pieceSpr = Sprite.Create(pieceTex,
                        new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

                    float px = srcPos.x - srcSize.x * 0.5f + c * (pieceW + gap) + pieceW * 0.5f;
                    float py = srcPos.y + srcSize.y * 0.5f - r * (pieceH + gap) - pieceH * 0.5f;

                    string pieceName = $"Split_{r}_{c}";
                    GameObject pieceObj = Core.UIFactory.CreateObject(pieceName, paper.gameObject);
                    RectTransform pRt = pieceObj.GetComponent<RectTransform>();
                    pRt.sizeDelta = new Vector2(pieceW, pieceH);
                    pRt.anchoredPosition = new Vector2(px, py);

                    Image pieceImg = pieceObj.AddComponent<Image>();
                    pieceImg.sprite = pieceSpr;
                    pieceImg.preserveAspect = false;

                    Modules.CanvasWorkspaceBuilder.AddManipulationComponents(pieceObj);
                    pieces.Add(pieceObj);
                }
            }

            var splitCmd = new SplitCommand(sourceObj, pieces, () =>
            {
                UpdateLayersPanel();
                if (globalInfoPanel != null && globalInfoPanel.activeSelf) UpdatePrintAreaList();
            });
            commandHistory.AddToHistory(splitCmd);

            if (splitOptionsPanel != null) splitOptionsPanel.SetActive(false);
            UpdateLayersPanel();
        }

        private int ParseSplitInput(InputField field, int fallback)
        {
            if (field == null) return fallback;
            int val;
            if (int.TryParse(field.text, out val) && val >= 1 && val <= 20)
                return val;
            return fallback;
        }

        /// <summary>
        /// AI Remove: U2-Net (Resources/U2NetSettings + u2net.onnx) or built-in corner color.
        /// </summary>
        public void ApplyAIRemoveBackground()
        {
            if (currentSelection == null) return;
            Image img = currentSelection.GetComponent<Image>();
            if (img == null || img.sprite == null)
            {
                ShowInfoPopup("Select an image layer first");
                return;
            }

            var u2Settings = TextureEffects.U2NetSettings.Load();
            if (u2Settings != null && u2Settings.IsValid)
            {
                TextureEffects.U2NetBackgroundRemoval.Instance.Configure(u2Settings);
                if (TextureEffects.U2NetBackgroundRemoval.Instance.IsReady)
                {
                    StartCoroutine(ApplyAIRemoveWithLoading(img, true));
                    return;
                }
#if HAS_SENTIS
                UnityEngine.Debug.LogWarning("[AI Remove] U2-Net worker not ready. See Console for [U2Net] messages.");
#endif
            }
#if HAS_SENTIS
            else if (u2Settings == null)
                UnityEngine.Debug.LogWarning("[AI Remove] U2NetSettings not found. It is auto-created at Resources/Models/U2NetSettings.asset on load; assign u2net ModelAsset to Base Onnx Model.");
#endif

            StartCoroutine(ApplyAIRemoveWithLoading(img, false));
        }

        private GameObject aiRemoveLoadingOverlay;

        private IEnumerator ApplyAIRemoveWithLoading(Image img, bool useU2Net)
        {
            // Show loading overlay
            aiRemoveLoadingOverlay = CreateLoadingOverlay("AI Remove - Processing...");
            yield return null; // let it render one frame

            if (useU2Net)
                ApplyAIRemoveBackgroundU2Net(img);
            else
                ApplyAIRemoveBackgroundBuiltIn(img);

            // Destroy loading overlay
            if (aiRemoveLoadingOverlay != null)
            {
                Destroy(aiRemoveLoadingOverlay);
                aiRemoveLoadingOverlay = null;
            }
        }

        private GameObject CreateLoadingOverlay(string message)
        {
            GameObject parent = editorArea != null ? editorArea : gameObject;

            GameObject overlay = Core.UIFactory.CreateObject("AIRemoveLoading", parent);
            Core.UIFactory.Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.4f);
            overlay.transform.SetAsLastSibling();

            GameObject panel = Core.UIFactory.CreateObject("LoadingPanel", overlay);
            RectTransform pRt = panel.GetComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(280, 100);
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = Color.white;
            Sprite cardSprite = Core.UIFactory.CreateRoundedRectSprite(128, 64, 12);
            if (cardSprite != null) { panelBg.sprite = cardSprite; panelBg.type = Image.Type.Sliced; }
            panel.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.85f, 0.85f, 0.85f);

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            Core.UIFactory.CreateText(message, panel, 14, new Color(0.2f, 0.2f, 0.2f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);

            return overlay;
        }

        private void ApplyAIRemoveBackgroundU2Net(Image img)
        {
            Texture2D srcTex = TextureEffects.SpriteTextureUtil.ExtractSpriteTexture(img.sprite, 0);
            if (srcTex == null)
            {
                ApplyAIRemoveBackgroundBuiltIn(img);
                return;
            }

            Texture2D resultTex = TextureEffects.U2NetBackgroundRemoval.Instance.RemoveBackground(srcTex);
            if (resultTex == null)
            {
                ApplyAIRemoveBackgroundBuiltIn(img);
                return;
            }

            Sprite oldSprite = img.sprite;
            Sprite newSprite = Sprite.Create(resultTex, new Rect(0, 0, resultTex.width, resultTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            img.sprite = newSprite;
            img.preserveAspect = true;
            img.color = Color.white;
            var cmd = new EraseCommand(img, oldSprite, newSprite, null);
            commandHistory.AddToHistory(cmd);
        }

        private void ApplyAIRemoveBackgroundBuiltIn(Image img)
        {
            Texture2D srcTex = TextureEffects.SpriteTextureUtil.ExtractSpriteTexture(img.sprite, 0);
            if (srcTex == null) return;

            int w = srcTex.width;
            int h = srcTex.height;
            Color[] pixels = srcTex.GetPixels();

            Color c00 = pixels[0];
            Color c10 = pixels[(h - 1) * w];
            Color c01 = pixels[w - 1];
            Color c11 = pixels[h * w - 1];
            Color bg = (c00 + c10 + c01 + c11) * 0.25f;

            float tolerance = 0.38f;
            float toleranceSq = tolerance * tolerance;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float dr = c.r - bg.r, dg = c.g - bg.g, db = c.b - bg.b;
                float distSq = dr * dr + dg * dg + db * db;
                float a = c.a;
                if (distSq <= toleranceSq)
                    a = 0f;
                else
                {
                    float dist = Mathf.Sqrt(distSq);
                    if (dist < tolerance * 1.5f)
                        a *= Mathf.Clamp01((dist - tolerance) / (tolerance * 0.5f));
                }
                pixels[i] = new Color(c.r, c.g, c.b, a);
            }

            Texture2D resultTex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            resultTex.SetPixels(pixels);
            resultTex.Apply();

            Sprite oldSprite = img.sprite;
            Sprite newSprite = Sprite.Create(resultTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            img.sprite = newSprite;
            img.preserveAspect = true;
            img.color = Color.white;

            var cmd = new EraseCommand(img, oldSprite, newSprite, null);
            commandHistory.AddToHistory(cmd);

        }

        // ---- Adjustment Panel ----

        public void ToggleAdjustmentPanel()
        {
            if (adjustmentPanel == null) return;

            bool show = !adjustmentPanel.activeSelf;

            if (show)
            {
                if (currentSelection == null)
                {
                    ShowInfoPopup("Select an image layer first");
                    return;
                }

                if (cropOptionsPanel != null) cropOptionsPanel.SetActive(false);
                if (eraserOptionsPanel != null) eraserOptionsPanel.SetActive(false);
                if (opacityOptionsPanel != null) opacityOptionsPanel.SetActive(false);
                if (splitOptionsPanel != null) splitOptionsPanel.SetActive(false);

                Image img = currentSelection.GetComponent<Image>();
                if (img == null || img.sprite == null) return;

                adjustmentOriginalTexture = TextureEffects.SpriteTextureUtil.ExtractSpriteTexture(img.sprite, 0);
                adjustmentOriginalSprite = img.sprite;

                for (int i = 0; i < adjustmentSliders.Length; i++)
                {
                    adjustmentSliders[i].value = 0;
                    adjustmentValueTexts[i].text = "0";
                }

                if (leftDrawer != null && adjustmentPanel.transform.parent != leftDrawer.transform)
                    adjustmentPanel.transform.SetParent(leftDrawer.transform, false);

                if (leftDrawer != null)
                {
                    foreach (Transform child in leftDrawer.transform)
                        if (child.gameObject != adjustmentPanel) child.gameObject.SetActive(false);
                }
                adjustmentPanel.SetActive(true);
            }
            else
            {
                CloseAdjustmentPanel();
            }
        }

        public void CloseAdjustmentPanel()
        {
            if (adjustmentPanel != null) adjustmentPanel.SetActive(false);
            adjustmentOriginalTexture = null;
            adjustmentOriginalSprite = null;

            if (leftDrawer != null)
            {
                foreach (Transform child in leftDrawer.transform)
                {
                    if (child.gameObject != adjustmentPanel)
                        child.gameObject.SetActive(true);
                }
            }
        }

        public void RestoreAdjustments()
        {
            if (adjustmentSliders == null) return;
            for (int i = 0; i < adjustmentSliders.Length; i++)
            {
                adjustmentSliders[i].value = 0;
                adjustmentValueTexts[i].text = "0";
            }
            ApplyAdjustments();
        }

        public void ScheduleAdjustment()
        {
            adjustmentDebounceTimer = ADJUSTMENT_DEBOUNCE;
        }

        public void ApplyAdjustments()
        {
            if (currentSelection == null || adjustmentOriginalTexture == null) return;
            Image img = currentSelection.GetComponent<Image>();
            if (img == null) return;

            float brightness = adjustmentSliders[0].value / 100f;  // -1 to +1
            float contrast   = adjustmentSliders[1].value / 100f;
            float saturation = adjustmentSliders[2].value / 100f;
            float hueShift   = adjustmentSliders[3].value / 200f;  // -0.5 to +0.5 (±180°)
            float temperature= adjustmentSliders[4].value / 100f;
            float tint       = adjustmentSliders[5].value / 100f;
            float highlights  = adjustmentSliders[6].value / 100f;
            float shadows     = adjustmentSliders[7].value / 100f;
            float sharpness   = adjustmentSliders[8].value / 100f;  // 0 to 1

            int w = adjustmentOriginalTexture.width;
            int h = adjustmentOriginalTexture.height;
            Color[] srcPixels = adjustmentOriginalTexture.GetPixels();
            Color[] dstPixels = new Color[srcPixels.Length];

            // Brightness: gamma-style curve. +100 → bright, -100 → dark, no fog.
            // gamma = 1/(1 + brightness) for positive, (1 - brightness) for negative
            float gamma;
            if (brightness >= 0f)
                gamma = 1f / (1f + brightness * 2f);  // +100 → gamma 0.33 (brighter)
            else
                gamma = 1f - brightness * 2f;          // -100 → gamma 3.0 (darker)

            // Contrast: stronger curve using pow. +100 → very punchy, -100 → flat gray
            // factor: at 0→1, at +1→3, at -1→0.33
            float contrastFactor = Mathf.Pow(3f, contrast);

            float satMul = 1f + saturation * 2f;  // -100→-1(desat), 0→1(normal), +100→3(vivid)

            for (int i = 0; i < srcPixels.Length; i++)
            {
                Color c = srcPixels[i];
                float r = c.r, g = c.g, b = c.b, a = c.a;

                // Brightness via gamma curve - preserves blacks, no fog
                if (Mathf.Abs(brightness) > 0.001f)
                {
                    r = Mathf.Pow(Mathf.Max(0f, r), gamma);
                    g = Mathf.Pow(Mathf.Max(0f, g), gamma);
                    b = Mathf.Pow(Mathf.Max(0f, b), gamma);
                }

                // Contrast (pivot at mid-gray 0.5)
                if (Mathf.Abs(contrast) > 0.001f)
                {
                    r = (r - 0.5f) * contrastFactor + 0.5f;
                    g = (g - 0.5f) * contrastFactor + 0.5f;
                    b = (b - 0.5f) * contrastFactor + 0.5f;
                }

                // Saturation (lerp toward/away from luminance)
                if (Mathf.Abs(saturation) > 0.001f)
                {
                    float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                    r = lum + (r - lum) * satMul;
                    g = lum + (g - lum) * satMul;
                    b = lum + (b - lum) * satMul;
                }

                // Hue rotation (±180°)
                if (Mathf.Abs(hueShift) > 0.001f)
                {
                    float hVal, sVal, vVal;
                    Color.RGBToHSV(new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b)),
                        out hVal, out sVal, out vVal);
                    hVal = ((hVal + hueShift) % 1f + 1f) % 1f;
                    Color hsv = Color.HSVToRGB(hVal, sVal, vVal);
                    r = hsv.r; g = hsv.g; b = hsv.b;
                }

                // Temperature: positive = warm (more orange), negative = cool (more blue)
                if (Mathf.Abs(temperature) > 0.001f)
                {
                    float t = temperature * 0.2f;
                    r += t;
                    g += t * 0.1f;
                    b -= t;
                }

                // Tint: positive = magenta/pink, negative = green
                if (Mathf.Abs(tint) > 0.001f)
                {
                    float ti = tint * 0.2f;
                    r += ti * 0.5f;
                    g -= ti;
                    b += ti * 0.5f;
                }

                // Highlights: brighten/darken only bright areas
                if (Mathf.Abs(highlights) > 0.001f)
                {
                    float lum = Mathf.Clamp01(0.2126f * r + 0.7152f * g + 0.0722f * b);
                    float mask = lum * lum;  // quadratic: strongly biased toward brights
                    float adj = highlights * mask;
                    r += adj; g += adj; b += adj;
                }

                // Shadows: brighten/darken only dark areas
                if (Mathf.Abs(shadows) > 0.001f)
                {
                    float lum = Mathf.Clamp01(0.2126f * r + 0.7152f * g + 0.0722f * b);
                    float invLum = 1f - lum;
                    float mask = invLum * invLum;  // quadratic: strongly biased toward darks
                    float adj = shadows * mask;
                    r += adj; g += adj; b += adj;
                }

                dstPixels[i] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), a);
            }

            // Sharpness (unsharp mask with 5-tap kernel)
            if (sharpness > 0.01f)
            {
                Color[] sharpened = new Color[dstPixels.Length];
                System.Array.Copy(dstPixels, sharpened, dstPixels.Length);
                float strength = sharpness * 5f;  // stronger effect at max
                for (int y = 1; y < h - 1; y++)
                {
                    for (int x = 1; x < w - 1; x++)
                    {
                        int idx = y * w + x;
                        Color center = dstPixels[idx];
                        Color avg = (dstPixels[idx - 1] + dstPixels[idx + 1] +
                                     dstPixels[idx - w] + dstPixels[idx + w]) * 0.25f;
                        float dr = center.r + (center.r - avg.r) * strength;
                        float dg = center.g + (center.g - avg.g) * strength;
                        float db = center.b + (center.b - avg.b) * strength;
                        sharpened[idx] = new Color(Mathf.Clamp01(dr), Mathf.Clamp01(dg), Mathf.Clamp01(db), center.a);
                    }
                }
                dstPixels = sharpened;
            }

            Texture2D resultTex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            resultTex.SetPixels(dstPixels);
            resultTex.Apply();

            Sprite newSprite = Sprite.Create(resultTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            img.sprite = newSprite;
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
            if (currentSelection == null) return;
            RectTransform rt = currentSelection.GetComponent<RectTransform>();
            if (rt == null) return;

            isUpdatingPositionUI = true;

            float userX = 300f - rt.anchoredPosition.x;
            float userY = rt.anchoredPosition.y + 300f;

            if (posXInput) posXInput.text = $"{userX:F1}";
            if (posYInput) posYInput.text = $"{userY:F1}";
            if (widthInput) widthInput.text = $"{rt.rect.width:F1}";
            if (heightInput) heightInput.text = $"{rt.rect.height:F1}";

            float rot = rt.localEulerAngles.z;
            if (rot > 180) rot -= 360;
            if (rotationInput) rotationInput.text = $"{-rot:F1}";

            isUpdatingPositionUI = false;
        }

        public void OnPositionInputChanged()
        {
            if (isUpdatingPositionUI || currentSelection == null) return;
            RectTransform rt = currentSelection.GetComponent<RectTransform>();
            if (rt == null) return;

            Vector2 oldPos = rt.anchoredPosition;

            if (posXInput != null && float.TryParse(posXInput.text, out float ux))
                rt.anchoredPosition = new Vector2(300f - ux, rt.anchoredPosition.y);
            if (posYInput != null && float.TryParse(posYInput.text, out float uy))
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, uy - 300f);

            if (widthInput != null && float.TryParse(widthInput.text, out float w) && w > 0)
                rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            if (heightInput != null && float.TryParse(heightInput.text, out float h) && h > 0)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);

            if (rotationInput != null && float.TryParse(rotationInput.text, out float r))
                rt.localEulerAngles = new Vector3(0, 0, -r);

            if (Vector2.Distance(oldPos, rt.anchoredPosition) > 0.01f)
            {
                RecordMove(rt, oldPos, rt.anchoredPosition);
                OnObjectMoved();
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

        private void SaveUploadToCache(string originalPath, Texture2D texture)
        {
            if (texture == null || string.IsNullOrEmpty(originalPath)) return;

            if (!Directory.Exists(UploadCachePath))
                Directory.CreateDirectory(UploadCachePath);

            string fileName = Path.GetFileName(originalPath);
            string destPath = Path.Combine(UploadCachePath, fileName);
            int counter = 1;
            while (File.Exists(destPath))
            {
                destPath = Path.Combine(UploadCachePath, Path.GetFileNameWithoutExtension(fileName) + "_" + counter + Path.GetExtension(fileName));
                counter++;
            }

            byte[] png = texture.EncodeToPNG();
            if (png != null && png.Length > 0)
            {
                File.WriteAllBytes(destPath, png);
                uploadedImagePaths.Add(destPath);
                SaveUploadManifest();
            }
        }

        private void SaveUploadManifest()
        {
            if (!Directory.Exists(UploadCachePath))
                Directory.CreateDirectory(UploadCachePath);
            File.WriteAllLines(UploadManifestPath, uploadedImagePaths.ToArray());
        }

        public void LoadUploadedImages()
        {
            uploadedImagePaths.Clear();
            if (!File.Exists(UploadManifestPath)) return;

            string[] lines = File.ReadAllLines(UploadManifestPath);
            foreach (string line in lines)
            {
                string path = line.Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    uploadedImagePaths.Add(path);
            }
            RefreshUploadGrid();
        }

        public void RefreshUploadGrid()
        {
            if (uploadListContainer == null) return;
            foreach (Transform child in uploadListContainer.transform)
                Destroy(child.gameObject);

            uploadSelectedPaths.Clear();
            UpdateUploadSelectionBar();

            if (uploadedImagePaths.Count == 0)
            {
                UIFactory.CreateText("No uploads yet.", uploadListContainer, 11, Color.gray, Vector2.zero, new Vector2(0, 22), TextAnchor.MiddleLeft, FontStyle.Normal).name = "UploadEmptyHint";
                return;
            }

            foreach (string cachedPath in uploadedImagePaths)
            {
                if (!File.Exists(cachedPath)) continue;
                CreateUploadThumbnail(cachedPath);
            }
        }

        private void CreateUploadThumbnail(string cachedPath)
        {
            if (uploadListContainer == null) return;

            byte[] data;
            try { data = File.ReadAllBytes(cachedPath); } catch { return; }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!tex.LoadImage(data)) { Destroy(tex); return; }

            float thumbSize = 100f;
            GameObject item = UIFactory.CreateObject("UploadThumb", uploadListContainer);
            RectTransform itemRt = item.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(thumbSize, thumbSize);
            item.AddComponent<LayoutElement>().preferredWidth = thumbSize;
            item.GetComponent<LayoutElement>().preferredHeight = thumbSize;

            Image thumbImg = item.AddComponent<Image>();
            thumbImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            thumbImg.preserveAspect = true;
            thumbImg.color = Color.white;

            string path = cachedPath;

            // Checkbox (top-left, hidden until hover, clickable to toggle selection)
            GameObject checkObj = UIFactory.CreateObject("Check", item);
            RectTransform checkRt = checkObj.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0, 1); checkRt.anchorMax = new Vector2(0, 1);
            checkRt.pivot = new Vector2(0, 1);
            checkRt.sizeDelta = new Vector2(24, 24);
            checkRt.anchoredPosition = new Vector2(2, -2);
            Image checkBg = checkObj.AddComponent<Image>();
            checkBg.color = new Color(1, 1, 1, 0.85f);

            GameObject checkMark = UIFactory.CreateObject("CheckIcon", checkObj);
            RectTransform cmRt = checkMark.GetComponent<RectTransform>();
            cmRt.anchorMin = new Vector2(0.1f, 0.1f); cmRt.anchorMax = new Vector2(0.9f, 0.9f);
            cmRt.offsetMin = Vector2.zero; cmRt.offsetMax = Vector2.zero;
            Image cmImg = checkMark.AddComponent<Image>();
            Sprite checkSpr = Resources.Load<Sprite>("EditIcons/p_check");
            if (checkSpr != null) { cmImg.sprite = checkSpr; cmImg.preserveAspect = true; }
            cmImg.color = Color.white;
            checkMark.SetActive(false);

            Button checkBtn = checkObj.AddComponent<Button>();
            checkBtn.targetGraphic = checkBg;
            checkBtn.onClick.AddListener(() => ToggleUploadSelection(path, checkObj));
            checkObj.SetActive(false);

            // Menu dots (top-right, hidden until hover)
            GameObject menuObj = UIFactory.CreateObject("Menu", item);
            RectTransform menuRt = menuObj.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(1, 1); menuRt.anchorMax = new Vector2(1, 1);
            menuRt.pivot = new Vector2(1, 1);
            menuRt.sizeDelta = new Vector2(24, 24);
            menuRt.anchoredPosition = new Vector2(-2, -2);
            Image menuBg = menuObj.AddComponent<Image>();
            menuBg.color = new Color(1, 1, 1, 0.85f);
            Sprite dotsSpr = Resources.Load<Sprite>("EditIcons/p_dot-three-v-lined");
            if (dotsSpr != null)
            {
                GameObject dotsIcon = UIFactory.CreateObject("DotsIcon", menuObj);
                RectTransform diRt = dotsIcon.GetComponent<RectTransform>();
                diRt.anchorMin = new Vector2(0.15f, 0.15f); diRt.anchorMax = new Vector2(0.85f, 0.85f);
                diRt.offsetMin = Vector2.zero; diRt.offsetMax = Vector2.zero;
                Image diImg = dotsIcon.AddComponent<Image>();
                diImg.sprite = dotsSpr; diImg.preserveAspect = true; diImg.color = new Color(0.3f, 0.3f, 0.3f);
            }
            menuObj.SetActive(false);

            Button menuBtn = menuObj.AddComponent<Button>();
            menuBtn.targetGraphic = menuBg;
            menuBtn.onClick.AddListener(() => ToggleUploadSelection(path, checkObj));

            // Invisible button covering entire thumbnail for adding to canvas
            // Must be behind checkbox and menu so those intercept clicks first
            Button clickBtn = item.AddComponent<Button>();
            clickBtn.targetGraphic = thumbImg;
            clickBtn.onClick.AddListener(() =>
            {
                if (!File.Exists(path)) return;
                if (TryLoadPngTextureFromFile(path, out Texture2D canvasTex, out string _err))
                    AddTextureToCanvas(path, canvasTex);
            });

            // Hover handler
            UploadThumbnailHover hover = item.AddComponent<UploadThumbnailHover>();
            hover.checkObj = checkObj;
            hover.menuObj = menuObj;
        }

        private void ToggleUploadSelection(string path, GameObject checkObj)
        {
            bool wasSelected = uploadSelectedPaths.Contains(path);
            if (wasSelected)
            {
                uploadSelectedPaths.Remove(path);
                if (checkObj != null)
                {
                    checkObj.GetComponent<Image>().color = new Color(1, 1, 1, 0.85f);
                    Transform cm = checkObj.transform.Find("CheckIcon");
                    if (cm != null) cm.gameObject.SetActive(false);
                }
            }
            else
            {
                uploadSelectedPaths.Add(path);
                if (checkObj != null)
                {
                    checkObj.GetComponent<Image>().color = new Color(0.55f, 0.88f, 0.58f, 0.9f);
                    Transform cm = checkObj.transform.Find("CheckIcon");
                    if (cm != null) cm.gameObject.SetActive(true);
                    checkObj.SetActive(true);
                }
            }
            UpdateUploadSelectionBar();
        }

        private void UpdateUploadSelectionBar()
        {
            if (uploadSelectionBar == null) return;
            int count = uploadSelectedPaths.Count;
            uploadSelectionBar.SetActive(count > 0);
            if (uploadSelectionCountText != null)
                uploadSelectionCountText.text = $"({count}) Selected";
            bool allSelected = count > 0 && count >= uploadedImagePaths.Count;
            if (uploadBarCheckImage != null)
                uploadBarCheckImage.color = allSelected ? new Color(0.55f, 0.88f, 0.58f) : new Color(0.65f, 0.65f, 0.65f);
        }

        public void DeleteSelectedUploads()
        {
            var toDelete = new List<string>(uploadSelectedPaths);
            foreach (string p in toDelete)
            {
                uploadedImagePaths.Remove(p);
                try { if (File.Exists(p)) File.Delete(p); } catch { }
            }
            uploadSelectedPaths.Clear();
            SaveUploadManifest();
            RefreshUploadGrid();
        }

        public void CancelUploadSelection()
        {
            uploadSelectedPaths.Clear();
            RefreshUploadGrid();
        }

        public void SelectAllUploads()
        {
            bool allSelected = uploadSelectedPaths.Count >= uploadedImagePaths.Count && uploadedImagePaths.Count > 0;
            if (allSelected)
            {
                uploadSelectedPaths.Clear();
                SetAllThumbnailCheckState(false);
            }
            else
            {
                uploadSelectedPaths.Clear();
                foreach (string p in uploadedImagePaths)
                    uploadSelectedPaths.Add(p);
                SetAllThumbnailCheckState(true);
            }
            UpdateUploadSelectionBar();
        }

        private void SetAllThumbnailCheckState(bool selected)
        {
            if (uploadListContainer == null) return;
            foreach (Transform child in uploadListContainer.transform)
            {
                Transform chk = child.Find("Check");
                if (chk == null) continue;
                Image chkImg = chk.GetComponent<Image>();
                Transform cm = chk.Find("CheckIcon");
                if (selected)
                {
                    chk.gameObject.SetActive(true);
                    if (chkImg != null) chkImg.color = new Color(0.55f, 0.88f, 0.58f, 0.9f);
                    if (cm != null) cm.gameObject.SetActive(true);
                }
                else
                {
                    if (chkImg != null) chkImg.color = new Color(1, 1, 1, 0.85f);
                    if (cm != null) cm.gameObject.SetActive(false);
                    chk.gameObject.SetActive(false);
                }
            }
        }

        

        private void AddUploadedListItem(string originalPath, Texture2D texture)
        {
            if (texture == null) return;
            SaveUploadToCache(originalPath, texture);
            RefreshUploadGrid();
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
            if (miniPreviewPanel != null && !miniPreviewPanel.activeSelf) return;

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
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (ctrl && shift && Input.GetKeyDown(KeyCode.Z))
            {
                Redo();
            }
            else if (ctrl && Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }

            // 1. Delete Selection
            if (currentSelection != null && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)))
            {
                RecordDelete(currentSelection);
                Deselect();
            }

            // Debounced adjustment apply
            if (adjustmentDebounceTimer > 0f)
            {
                adjustmentDebounceTimer -= Time.unscaledDeltaTime;
                if (adjustmentDebounceTimer <= 0f)
                {
                    adjustmentDebounceTimer = -1f;
                    ApplyAdjustments();
                }
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
            if (selectionFrame == null) return;
            
            rotationHandle = new GameObject("RotationHandle");
            rotationHandle.transform.SetParent(selectionFrame.transform, false);
            rotationHandle.AddComponent<SelectionAdornment>();
            
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
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -30f);
            
            RotationHandler handler = rotationHandle.AddComponent<RotationHandler>();
            handler.target = currentSelection.GetComponent<RectTransform>();
            handler.controller = this;
        }

        private void CreateSelectionFrame()
        {
            DestroySelectionFrame();
            if (currentSelection == null) return;

            RectTransform targetRt = currentSelection.GetComponent<RectTransform>();
            if (targetRt == null) return;

            selectionFrame = new GameObject("SelectionFrame", typeof(RectTransform), typeof(SelectionAdornment));
            selectionFrame.transform.SetParent(currentSelection.transform, false);
            selectionFrame.transform.SetAsLastSibling();

            RectTransform frameRt = selectionFrame.GetComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = Vector2.zero;
            frameRt.offsetMax = Vector2.zero;
            frameRt.pivot = targetRt.pivot;

            CreateSelectionBorder(selectionFrame.transform);

            CreateCornerHandle("TopLeftHandle", selectionFrame.transform, new Vector2(0f, 1f), new Vector2(-1, 1));
            CreateCornerHandle("TopRightHandle", selectionFrame.transform, new Vector2(1f, 1f), new Vector2(1, 1));
            CreateCornerHandle("BottomLeftHandle", selectionFrame.transform, new Vector2(0f, 0f), new Vector2(-1, -1));
            CreateCornerHandle("BottomRightHandle", selectionFrame.transform, new Vector2(1f, 0f), new Vector2(1, -1));
        }

        private void DestroySelectionFrame()
        {
            if (selectionFrame != null) Destroy(selectionFrame);
            selectionFrame = null;
        }

        private static void CreateSelectionBorder(Transform parent)
        {
            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image), typeof(SelectionAdornment));
            border.transform.SetParent(parent, false);

            RectTransform rt = border.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = border.GetComponent<Image>();
            img.sprite = CreateSelectionBorderSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.31f, 0.86f, 0.45f);
            img.raycastTarget = false;
        }

        private void CreateCornerHandle(string name, Transform parent, Vector2 anchor, Vector2 signs)
        {
            const float visualSize = 9f;
            const float hitSize = 22f;

            GameObject handle = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(SelectionAdornment));
            handle.transform.SetParent(parent, false);

            RectTransform rt = handle.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(hitSize, hitSize);
            rt.anchoredPosition = new Vector2(-signs.x * visualSize * 0.5f, -signs.y * visualSize * 0.5f);

            Image img = handle.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.001f);

            GameObject visual = new GameObject("Visual", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(SelectionAdornment));
            visual.transform.SetParent(handle.transform, false);
            RectTransform visualRt = visual.GetComponent<RectTransform>();
            visualRt.anchorMin = new Vector2(0.5f, 0.5f);
            visualRt.anchorMax = new Vector2(0.5f, 0.5f);
            visualRt.pivot = new Vector2(0.5f, 0.5f);
            visualRt.sizeDelta = new Vector2(visualSize, visualSize);
            visualRt.anchoredPosition = Vector2.zero;

            Image visualImg = visual.GetComponent<Image>();
            visualImg.color = Color.white;
            visualImg.raycastTarget = false;

            Outline outline = visual.GetComponent<Outline>();
            outline.effectColor = new Color(0.31f, 0.86f, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            SelectionResizeHandle resizeHandle = handle.AddComponent<SelectionResizeHandle>();
            resizeHandle.target = currentSelection.GetComponent<RectTransform>();
            resizeHandle.controller = this;
            resizeHandle.xSign = Mathf.RoundToInt(signs.x);
            resizeHandle.ySign = Mathf.RoundToInt(signs.y);
        }

        private static Sprite CreateSelectionBorderSprite()
        {
            const int size = 128;
            const int radius = 10;
            const float borderWidth = 3.0f;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 halfOuter = new Vector2(size * 0.5f - 2f, size * 0.5f - 2f);
            Vector2 halfInner = halfOuter - Vector2.one * borderWidth;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float outer = SignedDistanceRoundedBox(p, halfOuter, radius);
                    float inner = SignedDistanceRoundedBox(p, halfInner, Mathf.Max(1f, radius - borderWidth));
                    float outerAlpha = 1f - Mathf.Clamp01(outer + 0.5f);
                    float innerAlpha = 1f - Mathf.Clamp01(inner + 0.5f);
                    float alpha = Mathf.Clamp01(outerAlpha - innerAlpha);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius + borderWidth, radius + borderWidth, radius + borderWidth, radius + borderWidth));
        }

        private static float SignedDistanceRoundedBox(Vector2 p, Vector2 b, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b + Vector2.one * r;
            Vector2 maxQ = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return maxQ.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        private void DestroyRotationHandle()
        {
            if (rotationHandle != null) Destroy(rotationHandle);
            rotationHandle = null;
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

