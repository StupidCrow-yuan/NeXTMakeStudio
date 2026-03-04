using UnityEngine;

namespace PocoRender.Utils
{
    /// <summary>
    /// Shader.Find() returns null in builds when the shader is not referenced
    /// by any material in the project's assets. This helper provides safe
    /// fallbacks using primitive materials (always available in builds).
    /// </summary>
    public static class SafeShaderHelper
    {
        private static Shader _cachedStandard;
        private static Shader _cachedUIDefault;

        /// <summary>
        /// Get the Standard shader safely. Falls back to the shader from a
        /// temporary Cube primitive (guaranteed to exist in any Unity build).
        /// </summary>
        public static Shader GetStandardShader()
        {
            if (_cachedStandard != null) return _cachedStandard;

            _cachedStandard = Shader.Find("Standard");
            if (_cachedStandard != null) return _cachedStandard;

            // Fallback: create a temp primitive and steal its shader
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var rend = temp.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
                _cachedStandard = rend.sharedMaterial.shader;
            Object.DestroyImmediate(temp);

            if (_cachedStandard == null)
                Debug.LogWarning("[SafeShaderHelper] Could not find any Standard-like shader");

            return _cachedStandard;
        }

        /// <summary>
        /// Get the UI/Default shader safely.
        /// </summary>
        public static Shader GetUIDefaultShader()
        {
            if (_cachedUIDefault != null) return _cachedUIDefault;

            _cachedUIDefault = Shader.Find("UI/Default");
            if (_cachedUIDefault != null) return _cachedUIDefault;

            // Fallback: UI/Default is used by Unity's Image component
            var tempObj = new GameObject("_tempUI");
            var img = tempObj.AddComponent<UnityEngine.UI.Image>();
            if (img.material != null && img.material.shader != null)
                _cachedUIDefault = img.material.shader;
            Object.DestroyImmediate(tempObj);

            return _cachedUIDefault;
        }

        /// <summary>
        /// Create a new Material using the Standard shader (build-safe).
        /// Returns null only if no shader is available at all.
        /// </summary>
        public static Material CreateStandardMaterial()
        {
            var shader = GetStandardShader();
            return shader != null ? new Material(shader) : null;
        }
    }
}
