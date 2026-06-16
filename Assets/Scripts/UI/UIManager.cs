using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Guildmaster
{
    /// <summary>
    /// Builds and drives the uGUI shell (UI_SPEC §1): a persistent top status
    /// strip, a content area with the 5 primary tabs (Guild/Roster/Quests/Craft/
    /// Hall), a bottom tab bar, and an overlay layer for modals (§4). Tabs are
    /// placeholders in this bootstrap — structure first, content later.
    ///
    /// The shell is built in CODE (no scene/prefab YAML) for a robust headless
    /// bootstrap; this is uGUI + TMP, fully compliant with §3.7. Refactor into
    /// prefabs during the UI polish pass.
    ///
    /// Public API:
    ///   BuildShell()        - construct the canvas + shell once.
    ///   Refresh()           - repaint glanceable state (gold, guild name, summary).
    ///   ShowTab(tab)        - switch the active primary tab.
    ///   ShowOverlay(title)/HideOverlay() - open/close the modal layer.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private Canvas _canvas;
        private TextMeshProUGUI _guildNameText;
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _alertText;

        private readonly Dictionary<GameTab, RectTransform> _tabPanels = new();
        private RectTransform _overlayLayer;
        private TextMeshProUGUI _overlayTitle;
        private TextMeshProUGUI _guildSummaryText;

        private RosterView _rosterView;
        private AdventurerDetailOverlay _detailOverlay;

        private bool _built;

        // Reference resolution: portrait-primary (UI_SPEC §6).
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        private static readonly Color ColBg = new Color(0.10f, 0.11f, 0.13f, 1f);
        private static readonly Color ColStrip = new Color(0.16f, 0.17f, 0.20f, 1f);
        private static readonly Color ColTabBar = new Color(0.14f, 0.15f, 0.18f, 1f);
        private static readonly Color ColTab = new Color(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color ColTabActive = new Color(0.32f, 0.45f, 0.60f, 1f);
        private static readonly Color ColOverlay = new Color(0f, 0f, 0f, 0.75f);

        public void BuildShell()
        {
            if (_built) return;
            _built = true;

            _rosterView = new RosterView();
            _detailOverlay = new AdventurerDetailOverlay();

            EnsureEventSystem();
            BuildCanvas();
            BuildTopStrip();
            BuildContentArea();
            BuildTabBar();
            BuildOverlayLayer();
            _detailOverlay.Build(_canvas.transform);

            ShowTab(GameTab.Guild); // app opens on Guild home (UI_SPEC §3 / §5)
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            // Project uses the new Input System (activeInputHandler = 1), so the
            // EventSystem needs InputSystemUIInputModule, not StandaloneInputModule.
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            // Created at runtime, the module has no actions asset — assign the
            // built-in defaults so pointer/click reaches the UI.
            module.AssignDefaultActions();
            DontDestroyOnLoad(es);
        }

        private void BuildCanvas()
        {
            var go = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var bg = UIFactory.CreateStretch(_canvas.transform, "Background");
            UIFactory.AddImage(bg, ColBg);
        }

        // ---- Top status strip (display-only, UI_SPEC §2) ----
        private void BuildTopStrip()
        {
            var strip = UIFactory.CreatePanel(_canvas.transform, "TopStatusStrip");
            strip.anchorMin = new Vector2(0f, 1f);
            strip.anchorMax = new Vector2(1f, 1f);
            strip.pivot = new Vector2(0.5f, 1f);
            strip.sizeDelta = new Vector2(0f, 150f);
            strip.anchoredPosition = Vector2.zero;
            UIFactory.AddImage(strip, ColStrip);

            _guildNameText = UIFactory.CreateText(strip, "GuildName", "Guild", 36f, TextAlignmentOptions.Left);
            var gn = (RectTransform)_guildNameText.transform;
            gn.anchorMin = new Vector2(0f, 0f); gn.anchorMax = new Vector2(0.5f, 1f);
            gn.offsetMin = new Vector2(24f, 0f); gn.offsetMax = new Vector2(0f, 0f);

            _goldText = UIFactory.CreateText(strip, "Gold", "Gold: 0", 36f, TextAlignmentOptions.Right);
            var gt = (RectTransform)_goldText.transform;
            gt.anchorMin = new Vector2(0.5f, 0f); gt.anchorMax = new Vector2(0.92f, 1f);
            gt.offsetMin = Vector2.zero; gt.offsetMax = Vector2.zero;

            _alertText = UIFactory.CreateText(strip, "Alert", "", 36f, TextAlignmentOptions.Right);
            var at = (RectTransform)_alertText.transform;
            at.anchorMin = new Vector2(0.92f, 0f); at.anchorMax = new Vector2(1f, 1f);
            at.offsetMin = Vector2.zero; at.offsetMax = new Vector2(-16f, 0f);
        }

        // ---- Content area: one panel per tab (UI_SPEC §3) ----
        private void BuildContentArea()
        {
            var content = UIFactory.CreatePanel(_canvas.transform, "ContentArea");
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            content.offsetMin = new Vector2(0f, UIFactory.MinTouch + 20f); // above tab bar
            content.offsetMax = new Vector2(0f, -150f);                    // below top strip

            foreach (GameTab tab in System.Enum.GetValues(typeof(GameTab)))
            {
                var panel = UIFactory.CreateStretch(content, $"Panel_{tab}");

                if (tab == GameTab.Roster)
                {
                    _rosterView.Build(panel); // real roster UI (Task02 §4)
                }
                else
                {
                    var title = UIFactory.CreateText(panel, "Title", tab.ToString(), 64f);
                    UIFactory.Stretch((RectTransform)title.transform);

                    if (tab == GameTab.Guild)
                    {
                        title.alignment = TextAlignmentOptions.Top;
                        _guildSummaryText = UIFactory.CreateText(panel, "Summary",
                            "Guild home — completed expeditions appear here.", 32f, TextAlignmentOptions.Center);
                        UIFactory.Stretch((RectTransform)_guildSummaryText.transform);
                    }
                }

                _tabPanels[tab] = panel;
            }
        }

        // ---- Bottom tab bar (persistent, thumb zone, UI_SPEC §1/§6) ----
        private void BuildTabBar()
        {
            var bar = UIFactory.CreatePanel(_canvas.transform, "TabBar");
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, UIFactory.MinTouch);
            bar.anchoredPosition = Vector2.zero;
            UIFactory.AddImage(bar, ColTabBar);

            var tabs = (GameTab[])System.Enum.GetValues(typeof(GameTab));
            float width = 1f / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                GameTab tab = tabs[i];
                var btn = UIFactory.CreateButton(bar, $"Tab_{tab}", tab.ToString(), ColTab, () => ShowTab(tab));
                var rt = (RectTransform)btn.transform;
                rt.anchorMin = new Vector2(i * width, 0f);
                rt.anchorMax = new Vector2((i + 1) * width, 1f);
                rt.offsetMin = new Vector2(4f, 4f);
                rt.offsetMax = new Vector2(-4f, -4f);
            }
        }

        // ---- Overlay layer for modals (UI_SPEC §4) ----
        private void BuildOverlayLayer()
        {
            _overlayLayer = UIFactory.CreateStretch(_canvas.transform, "OverlayLayer");
            var dim = UIFactory.AddImage(_overlayLayer, ColOverlay);

            // Tap-outside dismiss (UI_SPEC §6): clicking the dim backdrop closes it.
            var dismiss = _overlayLayer.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = dim;
            dismiss.onClick.AddListener(HideOverlay);

            var card = UIFactory.CreatePanel(_overlayLayer, "OverlayCard");
            card.anchorMin = new Vector2(0.1f, 0.3f);
            card.anchorMax = new Vector2(0.9f, 0.7f);
            card.offsetMin = Vector2.zero; card.offsetMax = Vector2.zero;
            UIFactory.AddImage(card, ColStrip);

            _overlayTitle = UIFactory.CreateText(card, "OverlayTitle", "Overlay", 48f);
            UIFactory.Stretch((RectTransform)_overlayTitle.transform);

            _overlayLayer.gameObject.SetActive(false);
        }

        public void ShowTab(GameTab tab)
        {
            foreach (var kv in _tabPanels)
            {
                kv.Value.gameObject.SetActive(kv.Key == tab);
            }
            // Highlight the active tab button.
            var bar = _canvas.transform.Find("TabBar");
            if (bar != null)
            {
                foreach (GameTab t in System.Enum.GetValues(typeof(GameTab)))
                {
                    var child = bar.Find($"Tab_{t}");
                    if (child != null)
                    {
                        var img = child.GetComponent<Image>();
                        if (img != null) img.color = (t == tab) ? ColTabActive : ColTab;
                    }
                }
            }
        }

        /// <summary>Open the Adventurer Detail overlay for a roster member (UI_SPEC §4.1).</summary>
        public void ShowAdventurerDetail(string adventurerId)
        {
            _detailOverlay?.Show(adventurerId);
        }

        public void ShowOverlay(string title)
        {
            if (_overlayTitle != null) _overlayTitle.text = title;
            if (_overlayLayer != null) _overlayLayer.gameObject.SetActive(true);
        }

        public void HideOverlay()
        {
            if (_overlayLayer != null) _overlayLayer.gameObject.SetActive(false);
        }

        /// <summary>Repaint glanceable state. Cheap; call after state changes, not per-frame.</summary>
        public void Refresh()
        {
            var guild = GuildManager.Instance;
            if (guild != null && guild.Data != null)
            {
                if (_guildNameText != null) _guildNameText.text = guild.GuildName;
                if (_goldText != null) _goldText.text = $"Gold: {guild.TotalGold}";
            }

            int completed = CountCompletedExpeditions();
            if (_alertText != null) _alertText.text = completed > 0 ? $"●{completed}" : "";

            if (_guildSummaryText != null)
            {
                int healthy = AdventurerManager.Instance != null ? AdventurerManager.Instance.CountHealthy() : 0;
                _guildSummaryText.text =
                    $"Completed expeditions: {completed}\nHealthy adventurers: {healthy}";
            }

            _rosterView?.Refresh();
        }

        private int CountCompletedExpeditions()
        {
            var em = ExpeditionManager.Instance;
            if (em == null) return 0;
            int n = 0;
            foreach (var exp in em.Active)
            {
                if (!exp.collected && em.CanComplete(exp)) n++;
            }
            return n;
        }
    }
}
