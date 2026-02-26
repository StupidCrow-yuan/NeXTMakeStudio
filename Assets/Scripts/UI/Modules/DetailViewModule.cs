using UnityEngine;
using UnityEngine.UI;
using PocoRender.UI.Core; 
using System.Collections.Generic;
using PocoRender.UI; // For ProjectDetailViewUpdater

namespace PocoRender.UI.Modules
{
    public class DetailViewModule
    {
        public static GameObject CreateProjectDetailView(GameObject parent, System.Action onBack, System.Action<Color?> addCanvasCallback, Dictionary<int, ProjectData> projectData)
        {
            // parent should be the DetailView GameObject itself or we create it?
            // Original code: CreateProjectDetailView(detailView, ...) where detailView is already created.
            // Let's assume parent is the container, and we add components to it.
            
            parent.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
            GameObject scroll = UIFactory.CreateObject("Scroll", parent);
            UIFactory.Stretch(scroll.GetComponent<RectTransform>());
            ScrollRect sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false; 
            sr.vertical = true; 
            sr.movementType = ScrollRect.MovementType.Clamped; 
            sr.elasticity = 0.1f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 10; 
            
            GameObject vp = UIFactory.CreateObject("VP", scroll);
            RectTransform vpRect = vp.GetComponent<RectTransform>();
            vpRect.anchorMin = new Vector2(0.2f, 0);
            vpRect.anchorMax = new Vector2(0.8f, 1);
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            vp.AddComponent<Image>().color = Color.clear;
            vp.AddComponent<RectMask2D>();
            
            GameObject content = UIFactory.CreateObject("Content", vp);
            RectTransform cr = content.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 1);
            cr.anchorMax = new Vector2(1, 1);
            cr.pivot = new Vector2(0.5f, 1);
            cr.offsetMin = Vector2.zero;
            cr.offsetMax = Vector2.zero;
            
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 40, 40); 
            vlg.spacing = 30; 
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            sr.viewport = vpRect;
            sr.content = cr;
            sr.vertical = true;

            // Back button
            UIFactory.CreateButton("< Back", parent, new Vector2(-750, 300), new Vector2(80, 30), Color.white, UIFactory.COLOR_TEXT_DARK).GetComponent<Button>().onClick.AddListener(()=> {
                onBack();
            });

            ProjectDetailViewUpdater updater = parent.AddComponent<ProjectDetailViewUpdater>();
            updater.SetScrollRect(sr);

            // Top Section
            GameObject top = UIFactory.CreateObject("Top", content); top.AddComponent<LayoutElement>().minHeight = 400;
            top.AddComponent<Image>().color = Color.white;
            HorizontalLayoutGroup thlg = top.AddComponent<HorizontalLayoutGroup>(); 
            thlg.padding = new RectOffset(20, 20, 20, 20); 
            thlg.spacing = 40;
            
            // Thumbnails
            GameObject thumbs = UIFactory.CreateObject("Thumbnails", top);
            LayoutElement thumbsLayout = thumbs.AddComponent<LayoutElement>();
            thumbsLayout.minWidth = 100;
            thumbsLayout.preferredWidth = 100;
            thumbsLayout.flexibleWidth = 0;
            VerticalLayoutGroup tvlg = thumbs.AddComponent<VerticalLayoutGroup>(); 
            tvlg.spacing = 10; 
            tvlg.childControlHeight = true;
            tvlg.childForceExpandHeight = false;
            tvlg.padding = new RectOffset(0, 0, 0, 0);
            tvlg.childControlWidth = true;
            tvlg.childForceExpandWidth = false;
            
            // Main Image
            GameObject img = UIFactory.CreateObject("BigImg", top); 
            LayoutElement imgLayout = img.AddComponent<LayoutElement>();
            imgLayout.preferredWidth = 500;
            imgLayout.preferredHeight = 500;
            imgLayout.flexibleWidth = 0;
            imgLayout.flexibleHeight = 0;
            imgLayout.minWidth = 500;
            imgLayout.minHeight = 500;
            Image mainImg = img.AddComponent<Image>();
            mainImg.color = Color.black; 
            mainImg.preserveAspect = true; 
            
            // Thumbnail 1
            GameObject t1 = UIFactory.CreateObject("Thumb_HomePage", thumbs);
            LayoutElement t1Layout = t1.AddComponent<LayoutElement>();
            t1Layout.minWidth = 100;
            t1Layout.minHeight = 100;
            t1Layout.preferredWidth = 100;
            t1Layout.preferredHeight = 100;
            Image t1Img = t1.AddComponent<Image>();
            t1Img.color = new Color(0.2f, 0.3f, 0.4f); 
            t1Img.preserveAspect = true; 
            Outline t1Outline = t1.AddComponent<Outline>();
            t1Outline.effectColor = UIFactory.COLOR_ACCENT_GREEN; 
            Button t1Btn = t1.AddComponent<Button>();
            
            // Thumbnail 2
            GameObject t2 = UIFactory.CreateObject("Thumb_UV", thumbs);
            LayoutElement t2Layout = t2.AddComponent<LayoutElement>();
            t2Layout.minWidth = 100;
            t2Layout.minHeight = 100;
            t2Layout.preferredWidth = 100;
            t2Layout.preferredHeight = 100;
            Image t2Img = t2.AddComponent<Image>();
            t2Img.color = new Color(0.8f, 0.4f, 0.2f); 
            t2Img.preserveAspect = true; 
            Outline t2Outline = t2.AddComponent<Outline>();
            t2Outline.effectColor = Color.clear;
            Button t2Btn = t2.AddComponent<Button>();
            
            updater.Initialize(mainImg, t1Img, t2Img, null, t1Outline, t2Outline, null);
            
            System.Action<Outline> highlightThumb = (selected) => {
                t1Outline.effectColor = Color.clear;
                t2Outline.effectColor = Color.clear;
                if (selected != null) selected.effectColor = UIFactory.COLOR_ACCENT_GREEN;
            };
            
            t1Btn.onClick.AddListener(() => {
                highlightThumb(t1Outline);
                updater.ShowHomePageImage();
            });
            
            t2Btn.onClick.AddListener(() => {
                highlightThumb(t2Outline);
                updater.ShowUV();
            });
            
            GameObject info = UIFactory.CreateObject("Info", top); info.AddComponent<LayoutElement>().flexibleWidth = 1;
            VerticalLayoutGroup ilg = info.AddComponent<VerticalLayoutGroup>(); ilg.spacing = 10;
            UIFactory.CreateText("Stained Glass Halloween Village", info, 28, UIFactory.COLOR_TEXT_DARK, Vector2.zero, new Vector2(0, 40), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.CreateText("By user123", info, 14, Color.gray, Vector2.zero, new Vector2(0, 20), TextAnchor.MiddleLeft);
            
            GameObject buttonContainer = UIFactory.CreateObject("ButtonContainer", info);
            HorizontalLayoutGroup btnLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 20;
            btnLayout.childControlHeight = true;
            btnLayout.childControlWidth = true;
            btnLayout.childForceExpandWidth = false;
            btnLayout.childForceExpandHeight = false;
            
            GameObject custBtn = UIFactory.CreateButton("Customize This Design", buttonContainer, Vector2.zero, new Vector2(0, 40), UIFactory.COLOR_ACCENT_GREEN, Color.white);
            custBtn.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            custBtn.AddComponent<LayoutElement>().minHeight = 40;
            custBtn.AddComponent<LayoutElement>().minWidth = 180;
            custBtn.AddComponent<LayoutElement>().flexibleWidth = 0;
            custBtn.GetComponent<Button>().onClick.AddListener(() => {
                onBack(); 
                addCanvasCallback(new Color(0.8f, 0.4f, 0.2f)); 
            });
            
            GameObject likeBtn = UIFactory.CreateButton("🟢Like", buttonContainer, Vector2.zero, new Vector2(0, 40), Color.white, UIFactory.COLOR_TEXT_DARK);
            likeBtn.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            likeBtn.AddComponent<LayoutElement>().minHeight = 40;
            likeBtn.AddComponent<LayoutElement>().minWidth = 60;
            likeBtn.AddComponent<LayoutElement>().flexibleWidth = 0;
            
            GameObject commentBtn = UIFactory.CreateButton("💬评论", buttonContainer, Vector2.zero, new Vector2(40, 40), Color.white, UIFactory.COLOR_TEXT_DARK);
            commentBtn.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            commentBtn.AddComponent<LayoutElement>().minHeight = 40;
            commentBtn.AddComponent<LayoutElement>().minWidth = 40;
            commentBtn.AddComponent<LayoutElement>().flexibleWidth = 0;
            commentBtn.GetComponent<Button>().onClick.AddListener(() => {
                updater.ScrollToComments(); 
            });
            
            GameObject favoriteBtn = UIFactory.CreateButton("★收藏", buttonContainer, Vector2.zero, new Vector2(40, 40), Color.white, UIFactory.COLOR_TEXT_DARK);
            favoriteBtn.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            favoriteBtn.AddComponent<LayoutElement>().minHeight = 40;
            favoriteBtn.AddComponent<LayoutElement>().minWidth = 60;
            favoriteBtn.AddComponent<LayoutElement>().flexibleWidth = 0;
            
            GameObject shareBtn = UIFactory.CreateButton("📤分享", buttonContainer, Vector2.zero, new Vector2(40, 40), Color.white, UIFactory.COLOR_TEXT_DARK);
            shareBtn.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            shareBtn.AddComponent<LayoutElement>().minHeight = 40;
            shareBtn.AddComponent<LayoutElement>().minWidth = 40;
            shareBtn.AddComponent<LayoutElement>().flexibleWidth = 0;
            
            // Details
            GameObject details = UIFactory.CreateObject("Details", content); details.AddComponent<LayoutElement>().minHeight = 200;
            details.AddComponent<Image>().color = Color.white;
            VerticalLayoutGroup dlg = details.AddComponent<VerticalLayoutGroup>(); dlg.padding = new RectOffset(20, 20, 20, 20);
            UIFactory.CreateText("Device: PocoRender Printer E1", details, 16, UIFactory.COLOR_TEXT_DARK, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft);
            UIFactory.CreateText("Print Mode: Standard Flatbed", details, 16, UIFactory.COLOR_TEXT_DARK, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft);
            UIFactory.CreateText("Material: Acrylic", details, 16, UIFactory.COLOR_TEXT_DARK, Vector2.zero, new Vector2(0, 30), TextAnchor.MiddleLeft);

            // Comments
            GameObject comments = UIFactory.CreateObject("Comments", content); comments.AddComponent<LayoutElement>().minHeight = 150;
            comments.AddComponent<Image>().color = Color.white;
            VerticalLayoutGroup clg = comments.AddComponent<VerticalLayoutGroup>(); 
            clg.padding = new RectOffset(20, 20, 20, 20); 
            clg.spacing = 10;
            UIFactory.CreateText("Comments", comments, 20, UIFactory.COLOR_TEXT_DARK, Vector2.zero, new Vector2(0, 30), TextAnchor.UpperLeft, FontStyle.Bold);
            
            GameObject commentInput = UIFactory.CreateObject("CommentInput", comments);
            commentInput.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            InputField inputField = commentInput.AddComponent<InputField>();
            
            GameObject placeholder = new GameObject("Placeholder", typeof(Text));
            placeholder.transform.SetParent(commentInput.transform, false);
            Text placeholderText = placeholder.GetComponent<Text>();
            placeholderText.text = "Add your comment...";
            placeholderText.color = Color.gray;
            placeholderText.fontSize = 14;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            
            GameObject textObj = new GameObject("Text", typeof(Text));
            textObj.transform.SetParent(commentInput.transform, false);
            Text text = textObj.GetComponent<Text>();
            text.color = UIFactory.COLOR_TEXT_DARK;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleLeft;
            
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.text = "";
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.caretBlinkRate = 0.8f;
            inputField.caretColor = UIFactory.COLOR_TEXT_DARK;
            
            LayoutElement inputLayout = commentInput.AddComponent<LayoutElement>();
            inputLayout.minHeight = 40;
            inputLayout.flexibleWidth = 1;

            return parent;
        }
    }
}



