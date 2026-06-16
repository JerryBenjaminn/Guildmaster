using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// A guild facility (UI_SPEC §3 Guild tab). Generic upgrade engine: a facility
    /// is defined by a list of levels, each with a cost and an effect magnitude.
    /// MVP content: Quest Board, Infirmary, Treasury, Training Ground — authored
    /// as assets, not code.
    /// </summary>
    [CreateAssetMenu(fileName = "FacilityData", menuName = "Guildmaster/Facility", order = 60)]
    public class FacilityData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Tooltip("Per-level upgrade definition. Index 0 = level 1.")]
        public List<FacilityLevel> levels = new List<FacilityLevel>();

        public int MaxLevel => levels.Count;

        public FacilityLevel GetLevel(int level)
        {
            int index = level - 1;
            if (index < 0 || index >= levels.Count) return null;
            return levels[index];
        }
    }

    [System.Serializable]
    public class FacilityLevel
    {
        [Tooltip("Gold cost to upgrade INTO this level.")]
        public int upgradeCost = 100;
        [Tooltip("Effect magnitude at this level (interpretation depends on the facility).")]
        public float effectMagnitude = 0f;
    }
}
