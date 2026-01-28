using UnityEditor;
using UnityEngine;

namespace NeXTMake.UI.EditorTools
{
    /// <summary>
    /// Re-tries binding after assets import (especially ONNX -> ModelAsset conversion).
    /// </summary>
    public class DepthAnythingV2ModelBindPostprocessor : AssetPostprocessor
    {
        private static bool IsDepthOnnx(string path)
        {
            return path.EndsWith("depth_anything_v2_vits.onnx") || path.EndsWith("depth_anything_v2_vitb.onnx");
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool touched = false;
            foreach (var p in importedAssets)
            {
                if (IsDepthOnnx(p)) { touched = true; break; }
            }
            if (!touched) return;

            // Delay once to allow Sentis importer to finish creating ModelAsset
            EditorApplication.delayCall += () =>
            {
                // Trigger the binder's logic by forcing domain reload-like behavior: call it via reflection-safe direct type usage.
                // (We keep the actual binding logic in DepthAnythingV2ModelAutoBind)
                try { DepthAnythingV2ModelAutoBind.TryBind(); }
                catch (System.Exception e) { Debug.LogWarning("[DepthAnythingV2] Postprocess bind retry failed: " + e.Message); }
            };
        }
    }
}



