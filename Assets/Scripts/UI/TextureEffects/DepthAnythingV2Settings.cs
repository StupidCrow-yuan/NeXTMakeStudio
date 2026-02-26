using UnityEngine;

#if HAS_SENTIS
using Unity.Sentis;
#endif

namespace PocoRender.UI.TextureEffects
{
    [CreateAssetMenu(fileName = "DepthAnythingV2Settings", menuName = "PocoRender/DepthAnything v2 Settings")]
    public class DepthAnythingV2Settings : ScriptableObject
    {
        [Header("Model")]
#if HAS_SENTIS
        public ModelAsset baseOnnxModel;
#else
        // Sentis未安装时占位，避免编译失败；安装后会自动切换为 ModelAsset 字段
        public UnityEngine.Object baseOnnxModel;
#endif

        [Header("I/O names (make these match your ONNX)")]
        public string inputName = "image";
        public string outputName = "predicted_depth";

        [Header("Preprocess")]
        [Tooltip("Model input is resized to this square size.")]
        public int inputSize = 384;

        [Tooltip("Use ImageNet mean/std normalization.")]
        public bool useImagenetNorm = true;

        [Header("Backend")]
        [Tooltip("GPU优先；如果GPU创建失败会自动回退到CPU。")]
        public bool preferGPU = true;

        [Header("Debug")]
        [Tooltip("开启后会输出模型绑定/推理后端/推理回退等关键日志（只打印少量）。")]
        public bool verboseLogging = true;
    }
}




