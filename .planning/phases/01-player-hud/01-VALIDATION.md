---
phase: 1
slug: player-hud
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-30
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Play Mode (manual) — no automated test framework in scope |
| **Config file** | none |
| **Quick run command** | Unity Editor Play Mode + observe HUD behavior |
| **Full suite command** | Unity Editor Play Mode — walk through all 5 success criteria |
| **Estimated runtime** | ~2-3 minutes manual walkthrough |

> Note: This phase is a Unity 2D game HUD with no existing test framework. All verification is manual Play Mode inspection. Automated checks are limited to compile-time verification (build succeeds).

---

## Sampling Rate

- **After every task commit:** Unity Editor must open without compile errors
- **After every plan wave:** Run Play Mode and verify that wave's HUD elements work
- **Before `/gsd:verify-work`:** Full success criteria walkthrough in Play Mode
- **Max feedback latency:** ~3 minutes

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Verification | Status |
|---------|------|------|-------------|-----------|--------------|--------|
| Death condition | 01 | 1 | HUD-01/02/04 | compile + play | CheckDeath() fires when corruption >= currentHP | ⬜ pending |
| ISkill interface | 01 | 1 | TECH-01 | compile | Project builds without error; ISkill has CooldownRemaining/CooldownDuration | ⬜ pending |
| SkillBase impl | 01 | 1 | TECH-01 | compile | SkillBase compiles; CooldownRemaining decrements during cooldown | ⬜ pending |
| World Space HUD | 02 | 2 | HUD-01/02/03 | play | Bar appears next to player, flips on direction change | ⬜ pending |
| Water bar fill | 02 | 2 | HUD-01 | play | Water bar decreases when SacrificeWater() called | ⬜ pending |
| Corruption fill | 02 | 2 | HUD-01 | play | Corruption rises from bottom as player takes corruption damage | ⬜ pending |
| Tier orbs | 02 | 2 | HUD-03 | play | O key cycles orb count 0→1→2→3→0 | ⬜ pending |
| Vignette stages | 03 | 3 | HUD-04 | play | Vignette appears at ratio 25/50/75% with correct colors | ⬜ pending |
| Vignette pulse | 03 | 3 | HUD-04 | play | Pulse speed increases at each stage | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

None — no test framework to install. All verification is Play Mode manual inspection.

*Existing infrastructure (Unity Editor) covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| World Space HUD follows player and flips | HUD-01/02/03 | Unity Play Mode spatial behavior | Enter Play Mode, observe bar position. Press movement keys to flip character, verify bar flips to same arm side |
| Corruption eats water from below in bar | HUD-01/02 | Visual fill behavior | Trigger ReceiveAttack() via debug or taking enemy damage. Observe corruption rising from bottom of bar |
| O key orb update | HUD-03 | Input event + UI update | Press O key in Play Mode, count orbs lighting up |
| Vignette color stages | HUD-04 | URP fullscreen visual | Use inspector to manually set corruption/HP to force ratio 0.3, 0.6, 0.8. Observe vignette color (lavender/indigo/purple) |
| Vignette pulse speed changes | HUD-04 | Timing perception | Hold at each ratio stage and observe pulse speed (slow/medium/fast) |
| Death fires at corruption == currentHP | HUD-01 | Gameplay loop | Set HP=50, manually add corruption to 50 via debug. Player should die |
| ISkill CooldownRemaining decrements | TECH-01 | Runtime value | Add debug log or inspector watch; use skill and observe CooldownRemaining value counting down |

---

## Validation Sign-Off

- [ ] All tasks produce no compile errors after each commit
- [ ] Unity Play Mode walkthrough covers all 5 success criteria
- [ ] Death condition change verified in Play Mode
- [ ] ISkill compile-time verification passes
- [ ] HUD bar flips correctly with player direction
- [ ] Vignette shows correct colors at 25/50/75% ratio
- [ ] `nyquist_compliant: true` set in frontmatter when all items checked

**Approval:** pending
