using System;
using System.Reflection;
using UnityEngine;

#if HAS_SENTIS
using Unity.Sentis;
#endif

namespace PocoRender.UI.TextureEffects
{
    /// <summary>
    /// DepthAnything v2 (Sentis) estimator.
    /// - GPU优先创建Worker，失败则回退CPU
    /// - 输入：Texture2D (RGB)，输出：灰度深度图 Texture2D(RGBA32)，范围0..1
    /// </summary>
    public sealed class DepthAnythingV2SentisEstimator : IDisposable
    {
        private static DepthAnythingV2SentisEstimator _instance;
        public static DepthAnythingV2SentisEstimator Instance => _instance ??= new DepthAnythingV2SentisEstimator();

#if HAS_SENTIS
        private DepthAnythingV2Settings _settings;
        private Model _model;
        private Worker _worker;
        private BackendType _backendInUse = BackendType.CPU;
        private bool _loggedReadyOnce;

        public bool IsReady => _worker != null;
        public BackendType BackendInUse => _backendInUse;
#else
        // Sentis未安装时：始终不可用，外部会自动回退 CPU 伪深度
        public bool IsReady => false;
#endif

        private DepthAnythingV2SentisEstimator() { }

        public void Configure(DepthAnythingV2Settings settings)
        {
#if HAS_SENTIS
            if (_settings == settings && _worker != null) return;
            _settings = settings;
            RebuildWorker();
#else
            // no-op
#endif
        }

        private void RebuildWorker()
        {
#if HAS_SENTIS
            DisposeWorker();

            if (_settings == null || _settings.baseOnnxModel == null)
            {
                if (_settings != null && _settings.verboseLogging)
                {
                    Debug.LogWarning("[DepthAnythingV2] Configure skipped: settings or baseOnnxModel is null.");
                }
                return;
            }

            _model = ModelLoader.Load(_settings.baseOnnxModel);

            if (_settings.preferGPU)
            {
                // Try GPU first
                if (TryCreateWorker(BackendType.GPUCompute, out _worker))
                {
                    _backendInUse = BackendType.GPUCompute;
                    if (!_loggedReadyOnce && _settings.verboseLogging)
                    {
                        _loggedReadyOnce = true;
                        Debug.Log("[DepthAnythingV2] Worker ready. Backend=GPUCompute");
                    }
                    return;
                }
            }

            // Fallback CPU
            if (TryCreateWorker(BackendType.CPU, out _worker))
            {
                _backendInUse = BackendType.CPU;
                if (!_loggedReadyOnce && _settings.verboseLogging)
                {
                    _loggedReadyOnce = true;
                    Debug.Log("[DepthAnythingV2] Worker ready. Backend=CPU");
                }
            }
#endif
        }

#if HAS_SENTIS
        private bool TryCreateWorker(BackendType backend, out Worker worker)
        {
            worker = null;
            try
            {
                worker = new Worker(_model, backend);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DepthAnythingV2] CreateWorker({backend}) failed: {e.Message}");
                worker = null;
                return false;
            }
        }
#endif

        public Texture2D EstimateDepth(Texture2D srcRgb, int outW, int outH)
        {
#if HAS_SENTIS
            if (_settings == null || _settings.baseOnnxModel == null)
            {
                Debug.LogWarning("[DepthAnythingV2] Settings/model not configured. Using fallback generator.");
                return null;
            }
            if (_worker == null) RebuildWorker();
            if (_worker == null) return null;
            if (srcRgb == null) return null;

            int inSize = Mathf.Max(64, _settings.inputSize);
            Debug.Log($"[DepthAnythingV2] EstimateDepth start. inSize={inSize}, out={outW}x{outH}, inputName={_settings.inputName}, outputName={_settings.outputName}, backend={_backendInUse}");

            // Resize to square model input
            Texture2D resized = SpriteTextureUtil.ResizeBilinear(srcRgb, inSize, inSize);

            // Build input tensor: NCHW float
            var input = TextureToNCHWTensor(resized, _settings.useImagenetNorm);
            UnityEngine.Object.Destroy(resized);

            try
            {
                _worker.SetInput(_settings.inputName, input);
                _worker.Schedule();
            }
            catch
            {
                input.Dispose();
                Debug.LogError("[DepthAnythingV2] Execute failed. Please check model inputName / Sentis API compatibility.");
                return null;
            }

            input.Dispose();

            Tensor<float> output = null;
            try
            {
                output = _worker.PeekOutput(_settings.outputName) as Tensor<float>;
            }
            catch
            {
                // Fallback: take first output
                try
                {
                    output = _worker.PeekOutput() as Tensor<float>;
                }
                catch { }
            }

            if (output == null)
            {
                Debug.LogError("[DepthAnythingV2] Output tensor not found. Check outputName in settings.");
                return null;
            }

            // Download depth to CPU
            output.CompleteAllPendingOperations();
            float[] depth = output.DownloadToArray();

            // Determine tensor layout: [1,1,H,W] or [1,H,W] or [H,W]
            int h = inSize, w = inSize;
            if (output.shape.rank >= 2)
            {
                h = output.shape[output.shape.rank - 2];
                w = output.shape[output.shape.rank - 1];
            }

            // Normalize to 0..1
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int i = 0; i < depth.Length; i++)
            {
                float v = depth[i];
                if (v < min) min = v;
                if (v > max) max = v;
            }
            float inv = (max > min) ? 1f / (max - min) : 0f;

            Texture2D depthTex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            Color[] cols = new Color[w * h];

            // Assume last dims are H,W; depth array is contiguous
            int count = Mathf.Min(cols.Length, depth.Length);
            for (int i = 0; i < count; i++)
            {
                float v = (depth[i] - min) * inv;
                cols[i] = new Color(v, v, v, 1f);
            }
            depthTex.SetPixels(cols);
            depthTex.Apply();

            // Resize back to requested output size
            if (outW > 0 && outH > 0 && (depthTex.width != outW || depthTex.height != outH))
            {
                Texture2D final = SpriteTextureUtil.ResizeBilinear(depthTex, outW, outH);
                UnityEngine.Object.Destroy(depthTex);
                return final;
            }

            return depthTex;
#else
            return null;
#endif
        }

#if HAS_SENTIS
        private static Tensor<float> TextureToNCHWTensor(Texture2D tex, bool imagenetNorm)
        {
            int w = tex.width;
            int h = tex.height;

            var shape = new TensorShape(1, 3, h, w);
            var t = new Tensor<float>(shape);

            Color32[] pixels = tex.GetPixels32();

            // ImageNet defaults
            const float meanR = 0.485f, meanG = 0.456f, meanB = 0.406f;
            const float stdR = 0.229f, stdG = 0.224f, stdB = 0.225f;

            int hw = h * w;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int p = y * w + x;
                    float r = pixels[p].r / 255f;
                    float g = pixels[p].g / 255f;
                    float b = pixels[p].b / 255f;

                    if (imagenetNorm)
                    {
                        r = (r - meanR) / stdR;
                        g = (g - meanG) / stdG;
                        b = (b - meanB) / stdB;
                    }

                    t[0, 0, y, x] = r;
                    t[0, 1, y, x] = g;
                    t[0, 2, y, x] = b;
                }
            }

            return t;
        }
#endif

        private void DisposeWorker()
        {
#if HAS_SENTIS
            if (_worker != null)
            {
                _worker.Dispose();
                _worker = null;
            }
            _model = null;
#endif
        }

        public void Dispose()
        {
            DisposeWorker();
        }
    }
}



