using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using NeXTMake.UI.Core; // For ProjectData

namespace NeXTMake.UI
{
    // Component to update detail view with project data
    public class ProjectDetailViewUpdater : MonoBehaviour
    {
        private Image mainImage;
        private Image thumbHomePage; // First thumbnail: home page image (BG + UV combined)
        private Image thumbUV; // UV texture thumbnail
        private Outline thumbHomePageOutline;
        private Outline thumbUVOutline;
        private ProjectData currentData;
        private ScrollRect scrollRect; // Reference to ScrollRect for scrolling to comments and resetting position
        
        public void Initialize(Image main, Image t1, Image t2, Image t3, Outline o1, Outline o2, Outline o3)
        {
            mainImage = main;
            thumbHomePage = t1; // First thumbnail is now the home page image
            thumbUV = t2; // UV texture thumbnail remains
            // thumbBG is no longer used
            thumbHomePageOutline = o1;
            thumbUVOutline = o2;
            // thumbBGOutline is no longer used
            
            // Find ScrollRect component in parent hierarchy
            scrollRect = GetComponentInChildren<ScrollRect>();
        }
        
        // Set reference to ScrollRect
        public void SetScrollRect(ScrollRect sr)
        {
            scrollRect = sr;
        }
        
        // Scroll to comments section
        public void ScrollToComments()
        {
            if (scrollRect != null)
            {
                // Wait for layout to update before scrolling
                StartCoroutine(ScrollToCommentsCoroutine());
            }
        }
        
        private IEnumerator ScrollToCommentsCoroutine()
        {
            // Wait one frame for layout to update
            yield return null;
            
            // Scroll to bottom (comments section)
            scrollRect.verticalNormalizedPosition = 0;
        }
        
        // Reset scroll position to top
        public void ResetScrollPosition()
        {
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1;
            }
        }
        
        public void UpdateWithProjectData(ProjectData data)
        {
            currentData = data;
            if (mainImage == null) return;
            
            // Reset scroll position to top when new project data is loaded
            ResetScrollPosition();
            
            // Update thumbnails
            // First thumbnail: Home Page Image (use combined texture if available, otherwise bg texture)
            if (data.combinedTexture != null) {
                thumbHomePage.sprite = Sprite.Create(data.combinedTexture, new Rect(0, 0, data.combinedTexture.width, data.combinedTexture.height), new Vector2(0.5f, 0.5f));
                thumbHomePage.color = Color.white;
            } else if (data.bgTexture != null) {
                thumbHomePage.sprite = Sprite.Create(data.bgTexture, new Rect(0, 0, data.bgTexture.width, data.bgTexture.height), new Vector2(0.5f, 0.5f));
                thumbHomePage.color = Color.white;
            } else {
                thumbHomePage.color = new Color(0.2f, 0.3f, 0.4f);
                thumbHomePage.sprite = null;
            }
            
            // Second thumbnail: UV Texture
            if (data.uvTexture != null) {
                thumbUV.sprite = Sprite.Create(data.uvTexture, new Rect(0, 0, data.uvTexture.width, data.uvTexture.height), new Vector2(0.5f, 0.5f));
                thumbUV.color = Color.white;
            } else {
                thumbUV.color = new Color(0.8f, 0.4f, 0.2f);
                thumbUV.sprite = null;
            }
            
            // Set default main image to Home Page Image (combined BG + UV)
            if (data.combinedTexture != null) {
                mainImage.sprite = Sprite.Create(data.combinedTexture, new Rect(0, 0, data.combinedTexture.width, data.combinedTexture.height), new Vector2(0.5f, 0.5f));
                mainImage.color = Color.white;
            } else if (data.bgTexture != null) {
                mainImage.sprite = Sprite.Create(data.bgTexture, new Rect(0, 0, data.bgTexture.width, data.bgTexture.height), new Vector2(0.5f, 0.5f));
                mainImage.color = Color.white;
            } else if (data.fallbackColor.HasValue) {
                mainImage.color = data.fallbackColor.Value;
                mainImage.sprite = null;
            }
            
            // Highlight home page thumbnail by default
            if (thumbHomePageOutline != null) {
                thumbHomePageOutline.effectColor = new Color(0.2f, 0.8f, 0.4f);
                thumbUVOutline.effectColor = Color.clear;
            }
        }
        
        // Show Home Page Image (combined BG + UV)
        public void ShowHomePageImage()
        {
            if (currentData != null && currentData.combinedTexture != null) {
                mainImage.sprite = Sprite.Create(currentData.combinedTexture, new Rect(0, 0, currentData.combinedTexture.width, currentData.combinedTexture.height), new Vector2(0.5f, 0.5f));
                mainImage.color = Color.white;
            } else if (currentData != null && currentData.bgTexture != null) {
                mainImage.sprite = Sprite.Create(currentData.bgTexture, new Rect(0, 0, currentData.bgTexture.width, currentData.bgTexture.height), new Vector2(0.5f, 0.5f));
                mainImage.color = Color.white;
            } else {
                mainImage.color = new Color(0.2f, 0.3f, 0.4f);
                mainImage.sprite = null;
            }
            
            // Update outline to highlight home page thumbnail
            if (thumbHomePageOutline != null) {
                thumbHomePageOutline.effectColor = new Color(0.2f, 0.8f, 0.4f);
                thumbUVOutline.effectColor = Color.clear;
            }
        }
        
        // Show UV Texture Only
        public void ShowUV()
        {
            if (currentData != null && currentData.uvTexture != null) {
                mainImage.sprite = Sprite.Create(currentData.uvTexture, new Rect(0, 0, currentData.uvTexture.width, currentData.uvTexture.height), new Vector2(0.5f, 0.5f));
                mainImage.color = Color.white;
            } else {
                mainImage.color = new Color(0.8f, 0.4f, 0.2f);
                mainImage.sprite = null;
            }
            
            // Update outline to highlight UV thumbnail
            if (thumbUVOutline != null) {
                thumbUVOutline.effectColor = new Color(0.2f, 0.8f, 0.4f);
                thumbHomePageOutline.effectColor = Color.clear;
            }
        }
        
        // This method is deprecated as per new requirements (no longer showing BG only)
        public void ShowBG()
        {
            // Fall back to showing home page image instead
            ShowHomePageImage();
        }
    }
}

