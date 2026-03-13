using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PocoRender.UI.TextureEffects
{
    /// <summary>
    /// Calls PicWish 万物抠图 API for high-quality background removal (preserves subject color).
    /// Sync mode: upload image_file + sync=1, response contains result image URL when state==1.
    /// </summary>
    public static class PicWishBackgroundRemoval
    {
        [Serializable]
        public class PicWishData
        {
            public int state;
            public string image;
            public int progress;
        }

        [Serializable]
        public class PicWishResponse
        {
            public int status;
            public string message;
            public PicWishData data;
        }

        /// <summary>
        /// Run sync API: upload texture as PNG, get back result image URL, download and return as Texture2D.
        /// On failure returns null and logs error.
        /// </summary>
        public static IEnumerator RemoveBackgroundSync(Texture2D sourceTexture, AIRemoveSettings settings, Action<Texture2D> onSuccess, Action<string> onError)
        {
            if (settings == null || !settings.HasValidApiKey)
            {
                onError?.Invoke("AI Remove: API key not set. Create AIRemoveSettings in Resources and set apiKey.");
                yield break;
            }

            int w = sourceTexture.width;
            int h = sourceTexture.height;
            Texture2D uploadTexture = sourceTexture;
            bool didResize = false;
            if (settings.maxUploadSize > 0 && (w > settings.maxUploadSize || h > settings.maxUploadSize))
            {
                float scale = Mathf.Min((float)settings.maxUploadSize / w, (float)settings.maxUploadSize / h);
                w = Mathf.Max(1, Mathf.RoundToInt(w * scale));
                h = Mathf.Max(1, Mathf.RoundToInt(h * scale));
                uploadTexture = SpriteTextureUtil.ResizeBilinear(sourceTexture, w, h);
                if (uploadTexture == null)
                {
                    onError?.Invoke("AI Remove: resize failed.");
                    yield break;
                }
                didResize = true;
            }

            byte[] pngBytes = uploadTexture.EncodeToPNG();
            if (didResize && uploadTexture != null)
                UnityEngine.Object.Destroy(uploadTexture);
            if (pngBytes == null || pngBytes.Length == 0)
            {
                onError?.Invoke("AI Remove: PNG encode failed.");
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("sync", "1");
            form.AddBinaryData("image_file", pngBytes, "image.png", "image/png");

            using (UnityWebRequest postReq = UnityWebRequest.Post(settings.apiUrl, form))
            {
                postReq.SetRequestHeader("X-API-KEY", settings.apiKey.Trim());
                postReq.timeout = 60;

#if UNITY_2020_1_OR_NEWER
                yield return postReq.SendWebRequest();
                bool postFailed = postReq.result != UnityWebRequest.Result.Success;
#else
                yield return postReq.Send();
                bool postFailed = postReq.isNetworkError || postReq.isHttpError;
#endif

                if (postFailed)
                {
                    onError?.Invoke("AI Remove: " + postReq.error);
                    yield break;
                }

                string json = postReq.downloadHandler?.text;
                if (string.IsNullOrEmpty(json))
                {
                    onError?.Invoke("AI Remove: empty response.");
                    yield break;
                }

                PicWishResponse resp = null;
                try
                {
                    resp = JsonUtility.FromJson<PicWishResponse>(json);
                }
                catch (Exception e)
                {
                    onError?.Invoke("AI Remove: parse error " + e.Message);
                    yield break;
                }

                if (resp == null || resp.status != 200)
                {
                    onError?.Invoke("AI Remove: " + (resp?.message ?? "unknown error"));
                    yield break;
                }

                if (resp.data == null || resp.data.state != 1 || string.IsNullOrEmpty(resp.data.image))
                {
                    onError?.Invoke("AI Remove: task failed (state=" + (resp.data?.state ?? -1) + ")");
                    yield break;
                }

                string imageUrl = resp.data.image;

                using (UnityWebRequest getReq = UnityWebRequestTexture.GetTexture(imageUrl))
                {
                    getReq.timeout = 30;
#if UNITY_2020_1_OR_NEWER
                    yield return getReq.SendWebRequest();
                    bool getFailed = getReq.result != UnityWebRequest.Result.Success;
#else
                    yield return getReq.Send();
                    bool getFailed = getReq.isNetworkError || getReq.isHttpError;
#endif

                    if (getFailed)
                    {
                        onError?.Invoke("AI Remove: download failed " + getReq.error);
                        yield break;
                    }

                    DownloadHandlerTexture dh = getReq.downloadHandler as DownloadHandlerTexture;
                    Texture2D resultTex = dh?.texture;
                    if (resultTex != null)
                    {
                        // Copy so we can dispose the request without destroying the texture
                        Texture2D copy = new Texture2D(resultTex.width, resultTex.height, TextureFormat.RGBA32, false, true);
                        copy.SetPixels(resultTex.GetPixels());
                        copy.Apply();
                        onSuccess?.Invoke(copy);
                    }
                    else
                    {
                        onError?.Invoke("AI Remove: failed to get texture from response.");
                    }
                }
            }
        }
    }
}
