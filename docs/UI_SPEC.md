# UI_SPEC.md — Guildmaster

> **Scope:** Information architecture only — structure, navigation, and what
> information lives where. **NOT** pixel-level visual design (colors, fonts,
> exact sizes come later, once structure works on-device).
> Referenced by CLAUDE.md. Mobile-first, portrait-primary.

---

## 0. GUIDING PRINCIPLES

1. **The UI is the game.** Player sees no combat/dungeons — only information
   and decisions. Clarity of information = quality of game.
2. **Fast core loop.** The 60–90s burst (collect → handle → re-send) must be
   reachable in the fewest taps. Optimize for the most frequent action.
3. **Thumb-friendly.** Portrait, one-handed. Primary navigation lives at the
   bottom (thumb rest zone). Hard-to-reach top = display, not action.
4. **"Where I am" vs. "what I'm doing."** Main views = places (bottom tabs).
   Tasks = overlays on top of a place (modals). Never confuse the two.
5. **Respect attention.** No flashing, no forced dwell. Information is glanceable.

---

## 1. NAVIGATION MODEL

**Bottom tab bar (persistent) + overlay/modal for secondary screens.**

- 5 primary tabs, always visible at the bottom, one tap each, thumb-reachable.
- Secondary screens (expedition prep, adventurer detail, upgrade confirm) open
  as **overlays** on top of the current tab — they do NOT live in the tab bar.
- Overlays have a clear dismiss (back arrow / swipe-down / tap-outside) and
  return the player exactly where they were.
- A persistent **top status strip** (display-only) shows global state.

```
┌─────────────────────────────────────┐
│  TOP STATUS STRIP (display only)     │  ← guild name · rank · gold · alerts
├─────────────────────────────────────┤
│                                      │
│         ACTIVE TAB CONTENT           │  ← one of the 5 main views
│         (scrollable)                 │
│                                      │
│   [overlays open on top of here]     │
│                                      │
├─────────────────────────────────────┤
│ [Guild][Roster][Quests][Craft][Hall]│  ← bottom tabs (persistent)
└─────────────────────────────────────┘
```

---

## 2. TOP STATUS STRIP (persistent, display-only)

Always visible. No primary actions here (hard to reach). Shows:
- Guild name + current **Rank** (Iron…Legendary)
- **Gold** (live count)
- **Alert indicator** — badge when something needs attention (completed
  expedition, death pending legacy choice, injury healed). Tapping it jumps to
  the relevant view/overlay.

The alert badge is critical: it's how the player, on opening the app, instantly
knows "something happened while I was away."

---

## 3. THE 5 PRIMARY TABS

### TAB 1 — GUILD (home / landing)
**Purpose:** The "what happened + what now" dashboard. This is where the app
opens by default, because it serves the core 60–90s burst.

Contains:
- **Active Expeditions list** — each shows team, dungeon, timer countdown OR
  "✓ Complete — collect" state. This is the heart of the loop.
- **Collect action** — completed expeditions surface here first. One tap to
  collect resolves loot/XP/gold (and triggers injury/death handling).
- **Facility upgrades** — the guild facilities (Quest Board, Infirmary,
  Treasury, Training Ground) with current level + upgrade button.
- Quick glance: roster health summary (e.g. "4 healthy, 1 injured").

> Design note: opening the app should land here, show completed expeditions
> at the top, and make "collect → re-send" a two-tap flow.

### TAB 2 — ROSTER
**Purpose:** Manage the adventurers themselves.

Contains:
- Scrollable list of all adventurers: portrait, name, class, level, **status**
  (color-coded: healthy / injured / on-expedition / training / dead-pending).
- Tap an adventurer → **Adventurer Detail overlay** (see §4.1).
- Filter/sort (by status, class, level) — minimal in MVP.

### TAB 3 — QUESTS
**Purpose:** Choose where to send teams. The decision surface.

Contains:
- List/grid of available dungeons: name, theme icon, difficulty, recommended
  level, entry fee, reward preview, cooldown state.
- Locked dungeons show unlock requirement (rank-gated).
- Tap a dungeon → **Expedition Prep overlay** (see §4.2).

### TAB 4 — CRAFT (shop + crafting, merged for MVP)
**Purpose:** Spend gold/materials on gear and consumables.
> MVP note: Smithy + Alchemy full crafting is deferred (§9 CLAUDE.md). For MVP
> this tab is a **simple shop** (buy gear/potions). Architecture leaves room to
> expand into full crafting tabs later without restructuring navigation.

Contains:
- Purchasable items grid (gear, potions, materials): icon, name, key stat,
  price. Tap to buy.
- (Deferred) crafting sub-tabs for Smithy/Alchemy.

### TAB 5 — HALL (Hall of Fame)
**Purpose:** Where the dynasty is *felt*. The emotional/legacy surface.

Contains:
- List of all fallen/retired adventurers: name, class, rank reached, cause of
  death, generation.
- Tap → simple detail (their story/stats). 
- (Deferred) full family-tree visualization — MVP is a list, which is enough to
  carry the emotional weight.

---

## 4. KEY OVERLAYS (tasks, open on top of a tab)

### 4.1 — Adventurer Detail
Opened from Roster. Shows everything about one adventurer:
- Large portrait, name, class, generation, family name.
- Stats (HP, Mana, Attack, Defense, etc.) with tap-for-tooltip.
- **Equipment slots** (weapon, armor, 2 accessories) — tap to assign/swap.
- **Ability slots** — assign from known abilities.
- **Lineage panel** — parent/grandparent class, inherited abilities, accumulated
  bloodline bonuses. (This is where dynasty depth becomes visible.)
- Actions: [Assign to Team] · [Train] · [Retire] · [Rename]

### 4.2 — Expedition Prep (most important overlay)
Opened from Quests. This is where the key strategic decision happens.
- Selected dungeon summary (difficulty, theme, entry fee, rewards).
- **Team slots (1–4)** — drag/tap adventurers in from a roster strip.
- **Live success-chance estimate** — updates as team changes. (Surfaces the
  risk/reward tension; never shows 100%.)
- **Synergy indicator** — shows class-pair bonuses for the chosen team.
- Per-member consumable assignment (potions).
- Optional ritual/blessing (if Shrine built — deferred, leave hook).
- **Expedition duration choice** — Quick / Short / Standard / Long / Overnight
  (the idle-tier selector; see balance data).
- Actions: [Send Expedition] · [Cancel]

> Design note: success-chance + duration choice together ARE the core strategic
> decision of the game. This overlay deserves the most polish later.

### 4.3 — Legacy / Class Choice (the emotional peak)
Triggered automatically when an adventurer dies (on collecting a fatal
expedition). **This is the most important moment in the game — it always
interrupts and demands an active choice. Never auto-resolved.**
- **Death summary** — who died, how, rank reached, days survived, deeds.
- **Legacy breakdown** — gold inherited, items available (player picks which to
  carry), abilities passed down, bloodline bonus gained.
- **Class selection** — available classes shown with synergy rating vs. the
  inherited gear/abilities; locked hybrids show "need X more Y ancestor."
- **Inherited loadout preview** — heir's starting stats/gear before confirming.
- Action: [Confirm Heir]

> This overlay carries the entire core fantasy. Even in MVP it must feel
> weighty and clear — not a dialog box, a *moment*.

### 4.4 — Upgrade / Purchase Confirm
Lightweight modal for spending gold (facility upgrade, shop buy): shows cost,
benefit of next level, confirm/cancel. Prevents fat-finger spends.

---

## 5. OPENING-THE-APP FLOW (the most-run path — optimize this)

```
App opens → GUILD tab (default)
  → completed expeditions shown at top, alert badge active
  → tap Collect on a finished expedition
      → loot/XP/gold resolved inline
      → IF death occurred → Legacy/Class overlay (4.3) interrupts
  → freed adventurers now available
  → tap Quests → pick dungeon → Expedition Prep (4.2) → Send
  → close app
```
This entire path should be achievable in well under a minute once familiar.
Every extra tap on this path is a tax on the game's most frequent action.

---

## 6. MOBILE CONSTRAINTS (structural)

- **Portrait primary.** (Landscape optional, far later.)
- **Touch targets ≥ 44×44px**, spacing ≥ 8px.
- Primary actions in the **lower 2/3** of the screen (thumb zone). Top strip is
  display-only.
- Lists scroll vertically; avoid horizontal-swipe navigation (conflicts with
  list/row gestures).
- Overlays dismissible by back / swipe-down / tap-outside, always returning to
  prior position.
- Collapsible/secondary info behind tap-for-tooltip rather than always-on
  (reduce clutter on small screens).
- Target 30+ FPS mid-range Android; UI should not allocate per-frame.

---

## 7. WHAT'S DEFERRED (structure stays ready)

- Full Smithy/Alchemy crafting tabs (MVP = simple shop in Craft tab).
- Family-tree visualization (MVP = Hall list).
- Ritual/blessing prep in Expedition Prep (leave a hook, don't build).
- Landscape layout.
- Visual theme/skin system, animations, juice — all post-structure.
- Pixel-level visual spec — a separate future document, after on-device testing.

---

*Version 0.1 — information architecture. Visual spec is a later, separate doc.*
