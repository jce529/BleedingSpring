# Architecture

**Analysis Date:** 2025-02-13

## Pattern Overview

**Overall:** Component-Based Orchestration (Unity)

**Key Characteristics:**
- **Orchestrator Pattern:** Complex entities like the Player are managed by a central `PlayerController` that coordinates sub-components via context injection.
- **Event-Driven Communication:** Components interact primarily through C# Actions/Events to maintain loose coupling (e.g., Stats notifying UI, Input notifying Controller).
- **Strategy & Template Method Patterns:** Combat skills are implemented as interchangeable strategies inheriting from a common base that defines the execution template.

## Layers

**Global Management:**
- Purpose: Manages global game state, time scale, and cross-scene persistence.
- Location: `Assets/GameScript/`
- Contains: `GameStateManager.cs`
- Depends on: None
- Used by: All systems needing to check or change game state (e.g., `PlayerWaterStats` for Game Over).

**Entity Orchestration:**
- Purpose: Coordinates movement, input, and actions for complex entities.
- Location: `Assets/Player/`, `Assets/Enemy/`
- Contains: `PlayerController.cs`, `EnemyAI.cs`
- Depends on: Sub-components (`PlayerMovement`, `InputHandler`), Stats, and Skills.
- Used by: Unity Engine (entry points for update loops).

**Action/Skill Logic:**
- Purpose: Defines specific behaviors or attacks that can be triggered by entities.
- Location: `Assets/Player/`
- Contains: `ISkill.cs`, `SkillBase.cs`, `BasicAttackSkill.cs`, `ProjectileSkill.cs`
- Depends on: `IPlayerContext` for entity state access.
- Used by: `PlayerController.cs`

**State & Data:**
- Purpose: Holds the "truth" about entity status (HP, Corruption, Tiers).
- Location: `Assets/Player/`, `Assets/Enemy/`
- Contains: `PlayerWaterStats.cs`, `EnemyStats.cs`
- Depends on: None (standalone data/logic providers)
- Used by: Orchestrators for decision making, UI for display.

## Data Flow

**Input to Action Flow:**

1. **Input Detection:** `InputHandler.cs` captures Unity Input System events.
2. **Event Broadcast:** `InputHandler` invokes C# events (e.g., `OnBasicAttack`).
3. **Orchestration:** `PlayerController` receives the event and checks the current `PlayerState`.
4. **Action Execution:** If valid, `PlayerController` calls `TryUse()` on the relevant `ISkill`.
5. **State Update:** The skill (via `SkillBase`) sets the player to `Attacking` state and deducts resources from `PlayerWaterStats`.
6. **UI Notification:** `PlayerWaterStats` invokes `OnWaterChanged`, which UI components (`PlayerHUDBar`) listen to.

**State Management:**
- Global state is handled by the `GameStateManager` singleton.
- Entity-local state is handled by state machines within `PlayerController` and `EnemyAI`.

## Key Abstractions

**IPlayerContext:**
- Purpose: Provides a restricted view of the `PlayerController` to its sub-components, preventing circular dependencies.
- Examples: `Assets/Player/IPlayerContext.cs`
- Pattern: Interface Segregation / Context Object.

**ISkill / SkillBase:**
- Purpose: Abstraction for all player actions, handling cooldowns and state transitions consistently.
- Examples: `Assets/Player/ISkill.cs`, `Assets/Player/SkillBase.cs`
- Pattern: Strategy & Template Method.

**IDamageable:**
- Purpose: Unified interface for any entity that can take damage.
- Examples: `Assets/GameScript/IDamageable.cs`
- Pattern: Interface-based polymorphism.

## Entry Points

**Unity Lifecycle:**
- Location: `Update()`, `FixedUpdate()`, `Start()` in Monobehaviours.
- Triggers: Unity Engine.
- Responsibilities: Driving entity AI (`EnemyAI.Update`), processing input (`PlayerController.Update`), and managing global state.

## Error Handling

**Strategy:** Defensive programming with Null Checks and Logging.

**Patterns:**
- **Null-conditional Operators:** Used for event invocation (`OnDeath?.Invoke()`).
- **Component Validation:** `RequireComponent` attributes and `TryGetComponent` are used to ensure dependencies exist.

## Cross-Cutting Concerns

**Logging:** Standard `Debug.Log` for state transitions and combat events.
**Validation:** Enforced via `Mathf.Clamp` on stats and `RequireComponent` on GameObjects.
**Authentication:** Not applicable (Single-player local state).

---

*Architecture analysis: 2025-02-13*
