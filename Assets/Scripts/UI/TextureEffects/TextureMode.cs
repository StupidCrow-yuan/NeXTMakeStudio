using System;

namespace PocoRender.UI.TextureEffects
{
    public enum TextureMode
    {
        Flat,
        FlatRaised,
        PatternTexture,
        ReliefTexture,
        CustomizeTexture
    }

    public static class TextureModeUtil
    {
        public static bool TryParseCraftMode(string craftMode, out TextureMode mode)
        {
            switch (craftMode)
            {
                case "Flat":
                    mode = TextureMode.Flat;
                    return true;
                case "Flat Raised":
                    mode = TextureMode.FlatRaised;
                    return true;
                case "Pattern Texture":
                    mode = TextureMode.PatternTexture;
                    return true;
                case "Relief Texture":
                    mode = TextureMode.ReliefTexture;
                    return true;
                case "Customize Texture":
                    mode = TextureMode.CustomizeTexture;
                    return true;
                default:
                    mode = TextureMode.Flat;
                    return false;
            }
        }

        public static bool IsParallaxMode(TextureMode mode)
        {
            return mode == TextureMode.FlatRaised
                   || mode == TextureMode.PatternTexture
                   || mode == TextureMode.ReliefTexture
                   || mode == TextureMode.CustomizeTexture;
        }
    }
}



