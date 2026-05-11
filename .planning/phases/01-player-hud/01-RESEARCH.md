# Phase 1: Player HUD — Research

**Researched:** 2026-03-30
**Updated:** 2026-04-03
**Phase:** 01-player-hud
**Requirements covered:** HUD-01, HUD-02, HUD-03, HUD-04, TECH-01

---

## Summary

Phase 1 builds three distinct visual systems on top of the existing `PlayerWaterStats` event infrastructure: (1) a World Space vertical bar attached to the player that unifies water/HP and corruption into a single dual-fill gauge with three orb indicators above it, (2) a Screen Space red vignette that pulses when HP is low (< 25%), and (3) two code-level changes — updating the death condition to a ratio-based logic (`Corruption / HP`) and extending `ISkill`/`SkillBase` with cooldown-tracking properties. All UI data flows from existing `Action` events (`OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged`).

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
4. Because `scale.x` inversion propagates to children, the bar automatically mirrors to the correct arm side on every flip.
5. Set Canvas `sortingLayerName` to the UI layer (or a dedicated "PlayerHUD" layer above "Player"). Set `orderInLayer` to a value above the player sprite.

---

## 2. Vertical Bar with Dual Fill

### Design goal

A single vertical bar where:
- **Water (blue)** fills from the top downward, representing `CurrentCleanWater / maxCleanWater`.
- **Corruption (dark purple/black)** fills from the bottom upward, representing `CurrentCorruption / CurrentCleanWater` (the death ratio).
- The two regions share the same bar space — when corruption meets water, the player dies.

### Recommended: Two stacked Images

**Structure under the bar background Image:**
```
BarBackground (Image — dark outline/bg)
├── WaterFill   (Image, Fill Method: Vertical, Fill Origin: Top)
└── CorruptionFill (Image, Fill Method: Vertical, Fill Origin: Bottom)
```

**Fill amount calculations:**
```csharp
waterImage.fillAmount = currentCleanWater / maxCleanWater;

float corruptionRatio = currentCleanWater > 0f
    ? Mathf.Clamp01(currentCorruption / currentCleanWater)
    : 1f;
corruptionImage.fillAmount = corruptionRatio;
```

---

## 3. Water Tier Orb Display

### Design goal

Three orbs above the vertical bar. Orbs 1–N light up based on `WaterTier` (0–3).

### Recommended: Image color toggle
- Use `litColor` and `dimColor` serialized in the inspector.
- Update in `OnWaterTierChanged(int tier)` handler.

---

## 4. Vignette Implementation

### Design Goal (Updated 2026-04-03)
- **Trigger**: Red pulsed vignette when HP < 25% (`CurrentCleanWater / MaxCleanWater < 0.25`).
- **Color**: Red (#FF0000).
- **Purpose**: Signals traditional death risk.

### Implementation: Screen Space Overlay Canvas + fullscreen radial gradient Image
- Matches the existing UGUI-only constraint.
- Monitored via `OnWaterChanged` event.
- Set Image `Raycast Target = false`.

---

## 5. Pulse Animation & Bar VFX

### Vignette Pulse (Low HP)
- Speed: Consistent pulse (e.g., 1.5s period).
- `Mathf.PingPong` modulation of the Red image alpha.

### Corruption Bar Dynamic VFX (80%+)
- **Trigger**: `현재 오염 비율 (Corruption / CurrentHP)`이 0.8 (80%) 이상일 때.
- **Visuals**:
    - **맥동 (Pulse)**: 바의 스케일이나 알파를 빠르게 변동시켜 경고.
    - **Recommended**: `corruptionFillImage.color`를 보라색과 밝은 빨간색 사이에서 빠르게 점멸(Blink)시키거나 바 자체를 미세하게 진동(Shake)시킴.

---

## 6. Death Condition Change

### Required change
- **Logic**: `CurrentCorruption >= CurrentCleanWater`.
- Update `PlayerWaterStats.CheckDeath()` to use this comparison.
- Update debug logs to reflect "오염도 >= 현재 HP".

---

## 7. ISkill Interface Extension

### Required additions
```csharp
float CooldownRemaining { get; }
float CooldownDuration  { get; }
```

### SkillBase implementation
- Replace `WaitForSeconds` with a `while` loop decrementing `_cooldownRemaining` by `Time.deltaTime` each frame inside `UseCoroutine`.

---

## 8. Event Subscription Pattern
- Subscribe in `Start`, unsubscribe in `OnDestroy`.
- Initialize UI state in `Start` using current stat values.

---

## 9. Direction Tracking
- Child Canvas inherits `parent.scale.x` flip automatically.
- No script needed for basic positioning.

---

## 10. Unity 6 URP Considerations
- Use `linearVelocity` for any physics interactions.
- World Space Canvas sorting via `Sorting Layer`.

---

## Validation Architecture

### HUD-01 — Corruption bar reacts to `OnCorruptionChanged`
- **Pass criteria:** CorruptionFill `fillAmount` reflects `CurrentCorruption / CurrentCleanWater`.

### HUD-02 — 80% Corruption Warning
- **Pass criteria:** Bar pulses/blinks when `Corruption / CurrentHP >= 0.8`.

### HUD-03 — Water tier orbs update
- **Pass criteria:** Correct number of orbs light up (0-3).

### HUD-04 — Danger vignette (Low HP)
- **Pass criteria:** Red vignette pulses ONLY when `HP < 25%`.

### TECH-01 — ISkill cooldown properties
- **Pass criteria:** `CooldownRemaining` counts down to 0 correctly.
