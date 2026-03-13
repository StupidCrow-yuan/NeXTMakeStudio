using UnityEngine;

#if HAS_SENTIS
using Unity.Sentis;
#endif

namespace PocoRender.UI.TextureEffects
{
    /// <summary>
    /// Settings for local U2-Net background removal (free, runs with Unity Sentis).
    /// Create via Assets → Create → PocoRender → U2-Net Settings, place in Resources as "U2NetSettings".
    /// Download u2net.onnx from: https://huggingface.co/danielgatis/rembg/tree/main (or tomjackson2023/rembg).
    /// </summary>
    [CreateAssetMenu(fileName = "U2NetSettings", menuName = "PocoRender/U2-Net Background Removal Settings")]
    public class U2NetSettings : ScriptableObject
    {
#if HAS_SENTIS
        [Header("Model")]
        public ModelAsset baseOnnxModel;
#else
        public UnityEngine.Object baseOnnxModel;
#endif

        [Header("I/O (match your ONNX)")]
        public string inputName = "input";
        public string outputName = "output";

        [Tooltip("Model input size (U2-Net usually 320).")]
        public int inputSize = 320;

        [Tooltip("Use ImageNet mean/std normalization.")]
        public bool useImagenetNorm = true;

        [Header("Backend")]
        public bool preferGPU = true;

        public bool IsValid => baseOnnxModel != null;

        private static U2NetSettings _cached;
        /// <summary>Load from Resources/Models/U2NetSettings or Resources/U2NetSettings.</summary>
        public static U2NetSettings Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<U2NetSettings>("Models/U2NetSettings");
            if (_cached == null)
                _cached = Resources.Load<U2NetSettings>("U2NetSettings");
            return _cached;
        }
    }
}
