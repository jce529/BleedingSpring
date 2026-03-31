---
status: partial
phase: 01-player-hud
source: [01-VERIFICATION.md]
started: 2026-03-30T03:47:30Z
updated: 2026-03-30T03:47:30Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Unity Editor compiles with zero errors
expected: All 3 new scripts (PlayerWaterStats.cs changes, PlayerHUDBar.cs, PlayerVignette.cs) compile with no errors
result: [pending]

### 2. HUD Bar: corruption fill rises in real-time
expected: Take corruption damage → corruptionFillImage.fillAmount rises from bottom
result: [pending]

### 3. HUD Bar: water fill decreases in real-time
expected: Take HP damage → waterFillImage.fillAmount decreases from top
result: [pending]

### 4. HUD Bar: tier orbs update on O key
expected: Press O → 0→1→2→3→0 orbs light up immediately
result: [pending]

### 5. HUD Bar: bar follows character direction
expected: Move left → bar mirrors to left side (World Space Canvas in player hierarchy)
result: [pending]

### 6. Vignette: no vignette below 25% ratio
expected: corruption/HP < 0.25 → vignetteImage.color is fully transparent
result: [pending]

### 7. Vignette: lavender stage at 25–49%
expected: ratio 0.25–0.49 → lavender pulse (slow, 0.5f speed)
result: [pending]

### 8. Vignette: indigo stage at 50–74%
expected: ratio 0.50–0.74 → indigo pulse (medium, 1.0f speed)
result: [pending]

### 9. Vignette: deep purple stage at 75%+
expected: ratio ≥ 0.75 → deep purple pulse (fast, 2.5f speed)
result: [pending]

### 10. Vignette: reverts when corruption reduces
expected: Purify → vignette drops to lower stage or disappears
result: [pending]

### 11. Vignette: does not block input
expected: Vignette visible but mouse clicks and movement work normally
result: [pending]

### 12. Death condition: ratio-based trigger
expected: corruption reaches currentHP → death log "오염도 >= 현재 HP -- 즉사 처리"
result: [pending]

### 13. Cooldown tracking: CooldownRemaining counts down
expected: Skill used → CooldownRemaining decreases from CooldownDuration to 0 per frame
result: [pending]

## Summary

total: 13
passed: 0
issues: 0
pending: 13
skipped: 0
blocked: 0

## Gaps
