using System;
using UnityEngine;

#if HAS_SENTIS
using Unity.Sentis;
#endif

namespace PocoRender.UI.TextureEffects
{
    /// <summary>
    /// Free local background removal using U2-Net ONNX + Unity Sentis.
    /// Same model family as rembg; no API key or cost.
    /// </summary>
    public sealed class U2NetBackgroundRemoval : IDisposable
    {
        private static U2NetBackgroundRemoval _instance;
        public static U2NetBackgroundRemoval Instance => _instance ??= new U2NetBackgroundRemoval();

#if HAS_SENTIS
        private U2NetSettings _settings;
        private Model _model;
        private Worker _worker;
        private bool _loggedOnce;
        private string _resolvedInputName;
        private string _resolvedOutputName;

        public bool IsReady => _worker != null;
#else
        public bool IsReady => false;
#endif

        private U2NetBackgroundRemoval() { }

        public void Configure(U2NetSettings settings)
        {
#if HAS_SENTIS
            if (_settings == settings && _worker != null) return;
            _settings = settings;
            RebuildWorker();
#endif
        }

#if HAS_SENTIS
        private void RebuildWorker()
        {
            DisposeWorker();
            if (_settings == null || !_settings.IsValid)
            {
                if (_settings != null) Debug.LogWarning("[U2Net] Settings invalid: baseOnnxModel not set.");
                return;
            }

            ModelAsset asset = _settings.baseOnnxModel as ModelAsset;
            if (asset == null)
            {
                Debug.LogWarning("[U2Net] baseOnnxModel is not a ModelAsset. Assign the ONNX (Sentis will use it as ModelAsset).");
                return;
            }

            try
            {
                _model = ModelLoader.Load(asset);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[U2Net] ModelLoader.Load failed: " + e.Message);
                return;
            }

            if (_model == null)
            {
                Debug.LogWarning("[U2Net] ModelLoader.Load returned null.");
                return;
            }

            // Read actual input/output names from model metadata
            _resolvedInputName = (_model.inputs != null && _model.inputs.Count > 0) ? _model.inputs[0].name : _settings.inputName;
            _resolvedOutputName = (_model.outputs != null && _model.outputs.Count > 0) ? _model.outputs[0].name : _settings.outputName;

            string inputsList = "";
            if (_model.inputs != null)
                foreach (var inp in _model.inputs) inputsList += inp.name + " ";
            string outputsList = "";
            if (_model.outputs != null)
                foreach (var outp in _model.outputs) outputsList += outp.name + " ";
            Debug.Log($"[U2Net] Model inputs: [{inputsList.Trim()}], outputs: [{outputsList.Trim()}]. Using input='{_resolvedInputName}', output='{_resolvedOutputName}'.");

            try
            {
                if (_settings.preferGPU && TryCreateWorker(BackendType.GPUCompute, out _worker))
                {
                    if (!_loggedOnce) { _loggedOnce = true; Debug.Log("[U2Net] Worker ready (GPU)."); }
                    return;
                }
            }
            catch (Exception e) { Debug.LogWarning("[U2Net] GPU failed: " + e.Message); }

            if (TryCreateWorker(BackendType.CPU, out _worker))
            {
                if (!_loggedOnce) { _loggedOnce = true; Debug.Log("[U2Net] Worker ready (CPU)."); }
            }
            else
                Debug.LogWarning("[U2Net] Worker creation failed (GPU and CPU).");
        }

        private bool TryCreateWorker(BackendType backend, out Worker worker)
        {
            worker = null;
            try
            {
                worker = new Worker(_model, backend);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DisposeWorker()
        {
            if (_worker != null) { _worker.Dispose(); _worker = null; }
            _model = null;
        }
#endif

        /// <summary>
        /// Run U2-Net and return texture with original RGB and alpha from mask. Returns null if not ready or error.
        /// </summary>
        public Texture2D RemoveBackground(Texture2D sourceRgb)
        {
#if HAS_SENTIS
            if (_worker == null || _settings == null || sourceRgb == null) return null;

            int inSize = Mathf.Max(64, _settings.inputSize);
            int origW = sourceRgb.width;
            int origH = sourceRgb.height;

            Texture2D resized = SpriteTextureUtil.ResizeBilinear(sourceRgb, inSize, inSize);
            if (resized == null) return null;

            Tensor<float> input = TextureToNCHW(resized);
            UnityEngine.Object.Destroy(resized);
            if (input == null) return null;

            try
            {
                _worker.SetInput(_resolvedInputName, input);
            }
            catch (Exception e)
            {
                input.Dispose();
                Debug.LogWarning($"[U2Net] SetInput('{_resolvedInputName}') failed: {e.Message}");
                return null;
            }

            try
            {
                _worker.Schedule();
            }
            catch (Exception e)
            {
                input.Dispose();
                Debug.LogWarning("[U2Net] Schedule failed: " + e.Message);
                return null;
            }

            input.Dispose();

            Tensor<float> maskTensor = null;
            try
            {
                maskTensor = _worker.PeekOutput(_resolvedOutputName) as Tensor<float>;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[U2Net] PeekOutput('{_resolvedOutputName}') failed: {e.Message}");
            }
            if (maskTensor == null)
            {
                try { maskTensor = _worker.PeekOutput() as Tensor<float>; }
                catch { }
            }
            if (maskTensor == null)
            {
                Debug.LogWarning("[U2Net] Output tensor is null after inference.");
                return null;
            }

            int mH = inSize, mW = inSize;
            if (maskTensor.shape.rank >= 2)
            {
                mH = maskTensor.shape[maskTensor.shape.rank - 2];
                mW = maskTensor.shape[maskTensor.shape.rank - 1];
            }
            int maskLen = mH * mW;

            maskTensor.CompleteAllPendingOperations();
            float[] maskData = maskTensor.DownloadToArray();
            maskTensor.Dispose();
            if (maskData.Length < maskLen)
            {
                Debug.LogWarning("[U2Net] Mask size mismatch: got " + maskData.Length + ", need " + maskLen);
                return null;
            }

            // If multi-channel (e.g. [1,7,H,W]), use last channel as final mask
            int offset = 0;
            if (maskData.Length > maskLen)
                offset = maskData.Length - maskLen;

            Texture2D maskTex = new Texture2D(mW, mH, TextureFormat.RGBA32, false, true);
            Color[] maskPixels = new Color[maskLen];
            for (int i = 0; i < maskLen; i++)
            {
                float v = Mathf.Clamp01(maskData[offset + i]);
                maskPixels[i] = new Color(v, v, v, v);
            }
            maskTex.SetPixels(maskPixels);
            maskTex.Apply();

            // Resize mask to original size
            Texture2D maskFull = SpriteTextureUtil.ResizeBilinear(maskTex, origW, origH);
            UnityEngine.Object.Destroy(maskTex);
            if (maskFull == null) return null;

            Color[] origPixels = sourceRgb.GetPixels();
            Color[] maskPixelsFull = maskFull.GetPixels();
            UnityEngine.Object.Destroy(maskFull);

            for (int i = 0; i < origPixels.Length && i < maskPixelsFull.Length; i++)
            {
                float a = maskPixelsFull[i].r;
                Color c = origPixels[i];
                origPixels[i] = new Color(c.r, c.g, c.b, c.a * a);
            }

            Texture2D result = new Texture2D(origW, origH, TextureFormat.RGBA32, false, true);
            result.SetPixels(origPixels);
            result.Apply();
            return result;
#else
            return null;
#endif
        }

#if HAS_SENTIS
        private Tensor<float> TextureToNCHW(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            var shape = new TensorShape(1, 3, h, w);
            var t = new Tensor<float>(shape);

            Color32[] pixels = tex.GetPixels32();
            const float meanR = 0.485f, meanG = 0.456f, meanB = 0.406f;
            const float stdR = 0.229f, stdG = 0.224f, stdB = 0.225f;
            int hw = h * w;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int p = y * w + x;
                    float r = pixels[p].r / 255f, g = pixels[p].g / 255f, b = pixels[p].b / 255f;
                    if (_settings.useImagenetNorm)
                    {
                        r = (r - meanR) / stdR;
                        g = (g - meanG) / stdG;
                        b = (b - meanB) / stdB;
                    }
                    t[0, 0, y, x] = r;
                    t[0, 1, y, x] = g;
                    t[0, 2, y, x] = b;
                }
            return t;
        }
#endif

        public void Dispose()
        {
#if HAS_SENTIS
            DisposeWorker();
#endif
        }
    }
}
