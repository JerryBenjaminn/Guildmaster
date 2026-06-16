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
    }
}
