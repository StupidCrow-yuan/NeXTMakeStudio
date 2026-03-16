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

            // Use Emission for the base color so it is 100% immune to scene ambient lighting
            // Set Albedo to black so diffuse lighting doesn't add any unwanted color shifts
            m.color = Color.black;
            m.mainTexture = mainTex;
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", mainTex);
            m.SetColor("_EmissionColor", Color.white);

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

            // Keep enough glossiness so the moving spotlight creates a visible specular highlight
            m.SetFloat("_Glossiness", 0.3f);
            m.SetFloat("_Metallic", 0.0f);
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



