# GEAR & TALENT SYSTEM — Guildmaster

> Two progression systems that turn "numbers go up" into "I'm crafting a build."
> Designed in full; built in layers (light → mid → deep).
> **Core constraint:** everything feeds the AUTO-RESOLVE calculation and team
> synergy — never real-time play. Gear/talents change *team power, success %,
> and outcome bands*, not moment-to-moment mechanics.
> Talents attach to the **bloodline** (Model 2): they persist and pass to heirs.

---

## PART A — DESIGN PRINCIPLES (both systems)

1. **Everything resolves to the combat math.** Effects modify: team Power,
   success-chance, outcome-band probabilities, injury/death-save rolls, or
   loot. If an effect can't be expressed as one of these, it doesn't belong.
2. **Decisions over numbers.** A bigger sword is boring. A sword that trades
   defense for crit, or that's strong vs. undead but weak vs. fire, is a
   *decision*. Aim for meaningful trade-offs, not strict upgrades.
3. **Depth from combination, not quantity.** 40 items + a talent tree should
   produce thousands of viable builds through interaction, not 40 isolated
   choices.
4. **Layered build:** ship the light version, expand without re-architecting.
   The data structures below support the deep version from day one (SOs ready),
   even when MVP only fills the light layer.

---

## PART B — GEAR SYSTEM

### B1. Slots (from GDD, confirmed)
Weapon · Armor · Accessory 1 · Accessory 2 · (Consumables handled separately)

### B2. The three layers

**LIGHT (MVP) — stats + a few special effects**
- Each item has base stats (Attack, Defense, HP, Mana, Crit, etc.).
- Items have a **tier** (material: leather→iron→steel→mithril→adamantite) and
  a **level**, driving base stat magnitude.
- ~20% of items carry **one special effect** (see B3) — the rest are pure stats.
- Procedurally generated from templates (per CLAUDE.md — no handcrafting).

**MID — affixes & conditional effects**
- Items roll **affixes** (prefix/suffix): "+15% dmg vs undead", "-10% injury
  chance", "+5% gold from this expedition". Multiple affixes per item.
- Introduces **conditional power**: gear that's strong in specific dungeon
  themes, encouraging swapping loadouts per expedition (a real decision).

**DEEP — set bonuses, scaling effects, build-defining uniques**
- **Set items**: wearing 2/4 pieces of a set grants escalating bonuses
  (drives "I'm building toward the X set").
- **Scaling effects**: "+1% crit per 2 levels", "damage scales with missing HP"
  (pairs with Berserker bloodline).
- **Legendary uniques** with build-defining effects (the 10–15 from GDD):
  e.g. "Staff of the Arc Mage: +100% spell power, -50% HP" — defines a glass-
  cannon build rather than just adding numbers.

### B3. Special effect categories (the interesting part)
All expressed as auto-resolve modifiers:
| Category | Example | Resolves as |
|----------|---------|-------------|
| Theme affinity | +X% vs undead/fire/beast | Power mod vs matching dungeon |
| Risk trade | +crit, -defense | Shifts outcome bands (more flawless/more close-call) |
| Survival | +X% death-save, -injury chance | Modifies death/injury rolls |
| Economy | +X% gold/loot from expedition | Post-success reward mod |
| Synergy enabler | "counts as Warrior for synergy" | Unlocks team synergy combos |
| Conditional | "+dmg if solo", "+dmg in full squad" | Power mod gated by team size |

> The **synergy enabler** and **conditional** categories are what make gear a
> *team-composition* decision, not just a per-character upgrade. This is the
> bridge between gear and the strategic core.

### B4. Acquisition
Dungeon loot (primary, procedural) · shop (gold) · inheritance (scaled to ~30%,
per D7) · (deferred) crafting at Smithy.

---

## PART C — TALENT SYSTEM (bloodline-attached, Model 2)

### C1. Core concept
Talents are a **dynasty-level progression**. Talent points are earned through
play and invested into a **bloodline talent tree** that persists across deaths
and passes to heirs. Permadeath does NOT wipe talent investment — it inherits.
This makes the talent tree the player's long-term "dynasty build," sitting
alongside bloodline stat bonuses and hybrid-class unlocks.

> Why bloodline, not character: permadeath would make per-character talents
> feel wasted and punish investment. Bloodline-attached talents *reward*
> continuity — the dynasty's identity deepens over generations.

### C2. How points are earned
Seed proposal (tune later): talent points granted by **dynasty milestones**,
not individual grinding — e.g. each rank-up, each first-clear of a dungeon,
each generation reaching a level threshold. This ties talent growth to
*meaningful progress*, not repetitive farming (respects the "no grind" pillar).

### C3. The three layers

**LIGHT (MVP) — a small flat talent list**
- ~10–15 talents, each a simple permanent dynasty bonus, pickable as points
  allow. Flat list, no tree structure yet.
- Examples: "+5% expedition success", "+10% gold", "-1 injury recovery hour",
  "+1 max roster slot", "heirs start at level 2".
- Proves the loop: earn points → invest → dynasty gets permanently stronger.

**MID — branching tree with paths**
- Talents organized into **branches** (e.g. Martial / Arcane / Economic /
  Survival). Investing in a branch unlocks deeper nodes in it.
- Introduces **opportunity cost**: limited points mean you specialize a
  dynasty's identity ("we are a wealthy economic guild" vs "a martial one").

**DEEP — class-linked branches & keystones**
- Branches gated/empowered by **bloodline composition**: a dynasty rich in
  Mage ancestors unlocks deeper Arcane talents. Ties talents to the breeding
  mechanic — your lineage choices shape which talents you *can* take.
- **Keystone talents**: powerful, build-defining nodes with trade-offs, e.g.
  "Glass Dynasty: +25% team power, +15% death chance" — a dynasty-wide identity
  choice echoing the legendary-item philosophy.
- **Respec**: allowed but costly (gold or a rare resource), so choices matter
  but aren't permanent traps.

### C4. Relationship to existing dynasty systems
Three dynasty layers now stack coherently:
1. **Bloodline stat bonuses** (passive, automatic, from ancestor classes) — D7.
2. **Hybrid/legendary class unlocks** (gates on lineage composition).
3. **Talent tree** (active investment, player-directed dynasty build) — NEW.

All three are "the dynasty remembers." Talents are the *player-directed* layer —
the others are earned passively; talents are *chosen*. That distinction is what
makes talents feel like agency rather than drift.

---

## PART D — HOW GEAR + TALENTS + SYNERGY COMBINE (the build space)

A single expedition's team power is roughly:

```
TeamPower = Σ (each member:
    base stats (class + level)
  + bloodline stat bonuses        [passive dynasty layer]
  + gear stats & affixes          [per-character, swappable]
  + gear special effects          [conditional / theme / synergy]
  )
  + team synergy bonuses          [from class pairs & gear enablers]
  + dynasty talent modifiers      [chosen dynasty layer]
→ compared to dungeon difficulty → success chance & outcome bands
```

The **build-craft loop** the player engages in:
1. Which adventurers, which classes? (roster + breeding)
2. Which gear on each — optimizing for *this dungeon's theme*? (swappable)
3. Which team synergies do my classes + gear enable? (composition)
4. Which dynasty talents am I building toward long-term? (meta)

That's four interacting decision layers producing the "thousands of builds from
small parts" depth — without any real-time mechanics.

---

## PART E — MVP SCOPE (what to actually build first)

| System | MVP (light) | Deferred (mid/deep) |
|--------|-------------|---------------------|
| Gear stats + tiers | ✅ | — |
| Gear special effects | ✅ ~20% of items, from B3 categories | more categories, scaling |
| Gear affixes | — | MID |
| Gear sets / legendaries | — | DEEP |
| Talent system | ✅ flat list, 10–15 nodes | tree, branches, keystones |
| Talent point sources | ✅ dynasty milestones | class-gated branches |
| Respec | — | DEEP |

**MVP proves:** gear creates per-dungeon loadout decisions, and the talent list
gives the dynasty a permanent, *chosen* progression that survives permadeath.
Both data structures are built to expand into the deep version via new SOs —
no re-architecture needed.

---

## PART F — OPEN DECISIONS (for Jerry)

1. **Talent point economy** — how many points, how fast? (Affects whether the
   tree feels rewarding vs. trivial.) Seed in C2; tune later.
2. **Respec policy** — gold cost, rare-resource cost, or free? (DEEP layer.)
3. **Gear loadout swapping** — free between expeditions, or a (small) cost /
   time to discourage perfect min-maxing every run? Recommend **free in MVP**,
   revisit if it feels too fiddly.
4. **Talent visibility** — does the heir "inherit" a visible tree, or is it
   purely guild-wide? Recommend **guild-wide dynasty tree** (one tree per
   dynasty, not per character) for clarity.

---

*Version 0.1 — designed in full, built in layers. Feeds the auto-resolve math.*
