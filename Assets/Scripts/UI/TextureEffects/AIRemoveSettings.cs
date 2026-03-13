using UnityEngine;

namespace PocoRender.UI.TextureEffects
{
    /// <summary>
    /// Settings for AI Remove (background removal). Uses PicWish 万物抠图 API when API Key is set.
    /// Create via Assets → Create → PocoRender → AI Remove Settings, then place in Resources folder
    /// and name it "AIRemoveSettings" so it can be loaded at runtime.
    /// Get API key: https://picwish.cn/background-removal-api-doc
    /// </summary>
    [CreateAssetMenu(fileName = "AIRemoveSettings", menuName = "PocoRender/AI Remove Settings")]
    public class AIRemoveSettings : ScriptableObject
    {
        [Header("PicWish 万物抠图 API (付费)")]
        [Tooltip("Optional. PicWish is paid (~0.5 pts/image). Prefer free U2-Net: add U2NetSettings with u2net.onnx in Resources.")]
        public string apiKey = "";

        [Tooltip("API endpoint (default: PicWish segmentation).")]
        public string apiUrl = "https://techsz.aoscdn.com/api/tasks/visual/segmentation";

        [Header("Limits")]
        [Tooltip("Max resolution for upload (longer side). PicWish supports up to 4096.")]
        public int maxUploadSize = 2048;

        public bool HasValidApiKey => !string.IsNullOrWhiteSpace(apiKey);

        private static AIRemoveSettings _cached;

        /// <summary>
        /// Load from Resources/AIRemoveSettings. Returns null if not found.
        /// </summary>
        public static AIRemoveSettings Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<AIRemoveSettings>("AIRemoveSettings");
            return _cached;
        }
    }
}
