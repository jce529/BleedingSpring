---
phase: 2
slug: enemy-world-space-ui
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-06
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Play Mode (manual UAT) — no automated test framework in project |
| **Config file** | none — Play Mode only |
| **Quick run command** | Open Unity Editor → confirm zero compile errors |
| **Full suite command** | Enter Play Mode; trigger damage, purify, destroy scenarios; inspect console |
| **Estimated runtime** | ~5 minutes manual |

---

## Sampling Rate

- **After every task commit:** Open Unity Editor, confirm zero compile errors
- **After every plan wave:** Enter Play Mode; test damage trigger, death trigger, visual correctness
- **Before `/gsd:verify-work`:** Full scenario (damage → bars appear → purify → no NRE → destroy → bars gone)
- **Max feedback latency:** ~5 minutes

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| TECH-02 | 01 | 1 | TECH-02 | Compile check | Open Unity, zero compile errors | ❌ W0 | ⬜ pending |
| ENM-01 | 01 | 2 | ENM-01 | Manual Play Mode | N/A — visual follow test | ❌ W0 | ⬜ pending |
| ENM-02 | 01 | 2 | ENM-02 | Manual Play Mode | N/A — hit/death visibility test | ❌ W0 | ⬜ pending |
| ENM-03 | 01 | 2 | ENM-03 | Manual Play Mode | N/A — corruption bar update | ❌ W0 | ⬜ pending |
| ENM-04 | 01 | 2 | ENM-04 | Manual + Inspector | Check anchorMin/Max via Inspector | ❌ W0 | ⬜ pending |
| ENM-05 | 01 | 3 | ENM-05 | Manual Play Mode | N/A — multi-enemy type check | ❌ W0 | ⬜ pending |
| TECH-03 | 01 | 3 | TECH-03 | Manual Play Mode | Purify enemy → no NRE in console | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `02-HUMAN-UAT.md` — manual test script covering all 7 behaviors (match Phase 1 pattern of `01-HUMAN-UAT.md`)
- No automated test files needed — project uses manual Play Mode verification consistent with Phase 1

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| HP bar follows moving enemy | ENM-01 | Visual position tracking; no Unity Test Runner | Play Mode: move enemy, confirm bar stays above head |
| Bars appear on first hit only | ENM-02 | Event-triggered visual; requires Play Mode input | Hit enemy with attack; confirm bars hidden before hit, visible after |
| Corruption bar updates | ENM-03 | Visual update; no automation | Trigger corruption change; confirm bar fills correctly |
| Sweet Spot anchor at correct fraction | ENM-04 | RectTransform Inspector verification | Inspector: confirm anchorMin.x == basePurificationMin, anchorMax.x == basePurificationMax |
| Different types show different Sweet Spot | ENM-05 | Requires multiple enemy configs in scene | Two enemy types in scene; confirm different overlay positions |
| No event leak on Purify path | TECH-03 | Runtime memory safety; requires Purify scenario | Purify enemy → NPC conversion → no NullReferenceException in console |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5 minutes
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
