---
phase: 01-player-hud
plan: "01"
subsystem: ui
tags: [unity, csharp, player, stats, skill, cooldown]

requires: []
provides:
  - Ratio-based corruption death condition (corruption >= currentHP)
  - ISkill interface with CooldownRemaining and CooldownDuration properties
  - SkillBase per-frame cooldown countdown tracking
affects: [01-02, 01-03]

tech-stack:
  added: []
  patterns:
    - "Ratio-based death: corruption death fires when CurrentCorruption >= CurrentCleanWater (not fixed threshold)"
    - "Cooldown tracking: per-frame while loop with _cooldownRemaining field exposed via interface"

key-files:
  created: []
  modified:
    - Assets/Player/PlayerWaterStats.cs
    - Assets/Player/ISkill.cs
    - Assets/Player/SkillBase.cs

key-decisions:
  - "Death condition changed to ratio-based: corruption >= currentHP (not maxCorruptionThreshold) — aligns with game design where HP is the danger threshold"
  - "CooldownRemaining tracked via per-frame countdown instead of WaitForSeconds — enables live HUD display"

requirements-completed: [TECH-01, HUD-04]

duration: 1 min
completed: 2026-03-30
---

# Phase 1 Plan 01: Core Systems Summary

**Ratio-based corruption death + per-frame skill cooldown tracking exposing CooldownRemaining/CooldownDuration via ISkill interface**

## Performance

- **Duration:** 1 min
- **Started:** 2026-03-30T03:42:01Z
- **Completed:** 2026-03-30T03:43:27Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments

- `PlayerWaterStats.CheckDeath()` now fires when `CurrentCorruption >= CurrentCleanWater` (ratio-based) instead of fixed `maxCorruptionThreshold`
- `ISkill` interface extended with `CooldownRemaining` and `CooldownDuration` properties for HUD consumption
- `SkillBase` implements both properties with a per-frame `_cooldownRemaining` countdown (replaces `WaitForSeconds`)

## Task Commits

1. **Task 01-01-01: Modify PlayerWaterStats death condition** — `eb85c11` (fix)
2. **Task 01-01-02: Extend ISkill interface with cooldown properties** — `d221894` (feat)
3. **Task 01-01-03: Implement cooldown tracking in SkillBase** — `ffbc770` (feat)

## Files Created/Modified

- `Assets/Player/PlayerWaterStats.cs` — CheckDeath() ratio logic + updated XML docs + new debug message
- `Assets/Player/ISkill.cs` — Added CooldownRemaining and CooldownDuration property declarations
- `Assets/Player/SkillBase.cs` — Added _cooldownRemaining field, two new properties, per-frame countdown loop

## Decisions Made

- None — followed plan as specified

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## Next Phase Readiness

- Wave 2 plans (01-02 PlayerHUDBar, 01-03 PlayerVignette) can now proceed — both depend on PlayerWaterStats events and ISkill cooldown interface established here
- Unity Editor must compile without errors before Wave 2 verifications

---
*Phase: 01-player-hud*
*Completed: 2026-03-30*
