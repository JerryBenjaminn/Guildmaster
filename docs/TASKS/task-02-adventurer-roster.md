# Task 02 — Adventurer / Roster System + Base Classes

> Builds the first real system onto the verified bootstrap skeleton.
> Read CLAUDE.md (project root) first — it is the law. Comply throughout,
> especially §3 (technical rules), §4 (locked decisions D1–D7), §11 (decision
> authority). Domain refs: docs/UI_SPEC.md, docs/GEAR_TALENT_SPEC.md.

---

## Why this is task 02

The adventurer is the root the rest of the game hangs off: expeditions need
real adventurers to compute team power, legacy needs an adventurer who dies,
gear needs slots on an adventurer. Build this fully and correctly now; later
systems (03 combat-resolve, 04 legacy) plug into it.

The core loop is already wired at stub level from bootstrap. This task replaces
the adventurer stub with a real roster system **without breaking the loop** —
it must remain send→collect runnable in-editor at every step.

## Scope — build fully

**1. Base classes (7) as data**
- Author `AdventurerClassData` SO assets for all 7 base classes:
  Adventurer, Warrior, Mage, Priest, Rogue, Ranger, Berserker.
- Each defines: base stats (HP, Mana, Attack, Magic Power, Defense, Speed,
  Crit), ability-slot configuration (per GDD/class), passive, starting gear
  type hint. **All numbers come from BalanceConfig or the SO — none hardcoded
  in .cs** (§3.3). These stat values are *seed proposals* under §11 — set
  sensible ones, mark them as tunable, report them.
- The class system engine must already handle hybrid/legendary tiers in code
  (from bootstrap). Author NO hybrid/legendary instances yet (§9).

**2. Adventurer runtime model — full**
- Identity: name, portrait ref, class, generation, family name.
- Stats: live values derived from class base + level + (later) bloodline/gear.
  Build the derivation pipeline so bloodline/gear modifiers can slot in later
  without rework (leave the hooks; they sum to zero for now).
- Level (1–20) + XP. Leveling grants stat growth per CLAUDE.md/GDD; on level
  thresholds, unlock ability slots. Keep growth values in SO/BalanceConfig.
- Status state machine: Healthy, Injured (Minor/Major/Critical), OnExpedition,
  Training, Retired, Dead. Enforce valid transitions (e.g. can't deploy an
  injured/dead adventurer).
- Equipment slots (weapon, armor, 2 accessories) + ability slots as data
  structures — slots exist and are assignable; gear *effects* are task 05, so
  assignment can be a no-op on stats for now, but the slots must be real.

**3. Roster management — full**
- AdventurerManager: create (recruit + legacy paths), remove, query, set
  status, count by status. Recruit costs gold via GuildManager (seed cost in
  BalanceConfig, §11). Roster cap from FacilityData (Barracks) — respect it;
  Barracks itself can stay minimal.
- Recruitment creates a Gen-1 "Adventurer"-class blank-slate by default.

**4. UI — Roster tab + Adventurer Detail overlay (functional, not polished)**
- Per UI_SPEC.md §3 (Tab 2) and §4.1. uGUI + TMP (§3.7), built onto the
  existing in-code shell. Roster list: portrait/name/class/level/status
  (color-coded). Tap → Adventurer Detail overlay showing stats, equipment
  slots, ability slots, lineage panel (lineage can show Gen-1 baseline now),
  actions [Assign to Team][Train][Retire][Rename].
- Functional and readable; visual polish/prefab refactor is a later UI pass.

**5. Keep the loop runnable**
- The bootstrap stub dungeon/expedition must still work: you can now build a
  team from real roster adventurers, send, and collect. Combat-resolve stays
  stubbed (real resolve = task 03) — but team power should be *computed from
  real adventurer stats* so 03 has something to consume.

## Do NOT build (defer)

Real combat-resolve math (task 03) · legacy/inheritance flesh (task 04 — the
engine hooks exist, don't fill) · gear effects on stats (task 05) · talents
(later) · hybrid/legendary class instances · audio/VFX/animation · crafting ·
prefab/visual polish pass · editor tools.

## Constraints (from CLAUDE.md)

uGUI + TMP only · 2D/URP · data-driven, no hardcoded content/balance numbers ·
single scene, no transitions · managers stay at 7, single responsibility ·
mobile-first (portrait, ≥44px targets, no per-frame alloc) · small descriptive
commits.

## Definition of done

- [ ] 7 base-class `AdventurerClassData` assets exist, stats in SO/BalanceConfig.
- [ ] Adventurer model complete: identity, derived stats (with zero-sum hooks
      for bloodline/gear), level/XP, status state machine with enforced
      transitions, equipment + ability slots as real assignable structures.
- [ ] AdventurerManager: recruit (gold cost), legacy-create path stub-callable,
      remove, query, status, cap enforcement.
- [ ] Roster tab lists adventurers (color-coded status); Adventurer Detail
      overlay opens with stats/slots/lineage/actions.
- [ ] Loop still runs: build team from real adventurers → send → collect
      (resolve still stubbed, but team power computed from real stats).
- [ ] Nothing from the "Do NOT build" list is present.
- [ ] EditMode tests added for: status transition validity, stat derivation,
      roster cap, recruit gold spend.

## Reporting

Report: what was built, any public API added/changed per manager, every seed
value you set (§11 — flag feel-decisions vs. frame-fills), anything ambiguous
or worth revisiting, and confirm the loop still runs. Note that you cannot
build/run from the sandbox — list exactly what Jerry should verify in Unity.
Do NOT change any locked decision (D1–D7); flag instead.
