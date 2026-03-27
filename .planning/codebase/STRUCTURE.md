# Codebase Structure

**Analysis Date:** 2026-03-27

## Directory Layout

```
My project/                          # Unity project root
├── Assets/                          # All game content and scripts
│   ├── Player/                      # Player scripts
│   ├── Enemy/                       # Enemy scripts
│   ├── GameScript/                  # Shared game systems (camera, game state, interfaces)
│   ├── Prefabs/                     # Unity prefab assets
│   ├── Scenes/                      # Unity scene files
│   ├── Settings/                    # URP renderer and scene template settings
│   │   └── Scenes/                  # Lighting settings per scene
│   └── ImportedAssets/              # Third-party purchased/downloaded assets
│       └── Hero Knight - Pixel Art/ # Pixel art character + animation set
│           ├── Animations/
│           ├── ColorSwap/
│           ├── Demo/                # Reference demo scripts (HeroKnight.cs)
│           ├── Environment/
│           └── Sprites/
├── Packages/                        # Unity Package Manager manifest
├── ProjectSettings/                 # Unity project-wide settings (input, physics, etc.)
├── Library/                         # Unity-generated cache (do not commit)
├── Logs/                            # Unity editor logs (do not commit)
├── Temp/                            # Build temp files (do not commit)
├── UserSettings/                    # Per-user editor preferences (do not commit)
├── BleedingSpring/                  # Nested git repo / submodule (separate project)
├── .planning/                       # GSD planning documents
│   └── codebase/                    # Codebase analysis docs (ARCHITECTURE.md, STRUCTURE.md, etc.)
├── .claude/                         # Claude/GSD tooling configuration
├── .vscode/                         # VS Code workspace settings
├── Assembly-CSharp.csproj           # Generated C# project file
├── My project.sln                   # Visual Studio solution
└── .gitignore                       # Unity standard gitignore
```

## Directory Purposes

**`Assets/Player/`:**
- Purpose: All scripts responsible for player character behavior
- Contains: Controller, movement, input, stats, skill base, three concrete skills, projectile, state enum, player context interface
- Key files:
  - `Assets/Player/PlayerController.cs` — top-level orchestrator, implements `IPlayerContext`
  - `Assets/Player/PlayerMovement.cs` — physics movement, jump, dash
  - `Assets/Player/InputHandler.cs` — Unity Input System wrapper, exposes C# events
  - `Assets/Player/PlayerWaterStats.cs` — water (HP) + corruption resource management
  - `Assets/Player/SkillBase.cs` — abstract skill base (Template Method)
  - `Assets/Player/BasicAttackSkill.cs` — [J] melee box attack
  - `Assets/Player/WideSlashSkill.cs` — [U] wide slash skill
  - `Assets/Player/ProjectileSkill.cs` — [I] ranged/laser skill
  - `Assets/Player/Projectile.cs` — projectile runtime behavior
  - `Assets/Player/ISkill.cs` — skill interface contract
  - `Assets/Player/IPlayerContext.cs` — player context interface (used by movement and skills)
  - `Assets/Player/PlayerState.cs` — `PlayerState` enum (Idle/Moving/Jumping/Falling/Dashing/Attacking/Dead)
  - `Assets/Player/PlayerStats.cs` — legacy HP-only stats class (superseded by `PlayerWaterStats`, pending removal)
  - `Assets/Player/PlayerCombat.cs` — legacy combat stub (superseded by individual skill files, pending deletion)

**`Assets/Enemy/`:**
- Purpose: All scripts for enemy characters
- Contains: AI state machine, attack logic, dual-resource stats, NPC transition on purification
- Key files:
  - `Assets/Enemy/EnemyAI.cs` — FSM: Idle/Patrol/Chase/Attack/Hit/Dead
  - `Assets/Enemy/EnemyAttack.cs` — attack execution (enemy spends own HP to attack player)
  - `Assets/Enemy/EnemyStats.cs` — HP + corruption stats, purify vs. destroy decision
  - `Assets/Enemy/EnemyState.cs` — `EnemyState` enum
  - `Assets/Enemy/PurifiedNPC.cs` — post-purification NPC behavior and dialogue

**`Assets/GameScript/`:**
- Purpose: Shared game systems that cross player/enemy boundaries
- Contains: Global game state manager, camera, shared interfaces
- Key files:
  - `Assets/GameScript/GameStateManager.cs` — singleton, `GameState` enum, `Time.timeScale` control
  - `Assets/GameScript/CameraFollow.cs` — smooth follow camera
  - `Assets/GameScript/IDamageable.cs` — shared damage interface (`TakeDamage(float, float)`)

**`Assets/Prefabs/`:**
- Purpose: Reusable Unity prefab assets
- Contains: `AttackRangeIndicator.prefab` — visual box shown during skill hitbox preview

**`Assets/Scenes/`:**
- Purpose: Unity scene files
- Contains: `SampleScene.unity` — the single active game scene

**`Assets/Settings/`:**
- Purpose: Unity URP (Universal Render Pipeline) rendering configuration
- Contains: `UniversalRP.asset`, `Renderer2D.asset`, `Lit2DSceneTemplate.scenetemplate`
- Generated: Partially (URP defaults), hand-configured

**`Assets/ImportedAssets/Hero Knight - Pixel Art/`:**
- Purpose: Third-party pixel art character asset pack used for player visuals and animation
- Contains: Animator controller, sprites, demo scripts (`HeroKnight.cs`, `Sensor_HeroKnight.cs`, `DestroyEvent_HeroKnight.cs`), color swap shader
- Note: Demo scripts in `Demo/` are reference only; do not modify. Production player uses `PlayerController.cs`, not `HeroKnight.cs`.

**`Assets/InputSystem_Actions.cs`:**
- Purpose: Auto-generated C# class from Unity Input System `.inputactions` asset
- Generated: Yes — do not edit by hand

**`Packages/`:**
- Purpose: Unity Package Manager dependency manifest
- Key files: `manifest.json` (package list), `packages-lock.json` (locked versions)

**`ProjectSettings/`:**
- Purpose: Unity project configuration (input bindings, physics layers, tags, quality settings)
- Generated: Partially — committed to version control

**`.planning/codebase/`:**
- Purpose: GSD codebase analysis documents consumed by `/gsd:plan-phase` and `/gsd:execute-phase`
- Generated: No (manually produced by GSD mapping agents)
- Committed: Yes

## Key File Locations

**Entry Points:**
- `Assets/Scenes/SampleScene.unity`: Only scene; contains all game objects

**Core Player Logic:**
- `Assets/Player/PlayerController.cs`: Player orchestrator and state machine
- `Assets/Player/PlayerMovement.cs`: All physics movement
- `Assets/Player/InputHandler.cs`: Input → event bridge
- `Assets/Player/PlayerWaterStats.cs`: Player resource system (HP + corruption)

**Core Enemy Logic:**
- `Assets/Enemy/EnemyAI.cs`: Enemy behavior state machine
- `Assets/Enemy/EnemyStats.cs`: Enemy resource system with purify/destroy logic

**Shared Contracts:**
- `Assets/GameScript/IDamageable.cs`: Universal damage interface
- `Assets/Player/ISkill.cs`: Skill interface
- `Assets/Player/IPlayerContext.cs`: Player context interface

**Game State:**
- `Assets/GameScript/GameStateManager.cs`: Global state singleton

**Skill Implementations:**
- `Assets/Player/SkillBase.cs`: Abstract skill base
- `Assets/Player/BasicAttackSkill.cs`: Melee
- `Assets/Player/WideSlashSkill.cs`: Wide slash
- `Assets/Player/ProjectileSkill.cs`: Projectile / laser

**Configuration:**
- `Packages/manifest.json`: Unity package dependencies
- `ProjectSettings/`: Physics layers, tags, Input System bindings

## Naming Conventions

**Files:**
- PascalCase for all C# scripts matching their class name: `PlayerController.cs`, `EnemyAI.cs`
- Interface files prefixed with `I`: `ISkill.cs`, `IPlayerContext.cs`, `IDamageable.cs`
- Enum files named after the enum: `PlayerState.cs`, `EnemyState.cs`
- Skill files suffixed with `Skill`: `BasicAttackSkill.cs`, `WideSlashSkill.cs`, `ProjectileSkill.cs`

**Classes:**
- PascalCase: `PlayerController`, `EnemyAI`, `SkillBase`
- Interfaces prefixed `I`: `ISkill`, `IPlayerContext`, `IDamageable`
- Enums PascalCase: `PlayerState`, `EnemyState`, `GameState`

**Members:**
- Private fields: camelCase with no prefix: `moveInput`, `attackCombo`, `patrolCenter`
- Public properties: PascalCase: `FacingRight`, `CurrentState`, `IsGrounded`
- Serialized Inspector fields: `[SerializeField] private` camelCase
- Events: `On` prefix PascalCase: `OnDeath`, `OnDamaged`, `OnWaterChanged`

**Directories:**
- PascalCase by domain: `Player/`, `Enemy/`, `GameScript/`

## Where to Add New Code

**New Player Skill:**
- Implementation: `Assets/Player/` — create `YourSkillName.cs` extending `SkillBase`, implement `ExecuteSkill()` coroutine
- Wire-up: Add component to player GameObject; register in `PlayerController.Awake()` and `Start()` (add `inputHandler.OnYourAction` event)
- Input binding: Add action in Unity Input System `.inputactions` asset, regenerate `InputSystem_Actions.cs`

**New Enemy Type:**
- Primary code: `Assets/Enemy/` — new scripts or subclass `EnemyAI` behavior
- Reuse: `EnemyStats.cs` and `EnemyAttack.cs` are component-based and work on any GameObject

**New Game System (UI, inventory, dialogue):**
- Implementation: `Assets/GameScript/` — place cross-cutting systems here
- Subscribe to `GameStateManager.OnGameStateChange` to react to state transitions

**New Shared Interface:**
- Location: `Assets/GameScript/` (e.g., alongside `IDamageable.cs`)

**New Prefab:**
- Location: `Assets/Prefabs/`

**New Scene:**
- Location: `Assets/Scenes/`
- Register in `ProjectSettings/EditorBuildSettings.asset`

## Special Directories

**`Library/`:**
- Purpose: Unity-generated import cache and artifacts
- Generated: Yes
- Committed: No (in `.gitignore`)

**`Temp/`:**
- Purpose: Build-time temporary files
- Generated: Yes
- Committed: No

**`UserSettings/`:**
- Purpose: Per-machine editor preferences (layout, recent projects)
- Generated: Yes
- Committed: No

**`BleedingSpring/`:**
- Purpose: Nested git repository — appears to be a separate sub-project or submodule
- Generated: No
- Committed: Has its own `.git` directory; treat as independent repo

**`Assets/ImportedAssets/`:**
- Purpose: Third-party asset store content kept separate from first-party code
- Generated: No (imported manually)
- Committed: Yes (binary assets tracked)

---

*Structure analysis: 2026-03-27*
