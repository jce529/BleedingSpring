# Codebase Structure

**Analysis Date:** 2025-02-13

## Directory Layout

```
Assets/
├── Enemy/          # Enemy AI, Stats, and Logic
├── GameScript/     # Global Managers, Camera, Shared Interfaces
├── ImportedAssets/ # Third-party assets (e.g., Pixel Art)
├── Player/         # Player Controller, Movement, Combat, Skills
├── Prefabs/        # Reusable Unity GameObjects
├── Scenes/         # Unity Scene files (.unity)
├── Settings/       # URP and Input System configurations
├── Tests/          # NUnit Tests (Editor and PlayMode)
└── UI/             # UI Components, HUDs, and UI Managers
```

## Directory Purposes

**Enemy/:**
- Purpose: Contains all logic related to enemy behavior and purification.
- Contains: AI state machines, enemy stats, and world-space UI.
- Key files: `EnemyAI.cs`, `EnemyStats.cs`, `EnemyAttack.cs`.

**Player/:**
- Purpose: Core logic for the player character.
- Contains: Controller, movement, skill implementations, and resource management.
- Key files: `PlayerController.cs`, `PlayerWaterStats.cs`, `SkillBase.cs`.

**GameScript/:**
- Purpose: Shared logic and global managers.
- Contains: Singletons and core interfaces used by multiple systems.
- Key files: `GameStateManager.cs`, `IDamageable.cs`.

**UI/:**
- Purpose: User interface management.
- Contains: HUD elements for player and boss, and UI-specific managers.
- Key files: `BossUIManager.cs`, `PlayerHUDBar.cs`.

**Tests/:**
- Purpose: Automated verification of code logic.
- Contains: Editor and Runtime tests.
- Key files: `Assets/Tests/Editor/BossUIManagerTests.cs`.

## Key File Locations

**Entry Points:**
- `Assets/Player/PlayerController.cs`: Main entry for player logic.
- `Assets/GameScript/GameStateManager.cs`: Main entry for global game flow.

**Configuration:**
- `Assets/InputSystem_Actions.inputactions`: Input mapping definitions.
- `Assets/Settings/`: URP and Quality settings.

**Core Logic:**
- `Assets/Player/SkillBase.cs`: Base for all player skills.
- `Assets/Enemy/EnemyAI.cs`: Base FSM for enemies.

**Testing:**
- `Assets/Tests/`: All test files are located here.

## Naming Conventions

**Files:**
- PascalCase: `PlayerController.cs`, `EnemyStats.cs`.
- Interfaces start with 'I': `ISkill.cs`, `IDamageable.cs`.

**Directories:**
- PascalCase: `GameScript`, `Player`.

## Where to Add New Code

**New Feature (Player Action):**
- Primary code: `Assets/Player/` (create a new class inheriting from `SkillBase`).
- Tests: `Assets/Tests/`.

**New Enemy Type:**
- Implementation: `Assets/Enemy/` (attach `EnemyAI` and `EnemyStats` components).

**New UI Screen:**
- Implementation: `Assets/UI/` (create a manager or HUD component).

**Shared Utility:**
- Shared helpers: `Assets/GameScript/`.

## Special Directories

**ImportedAssets/:**
- Purpose: Contains external packages and assets.
- Generated: No (externally provided).
- Committed: Yes.

**Settings/:**
- Purpose: ScriptableObject-based configurations.
- Generated: No.
- Committed: Yes.

---

*Structure analysis: 2025-02-13*
