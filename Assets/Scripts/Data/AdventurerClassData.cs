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

        [Tooltip("Base stats at level 1 (HP, Mana, Attack, MagicPower, Defense, Speed, Crit). Content, not balance constants.")]
        public StatBlock baseStats = new StatBlock(100, 0, 10, 0, 10, 10, 5);

        [Tooltip("Stat growth applied per level (content).")]
        public StatBlock statsPerLevel = new StatBlock(10, 0, 2, 0, 2, 1, 1);

        [Header("Ability slots (per-class config, Task02 §1)")]
        [Tooltip("Levels at which this class gains an ability slot. Slot count at a level = entries <= that level.")]
        public List<int> abilitySlotUnlockLevels = new List<int> { 1, 3, 6, 10 };

        [Tooltip("Class passive ability (referenced by id). Optional; abilities are authored later.")]
        public string passiveAbilityId;
        [TextArea] public string passiveNote;

        [Tooltip("Hint for the kind of starting gear this class favours (display/UX only for now).")]
        public string startingGearHint;

        [Header("Unlock requirements (Hybrid/Legendary only)")]
        [Tooltip("Lineage composition required to unlock this class, e.g. need N ancestors of class X. Empty for base classes.")]
        public List<LineageRequirement> unlockRequirements = new List<LineageRequirement>();

        [Tooltip("Signature abilities this class tends to carry (referenced by id).")]
        public List<string> signatureAbilityIds = new List<string>();

        /// <summary>Number of ability slots unlocked at a given level.</summary>
        public int AbilitySlotsAtLevel(int level)
        {
            int count = 0;
            for (int i = 0; i < abilitySlotUnlockLevels.Count; i++)
            {
                if (abilitySlotUnlockLevels[i] <= level) count++;
            }
            return count;
        }

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
