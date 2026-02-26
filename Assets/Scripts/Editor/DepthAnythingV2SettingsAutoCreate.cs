using UnityEditor;
using UnityEngine;

namespace PocoRender.UI.EditorTools
{
    [InitializeOnLoad]
    public static class DepthAnythingV2SettingsAutoCreate
    {
        private const string AssetPath = "Assets/Resources/DepthAnythingV2Settings.asset";

        static DepthAnythingV2SettingsAutoCreate()
        {
            EditorApplication.delayCall += EnsureAsset;
        }

        private static void EnsureAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PocoRender.UI.TextureEffects.DepthAnythingV2Settings>(AssetPath);
            if (existing != null) return;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var asset = ScriptableObject.CreateInstance<PocoRender.UI.TextureEffects.DepthAnythingV2Settings>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DepthAnythingV2] Created default settings asset at: " + AssetPath + " (please assign ONNX ModelAsset).");
        }
    }
}



