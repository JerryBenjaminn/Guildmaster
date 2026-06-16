using System.Collections.Generic;
using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// A dungeon / expedition destination (UI_SPEC §3 Quests). Difficulty and
    /// rewards are CONTENT (per-asset), not balance constants. Adding a dungeon =
    /// creating one of these assets, never writing code.
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonData", menuName = "Guildmaster/Dungeon", order = 40)]
    public class DungeonData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        public DungeonTheme theme = DungeonTheme.Neutral;

        [Tooltip("Difficulty value compared against team Power in the auto-resolve math.")]
        public float difficulty = 100f;

        [Tooltip("Recommended adventurer level (display/guidance only).")]
        public int recommendedLevel = 1;

        [Tooltip("Gold cost to send an expedition here.")]
        public int entryFee = 0;

        [Tooltip("Guild rank required to unlock (0 = available from start).")]
        public int requiredRank = 0;

        [Tooltip("Enemies that may appear (content; feeds future visual battler).")]
        public List<EnemyData> enemies = new List<EnemyData>();

        [Tooltip("Items that can drop here (procedural generation pulls from these templates).")]
        public List<ItemData> lootTable = new List<ItemData>();

        [Tooltip("Reward multiplier applied to the duration-tier gold/hr from BalanceConfig.")]
        public float rewardMultiplier = 1f;
    }
}
