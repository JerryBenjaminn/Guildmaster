using UnityEngine;

namespace Guildmaster
{
    /// <summary>
    /// An ability. In MVP abilities provide utility/synergy modifiers to the
    /// auto-resolve math (NOT raw power — anti-snowball, D7). 2-3 signature
    /// abilities pass down through inheritance.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityData", menuName = "Guildmaster/Ability", order = 30)]
    public class AbilityData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Tooltip("Flat stat contribution this ability grants to its bearer.")]
        public StatBlock statBonus = new StatBlock();

        [Tooltip("Success-chance modifier as a fraction (utility/synergy, kept small).")]
        public float successChanceBonus = 0f;

        [Tooltip("If set, this ability counts toward team synergy combos.")]
        public bool isSynergyEnabler = false;
    }
}
