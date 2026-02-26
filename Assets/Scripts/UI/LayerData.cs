using UnityEngine;

namespace PocoRender.UI
{
    public class LayerData : MonoBehaviour
    {
        public string craftMode = "Flat";
        public string inkMode = "White > CMYK";

        // Customize Texture: user-provided depth map (must match the current layer sprite size)
        public Texture2D customDepthMap;
        public int customDepthWidth;
        public int customDepthHeight;
    }
}


