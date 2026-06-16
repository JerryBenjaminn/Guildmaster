using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Defines an adventurer class. The engine handles Base / Hybrid / Legendary
    /// from day one (CLAUDE.md §5); MVP authors only Base + 2 Hybrid assets.
    /// Hybrid/legendary unlocks are lineage-driven and permanent/guild-wide.
    /// </summary>
    [CreateAssetMenu(fileName = "ClassData", menuName = "Guildmaster/Adventurer Class", order = 10)]
    public class AdventurerClassData : ScriptableObject
    {
        [Tooltip("Stable id used in save files & lineage tracking. Must be unique.")]
        public string id;
        public string displayName;
        [TextArea] public string description;

        public ClassTier tier = ClassTier.Base;

        [Tooltip("Base stats at level 1. Magnitudes are content, not balance constants.")]
        public StatBlock baseStats = new StatBlock(100, 0, 10, 10, 5);

        [Tooltip("Stat growth applied per level (content).")]
        public StatBlock statsPerLevel = new StatBlock(10, 0, 2, 2, 1);

        [Header("Unlock requirements (Hybrid/Legendary only)")]
        [Tooltip("Lineage composition required to unlock this class, e.g. need N ancestors of class X. Empty for base classes.")]
        public List<LineageRequirement> unlockRequirements = new List<LineageRequirement>();

        [Tooltip("Signature abilities this class tends to carry (referenced by id).")]
        public List<string> signatureAbilityIds = new List<string>();

        public StatBlock StatsAtLevel(int level)
        {
            var result = baseStats;
            for (int i = 1; i < level; i++)
            {
                result = result + statsPerLevel;
            }
            return result;
        }
    }

    [System.Serializable]
    public class LineageRequirement
    {
        [Tooltip("Ancestor class id required in the bloodline.")]
        public string ancestorClassId;
        [Tooltip("How many ancestors of that class are needed.")]
        public int count = 1;
    }
}
