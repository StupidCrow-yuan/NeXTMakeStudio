using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using PocoRender.Core;

namespace PocoRender.UI
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
                Debug.Log("UV Print card button listener added");
            }

            if (print3DCard != null)
            {
                var btn = print3DCard.GetComponent<Button>();
                if (btn == null) btn = print3DCard.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SetSelectedMode(PrintMode.Print3D));
                Debug.Log("3D Print card button listener added");
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
                Debug.Log("Close button listener added");
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmSelection);
                Debug.Log("Confirm button listener added");
            }
        }

        public void Show()
        {
            Debug.Log("Show called");
            if (dialogContainer != null)
            {
                dialogContainer.SetActive(true);
                Debug.Log("dialogContainer set to active");
            }
            if (backgroundOverlay != null)
            {
                backgroundOverlay.gameObject.SetActive(true);
                Debug.Log("backgroundOverlay gameObject set to active");
            }
            // 确保父对象overlay也被设置为可见
            if (transform.parent != null)
            {
                transform.parent.gameObject.SetActive(true);
                Debug.Log("Parent overlay set to active");
            }
        }

        public void Hide()
        {
            Debug.Log("Hide called");
            if (dialogContainer != null)
            {
                dialogContainer.SetActive(false);
                Debug.Log("dialogContainer set to inactive");
            }
            if (backgroundOverlay != null)
            {
                backgroundOverlay.gameObject.SetActive(false);
                Debug.Log("backgroundOverlay gameObject set to inactive");
            }
            // 确保父对象overlay也被设置为不可见
            if (transform.parent != null)
            {
                transform.parent.gameObject.SetActive(false);
                Debug.Log("Parent overlay set to inactive");
            }
        }

        public void ConfirmSelection()
        {
            Debug.Log($"ConfirmSelection called, selected mode: {selectedMode}");
            Hide();
            if (OnStudioSelected != null)
            {
                Debug.Log($"OnStudioSelected event has {OnStudioSelected.GetPersistentEventCount()} listeners");
                OnStudioSelected.Invoke(selectedMode);
            }
            else
            {
                Debug.LogWarning("OnStudioSelected event is null");
            }
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


