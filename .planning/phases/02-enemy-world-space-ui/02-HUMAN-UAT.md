---
status: partial
phase: 02-enemy-world-space-ui
source: [02-VALIDATION.md]
started: 2026-04-06T00:00:00Z
updated: 2026-04-06T00:00:00Z
---

## Current Test
[awaiting human testing]

## Tests

1. **Unity Editor compiles with zero errors**
   - expected: EnemyStats.cs and EnemyWorldSpaceUI.cs compile cleanly with zero errors in Console.
   - Requirement: TECH-02.
   - result: [pending]

2. **HP bar follows moving enemy**
   - expected: In Play Mode, move enemy across the scene; HP bar stays anchored above the enemy sprite head (no lag, no detachment).
   - Requirement: ENM-01.
   - result: [pending]

3. **Bars hidden before first hit, visible after**
   - expected: At scene start, HP/Corruption bars are invisible (CanvasGroup.alpha == 0). After first damage via player attack, bars fade in over ~0.15s.
   - Requirement: ENM-02.
   - result: [pending]

4. **Corruption bar updates in real-time**
   - expected: Trigger corruption change (player attack); Corruption bar fillAmount matches CurrentCorruption / MaxCorruption.
   - Requirement: ENM-03.
   - result: [pending]

5. **Sweet Spot overlay at correct fraction**
   - expected: Pause Play Mode, select enemy in hierarchy, inspect Sweet Spot overlay RectTransform: anchorMin.x equals Clamp01(basePurificationMin - bonusPurificationMargin) and anchorMax.x equals Clamp01(basePurificationMax + bonusPurificationMargin). offsetMin and offsetMax are both (0,0).
   - Requirement: ENM-04.
   - result: [pending]

6. **Different enemy types show different Sweet Spot ranges**
   - expected: Place two enemies with different basePurificationMin/Max values in scene; confirm overlays visually span different widths/positions.
   - Requirement: ENM-05.
   - result: [pending]

7. **No event leak on Purify path (TECH-03)**
   - expected: Lower enemy HP to 0 within purification range so Purify() runs → PurifiedNPC.Activate() is called → EnemyStats.enabled = false → No NullReferenceException or MissingReferenceException appears in the Unity Console. The UI Canvas goes inactive on OnDeath.
   - Requirement: TECH-03.
   - result: [pending]

## Deferred
ENM-06 (PURIFIED/DESTROYED popup text) is deferred per CONTEXT.md D-07 and is NOT tested in this phase.
