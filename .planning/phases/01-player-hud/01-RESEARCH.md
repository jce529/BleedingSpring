# Phase 1: Player HUD — Research

**Researched:** 2026-03-30
**Phase:** 01-player-hud
**Requirements covered:** HUD-01, HUD-02, HUD-03, HUD-04, TECH-01

---

## Summary

Phase 1 builds three distinct visual systems on top of the existing `PlayerWaterStats` event infrastructure: (1) a World Space vertical bar attached to the player that unifies water/HP and corruption into a single dual-fill gauge with three orb indicators above it, (2) a Screen Space vignette that pulses in three color stages based on the corruption-to-HP ratio, and (3) two code-level changes — updating the death condition in `CheckDeath()` and extending `ISkill`/`SkillBase` with cooldown-tracking properties. All UI data flows from existing `Action` events (`OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged`); no polling is required. The CONTEXT.md decisions override the original REQUIREMENTS.md wording for HUD-01 through HUD-04.

---

## 1. World Space HUD Attachment

### How World Space Canvas works in UGUI 2.0

A Canvas set to **Render Mode: World Space** is a regular `GameObject` with a `RectTransform`. It renders in scene units, not screen pixels. It is sorted by the scene's camera like any other sprite — so the **Sorting Layer** and **Order in Layer** must be set so the bar renders in front of the player sprite.

### Attachment strategy for a 2D character that flips via `scale.x`

The codebase flips the player by negating `transform.localScale.x` inside `PlayerMovement.Flip()`. This means any **child** of the player `Transform` automatically mirrors with it — including a child World Space Canvas.

**Recommended approach: child Canvas with fixed local offset**

1. Create a child `GameObject` on the player named `HUDRoot`.
2. Attach a `Canvas` (World Space) and `CanvasScaler` to `HUDRoot`.
3. Set `HUDRoot`'s local position to a fixed offset, e.g., `(0.35f, 0.9f, 0f)` — right of center at torso height. Tune in Play Mode.
4. Because `scale.x` inversion propagates to children, the bar automatically mirrors to the correct arm side on every flip. **No direction-tracking script is needed for the bar position itself.**
5. Set Canvas `sortingLayerName` to the UI layer (or a dedicated "PlayerHUD" layer above "Player"). Set `orderInLayer` to a value above the player sprite.

**Important Unity 6 note:** World Space Canvas in URP requires the camera to have a `Universal Additional Camera Data` component with the correct renderer assigned. Verify the 2D renderer is active. `EventSystem` must exist in the scene for UGUI to function, but is not strictly needed for a display-only HUD.

**Canvas dimensions:** Keep the RectTransform small (e.g., 50×200 units in scene space at scale 0.01) so it maps neatly to sprite-pixel scale. A common pattern for a 100-unit-per-world-unit game is `width=0.25, height=1.0, scale=(0.01, 0.01, 0.01)`.

---

## 2. Vertical Bar with Dual Fill

### Design goal (D-13)

A single vertical bar where:
- **Water (blue)** fills from the top downward, representing `CurrentCleanWater / maxCleanWater`.
- **Corruption (dark purple/black)** fills from the bottom upward, representing `CurrentCorruption / CurrentCleanWater` (the death ratio per D-01/D-02).
- The two regions share the same bar space — when corruption meets water, the player dies.

### UGUI Image fill options

| Approach | Description | Suitability |
|---|---|---|
| Two stacked `Image` (Filled, Vertical) | Water image fills top-to-bottom; corruption image fills bottom-to-top on the same rect | Best fit — simple, no shader |
| Single `Image` with custom texture | Texture encodes both colors; script adjusts UV or fill | Overly complex for this use case |
| Slider component | Wrong semantic; clamps at 0/1 and does not support inverted fill on same rect | Avoid |

### Recommended: Two stacked Images

**Structure under the bar background Image:**
```
BarBackground (Image — dark outline/bg)
├── WaterFill   (Image, Fill Method: Vertical, Fill Origin: Top,    fillAmount driven by water ratio)
└── CorruptionFill (Image, Fill Method: Vertical, Fill Origin: Bottom, fillAmount driven by corruption ratio)
```

Both `WaterFill` and `CorruptionFill` share the same `RectTransform` anchors stretching to fill the background. The corruption image sits **above** the water image in the hierarchy (renders on top) so the visual encroachment is clear. Alternatively, CorruptionFill can be a sibling below WaterFill with a slightly different Z or draw order — test what gives the clearest "corruption eating water" look.

**Fill amount calculations:**

```csharp
// WaterFill: what fraction of max HP remains
waterImage.fillAmount = currentCleanWater / maxCleanWater;

// CorruptionFill: what fraction of current HP is corrupted (death ratio)
// Guard against division by zero when currentCleanWater == 0
float corruptionRatio = currentCleanWater > 0f
    ? Mathf.Clamp01(currentCorruption / currentCleanWater)
    : 1f;
corruptionImage.fillAmount = corruptionRatio;
```

**Event wiring:** Subscribe to both `OnWaterChanged(float current, float max)` and `OnCorruptionChanged(float current, float max)`. Both values are needed simultaneously to compute the corruption ratio (corruption / currentHP). Cache `currentCleanWater` in the HUD script from the last `OnWaterChanged` call.

**Color values (D-04 palette applied to bar):**
- Water fill: `Color(0.25f, 0.65f, 1.0f, 0.9f)` — clear blue
- Corruption fill: `Color(0.18f, 0.05f, 0.22f, 1.0f)` — dark purple/black

---

## 3. Water Tier Orb Display

### Design goal (D-14)

Three orbs above the vertical bar. Orbs 1–N light up based on `WaterTier` (0–3). At tier 0, all are dark. At tier 3, all three are lit.

### UGUI approach

**Option A — GameObject activation:** Three child GameObjects with an Image each. Enable/disable the Image component or the GameObject. Simple, zero runtime allocation.

**Option B — Image color/alpha toggle:** Keep all three Images active; swap between a "lit" color/alpha and a "dim" color/alpha in code. Avoids the overhead of `SetActive` and allows future animation (fade, scale pop).

**Recommended: Option B (color toggle)** — it supports a future Polish pass (scale bounce when tier increases) without structural changes. Use two pre-defined colors: `litColor` and `dimColor` serialized in the inspector, so the artist can tune them without code changes.

```csharp
// In OnWaterTierChanged(int tier) handler:
for (int i = 0; i < orbImages.Length; i++)
    orbImages[i].color = (i < tier) ? litColor : dimColor;
```

**Layout:** Three Images in a horizontal `HorizontalLayoutGroup` above the bar, with spacing. Use `ContentSizeFitter` set to preferred size, or manually size the row RectTransform.

---

## 4. Vignette Implementation

### Context: D-05 leaves this to Claude's discretion

Three options exist in Unity 6 URP:

| Approach | Pros | Cons |
|---|---|---|
| Screen Space Overlay Canvas + fullscreen Image with radial gradient texture | Zero post-processing cost, zero render pipeline dependency, trivially scriptable | Requires a pre-made radial gradient texture; sits on top of all world geometry including UI |
| URP Volume + built-in Vignette post-process | Native URP integration, physically correct falloff | Built-in URP Vignette only supports grayscale intensity, not per-stage color; requires Volume scripting API; cannot be pulsed without runtime Volume profile modification |
| Full Screen Pass Renderer Feature (URP 17+) | Full control via custom shader | Requires HLSL shader authoring; overkill for a color + opacity vignette |

### Recommended: Screen Space Overlay Canvas + fullscreen radial gradient Image

**Rationale:**
- Matches the existing UGUI-only constraint (PROJECT.md: "UI 렌더링: Unity UGUI 2.0.0").
- Color changes (D-04) and pulse opacity (D-03) are trivial: just set `image.color`.
- No render pipeline coupling — works across any renderer configuration.
- Sits on top of all game content by design (that is the expected behavior for a vignette).

**Setup:**
1. Create a separate Canvas (`Render Mode: Screen Space — Overlay`, `Sort Order` high enough to be above all other UI).
2. Add a fullscreen `Image` child with `RectTransform` stretching to all edges (`left=0, right=0, top=0, bottom=0`).
3. Assign a radial gradient texture: dark at edges, transparent in center. This can be a 256×256 PNG with center alpha=0 and edge alpha=1 in the target color channel. Alternatively, use a sprite with the Sprite Editor to define a 9-slice that is entirely soft alpha.
4. Set Image `Raycast Target = false` (it must never block player input).
5. Set Image `Color` to the stage color and modulate `Color.a` for pulse.

**Stage colors (D-04):**
```csharp
private static readonly Color[] StageColors = {
    new Color(0.75f, 0.60f, 1.00f, 0f),   // inactive (transparent)
    new Color(0.75f, 0.60f, 1.00f, 0f),   // stage 1 — lavender base (alpha driven by pulse)
    new Color(0.25f, 0.10f, 0.70f, 0f),   // stage 2 — indigo
    new Color(0.50f, 0.00f, 0.80f, 0f),   // stage 3 — deep purple
};
```
The `a` component is 0 in the color definition because it is overwritten each frame by the pulse calculation. The RGB is set to the stage color. Alternatively store RGB and alpha range separately.

---

## 5. Pulse Animation

### Design goal (D-03)

Three pulse speeds:
- Stage 1: slow (~2 s period)
- Stage 2: medium (~1 s period)
- Stage 3: fast (~0.4 s period)

### Implementation: `Mathf.PingPong` in `Update` — no Animator required

```csharp
private static readonly float[] PulsePeriod = { 0f, 2.0f, 1.0f, 0.4f };
private static readonly float   MinAlpha    = 0.05f;
private static readonly float   MaxAlpha    = 0.45f;

// In Update(), only when a stage is active:
if (currentStage == 0)
{
    vignetteImage.color = Color.clear;
    return;
}

float period    = PulsePeriod[currentStage];
float t         = Mathf.PingPong(Time.time / period, 1f);   // 0→1→0 over one period
float alpha     = Mathf.Lerp(MinAlpha, MaxAlpha, t);
Color stageColor = StageColors[currentStage];
vignetteImage.color = new Color(stageColor.r, stageColor.g, stageColor.b, alpha);
```

`Mathf.PingPong` is framerate-independent (uses `Time.time`), produces a smooth triangle wave, and requires no allocation. It is the idiomatic Unity approach for this pattern without an Animator.

**Stage transition:** When the ratio crosses a threshold boundary, set `currentStage` immediately. The pulse restarts naturally from the new period. A brief `Color.Lerp` over ~0.2 s for the color channel could be added in a later Polish pass (deferred per CONTEXT.md).

**Ratio calculation (D-02):**
```csharp
// Computed in event handlers, not Update
float ratio = (currentCleanWater > 0f)
    ? Mathf.Clamp01(currentCorruption / currentCleanWater)
    : 1f;

currentStage = ratio >= 0.75f ? 3
             : ratio >= 0.50f ? 2
             : ratio >= 0.25f ? 1
             : 0;
```

**Update vs event-based calculation:** The ratio changes only when `OnWaterChanged` or `OnCorruptionChanged` fires. Recalculate `currentStage` only inside those event handlers. The `Update` loop only reads `currentStage` and `Time.time` — no division in the hot path.

---

## 6. Death Condition Change

### Current code (`PlayerWaterStats.CheckDeath`, line 157–175)

```csharp
bool waterDepleted   = CurrentCleanWater <= 0f;
bool corruptionDeath = CurrentCorruption >= maxCorruptionThreshold;  // <-- this line changes
```

### Required change (D-01)

Replace `CurrentCorruption >= maxCorruptionThreshold` with `CurrentCorruption >= CurrentCleanWater`:

```csharp
// BEFORE:
bool corruptionDeath = CurrentCorruption >= maxCorruptionThreshold;

// AFTER:
bool corruptionDeath = CurrentCorruption >= CurrentCleanWater;
```

That is the only line that changes in `CheckDeath()`. The debug log message on line 169 should also be updated to describe the new condition:

```csharp
// BEFORE:
Debug.Log("[PlayerWaterStats] 오염도 초과 — 즉사 처리");

// AFTER:
Debug.Log("[PlayerWaterStats] 오염도 ≥ 현재 HP — 즉사 처리");
```

**Side effects to audit:**
- `TakeDamage()` and `ReceiveAttack()` both clamp `CurrentCorruption` to `maxCorruptionThreshold`. This clamping is now a less meaningful ceiling — it prevents corruption from exceeding 100 but the death trigger fires before that in most gameplay scenarios. The clamp can remain as a safe guard; it does not interfere with the new condition.
- `CorruptionRatio` property (`CurrentCorruption / maxCorruptionThreshold`) is still valid for any code that uses it as a 0–1 gauge reference (e.g., vignette ratio uses `corruption / currentHP`, not `CorruptionRatio`).
- `maxCorruptionThreshold` field remains useful as a cap for incoming corruption values; it is still referenced in `ReceiveAttack` and `TakeDamage`. Do not remove it.
- `SacrificeWater` returns `false` if `CurrentCleanWater <= cost` — this means a player can never skill themselves to exactly 0 HP. Since the new death condition is `corruption >= currentHP`, a player at very low HP with any corruption is in immediate danger. This interaction is intentional (D-01 design note in CONTEXT.md: "고위험 고보상 결정이 더 긴박해짐").

---

## 7. ISkill Interface Extension

### Current `ISkill` interface (Assets/Player/ISkill.cs)

```csharp
public interface ISkill
{
    bool IsOnCooldown { get; }
    int  Stage        { get; }
    void Initialize(IPlayerContext context);
    void SetStage(int stage);
    void TryUse();
}
```

### Required additions (D-10)

```csharp
float CooldownRemaining { get; }
float CooldownDuration  { get; }
```

### How to add without breaking existing implementations

`SkillBase` is the only concrete implementation of `ISkill` in the codebase (all three skills inherit from it). Adding properties to the interface requires `SkillBase` to implement them. No other class directly implements `ISkill`.

**ISkill change:**
```csharp
public interface ISkill
{
    bool  IsOnCooldown      { get; }
    int   Stage             { get; }
    float CooldownRemaining { get; }   // NEW — seconds left on cooldown, 0 if not on cooldown
    float CooldownDuration  { get; }   // NEW — total cooldown time in seconds

    void Initialize(IPlayerContext context);
    void SetStage(int stage);
    void TryUse();
}
```

**SkillBase implementation:**

`cooldownDuration` (SerializeField float) already exists. A timer field tracking elapsed cooldown time is needed.

Add a `private float _cooldownRemaining` field and update it inside `UseCoroutine`:

```csharp
// New field in SkillBase:
private float _cooldownRemaining;

// New ISkill properties:
public float CooldownRemaining => _cooldownRemaining;
public float CooldownDuration  => cooldownDuration;

// Updated UseCoroutine — replace the final WaitForSeconds block:
// BEFORE:
yield return new WaitForSeconds(cooldownDuration);
IsOnCooldown = false;

// AFTER:
_cooldownRemaining = cooldownDuration;
while (_cooldownRemaining > 0f)
{
    _cooldownRemaining -= Time.deltaTime;
    yield return null;
}
_cooldownRemaining = 0f;
IsOnCooldown       = false;
```

This changes the cooldown wait from a single `WaitForSeconds` to a per-frame countdown, which is required to expose a meaningful `CooldownRemaining` value. The behavior is identical from the gameplay perspective; the additional per-frame coroutine step has negligible cost.

**Backward compatibility:** `BasicAttackSkill`, `WideSlashSkill`, and `ProjectileSkill` inherit from `SkillBase` and do not override `UseCoroutine` or any cooldown logic. They gain the new properties automatically with zero code changes in the concrete skill files.

---

## 8. Event Subscription Pattern

### Recommended pattern: `Awake` find + `Start` bind + `OnDestroy` unbind

The HUD scripts are children of (or siblings to) the player. They should use the following pattern:

```csharp
public class PlayerHUDBar : MonoBehaviour
{
    private PlayerWaterStats _stats;

    private void Awake()
    {
        // Walk up the hierarchy to find PlayerWaterStats on the player root.
        // GetComponentInParent is safe for a child Canvas on the player prefab.
        _stats = GetComponentInParent<PlayerWaterStats>();
    }

    private void Start()
    {
        if (_stats == null) return;
        _stats.OnWaterChanged      += HandleWaterChanged;
        _stats.OnCorruptionChanged += HandleCorruptionChanged;
        _stats.OnWaterTierChanged  += HandleWaterTierChanged;

        // Force initial UI state from current values (avoids blank bar on scene load)
        HandleWaterChanged(_stats.CurrentCleanWater, _stats.MaxCleanWater);
        HandleCorruptionChanged(_stats.CurrentCorruption, _stats.maxCorruptionThreshold);
        HandleWaterTierChanged(_stats.WaterTier);
    }

    private void OnDestroy()
    {
        if (_stats == null) return;
        _stats.OnWaterChanged      -= HandleWaterChanged;
        _stats.OnCorruptionChanged -= HandleCorruptionChanged;
        _stats.OnWaterTierChanged  -= HandleWaterTierChanged;
    }

    private void HandleWaterChanged(float current, float max) { /* update fills */ }
    private void HandleCorruptionChanged(float current, float max) { /* update fills */ }
    private void HandleWaterTierChanged(int tier) { /* update orbs */ }
}
```

**Key points:**
- Subscribe in `Start`, not `Awake` — both components may be on the same prefab; `Awake` order within one frame is not guaranteed.
- Always unsubscribe in `OnDestroy` to prevent stale delegate errors if the HUD is destroyed before the player (e.g., scene reload).
- Call handlers immediately in `Start` with current values to initialize bar fill without waiting for the next stat change event.
- The vignette script will be on a separate Screen Space Canvas (not a child of the player). It can `FindObjectOfType<PlayerWaterStats>()` in `Start`, or receive a reference via an inspector field or a singleton accessor. **Inspector field is preferred** for clarity and performance: wire it in the prefab.

---

## 9. Direction Tracking

### How the player flip works

`PlayerMovement.Flip()` (line 172–177) negates `transform.localScale.x`. Because the HUD bar Canvas is a **child** of the player Transform, it inherits the scale inversion automatically. The bar mirrors to the correct side without any additional script.

### What this means for the HUD

No direction-tracking script is needed for the World Space HUD bar position. Placing the Canvas at a fixed positive local X offset (e.g., `localPosition = (0.35f, 0.9f, 0f)`) means:
- When `scale.x = 1` (facing right): bar is to the right — correct (behind the right arm).
- When `scale.x = -1` (facing left): bar is to the left — correct (mirrored with the sprite).

### Caveat: Text and non-symmetric children

If any child of the HUD contains **text** or **asymmetric images**, those will also mirror. For orb Images (symmetric circles) this is a non-issue. If a future addition has directional arrows or text labels, those children would need a counter-scale `(-1, 1, 1)` to remain readable. This is a known limitation of the child-flip approach. For Phase 1, all HUD elements are symmetric, so no counter-scale is needed.

### If independent direction tracking is later needed

Subscribe to a direction change event, or in `LateUpdate` check `transform.parent.localScale.x` and adjust local position sign accordingly. `LateUpdate` is preferred over `Update` for camera/UI following because it runs after all physics and movement.

---

## 10. Unity 6 URP Considerations

### Relevant Unity 6000.3 / URP 17.3 specifics

**World Space Canvas and URP 2D Renderer:**
- The 2D URP renderer uses a 2D-specific pipeline; World Space Canvases render in the normal scene pass. No special configuration is required beyond ensuring the Canvas has a valid `worldCamera` reference if it needs to receive input (display-only HUDs can leave it null).
- `Sorting Layer` on the Canvas Renderer component controls draw order relative to sprites. Set it explicitly — do not rely on default layer order.

**Screen Space Overlay Canvas:**
- Renders after all camera renders. In URP, this means after post-processing, which is correct for a vignette.
- Does **not** go through URP render passes and is not affected by `Volume` profiles. Fully independent.

**URP Volume Vignette (not recommended for this phase):**
- `UniversalRenderPipelineAsset` must have Post Processing enabled per camera.
- The built-in Vignette effect in URP 17 supports `intensity` (0–1) and `color` (single Color). Scripting it at runtime requires `volume.profile.TryGet<Vignette>(out var v)` and setting `v.intensity.Override(value)`. This is viable but adds pipeline coupling that the Screen Space Overlay approach avoids.
- **Do not use** for this phase given the design requires per-stage color changes and pulsing opacity, which are simpler to script on a plain Image.

**`Rigidbody2D.linearVelocity`:**
- The codebase already uses the Unity 6 renamed API (`linearVelocity` instead of `velocity`). Any new HUD scripts that need physics data should follow this convention.

**C# 9 features available:**
- Target-typed `new`, records, switch expressions, and pattern matching are available. Use where they improve clarity.

**UGUI 2.0.0 notes:**
- `Image.fillAmount` remains 0–1 float, Vertical fill with Top/Bottom origin — API unchanged from UGUI 1.x.
- `CanvasScaler` in World Space mode: `dynamicPixelsPerUnit` controls the text sharpness. For a purely graphic HUD (no text), this setting is not critical.

---

## Implementation Order (Recommended)

Implement in this order to enable incremental testing in Play Mode after each step:

1. **TECH-01 — ISkill + SkillBase cooldown extension** (pure code, no scene changes, verifiable immediately via Play Mode debug log)
2. **Death condition change — `PlayerWaterStats.CheckDeath()`** (single-line code change, testable with debug corruption input)
3. **World Space Canvas setup** (scene work: create child Canvas on player prefab, verify it flips correctly)
4. **Dual-fill vertical bar** (add WaterFill + CorruptionFill Images, wire `OnWaterChanged` / `OnCorruptionChanged`)
5. **Water tier orbs** (add three orb Images above bar, wire `OnWaterTierChanged`, test with O key)
6. **Screen Space vignette Canvas** (create overlay Canvas + fullscreen Image with gradient texture)
7. **Pulse animation + stage logic** (add `VignetteController` script, connect to `PlayerWaterStats` events, test all three stages)

---

## Risks & Pitfalls

| Risk | Description | Mitigation |
|---|---|---|
| **Divide-by-zero in corruption ratio** | `currentCorruption / currentCleanWater` when HP reaches 0 before death check fires | Guard: `currentCleanWater > 0f ? ... : 1f` in every ratio computation |
| **Stale event subscription** | If HUD GameObject is destroyed before the player (e.g., scene reload), the delegate reference remains valid but its closure object is gone | Always unsubscribe in `OnDestroy`; use the pattern in Section 8 |
| **Child Canvas scale distortion** | When `parent.scale.x = -1`, child RectTransform widths/positions compute correctly but any `GetWorldCorners` calls return mirrored values | Avoid world-space rect calculations in the HUD scripts; use local positions only |
| **Bar behind player sprite** | World Space Canvas may render behind the player sprite if Sorting Layer/Order is not set correctly | Set Canvas `sortingLayerName` to a layer above "Player"; confirm in Scene view with `Sprite Renderer` order inspector |
| **WaitForSeconds vs manual timer in SkillBase** | Replacing `WaitForSeconds(cooldownDuration)` with a manual timer loop changes the coroutine from a single yield to N yields | Test that cooldown still releases correctly when the GameObject is disabled mid-cooldown (coroutine auto-stops; `IsOnCooldown` will remain `true` until the object is re-enabled — this is existing behavior and is unchanged) |
| **Vignette blocking UI input** | The fullscreen Image receives raycasts by default | Set `Raycast Target = false` on the vignette Image |
| **Initial HUD state blank** | Events only fire on change; if no stat changes occur after scene load, HUD shows default (empty) values | Call handlers in `Start()` with current stat values as documented in Section 8 |
| **`maxCorruptionThreshold` clamping conflict** | After the death condition change, corruption can now kill the player before reaching `maxCorruptionThreshold` | The clamp in `TakeDamage`/`ReceiveAttack` is harmless; leave it in place. No refactor needed |
| **Screen Space vignette above game UI** | If other future Screen Space Canvases exist, Sort Order determines stacking | Set vignette Canvas Sort Order high (e.g., 100) so it is always on top; document this convention |

---

## Validation Architecture

How to verify each requirement passes in Unity Play Mode:

### HUD-01 — Corruption bar reacts to `OnCorruptionChanged`

**How to verify:**
- Enter Play Mode. Open the Inspector on the player; use a debug method or the existing `TakeDamage` call via a test script to increment corruption.
- Observe the CorruptionFill Image's `fillAmount` change in real time in the Inspector.
- Trigger `ReceiveAttack(10f, 0.3f)` — corruption fill should rise from bottom. Trigger `Purify(20f)` — corruption fill should drop.
- **Pass criteria:** CorruptionFill `fillAmount` reflects `CurrentCorruption / CurrentCleanWater` within one frame of the event.

*Note: CONTEXT.md overrides REQUIREMENTS.md for HUD-01 — this is the dual-fill World Space bar, not a separate screen-space corruption bar, and the color warning is replaced by the vignette system (HUD-04).*

### HUD-02 — Visual warning for high corruption ratio

**How to verify (CONTEXT.md override: this is now the vignette, not bar color change):**
- Bring corruption to 25%, 50%, and 75% of current HP. Observe vignette Image color and pulse.
- Stage 1 at 25%: lavender pulse, slow period (~2 s).
- Stage 2 at 50%: indigo pulse, medium period (~1 s).
- Stage 3 at 75%: deep purple pulse, fast period (~0.4 s).
- Drop corruption below threshold: vignette disappears.
- **Pass criteria:** All three stages activate and deactivate at correct ratios with correct colors and pulse speeds.

### HUD-03 — Water tier orbs update on O key

**How to verify:**
- Enter Play Mode. Press O key — `PlayerController.HandleWaterTierSwitchInput()` calls `Stats.CycleWaterTier()` which fires `OnWaterTierChanged`.
- Cycle through 0→1→2→3→0. Count lit orbs each cycle: 0, 1, 2, 3, 0.
- **Pass criteria:** Exactly N orbs are lit (colored `litColor`) when `WaterTier == N`, and the update is instant (within the same frame as the O key press).

### HUD-04 — Danger vignette (CONTEXT.md override: corruption ratio, not HP ratio)

**How to verify:**
- This is validated as part of HUD-02 above. HUD-04 in REQUIREMENTS.md described an HP-based red vignette; CONTEXT.md D-02/D-03/D-04/D-15 replace it entirely with the corruption-ratio vignette. No HP-based vignette is implemented.
- **Pass criteria:** See HUD-02 validation above. D-15 explicitly states "HP 단독 비네트 없음."

### TECH-01 — ISkill cooldown properties accessible

**How to verify:**
- Add a temporary debug `OnGUI` block (or expand the existing one in `PlayerController`) that reads `(basicAttack as ISkill).CooldownRemaining` and `CooldownDuration` and displays them.
- Trigger a basic attack. Observe `CooldownRemaining` counting down from `cooldownDuration` to 0 in real time.
- **Pass criteria:** `CooldownRemaining` is 0 when not on cooldown, counts down from `CooldownDuration` during cooldown, and `CooldownDuration` matches the `cooldownDuration` SerializeField value shown in the Inspector. All three skills compile without error.

---

## RESEARCH COMPLETE
