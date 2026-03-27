# Feature Research

**Domain:** Hardcore 2D Action Roguelike — UI System (Health/Resource Bars, HUD)
**Researched:** 2026-03-27
**Confidence:** HIGH (reference games well-documented through training data; no external search available but these are canonical, stable titles)

---

## Reference Game Analysis

Before categorizing features, here is what the four reference games actually do — evidence-first.

### Dead Cells
- **Player HP:** Segmented bar (cells), top-left corner, Screen Space Overlay
- **Player resource:** No visible mana; skills show cooldown as icon overlay (fill drain + timer)
- **Enemy HP:** World-space thin bar above enemy head — appears only when enemy is damaged or in proximity
- **Boss HP:** Large bottom-screen bar with boss name, Screen Space, appears on boss room entry
- **Danger signal:** Screen edges pulse red at critical HP (~20%)
- **Secondary resource tracking:** None visible in HUD during normal play
- **Death condition:** HP = 0 only

### Hades
- **Player HP:** Bar bottom-left, discrete pip segments
- **Player resource:** Dash charges as pips; cast charges as pips; no traditional mana bar
- **Enemy HP:** No bars on regular enemies
- **Boss HP:** Full-width bottom-screen bar with boss name, phase transition tick-marks (thin vertical lines at phase thresholds), Screen Space
- **Danger signal:** Screen vignette darkens and reddens at low HP; controller rumble
- **Death condition:** HP = 0 only

### Hollow Knight
- **Player HP:** Discrete mask icons (not a bar), top-left
- **Player resource:** Soul meter as circular gauge adjacent to masks, fills from enemy hits
- **Enemy HP:** No bars on any enemy (including mini-bosses)
- **Boss HP:** Segmented bar at screen bottom, appears on boss room seal
- **Danger signal:** Screen flash + audio sting on hit; no persistent low-HP indicator
- **Death condition:** HP = 0 (all masks gone) only

### Enter the Gungeon
- **Player HP:** Heart icons, top-left; armor as shield icons
- **Enemy HP:** No bars on any enemy
- **Boss HP:** Bar at top of screen with boss name; phase markers as tick lines
- **Danger signal:** Screen shake on hits; no persistent low-HP indicator
- **Death condition:** HP = 0 only

### Cross-Genre Pattern: "Danger Zone" Visualization
None of the four reference games display a **target range** on a status bar. The closest mechanic is Hades/Gungeon boss bars using **threshold tick marks** (thin vertical lines) to indicate phase transitions — but these mark thresholds to cross, not a range to stop within.

This means **Bleeding Spring's Sweet Spot highlighted range on the enemy corruption bar is a genuinely novel UI pattern.** There is no direct genre precedent to copy. The design decision documented in PROJECT.md (color-band on the bar) is the right call — it is the most legible approach for a novel mechanic.

---

## Feature Landscape

### Table Stakes (Players Expect These)

Features a hardcore action roguelike player assumes exist. Missing these = product feels broken or unfinished.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Player HP bar (맑은 물) | Every action game has a visible HP indicator; Dead Cells, Hades all show it | LOW | `Image.fillAmount` driven by `OnWaterChanged` event already wired in `PlayerWaterStats` |
| Skill cooldown display | Dead Cells established icon-overlay cooldown as genre standard; players expect to know when skills are ready | MEDIUM | 3 skills, each needs icon + cooldown fill + optional timer text; must wire to `ISkill` cooldown state |
| Boss HP bar (Screen Space) | All four reference games have a dedicated full-screen-width boss bar; absence feels like a missing feature | MEDIUM | Needs boss detection trigger (enter boss room / flag on enemy); separate Canvas in Screen Space - Camera or Overlay |
| Critical HP danger signal | Genre standard: red vignette or screen-edge pulse at low HP informs player danger state without requiring them to look at a corner bar | LOW | Post-process vignette (URP 2D supports via Full Screen Pass Renderer Feature) or a simple screen-edge Image; threshold ~20-25% |
| Enemy world-space HP bar | Dead Cells uses these; players in action games want to know roughly how much health an enemy has | MEDIUM | World Space Canvas per enemy, follows enemy transform; show only when enemy is damaged or targeted |
| Death condition clarity (HP=0) | Players must understand that HP reaching zero is fatal; standard across all genre references | LOW | Visual feedback on death (screen flash, game over) already partially handled by `GameStateManager`; no special bar needed beyond HP bar reaching zero |

### Differentiators (Bleeding Spring's Competitive Advantage)

Features unique to this game's design. These are where the UI must do extra work because no reference game shows the player how to do this.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Enemy Corruption bar with Sweet Spot highlighted range | The core mechanic — player reads enemy corruption state and decides to kill now (purify) or wait; no genre precedent means the UI must be completely self-teaching | HIGH | Single `Image` with a custom colored overlay or shader segment for the sweet-spot band; driven by `EnemyStats.sweetSpotMin/Max` per enemy type; must be instantly readable at a glance during fast combat |
| Player Corruption bar (오염도) as a death-threat indicator | A second death axis (Corruption >= 100% = instant death) that no reference game has; players conditioned to only watch HP must also watch this | MEDIUM | Distinct visual treatment from HP bar — different color, different position, or an alarming visual change as it approaches 100%; the bar itself is table stakes but its **alarming behavior** is a differentiator |
| Corruption bar "danger zone" (player-side, approaching 100%) | Analogous to red screen edge for HP, but for the corruption axis; players must feel escalating tension as corruption climbs | LOW-MEDIUM | Color shift on the bar fill (green → yellow → red as corruption increases) or pulsing animation above 80%; more impactful than a simple static bar |
| Water Tier indicator (0-3 단계) | Directly affects skill damage and corruption output; no reference game has a tiered resource modifier visible in HUD | LOW | 4-state display (0/1/2/3); icon strip, number overlay, or segmented pip row; wired to O-key tier switch in existing `PlayerWaterStats` |
| Sweet Spot purification result feedback | After killing an enemy, player needs immediate confirmation that they hit the Sweet Spot (Purified vs Destroyed); this reinforces the strategic loop | MEDIUM | World-space popup text or icon stamp (PURIFIED / DESTROYED) over enemy position on death; driven by existing purification result event in `EnemyStats` |
| Dual death condition visualization | HP=0 OR Corruption>=100% — players need to understand two death axes exist; no reference game has this | LOW | Corruption bar reaching 100% should have a distinct "full/critical" visual (e.g., flashing red fill or screen-edge corruption tint) that mirrors HP danger signaling |

### Anti-Features (Do Not Build at This Stage)

Features that seem reasonable but create scope/complexity problems without delivering proportional value for this milestone.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Animated HP drain with ghost/lag trail (e.g., yellow trailing bar behind red fill) | Looks polished; used in some RPGs and Soulsborne games | Requires a secondary Image tracking "previous HP" with a lerp, adds visual complexity, and can obscure the Sweet Spot band on enemy corruption bars in world-space; adds implementation time without contributing to readability of the core mechanic | Simple immediate fill update; add trail animation in a polish pass after core UI is validated |
| Numerical HP/Corruption readout alongside bars | Players sometimes want exact numbers | In a fast-paced action game, numbers are not read in combat; they add visual noise near enemy bars where Sweet Spot visualization needs to be clean | Show numbers only on the player HUD tooltip or pause screen if requested later |
| Floating damage numbers | Common in ARPGs and roguelikes; often requested | Significant implementation work (pooled text objects, trajectory animation); not in scope for UI system milestone; can conflict visually with world-space enemy bars | Defer to gameplay feedback pass in Phase 2+ |
| Minimap or room map | Genre staple in dungeon-crawlers | Requires room/map system which is explicitly Phase 2+ in PROJECT.md; has zero dependencies that are built in Phase 1 | Out of scope; build when dungeon room structure exists |
| Status effect icons on enemy bars | Useful for games with many debuffs | No status effect system exists in the current codebase; would require building an entire status system to populate | No status system in scope; defer completely |
| Scrolling/animated boss HP bar reveal (cinematic) | Hades-style dramatic bar entrance animation | Adds implementation time disproportionate to milestone; core readability of the boss bar is the actual need | Static bar that appears instantly on boss encounter is sufficient; add entrance animation as a polish pass |

---

## Feature Dependencies

```
[Player HP Bar]
    └──requires──> [PlayerWaterStats.OnWaterChanged event] (already exists)

[Player Corruption Bar]
    └──requires──> [PlayerWaterStats.OnCorruptionChanged event] (already exists)
    └──enhances──> [Corruption Danger Zone Visual] (color shift/pulse overlay on same bar)

[Water Tier Indicator]
    └──requires──> [PlayerWaterStats tier value + tier change event] (exists, needs event confirmation)

[Skill Cooldown Display]
    └──requires──> [ISkill.IsOnCooldown / cooldown progress accessor] (interface may need extension)
    └──requires──> [Skill icon assets per skill] (art dependency)

[Enemy World-Space HP Bar]
    └──requires──> [EnemyStats.OnDamaged / current HP accessor] (already exists via IDamageable)
    └──requires──> [World Space Canvas attached to enemy prefab]

[Enemy Corruption Bar + Sweet Spot Band]
    └──requires──> [Enemy World-Space HP Bar Canvas] (same Canvas, additional bar below)
    └──requires──> [EnemyStats.sweetSpotMin, sweetSpotMax values accessible] (must verify exposed)
    └──requires──> [EnemyStats.OnCorruptionChanged event] (already exists)

[Sweet Spot Purification Result Feedback]
    └──requires──> [Enemy World-Space bars] (uses same world position)
    └──requires──> [EnemyStats purification result event on death] (must verify exists or add)

[Boss HP Bar (Screen Space)]
    └──requires──> [Boss detection mechanism — flag on EnemyStats or separate BossEnemy component]
    └──requires──> [Screen Space Canvas (separate from world-space enemy Canvas)]
    └──conflicts──> [Enemy World-Space HP Bar] (boss should NOT also have world-space bar; one or the other)

[Critical HP Danger Signal (vignette/screen edge)]
    └──requires──> [Player HP Bar] (same data source, threshold trigger)
    └──enhances──> [Player HP Bar] (reinforces low-HP state)

[Dual Death Condition Visualization]
    └──requires──> [Player HP Bar] + [Player Corruption Bar] (both must be present first)
```

### Dependency Notes

- **Enemy Corruption Bar requires Sweet Spot values exposed:** `EnemyStats` must expose `sweetSpotMin` and `sweetSpotMax` as accessible floats (not just used internally). Verify this before implementation; may need a getter or property addition.
- **Skill Cooldown Display may require ISkill extension:** The existing `ISkill` interface needs a `CooldownProgress` (0-1 float) or `RemainingCooldown` property for the UI to poll or subscribe to. This is a small interface change but touches all 3 skill implementations.
- **Boss bar conflicts with world-space bar:** A boss enemy should disable the world-space Canvas component when the Screen Space boss bar activates. Needs a coordinator (e.g., `BossUIController`) that listens for boss encounter start.
- **Sweet Spot Purification Feedback requires a death-result event:** `EnemyStats` must fire an event carrying `PurificationResult` (Purified/Destroyed) on death. Verify this exists; if not, add it before building the feedback UI.

---

## MVP Definition

### Launch With (this milestone — Phase 1 UI System)

Minimum required to make the core gameplay readable and playable.

- [ ] Player HP bar (맑은 물) — without this, the primary death condition is invisible
- [ ] Player Corruption bar (오염도) with danger coloring approaching 100% — second death axis must be legible
- [ ] Water Tier indicator — tier directly changes skill behavior; player must always know current tier
- [ ] Skill cooldown display — players cannot use skills strategically without knowing cooldown state
- [ ] Enemy world-space HP bar (appears on damage) — confirms hits and shows enemy health state
- [ ] Enemy corruption bar with Sweet Spot highlighted band — this IS the core mechanic; without it the game's differentiating loop is invisible to the player
- [ ] Boss Screen Space HP bar — genre expectation; boss fights without a dedicated bar feel unpolished
- [ ] Critical HP danger signal (screen vignette or edge pulse) — supports reading player danger state during fast combat

### Add After Validation (v1.x — polish pass)

- [ ] Sweet Spot purification result feedback (PURIFIED / DESTROYED popup) — confirms strategic decision; add after verifying core bars work correctly in combat
- [ ] Corruption bar max-fill alarm (flashing at 100%) — reinforces dual death condition; add after base bars validated
- [ ] Skill icon art — replace placeholder with actual Aseprite assets once art pipeline is confirmed working

### Future Consideration (Phase 2+)

- [ ] Purification/Destruction counter display — needs Phase 2 story branching system to make it meaningful
- [ ] Run summary screen — needs Phase 2 run structure
- [ ] Animated HP ghost/trail — pure polish; defer until core is stable
- [ ] Floating damage numbers — Phase 2+ gameplay feedback pass

---

## Feature Prioritization Matrix

| Feature | Player Value | Implementation Cost | Priority |
|---------|--------------|---------------------|----------|
| Enemy Corruption Bar + Sweet Spot Band | HIGH — core mechanic visibility | HIGH — novel UI pattern, no direct reference | P1 |
| Player HP Bar | HIGH — primary death axis | LOW — standard fill bar + existing event | P1 |
| Player Corruption Bar with danger coloring | HIGH — secondary death axis | LOW-MEDIUM — bar is simple; danger coloring adds small cost | P1 |
| Skill Cooldown Display | HIGH — tactical decision requires this | MEDIUM — needs ISkill interface extension | P1 |
| Enemy World-Space HP Bar | HIGH — combat feedback | MEDIUM — World Space Canvas per enemy prefab | P1 |
| Boss Screen Space HP Bar | MEDIUM-HIGH — genre expectation | MEDIUM — needs boss detection mechanism | P1 |
| Water Tier Indicator | MEDIUM — affects skill behavior | LOW — 4-state display, event already wired | P1 |
| Critical HP Danger Signal | MEDIUM — combat readability aid | LOW — URP vignette or screen-edge Image | P2 |
| Sweet Spot Purification Result Feedback | MEDIUM — reinforces strategic loop | MEDIUM — pooled world-space popup system | P2 |
| Corruption Bar Max-Fill Alarm | MEDIUM — dual death condition clarity | LOW — animation/color on existing bar | P2 |

**Priority key:**
- P1: Required for this milestone — gameplay is unreadable without it
- P2: Add in same milestone if time allows, or in first patch
- P3: Future milestone

---

## Competitor Feature Analysis

| Feature | Dead Cells | Hades | Hollow Knight | Enter the Gungeon | Bleeding Spring Approach |
|---------|------------|-------|---------------|-------------------|--------------------------|
| Player HP display | Segmented bar, top-left | Bar + pip segments, bottom-left | Discrete mask icons, top-left | Heart icons, top-left | Bar (맑은 물), Screen Space — continuous fill is better for second-by-second combat skill use |
| Secondary resource | None visible (scrolls are pickups) | Dash/cast charge pips | Soul meter circular gauge | Armor shield icons | Corruption bar — distinct visual treatment; must not look like a "fill up to use" resource — it's a danger meter |
| Enemy HP bar | World-space, above head | None on regular enemies | None | None | World-space bar — follow Dead Cells; corruption mechanic requires players to watch enemy state closely |
| Boss HP bar | Bottom screen, full width | Bottom screen, full width, phase ticks | Bottom screen, segmented | Top screen | Bottom screen, full width — genre convention; top causes eye travel conflict with player HUD |
| Skill cooldown | Icon fill drain overlay | Separate ability icons with cooldown | None (no cooldowns) | None | Icon + cooldown fill; 3 skills need clear individual status |
| Danger zone on bar | None | Phase tick marks on boss bar (thresholds, not ranges) | None | Phase tick marks on boss bar | Color-band segment on enemy corruption bar — genre-first design; no reference exists, so clarity of implementation is critical |
| Critical HP signal | Screen edge red pulse | Screen vignette darkens | Screen flash on hit only | Screen shake | Screen edge vignette/pulse — follow Dead Cells/Hades standard |

---

## Sources

- Dead Cells (Motion Twin, 2018): UI analysis from training data — HIGH confidence
- Hades (Supergiant Games, 2020): UI analysis from training data — HIGH confidence
- Hollow Knight (Team Cherry, 2017): UI analysis from training data — HIGH confidence
- Enter the Gungeon (Dodge Roll, 2016): UI analysis from training data — HIGH confidence
- Unity UGUI documentation: `Image.fillAmount`, World Space Canvas, Screen Space - Overlay patterns — HIGH confidence (well-established Unity patterns)
- No external search was available during this research session; all findings are from training data (knowledge cutoff August 2025). Reference games are mature, stable titles — confidence in their UI patterns is HIGH.

---

*Feature research for: Bleeding Spring — UI System milestone (Phase 1)*
*Researched: 2026-03-27*
