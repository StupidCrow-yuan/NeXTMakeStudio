using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PocoRender.UI
{
    public class UploadThumbnailHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject checkObj;
        public GameObject menuObj;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (checkObj != null) checkObj.SetActive(true);
            if (menuObj != null) menuObj.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (checkObj != null)
            {
                Image chk = checkObj.GetComponent<Image>();
                bool isSelected = chk != null && chk.color.g > 0.5f && chk.color.r < 0.5f;
                if (!isSelected) checkObj.SetActive(false);
            }
            if (menuObj != null) menuObj.SetActive(false);
        }
    }
}
