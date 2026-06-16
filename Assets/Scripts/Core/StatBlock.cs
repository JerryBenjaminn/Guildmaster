using System;

namespace Guildmaster
{
    /// <summary>
    /// A bundle of the core combat stats (GDD/Task02 stat set: HP, Mana, Attack,
    /// Magic Power, Defense, Speed, Crit). Pure data: holds values and supports
    /// addition so contributions (class base + level + bloodline + gear) can be
    /// summed. Converting stats into a single "Power" number is a balance concern
    /// and lives in <see cref="CombatResolver"/> (weights from BalanceConfig) —
    /// keep this struct free of balance numbers.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public int hp;
        public int mana;
        public int attack;
        public int magicPower;
        public int defense;
        public int speed;
        public int crit;

        public StatBlock(int hp, int mana, int attack, int magicPower, int defense, int speed, int crit)
        {
            this.hp = hp;
            this.mana = mana;
            this.attack = attack;
            this.magicPower = magicPower;
            this.defense = defense;
            this.speed = speed;
            this.crit = crit;
        }

        public static StatBlock operator +(StatBlock a, StatBlock b)
        {
            return new StatBlock(
                a.hp + b.hp,
                a.mana + b.mana,
                a.attack + b.attack,
                a.magicPower + b.magicPower,
                a.defense + b.defense,
                a.speed + b.speed,
                a.crit + b.crit);
        }

        public int Get(StatType type)
        {
            switch (type)
            {
                case StatType.HP: return hp;
                case StatType.Mana: return mana;
                case StatType.Attack: return attack;
                case StatType.MagicPower: return magicPower;
                case StatType.Defense: return defense;
                case StatType.Speed: return speed;
                case StatType.Crit: return crit;
                default: return 0;
            }
        }

        public override string ToString()
        {
            return $"HP {hp}, MP {mana}, ATK {attack}, MAG {magicPower}, DEF {defense}, SPD {speed}, CRIT {crit}";
        }
    }
}
