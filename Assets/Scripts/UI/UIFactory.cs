using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Guildmaster
{
    /// <summary>
    /// Small helpers for building the uGUI + TextMeshPro shell in code. CLAUDE.md
    /// §3.7 locks UI to uGUI Canvas + TMP (never UI Toolkit). The bootstrap shell
    /// is constructed programmatically; Jerry can refactor panels into prefabs in
    /// the UI polish pass. Touch sizing honours UI_SPEC §6 (≥44px targets).
    /// </summary>
    public static class UIFactory
    {
        // Mobile minimum touch target (UI_SPEC §6). Real px on the reference canvas.
        public const float MinTouch = 132f; // ~44dp at the 1080-wide reference

        public static RectTransform CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform CreateStretch(Transform parent, string name)
        {
            var rt = CreatePanel(parent, name);
            Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image AddImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            return tmp;
        }

        /// <summary>A tappable button with a TMP label. Returns the Button.</summary>
        public static Button CreateButton(Transform parent, string name, string label, Color bg,
            UnityEngine.Events.UnityAction onClick)
        {
            var rt = CreatePanel(parent, name);
            var img = AddImage(rt, bg);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            if (onClick != null) button.onClick.AddListener(onClick);

            var labelRt = CreateText(rt, "Label", label, 32f);
            Stretch((RectTransform)labelRt.transform);
            return button;
        }

        public static TextMeshProUGUI LabelOf(Button button)
        {
            return button.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>Color-coding for roster status (UI_SPEC §3 Tab 2).</summary>
        public static Color StatusColor(AdventurerStatus status)
        {
            switch (status)
            {
                case AdventurerStatus.Healthy: return new Color(0.45f, 0.85f, 0.45f);
                case AdventurerStatus.Injured: return new Color(0.95f, 0.6f, 0.25f);
                case AdventurerStatus.OnExpedition: return new Color(0.4f, 0.65f, 0.95f);
                case AdventurerStatus.Training: return new Color(0.9f, 0.85f, 0.35f);
                case AdventurerStatus.Retired: return new Color(0.6f, 0.6f, 0.6f);
                case AdventurerStatus.DeadPending:
                case AdventurerStatus.Dead: return new Color(0.9f, 0.35f, 0.35f);
                default: return Color.white;
            }
        }

        public static void SetPreferredHeight(RectTransform rt, float height)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }

        /// <summary>
        /// A vertical ScrollRect. Returns the ScrollRect root; outputs the Content
        /// RectTransform (with a VerticalLayoutGroup + ContentSizeFitter) that rows
        /// should be parented to.
        /// </summary>
        public static RectTransform CreateVerticalScroll(Transform parent, string name, out RectTransform content)
        {
            var root = CreatePanel(parent, name);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;

            var viewport = CreatePanel(root, "Viewport");
            Stretch(viewport);
            AddImage(viewport, new Color(0f, 0f, 0f, 0.001f)); // needs a graphic to receive drags
            viewport.gameObject.AddComponent<RectMask2D>();
            sr.viewport = viewport;

            content = CreatePanel(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;

            return root;
        }

        public static TMP_InputField CreateInputField(Transform parent, string name, string initialText)
        {
            var rt = CreatePanel(parent, name);
            AddImage(rt, new Color(1f, 1f, 1f, 0.10f));
            var field = rt.gameObject.AddComponent<TMP_InputField>();

            var textArea = CreatePanel(rt, "TextArea");
            Stretch(textArea);
            textArea.offsetMin = new Vector2(10f, 6f);
            textArea.offsetMax = new Vector2(-10f, -6f);
            textArea.gameObject.AddComponent<RectMask2D>();

            var placeholder = CreateText(textArea, "Placeholder", "Enter name...", 28f, TextAlignmentOptions.Left);
            Stretch((RectTransform)placeholder.transform);
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);

            var textComp = CreateText(textArea, "Text", "", 28f, TextAlignmentOptions.Left);
            Stretch((RectTransform)textComp.transform);

            field.textViewport = textArea;
            field.textComponent = textComp;
            field.placeholder = placeholder;
            field.text = initialText;
            return field;
        }
    }
}
