# Pitfalls Research

**Domain:** Unity 6 UGUI — 2D Roguelike UI System (World Space enemy bars, event-driven HUD, Sweet Spot visualization, bar pooling, boss transitions)
**Researched:** 2026-03-27
**Confidence:** HIGH (Unity UGUI behavior is well-established; codebase-specific pitfalls derived from direct code audit of EnemyStats.cs, EnemyAI.cs, CONCERNS.md)

---

## Critical Pitfalls

### Pitfall 1: UI Script Subscribes in Start() but Enemy Is Destroyed via Destroy(gameObject) — Leak Path Guaranteed

**What goes wrong:**
`EnemyStats.Die()` calls `Destroy(gameObject)` immediately (line 171, EnemyStats.cs). If a World Space health bar script subscribes to `EnemyStats.OnDamaged` and `EnemyStats.OnDeath` in `Start()` or `OnEnable()`, the subscription remains on the destroyed object's delegate until the next GC cycle. If the bar is a child of the enemy GameObject, `OnDestroy()` fires correctly. But if the bar is instantiated separately (e.g. pooled bar attached via a manager), `OnDestroy()` on the bar may not fire before the enemy is destroyed, leaving the bar's callback in the delegate list. When the next `TakeDamage` fires on the same frame as `Destroy`, both `OnDamaged` and `OnDeath` fire (confirmed in CONCERNS.md "Known Bugs: EnemyStats.TakeDamage fires OnDamaged even when already dead") — a UI bar callback will then attempt to call `SetFillAmount` on a destroyed RectTransform, throwing `MissingReferenceException`.

**Why it happens:**
Developers make the bar a sibling/separate prefab for flexibility (so it can float above the enemy), then subscribe to the parent's events without ensuring unsubscription order is tightly controlled. Unity destroys child objects in undefined order relative to the parent when `Destroy(gameObject)` is called on the root.

**How to avoid:**
- Make the bar script always subscribe in `OnEnable()` and unsubscribe in `OnDisable()`, not `Start()`/`OnDestroy()`. This handles pooled objects correctly.
- Alternatively: subscribe in `Start()` only, unsubscribe in `OnDestroy()`, AND add a null-check guard: `if (stats == null || stats.gameObject == null) return;` at the top of every callback.
- The safest pattern for a pooled bar: expose a `void Bind(EnemyStats target)` / `void Unbind()` API on the bar script; the pool manager calls `Unbind()` before returning the bar to the pool.

**Warning signs:**
- `MissingReferenceException: The object of type 'RectTransform'` in the console on enemy death.
- `NullReferenceException` inside a UI callback after enemies die rapidly.
- Memory profiler showing delegate lists growing after waves of enemies.

**Phase to address:** Phase 1 (World Space Enemy UI) — establish the Bind/Unbind pattern before any bar is wired to events.

---

### Pitfall 2: World Space Canvas Per Enemy — Draw Call Explosion

**What goes wrong:**
The most common implementation creates a `Canvas` component (Render Mode: World Space) as a child of each enemy prefab. This is visually easy but produces one draw call batch per Canvas. With 20 enemies on screen, that is 20+ separate Canvas batches. Unity's Canvas batching only operates within a single Canvas; elements on different Canvases can never batch together. In a roguelike with many enemies, this silently tanks performance.

**Why it happens:**
Unity documentation and most tutorials show the "add a Canvas to the enemy prefab" approach because it is the simplest setup. The batching consequence is not mentioned until you read the Canvas optimization guide.

**How to avoid:**
Use a **single World Space Canvas** in the scene (placed at a fixed Z, e.g. Z = -1 to render above sprites but below the camera near plane). Instantiate/pool all enemy bars as children of this single Canvas. Each bar follows its enemy via `LateUpdate()` or a dedicated `EnemyBarFollower` component that sets `rectTransform.position = Camera.main.WorldToScreenPoint(enemy.position) + offset`. This keeps all enemy bars in one batch.

**Warning signs:**
- Frame Debugger shows a separate "Canvas.RenderOverlays" or "Draw Dynamic" call for every enemy.
- Profiler shows `Canvas.SendWillRenderCanvases` scaling linearly with enemy count.

**Phase to address:** Phase 1 — architecture decision must be made before the first bar prefab is built. Retrofit is expensive.

---

### Pitfall 3: Dirty-Marking the Canvas Every Frame via Script-Driven Fill Updates

**What goes wrong:**
Setting `image.fillAmount = value` on a UGUI Image every `Update()` frame — even if the value has not changed — marks the Canvas dirty and triggers a full Canvas rebuild (mesh regeneration, batching). For a World Space enemy bar updated from `OnDamaged` events, this only fires on hits, which is fine. But if a developer adds a smooth lerp animation to the bar and drives it from `Update()` (e.g. `currentFill = Mathf.Lerp(currentFill, targetFill, t * Time.deltaTime)`), the Canvas rebuilds every frame for every visible enemy bar.

**Why it happens:**
Smooth health bar animations look better than snapping. The lerp approach is universally demonstrated in tutorials. The Canvas rebuild cost is invisible until many bars run simultaneously.

**How to avoid:**
- Only update `fillAmount` when the value actually changes: cache `lastFill` and skip the assignment if `Mathf.Approximately(newFill, lastFill)`.
- For smooth animations: use `DOTween` or a coroutine that runs only for the duration of the animation and stops when the target is reached, rather than polling in `Update()`.
- If you do use `Update()` lerp: check `if (Mathf.Abs(currentFill - targetFill) < 0.001f) { currentFill = targetFill; enabled = false; }` to self-disable the component once settled.

**Warning signs:**
- Profiler shows `Canvas.BuildBatch` spiking proportionally to enemy count every frame, not just on hit frames.
- `WillRenderCanvases` consistently high even in idle scenes with many enemies.

**Phase to address:** Phase 1 — establish the event-driven (not polling) update pattern from the start.

---

### Pitfall 4: Sweet Spot Zone RectTransform Anchors Calculated in Wrong Space

**What goes wrong:**
The Sweet Spot zone (e.g. 30%–70% of the Corruption bar, defined by `basePurificationMin` and `basePurificationMax` in `EnemyStats`) must be rendered as a colored overlay on the bar. A common approach: place a child `Image` RectTransform over the bar and set its `anchorMin.x` and `anchorMax.x` to the Sweet Spot percentages. This works only if the bar's RectTransform width is driven by anchors (stretch), not by a fixed pixel width. If the bar uses `Image.fillAmount` for the corruption fill, the underlying RectTransform is always full width — the Sweet Spot overlay must be anchored relative to the full bar width, not the filled portion. Confusing "fill space" with "bar space" causes the zone to shift as corruption changes.

**Why it happens:**
The Corruption bar shrinks visually (via `fillAmount`) but its RectTransform does not shrink. Developers place the Sweet Spot overlay as a child of the fill image rather than as a sibling anchored to the bar background, causing the overlay to scale with the fill.

**How to avoid:**
- Make the Sweet Spot overlay a child of the **bar background** (not the fill image).
- Set `anchorMin = new Vector2(sweetSpotMin, 0)` and `anchorMax = new Vector2(sweetSpotMax, 1)` on the overlay's RectTransform, where `sweetSpotMin` and `sweetSpotMax` are the normalized values from `EnemyStats.basePurificationMin` and `basePurificationMax` (adjusted for `bonusPurificationMargin`).
- Set `offsetMin = offsetMax = Vector2.zero` to make it pixel-perfect.
- The overlay stays fixed even as the fill bar shrinks, because it is anchored to the background, not the fill.

**Warning signs:**
- Sweet Spot zone visually tracks the edge of the corruption fill instead of staying fixed.
- Zone position shifts when `WidenPurificationRange()` is called mid-combat without a UI update.
- Zone appears correct at default corruption (100%) but drifts at low corruption values.

**Phase to address:** Phase 1 — this is a structural layout decision for the bar prefab. It cannot be easily patched after the prefab is built and distributed in enemy prefabs.

---

### Pitfall 5: Event Unsubscription Fails When EnemyStats Is Disabled (Not Destroyed) on Purify

**What goes wrong:**
`EnemyStats.Purify()` calls `enabled = false` (line 155, EnemyStats.cs) rather than destroying the object. This means `OnDestroy()` is **not called** on `EnemyStats` or its siblings. If the enemy UI bar subscribes to `EnemyStats.OnDamaged`/`OnDeath` and unsubscribes in `OnDestroy()`, the subscription lives indefinitely. The `PurifiedNPC` component is then added (line 158) and activates, but the bar script — still subscribed to a disabled `EnemyStats` — holds a live reference to events on a component that is now disabled and will never fire again. This is a silent reference retention (not a crash), but it holds the bar's memory allocation and prevents GC of any closures captured in the delegate.

**Why it happens:**
The Purify path disables the component rather than destroying the GameObject, which is correct for the gameplay (the object becomes an NPC). But UI authors expect either `OnDestroy` or a clear "this enemy is done" signal. The existing `OnDeath` event does fire on Purify, so if the bar subscribes to `OnDeath` for unsubscription, this is avoidable — but only if the bar's `OnDeath` handler explicitly calls its own `Unbind()`.

**How to avoid:**
- In the bar's `OnDeath` callback handler, always call `Unbind()` (which unsubscribes from all `EnemyStats` events) as the first action before any visual transition.
- Do not rely solely on `OnDestroy()` for unsubscription when the Purify path keeps the GameObject alive.
- Pattern: `void HandleDeath() { Unbind(); StartCoroutine(DeathFadeOut()); }`

**Warning signs:**
- After purifying an enemy, its bar lingers in memory (visible in the Memory Profiler as a live `EnemyBarController` instance).
- A purified enemy's UI bar does not hide after purification, because `OnDestroy` never fires.

**Phase to address:** Phase 1 — the `Bind/Unbind` pattern established for Pitfall 1 naturally covers this if `OnDeath` triggers `Unbind()`.

---

### Pitfall 6: Boss Bar Screen Space Canvas Conflict with World Space Enemy Bars

**What goes wrong:**
Adding a Screen Space — Overlay Canvas for the boss bar while enemy bars use a World Space Canvas causes z-sorting issues in Unity 6 URP 2D. URP 2D renders the World Space canvas in the camera's geometry pass; Overlay canvases always render on top. This is usually desired, but if particle effects or URP post-processing are applied, the Screen Space Overlay canvas bypasses all camera stacks and post-processing — meaning the boss bar will appear with no bloom/color grading that the rest of the game has. Additionally, switching from Screen Space — Overlay to Screen Space — Camera mode (which participates in the camera stack) requires assigning the Render Camera, which is often forgotten during scene setup, producing a blank boss bar.

**Why it happens:**
"Screen Space Overlay" is the Unity default and works immediately with no camera configuration. The post-processing exclusion issue only manifests once post-processing is added.

**How to avoid:**
- Use **Screen Space — Camera** (not Overlay) for the boss bar Canvas. Assign the main camera. Set Plane Distance to 1 (just in front of the camera).
- This participates in the URP camera stack and receives post-processing correctly.
- Alternatively, place the boss bar on the same World Space Canvas as enemy bars but at a fixed screen-relative position (calculated each frame from viewport coordinates). More complex but fully unified rendering.

**Warning signs:**
- Boss bar is visible but ignores any URP post-processing (bloom, color correction).
- Boss bar clips into 3D/2D geometry instead of rendering on top.
- Boss bar disappears entirely after changing `Camera.main` or adding a URP camera overlay.

**Phase to address:** Phase 1 (Boss UI setup) — camera mode must be set correctly when creating the Canvas. Changing it later requires re-testing all UI positioning.

---

### Pitfall 7: Enemy Bar Pooling — Bar Retains Stale EnemyStats Reference After Return to Pool

**What goes wrong:**
When an enemy dies and the bar is returned to a pool, the bar's cached `EnemyStats stats` field still references the destroyed (or disabled-and-repurposed) enemy. When the pool reuses the bar for a new enemy, if `Bind(newStats)` is not called before the bar becomes visible, the bar reads from the previous enemy's stats — displaying wrong HP/Corruption values for one frame, or worse, calling methods on a destroyed `MonoBehaviour` reference.

**Why it happens:**
Pooling frameworks often just call `gameObject.SetActive(true)` to reuse objects. Without an explicit reset step, old references persist. This is especially insidious because the bar "works" most of the time (the new `Bind` call arrives shortly after), but the one-frame stale read can cause a visible flicker or a `MissingReferenceException` if the old enemy's GameObject was destroyed.

**How to avoid:**
- Pool lifecycle: `Acquire()` → `bar.Bind(enemyStats)` → `bar.gameObject.SetActive(true)`. Never activate before binding.
- `Release()` → `bar.Unbind()` → `bar.gameObject.SetActive(false)`.
- In `Unbind()`: set `stats = null` explicitly. Add a null check at the top of all callbacks: `if (stats == null) return;`.

**Warning signs:**
- One-frame flicker of wrong health values when a new enemy spawns.
- `MissingReferenceException` on `stats.CurrentHp` access after rapid enemy spawning.
- Bar shows 0/0 HP for new enemies at the start of their life.

**Phase to address:** Phase 1 — the pool acquire/release contract must be defined when building the bar pooling system.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| One Canvas per enemy prefab | Zero setup cost, self-contained prefab | One draw call batch per enemy; 20 enemies = 20+ batches; cannot be fixed without refactoring all enemy prefabs | Never for production |
| `Update()` lerp on fill values | Smooth animation, trivial to write | Canvas rebuild every frame for every animated bar | Never when pooling many bars |
| Unsubscribe only in `OnDestroy()` | Matches Unity component lifecycle pattern | Fails for Purify path (`enabled = false`), fails for pooled bars returned before destroy | Only acceptable for non-pooled, non-purifiable enemies |
| Screen Space — Overlay for boss bar | Works immediately with no camera config | Bypasses URP post-processing; breaks if camera stack changes | Acceptable for a prototype with no post-processing |
| Hardcoded Sweet Spot values read once on `Start()` | Simple | Bar does not reflect `WidenPurificationRange()` upgrades during play | Never — roguelikes have upgrades mid-run |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| `EnemyStats` events + UI bar | Subscribe in `Start()`, unsubscribe in `OnDestroy()` only | Use `Bind(EnemyStats)` / `Unbind()` API; `OnDeath` triggers `Unbind()` before visual fade |
| URP 2D + World Space Canvas | Assign Sorting Layer "Default" to the Canvas | Assign the Canvas Sorting Layer to match or exceed the enemy sprite layer to prevent z-fighting |
| URP 2D + Screen Space Camera Canvas | Forget to assign Render Camera after creating Canvas | Always set `Canvas.worldCamera = Camera.main` in `Awake()`; add a validation check that logs an error if null |
| `EnemyStats.bonusPurificationMargin` + Sweet Spot overlay | Read `basePurificationMin/Max` once on bar spawn | Subscribe to a new `OnPurificationRangeChanged` event (or poll) so the Sweet Spot overlay updates when `WidenPurificationRange()` is called |
| `EnemyStats.Die()` calls `Destroy(gameObject)` immediately | Expect a grace frame for cleanup | Bar must handle `OnDeath` by unsubscribing immediately; do not assume the EnemyStats reference is valid after `OnDeath` fires |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Per-enemy Canvas components | Frame Debugger shows N draw call batches for N enemies | Single shared World Space Canvas, bars as pooled children | Noticeable at ~10 enemies; severe at ~30+ |
| `Update()` fill lerp on all visible bars | `Canvas.BuildBatch` appears every frame in Profiler | Event-driven updates + self-disabling coroutine for animation | 1 bar is fine; 20 bars causes consistent 1–3ms overhead |
| `Camera.main` lookup in bar `LateUpdate()` | GC allocation each frame (Camera.main uses `FindFirstObjectByType` internally until Unity 6.1) | Cache `Camera.main` reference in `Awake()` on the bar manager | Every bar doing this independently multiplies the cost |
| `OnGUI()` in `EnemyStats` left in non-editor builds | `OnGUI` is called every frame for every enemy even in builds (the `#if UNITY_EDITOR` guard wraps only the method body, but `OnGUI` itself is still registered) | Wrap the entire `OnGUI` method definition in `#if UNITY_EDITOR` | Any build with debug draw enabled |
| Instantiating bar prefabs at enemy spawn in a roguelike | GC spike on room entry when many enemies spawn simultaneously | Pre-warm bar pool at scene load; acquire from pool in `EnemyAI.Start()` | Noticeable with 8+ simultaneous spawns |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Sweet Spot zone is the same color as the corruption bar fill | Players cannot distinguish the target zone at a glance — the core mechanic becomes unreadable | Use a strongly contrasting color with alpha (e.g. bright gold or white with 60% opacity) that overlays cleanly over both low and high corruption fill colors |
| Sweet Spot zone has no edge markers | Zone boundaries are ambiguous; players misjudge the kill moment | Add 1–2px vertical lines at zone min and max boundaries (child Images with width ~2px, anchored at the edge positions) |
| Enemy bar always visible even at full health | Visual noise; bars appear before the player engages, cluttering the screen | Show bar only after first hit, or fade in on aggro state entry; `EnemyAI.ChangeState(Chase)` is a natural show trigger |
| Boss bar activates instantly (no transition) | Jarring visual pop that breaks immersion at a high-tension moment | Fade in the boss bar Canvas Group alpha over 0.5–1s using a coroutine or DOTween tween on boss encounter trigger |
| Bar stays at 0% fill for 1–2 frames after enemy death | Player sees a brief "dead but alive" visual artifact | On `OnDeath`, immediately set fill to 0 and hide the bar in the same callback, before the fade-out animation starts |

---

## "Looks Done But Isn't" Checklist

- [ ] **Enemy bar follows enemy:** Bar position updates in `LateUpdate()`, not `Update()`, to avoid one-frame lag behind physics-driven movement.
- [ ] **Sweet Spot zone updates:** Verify zone overlay recalculates when `WidenPurificationRange()` is called — not just at bar spawn. Test by calling `WidenPurificationRange(0.1f)` mid-combat.
- [ ] **Purify path hides bar:** After purification, bar disappears. `OnDeath` fires on Purify (confirmed in EnemyStats.cs line 154), so the bar hides only if it subscribes to `OnDeath`. Verify manually.
- [ ] **Pooled bar binding verified:** Acquire a bar, kill the enemy, return bar to pool, spawn a new enemy, verify bar shows new enemy's correct stats — not the previous enemy's.
- [ ] **Boss bar deactivates on boss death:** Boss `OnDeath` fires; verify the boss bar Canvas Group fades out and the Canvas is disabled (not just invisible) to stop it consuming batching resources.
- [ ] **No bar visible for purified NPCs:** After purification, `PurifiedNPC` is active on the same GameObject. The bar must not re-show when `PurifiedNPC.Activate()` is called — verify `EnemyStats.enabled = false` prevents any further event firing.
- [ ] **Bar hidden when not in camera view:** Bars outside camera frustum still consume Canvas batching if not culled. Use `CanvasRenderer.cull = true` or check distance to camera in the bar follower script.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Per-enemy Canvas architecture | HIGH | Remove Canvas component from all enemy prefabs; create shared World Space Canvas in scene; update all bar follower scripts to position relative to camera; re-test all enemy bars |
| Memory leak from unsubscribed events | MEDIUM | Audit all UI scripts for missing `OnDisable`/`Unbind` calls; add null guards to all callbacks; run Memory Profiler after 5 minutes of play to verify no growth |
| Sweet Spot overlay in wrong parent | MEDIUM | Reparent overlay RectTransform from fill Image to bar background Image; recalculate anchor values; regression test at corruption 0%, 50%, 100% |
| Boss bar Screen Space Overlay blocking post-processing | LOW | Change Canvas Render Mode from Overlay to Camera; assign `Camera.main`; re-test all UI positioning offsets |
| Stale bar reference after pooling | LOW | Add explicit `stats = null` in `Unbind()`; add null-check guards in all callbacks; re-test rapid enemy spawn/death cycles |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| UI event subscription leak (Bind/Unbind pattern) | Phase 1 — establish before first bar script | Memory Profiler: allocations do not grow after 30-enemy combat session |
| Per-enemy Canvas draw call explosion | Phase 1 — single shared Canvas architecture decision | Frame Debugger: enemy bar batches stay constant regardless of live enemy count |
| Canvas dirty every frame via Update() lerp | Phase 1 — event-driven update pattern | Profiler: `Canvas.BuildBatch` only spikes on hit frames, not every frame |
| Sweet Spot overlay parent hierarchy | Phase 1 — bar prefab layout decision | Manual test: zone stays fixed while corruption fill drains from 100% to 0% |
| Purify path skips OnDestroy unsubscription | Phase 1 — OnDeath triggers Unbind() | Manual test: purify an enemy; verify bar hides and no MissingReferenceException |
| Boss bar Canvas mode (URP post-processing) | Phase 1 — boss UI Canvas setup | QA: apply a URP Volume post-processing profile; verify boss bar renders with color grading |
| Pooled bar retains stale EnemyStats reference | Phase 1 — pool acquire/release contract | Automated or manual: spawn 10 enemies rapidly, kill all, spawn 10 more; verify no stale values or exceptions |
| `bonusPurificationMargin` Sweet Spot not live-updated | Phase 2+ (upgrades) — but bar must expose update API in Phase 1 | When upgrade system is added: call `WidenPurificationRange()`; verify all visible bars update immediately |

---

## Sources

- Direct code audit: `Assets/Enemy/EnemyStats.cs` — Purify path, event declarations, `Destroy(gameObject)` in `Die()` (2026-03-27)
- Direct code audit: `Assets/Enemy/EnemyAI.cs` — Subscribe in `Start()`, unsubscribe in `OnDestroy()` pattern (2026-03-27)
- `.planning/codebase/CONCERNS.md` — "EnemyStats.TakeDamage fires OnDamaged even when already dead" known bug; "EnemyStats.Purify() calls AddComponent at runtime" fragile area (2026-03-27)
- Unity UGUI documentation: Canvas batching behavior, Render Mode options, fillAmount dirty marking — HIGH confidence (stable behavior since Unity 5, confirmed in Unity 6 UGUI 2.0.0)
- Unity URP 2D documentation: Screen Space — Camera vs Overlay, camera stack participation — HIGH confidence
- Unity Canvas performance guide: "One Canvas per dynamic element" anti-pattern — HIGH confidence (well-documented in Unity manual)

---
*Pitfalls research for: Unity 6 UGUI — 2D Action Roguelike UI System (Bleeding Spring)*
*Researched: 2026-03-27*
