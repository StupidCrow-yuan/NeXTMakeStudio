using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.UI
{
    public class Print3DStudioLayout : MonoBehaviour
    {
        public RectTransform mainContainer;
        public RectTransform topRow;    // Menu Bar
        public RectTransform secondRow; // Function Bar
        public RectTransform sidebar;
        public RectTransform contentArea;
        
        // Added for compatibility with PocoRenderStudioUIAutoSetup
        public RectTransform modelViewer;
        public RectTransform controlsPanel;
        public RectTransform infoPanel;

        // Views
        public GameObject exploreView;
        public GameObject sliceView;
        public GameObject devicesView;

        // Tabs
        public Button btnExplore;
        public Button btnSlice;
        public Button btnDevices;

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public void SwitchTab(string tabName)
        {
            if (exploreView != null) exploreView.SetActive(tabName == "Explore");
            if (sliceView != null) sliceView.SetActive(tabName == "Slice");
            if (devicesView != null) devicesView.SetActive(tabName == "Devices");

            // Update Button Colors (Simple highlight)
            HighlightButton(btnExplore, tabName == "Explore");
            HighlightButton(btnSlice, tabName == "Slice");
            HighlightButton(btnDevices, tabName == "Devices");
        }

        void HighlightButton(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? new Color(0.3f, 0.3f, 0.3f) : Color.clear;
        }
    }
}


