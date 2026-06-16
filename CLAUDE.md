# CLAUDE.md — Guildmaster

> Persistent project context for Claude Code. Read this every session.
> This file is **law**. When in doubt, follow the rules here over any instinct.
> Keep it current: when an architectural decision changes, update this file.

---

## 1. WHAT THIS PROJECT IS

**Guildmaster** is a mobile-first idle/management RPG. The player runs an
adventurer's guild across **generations**: recruit heroes, send them on
timed expeditions, and when they die (permadeath), their descendants inherit
skills, items, and bloodline bonuses — eventually unlocking hybrid classes.

**One-line pitch:** "A generational dynasty idle-RPG where every death builds
a legacy."

**Platform priority:** Mobile-first (Android/Google Play). PC later. Every
design and technical decision favors mobile.

### Design pillars (every feature must serve at least one)
1. **Meaningful generations** — death is permanent and emotionally weighty,
   but the legacy system ensures progress continues.
2. **Strategic depth** — simple surface, deep optimization. Auto-resolve
   combat means no twitch skill; the skill is in planning and breeding.
3. **Respect player time** — no energy systems, no FOMO, no manipulative
   monetization. Play 90 seconds or 90 minutes; both are valid.

---

## 2. CORE LOOP (the spine — never break this)

```
Open app → see completed expeditions → collect loot/XP/gold →
handle injuries/deaths → (if death) LEGACY + CLASS CHOICE →
assign teams to new expeditions → close app → expeditions run OFFLINE → repeat
```

- The **idle/timer expedition** is the heart. Expeditions run in **real time**
  and progress **while the app is closed**.
- The **death → legacy → class-choice** moment is the emotional peak. It is
  **always an active player decision**, never automated.

---

## 3. HARD TECHNICAL RULES (do not violate)

These exist to prevent architectural drift across many sessions.

1. **Single scene.** One `MainGame` scene. UI is panels toggled on/off.
   **No scene transitions.** Ever.
2. **Data-driven everything.** All content (dungeons, classes, items,
   abilities, enemies, facilities) lives in **ScriptableObjects**. Adding
   content = creating a new SO asset, NOT writing code.
3. **No hardcoded content or balance numbers.** Every tunable value
   (timers, gold, success %, scaling factors) lives in a ScriptableObject
   or a central config SO, editable in the Inspector. If you're about to
   type a balance number into a `.cs` file, stop and put it in an SO.
4. **Manager singletons** (`DontDestroyOnLoad`): `GameManager`,
   `AdventurerManager`, `ExpeditionManager`, `LegacyManager`,
   `GuildManager`, `UIManager`, `AudioManager`. One responsibility each.
5. **Three save files (JSON):** `persistent.json` (guild progress, never
   wiped), `legacy.json` (lineage/inheritance), `current.json` (active
   adventurers, wipeable for fresh start).
6. **Mobile constraints:** 30+ FPS on mid-range Android. Touch targets
   ≥ 44×44px, spacing ≥ 8px. Portrait-primary. Minimal allocations in
   update loops. Object-pool anything spawned repeatedly.
7. **UI system = uGUI (Canvas-based).** NOT UI Toolkit. All UI is built with
   uGUI + TextMeshPro. Jerry knows uGUI; it's the fastest path to ship. This
   is a locked decision — do not introduce UI Toolkit.
8. **2D project, URP 2D renderer.** Create as a Unity 6 **2D** project (URP
   with the 2D renderer). The game is UI/sprite-driven. Do NOT create a 3D
   project. If 3D elements are ever needed later, add them targetedly (URP
   already handles both) — never pre-pay 3D defaults now.

---

## 4. LOCKED DESIGN DECISIONS

These were debated and decided. Do not reopen without explicit instruction.

| # | Decision | Rule |
|---|----------|------|
| D1 | **Real-time, no "End Day" button** | All timers (expeditions, injury recovery, training XP) progress in real time, including offline. There is NO turn-based day advancement. |
| D2 | **Offline accrual cap = 12h** | Expeditions/training accrue offline up to 12h, then hold. Completed expeditions wait for collection; they do NOT auto-restart. Injury recovery has NO cap. |
| D3 | **Outcome locked at send-time** | An expedition's result (success/injury/death) is rolled and stored the moment the team is sent, then hidden until the timer reveals it. Closing the app CANNOT change the outcome. No save-scumming. Permadeath stays honest. |
| D4 | **Gold "at-risk pool"** | On death, the player loses a % of *gold earned since the last death*, NOT the whole treasury. Long-term savings are protected; a single death still stings. |
| D5 | **Combat = auto-resolve (calculation)** | MVP uses GDD "Option A": sum team power, compare to dungeon, roll. NO visual battler in MVP (deferred). |
| D6 | **No monetization in MVP** | Ship a free, clean MVP. Validate retention (D1/D7 return) BEFORE adding any cosmetics/battle pass. Do not build IAP/ads yet. |
| D7 | **Inheritance anti-snowball** | Items scale to ~30% of original level on inherit. Bloodline bonuses cap at +20% per stat. Inherited abilities give utility/synergy, not raw power. Target: Gen 10 ≈ +40% stronger than Gen 1, never invincible. |

---

## 5. KEY SYSTEMS (build whole, even if content is minimal)

Principle: **full skeleton, minimal flesh.** Every system is built complete
and data-driven now; content is seeded minimally and expanded later via SOs.

- **Expedition/idle system** — real-time timers, offline accrual (12h cap),
  outcome locked at send-time (D3).
- **Auto-resolve combat** — power vs. difficulty, success-chance formula
  (clamped 5–95%, never 100%), outcome bands (Flawless→Catastrophe).
- **Injury + death + death-save** — permadeath on failed save. Infirmary
  and Priest presence modify survival.
- **Legacy/inheritance** — gold (at-risk pool, D4), items (scaled, D7),
  abilities (2–3 signature), bloodline bonuses (cumulative, capped),
  lineage tracking (dictionary of class→count).
- **Class system** — engine must handle base + hybrid + legendary tiers in
  CODE from day one. Hybrid unlocks are lineage-driven and permanent/guild-wide.
- **Gear system** — stats + special effects feeding the auto-resolve math.
  Built in layers (light/mid/deep). See **GEAR_TALENT_SPEC.md**.
- **Talent system** — bloodline-attached (persists & inherits), player-directed
  dynasty progression. Flat list in MVP, tree later. See **GEAR_TALENT_SPEC.md**.
- **Guild facilities** — generic upgrade engine; a few facilities as content.
- **Procedural loot generation** — items generated from templates, not
  handcrafted, so content scales freely.

> **Companion docs:** UI follows **UI_SPEC.md** (information architecture).
> Gear & talents follow **GEAR_TALENT_SPEC.md**. These are authoritative for
> their domains; this file stays the top-level law.

---

## 6. MVP CONTENT TARGETS (the flesh — minimal but complete-feeling)

| Content | MVP | Full (deferred) |
|---------|-----|-----------------|
| Dungeons | 6 (1 tutorial + 5 tiered) | 25–30 |
| Base classes | 7 | 7 |
| Hybrid classes | 2 (Battlemage, Paladin) | 10 |
| Legendary classes | 0 | 5 |
| Guild facilities | 4 (Quest Board, Infirmary, Treasury, Training Ground) | 10 |
| Items | ~40 (procedural) | 200+ |
| Gear special effects | ~20% of items (light layer) | affixes, sets, legendaries |
| Talents | flat list, 10–15 nodes | tree, branches, keystones |
| Abilities | ~20 | 100+ |
| Bosses | 2 | 10+ |

**MVP "done" = a genuinely fun 60–90 min vertical slice where:** first death
lands ~60 min, first hybrid (Battlemage) unlockable ~Gen 3–5, offline accrual
works across an app kill, and the save survives reinstall.

---

## 7. KEY BALANCE CONSTANTS (seed values — tune in Inspector, never hardcode)

> These live in a central `BalanceConfig` SO (or per-system SOs). Listed here
> for reference only. **Source of truth is the SO asset, not this file.**

- Expedition gold/hr **decreases** with duration (active play slightly
  favored, absent play not punished): ~240/hr (10min) → ~120/hr (8h).
- Offline cap: **12h**. Success chance clamp: **5%–95%**.
- Item inherit scaling factor: **0.3** (the main power-creep dial).
- Bloodline cap: **+20%** per stat type.
- Time-to-first-death target: **~60 min**. Time-to-first-hybrid: **~Gen 3–5**.

---

## 8. CODE STYLE & CONVENTIONS

- Unity 6, C#. Prefer composition over inheritance for behaviors.
- One class per file; file name == class name.
- ScriptableObject types end in `Data` (e.g., `DungeonData`, `ClassData`).
- Managers are singletons; access via `Manager.Instance`.
- No magic numbers in logic — pull from SO config.
- Keep methods short and single-purpose. Comment the *why*, not the *what*.
- Commit often, small, descriptive messages. `.gitignore` for Unity.

---

## 9. WHAT NOT TO BUILD YET (defer — architecture stays ready)

Visual auto-battler · Smithy/Alchemy crafting UIs · the 8 remaining hybrids ·
5 legendary classes · named companions + dialogue · family-tree visualization ·
monetization (IAP/ads) · localization · seasonal/event dungeons.

Build the *engine* to support these (data-driven), but ship none of them in MVP.

### Editor tooling: prefer "run & report" over building tools
Do **not** proactively build custom Unity editor tools. This project is
data-and-relationships, not spatial handcraft — the default ScriptableObject
Inspector is sufficient for creating content. When Jerry needs analysis
(economy curves, lineage simulation, drop-rate balance), **run the numbers
ad-hoc and report back in chat**, rather than building a persistent GUI.
Only build an editor tool if Jerry explicitly asks for one.

### Audio / VFX / animation: skeleton now, content later
`AudioManager` exists as a manager from day one (an empty, working skeleton),
but **do not author audio, particles, VFX, or animations in MVP scaffolding.**
Game feel (UI feedback sounds, collect/level-up juice, expedition-complete
cues) is designed against *working UI*, not into a vacuum. Build the hooks
(e.g. `AudioManager.Play(id)` callable), leave the content for a later polish
pass that Jerry directs.

---

## 10. WORKING RELATIONSHIP & AGENT ROLES

Jerry is the **designer/owner** and makes all design decisions. He is a
capable Unity developer and handles **design changes, balance feel, game feel,
and project direction** himself. He does NOT need content hand-fed; he needs
solid systems and honest technical partnership.

When a request conflicts with a rule here, **flag it and discuss** rather than
silently complying or silently refusing. Proactively surface architectural
risks and better solutions — don't just transcribe instructions.

### Two-agent setup (coder + reviewer)
This project uses two Claude Code agents with distinct roles:

- **Coder agent** — implements features per Jerry's direction and this file.
  Writes the code, follows the architecture, builds systems whole and
  data-driven.
- **Reviewer/test agent** — runs *after* the coder, in a **fresh, isolated
  context**. Its job is to read the new code cold — against this CLAUDE.md and
  the code itself only — verify it follows every rule (especially §3 and §4),
  test it, and report deviations. It does NOT know what the coder "intended";
  that independence is the whole point. It catches what the coder is blind to.

Design and coding are **sequential** (design feeds code), so they are NOT
parallelized. Coding and review ARE separated, by context, for quality.
Jerry reviews the reviewer's report and decides next steps.

Reviewer focus areas, in priority order:
1. **Rule violations** — hardcoded content/numbers, scene transitions,
   non-data-driven content, broken save architecture (§3, §4).
2. **Locked-decision drift** — anything contradicting D1–D7.
3. **Correctness & tests** — does it actually work? Edge cases?
4. **Mobile fitness** — allocations, frame cost, touch sizing.

*Future note:* store agent configs in `.claude/agents/` once roles stabilize,
so they are version-controlled and CLAUDE.md-aware.

---

## 11. DECISION AUTHORITY (what agents may decide vs. must bring to Jerry)

The core principle: **agents operate freely INSIDE the frames Jerry has set,
but may not change the frames themselves.** The "frames" are the locked
decisions (D1–D7), the numeric bounds in §7 (clamps, caps, targets), and the
intended *game feel*. Implementation within those frames is the agent's to own;
the frames are Jerry's.

### Agent MAY decide alone (implementation-level)
- Code structure: variable/method names, file organization, how an algorithm
  is written internally.
- Obvious bug fixes that don't change game rules or feel.
- Placeholder/seed values for tunables that are *meant* to be tuned later —
  **as long as** they live in a ScriptableObject, are flagged as a proposed
  seed, and are reported to Jerry (see "propose & report" below).

### Agent MUST bring to Jerry (design-level)
- Anything that changes **game feel or rules**: balance numbers, success
  formulas, inheritance logic, what-unlocks-what.
- Anything touching **locked decisions D1–D7** (even if it seems sensible).
- Adding a **new system or content type** that wasn't designed.
- **Any deviation from CLAUDE.md rules** — flag and discuss, never silently
  comply or silently refuse.

### Propose & report (the grey zone)
When a new tunable needs a starting value (e.g. a new dungeon's entry fee /
rewards), the agent **may set a seed value**, but must **clearly mark it as a
proposal and report what it set**, so Jerry can override. Never bury new
balance numbers silently.

### Reviewer/test agent — special authority over numbers
The test agent finds balance problems by playing the game through, so it needs
teeth — but only inside the frames. Split "changing balance" into two cases:

1. **Broken / out-of-frame values = FIX IT.** A success % that leaks past the
   95% clamp, a negative entry fee, a bloodline bonus that exceeds the +20% cap,
   a formula that overflows. These are *bugs that show up as numbers*, not feel
   decisions. The test agent may and SHOULD correct them — that is its job
   (enforce the frames Jerry set).
2. **Feel decisions = PROPOSE, don't change.** "First death lands at 30 min,
   feels punishing" or "inheritance feels too weak to be rewarding." These are
   genuine design judgments and belong to Jerry. The test agent may **propose
   and justify** with data ("tested, first death hit at 25 min, suggest raising
   early success %, here's the proposed change") but must NOT apply it without
   Jerry's approval.

Boundary example:
- §7 says success-chance clamp = 5–95%, time-to-first-death ≈ 60 min.
  Those are *frames*.
- Success % leaks above 95% in a dungeon → test agent **fixes it** (broke a
  frame, clear bug).
- First death lands at 30 min, not 60 → test agent **proposes to Jerry** (the
  frame says 60, but *how* you get there is a feel decision).

This keeps the test agent's teeth (it can clean its own mess and keep systems
healthy) while keeping the game's soul in Jerry's hands.

---

*Version 0.3 — living document. Update when decisions change.*
