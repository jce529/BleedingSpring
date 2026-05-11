# Codebase Concerns

**Analysis Date:** 2024-10-24

## Tech Debt

**Unused Stat System:**
- Issue: `PlayerStats.cs` is redundant and has been replaced by `PlayerWaterStats.cs`.
- Files: `Assets/Player/PlayerStats.cs`
- Impact: Confusion for developers; potential for mixing up old and new systems.
- Fix approach: Remove `PlayerStats.cs` and ensure all references are updated to `PlayerWaterStats.cs`.

**Fragile HUD Inheritance:**
- Issue: `EnemyWorldSpaceUI` inherits from `PlayerHUDBar` but overrides `Awake` and `Start` without calling `base`.
- Files: `Assets/Enemy/EnemyWorldSpaceUI.cs`, `Assets/Player/PlayerHUDBar.cs`
- Impact: Base class initialization is skipped, which may lead to bugs if the base class is updated with essential logic.
- Fix approach: Refactor common HUD logic into a base class that doesn't assume a specific stat component, or use composition.

**Stubbed Logic:**
- Issue: "TODO: 파괴 이펙트, 패널티 처리" (Destruction effect, penalty processing) remains in `EnemyStats.cs`.
- Files: `Assets/Enemy/EnemyStats.cs`
- Impact: Enemies just disappear when they die without visual feedback or game consequences for purification failure.
- Fix approach: Implement death particles and global penalty systems (e.g., increasing world corruption).

## Performance Bottlenecks

**Object Instantiation Spam:**
- Problem: `SpawnAfterimages` coroutine instantiates and destroys ghost objects every 0.03s during dash.
- Files: `Assets/Player/PlayerMovement.cs`
- Cause: Frequent memory allocation and garbage collection.
- Improvement path: Implement an Object Pool for dash afterimages.

**Frequent Distance Checks:**
- Problem: `EnemyAI` checks distance to player every frame in `Update`.
- Files: `Assets/Enemy/EnemyAI.cs`
- Cause: `DistToPlayer()` uses `Vector2.Distance` (square root operation) inside `UpdateIdle`, `UpdatePatrol`, `UpdateChase`, etc.
- Improvement path: Use `sqrMagnitude` for distance comparisons and consider a lower frequency for AI environment sensing (e.g., using a timer or `FixedUpdate`).

**Global Object Searching:**
- Problem: `FindFirstObjectByType<PlayerWaterStats>()` is used in `Start()` or on demand.
- Files: `Assets/Enemy/EnemyAttack.cs`, `Assets/UI/Boss/BossHUDBar.cs`
- Cause: Lack of a central reference for the player instance.
- Improvement path: Cache the player reference in a `GameManager` or a `ServiceLocator`.

## Fragile Areas

**Ambiguous Interface Semantics:**
- Files: `Assets/GameScript/IDamageable.cs`, `Assets/Player/PlayerWaterStats.cs`, `Assets/Enemy/EnemyStats.cs`
- Why fragile: The `corruptionDamage` parameter in `TakeDamage` means "increase corruption" for players but "decrease corruption" for enemies.
- Safe modification: Rename parameters to be more explicit (e.g., `corruptionDelta`) or split the interface.
- Test coverage: Gaps in edge cases where both HP and corruption reach thresholds simultaneously.

**String-based Tagging:**
- Files: `Assets/Enemy/BossRoomTrigger.cs`, `Assets/Enemy/EnemyAI.cs`
- Why fragile: Hardcoded "Player" tag strings are prone to typos and make renaming difficult.
- Safe modification: Use a constant or a `LayerMask`.

## Scaling Limits

**HUD Tight Coupling:**
- Current capacity: Handles Player, Enemy, and Boss HUDs.
- Limit: Adding new types of entities with different stat structures (e.g., destructible objects with only HP) requires modifying the hierarchy.
- Scaling path: Decouple HUD bars from specific stat classes using a generic `IStatsProvider` or similar interface.

## Test Coverage Gaps

**Stat Edge Cases:**
- What's not tested: Concurrent death and purification; corruption going negative (over-purification).
- Files: `Assets/Enemy/EnemyStats.cs`
- Risk: Critical gameplay bugs where enemies might be purified and destroyed at the same time.
- Priority: Medium

---

*Concerns audit: 2024-10-24*
