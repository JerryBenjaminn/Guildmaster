using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Guildmaster
{
    /// <summary>
    /// Builds and refreshes the Roster tab (UI_SPEC §3 Tab 2): a recruit header and
    /// a scrollable, color-coded list of adventurers. Tapping a row opens the
    /// Adventurer Detail overlay. Plain helper (not a MonoBehaviour); owned by
    /// UIManager. Rebuilds the list on change — cheap, not per-frame.
    /// </summary>
    public class RosterView
    {
        private TextMeshProUGUI _countLabel;
        private Button _recruitButton;
        private RectTransform _listContent;

        private static readonly Color ColHeader = new Color(0.18f, 0.19f, 0.22f, 1f);
        private static readonly Color ColRecruit = new Color(0.30f, 0.50f, 0.34f, 1f);
        private static readonly Color ColRow = new Color(0.20f, 0.21f, 0.25f, 1f);

        public void Build(RectTransform parent)
        {
            // Header bar (recruit button + count).
            var header = UIFactory.CreatePanel(parent, "RosterHeader");
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, UIFactory.MinTouch);
            header.anchoredPosition = Vector2.zero;
            UIFactory.AddImage(header, ColHeader);

            _recruitButton = UIFactory.CreateButton(header, "RecruitBtn", "Recruit", ColRecruit, OnRecruit);
            var rb = (RectTransform)_recruitButton.transform;
            rb.anchorMin = new Vector2(0f, 0f); rb.anchorMax = new Vector2(0.45f, 1f);
            rb.offsetMin = new Vector2(8f, 8f); rb.offsetMax = new Vector2(-8f, -8f);

            _countLabel = UIFactory.CreateText(header, "Count", "", 30f, TextAlignmentOptions.Right);
            var cl = (RectTransform)_countLabel.transform;
            cl.anchorMin = new Vector2(0.45f, 0f); cl.anchorMax = new Vector2(1f, 1f);
            cl.offsetMin = Vector2.zero; cl.offsetMax = new Vector2(-16f, 0f);

            // Scroll list filling the rest below the header.
            var scrollRoot = UIFactory.CreateVerticalScroll(parent, "RosterScroll", out _listContent);
            scrollRoot.anchorMin = new Vector2(0f, 0f);
            scrollRoot.anchorMax = new Vector2(1f, 1f);
            scrollRoot.offsetMin = new Vector2(0f, 0f);
            scrollRoot.offsetMax = new Vector2(0f, -UIFactory.MinTouch);

            Refresh();
        }

        public void Refresh()
        {
            var adv = AdventurerManager.Instance;
            if (adv == null || _listContent == null) return;

            int cap = adv.RosterCap();
            _countLabel.text = $"Roster {adv.LivingCount()}/{cap}";

            var balance = GameManager.Instance != null ? GameManager.Instance.Balance : null;
            if (balance != null)
                UIFactory.LabelOf(_recruitButton).text = $"Recruit ({balance.recruitCost}g)";

            // Clear and rebuild rows.
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_listContent.GetChild(i).gameObject);
            }

            foreach (var a in adv.Roster)
            {
                string capturedId = a.id;
                string status = a.status == AdventurerStatus.Injured
                    ? $"{a.status}/{a.injurySeverity}"
                    : a.status.ToString();
                string cls = ContentDatabase.GetClass(a.classId)?.displayName ?? a.classId;
                string label = $"Lv.{a.level}  {a.displayName}  -  {cls}  [{status}]";

                var row = UIFactory.CreateButton(_listContent, $"Row_{a.id}", label, ColRow,
                    () => UIManager.Instance.ShowAdventurerDetail(capturedId));
                UIFactory.SetPreferredHeight((RectTransform)row.transform, UIFactory.MinTouch);

                var lbl = UIFactory.LabelOf(row);
                lbl.alignment = TextAlignmentOptions.Left;
                lbl.color = UIFactory.StatusColor(a.status);
                lbl.margin = new Vector4(16f, 0f, 8f, 0f);
            }
        }

        private void OnRecruit()
        {
            var adv = AdventurerManager.Instance;
            if (adv == null) return;
            adv.TryRecruit();
            UIManager.Instance.Refresh();
            GameManager.Instance?.SaveGame();
        }
    }
}
