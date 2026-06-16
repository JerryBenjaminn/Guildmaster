# Task 03 — Combat-Resolve + Expedition Timing (D8)

> Builds the real auto-resolve math and lands D8 onto the verified Task 01/02
> base. Read CLAUDE.md (project root) first — it is the law. Comply throughout,
> especially §3 (technical rules), §4 (locked decisions D1–D8), §11 (decision
> authority). Domain refs: docs/GEAR_TALENT_SPEC.md, docs/UI_SPEC.md.

---

## Why this is task 03

Bootstrap (01) wired a stub resolve; roster (02) gives real adventurers with
real stats. This task makes the **outcome honest and complete at the system
level** — proper outcome bands, rewards, injuries, death-save — and implements
**D8 (Speed reduces expedition time, capped)**. It is deliberately **headless**:
no new tabs or dungeons. The loop stays exercisable via the editor-only debug
hook and the EditMode tests; the player-facing Quests tab, Expedition Prep
overlay, and the 6 MVP dungeons are **Task 04**.

Scope was agreed with Jerry (system-first; defer Infirmary/Priest death-save
mods; implement real injury-recovery timers).

## Scope — build fully

### 1. Outcome-band semantics (refines the Task 01 stub)

Bands split into **victory** and **defeat** so "Success vs CloseCall" is a real,
felt distinction (Jerry's guard: a clean win must not feel like a costly one).

- **Victory** (`roll < successChance`), by how comfortably it passed:
  - **Flawless** — passed by a wide margin. No injury. Bonus rewards.
  - **Success** — solid win. Full rewards. *At most an occasional Minor injury*
    (low chance) — usually none.
  - **CloseCall** — barely won. Reduced rewards. Minor injury likely.
- **Defeat** (`roll >= successChance`), by how badly it failed:
  - **Failure** — lost. No gold (small consolation XP). Major injuries.
  - **Catastrophe** — lost badly. No rewards. Death-save per member; survivors
    take Critical injuries.

> This supersedes the Task 01 band rule where CloseCall was a *failed* roll.
> CloseCall is now the marginal **victory** band.

Band thresholds live in BalanceConfig (margins, §11 seeds below). Success chance
stays clamped 5–95% (never 100%) — unchanged frame.

### 2. Band → rewards

Gold and XP scale by a per-band multiplier from BalanceConfig (no hardcoded
numbers). Gold flows to the at-risk pool (D4) via GuildManager on collect; XP
flows through `AdventurerManager.AddXp` (level-ups) — both already wired.
**Item/loot drops remain deferred to task 05** — gold + XP only this task (the
`lootSeed` keeps being stored for later).

### 3. Band → injury severity (approved mapping)

| Band | Result |
|------|--------|
| Flawless | none |
| Success | occasional **Minor** (low chance) — usually none |
| CloseCall | **Minor** |
| Failure | **Major** |
| Catastrophe | death-save; survivors **Critical** |

Per-band injury chances are BalanceConfig seeds. Severity per band is a rule
(fixed mapping), driven by the resolve.

### 4. Death-save (honest base; mods deferred)

On Catastrophe, each member rolls `baseDeathSaveChance`; failure → `DeadPending`
(the legacy/class-choice moment, never auto-resolved — UI_SPEC §4.3). **Infirmary
facility + Priest presence modifiers are left as a zero-sum hook** (a single
modifier input that is 0 now), wired when facilities/abilities get their task.
Permadeath stays honest (D3 — outcome locked at send-time).

### 5. D8 — Speed reduces expedition time (capped)

- `BalanceConfig`: `speedTimeReductionPerPoint` (factor, feel seed) and
  `speedTimeReductionCap` (frame, 0.25–0.30).
- At **send-time**: `effectiveDurationSeconds = round(tierDurationSeconds ×
  (1 − reduction))`, where `reduction = min(cap, ΣteamSpeed × factor)`.
  Store the reduced duration on the Expedition. Outcome is still rolled and
  locked at send (D3); wall-clock completion and the 12h offline cap (D2) are
  unchanged; **Speed never affects the combat outcome** (powerPerSpeed stays 0).
- Expose a pure helper (e.g. `ExpeditionTiming.EffectiveDuration(...)` /
  `SpeedReduction(...)`) so it is unit-testable and reusable by the future Prep
  UI's duration preview.
- The editor-only debug send must reflect the reduced duration (log it).
- **Retune per-class Speed** (in the class assets) so Rogue/Ranger are clearly
  fastest, Warrior/Priest slowest, others moderate — Speed is a desirable
  specialization, not free value (D8). Proposed values in the seed list.

### 6. Injury recovery timers (real-time, no offline cap — D2)

- On injury: `injuryHealAtTicksUtc = nowUtc + recoveryDuration(severity)`.
  Durations per severity are BalanceConfig seeds (short — see below).
- Recovery completes by **wall-clock even while the app is closed** (D2: injury
  recovery has NO offline cap). Auto-transition `Injured → Healthy` (via the
  state machine, clearing severity) when due.
- Process recovery **poll-light**: on game load and on UI Refresh, plus a coarse
  low-frequency check (e.g. a ~5 s repeating tick) — no per-frame work, no
  per-frame allocations (§3.6).
- Surface "recovers in mm:ss / hh:mm" on the **existing** Adventurer Detail
  overlay (and optionally the roster row). No new tab/overlay.

### 7. Tests (EditMode)

- D8: reduction scales with team Speed; **clamps at the cap**; zero Speed → no
  reduction; outcome unaffected by Speed.
- Band thresholds: roll/chance → correct band across victory & defeat ranges.
- Band → reward multipliers (gold + XP) applied correctly per band.
- Band → injury severity mapping (incl. Success rarely/never injures).
- Death-save: Catastrophe failure → DeadPending; success → Critical injury.
- Injury recovery: due recovery transitions to Healthy, including when the
  elapsed time spans an offline gap (no cap).

## Do NOT build (defer)

Quests tab · Expedition Prep overlay · the 6 MVP dungeons (all Task 04) ·
Infirmary/Priest death-save modifiers (hook only) · item/loot generation &
gear stat effects (task 05) · talents · visual auto-battler · audio/VFX ·
crafting · editor tools · prefab/visual polish.

## Constraints (from CLAUDE.md)

Data-driven, no hardcoded content/balance numbers (all tunables in BalanceConfig)
· single scene, no transitions · managers stay at 7, single responsibility ·
uGUI + TMP only · mobile-first (portrait, ≥44px, no per-frame alloc) · keep the
send→collect loop runnable at every step · small descriptive commits · do NOT
change a locked decision (D1–D8) — flag instead.

## Definition of done

- [ ] CombatResolver: victory/defeat band semantics, band-scaled gold+XP, band→
      severity mapping, honest base death-save (with a zero-sum survival-mod hook).
- [ ] D8 implemented: factor + cap in BalanceConfig, applied at send-time, capped;
      pure testable timing helper; debug send reflects reduced duration.
- [ ] Per-class Speed retuned (Rogue/Ranger high, Warrior/Priest low).
- [ ] Injury recovery timers: severity-scaled, no offline cap, auto-recover,
      shown on the existing Adventurer Detail overlay.
- [ ] EditMode tests above pass.
- [ ] Loop still runs (debug hook send→collect, now with real bands/rewards/timing).
- [ ] Nothing from "Do NOT build" is present. No locked decision changed.

## §11 SEED LIST (proposed values — Jerry to tune; flagged feel vs. frame)

> All live in `BalanceConfig` (source of truth). Marked **FRAME** (a bound Jerry
> set) or **SEED** (a feel value to tune).

**D8 timing**
- `speedTimeReductionPerPoint = 0.0025` — **SEED**. With the retuned stats a
  4-member Rogue/Ranger team lands ~15–20% early-game; the cap is only neared by
  high-level all-fast stacking (sacrificing power/diversity/survivability).
- `speedTimeReductionCap = 0.30` — **FRAME** (D8 says ~25–30%). Ceiling for the
  obsessive, not the normal state.

**Band reward multipliers** (index: Flawless, Success, CloseCall, Failure, Catastrophe) — **SEED**
- gold: `[1.25, 1.0, 0.6, 0.0, 0.0]`
- xp:   `[1.25, 1.0, 0.75, 0.25, 0.0]`

**Band thresholds** (replace the Task 01 stub margins) — **SEED**
- `flawlessMargin = 0.30` — victory margin (chance − roll) ≥ this ⇒ Flawless.
- `closeCallMargin = 0.10` — victory margin ≤ this ⇒ CloseCall (else Success).
- `catastropheMargin = 0.40` — defeat margin (roll − chance) ≥ this ⇒ Catastrophe (else Failure).

**Band → injury** — mapping is a **RULE** (approved); chances are **SEED**
- `successInjuryChance = 0.10` (Success: occasional Minor, else none)
- `closeCallInjuryChance = 0.75` (CloseCall: Minor)
- `failureInjuryChance = 1.0` (Failure: Major)
- Catastrophe: death-save; survivors always Critical.

**Death-save** — `baseDeathSaveChance = 0.5` (**SEED**, already present); survival
modifier input defaults `0` (zero-sum hook for Infirmary/Priest, deferred).

**Injury recovery durations** — **SEED** (CANONICAL — supersedes the older
2 h / 6 h / 12 h balance-table values; update any conflicting reference)
- Minor = **30 min**, Major = **2 h**, Critical = **6 h**.

**Retuned per-class Speed** — **SEED** (base @ L1 / growth per level)
| Class | Speed L1 | /level |
|-------|----------|--------|
| Rogue | 16 | +2 |
| Ranger | 14 | +2 |
| Berserker | 10 | +1 |
| Adventurer | 8 | +1 |
| Mage | 8 | +1 |
| Priest | 6 | +1 |
| Warrior | 5 | +1 |

> Other class stats stay as the Task 02 archetype seeds; they get balanced
> against real dungeon difficulty in Task 04. Only Speed is retuned here for D8.

## Reporting (when built)

Report: what was built; public API added/changed per manager/system; every seed
value actually set (flag feel vs. frame); confirm the loop still runs and Speed
does NOT affect outcomes; anything ambiguous or worth revisiting. Note you can't
build/run from the sandbox — list exactly what Jerry verifies in Unity. Do NOT
change any locked decision (D1–D8); flag instead.

---

*Gate: this doc → Jerry's OK → build → Jerry verifies in Unity.*
