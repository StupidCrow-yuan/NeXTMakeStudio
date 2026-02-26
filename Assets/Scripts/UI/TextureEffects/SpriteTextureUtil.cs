using UnityEngine;

namespace PocoRender.UI.TextureEffects
{
    public static class SpriteTextureUtil
    {
        /// <summary>
        /// Extracts a readable Texture2D matching the sprite rect (in pixels), optionally downscaled so max side <= maxSize.
        /// </summary>
        public static Texture2D ExtractSpriteTexture(Sprite sprite, int maxSize = 512)
        {
            if (sprite == null) return null;
            if (sprite.texture == null) return null;

            Texture2D readable = TextureReadback.ToReadableTexture(sprite.texture, 0);
            if (readable == null) return null;

            // Crop sprite rect
            Rect r = sprite.rect;
            int sx = Mathf.Clamp(Mathf.RoundToInt(r.x), 0, readable.width - 1);
            int sy = Mathf.Clamp(Mathf.RoundToInt(r.y), 0, readable.height - 1);
            int sw = Mathf.Clamp(Mathf.RoundToInt(r.width), 1, readable.width - sx);
            int sh = Mathf.Clamp(Mathf.RoundToInt(r.height), 1, readable.height - sy);

            Color[] pixels = readable.GetPixels(sx, sy, sw, sh);
            Texture2D cropped = new Texture2D(sw, sh, TextureFormat.RGBA32, false, true);
            cropped.SetPixels(pixels);
            cropped.Apply();

            if (maxSize > 0 && (cropped.width > maxSize || cropped.height > maxSize))
            {
                float scale = Mathf.Min((float)maxSize / cropped.width, (float)maxSize / cropped.height);
                int nw = Mathf.Max(1, Mathf.RoundToInt(cropped.width * scale));
                int nh = Mathf.Max(1, Mathf.RoundToInt(cropped.height * scale));
                Texture2D resized = ResizeBilinear(cropped, nw, nh);
                Object.Destroy(cropped);
                return resized;
            }

            return cropped;
        }

        public static Texture2D ResizeBilinear(Texture2D src, int newWidth, int newHeight)
        {
            if (src == null) return null;
            newWidth = Mathf.Max(1, newWidth);
            newHeight = Mathf.Max(1, newHeight);

            Texture2D dst = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false, true);
            Color[] dstPixels = new Color[newWidth * newHeight];

            float invW = 1f / (newWidth - 1f);
            float invH = 1f / (newHeight - 1f);

            for (int y = 0; y < newHeight; y++)
            {
                float v = (newHeight == 1) ? 0f : y * invH;
                for (int x = 0; x < newWidth; x++)
                {
                    float u = (newWidth == 1) ? 0f : x * invW;
                    dstPixels[y * newWidth + x] = src.GetPixelBilinear(u, v);
                }
            }

            dst.SetPixels(dstPixels);
            dst.Apply();
            return dst;
        }
    }
}



