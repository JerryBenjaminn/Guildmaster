using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// An enemy template. In MVP auto-resolve, enemies contribute to a dungeon's
    /// effective difficulty; the structure is ready for a future visual battler
    /// (CLAUDE.md §9 — deferred, but data stays ready).
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Guildmaster/Enemy", order = 50)]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string displayName;

        public DungeonTheme theme = DungeonTheme.Neutral;

        [Tooltip("Power this enemy adds to its dungeon's effective difficulty.")]
        public float powerContribution = 10f;

        public StatBlock stats = new StatBlock(50, 0, 8, 5, 0);

        public bool isBoss = false;
    }
}
