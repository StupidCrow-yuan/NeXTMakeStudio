using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using NeXTMake.Core;

namespace NeXTMake.UI
{
    public class StudioSelectionDialog : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject dialogContainer;
        public Image backgroundOverlay;
        public GameObject uvPrintCard;
        public GameObject print3DCard;
        public Image uvPrintCheckmark;
        public Image print3DCheckmark;
        public Image uvPrintBorder;
        public Image print3DBorder;
        public Button closeButton;
        public Button confirmButton;

        [Header("Events")]
        public UnityEvent<PrintMode> OnStudioSelected;

        private PrintMode selectedMode = PrintMode.UVPrint;

        void Awake()
        {
            if (OnStudioSelected == null) OnStudioSelected = new UnityEvent<PrintMode>();
        }

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            SetSelectedMode(PrintMode.UVPrint);

            if (uvPrintCard != null)
            {
                var btn = uvPrintCard.GetComponent<Button>();
                if (btn == null) btn = uvPrintCard.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SetSelectedMode(PrintMode.UVPrint));
            }

            if (print3DCard != null)
            {
                var btn = print3DCard.GetComponent<Button>();
                if (btn == null) btn = print3DCard.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SetSelectedMode(PrintMode.Print3D));
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmSelection);
            }
        }

        public void Show()
        {
            if (dialogContainer != null) dialogContainer.SetActive(true);
            if (backgroundOverlay != null) backgroundOverlay.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (dialogContainer != null) dialogContainer.SetActive(false);
            if (backgroundOverlay != null) backgroundOverlay.gameObject.SetActive(false);
        }

        public void ConfirmSelection()
        {
            Hide();
            OnStudioSelected?.Invoke(selectedMode);
        }

        public void SetSelectedMode(PrintMode mode)
        {
            selectedMode = mode;
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            bool isUV = selectedMode == PrintMode.UVPrint;
            
            if (uvPrintCheckmark != null) uvPrintCheckmark.gameObject.SetActive(isUV);
            if (uvPrintBorder != null) uvPrintBorder.color = isUV ? new Color(0.2f, 0.8f, 0.4f) : Color.clear; // Green
            
            if (print3DCheckmark != null) print3DCheckmark.gameObject.SetActive(!isUV);
            if (print3DBorder != null) print3DBorder.color = !isUV ? new Color(0.2f, 0.8f, 0.4f) : Color.clear;
        }
    }
}

