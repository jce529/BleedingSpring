# Phase 2: Enemy World Space UI - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-10
**Phase:** 02-enemy-world-space-ui
**Areas discussed:** Visual Density & Layering, Sweet Spot Feedback, Visual Refinements, HP Visibility

---

## Visual Density & Layering

| Option | Description | Selected |
|--------|-------------|----------|
| Standard UI Layer | Standard UI layer, might be occluded by world objects. | ✓ |
| Dynamic Y-Sorting | Bars for closer enemies render above those further away. | ✓ |
| World Proportional Scale | Scale with the parent enemy object. | ✓ |

**User's choice:** Standard UI Layer, Dynamic Y-Sorting, World Proportional.
**Notes:** Grounded look with 2D depth sorting.

---

## Sweet Spot Feedback

| Option | Description | Selected |
|--------|-------------|----------|
| Color Change/Flash | Overlay color changes when in the Sweet Spot. | ✓ |
| Trigger Flash | Immediate flash/glow on entering range. | ✓ |

**User's choice:** Color Change/Flash + Trigger Flash.
**Notes:** Strong visual feedback for tactical timing.

---

## Visual Refinements (Vertical HUD)

| Option | Description | Selected |
|--------|-------------|----------|
| Vertical Bar | Vertical bar behind the monster (like player). | ✓ |
| 0.15s Snap-In | Current fade-in duration. | ✓ |

**User's choice:** Vertical, behind the monster. 0.15s fade.
**Notes:** Massive shift to mirror player's vertical shrinking HUD logic.

---

## HP Visibility

| Option | Description | Selected |
|--------|-------------|----------|
| Combined Indicator | Shrinking bar height represents both HP and Corruption. | ✓ |
| Separate HP Bar | Add a small, separate HP bar. | |

**User's choice:** Combined Indicator.
**Notes:** Minimalist approach; corruption bar serves as the primary health visual.

---

## Claude's Discretion

- Container shrinking based on Current Corruption.
- Purification fill logic: (Max - Current) / Current.
- Specific Z-offset for "behind the monster" positioning.

## Deferred Ideas

- ENM-06 (Purified/Destroyed text popups) - Skipped per user preference.
- Ghost bars (trail effect) - Moved to v2.
