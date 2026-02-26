#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using PocoRender.UI;
using PocoRender.UI.Core;
using PocoRender.UI.Modules;

/// <summary>
/// Editor utility to generate Prefabs from the current code-generated UI.
/// Run from menu: PocoRender > Generate Prefabs.
/// After generation, the runtime can instantiate these prefabs instead of code-gen.
/// </summary>
public static class PrefabGenerator
{
    private const string PrefabRoot = "Assets/Prefabs/UI";

    [MenuItem("PocoRender/Generate All Prefabs")]
    public static void GenerateAllPrefabs()
    {
        EnsureDirectory(PrefabRoot);
        GenerateCanvasEditorPrefab();
        GenerateSelectionDialogPrefab();
        Debug.Log("[PrefabGenerator] All prefabs generated in " + PrefabRoot);
        AssetDatabase.Refresh();
    }

    [MenuItem("PocoRender/Generate Canvas Editor Prefab")]
    public static void GenerateCanvasEditorPrefab()
    {
        EnsureDirectory(PrefabRoot);
        
        // Temporarily create UI in scene
        Canvas canvas = UIFactory.FindOrCreateCanvas();
        GameObject tempParent = UIFactory.CreateObject("TempCanvasEditor", canvas.gameObject);
        UIFactory.Stretch(tempParent.GetComponent<RectTransform>());
        
        CanvasModule.CreateCanvasEditor(tempParent);
        
        // Save the EditorArea child as prefab
        Transform editorArea = tempParent.transform.Find("EditorArea");
        if (editorArea != null)
        {
            string path = PrefabRoot + "/CanvasEditor.prefab";
            PrefabUtility.SaveAsPrefabAsset(editorArea.gameObject, path);
            Debug.Log("[PrefabGenerator] Saved: " + path);
        }
        
        Object.DestroyImmediate(tempParent);
    }

    [MenuItem("PocoRender/Generate Selection Dialog Prefab")]
    public static void GenerateSelectionDialogPrefab()
    {
        EnsureDirectory(PrefabRoot);
        
        Canvas canvas = UIFactory.FindOrCreateCanvas();
        
        // Build the selection dialog
        GameObject overlay = UIFactory.CreateObject("DialogOverlay", canvas.gameObject);
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        UIFactory.Stretch(overlay.GetComponent<RectTransform>());

        GameObject dialog = UIFactory.CreateObject("SelectionDialog", overlay);
        dialog.AddComponent<Image>().color = Color.white;
        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f); dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(900, 600);

        UIFactory.CreateText("Select a Studio", dialog, 32, UIFactory.COLOR_TEXT_DARK, new Vector2(0, 240), new Vector2(400, 60), TextAnchor.MiddleCenter, FontStyle.Bold);

        GameObject cardsRow = UIFactory.CreateObject("CardsRow", dialog);
        HorizontalLayoutGroup hlg = cardsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 60; hlg.childAlignment = TextAnchor.MiddleCenter; hlg.childControlWidth = false; hlg.childControlHeight = false;
        UIFactory.Stretch(cardsRow.GetComponent<RectTransform>());

        GameObject uvCard = UIFactory.CreateSelectionCard("UV Print Studio", cardsRow);
        GameObject p3dCard = UIFactory.CreateSelectionCard("3D Print Studio", cardsRow);
        GameObject confirmBtn = UIFactory.CreateButton("Enter Studio", dialog, new Vector2(0, -220), new Vector2(200, 50), UIFactory.COLOR_ACCENT_GREEN, Color.white);

        StudioSelectionDialog script = dialog.AddComponent<StudioSelectionDialog>();
        script.dialogContainer = dialog; script.backgroundOverlay = overlay.GetComponent<Image>();
        script.uvPrintCard = uvCard; script.print3DCard = p3dCard;
        script.uvPrintBorder = uvCard.GetComponent<Image>(); script.print3DBorder = p3dCard.GetComponent<Image>();
        script.uvPrintCheckmark = uvCard.transform.Find("Checkmark")?.GetComponent<Image>();
        script.print3DCheckmark = p3dCard.transform.Find("Checkmark")?.GetComponent<Image>();
        script.confirmButton = confirmBtn.GetComponent<Button>();

        string path = PrefabRoot + "/SelectionDialog.prefab";
        PrefabUtility.SaveAsPrefabAsset(overlay, path);
        Debug.Log("[PrefabGenerator] Saved: " + path);
        
        Object.DestroyImmediate(overlay);
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif


