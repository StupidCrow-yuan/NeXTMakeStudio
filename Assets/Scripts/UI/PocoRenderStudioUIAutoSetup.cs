using UnityEngine;
using UnityEngine.UI;
using PocoRender.Core;
using PocoRender.UI.Core;
using PocoRender.UI.Modules;
using System.Collections.Generic;

namespace PocoRender.UI
{
    public class PocoRenderStudioUIAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup Settings")]
        public bool autoCreateOnStart = true;
        public bool replaceOldUI = true;
        public Font defaultFont;

        void Start()
        {
            if (autoCreateOnStart) SetupPocoRenderStudioUI();
        }

        public void SetupPocoRenderStudioUI()
        {
            Debug.Log($"[PocoRenderStudio] Building UI... Version: 4.4_ModularUpdate");

            // Initialize Factory Settings
            UIFactory.DefaultFont = defaultFont;

            Canvas canvas = UIFactory.FindOrCreateCanvas();
            if (replaceOldUI) UIFactory.CleanupOldUI(canvas);

            GameObject mainContainer = UIFactory.CreateObject("PocoRenderStudioUIContainer", canvas.gameObject);
            RectTransform mainRect = mainContainer.GetComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero; mainRect.anchorMax = Vector2.one; mainRect.sizeDelta = Vector2.zero;

            PocoRenderStudioUIManager uiManager = mainContainer.AddComponent<PocoRenderStudioUIManager>();
            uiManager.mainContainer = mainRect;
            uiManager.rootCanvas = canvas;
            uiManager.printModeManager = mainContainer.AddComponent<PrintModeManager>();

            CreateSelectionDialog(mainContainer, uiManager);
            
            // Create UV Print Layout (Home Module)
            HomeModule homeModule = new HomeModule();
            // Note: AddNewCanvas callback logic is internal to HomeModule in my refactoring, 
            // but CreateUVPrintLayout accepts a callback? 
            // In my refactoring, CreateUVPrintLayout accepts (parent, manager, addCanvasCallback).
            // But who provides addCanvasCallback?
            // In the original code, AddNewCanvas was defined in CreateUVPrintLayout scope.
            // In HomeModule.CreateUVPrintLayout, I defined AddNewCanvas inside it.
            // And CreateUVPrintLayout takes 'addCanvasCallback' as argument... wait.
            // In HomeModule.cs, I defined AddNewCanvas inside CreateUVPrintLayout and passed IT to CreateHomeViewContent.
            // The 'addCanvasCallback' parameter in CreateUVPrintLayout is unused in my HomeModule implementation?
            // Let's check HomeModule.cs again.
            // Line 110: "Define AddCanvas Action".
            // Line 149: "CreateHomeViewContent(homeView, AddNewCanvas);"
            // So the parameter 'addCanvasCallback' in CreateUVPrintLayout signature (Line 13) is NOT used for creating new canvases.
            // It might be used for EXTERNAL calls if needed, but here we just pass null or empty action.
            
            homeModule.CreateUVPrintLayout(mainContainer, uiManager, null);
            
            // Create 3D Print Layout
            Print3DModule.CreatePrint3DLayout(mainContainer, uiManager);

            uiManager.Initialize();
            Debug.Log("[PocoRenderStudio] UI setup completed.");
        }

        // --- 1. Selection Dialog ---
        void CreateSelectionDialog(GameObject parent, PocoRenderStudioUIManager manager)
        {
            GameObject overlay = UIFactory.CreateObject("DialogOverlay", parent);
            Image overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);
            UIFactory.Stretch(overlay.GetComponent<RectTransform>());

            GameObject dialog = UIFactory.CreateObject("SelectionDialog", overlay);
            Image dialogBg = dialog.AddComponent<Image>(); dialogBg.color = Color.white;
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f); dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero; dialogRect.sizeDelta = new Vector2(900, 600);

            UIFactory.CreateText("Select a Studio", dialog, 32, UIFactory.COLOR_TEXT_DARK, new Vector2(0, 240), new Vector2(400, 60), TextAnchor.MiddleCenter, FontStyle.Bold);

            GameObject cardsRow = UIFactory.CreateObject("CardsRow", dialog);
            HorizontalLayoutGroup hlg = cardsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 60; hlg.childAlignment = TextAnchor.MiddleCenter; hlg.childControlWidth = false; hlg.childControlHeight = false;
            UIFactory.Stretch(cardsRow.GetComponent<RectTransform>());

            GameObject uvCard = UIFactory.CreateSelectionCard("UV Print Studio", cardsRow);
            GameObject p3dCard = UIFactory.CreateSelectionCard("3D Print Studio", cardsRow);

            GameObject confirmBtn = UIFactory.CreateButton("Enter Studio", dialog, new Vector2(0, -220), new Vector2(200, 50), UIFactory.COLOR_ACCENT_GREEN, Color.white);

            StudioSelectionDialog script = dialog.AddComponent<StudioSelectionDialog>();
            script.dialogContainer = dialog; script.backgroundOverlay = overlayImg;
            script.uvPrintCard = uvCard; script.print3DCard = p3dCard;
            script.uvPrintBorder = uvCard.GetComponent<Image>(); script.print3DBorder = p3dCard.GetComponent<Image>();
            script.uvPrintCheckmark = uvCard.transform.Find("Checkmark")?.GetComponent<Image>();
            script.print3DCheckmark = p3dCard.transform.Find("Checkmark")?.GetComponent<Image>();
            script.confirmButton = confirmBtn.GetComponent<Button>();
            
            // 不需要在这里绑定点击事件，因为StudioSelectionDialog的Initialize方法会自动绑定
            // 我们只需要确保StudioSelectionDialog的Initialize方法被正确调用

            manager.selectionDialog = script;
            // 手动调用Initialize方法，确保所有事件监听器都被正确绑定
            // 因为如果我们立即将overlay设置为隐藏，Start方法可能不会被调用
            script.Initialize();
            overlay.SetActive(false);
        }
    }
}


