---
phase: 01-player-hud
plan: "03"
subsystem: ui
tags: [unity, csharp, ui, screen-space, vignette, player]

requires:
  - phase: 01-01
    provides: PlayerWaterStats events (OnWaterChanged, OnCorruptionChanged)
provides:
  - PlayerVignette MonoBehaviour for screen-space corruption warning vignette
affects: []

tech-stack:
  added: []
  patterns:
    - "Screen Space Overlay vignette: Image with raycastTarget=false on dedicated Canvas"
    - "Event-driven stage recalculation: corruption/HP ratio computed on each stat event"
    - "Per-frame pulse: Mathf.PingPong drives alpha modulation in Update()"

key-files:
  created:
    - Assets/UI/PlayerVignette.cs
  modified: []

key-decisions:
  - "Vignette ratio uses currentCleanWater as denominator (not maxCorruptionThreshold) — consistent with D-02 and D-01 ratio-based death"
  - "Stage computed in event handler, pulse applied in Update() — avoids per-frame division"

requirements-completed: [HUD-02, HUD-04]

duration: 1 min
completed: 2026-03-30
---

# Phase 1 Plan 03: Vignette System Summary

**PlayerVignette MonoBehaviour — 3-stage screen-space pulsing vignette (lavender/indigo/deep purple) at corruption/HP thresholds 25%/50%/75% with per-stage pulse speeds**

## Performance

- **Duration:** 1 min
- **Started:** 2026-03-30T03:45:35Z
- **Completed:** 2026-03-30T03:46:18Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- `Assets/UI/` directory created (new)
- `PlayerVignette.cs` created — subscribes to `OnWaterChanged` and `OnCorruptionChanged`
- 3 color stages: lavender (#E6C8FF), indigo (#3D3580), deep purple (#6B2D8B)
- Pulse speeds: 0.5f / 1.0f / 2.5f per stage
- `raycastTarget = false` set both in Inspector-facing code and Start()
- Divide-by-zero guard when `currentCleanWater == 0`
- `FindFirstObjectByType` fallback when `playerStats` not wired in Inspector

## Task Commits

1. **Task 01-03-01: Create PlayerVignette MonoBehaviour script** — `ebde84d` (feat)

## Files Created/Modified

- `Assets/UI/PlayerVignette.cs` — Screen Space vignette component with 3-stage pulse

## Decisions Made

- None — followed plan as specified

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

Manual Unity Editor scene setup required:
1. Create "VignetteCanvas" GameObject — Canvas: Screen Space - Overlay, Sort Order=100
2. Add "VignetteImage" child Image, anchor stretch all edges
3. Assign radial gradient texture (or white sprite for color-only fallback)
4. Set Raycast Target = false in Inspector
5. Add PlayerVignette component, wire vignetteImage and playerStats references

## Next Phase Readiness

- All 3 plans in Phase 1 complete
- Ready for verifier — Unity Editor compile check required
- Human verification needed: Play Mode testing of HUD bar and vignette behavior

---
*Phase: 01-player-hud*
*Completed: 2026-03-30*
