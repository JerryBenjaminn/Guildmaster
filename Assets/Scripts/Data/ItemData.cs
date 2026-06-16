using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// Gear template (GEAR_TALENT_SPEC Part B). MVP = LIGHT layer (stats + tier +
    /// level, ~20% carry one special effect). Affixes/sets/legendaries are MID/
    /// DEEP and added later via new fields/SOs without re-architecture.
    /// Items are procedurally generated from these templates, not handcrafted.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData", menuName = "Guildmaster/Item", order = 20)]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        public ItemSlot slot = ItemSlot.Weapon;
        public MaterialTier material = MaterialTier.Leather;

        [Tooltip("Item level — combines with material to drive base stat magnitude.")]
        public int itemLevel = 1;

        public StatBlock baseStats = new StatBlock(0, 0, 5, 0, 0, 0, 0);

        [Header("Special effect (LIGHT layer — ~20% of items carry one)")]
        public SpecialEffectCategory specialEffect = SpecialEffectCategory.None;
        [Tooltip("Theme this effect applies to, when relevant (e.g. ThemeAffinity).")]
        public DungeonTheme effectTheme = DungeonTheme.Neutral;
        [Tooltip("Magnitude of the special effect as a fraction (e.g. 0.15 = +15%). Resolves to auto-resolve math.")]
        public float effectMagnitude = 0f;
    }
}
