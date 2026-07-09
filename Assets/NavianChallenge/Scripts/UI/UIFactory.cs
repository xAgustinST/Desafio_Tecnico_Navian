using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace NavianChallenge.UI
{
    /// <summary>
    /// Procedural uGUI builders (no prefabs, no TextMeshPro — the project only has the
    /// built-in UI module) for the "Neuro Surgical Explorer" panel. Every stacked container
    /// uses <c>childControlHeight = true</c> so each child's <see cref="LayoutElement"/> is
    /// actually respected — with it left false (an earlier version of this file did), Unity
    /// keeps the child's raw RectTransform size (e.g. a fresh Slider defaults to 100x100),
    /// which is what produced giant vertical bars and overlapping rows.
    ///
    /// Factory methods are prefixed (MakeButton/MakeToggle/MakeSlider/SetPreferredHeight)
    /// rather than matching the UnityEngine.UI type names 1:1 — a static method named the
    /// same as its own return type (e.g. "Slider Slider(...)") makes the bare type name
    /// resolve to the method group everywhere else in this class (CS0119 on things like
    /// "Slider.Direction"), so names must differ from the UI types they build.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color ColorBg = new Color(0.045f, 0.06f, 0.09f, 0.93f);
        public static readonly Color ColorAccent = new Color(0.25f, 0.58f, 0.95f, 1f);
        public static readonly Color ColorText = new Color(0.94f, 0.96f, 0.98f, 1f);
        public static readonly Color ColorTextDim = new Color(0.64f, 0.69f, 0.76f, 1f);
        public static readonly Color ColorControlBg = new Color(0.15f, 0.18f, 0.23f, 1f);
        public static readonly Color ColorDivider = new Color(1f, 1f, 1f, 0.09f);

        const int FontSectionHeader = 15;
        const int FontSub = 12;
        const int FontBody = 14;

        static Font font;
        public static Font DefaultFont => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform AddRect(GameObject go, Transform parent)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static LayoutElement SetPreferredHeight(GameObject go, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;
            return le;
        }

        public static LayoutElement SetFlexibleHeight(GameObject go, float flex)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.flexibleHeight = flex;
            le.flexibleWidth = 1;
            return le;
        }

        static VerticalLayoutGroup Stack(GameObject go, float spacing, bool controlHeight = true)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = controlHeight;
            vlg.childForceExpandHeight = false;
            vlg.spacing = spacing;
            return vlg;
        }

        // --- Text ---

        public static Text Label(Transform parent, string text, int fontSize = FontBody, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            AddRect(go, parent);
            var t = go.AddComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color ?? ColorText;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetPreferredHeight(go, fontSize + 10);
            return t;
        }

        public static void Divider(Transform parent)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            AddRect(go, parent);
            var img = go.AddComponent<Image>();
            img.color = ColorDivider;
            SetPreferredHeight(go, 1f);
        }

        /// <summary>Small dim caption used to group a few rows within a section (e.g. "Crop Box").</summary>
        public static void SubHeader(Transform parent, string title)
        {
            var t = Label(parent, title, FontSub, FontStyle.Bold, TextAnchor.MiddleLeft, ColorTextDim);
            SetPreferredHeight(t.gameObject, FontSub + 12);
        }

        // --- Panel / Section scaffolding ---

        /// <summary>
        /// Builds the Canvas/CanvasScaler/GraphicRaycaster AND the fixed-width root panel
        /// (pinned header + scrollable body) in one atomic call — deliberately not split
        /// across a "create canvas, hand its Transform to a separate call" sequence, which
        /// in practice was ending up with the panel parented to nothing (a RectTransform
        /// with no Canvas ancestor never renders, even though its own Pos/Width/Height look
        /// perfectly normal — a nasty silent failure mode).
        /// Returns (canvas root, outer panel, header parent, scrollable content parent).
        /// </summary>
        public static (Transform canvasRoot, RectTransform panel, Transform header, Transform scrollContent) CreateCanvasAndScrollPanel(Transform parent, float width)
        {
            var canvasGo = new GameObject("ChallengeCanvas");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform canvasRoot = canvasGo.transform;

            var panelGo = new GameObject("PanelContainer", typeof(RectTransform));
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.SetParent(canvasRoot, false);
            panelRt.anchorMin = new Vector2(0, 0);
            panelRt.anchorMax = new Vector2(0, 1);
            panelRt.pivot = new Vector2(0, 1);
            panelRt.anchoredPosition = new Vector2(20, -20);
            panelRt.sizeDelta = new Vector2(width, -40);

            panelGo.AddComponent<Image>().color = ColorBg;
            Stack(panelGo, 10);
            panelGo.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(18, 18, 16, 16);

            var headerGo = new GameObject("Header", typeof(RectTransform));
            AddRect(headerGo, panelGo.transform);
            Stack(headerGo, 3);

            Divider(panelGo.transform);

            var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
            AddRect(scrollGo, panelGo.transform);
            SetFlexibleHeight(scrollGo, 1f);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 26f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRt = AddRect(viewportGo, scrollGo.transform);
            Stretch(viewportRt, Vector2.zero, Vector2.zero);
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0.001f);
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var contentRt = AddRect(contentGo, viewportGo.transform);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            Stack(contentGo, 16);
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            return (canvasRoot, panelRt, headerGo.transform, contentGo.transform);
        }

        /// <summary>
        /// A titled section (accent header + divider) inside the scrollable panel. Its own
        /// nested VerticalLayoutGroup reports its preferred height upward automatically, so
        /// no ContentSizeFitter is needed here (it would fight the parent-controlled sizing).
        /// Returns the content transform — add the section's rows into it.
        /// </summary>
        public static Transform CreateSection(Transform parent, string title)
        {
            var wrap = new GameObject("Section_" + title, typeof(RectTransform));
            AddRect(wrap, parent);
            Stack(wrap, 8);

            var header = Label(wrap.transform, title, FontSectionHeader, FontStyle.Bold, TextAnchor.MiddleLeft, ColorAccent);
            SetPreferredHeight(header.gameObject, FontSectionHeader + 8);
            Divider(wrap.transform);

            var content = new GameObject("Content", typeof(RectTransform));
            AddRect(content, wrap.transform);
            Stack(content, 8);

            return content.transform;
        }

        // --- Controls ---

        public static Button MakeButton(Transform parent, string label, UnityAction onClick)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform));
            AddRect(go, parent);
            var img = go.AddComponent<Image>();
            img.color = ColorControlBg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.22f, 0.26f, 0.32f, 1f);
            colors.pressedColor = ColorAccent;
            colors.selectedColor = colors.normalColor;
            btn.colors = colors;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            Stretch(AddRect(txtGo, go.transform), new Vector2(8, 0), new Vector2(-8, 0));
            var t = txtGo.AddComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = FontBody;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = ColorText;
            t.text = label;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 9;
            t.resizeTextMaxSize = FontBody;

            if (onClick != null) btn.onClick.AddListener(onClick);
            SetPreferredHeight(go, 34);
            return btn;
        }

        public static Toggle MakeToggle(Transform parent, string label, bool value, UnityAction<bool> onChanged)
        {
            var go = new GameObject("Toggle_" + label, typeof(RectTransform));
            AddRect(go, parent);
            var toggle = go.AddComponent<Toggle>();

            var bgGo = new GameObject("Background", typeof(RectTransform));
            var bgRt = AddRect(bgGo, go.transform);
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.pivot = new Vector2(0, 0.5f);
            bgRt.anchoredPosition = new Vector2(2, 0);
            bgRt.sizeDelta = new Vector2(22, 22);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = ColorControlBg;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            var checkRt = AddRect(checkGo, bgGo.transform);
            Stretch(checkRt, new Vector2(4, 4), new Vector2(-4, -4));
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = ColorAccent;

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = value;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = AddRect(labelGo, go.transform);
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(32, 0);
            labelRt.offsetMax = new Vector2(-4, 0);
            var t = labelGo.AddComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = FontBody;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = ColorText;
            t.text = label;

            if (onChanged != null) toggle.onValueChanged.AddListener(onChanged);
            SetPreferredHeight(go, 30);
            return toggle;
        }

        public static Slider MakeSlider(Transform parent, string label, float min, float max, float value, UnityAction<float> onChanged)
        {
            var row = new GameObject("SliderRow_" + label, typeof(RectTransform));
            AddRect(row, parent);
            Stack(row, 4);
            SetPreferredHeight(row, 48);

            Label(row.transform, label, FontSub, FontStyle.Normal, TextAnchor.MiddleLeft, ColorTextDim);

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            AddRect(sliderGo, row.transform);
            SetPreferredHeight(sliderGo, 22);
            var slider = sliderGo.AddComponent<Slider>();

            var bgGo = new GameObject("Background", typeof(RectTransform));
            var bgRt = AddRect(bgGo, sliderGo.transform);
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(1, 0.5f);
            bgRt.sizeDelta = new Vector2(0, 6);
            bgRt.anchoredPosition = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = ColorControlBg;

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = AddRect(fillAreaGo, sliderGo.transform);
            fillAreaRt.anchorMin = new Vector2(0, 0.5f);
            fillAreaRt.anchorMax = new Vector2(1, 0.5f);
            fillAreaRt.sizeDelta = new Vector2(-16, 6);
            fillAreaRt.anchoredPosition = Vector2.zero;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            var fillRt = AddRect(fillGo, fillAreaGo.transform);
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.sizeDelta = new Vector2(10, 0);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = ColorAccent;

            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRt = AddRect(handleAreaGo, sliderGo.transform);
            Stretch(handleAreaRt, new Vector2(8, 0), new Vector2(-8, 0));

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            var handleRt = AddRect(handleGo, handleAreaGo.transform);
            handleRt.sizeDelta = new Vector2(16, 22);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = ColorText;

            slider.targetGraphic = handleImg;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        /// <summary>Segmented button row (used instead of Dropdown — simpler to build procedurally).</summary>
        public static Button[] ButtonGroup(Transform parent, string[] labels, int selected, UnityAction<int> onChanged)
        {
            var row = new GameObject("ButtonGroup", typeof(RectTransform));
            AddRect(row, parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 6;
            SetPreferredHeight(row, 34);

            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                buttons[i] = MakeButton(row.transform, labels[i], () => onChanged?.Invoke(idx));
            }
            HighlightButtonGroup(buttons, selected);
            return buttons;
        }

        public static void HighlightButtonGroup(Button[] buttons, int selected)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var img = buttons[i].GetComponent<Image>();
                img.color = i == selected ? ColorAccent : ColorControlBg;
            }
        }

        /// <summary>
        /// Fixed-size square preview (e.g. for a RenderTexture) framed inside a full-width
        /// row, centered independently of the row's stretched width so it never distorts.
        /// </summary>
        public static RawImage CreateThumbnail(Transform parent, int size)
        {
            var wrap = new GameObject("ThumbnailWrap", typeof(RectTransform));
            AddRect(wrap, parent);
            SetPreferredHeight(wrap, size + 8);
            wrap.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

            var imgGo = new GameObject("Thumbnail", typeof(RectTransform));
            var imgRt = AddRect(imgGo, wrap.transform);
            imgRt.anchorMin = new Vector2(0.5f, 0.5f);
            imgRt.anchorMax = new Vector2(0.5f, 0.5f);
            imgRt.pivot = new Vector2(0.5f, 0.5f);
            imgRt.sizeDelta = new Vector2(size, size);
            imgRt.anchoredPosition = Vector2.zero;

            var img = imgGo.AddComponent<RawImage>();
            img.color = Color.white;
            return img;
        }
    }
}
