using UnityEngine;
using PocoRender.Utils;

namespace PocoRender.UI.TextureEffects
{
    public static class ParallaxMaterialUtil
    {
        public static Material CreateParallaxMaterial(Texture2D mainTex, Texture2D heightMap, TextureMode mode)
        {
            Shader shader = SafeShaderHelper.GetStandardShader();
            if (shader == null) return null;

            Material m = new Material(shader);
            SetMaterialTransparent(m);

            m.mainTexture = mainTex;

            if (heightMap != null)
            {
                m.SetTexture("_ParallaxMap", heightMap);
                m.EnableKeyword("_PARALLAXMAP");

                float parallax = mode switch
                {
                    TextureMode.FlatRaised => 0.03f,
                    TextureMode.PatternTexture => 0.025f,
                    TextureMode.ReliefTexture => 0.05f,
                    _ => 0.03f
                };
                m.SetFloat("_Parallax", parallax);
            }

            // Reduce glossy look for clearer relief
            m.SetFloat("_Glossiness", 0.0f);
            return m;
        }

        // Standard shader transparency setup (Unity)
        private static void SetMaterialTransparent(Material material)
        {
            // 0 = Opaque, 3 = Transparent
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            material.SetOverrideTag("RenderType", "Transparent");
        }
    }
}



