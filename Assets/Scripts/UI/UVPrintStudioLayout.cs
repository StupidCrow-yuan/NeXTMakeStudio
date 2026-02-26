using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.UI
{
    public class UVPrintStudioLayout : MonoBehaviour
    {
        public RectTransform mainContainer;
        public RectTransform topBar;
        public RectTransform navBar;
        public ScrollRect mainScrollView;
        
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}


