using UnityEngine;

namespace PocoRender.UI.TextureEffects
{
    public static class TextureReadback
    {
        /// <summary>
        /// Create a readable Texture2D from any source Texture (even if not marked readable).
        /// Downscales to fit maxSize to keep things fast for previews.
        /// </summary>
        public static Texture2D ToReadableTexture(Texture source, int maxSize)
        {
            if (source == null) return null;

            int srcW = source.width;
            int srcH = source.height;
            if (srcW <= 0 || srcH <= 0) return null;

            float scale = 1f;
            int maxDim = Mathf.Max(srcW, srcH);
            if (maxSize > 0 && maxDim > maxSize)
            {
                scale = (float)maxSize / maxDim;
            }

            int w = Mathf.Max(2, Mathf.RoundToInt(srcW * scale));
            int h = Mathf.Max(2, Mathf.RoundToInt(srcH * scale));

            // The source image shown on the canvas is authored as a normal sRGB image.
            // Read it back through an sRGB RT so the mini preview does not pick up a warm/washed tint.
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(source, rt);

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply(false, false);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }
    }
}



