using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.UI.TextureEffects
{
    public static class PreviewMeshBuilder
    {
        /// <summary>
        /// Build a quad for a UI Image layer and apply a parallax material.
        /// The parent should already be set up as: rotation X=90, scale=0.01, position above paper.
        /// </summary>
        public static GameObject BuildImageLayerQuad(Image img, RectTransform srcRt, Transform parent, float zOffsetUiUnits, TextureMode mode, Texture2D heightOverride, int maxTexSize = 512)
        {
            if (img == null || img.sprite == null || srcRt == null || parent == null) return null;

            // Create a readable downscaled copy of the sprite texture and crop to sprite rect
            Texture2D readableTex = TextureReadback.ToReadableTexture(img.sprite.texture, maxTexSize);
            if (readableTex == null) return null;

            Texture2D spriteTex = CropSprite(readableTex, img.sprite, img.sprite.texture.width, img.sprite.texture.height);
            if (spriteTex == null) spriteTex = readableTex;

            Texture2D height = heightOverride != null ? heightOverride : HeightMapGenerator.GenerateHeightMap(spriteTex, mode);
            // Ensure height map matches main texture size; if not, ignore override to avoid wrong mapping
            if (height != null && (height.width != spriteTex.width || height.height != spriteTex.height))
            {
                height = HeightMapGenerator.GenerateHeightMap(spriteTex, mode);
            }
            Material mat = ParallaxMaterialUtil.CreateParallaxMaterial(spriteTex, height, mode);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"TexQuad_{img.gameObject.name}";
            Object.DestroyImmediate(quad.GetComponent<Collider>());

            quad.transform.SetParent(parent, false);

            // UI space → parent space (parent scaled 0.01)
            Vector2 pos = srcRt.anchoredPosition;
            float w = Mathf.Abs(srcRt.rect.width) > 0 ? srcRt.rect.width : srcRt.sizeDelta.x;
            float h = Mathf.Abs(srcRt.rect.height) > 0 ? srcRt.rect.height : srcRt.sizeDelta.y;

            quad.transform.localPosition = new Vector3(pos.x, pos.y, -zOffsetUiUnits);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(w, h, 1f);

            MeshRenderer mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // Render order stability
            if (mr.sharedMaterial != null)
            {
                mr.sharedMaterial.renderQueue = 3000 + Mathf.RoundToInt(zOffsetUiUnits);
            }

            return quad;
        }

        private static Texture2D CropSprite(Texture2D readableFull, Sprite sprite, int originalW, int originalH)
        {
            if (readableFull == null || sprite == null) return null;

            Rect r = sprite.rect; // in original texture pixels
            if (r.width <= 1 || r.height <= 1) return null;

            // Map original sprite rect into readableFull space
            float sx = r.x / originalW;
            float sy = r.y / originalH;
            float sw = r.width / originalW;
            float sh = r.height / originalH;

            int x0 = Mathf.Clamp(Mathf.RoundToInt(sx * readableFull.width), 0, readableFull.width - 1);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(sy * readableFull.height), 0, readableFull.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(sw * readableFull.width), 2, readableFull.width - x0);
            int h = Mathf.Clamp(Mathf.RoundToInt(sh * readableFull.height), 2, readableFull.height - y0);

            Color32[] src = readableFull.GetPixels32();
            Color32[] dst = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                int srcRow = (y0 + y) * readableFull.width;
                int dstRow = y * w;
                for (int x = 0; x < w; x++)
                {
                    dst[dstRow + x] = src[srcRow + (x0 + x)];
                }
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(dst);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
    }
}



