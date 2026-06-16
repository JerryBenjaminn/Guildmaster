using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Guildmaster
{
    /// <summary>
    /// The Adventurer Detail overlay (UI_SPEC §4.1): stats, equipment slots,
    /// ability slots, lineage panel, and the actions [Assign to Team] [Train]
    /// [Retire] [Rename]. Its own dimmed, tap-outside-to-dismiss layer. Functional,
    /// not polished — prefab/visual pass is later.
    ///
    /// Scope notes: "Assign to Team" is a stub (Expedition Prep is a later task);
    /// "Train" exercises the Healthy&lt;-&gt;Training state transition (the training
    /// XP system itself is later).
    /// </summary>
    public class AdventurerDetailOverlay
    {
        private GameObject _root;
        private string _currentId;

        private TextMeshProUGUI _name, _sub, _stats, _equip, _ability, _lineage, _result;
        private TMP_InputField _renameField;
        private Button _trainButton;

        private static readonly Color ColDim = new Color(0f, 0f, 0f, 0.78f);
        private static readonly Color ColCard = new Color(0.16f, 0.17f, 0.20f, 1f);
        private static readonly Color ColAction = new Color(0.26f, 0.34f, 0.46f, 1f);
        private static readonly Color ColRetire = new Color(0.46f, 0.30f, 0.30f, 1f);

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Build(Transform canvas)
        {
            var layer = UIFactory.CreateStretch(canvas, "AdventurerDetailOverlay");
            _root = layer.gameObject;
            var dim = UIFactory.AddImage(layer, ColDim);
            var dismiss = layer.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = dim;
            dismiss.onClick.AddListener(Hide);

            var card = UIFactory.CreatePanel(layer, "Card");
            card.anchorMin = new Vector2(0.05f, 0.1f);
            card.anchorMax = new Vector2(0.95f, 0.9f);
            card.offsetMin = Vector2.zero; card.offsetMax = Vector2.zero;
            UIFactory.AddImage(card, ColCard);
            // Block the dismiss click when tapping the card itself.
            card.gameObject.AddComponent<Button>();

            _name = Text(card, "Name", 48f, 0.88f, 1.00f, TextAlignmentOptions.Center);
            _sub = Text(card, "Sub", 28f, 0.82f, 0.88f, TextAlignmentOptions.Center);
            _stats = Text(card, "Stats", 26f, 0.58f, 0.82f, TextAlignmentOptions.TopLeft);
            _equip = Text(card, "Equip", 24f, 0.42f, 0.58f, TextAlignmentOptions.TopLeft);
            _ability = Text(card, "Ability", 24f, 0.32f, 0.42f, TextAlignmentOptions.TopLeft);
            _lineage = Text(card, "Lineage", 24f, 0.22f, 0.32f, TextAlignmentOptions.TopLeft);
            _result = Text(card, "Result", 22f, 0.17f, 0.22f, TextAlignmentOptions.Center);
            _result.color = new Color(0.9f, 0.85f, 0.4f);

            // Rename field row.
            _renameField = UIFactory.CreateInputField(card, "Rename", "");
            var rf = (RectTransform)_renameField.transform;
            rf.anchorMin = new Vector2(0.04f, 0.10f); rf.anchorMax = new Vector2(0.62f, 0.16f);
            rf.offsetMin = Vector2.zero; rf.offsetMax = Vector2.zero;
            var saveBtn = UIFactory.CreateButton(card, "RenameSave", "Rename", ColAction, OnRename);
            Anchor(saveBtn, 0.64f, 0.10f, 0.96f, 0.16f);

            // Action row: Assign / Train / Retire.
            var assign = UIFactory.CreateButton(card, "Assign", "Assign to Team", ColAction, OnAssign);
            Anchor(assign, 0.04f, 0.02f, 0.36f, 0.085f);
            _trainButton = UIFactory.CreateButton(card, "Train", "Train", ColAction, OnTrain);
            Anchor(_trainButton, 0.37f, 0.02f, 0.63f, 0.085f);
            var retire = UIFactory.CreateButton(card, "Retire", "Retire", ColRetire, OnRetire);
            Anchor(retire, 0.64f, 0.02f, 0.96f, 0.085f);

            _root.SetActive(false);
        }

        public void Show(string adventurerId)
        {
            _currentId = adventurerId;
            _result.text = "";
            Populate();
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void Populate()
        {
            var adv = AdventurerManager.Instance;
            var a = adv != null ? adv.GetById(_currentId) : null;
            if (a == null) { Hide(); return; }

            var cls = ContentDatabase.GetClass(a.classId);
            _name.text = a.displayName;
            _name.color = UIFactory.StatusColor(a.status);
            string statusStr = a.status.ToString();
            if (a.status == AdventurerStatus.Injured)
            {
                double secs = adv.RecoverySecondsRemaining(a, DateTime.UtcNow.Ticks);
                statusStr = $"{a.status} ({a.injurySeverity}) — heals in {FormatDuration(secs)}";
            }
            _sub.text = $"{(cls != null ? cls.displayName : a.classId)}  •  Gen {a.generation}  •  {statusStr}";

            var s = a.stats;
            _stats.text =
                $"<b>Stats (Lv.{a.level}, XP {a.xp})</b>\n" +
                $"HP {s.hp}    Mana {s.mana}\n" +
                $"Attack {s.attack}    Magic {s.magicPower}\n" +
                $"Defense {s.defense}    Speed {s.speed}    Crit {s.crit}";

            _equip.text = "<b>Equipment</b>\n" +
                $"Weapon: {ItemName(a.equipment.weaponItemId)}\n" +
                $"Armor: {ItemName(a.equipment.armorItemId)}\n" +
                $"Accessory 1: {ItemName(a.equipment.accessory1ItemId)}    Accessory 2: {ItemName(a.equipment.accessory2ItemId)}";

            int slots = adv.AbilitySlotCount(a);
            var ab = new StringBuilder();
            ab.Append("<b>Ability slots</b>  ").Append(a.abilityIds.Count).Append('/').Append(slots).Append('\n');
            if (a.abilityIds.Count == 0) ab.Append("(none assigned)");
            else ab.Append(string.Join(", ", a.abilityIds));
            _ability.text = ab.ToString();

            _lineage.text = "<b>Lineage</b>\n" +
                $"Family: {(string.IsNullOrEmpty(a.familyName) ? "—" : a.familyName)}\n" +
                $"Bloodline bonus: {BloodlineSummary()}";

            _renameField.text = a.displayName;
            UIFactory.LabelOf(_trainButton).text = a.status == AdventurerStatus.Training ? "Stop Training" : "Train";
        }

        private string BloodlineSummary()
        {
            var lm = LegacyManager.Instance;
            if (lm == null || lm.BloodlineBonuses == null || lm.BloodlineBonuses.Count == 0) return "none yet (Gen-1 baseline)";
            var sb = new StringBuilder();
            foreach (var b in lm.BloodlineBonuses)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(b.statType).Append(" +").Append(Mathf.RoundToInt(b.fraction * 100f)).Append('%');
            }
            return sb.ToString();
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0) return "moments";
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }

        private static string ItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "<i>Empty</i>";
            return ContentDatabase.GetItem(itemId)?.displayName ?? itemId;
        }

        // ---- Actions ----

        private void OnAssign()
        {
            _result.text = "Expedition prep (team assignment) arrives in a later task.";
        }

        private void OnTrain()
        {
            var adv = AdventurerManager.Instance;
            var a = adv?.GetById(_currentId);
            if (a == null) return;

            AdventurerStatus target = a.status == AdventurerStatus.Training
                ? AdventurerStatus.Healthy
                : AdventurerStatus.Training;

            if (adv.TrySetStatus(_currentId, target))
            {
                _result.text = target == AdventurerStatus.Training ? "Now training." : "Returned to duty.";
                AfterChange();
            }
            else
            {
                _result.text = $"Can't change to {target} from {a.status}.";
            }
        }

        private void OnRetire()
        {
            var adv = AdventurerManager.Instance;
            if (adv == null) return;
            if (adv.Retire(_currentId)) { _result.text = "Retired."; AfterChange(); }
            else { _result.text = "Cannot retire from the current status."; }
        }

        private void OnRename()
        {
            var adv = AdventurerManager.Instance;
            if (adv == null) return;
            adv.Rename(_currentId, _renameField.text);
            _result.text = "Renamed.";
            AfterChange();
        }

        private void AfterChange()
        {
            Populate();
            UIManager.Instance.Refresh();
            GameManager.Instance?.SaveGame();
        }

        // ---- layout helpers ----

        private static TextMeshProUGUI Text(RectTransform card, string name, float size, float yMin, float yMax, TextAlignmentOptions align)
        {
            var t = UIFactory.CreateText(card, name, "", size, align);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = new Vector2(0.04f, yMin);
            rt.anchorMax = new Vector2(0.96f, yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return t;
        }

        private static void Anchor(Button b, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
