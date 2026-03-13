using UnityEditor;
using UnityEngine;
using PocoRender.UI.TextureEffects;

namespace PocoRender.UI.EditorTools
{
    [InitializeOnLoad]
    public static class U2NetSettingsAutoCreate
    {
        private const string AssetPath = "Assets/Resources/Models/U2NetSettings.asset";

        static U2NetSettingsAutoCreate()
        {
            EditorApplication.delayCall += EnsureAsset;
        }

        private static void EnsureAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<U2NetSettings>(AssetPath);
            if (existing != null) return;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Models"))
                AssetDatabase.CreateFolder("Assets/Resources", "Models");

            var asset = ScriptableObject.CreateInstance<U2NetSettings>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[U2Net] Created U2NetSettings at " + AssetPath + ". Assign u2net ModelAsset (from Resources/Models) to Base Onnx Model.");
        }
    }
}
