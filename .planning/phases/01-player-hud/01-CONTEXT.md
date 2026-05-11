# Phase 1 Context: Player HUD (Revised)

## Core Vision: "Shrinking Survival Container"
The HUD is not just a static gauge; it represents the player's physical survival space. As "Clear Water" (HP/Resource) is consumed, the HUD itself shrinks, and the "Corruption" (Death) fills this shrinking space from the bottom up.

## Implementation Decisions

### 1. HUD Positioning & Structure
- **Main Gauge (Water/Corruption)**:
    - **Type**: World Space (Follows Player).
    - **Orientation**: **Vertical Bar** positioned slightly behind/beside the player character.
    - **Layout**: The container's height scale is dynamic.
- **Water Tier Orbs**:
    - **Positioning**: Separated from the main bar. Located **directly above the player character's head**.
    - **Visualization**: 3 distinct orbs indicating the current Tier (0-3).

### 2. "Shrinking" Logic & Visuals
- **The Container (Water/HP)**:
    - The total visible height of the HUD bar represents `CurrentCleanWater`.
    - `Height Scale = CurrentCleanWater / MaxCleanWater`.
    - **Transparency**: The empty space (gap between Max and Current Water) must be **completely transparent**.
- **The Fill (Corruption)**:
    - Corruption fills from the bottom of the *current* container.
    - `Fill Amount = CurrentCorruption / CurrentCleanWater`.
    - **Death Visual**: When the Corruption fill reaches the top of the shrunken bar (100%), the player dies.
- **80%+ Corruption Warning**:
    - Trigger: `CurrentCorruption / CurrentCleanWater >= 0.8`.
    - Effect: Pulse the corruption color (Red/Purple) or add a glitch/shaking effect to the shrinking bar.

### 3. Safety Net (Vignette)
- **Low HP Warning**:
    - Trigger: `CurrentCleanWater / MaxCleanWater < 0.25`.
    - Effect: Red pulsed screen-space vignette (independent of the world-space HUD).

## Technical Requirements
- **Dynamic Scaling**: The `RectTransform` height or `Transform.localScale.y` must be updated based on `OnWaterChanged`.
- **Relative Filling**: The corruption `Image.fillAmount` must be calculated relative to `CurrentCleanWater`, not `MaxCleanWater`.
- **Orb Positioning**: Ensure the orbs follow the player head position consistently regardless of the shrinking bar's state.

## Success Criteria for Downstream Agents
- [ ] HUD bar physically shortens when water is spent (skills/damage).
- [ ] Corruption fills the *shortened* bar correctly.
- [ ] Empty water space is invisible/transparent.
- [ ] Tier orbs are centered above the player's head.
- [ ] All events (`OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged`) are correctly subscribed and unsubscribed.

---
*Created: 2026-04-06 — Context revised based on user vision for a shrinking survival HUD.*
