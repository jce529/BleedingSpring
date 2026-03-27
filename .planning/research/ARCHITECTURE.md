# Architecture Research

**Domain:** Unity 6 (2D URP) — UGUI System Integration into Existing Roguelike
**Researched:** 2026-03-27
**Confidence:** HIGH (grounded in existing codebase + Unity UGUI fundamentals that have been stable across Unity 5–6)

---

## System Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                     SCREEN SPACE — OVERLAY CANVAS                    │
│  ┌───────────────────┐        ┌──────────────────────────────────┐   │
│  │   Player HUD      │        │   Boss Bar (conditional active)  │   │
│  │  - WaterBar       │        │  - BossHpBar (full width)        │   │
│  │  - CorruptionBar  │        │  - BossNameLabel                 │   │
│  │  - WaterTierIcons │        └──────────────────────────────────┘   │
│  │  - SkillCooldowns │                                               │
│  └───────────────────┘                                               │
├──────────────────────────────────────────────────────────────────────┤
│                     WORLD SPACE CANVAS (per enemy prefab)            │
│  ┌──────────────────────────────────────────────────┐                │
│  │  EnemyUIRoot (follows enemy Transform)           │                │
│  │  - EnemyHpBar (Image fill)                       │                │
│  │  - EnemyCorruptionBar (Image fill)               │                │
│  │    └─ SweetSpotOverlay (RectTransform, fixed)    │                │
│  └──────────────────────────────────────────────────┘                │
├──────────────────────────────────────────────────────────────────────┤
│                     GAME LAYER (existing codebase)                   │
│  ┌───────────────┐  ┌───────────────┐  ┌──────────────────────────┐ │
│  │PlayerWaterStats│  │  EnemyStats  │  │    GameStateManager      │ │
│  │ OnWaterChanged │  │  OnDamaged   │  │    OnGameStateChange     │ │
│  │ OnCorruption.. │  │  OnDeath     │  │                          │ │
│  │ OnWaterTier..  │  │  (+ props)   │  │                          │ │
│  └───────────────┘  └───────────────┘  └──────────────────────────┘ │
│          ↑                  ↑                        ↑               │
│    PlayerHUDPresenter  EnemyUIPresenter        GameOverUI /          │
│    (subscribes)        (subscribes)            PauseUI               │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Component Responsibilities

| Component | Responsibility | Script Location |
|-----------|----------------|-----------------|
| `PlayerHUDPresenter` | Subscribe to `PlayerWaterStats` events, drive HUD Image fills and tier icons | `Assets/UI/Player/PlayerHUDPresenter.cs` |
| `WaterBarView` | Own the `Image` reference for the water (HP) fill bar | `Assets/UI/Player/WaterBarView.cs` |
| `CorruptionBarView` | Own the `Image` reference for the player corruption fill bar | `Assets/UI/Player/CorruptionBarView.cs` |
| `WaterTierIndicator` | Display 0–3 tier icons / highlight active tier | `Assets/UI/Player/WaterTierIndicator.cs` |
| `SkillCooldownView` | Radial fill overlay per skill slot, driven by `ISkill.IsOnCooldown` + remaining time | `Assets/UI/Player/SkillCooldownView.cs` |
| `EnemyUIPresenter` | Subscribe to `EnemyStats` events, drive enemy bars, compute Sweet Spot overlay position | `Assets/UI/Enemy/EnemyUIPresenter.cs` |
| `EnemyHpBarView` | Own the HP fill `Image` on the World Space canvas | `Assets/UI/Enemy/EnemyHpBarView.cs` |
| `EnemyCorruptionBarView` | Own the corruption fill `Image` and the Sweet Spot RectTransform | `Assets/UI/Enemy/EnemyCorruptionBarView.cs` |
| `SweetSpotOverlay` | Stateless view: given (min, max, barWidth) positions and sizes a highlight rect | `Assets/UI/Enemy/SweetSpotOverlay.cs` |
| `BossBarPresenter` | Activated on boss room entry, subscribes to boss `EnemyStats`, drives screen-space bar | `Assets/UI/Boss/BossBarPresenter.cs` |
| `GameOverUI` | Subscribes to `GameStateManager.OnGameStateChange`, shows panel on `GameOver` | `Assets/UI/Overlay/GameOverUI.cs` |

---

## Recommended Project Structure

```
Assets/
└── UI/
    ├── Player/
    │   ├── PlayerHUDPresenter.cs      # Event bridge: PlayerWaterStats → HUD views
    │   ├── WaterBarView.cs            # Drives water HP Image.fillAmount
    │   ├── CorruptionBarView.cs       # Drives player corruption Image.fillAmount
    │   ├── WaterTierIndicator.cs      # Highlights active tier icon (0–3)
    │   └── SkillCooldownView.cs       # Radial overlay per skill slot
    ├── Enemy/
    │   ├── EnemyUIPresenter.cs        # Event bridge: EnemyStats → enemy bar views
    │   ├── EnemyHpBarView.cs          # HP fill bar
    │   ├── EnemyCorruptionBarView.cs  # Corruption fill bar + hosts SweetSpotOverlay
    │   └── SweetSpotOverlay.cs        # Positions/sizes the highlight rect
    ├── Boss/
    │   ├── BossBarPresenter.cs        # Binds to boss EnemyStats on room entry
    │   └── BossBarView.cs             # Screen Space fill bar + name label
    └── Overlay/
        ├── GameOverUI.cs              # GameStateManager → show/hide panel
        └── PauseUI.cs                 # GameStateManager.Paused → show/hide
```

---

## Architectural Patterns

### Pattern 1: Presenter-per-Stats (MVP, no framework)

**What:** Each stats class (`PlayerWaterStats`, `EnemyStats`) owns C# `Action` events that already fire on every change. A dedicated Presenter MonoBehaviour subscribes to those events in `Start()` and translates data values into UGUI property writes. The View scripts (Bar, Indicator) expose only typed setters — no events of their own, no game-layer references.

**Why this codebase:** The existing events (`OnWaterChanged(float, float)`, `OnCorruptionChanged(float, float)`, `OnWaterTierChanged(int)`) already carry exactly the data the HUD needs. No polling, no intermediate bus needed.

**When to use:** All UI in this project. The coupling distance is: Stats → Presenter → View. Stats knows nothing about UI. View knows nothing about game logic.

**Component boundary rule:**
- Presenter holds a `[SerializeField]` reference to the Stats script (injected via prefab / Inspector).
- View holds only UGUI component references (`Image`, `Text`, `RectTransform`).
- Presenter never touches UGUI components directly; it calls View setters.

**Example structure:**
```csharp
// PlayerHUDPresenter.cs
public class PlayerHUDPresenter : MonoBehaviour
{
    [SerializeField] private PlayerWaterStats stats;
    [SerializeField] private WaterBarView     waterBar;
    [SerializeField] private CorruptionBarView corruptionBar;
    [SerializeField] private WaterTierIndicator tierIndicator;

    private void Start()
    {
        // Initialise from current values (covers scene-load state)
        waterBar.SetFill(stats.WaterRatio);
        corruptionBar.SetFill(stats.CorruptionRatio);
        tierIndicator.SetTier(stats.WaterTier);

        stats.OnWaterChanged      += HandleWaterChanged;
        stats.OnCorruptionChanged += HandleCorruptionChanged;
        stats.OnWaterTierChanged  += tierIndicator.SetTier;
    }

    private void OnDestroy()
    {
        stats.OnWaterChanged      -= HandleWaterChanged;
        stats.OnCorruptionChanged -= HandleCorruptionChanged;
        stats.OnWaterTierChanged  -= tierIndicator.SetTier;
    }

    private void HandleWaterChanged(float current, float max)
        => waterBar.SetFill(current / max);

    private void HandleCorruptionChanged(float current, float max)
        => corruptionBar.SetFill(current / max);
}
```

**Trade-offs:**
- No ScriptableObject event bus overhead — appropriate for a single-player game with a small number of stats objects.
- Presenter needs an Inspector reference to the Stats script. For enemy prefabs, the Presenter and EnemyStats live on the same prefab, so `GetComponent<EnemyStats>()` in `Awake` is clean.

---

### Pattern 2: World Space Canvas on Enemy Prefab

**What:** Each enemy prefab gets a child GameObject with a `Canvas` set to **World Space** render mode. The canvas `RectTransform` is positioned at a fixed local offset above the sprite pivot (e.g., `localPosition = (0, 1.8f, 0)`). The canvas does not use a Camera reference for World Space in 2D URP — it renders into world units automatically.

**Why:** Following the enemy Transform via a child canvas requires zero per-frame code. Unity handles positioning; the bars simply stay in local space above the enemy. This is the standard approach for 2D action games.

**Canvas settings:**
- Render Mode: World Space
- Dynamic Pixels Per Unit: 10 (prevents blurry text/sprites at small world sizes)
- Sorting Layer: "UI" (above all sprite layers)
- Sort Order: set per-enemy so bars do not z-fight when enemies stack

**Prefab hierarchy:**
```
EnemyRoot (EnemyAI, EnemyAttack, EnemyStats, EnemyUIPresenter)
└── EnemyCanvas [Canvas — World Space, RectTransform 200×40px at (0, 1.8, 0)]
    ├── HpBarBG [Image — dark background]
    │   └── HpBarFill [Image — green, fillMethod=Horizontal, EnemyHpBarView]
    ├── CorruptionBarBG [Image — dark background]
    │   ├── CorruptionBarFill [Image — purple, fillMethod=Horizontal, EnemyCorruptionBarView]
    │   └── SweetSpotHighlight [Image — semi-transparent yellow, SweetSpotOverlay]
    └── (optional) EnemyNameLabel [TextMeshProUGUI]
```

**EnemyUIPresenter wiring:** The Presenter lives on `EnemyRoot`. It calls `GetComponent<EnemyStats>()` in `Awake` — no Inspector drag needed. It reads `basePurificationMin`, `basePurificationMax`, `bonusPurificationMargin` from `EnemyStats` to initialize the Sweet Spot overlay.

---

### Pattern 3: Sweet Spot Highlight — RectTransform Anchored Overlay

**What:** The Sweet Spot is a semi-transparent colored `Image` child of `CorruptionBarFill`'s parent (the bar background), positioned and sized in local rect space to cover exactly the purification range.

**Why this approach over a custom shader:** The corruption bar uses `Image.fillMethod = Horizontal`, which means the fill region from 0→1 maps directly to `RectTransform.rect.width`. A child RectTransform sitting above the bar can be positioned using the same 0–1 normalized values, multiplied by bar width. No shader, no custom mesh — pure UGUI.

**Calculation (in `SweetSpotOverlay.cs`):**
```
barWidth  = corruptionBarRect.rect.width   // e.g. 200px

// EnemyStats properties:
float effectiveMin = stats.basePurificationMin - stats.bonusPurificationMargin;
float effectiveMax = stats.basePurificationMax + stats.bonusPurificationMargin;

// Position: left edge of highlight at effectiveMin fraction of bar width
// Pivot of SweetSpotHighlight = (0, 0.5) — left-anchored
highlightRect.anchorMin = new Vector2(0, 0);
highlightRect.anchorMax = new Vector2(0, 1);
highlightRect.offsetMin = new Vector2(effectiveMin * barWidth, 0);
highlightRect.offsetMax = new Vector2(effectiveMax * barWidth, 0);
```

Equivalently, use `anchorMin.x = effectiveMin` and `anchorMax.x = effectiveMax` with both offsets zeroed — **this is the cleaner approach** since the bar background RectTransform is the parent:

```csharp
// SweetSpotOverlay.cs
public void SetRange(float normalizedMin, float normalizedMax)
{
    var rect = GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(normalizedMin, 0f);
    rect.anchorMax = new Vector2(normalizedMax, 1f);
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
}
```

**Call site in EnemyUIPresenter:**
```csharp
float min = stats.basePurificationMin - stats.bonusPurificationMargin;
float max = stats.basePurificationMax + stats.bonusPurificationMargin;
sweetSpotOverlay.SetRange(min, max);
```

This must be called once on `Start()` and again whenever `bonusPurificationMargin` changes (future item system). `EnemyStats` should add an `OnPurificationRangeChanged` event when the item system is implemented; until then, call `SetRange` once in `Start`.

**Visual spec:**
- Color: `new Color(1f, 0.9f, 0.2f, 0.4f)` — semi-transparent gold
- Raycast Target: false (no input blocking)
- Image Type: Simple (no sprite needed — solid color via `Image.color`)

---

### Pattern 4: Boss Bar — Screen Space, Conditional Activation

**What:** The Boss Bar lives in the same Screen Space Overlay Canvas as the Player HUD but is inside a dedicated Panel that starts **inactive** (`gameObject.SetActive(false)`). When the game enters a boss room, a `BossBarPresenter` is activated, which calls `SetActive(true)` on the panel and subscribes to the boss's `EnemyStats`.

**Why separate from enemy World Space Canvas:** The boss bar must always be screen-anchored, large, and visible regardless of boss position. Using a World Space canvas for the boss would require following its transform — which breaks when the boss moves off-screen.

**Activation pattern:**
```csharp
// BossBarPresenter.cs — called by a future BossRoomTrigger
public void BindToBoss(EnemyStats bossStats)
{
    this.bossStats = bossStats;
    bossBarPanel.SetActive(true);
    bossNameLabel.text = bossStats.gameObject.name;

    // Initialise fill from current values
    bossHpBar.SetFill(bossStats.CurrentHp / bossStats.MaxHp);

    bossStats.OnDamaged += HandleBossDamaged;
    bossStats.OnDeath   += HandleBossDeath;
}

private void HandleBossDamaged()
{
    bossHpBar.SetFill(bossStats.CurrentHp / bossStats.MaxHp);
}

private void HandleBossDeath()
{
    bossStats.OnDamaged -= HandleBossDamaged;
    bossStats.OnDeath   -= HandleBossDeath;
    StartCoroutine(HidePanelAfterDelay(2f));
}
```

**Note on `EnemyStats` gap:** `EnemyStats` currently fires `OnDamaged` (no data) and `OnDeath`. It does not fire a continuous stat-change event the way `PlayerWaterStats` does. The boss bar must re-read `stats.CurrentHp / stats.MaxHp` inside `HandleBossDamaged`. This is acceptable since `OnDamaged` fires on every `TakeDamage` call.

---

### Pattern 5: Skill Cooldown — Radial Fill Overlay

**What:** Per skill slot, a full-size semi-transparent dark `Image` using `fillMethod = Radial360` is overlaid on the skill icon. When a skill enters cooldown, the fill amount starts at 1.0 (fully covered) and is driven to 0.0 over the cooldown duration.

**Why radial over linear:** Radial communicates "time remaining until ready" more intuitively for action game skills. It is a UGUI built-in with no custom shaders needed.

**Cooldown data gap:** `SkillBase` exposes `IsOnCooldown` (bool) and `cooldownDuration` (serialized float) but does not expose a remaining-time float property. The `SkillCooldownView` needs to drive the fill by tracking elapsed time itself, starting a coroutine when `IsOnCooldown` transitions from false to true.

**Implementation approach — polling in Update:**
```csharp
// SkillCooldownView.cs
private ISkill skill;
private bool wasOnCooldown;
private float cooldownStart;

private void Update()
{
    bool isOnCooldown = skill.IsOnCooldown;

    if (isOnCooldown && !wasOnCooldown)
        cooldownStart = Time.time;

    if (isOnCooldown)
    {
        float elapsed  = Time.time - cooldownStart;
        float fill     = 1f - Mathf.Clamp01(elapsed / cooldownDuration);
        radialOverlay.fillAmount = fill;
        radialOverlay.gameObject.SetActive(true);
    }
    else
    {
        radialOverlay.gameObject.SetActive(false);
    }

    wasOnCooldown = isOnCooldown;
}
```

**Alternative (preferred if `SkillBase` is extended):** Add `public float CooldownRemaining { get; }` and `public float CooldownDuration { get; }` to `ISkill` and `SkillBase`. This removes polling and lets the View be purely data-driven. This extension should be done in Phase 1 alongside UI implementation.

---

## Data Flow

### Player Stats to HUD

```
PlayerWaterStats.SacrificeWater() / ReceiveAttack() / Heal()
    │
    ├── OnWaterChanged(float current, float max) ──────► PlayerHUDPresenter
    │                                                         └── WaterBarView.SetFill(current/max)
    │                                                              └── Image.fillAmount = value
    │
    ├── OnCorruptionChanged(float current, float max) ─► PlayerHUDPresenter
    │                                                         └── CorruptionBarView.SetFill(current/max)
    │
    └── OnWaterTierChanged(int tier) ──────────────────► WaterTierIndicator.SetTier(tier)
```

### Enemy Stats to World Space Bars

```
EnemyStats.TakeDamage(hp, corruption)
    │
    ├── CurrentHp / CurrentCorruption updated (no events for continuous values)
    │
    └── OnDamaged.Invoke() ─────────────────────────────► EnemyUIPresenter.HandleDamaged()
                                                               ├── EnemyHpBarView.SetFill(stats.CurrentHp / stats.MaxHp)
                                                               └── EnemyCorruptionBarView.SetFill(
                                                                       stats.CurrentCorruption / stats.MaxCorruption)
```

**Data flow gap flagged:** `EnemyStats` does not fire separate HP-changed and Corruption-changed events. It fires only `OnDamaged` (both change together per `TakeDamage`). This is sufficient: the Presenter reads both ratios on every `OnDamaged`. No refactoring of `EnemyStats` required for Phase 1.

### Game State to Overlay UI

```
PlayerWaterStats.CheckDeath()
    └── GameStateManager.SetState(GameOver)
            └── OnGameStateChange(GameState.GameOver) ──► GameOverUI.HandleStateChange()
                                                               └── gameOverPanel.SetActive(true)
```

---

## Component Boundaries

| Boundary | Communication | Direction | Notes |
|----------|---------------|-----------|-------|
| `PlayerWaterStats` ↔ `PlayerHUDPresenter` | C# Action events | Stats → Presenter (one-way) | Presenter subscribes; Stats never references UI |
| `EnemyStats` ↔ `EnemyUIPresenter` | C# Action events + direct property read | Stats → Presenter (one-way) | Presenter reads `CurrentHp`, `MaxHp`, `CurrentCorruption`, `MaxCorruption` on `OnDamaged` |
| `EnemyUIPresenter` ↔ `SweetSpotOverlay` | Direct method call `SetRange(min, max)` | Presenter → View | Called once in `Start`; repeated when purification margin changes |
| `SkillBase` ↔ `SkillCooldownView` | Polling `ISkill.IsOnCooldown` in `Update` | View polls Skill (acceptable for 3 skills) | Upgrade to event/property if ISkill is extended |
| `GameStateManager` ↔ `GameOverUI` / `PauseUI` | `OnGameStateChange` event | Manager → UI | Same pattern as existing `InputHandler` subscription |
| `BossBarPresenter` ↔ `EnemyStats` (boss) | Direct method call `BindToBoss(EnemyStats)` + events | External trigger → Presenter, then Stats → Presenter | Boss room trigger calls `BindToBoss`; future milestone |

---

## Build Order

Dependencies between UI components determine implementation sequence:

```
1. UGUI Canvas scaffold
   └── Create Player HUD Canvas (Screen Space Overlay)
   └── Create Boss Bar panel (inactive) inside same Canvas

2. WaterBarView + CorruptionBarView (stateless fill setters)
   └── No dependencies on game scripts

3. PlayerHUDPresenter
   └── Depends on: PlayerWaterStats (exists), WaterBarView, CorruptionBarView

4. WaterTierIndicator
   └── Depends on: PlayerHUDPresenter wiring (OnWaterTierChanged)

5. SweetSpotOverlay (stateless, pure view)
   └── No game-script dependencies

6. EnemyCorruptionBarView + EnemyHpBarView
   └── No dependencies on game scripts

7. EnemyUIPresenter (on enemy prefab)
   └── Depends on: EnemyStats (exists), EnemyHpBarView, EnemyCorruptionBarView, SweetSpotOverlay

8. SkillCooldownView
   └── Depends on: ISkill interface (exists); optionally on SkillBase cooldown extension

9. BossBarPresenter + BossBarView
   └── Depends on: EnemyStats (exists), BossBarView, BossBarPanel activation logic
   └── Requires: boss room trigger (Phase 2 scope) — stub the bind method for Phase 1 testing
```

**Rationale for this order:** Stateless view components (2, 5, 6) can be built and tested with placeholder values in the Inspector before any Presenter exists. Presenters (3, 7, 8, 9) are wired last once their view dependencies are confirmed working. Boss bar is last because it requires scene architecture (room trigger) that doesn't exist yet.

---

## Anti-Patterns

### Anti-Pattern 1: Presenter Directly Touching `Image.fillAmount`

**What people do:** Write a single `PlayerHUD` MonoBehaviour that holds `[SerializeField] Image waterBarFill` and sets `waterBarFill.fillAmount = stats.WaterRatio` directly inside the event handler.

**Why it's wrong:** It works for one bar. With five HUD elements, the class becomes a 200-line God Object that mixes layout concerns, stat interpretation, and animation. Adding a "flash red when HP low" effect requires branching inside what should be a simple fill setter.

**Do this instead:** Keep Presenters thin (interpret data, call typed setters). Keep View scripts thin (own the `Image` reference, expose `SetFill(float)`, handle visual effects like flashing internally). This lets you swap visuals (e.g., replace bar with icon row) without touching Presenter logic.

---

### Anti-Pattern 2: World Space Canvas Not as Child of Enemy

**What people do:** Create a single separate UI GameObject for enemy bars, then write `LateUpdate` code to `Camera.WorldToScreenPoint` the enemy position and move a Screen Space element to match.

**Why it's wrong:** Screen-to-world conversion requires camera reference, breaks with camera zoom, and produces one-frame lag artifacts. Screen Space enemy bars also clip incorrectly when enemies go near screen edges.

**Do this instead:** Child the Canvas directly to the enemy prefab. Unity moves it automatically. Zero code for positioning.

---

### Anti-Pattern 3: Subscribing to Stats Events Without Unsubscribing

**What people do:** Subscribe in `Start()` and forget to unsubscribe on `OnDestroy()`.

**Why it's wrong:** When an enemy is `Destroy`'d (which `EnemyStats.Die()` does immediately), the `EnemyStats` component fires `OnDeath` and is then destroyed. If `EnemyUIPresenter` does not unsubscribe, the delegate list on the stats object holds a reference that causes `MissingReferenceException` on the next event fire — or silently leaks memory. The existing codebase already follows the `OnDestroy` unsubscribe pattern (noted in its ARCHITECTURE.md error handling section). All new UI Presenters must follow it.

**Do this instead:** Mirror the exact pattern `EnemyAI` uses: subscribe in `Start()`, unsubscribe every handler in `OnDestroy()`.

---

### Anti-Pattern 4: Polling `EnemyStats` in `Update` for HP Values

**What people do:** Skip event wiring and instead read `stats.CurrentHp` in `Update()` every frame.

**Why it's wrong:** With 10 enemies active, that is 10 unnecessary reads per frame for values that change only on attack hit. With 30 enemies in a roguelike wave, it scales poorly. It also bypasses the established event architecture.

**Do this instead:** Use `OnDamaged` (already exists) as the trigger. Read the stat values once inside the handler.

---

### Anti-Pattern 5: Boss Bar on a Separate Scene Canvas

**What people do:** Create a second Camera and a second Canvas just for the boss bar to ensure it always renders on top.

**Why it's wrong:** Unnecessary for a 2D game with a single camera. Sort Order on Canvases is sufficient. Multiple Canvases complicate the UI hierarchy and increase draw calls.

**Do this instead:** Put the boss bar panel in the same Screen Space Overlay Canvas as the Player HUD, at a higher Sort Order (or simply below the HUD in hierarchy — Screen Space Overlay renders in hierarchy order). Activate/deactivate the panel via `SetActive`.

---

## Integration Points

### Existing Game Scripts → New UI Scripts

| Game Script | UI Script | How Connected |
|-------------|-----------|---------------|
| `PlayerWaterStats` | `PlayerHUDPresenter` | Inspector `[SerializeField]` reference; Presenter subscribes to 3 events |
| `EnemyStats` | `EnemyUIPresenter` | `GetComponent<EnemyStats>()` in `Awake` (same prefab); subscribes to `OnDamaged` + `OnDeath` |
| `SkillBase` (×3) | `SkillCooldownView` (×3) | Inspector reference to each skill MonoBehaviour cast to `ISkill` |
| `GameStateManager` | `GameOverUI`, `PauseUI` | Subscribe to `GameStateManager.Instance.OnGameStateChange` in `Start` |
| `EnemyStats` (boss) | `BossBarPresenter` | `BindToBoss(EnemyStats)` called by future boss room trigger |

### Internal UI Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| `PlayerHUDPresenter` ↔ `WaterBarView` | Direct method call | Both on same Canvas; Inspector reference |
| `EnemyUIPresenter` ↔ `SweetSpotOverlay` | `SetRange(float, float)` method | SweetSpotOverlay is child of CorruptionBarBG in prefab |
| `BossBarPresenter` ↔ `BossBarView` | Direct method call | Both on Player HUD Canvas |

---

## Scalability Considerations

This is a single-player game. Scalability here means "stays clean as enemy count and skill count grow":

| Concern | At 5 enemies | At 30 enemies | Notes |
|---------|-------------|---------------|-------|
| World Space Canvas draw calls | 5 canvases | 30 canvases | Each World Space canvas is a separate draw call batch. For 30+ enemies, consider one shared World Space canvas + manual bar positioning. Not needed for Phase 1. |
| Sweet Spot overlay recalculation | O(1) per enemy on Start | O(1) per enemy on Start | Static until item system changes margins; no per-frame cost |
| Skill cooldown polling | 3 Update calls | 3 Update calls (skill count doesn't change) | Negligible |
| Boss bar | 1 active at a time | 1 active at a time | Always O(1) |

---

## Sources

- Existing codebase: `Assets/Enemy/EnemyStats.cs`, `Assets/Player/PlayerWaterStats.cs`, `Assets/Player/SkillBase.cs`, `Assets/GameScript/GameStateManager.cs` — read directly 2026-03-27
- Existing architecture document: `.planning/codebase/ARCHITECTURE.md` — read directly 2026-03-27
- Unity UGUI Image.fillMethod / RectTransform anchor system — HIGH confidence (stable API across Unity 5–6, documented at docs.unity3d.com)
- World Space Canvas child-of-prefab pattern — HIGH confidence (fundamental Unity pattern, unchanged in Unity 6)
- MVP/Presenter pattern for Unity — HIGH confidence (widely established; matches existing event-driven structure in codebase)

---

*Architecture research for: Bleeding Spring — UGUI UI System Integration*
*Researched: 2026-03-27*
