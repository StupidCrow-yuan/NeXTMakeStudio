using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PocoRender.UI.Core;

namespace PocoRender.UI.Modules
{
    /// <summary>
    /// Builds the right panel of the canvas editor: layer info, global info, bottom buttons.
    /// Extracted from the monolithic CanvasModule for maintainability.
    /// </summary>
    public static class CanvasRightPanelBuilder
    {
        public static void CreateRightPanel(GameObject editorArea, CanvasController controller)
        {
            GameObject rightPanel = UIFactory.CreateObject("RightPanel", editorArea);
            RectTransform rpRect = rightPanel.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.75f, 0); rpRect.anchorMax = new Vector2(1, 1);
            rpRect.offsetMin = Vector2.zero; rpRect.offsetMax = Vector2.zero;
            rightPanel.AddComponent<Image>().color = Color.white;
            rightPanel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);

            // Scrollable panels root
            GameObject panelsRoot = UIFactory.CreateObject("PanelsRoot", rightPanel);
            RectTransform prRt = panelsRoot.GetComponent<RectTransform>();
            UIFactory.Stretch(prRt); prRt.offsetMin = new Vector2(0, 100);

            ScrollRect mainSr = panelsRoot.AddComponent<ScrollRect>();
            mainSr.horizontal = false; mainSr.vertical = true;
            mainSr.scrollSensitivity = 60;
            mainSr.movementType = ScrollRect.MovementType.Clamped;

            GameObject mainVp = UIFactory.CreateObject("Viewport", panelsRoot);
            UIFactory.Stretch(mainVp.GetComponent<RectTransform>());
            Image vpImg = mainVp.AddComponent<Image>(); vpImg.color = new Color(0, 0, 0, 0); vpImg.raycastTarget = true;
            mainVp.AddComponent<RectMask2D>();

            GameObject mainContent = UIFactory.CreateObject("MainContent", mainVp);
            RectTransform mcRt = mainContent.GetComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0, 1); mcRt.anchorMax = new Vector2(1, 1);
            mcRt.pivot = new Vector2(0.5f, 1); mcRt.sizeDelta = new Vector2(0, 0);
            mainContent.AddComponent<VerticalLayoutGroup>();
            mainContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            mainSr.viewport = mainVp.GetComponent<RectTransform>(); mainSr.content = mcRt;

            // Layer Info Panel
            GameObject layerPanel = UIFactory.CreateObject("LayerInfoPanel", mainContent);
            layerPanel.AddComponent<LayoutElement>().flexibleHeight = 1;
            VerticalLayoutGroup lpVlg = layerPanel.AddComponent<VerticalLayoutGroup>();
            lpVlg.padding = new RectOffset(20, 20, 20, 20); lpVlg.spacing = 15;
            lpVlg.childControlHeight = false; lpVlg.childForceExpandHeight = false;
            controller.layerInfoPanel = layerPanel;
            layerPanel.SetActive(false);

            // Global Info Panel
            GameObject globalPanel = UIFactory.CreateObject("GlobalInfoPanel", mainContent);
            globalPanel.AddComponent<LayoutElement>().flexibleHeight = 1;
            controller.globalInfoPanel = globalPanel;
            CreateGlobalInfoPanel(globalPanel, controller);

            PopulateLayerInfoPanel(layerPanel, controller);
            CreateBottomButtons(rightPanel, controller);
        }

        private static void PopulateLayerInfoPanel(GameObject layerPanel, CanvasController controller)
        {
            // Position (editable)
            UIFactory.CreateText("Position", layerPanel, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject posPanel = UIFactory.CreateObject("PosPanel", layerPanel);
            VerticalLayoutGroup posVlg = posPanel.AddComponent<VerticalLayoutGroup>(); posVlg.spacing = 5;
            posPanel.AddComponent<LayoutElement>().minHeight = 110;

            System.Func<string, string, GameObject, InputField> createPosField = (label, unit, parent) =>
            {
                GameObject field = UIFactory.CreateObject(label + "Field", parent);
                HorizontalLayoutGroup fhlg = field.AddComponent<HorizontalLayoutGroup>();
                fhlg.spacing = 4; fhlg.childAlignment = TextAnchor.MiddleLeft;
                fhlg.childControlWidth = true; fhlg.childControlHeight = true;
                fhlg.childForceExpandWidth = false;

                var labelObj = UIFactory.CreateText(label, field, 12, Color.gray, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
                labelObj.AddComponent<LayoutElement>().minWidth = 16;

                GameObject inpObj = UIFactory.CreateObject("Input", field);
                inpObj.AddComponent<LayoutElement>().flexibleWidth = 1;
                inpObj.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
                InputField inp = inpObj.AddComponent<InputField>();
                inp.contentType = InputField.ContentType.DecimalNumber;
                GameObject txtObj = UIFactory.CreateText("--", inpObj, 13, Color.black, Vector2.zero, Vector2.zero);
                UIFactory.Stretch(txtObj.GetComponent<RectTransform>());
                txtObj.GetComponent<RectTransform>().offsetMin = new Vector2(6, 0);
                txtObj.GetComponent<RectTransform>().offsetMax = new Vector2(-26, 0);
                inp.textComponent = txtObj.GetComponent<Text>();

                var unitObj = UIFactory.CreateText(unit, inpObj, 11, new Color(0.55f, 0.55f, 0.55f), Vector2.zero, Vector2.zero, TextAnchor.MiddleRight);
                RectTransform unitRt = unitObj.GetComponent<RectTransform>();
                unitRt.anchorMin = new Vector2(1, 0); unitRt.anchorMax = new Vector2(1, 1);
                unitRt.pivot = new Vector2(1, 0.5f);
                unitRt.sizeDelta = new Vector2(24, 0); unitRt.anchoredPosition = new Vector2(-4, 0);

                return inp;
            };

            GameObject r1 = UIFactory.CreateObject("Row1", posPanel); r1.AddComponent<LayoutElement>().minHeight = 28;
            HorizontalLayoutGroup r1hlg = r1.AddComponent<HorizontalLayoutGroup>(); r1hlg.spacing = 10;
            controller.posXInput = createPosField("X", "mm", r1);
            controller.posYInput = createPosField("Y", "mm", r1);

            GameObject r2 = UIFactory.CreateObject("Row2", posPanel); r2.AddComponent<LayoutElement>().minHeight = 28;
            HorizontalLayoutGroup r2hlg = r2.AddComponent<HorizontalLayoutGroup>(); r2hlg.spacing = 10;
            controller.widthInput = createPosField("W", "mm", r2);
            controller.heightInput = createPosField("H", "mm", r2);

            GameObject r3 = UIFactory.CreateObject("Row3", posPanel); r3.AddComponent<LayoutElement>().minHeight = 28;
            HorizontalLayoutGroup r3hlg = r3.AddComponent<HorizontalLayoutGroup>(); r3hlg.spacing = 10;
            controller.rotationInput = createPosField("R", "°", r3);
            
            // Dummy field to match layout of W/H rows
            var dummy = createPosField(" ", "", r3);
            var dummyGroup = dummy.transform.parent.gameObject.AddComponent<CanvasGroup>();
            dummyGroup.alpha = 0; dummyGroup.blocksRaycasts = false;

            controller.posXInput.onEndEdit.AddListener((_) => controller.OnPositionInputChanged());
            controller.posYInput.onEndEdit.AddListener((_) => controller.OnPositionInputChanged());
            controller.widthInput.onEndEdit.AddListener((_) => controller.OnPositionInputChanged());
            controller.heightInput.onEndEdit.AddListener((_) => controller.OnPositionInputChanged());
            controller.rotationInput.onEndEdit.AddListener((_) => controller.OnPositionInputChanged());

            controller.positionPanel = posPanel;

            // Craft Mode
            UIFactory.CreateText("Craft Mode", layerPanel, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject craftGrid = UIFactory.CreateObject("CraftGrid", layerPanel);
            controller.craftModeContainer = craftGrid;
            GridLayoutGroup cglg = craftGrid.AddComponent<GridLayoutGroup>();
            cglg.padding = new RectOffset(3, 3, 3, 3);
            cglg.cellSize = new Vector2(120, 40); cglg.spacing = new Vector2(8, 8); cglg.constraintCount = 2;
            craftGrid.AddComponent<LayoutElement>().minHeight = 160;
            string[] craftModes = { "Flat", "Flat Raised", "Pattern Texture", "Relief Texture", "Customize Texture" };
            foreach(var cm in craftModes) {
                GameObject btn = UIFactory.CreateButton(cm, craftGrid, Vector2.zero, new Vector2(0, 0), Color.white, Color.black);
                btn.GetComponentInChildren<Text>().fontSize = 12;
                btn.AddComponent<Outline>().effectColor = cm == "Flat" ? Color.green : Color.gray;
                string mode = cm;
                btn.GetComponent<Button>().onClick.AddListener(() => {
                    foreach(Transform child in craftGrid.transform) { var outline = child.GetComponent<Outline>(); if(outline) outline.effectColor = (child.name == "Btn_" + mode) ? Color.green : Color.gray; }
                    controller.OnCraftModeChanged(mode);
                });
            }

            // Mini Preview
            CreateMiniPreview(layerPanel, controller);

            // Ink Mode
            UIFactory.CreateText("Ink Mode", layerPanel, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject dropdownObj = CanvasModalBuilder.CreateCustomDropdown("InkDropdown", layerPanel,
                new[] { "White > CMYK", "CMYK", "Gloss Varnish", "White", "CMYK > White", "White > CMYK > Gloss Varnish", "Sticker" },
                0, (idx) => {});
            dropdownObj.GetComponent<LayoutElement>().minHeight = 40; // Match Material dropdown height
            dropdownObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40); // Explicitly set height since VLG controlHeight is false
            UIFactory.AddDropdownArrow(dropdownObj, 14f); // Re-apply arrow for new height? No, just size.
            
            // Fix arrow size for 28 height if needed
            // The AddDropdownArrow uses 14f in CanvasModalBuilder, maybe too big for 28.
            // But we can't easily change it here without refinding the arrow.
            // Let's assume 14px fits in 28px. It should be fine.
        }

        private static void CreateMiniPreview(GameObject layerPanel, CanvasController controller)
        {
            GameObject miniPrev = UIFactory.CreateObject("MiniPreview", layerPanel);
            miniPrev.AddComponent<Image>().color = new Color(0.98f, 0.98f, 0.98f);
            miniPrev.AddComponent<Outline>().effectColor = new Color(0.9f, 0.9f, 0.9f);
            AspectRatioFitter arf = miniPrev.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; arf.aspectRatio = 1.0f;
            LayoutElement le = miniPrev.AddComponent<LayoutElement>();
            le.preferredHeight = 220;
            le.minHeight = 120;
            le.minWidth = 120;
            
            // Customize Upload
            GameObject custPanel = UIFactory.CreateObject("CustomizeUpload", layerPanel);
            custPanel.AddComponent<LayoutElement>().minHeight = 80;
            custPanel.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
            custPanel.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            VerticalLayoutGroup cvlg = custPanel.AddComponent<VerticalLayoutGroup>(); cvlg.padding = new RectOffset(10, 10, 10, 10); cvlg.spacing = 5;
            UIFactory.CreateText("Upload Depth Map (JPG/PNG/SVG/WebP)", custPanel, 11, Color.gray, Vector2.zero, Vector2.zero);
            UIFactory.CreateButton("Upload \u2912", custPanel, Vector2.zero, new Vector2(0, 35), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.OnUploadDepthMap());
            custPanel.SetActive(false);
            controller.customizePanel = custPanel;

            // Zoom buttons
            GameObject miniZoomBar = UIFactory.CreateObject("MiniZoomBar", miniPrev);
            RectTransform mzbRt = miniZoomBar.GetComponent<RectTransform>();
            mzbRt.anchorMin = new Vector2(0, 1); mzbRt.anchorMax = new Vector2(0, 1);
            mzbRt.pivot = new Vector2(0, 1); mzbRt.sizeDelta = new Vector2(80, 25); mzbRt.anchoredPosition = new Vector2(5, -5);
            miniZoomBar.AddComponent<HorizontalLayoutGroup>().spacing = 5;
            UIFactory.CreateButton("-", miniZoomBar, Vector2.zero, new Vector2(25, 20), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ChangeMiniZoom(-0.5f));
            UIFactory.CreateButton("+", miniZoomBar, Vector2.zero, new Vector2(25, 20), Color.white, Color.black).GetComponent<Button>().onClick.AddListener(() => controller.ChangeMiniZoom(0.5f));

            // Depth download button
            GameObject depthDl = UIFactory.CreateObject("DepthDownload", miniPrev);
            RectTransform ddlRt = depthDl.GetComponent<RectTransform>();
            ddlRt.anchorMin = new Vector2(1, 0); ddlRt.anchorMax = new Vector2(1, 0); ddlRt.pivot = new Vector2(1, 0);
            ddlRt.sizeDelta = new Vector2(140, 24); ddlRt.anchoredPosition = new Vector2(-6, 6);
            depthDl.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
            depthDl.AddComponent<Button>().onClick.AddListener(() => controller.OnDownloadDepthImage());
            depthDl.AddComponent<Outline>().effectColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            HorizontalLayoutGroup ddlHlg = depthDl.AddComponent<HorizontalLayoutGroup>();
            ddlHlg.padding = new RectOffset(6, 6, 2, 2); ddlHlg.spacing = 6; ddlHlg.childAlignment = TextAnchor.MiddleLeft;
            ddlHlg.childControlWidth = true; ddlHlg.childControlHeight = true; ddlHlg.childForceExpandWidth = false; ddlHlg.childForceExpandHeight = false;
            UIFactory.CreateText("\u2193", depthDl, 13, new Color(0.2f, 0.2f, 0.2f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.CreateText("Depth Image", depthDl, 12, new Color(0.2f, 0.2f, 0.2f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Normal);

            // 3D view
            GameObject mini3D = UIFactory.CreateObject("Mini3DView", miniPrev);
            UIFactory.Stretch(mini3D.GetComponent<RectTransform>());
            mini3D.GetComponent<RectTransform>().offsetMin = new Vector2(5, 5); mini3D.GetComponent<RectTransform>().offsetMax = new Vector2(-5, -30);
            RawImage miniRi = mini3D.AddComponent<RawImage>(); miniRi.raycastTarget = true;
            
            Model3DViewer miniViewer = miniPrev.AddComponent<Model3DViewer>();
            miniViewer.targetImage = miniRi; miniViewer.textureHeight = 1024; miniViewer.textureWidth = 1024;
            Model3DController miniController = miniPrev.AddComponent<Model3DController>();
            miniController.modelViewer = miniViewer; miniController.enableRotation = true; miniController.enableZoom = true; miniController.enablePan = true;
            
            controller.miniPreviewPanel = miniPrev;
            controller.miniPreviewImage = miniRi;
            controller.miniModelViewer = miniViewer;
            depthDl.transform.SetAsLastSibling();
            miniPrev.SetActive(false);
        }

        private static void CreateBottomButtons(GameObject rightPanel, CanvasController controller)
        {
            GameObject bottomRow = UIFactory.CreateObject("BottomActions", rightPanel);
            RectTransform brRt = bottomRow.GetComponent<RectTransform>();
            brRt.anchorMin = new Vector2(0, 0); brRt.anchorMax = new Vector2(1, 0);
            brRt.pivot = new Vector2(0.5f, 0); brRt.sizeDelta = new Vector2(-40, 56); brRt.anchoredPosition = new Vector2(0, 18);
            HorizontalLayoutGroup ahlg = bottomRow.AddComponent<HorizontalLayoutGroup>();
            ahlg.padding = new RectOffset(3, 3, 3, 3);
            ahlg.spacing = 12; ahlg.childControlWidth = true; ahlg.childForceExpandWidth = true; ahlg.childControlHeight = true; ahlg.childForceExpandHeight = false;
            
            GameObject previewBtn = UIFactory.CreateButton("Preview", bottomRow, Vector2.zero, new Vector2(0, 40), Color.white, new Color(0.2f, 0.2f, 0.2f));
            previewBtn.AddComponent<LayoutElement>().minHeight = 40;
            previewBtn.AddComponent<Outline>().effectColor = new Color(0.75f, 0.75f, 0.75f);
            previewBtn.GetComponent<Button>().onClick.AddListener(() => controller.OnPreviewRequested?.Invoke());
            
            GameObject printBtn = UIFactory.CreateButton("Print", bottomRow, Vector2.zero, new Vector2(0, 40), UIFactory.COLOR_ACCENT_GREEN, Color.white);
            printBtn.AddComponent<LayoutElement>().minHeight = 40;
            printBtn.GetComponent<Button>().onClick.AddListener(() => controller.OnPrintRequested?.Invoke());
        }

        public static void CreateGlobalInfoPanel(GameObject parent, CanvasController controller)
        {
            VerticalLayoutGroup vlg = parent.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20); vlg.spacing = 20;
            vlg.childControlHeight = true; vlg.childForceExpandWidth = true;

            // Device Info
            GameObject deviceBox = UIFactory.CreateObject("DeviceBox", parent);
            deviceBox.AddComponent<LayoutElement>().minHeight = 80;
            deviceBox.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            HorizontalLayoutGroup dhlg = deviceBox.AddComponent<HorizontalLayoutGroup>();
            dhlg.padding = new RectOffset(10, 10, 10, 10); dhlg.spacing = 15; dhlg.childAlignment = TextAnchor.MiddleLeft;
            GameObject thumb = UIFactory.CreateObject("Thumb", deviceBox);
            thumb.AddComponent<LayoutElement>().minWidth = 60; thumb.GetComponent<LayoutElement>().minHeight = 60;
            thumb.AddComponent<Image>().color = Color.black;
            GameObject info = UIFactory.CreateObject("Info", deviceBox);
            VerticalLayoutGroup ivlg = info.AddComponent<VerticalLayoutGroup>(); ivlg.childAlignment = TextAnchor.MiddleLeft; ivlg.spacing = 2;
            UIFactory.CreateText("PocoRender 8260", info, 14, Color.black, Vector2.zero, new Vector2(150, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.CreateText("\u25CF Disconnected", info, 12, Color.gray, Vector2.zero, new Vector2(150, 18), TextAnchor.MiddleLeft);

            // Print Bed
            CreatePrintBedSection(parent, controller);
            // Alignment
            CreateAlignmentSection(parent, controller);
            // Material
            CreateMaterialSection(parent, controller);
            // Quality
            CreateQualitySection(parent, controller);
            // Print Settings (migrated from Qt Print Setting)
            CreateGlobalPrintSettingsSection(parent, controller);
            // Choke
            CreateChokeSection(parent, controller);
            // Print Area
            CreatePrintAreaSection(parent, controller);
        }

        private static void CreatePrintBedSection(GameObject parent, CanvasController controller)
        {
            GameObject bedSection = UIFactory.CreateObject("PrintBed", parent);
            VerticalLayoutGroup bvlg = bedSection.AddComponent<VerticalLayoutGroup>(); bvlg.spacing = 10;
            GameObject bedHeader = UIFactory.CreateObject("Header", bedSection);
            bedHeader.AddComponent<LayoutElement>().minHeight = 25;
            bedHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("Print Bed", bedHeader, 14, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject bedInfoBtn = UIFactory.CreateObject("BedInfo", bedHeader);
            bedInfoBtn.AddComponent<LayoutElement>().minWidth = 20; bedInfoBtn.GetComponent<LayoutElement>().minHeight = 20;
            Text biText = bedInfoBtn.AddComponent<Text>();
            biText.text = "\u24D8"; biText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); biText.color = Color.gray; biText.alignment = TextAnchor.MiddleCenter; biText.fontSize = 16; biText.raycastTarget = true;
            bedInfoBtn.AddComponent<Button>().onClick.AddListener(() => CanvasModalBuilder.CreatePrintBedModal(controller.editorArea));

            CanvasModalBuilder.CreateCustomDropdown("BedDropdown", bedSection, new[]{ "Mini Flatbed", "Standard Flatbed", "Rotary", "Roll-To-Film" }, 1, (idx) => {});

            GameObject sizeRow = UIFactory.CreateObject("SizeRow", bedSection);
            sizeRow.AddComponent<LayoutElement>().minHeight = 40;
            HorizontalLayoutGroup shlg = sizeRow.AddComponent<HorizontalLayoutGroup>(); shlg.spacing = 10;
            System.Action<string, string> CreateInputField = (label, val) => {
                GameObject f = UIFactory.CreateObject("Field", sizeRow);
                f.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
                HorizontalLayoutGroup fhlg = f.AddComponent<HorizontalLayoutGroup>(); fhlg.padding = new RectOffset(10, 10, 0, 0);
                UIFactory.CreateText(label, f, 12, Color.gray, Vector2.zero, new Vector2(20, 0));
                UIFactory.CreateText(val, f, 13, Color.black, Vector2.zero, new Vector2(40, 0), TextAnchor.MiddleLeft);
                UIFactory.CreateText("mm", f, 12, Color.gray, Vector2.zero, new Vector2(30, 0), TextAnchor.MiddleRight);
            };
            CreateInputField("W", "335"); CreateInputField("H", "420");
        }

        private static void CreateAlignmentSection(GameObject parent, CanvasController controller)
        {
            GameObject alignSection = UIFactory.CreateObject("Alignment", parent);
            VerticalLayoutGroup avg = alignSection.AddComponent<VerticalLayoutGroup>(); avg.spacing = 10;
            GameObject alignHeader = UIFactory.CreateObject("Header", alignSection);
            alignHeader.AddComponent<LayoutElement>().minHeight = 25;
            alignHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("Design Alignment", alignHeader, 14, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject alignInfoBtn = UIFactory.CreateObject("AlignInfo", alignHeader);
            alignInfoBtn.AddComponent<LayoutElement>().minWidth = 20; alignInfoBtn.GetComponent<LayoutElement>().minHeight = 20;
            Text aiText = alignInfoBtn.AddComponent<Text>();
            aiText.text = "\u24D8"; aiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); aiText.color = Color.gray; aiText.alignment = TextAnchor.MiddleCenter; aiText.fontSize = 16; aiText.raycastTarget = true;
            alignInfoBtn.AddComponent<Button>().onClick.AddListener(() => CanvasModalBuilder.CreateAlignmentModal(controller.editorArea));

            GameObject photoAlign = UIFactory.CreateObject("PhotoAlign", alignSection);
            photoAlign.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            VerticalLayoutGroup pavg = photoAlign.AddComponent<VerticalLayoutGroup>(); pavg.padding = new RectOffset(15, 15, 15, 15); pavg.spacing = 10;
            GameObject paHeader = UIFactory.CreateObject("Header", photoAlign);
            paHeader.AddComponent<LayoutElement>().minHeight = 25; paHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("Photo Alignment", paHeader, 13, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text paCheck = UIFactory.CreateText("\u2714", paHeader, 14, UIFactory.COLOR_ACCENT_GREEN, Vector2.zero, new Vector2(20, 20), TextAnchor.MiddleRight).GetComponent<Text>();
            UIFactory.CreateButton("\uD83D\uDCF7 Snapshot", photoAlign, Vector2.zero, new Vector2(0, 40), new Color(0.7f, 0.9f, 0.8f), Color.white);
            UIFactory.CreateButton("\uD83D\uDCF7 Assisted shot", photoAlign, Vector2.zero, new Vector2(0, 40), Color.white, Color.gray).GetComponent<Button>().interactable = false;

            GameObject zeroAlign = UIFactory.CreateObject("ZeroAlign", alignSection);
            zeroAlign.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            HorizontalLayoutGroup zhlg = zeroAlign.AddComponent<HorizontalLayoutGroup>(); zhlg.padding = new RectOffset(15, 15, 10, 10);
            UIFactory.CreateText("Zero Point Alignment", zeroAlign, 13, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text zaCheck = UIFactory.CreateText("\u25CB", zeroAlign, 18, Color.gray, Vector2.zero, new Vector2(20, 20), TextAnchor.MiddleRight).GetComponent<Text>();

            photoAlign.AddComponent<Button>().onClick.AddListener(() => { paCheck.text = "\u2714"; paCheck.color = UIFactory.COLOR_ACCENT_GREEN; zaCheck.text = "\u25CB"; zaCheck.color = Color.gray; });
            zeroAlign.AddComponent<Button>().onClick.AddListener(() => { paCheck.text = "\u25CB"; paCheck.color = Color.gray; zaCheck.text = "\u2714"; zaCheck.color = UIFactory.COLOR_ACCENT_GREEN; });
        }

        private static void CreateMaterialSection(GameObject parent, CanvasController controller)
        {
            GameObject matSection = UIFactory.CreateObject("Material", parent);
            VerticalLayoutGroup mavg = matSection.AddComponent<VerticalLayoutGroup>(); mavg.spacing = 10;
            UIFactory.CreateText("Material", matSection, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject matRow = UIFactory.CreateObject("MatRow", matSection);
            matRow.AddComponent<LayoutElement>().minHeight = 40;
            HorizontalLayoutGroup mhlg = matRow.AddComponent<HorizontalLayoutGroup>(); mhlg.spacing = 10;
            string[] materials = { "Unknown", "Wood", "Acrylic", "Metal", "Drawing Board", "Plastic", "Ceramics", "Cotton canvas", "Polyester canvas", "Linen canvas", "Artificial leather", "Genuine leather", "Cardboard" };
            var matDD = CanvasModalBuilder.CreateCustomDropdown("MatDropdown", matRow, materials, 0, (idx) => {});
            matDD.AddComponent<LayoutElement>().flexibleWidth = 4; // Give it more weight

            GameObject setColorBackBtn = UIFactory.CreateObject("SetColorBack", matRow);
            var setColorBackLe = setColorBackBtn.AddComponent<LayoutElement>();
            setColorBackLe.minWidth = 36; setColorBackLe.minHeight = 36; setColorBackLe.preferredWidth = 36; setColorBackLe.preferredHeight = 36;
            setColorBackLe.flexibleWidth = 1; // Less weight
            setColorBackBtn.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
            Outline setColorBackOut = setColorBackBtn.AddComponent<Outline>(); setColorBackOut.effectColor = new Color(0.75f, 0.75f, 0.75f); setColorBackOut.effectDistance = new Vector2(1, 1);
            GameObject colorSwatch = UIFactory.CreateObject("ColorSwatch", setColorBackBtn);
            Image colorBoxImg = colorSwatch.AddComponent<Image>(); colorBoxImg.color = Color.white;
            colorSwatch.AddComponent<Outline>().effectColor = new Color(0.65f, 0.65f, 0.65f);
            RectTransform swRt = colorSwatch.GetComponent<RectTransform>();
            UIFactory.Stretch(swRt); swRt.offsetMin = new Vector2(6, 6); swRt.offsetMax = new Vector2(-6, -6);
            setColorBackBtn.AddComponent<Button>().onClick.AddListener(() => CanvasModalBuilder.CreateColorPickerModal(controller.editorArea, (c) => { colorBoxImg.color = c; controller.SetPaperColor(c); }));

            GameObject bgCheck = UIFactory.CreateObject("BGCheck", matSection);
            HorizontalLayoutGroup bchlg = bgCheck.AddComponent<HorizontalLayoutGroup>(); bchlg.spacing = 8; bchlg.childForceExpandWidth = false; bchlg.childControlWidth = true;
            Button syncBtn = UIFactory.CreateButton("\u2714", bgCheck, Vector2.zero, new Vector2(20, 20), UIFactory.COLOR_ACCENT_GREEN, Color.white).GetComponent<Button>();
            var syncLe = syncBtn.gameObject.AddComponent<LayoutElement>(); syncLe.minWidth = 20; syncLe.preferredWidth = 20; syncLe.minHeight = 20; syncLe.preferredHeight = 20;
            syncBtn.onClick.AddListener(() => {
                bool currentlyOn = syncBtn.GetComponent<Image>().color == UIFactory.COLOR_ACCENT_GREEN;
                bool nextOn = !currentlyOn;
                syncBtn.GetComponent<Image>().color = nextOn ? UIFactory.COLOR_ACCENT_GREEN : Color.gray;
                syncBtn.GetComponentInChildren<Text>().text = nextOn ? "\u2714" : "";
                controller.SetUseMaterialColor(nextOn);
                if (nextOn) controller.SetPaperColor(colorBoxImg.color);
            });
            var bgLabel = UIFactory.CreateText("Use material color as canvas background color \u24D8", bgCheck, 11, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            bgLabel.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        private static void CreateQualitySection(GameObject parent, CanvasController controller)
        {
            GameObject qualSection = UIFactory.CreateObject("Quality", parent);
            VerticalLayoutGroup qavg = qualSection.AddComponent<VerticalLayoutGroup>(); qavg.spacing = 10;
            UIFactory.CreateText("Quality", qualSection, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);
            CanvasModalBuilder.CreateCustomDropdown("QualDropdown", qualSection, new[]{ "High Quality", "Standard", "Draft" }, 0, (idx) => {});
        }

        private static void CreateGlobalPrintSettingsSection(GameObject parent, CanvasController controller)
        {
            GameObject section = UIFactory.CreateObject("GlobalPrintSettings", parent);
            VerticalLayoutGroup vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;

            UIFactory.CreateText("Print Settings", section, 14, Color.black, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft, FontStyle.Bold);

            // DPI
            CanvasModalBuilder.CreateCustomDropdown("PrintDpiDropdown", section,
                new[] { "180 DPI", "240 DPI", "300 DPI", "360 DPI", "600 DPI" }, 3,
                (idx) =>
                {
                    int[] values = { 180, 240, 300, 360, 600 };
                    if (idx >= 0 && idx < values.Length) controller.printResolutionDpi = values[idx];
                });

            // Color mode
            CanvasModalBuilder.CreateCustomDropdown("PrintColorModeDropdown", section,
                new[] { "CMYK", "Gray" }, 0,
                (idx) => { controller.printColorMode = idx == 1 ? "Gray" : "CMYK"; });

            // Paper size
            CanvasModalBuilder.CreateCustomDropdown("PrintPaperSizeDropdown", section,
                new[] { "A4", "A3", "Custom" }, 0,
                (idx) =>
                {
                    if (idx == 1) controller.printPaperSize = "A3";
                    else if (idx == 2) controller.printPaperSize = "custom";
                    else controller.printPaperSize = "A4";
                });

            // Media
            CanvasModalBuilder.CreateCustomDropdown("PrintMediaDropdown", section,
                new[] { "plain", "photo", "canvas" }, 0,
                (idx) =>
                {
                    if (idx == 1) controller.printMediaType = "photo";
                    else if (idx == 2) controller.printMediaType = "canvas";
                    else controller.printMediaType = "plain";
                });

            // Toggles
            System.Action<string, bool, System.Action<bool>> addToggle = (label, defaultValue, setter) =>
            {
                GameObject row = UIFactory.CreateObject(label + "Row", section);
                row.AddComponent<LayoutElement>().minHeight = 28;
                HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 8;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childForceExpandWidth = false;

                GameObject btnObj = UIFactory.CreateButton(defaultValue ? "✔" : "", row, Vector2.zero, new Vector2(24, 24),
                    defaultValue ? UIFactory.COLOR_ACCENT_GREEN : Color.gray, Color.white);
                Button btn = btnObj.GetComponent<Button>();
                Text txt = UIFactory.CreateText(label, row, 12, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft).GetComponent<Text>();
                txt.alignment = TextAnchor.MiddleLeft;
                bool state = defaultValue;
                setter(state);
                btn.onClick.AddListener(() =>
                {
                    state = !state;
                    btnObj.GetComponent<Image>().color = state ? UIFactory.COLOR_ACCENT_GREEN : Color.gray;
                    btnObj.GetComponentInChildren<Text>().text = state ? "✔" : "";
                    setter(state);
                });
            };

            addToggle("Halftone", true, (v) => controller.printEnableHalftone = v);
            addToggle("Ink Optimization", false, (v) => controller.printEnableInkOptimization = v);
            addToggle("Skin Detection", true, (v) => controller.printEnableSkinDetection = v);
            addToggle("Guided Filter", true, (v) => controller.printEnableGuidedFilter = v);
            addToggle("Show Ink Preview", true, (v) => controller.printShowInkPreview = v);
            addToggle("Mirror Print", false, (v) => controller.printMirror = v);
        }

        private static void CreateChokeSection(GameObject parent, CanvasController controller)
        {
            GameObject chokeSection = UIFactory.CreateObject("Choke", parent);
            chokeSection.AddComponent<LayoutElement>().minHeight = 60;
            VerticalLayoutGroup cavg = chokeSection.AddComponent<VerticalLayoutGroup>(); cavg.spacing = 5;
            GameObject chokeHeader = UIFactory.CreateObject("Header", chokeSection);
            chokeHeader.AddComponent<LayoutElement>().minHeight = 20;
            chokeHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("White Underbase Choke", chokeHeader, 13, Color.gray, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            GameObject chokeInfoBtn = UIFactory.CreateObject("ChokeInfo", chokeHeader);
            chokeInfoBtn.AddComponent<LayoutElement>().minWidth = 20; chokeInfoBtn.GetComponent<LayoutElement>().minHeight = 20;
            Text ciText = chokeInfoBtn.AddComponent<Text>();
            ciText.text = "\u24D8"; ciText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); ciText.color = Color.gray; ciText.alignment = TextAnchor.MiddleCenter; ciText.fontSize = 14; ciText.raycastTarget = true;
            chokeInfoBtn.AddComponent<Button>().onClick.AddListener(() => CanvasModalBuilder.CreateChokeModal(controller.editorArea));
            Text chokeValue = UIFactory.CreateText("0.2 mm", chokeHeader, 13, Color.black, Vector2.zero, new Vector2(60, 20), TextAnchor.MiddleRight, FontStyle.Bold).GetComponent<Text>();

            GameObject sliderObj = UIFactory.CreateObject("Slider", chokeSection);
            sliderObj.AddComponent<LayoutElement>().minHeight = 24;
            UIFactory.Stretch(sliderObj.GetComponent<RectTransform>());
            Slider slider = sliderObj.AddComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 0.5f; slider.value = 0.2f;
            slider.onValueChanged.AddListener((v) => chokeValue.text = $"{v:F1} mm");
            GameObject track = UIFactory.CreateObject("Track", sliderObj);
            RectTransform tr = track.GetComponent<RectTransform>(); tr.anchorMin = new Vector2(0, 0.5f); tr.anchorMax = new Vector2(1, 0.5f); tr.sizeDelta = new Vector2(0, 6);
            track.AddComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
            GameObject fill = UIFactory.CreateObject("Fill", track);
            RectTransform fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0.4f, 1); fr.sizeDelta = Vector2.zero;
            fill.AddComponent<Image>().color = UIFactory.COLOR_ACCENT_GREEN; slider.fillRect = fr;
            GameObject handle = UIFactory.CreateObject("Handle", sliderObj);
            RectTransform hr = handle.GetComponent<RectTransform>(); hr.anchorMin = new Vector2(0.4f, 0.5f); hr.anchorMax = new Vector2(0.4f, 0.5f); hr.sizeDelta = new Vector2(24, 24); hr.pivot = new Vector2(0.5f, 0.5f);
            Image handleImg = handle.AddComponent<Image>();
            Sprite brushSprite = Resources.Load<Sprite>("EditIcons/p_brush_press");
            if (brushSprite == null)
            {
                // Fallback: try loading as Texture2D if Sprite conversion hasn't run
                Texture2D tex = Resources.Load<Texture2D>("EditIcons/p_brush_press");
                if (tex != null)
                {
                    brushSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (brushSprite != null)
            {
                handleImg.sprite = brushSprite;
                handleImg.color = Color.white;
                handleImg.preserveAspect = true;
            }
            else
            {
                handleImg.color = Color.white;
                Outline handleOut = handle.AddComponent<Outline>(); handleOut.effectColor = new Color(0.6f, 0.6f, 0.6f); handleOut.effectDistance = new Vector2(1, 1);
            }
            slider.handleRect = hr;
        }

        private static void CreatePrintAreaSection(GameObject parent, CanvasController controller)
        {
            GameObject areaBox = UIFactory.CreateObject("AreaBox", parent);
            areaBox.AddComponent<LayoutElement>().minHeight = 80;
            areaBox.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f);
            VerticalLayoutGroup abvlg = areaBox.AddComponent<VerticalLayoutGroup>(); abvlg.padding = new RectOffset(15, 15, 10, 10); abvlg.spacing = 10;
            GameObject areaHeader = UIFactory.CreateObject("Header", areaBox);
            areaHeader.AddComponent<LayoutElement>().minHeight = 20;
            areaHeader.AddComponent<HorizontalLayoutGroup>();
            UIFactory.CreateText("\u2304 Print area", areaHeader, 13, Color.black, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, FontStyle.Bold);
            GameObject listContainer = UIFactory.CreateObject("ListContainer", areaBox);
            listContainer.AddComponent<VerticalLayoutGroup>().spacing = 5;
            listContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            controller.printAreaListContainer = listContainer;
        }
    }
}


