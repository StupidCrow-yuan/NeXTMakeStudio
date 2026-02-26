using UnityEngine;

namespace PocoRender.UI.TextureEffects
{
    public static class HeightMapGenerator
    {
        private static bool _loggedSentisStatus;
        private static bool _loggedSentisFallbackOnce;
        private static bool _loggedSentisSuccessOnce;

        /// <summary>
        /// Generate a grayscale height map (0..1) from a source sprite texture (cropped to sprite rect).
        /// This is a CPU fallback (fast + deterministic) that we can later replace with real depth models.
        /// </summary>
        public static Texture2D GenerateHeightMap(Texture2D spriteTex, TextureMode mode)
        {
            if (spriteTex == null) return null;

            // DepthAnything v2 (Sentis): only for ReliefTexture; output must match spriteTex size
            if (mode == TextureMode.ReliefTexture)
            {
                if (!_loggedSentisStatus)
                {
                    _loggedSentisStatus = true;
#if HAS_SENTIS
                    Debug.Log("[DepthAnythingV2] HAS_SENTIS=ON. Will try Sentis for Relief Texture.");
#else
                    Debug.LogWarning("[DepthAnythingV2] HAS_SENTIS=OFF. Relief Texture will use CPU fallback until Sentis is installed.");
#endif
                }

#if !HAS_SENTIS
                // Sentis未安装/未解析时，直接走CPU fallback；不要再输出 baseOnnxModel 等误导信息。
#else
                var settings = Resources.Load<DepthAnythingV2Settings>("DepthAnythingV2Settings");
                if (settings == null)
                {
                    if (!_loggedSentisFallbackOnce)
                    {
                        _loggedSentisFallbackOnce = true;
                        Debug.LogWarning("[DepthAnythingV2] Settings asset not found at Resources/DepthAnythingV2Settings. Using CPU fallback.");
                    }
                }
                else if (settings.baseOnnxModel == null)
                {
                    if (settings.verboseLogging && !_loggedSentisFallbackOnce)
                    {
                        _loggedSentisFallbackOnce = true;
                        Debug.LogWarning("[DepthAnythingV2] Settings.baseOnnxModel is null. Using CPU fallback.");
                    }
                }
                else
                {
                    DepthAnythingV2SentisEstimator.Instance.Configure(settings);
                    Texture2D da = DepthAnythingV2SentisEstimator.Instance.EstimateDepth(spriteTex, spriteTex.width, spriteTex.height);
                    if (da != null)
                    {
                        if (settings.verboseLogging && !_loggedSentisSuccessOnce)
                        {
                            _loggedSentisSuccessOnce = true;
                            Debug.Log($"[DepthAnythingV2] Depth generated via Sentis. size={da.width}x{da.height}");
                        }
                        return da;
                    }

                    if (settings.verboseLogging && !_loggedSentisFallbackOnce)
                    {
                        _loggedSentisFallbackOnce = true;
                        Debug.LogWarning("[DepthAnythingV2] Sentis inference failed; falling back to CPU depth.");
                    }
                }
#endif
            }

            int w = spriteTex.width;
            int h = spriteTex.height;
            var src = spriteTex.GetPixels32();

            // Use R8 when possible, but RGBA32 is fine for Unity 2022 preview usage.
            Texture2D height = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            Color32[] outPx = new Color32[src.Length];

            // Detect whether alpha channel contains meaningful mask information.
            // JPG or fully-opaque PNG typically has alpha=1 everywhere; in that case alpha-as-height would become "flat raised" over the whole image.
            int aMin = 255, aMax = 0;
            for (int i = 0; i < src.Length; i++)
            {
                byte a = src[i].a;
                if (a < aMin) aMin = a;
                if (a > aMax) aMax = a;
            }
            bool alphaHasSignal = (aMax - aMin) > 8; // ~3% range

            // Parameters per mode
            float noiseFreq = (mode == TextureMode.PatternTexture) ? 10f : 6f;
            float noiseAmp = (mode == TextureMode.PatternTexture) ? 0.35f : 0.15f;
            float reliefBoost = (mode == TextureMode.ReliefTexture) ? 1.25f : 1.0f;

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    int i = y * w + x;
                    Color32 c = src[i];

                    // Base height: alpha-driven (keeps transparent areas flat)
                    float a = alphaHasSignal ? (c.a / 255f) : 1f;
                    float lum = (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;

                    float baseH;
                    switch (mode)
                    {
                        case TextureMode.FlatRaised:
                            // If alpha is meaningful, use it as height. Otherwise (JPG/opaque), use luminance to avoid "uniform raise".
                            baseH = alphaHasSignal ? a : Mathf.Pow(lum, 0.9f);
                            break;
                        case TextureMode.PatternTexture:
                            // Pattern: alpha mask + procedural noise
                            baseH = a * (0.35f + 0.65f * lum);
                            baseH += a * (Mathf.PerlinNoise(nx * noiseFreq, ny * noiseFreq) - 0.5f) * noiseAmp;
                            break;
                        case TextureMode.ReliefTexture:
                            // Relief: luminance-as-depth placeholder (DepthAnything v2 can replace this)
                            baseH = a * Mathf.Pow(lum, 0.85f) * reliefBoost;
                            // Mild edge emphasis
                            baseH += a * (Mathf.PerlinNoise(nx * noiseFreq, ny * noiseFreq) - 0.5f) * 0.08f;
                            break;
                        default:
                            baseH = a;
                            break;
                    }

                    baseH = Mathf.Clamp01(baseH);
                    byte v = (byte)Mathf.RoundToInt(baseH * 255f);
                    outPx[i] = new Color32(v, v, v, 255);
                }
            }

            height.SetPixels32(outPx);
            height.Apply(false, false);
            height.wrapMode = TextureWrapMode.Clamp;
            height.filterMode = FilterMode.Bilinear;
            return height;
        }
    }
}



