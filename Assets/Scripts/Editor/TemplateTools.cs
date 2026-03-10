using UnityEngine;
using UnityEditor;
using System.IO;

namespace PocoRender.Editor
{
    public static class TemplateTools
    {
        [MenuItem("PocoRender/Fix Template Images (Convert to Sprite)")]
        public static void FixTemplateImages()
        {
            string folderPath = "Assets/Resources/CanVas/Templates";
            
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"[TemplateTools] Directory not found: {folderPath}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folderPath });
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    bool changed = false;

                    // 1. Ensure it's a Sprite
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }

                    // 2. Ensure it's placed in UI correctly (optional but good for UI)
                    // importer.spriteImportMode = SpriteImportMode.Single; 

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        count++;
                        Debug.Log($"[TemplateTools] Converted to Sprite: {path}");
                    }
                }
            }

            if (count > 0)
            {
                Debug.Log($"[TemplateTools] Successfully converted {count} images to Sprite format.");
            }
            else
            {
                Debug.Log("[TemplateTools] All images in Templates folder are already Sprites.");
            }
            
            AssetDatabase.Refresh();
        }
    }
}
