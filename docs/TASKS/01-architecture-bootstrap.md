# Claude Code — Architecture Bootstrap Prompt (Guildmaster)

> Paste this as the first big task for the coder agent, once the repo and
> empty Unity 6 2D (URP) project exist and CLAUDE.md + /docs are committed.

---

## Context

Read **CLAUDE.md** (project root) in full before doing anything — it is the
law for this project. Also read **/docs/UI_SPEC.md** and
**/docs/GEAR_TALENT_SPEC.md** for domain detail. Everything below must comply
with those documents, especially §3 (hard technical rules), §4 (locked
decisions D1–D7), and §11 (decision authority).

This is the **architecture bootstrap** task: stand up the skeleton of the whole
game — every core system as an empty-but-correct structure — without building
gameplay content yet. "Full skeleton, minimal flesh."

## Goal of this task

A Unity project that **compiles, runs, opens to a placeholder main screen, and
has every architectural pillar in place and wired together**, but with no real
content. After this task, adding features = filling in systems and creating
ScriptableObject assets, not restructuring.

## What to build

**1. Folder structure**
```
Assets/
  Scripts/
    Managers/      (singleton managers)
    Data/          (ScriptableObject type definitions)
    Systems/       (logic: expedition, combat-resolve, legacy, etc.)
    UI/            (uGUI controllers/panels)
    Save/          (serialization)
  ScriptableObjects/  (SO asset instances live here)
  Scenes/             (MainGame.unity)
  UI/                 (prefabs, sprites later)
```

**2. Single scene**
- One `MainGame` scene (per D-rule: no scene transitions, ever).
- A root Canvas (uGUI + TextMeshPro) with a placeholder for the bottom tab bar
  and a content area, matching UI_SPEC.md's navigation model (5 tabs + top
  status strip + overlay layer). Tabs can be empty placeholders for now.

**3. Manager singletons** (DontDestroyOnLoad, one responsibility each, empty but
working skeletons with clear public APIs):
`GameManager`, `AdventurerManager`, `ExpeditionManager`, `LegacyManager`,
`GuildManager`, `UIManager`, `AudioManager`.
- `AudioManager` exposes a callable `Play(id)` hook but plays nothing yet.

**4. ScriptableObject type definitions** (the data-driven backbone — define the
TYPES now, create few/no instances). At minimum:
`AdventurerClassData`, `ItemData`, `AbilityData`, `DungeonData`, `EnemyData`,
`FacilityData`, `TalentData`, and a central `BalanceConfig` SO holding the
tunable constants from CLAUDE.md §7 (offline cap, success clamps, item-inherit
scaling 0.3, bloodline cap, expedition tier table, etc.). **No balance numbers
hardcoded in .cs files — all live in BalanceConfig or the relevant SO.**

**5. Save system** (three JSON files per CLAUDE.md §3.5):
`persistent.json`, `legacy.json`, `current.json`. Implement save/load with a
`SaveManager` (or fold into GameManager) that survives app kill. Local save
mandatory; cloud later. Test that it round-trips.

**6. Core loop skeleton (wiring, not content)**
- `ExpeditionManager` can register a timed expedition that progresses in
  **real time** and accrues **offline** (12h cap), with **outcome locked at
  send-time** (D3) — stored hidden, revealed when timer completes. Implement
  the mechanism even though there's no real combat content yet (a stub
  resolve is fine, but the lock-at-send and offline-accrual mechanics must be
  real and correct).
- `LegacyManager` has the inheritance entry point (gold at-risk pool D4, item
  scaling D7, bloodline tracking) as working methods, even if invoked on stub
  data.

## What NOT to build (defer — see CLAUDE.md §9)

No real dungeons/classes/items/abilities content beyond 1–2 throwaway stubs for
testing wiring. No visual auto-battler. No crafting UIs. No monetization. No
audio/VFX/animation content. No editor tools. No hybrid/legendary class content
(the class *system* must handle them, but author no instances yet).

## Constraints to respect (from CLAUDE.md)

- uGUI + TextMeshPro only (not UI Toolkit). 2D project, URP 2D renderer.
- Data-driven everything; no hardcoded content or balance numbers.
- Mobile-first: portrait, 30+ FPS target, touch targets ≥44px, no per-frame
  allocations in the skeleton.
- Manager singletons, single responsibility, clear public APIs.
- Commit in small, descriptive increments as you go.

## Definition of done for this task

- [ ] Project compiles, runs, opens to MainGame with a placeholder UI shell
      (top strip + 5 tab placeholders + overlay layer).
- [ ] All 7 managers exist as singletons with documented public APIs.
- [ ] All listed ScriptableObject types defined; `BalanceConfig` holds the §7
      constants (no hardcoded numbers in logic).
- [ ] Save system round-trips across an app restart (demonstrate/test).
- [ ] Expedition mechanic proven: can send a stub expedition, it locks outcome
      at send-time, accrues in real time, respects the 12h offline cap, and
      reveals the (stubbed) result on completion.
- [ ] Nothing from the "what NOT to build" list is present.

## Reporting

When done, report: what was built, the public API of each manager, where the
BalanceConfig lives and what it contains, any decisions you made that touch
CLAUDE.md §11 (flag anything you set a seed value for), and anything in the
specs that was ambiguous or that you'd recommend revisiting. Do NOT change any
locked decision (D1–D7) — if one seems wrong, flag it instead.
