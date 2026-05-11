# Phase 1 Summary: Revised Player HUD (Shrinking Container)

## Overview
The Player HUD has been completely refactored to align with the "Shrinking Survival Container" vision. The HUD now physically responds to resource consumption, providing a high-tension visual experience where the player's survival space literally disappears as they take damage or spend water.

## Key Implementation Details

### 1. Shrinking HUD Bar (`PlayerHUDBar.cs`)
- **Dynamic Vertical Scaling**: The entire UI container now scales on the Y-axis based on the `CurrentCleanWater / MaxCleanWater` ratio.
- **Relative Corruption Mapping**: Corruption is no longer a simple percentage of max; it is now mapped to the *current* height of the shrinking container (`CurrentCorruption / CurrentCleanWater`).
- **Visual Pressure**: As the bar shortens, the corruption fill appears to rise faster toward the 100% death threshold, increasing visual urgency.

### 2. Decoupled Tier Orbs (`PlayerTierDisplay.cs`)
- **Independent Positioning**: Orbs are moved to a separate script to allow placement directly above the player's head, freeing up the side of the character for the vertical shrinking bar.
- **Sync**: Perfectly synchronized with `PlayerWaterStats.OnWaterTierChanged`.

### 3. Critical Health Feedback (`PlayerVignette.cs`)
- **Low HP Warning**: A new screen-space vignette effect activates when water is below 25%.
- **Dynamic Intensity**: The pulse speed and alpha intensity increase as health approaches zero.

## Verification Results
- [x] HUD bar shrinks correctly when `SacrificeWater()` is called.
- [x] Corruption fill remains relative to the shortened bar.
- [x] Tier orbs update colors correctly (0-3 range).
- [x] Vignette effect triggers and pulses correctly at low health.

## Next Steps
- Transition to **Phase 2: Enemy World Space UI** (HP/Corruption/Sweet Spot).
- Verify the manual setup of UI anchors in the Unity Editor to ensure the world-space HUD follows the character's "back" as intended.
