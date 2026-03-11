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
            string[] folders = {
                "Assets/Resources/CanVas/Templates",
                "Assets/Resources/EditIcons"
            };

            int totalCount = 0;
            foreach (string folderPath in folders)
            {
                if (!Directory.Exists(folderPath))
                {
                    Debug.LogWarning($"[TemplateTools] Directory not found, skipping: {folderPath}");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folderPath });

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer != null && importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.SaveAndReimport();
                        totalCount++;
                        Debug.Log($"[TemplateTools] Converted to Sprite: {path}");
                    }
                }
            }

            if (totalCount > 0)
                Debug.Log($"[TemplateTools] Successfully converted {totalCount} images to Sprite format.");
            else
                Debug.Log("[TemplateTools] All images are already Sprites.");

            AssetDatabase.Refresh();
        }
    }
}
