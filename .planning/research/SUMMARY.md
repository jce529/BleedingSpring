# Project Research Summary

**Project:** Bleeding Spring (혈연) — UI System Milestone
**Domain:** Unity 6 UGUI Integration for 2D Hardcore Action Roguelike
**Researched:** 2026-03-27
**Confidence:** HIGH

## Executive Summary

Bleeding Spring is a 2D hardcore action roguelike built on Unity 6 (URP 2D) with a mature, SOLID-oriented codebase — player movement, dual-resource stats, skill system, enemy AI, and game state management are all implemented. The current milestone has a single job: make the game readable. Every gameplay decision the player must make (conserve HP, manage corruption, target the Sweet Spot, time skill cooldowns) is currently invisible because no UI exists. Building the UI system is not a polish task — it is the precondition for the game being playable at all.

The recommended approach is UGUI-only, event-driven, and structured around a Presenter-View pattern that mirrors what is already in the codebase. All required packages are already installed (UGUI 2.0.0, URP 17.3.0). No new dependencies are needed. The three-canvas architecture — Screen Space Overlay for player HUD and boss bar, World Space per-enemy for enemy bars — is the correct split because World Space is the only viable solution for bars that follow individual enemies, and there is no alternative in Unity 6 UI Toolkit. The enemy corruption bar with its Sweet Spot highlighted zone is the highest-complexity and highest-value item: it has no genre precedent, must be self-teaching to the player, and has specific structural constraints (the overlay must be a sibling of the fill, not its child) that must be decided before the first prefab is built.

The top risks are all architectural decisions that become expensive to reverse if made wrong in Phase 1: per-enemy Canvas components (draw call explosion at scale), event subscriptions without a Bind/Unbind pattern (memory leaks and MissingReferenceException on enemy death/purification), and the Sweet Spot overlay parented to the wrong RectTransform (zone drifts as corruption fill changes). All three can be fully prevented by establishing the correct patterns before writing a single bar script.

---

## Key Findings

### Recommended Stack

The stack decision is locked: UGUI 2.0.0 is installed and is the only viable option. UI Toolkit cannot render in World Space in Unity 6, which makes it unsuitable for per-enemy bars. UGUI's `Image.fillAmount` binds directly to the existing C# `Action` events on `PlayerWaterStats` and `EnemyStats` with trivial MonoBehaviour code — no intermediate bus, no framework overhead. TextMeshPro is bundled with Unity 6 (requires a one-time Essential Resources import) and should be used for all text elements; the legacy `Text` component is deprecated.

**Core technologies:**
- **UGUI 2.0.0** (already installed): all runtime UI — `Image.fillAmount` for bars, World Space Canvas per enemy, Screen Space Overlay for player HUD and boss bar
- **Unity URP 17.3.0** (already installed): render pipeline; World Space canvases draw correctly in URP 2D with `Canvas.worldCamera` set to the scene camera
- **TextMeshPro** (bundled with Unity 6): all text elements (tier labels, boss name, cooldown numbers if used); import Essential Resources once before use
- **Unity Coroutines** (built-in): drive skill cooldown fill animation; `SkillBase` cooldown is coroutine-driven internally

**Three-canvas layout:**

| Canvas | Mode | Purpose |
|--------|------|---------|
| `HUDCanvas` | Screen Space - Overlay, Sort Order 10 | Player HP bar, Corruption bar, Water Tier indicator, Skill cooldown slots |
| `EnemyCanvas` (per prefab) | World Space | Per-enemy HP bar + Corruption bar with Sweet Spot zone; child of enemy GameObject |
| `BossHUDCanvas` (panel in HUDCanvas) | Screen Space - Overlay, Sort Order 20 | Large boss HP bar anchored bottom-center; inactive until boss encounter |

See `.planning/research/STACK.md` for complete event binding reference, component patterns, and version compatibility table.

---

### Expected Features

Reference game analysis (Dead Cells, Hades, Hollow Knight, Enter the Gungeon) establishes clear genre expectations. The Sweet Spot highlighted range on the enemy corruption bar is a genuinely novel UI pattern — no reference game shows a target zone to stop within on a stat bar. This means the UI must be fully self-teaching; clarity of implementation is more important than visual polish.

**Must have (table stakes) — P1:**
- Player HP bar (맑은 물) — primary death axis; absence makes the game unreadable
- Player Corruption bar (오염도) with danger coloring approaching 100% — second death axis; distinct visual treatment required, not just another bar
- Water Tier indicator (0–3) — tier directly changes skill behavior; player must always know current state
- Skill cooldown display (3 slots) — Dead Cells established this as genre standard; strategic skill use is impossible without it
- Enemy World Space HP bar (appears on first hit) — confirms hits; Dead Cells uses this; genre expectation
- Enemy Corruption bar with Sweet Spot highlighted band — this IS the core mechanic; without it the purification/destruction loop is invisible
- Boss Screen Space HP bar — all four reference games have a full-width bottom-screen boss bar; absence feels unfinished
- Critical HP danger signal (screen edge vignette or pulse) — Dead Cells/Hades standard; supports reading danger state during fast combat

**Should have (competitive) — P2 (same milestone if time allows):**
- Sweet Spot purification result feedback (PURIFIED / DESTROYED popup over enemy on death) — reinforces the strategic loop immediately after a kill
- Corruption bar max-fill alarm (flashing or color change at 100%) — clarifies the dual death condition
- Skill icon art (Aseprite assets replace placeholder rectangles) — art pipeline validation

**Defer (Phase 2+):**
- Animated HP ghost/lag trail — pure polish; adds complexity without readability benefit at this stage
- Floating damage numbers — significant implementation work; conflicts visually with world-space bars
- Purification/destruction counter display — needs Phase 2 story branching system to be meaningful
- Minimap / room map — requires dungeon structure that does not exist yet

See `.planning/research/FEATURES.md` for full competitor feature matrix and dependency graph.

---

### Architecture Approach

The architecture follows a lightweight MVP pattern: Stats classes own events, Presenter MonoBehaviours subscribe to those events and call typed setters on View scripts, View scripts own UGUI component references and handle visual effects internally. No framework, no ScriptableObject event bus — the existing codebase already uses direct C# events consistently and the new UI layer must match that pattern. The only significant addition to the existing game scripts is a `CooldownRatio` property (or `OnCooldownProgress` event) on `SkillBase`, since `IsOnCooldown` (bool) is insufficient for filling a radial overlay.

**Major components:**

1. **`PlayerHUDPresenter`** — subscribes to `PlayerWaterStats` events; drives `WaterBarView`, `CorruptionBarView`, `WaterTierIndicator`; Inspector-wired reference to stats
2. **`EnemyUIPresenter`** — subscribes to `EnemyStats.OnDamaged` / `OnDeath`; drives `EnemyHpBarView`, `EnemyCorruptionBarView`, `SweetSpotOverlay`; lives on enemy root, uses `GetComponent<EnemyStats>()` in `Awake`
3. **`SweetSpotOverlay`** — stateless view; given `(normalizedMin, normalizedMax)`, sets `RectTransform.anchorMin/anchorMax` to cover the purification range; must be a child of the bar background, not the fill image
4. **`SkillCooldownView`** — radial fill overlay per skill slot; polls `ISkill.IsOnCooldown` in `Update` to detect cooldown start, then drives fill by tracking elapsed time; upgrade to event-driven once `SkillBase` is extended
5. **`BossBarPresenter`** — activated via `BindToBoss(EnemyStats)` on boss encounter; drives screen-space bar; inactive by default
6. **`GameOverUI` / `PauseUI`** — subscribe to `GameStateManager.OnGameStateChange`; show/hide panels; mirrors existing `InputHandler` subscription pattern

**Build order:** View scripts first (stateless, testable with placeholder values) → Presenters (wired once views are confirmed) → Boss bar last (requires scene trigger not yet built).

See `.planning/research/ARCHITECTURE.md` for full prefab hierarchy, data flow diagrams, component boundaries, and anti-pattern analysis.

---

### Critical Pitfalls

1. **Event subscription leak via Destroy/Purify paths** — `EnemyStats.Die()` calls `Destroy(gameObject)` immediately; `EnemyStats.Purify()` calls `enabled = false` (not Destroy), so `OnDestroy()` never fires on the Purify path. Subscribing in `Start()` and unsubscribing only in `OnDestroy()` guarantees reference retention on purification and `MissingReferenceException` on rapid death. **Prevention:** Implement a `Bind(EnemyStats)` / `Unbind()` API on every bar script; `OnDeath` handler always calls `Unbind()` as its first action before any visual transition.

2. **Per-enemy Canvas components explode draw calls** — one `Canvas` component per enemy prefab produces one draw call batch per enemy. Unity cannot batch across Canvas boundaries. At 20+ enemies this silently tanks performance and cannot be fixed without refactoring every enemy prefab. **Prevention:** Decide the canvas architecture before the first bar prefab is built. Per-enemy Canvas is acceptable for a Phase 1 prototype with a known enemy count ceiling; a shared World Space Canvas with bar followers scales better and should be used if 10+ simultaneous enemies are expected.

3. **Sweet Spot overlay parented to fill image instead of bar background** — `Image.fillAmount` does not resize the RectTransform; the fill image's rect is always full width. A child overlay on the fill image will appear to cover the correct fraction at 100% corruption but will stay anchored to the fill, making the zone visually drift as corruption drops. **Prevention:** Parent `SweetSpotOverlay` to the bar background RectTransform, set `anchorMin.x = effectiveMin`, `anchorMax.x = effectiveMax`, `offsetMin = offsetMax = Vector2.zero`. This is a prefab layout decision; reverting it after enemy prefabs are distributed is expensive.

4. **Canvas dirty-marking from Update() lerp** — driving `Image.fillAmount` every frame (even with no change) triggers a full Canvas rebuild. With smooth lerp animations running on all visible enemy bars this causes `Canvas.BuildBatch` to spike every frame. **Prevention:** Use event-driven updates (write `fillAmount` only inside event callbacks); for any smooth animation use a self-terminating coroutine, not a continuous `Update()` loop.

5. **Boss bar on Screen Space - Overlay bypasses URP post-processing** — Overlay canvases render outside the URP camera stack; any bloom, color grading, or vignette applied via a Volume will not affect the boss bar. **Prevention:** Use Screen Space - Camera mode for the boss bar canvas and assign `Canvas.worldCamera = Camera.main` in `Awake()`. The player HUD is acceptable as Overlay (since no post-processing is typically applied to the HUD itself).

See `.planning/research/PITFALLS.md` for full pitfall catalogue, performance traps, UX pitfalls, and the "Looks Done But Isn't" verification checklist.

---

## Implications for Roadmap

This milestone covers a single phase: the UI system. Based on the dependency analysis in FEATURES.md and the build order in ARCHITECTURE.md, the phase decomposes naturally into three sequential sub-phases with a clear critical path.

### Phase 1-A: Foundations and Player HUD

**Rationale:** View components have no game-script dependencies and can be built and tested with placeholder values before any Presenter exists. Player HUD is the lowest-complexity wiring (events already exist, all signatures verified) and validates the Presenter-View pattern before applying it to the more complex enemy bars.

**Delivers:** Working player HUD — HP bar, Corruption bar with danger coloring, Water Tier indicator, Skill cooldown slots. EventSystem and CanvasScaler configured. TMP Essential Resources imported.

**Addresses features:** Player HP bar, Player Corruption bar, Water Tier indicator, Skill cooldown display (P1 features with lowest risk).

**Avoids pitfalls:** Establishes `[SerializeField]` reference wiring pattern (no `FindObjectOfType`); confirms `CanvasScaler` Scale With Screen Size at 1920x1080 before any layout work.

**Required codebase change:** Add `CooldownRatio` property (or `OnCooldownProgress` event) to `SkillBase` and extend `ISkill` interface.

---

### Phase 1-B: Enemy World Space UI and Sweet Spot Visualization

**Rationale:** This is the highest-value and highest-risk deliverable of the milestone. The Sweet Spot visualization is the core mechanic's only communication channel to the player. It has no genre precedent to copy, requires a specific prefab structure (overlay as sibling of fill, not child), and the architecture decisions made here (per-enemy Canvas vs shared Canvas, Bind/Unbind pattern) are expensive to reverse once enemy prefabs are distributed.

**Delivers:** Working World Space bars on all enemy prefabs — HP bar, Corruption bar with correctly positioned Sweet Spot zone, hide/show on hit/death/purification. Bind/Unbind pattern established and tested.

**Addresses features:** Enemy World Space HP bar, Enemy Corruption bar with Sweet Spot band (highest-priority differentiators).

**Avoids pitfalls:** Bind/Unbind pattern prevents event leak on Destroy and Purify paths; Sweet Spot overlay parent hierarchy established correctly before prefab distribution; canvas architecture decision made before scale forces a refactor.

**Required verification:** Confirm `EnemyStats` exposes `basePurificationMin`, `basePurificationMax`, `bonusPurificationMargin` as accessible properties (not just internal fields). Confirm `OnDeath` fires on both the Die() and Purify() paths (verified in PITFALLS.md from EnemyStats.cs line 154 — yes it does).

---

### Phase 1-C: Boss Bar and Polish

**Rationale:** Boss bar is last because it requires a boss detection/room trigger that does not yet exist. A stub `BindToBoss(EnemyStats)` method allows Phase 1-C to be built and tested in isolation without the full room system. This phase also includes the P2 polish items if time allows.

**Delivers:** Working Screen Space boss HP bar (Screen Space - Camera mode, not Overlay), activated via `BossBarPresenter.BindToBoss()`. Optional: Sweet Spot purification result feedback popup, Corruption bar max-fill alarm.

**Addresses features:** Boss Screen Space HP bar (P1); Sweet Spot purification result feedback, Corruption bar max-fill alarm (P2).

**Avoids pitfalls:** Boss canvas in Screen Space - Camera mode (not Overlay) to participate in URP camera stack and receive post-processing correctly.

---

### Phase Ordering Rationale

- **Views before Presenters:** Stateless view components can be validated in the Inspector with placeholder values before any game-script wiring. This prevents debugging two systems simultaneously when event subscriptions are added.
- **Player HUD before enemy bars:** The Presenter-View pattern is simpler to validate on the player stats path (one stats object, all events already exist and verified) before applying it to the more structurally complex enemy bar (world space, Bind/Unbind lifecycle, pooling concerns).
- **Enemy bars before boss bar:** Boss bar is a simplified version of the enemy bar pattern (same `EnemyStats` data source) and can reuse the established Presenter pattern; building enemy bars first means boss bar wiring is a solved problem.
- **Architectural decisions front-loaded:** All irreversible layout decisions (Sweet Spot overlay parent, Canvas architecture) are addressed in Phase 1-B before any prefab is distributed to multiple enemy types.

---

### Research Flags

Phases likely needing deeper research during planning:

- **Phase 1-B (Enemy World Space UI):** Sweet Spot overlay is a novel UI pattern with no reference implementation. The RectTransform anchor calculation and the Bind/Unbind lifecycle require careful specification before implementation. Recommend a short research-phase pass specifically on the Sweet Spot prefab layout and the purification path's event sequencing.
- **Phase 1-B (Canvas architecture):** The per-enemy Canvas vs shared Canvas decision depends on expected enemy count. If the game design targets 15+ simultaneous enemies, research the shared Canvas + follower approach before committing to per-enemy Canvas.

Phases with standard patterns (skip research-phase):

- **Phase 1-A (Player HUD):** All event signatures verified from source, all patterns (filled Image, Presenter-View) are well-established Unity patterns. No novel decisions required.
- **Phase 1-C (Boss Bar):** Same data binding pattern as enemy bars, applied to a screen-space canvas. Canvas mode selection (Screen Space - Camera) is documented. Standard implementation.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All package versions verified directly from `manifest.json` and `packages-lock.json`; World Space Canvas limitation of UI Toolkit is a documented Unity 6 engine constraint |
| Features | HIGH | Reference games are mature, stable titles; UI patterns from Dead Cells, Hades, Hollow Knight, Enter the Gungeon are well-documented through training data; Sweet Spot novelty finding is a derived conclusion, not a lookup |
| Architecture | HIGH | Grounded in direct source code audit of `EnemyStats.cs`, `PlayerWaterStats.cs`, `SkillBase.cs`, `GameStateManager.cs`; UGUI RectTransform anchor system has been stable across Unity 5–6 |
| Pitfalls | HIGH | Critical pitfalls (Destroy/Purify event leak, Canvas draw call batching, fillAmount dirty marking) derived from direct code audit of `EnemyStats.cs` and verified against Unity UGUI documented behavior; not inferred |

**Overall confidence:** HIGH

### Gaps to Address

- **`SkillBase.cooldownDuration` access:** The field is `protected` and `SkillCooldownView` needs it to drive the radial fill. Either expose it as a `public` property on `SkillBase`, or add `public float CooldownDuration { get; }` to `ISkill`. This is a required interface change before Phase 1-A skill cooldown implementation. Decide and implement before writing `SkillCooldownView`.

- **`EnemyStats` purification result event:** The Sweet Spot purification result feedback (PURIFIED / DESTROYED popup) requires that `EnemyStats` fires an event carrying a `PurificationResult` value on death. Verify whether this event exists; if not, it must be added before Phase 1-C polish work. The core Phase 1-B bars do not depend on this — it is a P2 item.

- **Boss encounter trigger mechanism:** `BossBarPresenter.BindToBoss(EnemyStats)` requires an external caller (a boss room trigger or encounter manager). This does not exist in the current codebase. For Phase 1-C, a direct Inspector assignment or a test script is sufficient; the real trigger is Phase 2 scope. Do not block Phase 1-C on this — stub it.

- **`bonusPurificationMargin` live update:** `SweetSpotOverlay.SetRange()` must be callable at runtime when `EnemyStats.WidenPurificationRange()` is called (future item system). Phase 1 only needs to call it once in `Start()`; but the API must be designed to support re-call. Exposing `SetRange(float, float)` as a public method on `SweetSpotOverlay` (which the ARCHITECTURE.md already specifies) is sufficient — the caller site will add the re-call when the item system is built.

---

## Sources

### Primary (HIGH confidence — direct code audit)
- `Assets/Player/PlayerWaterStats.cs` — verified event signatures: `OnWaterChanged(float, float)`, `OnCorruptionChanged(float, float)`, `OnWaterTierChanged(int)`
- `Assets/Enemy/EnemyStats.cs` — verified `OnDamaged`, `OnDeath` events; `basePurificationMin`, `basePurificationMax`, `bonusPurificationMargin`, `CorruptionRatio` properties; Purify path disables component (`enabled = false`)
- `Assets/Player/SkillBase.cs` — verified `IsOnCooldown` bool property; confirmed absence of cooldown progress event
- `Assets/Player/ISkill.cs` — verified interface contract
- `Packages/manifest.json` + `packages-lock.json` — verified UGUI 2.0.0, URP 17.3.0, Input System 1.19.0 versions and builtin source
- `.planning/codebase/CONCERNS.md` — verified known bug: `EnemyStats.TakeDamage` fires `OnDamaged` even when already dead

### Primary (HIGH confidence — stable Unity API)
- Unity UGUI 2.0.0: `Image.fillAmount`, `Image.fillMethod`, RectTransform anchor system, World Space Canvas, CanvasScaler Scale With Screen Size — behavior unchanged across Unity 5–6
- Unity URP 17.3.0: Screen Space - Camera vs Overlay canvas modes, camera stack participation, World Space Canvas depth sorting in URP 2D

### Secondary (HIGH confidence — training data, stable titles)
- Dead Cells (Motion Twin, 2018): world-space enemy bars, skill cooldown icon overlay, screen-edge danger pulse
- Hades (Supergiant Games, 2020): boss bar bottom-screen with phase tick marks, screen vignette at low HP
- Hollow Knight (Team Cherry, 2017): discrete mask HP icons, soul meter circular gauge, no enemy bars
- Enter the Gungeon (Dodge Roll, 2016): heart HP icons, boss bar top-screen, phase markers

---
*Research completed: 2026-03-27*
*Ready for roadmap: yes*
