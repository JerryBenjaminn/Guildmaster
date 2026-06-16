using System;

namespace Guildmaster
{
    /// <summary>
    /// A bundle of the core combat stats. Pure data: holds values and supports
    /// addition so contributions (class base + level + gear + bloodline) can be
    /// summed. The conversion of stats into a single "Power" number is a balance
    /// concern and lives in <see cref="CombatResolver"/> (weights come from
    /// BalanceConfig), NOT here — keep this struct free of balance numbers.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public int hp;
        public int mana;
        public int attack;
        public int defense;
        public int crit;

        public StatBlock(int hp, int mana, int attack, int defense, int crit)
        {
            this.hp = hp;
            this.mana = mana;
            this.attack = attack;
            this.defense = defense;
            this.crit = crit;
        }

        public static StatBlock operator +(StatBlock a, StatBlock b)
        {
            return new StatBlock(
                a.hp + b.hp,
                a.mana + b.mana,
                a.attack + b.attack,
                a.defense + b.defense,
                a.crit + b.crit);
        }

        public int Get(StatType type)
        {
            switch (type)
            {
                case StatType.HP: return hp;
                case StatType.Mana: return mana;
                case StatType.Attack: return attack;
                case StatType.Defense: return defense;
                case StatType.Crit: return crit;
                default: return 0;
            }
        }

        public override string ToString()
        {
            return $"HP {hp}, MP {mana}, ATK {attack}, DEF {defense}, CRIT {crit}";
        }
    }
}
