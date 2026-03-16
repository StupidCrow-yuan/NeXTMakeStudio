using UnityEditor;
using UnityEngine;

#if HAS_SENTIS
using Unity.Sentis;
#endif

namespace PocoRender.UI.EditorTools
{
    /// <summary>
    /// Auto-binds DepthAnything v2 ONNX ModelAsset into Resources/DepthAnythingV2Settings.asset
    /// so the team doesn't need to manually drag references.
    /// Priority: vits (small) -> vitb (base).
    /// </summary>
    [InitializeOnLoad]
    public static class DepthAnythingV2ModelAutoBind
    {
        private const string SettingsPath = "Assets/Resources/DepthAnythingV2Settings.asset";
        private const string VitsPath = "Assets/Resources/Models/depth_anything_v2_vits.onnx";
        private const string VitbPath = "Assets/Resources/Models/depth_anything_v2_vitb.onnx";
        private static bool _loggedNotFoundOnce;

        static DepthAnythingV2ModelAutoBind()
        {
            EditorApplication.delayCall += TryBind;
        }

        internal static void TryBind()
        {
#if !HAS_SENTIS
            // Sentis not installed or not resolved; skip binding.
            return;
#else
            var settings = AssetDatabase.LoadAssetAtPath<PocoRender.UI.TextureEffects.DepthAnythingV2Settings>(SettingsPath);
            if (settings == null)
            {
                Debug.LogWarning("[DepthAnythingV2] Settings asset not found: " + SettingsPath);
                return;
            }

            // If already bound, do nothing
            if (settings.baseOnnxModel != null)
            {
                if (settings.verboseLogging)
                {
                    Debug.Log("[DepthAnythingV2] Settings already has baseOnnxModel bound: " + AssetDatabase.GetAssetPath(settings.baseOnnxModel));
                }
                return;
            }

            ModelAsset model = null;
            // 1) Try fixed paths (team convention)
            model = AssetDatabase.LoadAssetAtPath<ModelAsset>(VitsPath);
            if (model == null) model = AssetDatabase.LoadAssetAtPath<ModelAsset>(VitbPath);

            // 2) Fallback: search in project (Sentis 2.x importer still yields ModelAsset at the asset path)
            if (model == null)
            {
                string[] vits = AssetDatabase.FindAssets("depth_anything_v2_vits t:ModelAsset");
                foreach (var guid in vits)
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    var m = AssetDatabase.LoadAssetAtPath<ModelAsset>(p);
                    if (m == null)
                    {
                        var all = AssetDatabase.LoadAllAssetsAtPath(p);
                        foreach(var sub in all)
                        {
                            if (sub is ModelAsset ma) { m = ma; break; }
                        }
                    }
                    if (m != null) { model = m; break; }
                }
            }
            if (model == null)
            {
                string[] vitb = AssetDatabase.FindAssets("depth_anything_v2_vitb t:ModelAsset");
                foreach (var guid in vitb)
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    var m = AssetDatabase.LoadAssetAtPath<ModelAsset>(p);
                    if (m == null)
                    {
                        var all = AssetDatabase.LoadAllAssetsAtPath(p);
                        foreach(var sub in all)
                        {
                            if (sub is ModelAsset ma) { m = ma; break; }
                        }
                    }
                    if (m != null) { model = m; break; }
                }
            }

            if (model == null)
            {
                if (settings.verboseLogging && !_loggedNotFoundOnce)
                {
                    _loggedNotFoundOnce = true;
                    Debug.LogWarning("[DepthAnythingV2] ModelAsset not found yet. 请确认 ONNX 已被 Sentis 导入为 ModelAsset。\n" +
                                     "- 优先检查固定路径: " + VitsPath + " / " + VitbPath + "\n" +
                                     "- 或在Project里搜索: t:ModelAsset depth_anything_v2_vits / vitb\n" +
                                     "你可以右键 ONNX -> Reimport，然后再点 Tools/DepthAnythingV2/Force Bind Settings Model。");
                }
                return;
            }

            settings.baseOnnxModel = model;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("[DepthAnythingV2] Auto-bound model: " + AssetDatabase.GetAssetPath(model));
#endif
        }
    }
}



