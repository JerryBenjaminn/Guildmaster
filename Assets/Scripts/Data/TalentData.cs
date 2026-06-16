using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// A bloodline-attached talent (GEAR_TALENT_SPEC Part C). MVP = LIGHT layer:
    /// a flat list of simple permanent dynasty bonuses. Talents persist across
    /// deaths and pass to heirs (Model 2). Tree/branches/keystones are MID/DEEP.
    /// </summary>
    [CreateAssetMenu(fileName = "TalentData", menuName = "Guildmaster/Talent", order = 70)]
    public class TalentData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        public TalentEffectType effect = TalentEffectType.ExpeditionSuccess;

        [Tooltip("Magnitude per rank. Meaning depends on effect type (fraction or flat count).")]
        public float magnitudePerRank = 0.05f;

        [Tooltip("Talent points to buy one rank.")]
        public int costPerRank = 1;

        [Tooltip("Maximum ranks (1 = single pick).")]
        public int maxRank = 1;
    }
}
