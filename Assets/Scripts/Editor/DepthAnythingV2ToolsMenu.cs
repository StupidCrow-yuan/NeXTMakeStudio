using UnityEditor;
using UnityEngine;

namespace PocoRender.UI.EditorTools
{
    public static class DepthAnythingV2ToolsMenu
    {
        private const string VitsPath = "Assets/Resources/Models/depth_anything_v2_vits.onnx";
        private const string VitbPath = "Assets/Resources/Models/depth_anything_v2_vitb.onnx";

        [MenuItem("Tools/DepthAnythingV2/Force Bind Settings Model")]
        private static void ForceBind()
        {
#if !HAS_SENTIS
            Debug.LogWarning("[DepthAnythingV2] HAS_SENTIS=OFF. 请先通过 Package Manager 安装 com.unity.sentis（安装成功后 manifest.json 会出现 dependencies: com.unity.sentis）。");
            return;
#else
            DepthAnythingV2ModelAutoBind.TryBind();
#endif
        }

        [MenuItem("Tools/DepthAnythingV2/Diagnose ONNX Import Status")]
        private static void Diagnose()
        {
            DiagnoseOne(VitsPath);
            DiagnoseOne(VitbPath);
        }

        private static void DiagnoseOne(string path)
        {
            var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
            var mainObj = AssetDatabase.LoadMainAssetAtPath(path);
            string mainTypeName = mainType != null ? mainType.FullName : "(null)";
            string mainObjTypeName = mainObj != null ? mainObj.GetType().FullName : "(null)";
            Debug.Log($"[DepthAnythingV2] ONNX asset='{path}'\n  mainAssetType={mainTypeName}\n  mainObjType={mainObjTypeName}");

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            Debug.Log($"[DepthAnythingV2] '{path}' has {allAssets.Length} sub-assets.");
            foreach (var a in allAssets)
            {
                if (a != null) Debug.Log($"   - subAsset: {a.name} ({a.GetType().FullName})");
            }

#if HAS_SENTIS
            var model = AssetDatabase.LoadAssetAtPath<Unity.Sentis.ModelAsset>(path);
            Debug.Log($"[DepthAnythingV2] LoadAssetAtPath<ModelAsset> => {(model != null ? "OK" : "NULL")}");
#endif
        }
    }
}


