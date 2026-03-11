using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PocoRender.Communication;
using PocoRender.UI.Modules;

namespace PocoRender.UI.Modules
{
    public class PublishDialog : MonoBehaviour
    {
        public RawImage previewImage;
        public InputField nameInput;
        public Dropdown categoryDropdown;
        public Dropdown themeDropdown;
        public Dropdown styleDropdown;
        public Dropdown licenseDropdown;
        public InputField tagsInput;
        public Button cancelButton;
        public Button publishButton;

        private QtBridgeController _bridge;
        private RectTransform _paper;
        private System.Action _onClose;

        public void Setup(QtBridgeController bridge, RectTransform paper, System.Action onClose)
        {
            _bridge = bridge;
            _paper = paper;
            _onClose = onClose;

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Close);

            publishButton.onClick.RemoveAllListeners();
            publishButton.onClick.AddListener(OnPublish);

            PopulateDropdown(categoryDropdown, new[] { "Select a category.", "Gifts", "Blended Crafts", "Home & Living", "Art Decor", "Digital Accessories", "Pet Supplies", "Toys & Games" });
            PopulateDropdown(themeDropdown, new[] { "Select a theme.", "Modern", "Vintage", "Minimalist", "Abstract", "Nature", "Holiday" });
            PopulateDropdown(styleDropdown, new[] { "Select Style", "Flat", "Relief", "Textured" });
            PopulateDropdown(licenseDropdown, new[] { "Select a license", "Standard License", "Extended License", "Creative Commons" });

            CapturePreview();
        }

        private void PopulateDropdown(Dropdown dropdown, string[] options)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
        }

        private void CapturePreview()
        {
            if (_paper == null) return;
            StartCoroutine(CaptureRoutine());
        }

        private System.Collections.IEnumerator CaptureRoutine()
        {
            yield return new WaitForEndOfFrame();
            
            Texture2D tex = HomeModule.CapturePaperFlatStatic(_paper);
            if (tex != null && previewImage != null)
            {
                previewImage.texture = tex;
                previewImage.color = Color.white;
            }
        }

        private void OnPublish()
        {
            if (_bridge == null || !_bridge.IsConnected)
            {
                Debug.LogWarning("[PublishDialog] Bridge not connected");
                return;
            }

            string json = ProjectSerializer.Serialize(_paper, nameInput.text, _paper.rect.width, _paper.rect.height);

            Texture2D thumb = HomeModule.CapturePaperFlatStatic(_paper);
            string thumbBase64 = "";
            if (thumb != null)
            {
                byte[] png = thumb.EncodeToPNG();
                thumbBase64 = System.Convert.ToBase64String(png);
                Destroy(thumb);
            }

            var metadata = new QtBridgeController.PublishMetadata
            {
                name = nameInput.text,
                category = categoryDropdown.options[categoryDropdown.value].text,
                theme = themeDropdown.options[themeDropdown.value].text,
                style = styleDropdown.options[styleDropdown.value].text,
                license = licenseDropdown.options[licenseDropdown.value].text,
                tags = tagsInput.text
            };

            _bridge.SendPublishWork((int)_paper.rect.width, (int)_paper.rect.height, thumbBase64, json, metadata);

            Close();
        }

        private void Close()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}
