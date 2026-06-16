namespace Guildmaster
{
    /// <summary>
    /// Shared enumerations used across systems. Kept in one file so the data
    /// vocabulary of the game is discoverable in a single place.
    /// </summary>

    /// Class progression tiers. The class SYSTEM must handle all three from day
    /// one (CLAUDE.md §5); MVP only authors Base + 2 Hybrid instances.
    public enum ClassTier { Base, Hybrid, Legendary }

    /// Equipment slots (GEAR_TALENT_SPEC B1). Consumables handled separately.
    public enum ItemSlot { Weapon, Armor, Accessory1, Accessory2 }

    /// Gear material tier — drives base stat magnitude (GEAR_TALENT_SPEC B2).
    public enum MaterialTier { Leather, Iron, Steel, Mithril, Adamantite }

    /// Dungeon themes, used by gear "theme affinity" effects (GEAR_TALENT_SPEC B3).
    public enum DungeonTheme { Neutral, Undead, Beast, Fire, Arcane, Nature }

    /// Auto-resolve outcome bands (CLAUDE.md §5). Flawless -> Catastrophe.
    public enum OutcomeBand { Flawless, Success, CloseCall, Failure, Catastrophe }

    /// Adventurer lifecycle status (UI_SPEC §2 / Roster colour coding).
    public enum AdventurerStatus { Healthy, Injured, OnExpedition, Training, DeadPending, Dead }

    /// Special-effect categories for gear (GEAR_TALENT_SPEC B3). All resolve to
    /// auto-resolve math modifiers.
    public enum SpecialEffectCategory { None, ThemeAffinity, RiskTrade, Survival, Economy, SynergyEnabler, Conditional }

    /// Flat-list talent effects for MVP (GEAR_TALENT_SPEC C3 light layer).
    public enum TalentEffectType { ExpeditionSuccess, GoldGain, InjuryRecovery, RosterSlot, HeirStartLevel }

    /// Core stat axes feeding the combat math (GEAR_TALENT_SPEC B2).
    public enum StatType { HP, Mana, Attack, Defense, Crit }

    /// The five primary navigation tabs (UI_SPEC §3).
    public enum GameTab { Guild, Roster, Quests, Craft, Hall }
}
