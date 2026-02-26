using UnityEngine;
using UnityEngine.UI;
using NeXTMake.UI.Core;

namespace NeXTMake.UI.Modules
{
    /// <summary>
    /// Orchestrator for building the Canvas Editor page.
    /// Delegates to focused builder classes for each section.
    /// </summary>
    public class CanvasModule
    {
        public static void CreateCanvasEditor(GameObject parent)
        {
            GameObject editorArea = UIFactory.CreateObject("EditorArea", parent);
            UIFactory.Stretch(editorArea.GetComponent<RectTransform>()); 

            CanvasController controller = editorArea.AddComponent<CanvasController>();
            controller.editorArea = editorArea;

            // 1. Workspace (center canvas area)
            GameObject workspace = UIFactory.CreateObject("Workspace", editorArea);
            RectTransform wsRect = workspace.GetComponent<RectTransform>();
            wsRect.anchorMin = new Vector2(0.3f, 0); wsRect.anchorMax = new Vector2(0.75f, 1);
            wsRect.offsetMin = Vector2.zero; wsRect.offsetMax = new Vector2(0, -30);
            workspace.AddComponent<Image>().color = new Color(0.93f, 0.93f, 0.94f); 
            workspace.AddComponent<RectMask2D>();

            // Background click to deselect
            GameObject bgBtnObj = UIFactory.CreateObject("BGDeselector", workspace);
            UIFactory.Stretch(bgBtnObj.GetComponent<RectTransform>());
            Image bgImg = bgBtnObj.AddComponent<Image>(); bgImg.color = Color.clear; bgImg.raycastTarget = true;
            bgBtnObj.AddComponent<Button>().onClick.AddListener(() => controller.Deselect());
            bgBtnObj.AddComponent<CanvasDragger>().controller = controller;

            // 2. Paper (the white canvas)
            GameObject paper = UIFactory.CreateObject("Paper", workspace);
            Image paperImg = paper.AddComponent<Image>(); paperImg.color = Color.white;
            controller.paperBackground = paperImg;
            paper.AddComponent<Outline>().effectColor = new Color(0.8f, 0.8f, 0.8f);
            paper.AddComponent<Button>().onClick.AddListener(() => controller.Deselect());
            paper.AddComponent<CanvasDragger>().controller = controller;
            RectTransform pRect = paper.GetComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(600, 600);
            controller.paper = pRect;
            
            // 3. Workspace sub-components (rulers, zoom, toolbar, layers)
            var (bRuler, rRuler) = CanvasWorkspaceBuilder.CreateRulers(workspace);
            controller.bottomRuler = bRuler;
            controller.rightRuler = rRuler;
            CanvasWorkspaceBuilder.CreateBottomControls(workspace, controller);
            CanvasWorkspaceBuilder.CreateContextToolbar(workspace, controller);
            CanvasWorkspaceBuilder.CreateLayersPanel(workspace, controller);
            controller.UpdateRulers();
            
            // 4. Left panel (tool buttons + drawer)
            CanvasLeftPanelBuilder.SetupLeftMenu(editorArea, pRect, controller);
            
            // 5. Right panel (layer info, global settings, buttons)
            CanvasRightPanelBuilder.CreateRightPanel(editorArea, controller);
        }

        // Keep public API for backward compatibility
        public static GameObject CreateModalPopup(GameObject root, string title)
            => CanvasModalBuilder.CreateModalPopup(root, title);

        public static GameObject CreateColorPicker(GameObject root)
            => CanvasModalBuilder.CreateColorPicker(root);
    }
}
