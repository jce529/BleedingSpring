# 아키텍처

**분석 날짜:** 2026-03-27

## 패턴 개요

**전체:** SOLID 지향 설계가 적용된 Unity 2D 컴포넌트 기반 아키텍처

**주요 특징:**
- 단일 책임 원칙 (SRP): 각 MonoBehaviour는 하나의 관심사를 처리 (움직임, 입력, 통계, 기술)
- 의존성 역전 원칙 (DIP): 서브시스템은 구체적인 클래스가 아닌 인터페이스를 통해 통신 (`IPlayerContext`, `ISkill`, `IDamageable`)
- 인터페이스 분리 원칙 (ISP): `ISkill`과 `IDamageable`은 작고 초점이 맞춰진 계약
- 이벤트 기반 통신: C# `Action` 이벤트는 통계 변화를 UI와 AI 반응으로부터 분리
- 유한 상태 머신 (FSM) 패턴은 플레이어와 적 상태 관리 모두에 사용

## 계층

**입력 계층:**
- 목적: Unity Input System 액션을 타입된 C# 이벤트로 변환
- 위치: `Assets/Player/InputHandler.cs`
- 포함: `InputSystem_Actions` 래퍼, 모든 플레이어 액션의 이벤트 선언
- 의존: `UnityEngine.InputSystem`, `GameStateManager` (입력 게이팅용)
- 사용처: `PlayerController` (모든 이벤트 구독)

**Player Controller Layer:**
- Purpose: Top-level player orchestrator — owns state machine, wires subsystems together
- Location: `Assets/Player/PlayerController.cs`
- Contains: `PlayerState` FSM, input event handlers, skill invocation, combo tracking
- Depends on: `IPlayerContext` (self-implements), `PlayerMovement`, `InputHandler`, `ISkill` implementations, `PlayerWaterStats`
- Used by: `EnemyAI` (FindFirstObjectByType), `InputHandler` (RequireComponent)

**Movement Layer:**
- Purpose: Physics-based movement, jump, dash with afterimage VFX
- Location: `Assets/Player/PlayerMovement.cs`
- Contains: grounded check, dash coroutine (with invincibility frames), flip logic, animator sync
- Depends on: `IPlayerContext` (for state changes and invincibility), `Rigidbody2D`
- Used by: `PlayerController.Tick()`, `TryJump()`, `TryDash()`

**Skill Layer:**
- Purpose: Abstract base + concrete skill implementations for player combat
- Location: `Assets/Player/SkillBase.cs` (abstract base), `Assets/Player/BasicAttackSkill.cs`, `Assets/Player/WideSlashSkill.cs`, `Assets/Player/ProjectileSkill.cs`
- Contains: cooldown management, `Attacking` state lifecycle, box indicator helpers, corruption damage calculation
- Depends on: `IPlayerContext`, `IDamageable` (for hitting enemies)
- Used by: `PlayerController` via `ISkill` interface

**Stats Layer:**
- Purpose: Manage player resources (water = HP, corruption) and trigger death/game-over
- Location: `Assets/Player/PlayerWaterStats.cs`
- Contains: `SacrificeWater()`, `ReceiveAttack()`, `CycleWaterTier()`, `CheckDeath()`, `OnDeath`/`OnWaterChanged`/`OnCorruptionChanged` events
- Depends on: `GameStateManager` (to trigger GameOver)
- Used by: `PlayerController`, `EnemyAttack`, `ProjectileSkill` (3rd stage)

**Enemy Layer:**
- Purpose: Enemy AI state machine, attack logic, and dual-resource (HP + corruption) stats
- Location: `Assets/Enemy/EnemyAI.cs`, `Assets/Enemy/EnemyAttack.cs`, `Assets/Enemy/EnemyStats.cs`
- Contains: `EnemyState` FSM (Idle/Patrol/Chase/Attack/Hit/Dead), purification vs. destruction logic
- Depends on: `IDamageable` (EnemyStats implements it), `PlayerWaterStats`, `PurifiedNPC`
- Used by: player skills (via `IDamageable`), each other via RequireComponent

**NPC Transition Layer:**
- Purpose: Convert a defeated (purified) enemy into an interactive NPC
- Location: `Assets/Enemy/PurifiedNPC.cs`
- Contains: `Activate()` (disables enemy components, changes tag/layer), proximity detection, dialogue display
- Depends on: Unity Physics2D, `EnemyAttack`, `EnemyStats`
- Used by: `EnemyStats.Purify()` (dynamically added if not present)

**Game Management Layer:**
- Purpose: Global game state (Playing/Paused/Inventory/Loading/GameClear/GameOver) and time scale control
- Location: `Assets/GameScript/GameStateManager.cs`
- Contains: Singleton pattern with `DontDestroyOnLoad`, `SetState()`, `OnGameStateChange` event
- Depends on: Nothing (top-level singleton)
- Used by: `InputHandler` (input gating), `PlayerWaterStats` (triggers GameOver)

**Camera Layer:**
- Purpose: Smooth camera tracking of the player
- Location: `Assets/GameScript/CameraFollow.cs`
- Contains: `Vector3.SmoothDamp` follow in `LateUpdate`
- Depends on: Target `Transform` (player)
- Used by: Main Camera GameObject

## Data Flow

**Player Input to Action:**

1. Hardware input received by Unity Input System
2. `InputHandler` translates to typed C# events (e.g., `OnBasicAttack`)
3. `PlayerController` event handler checks current `PlayerState` guards
4. If allowed, delegates to appropriate subsystem (`PlayerMovement.TryJump()` or `ISkill.TryUse()`)
5. Subsystem executes, updates `PlayerState` via `IPlayerContext.ChangeState()`
6. `PlayerMovement.UpdateAnimator()` syncs Animator parameters each tick

**Player Attack to Enemy Damage:**

1. `PlayerController.HandleBasicAttackInput()` calls `basicAttack.TryUse()`
2. `SkillBase.UseCoroutine()` sets state to `Attacking`, calls `ExecuteSkill()`
3. Skill calculates `GetFrontBoxCenter()`, runs `Physics2D.OverlapBoxAll()` against `enemyLayer`
4. For each hit collider, calls `IDamageable.TakeDamage(hpDamage, corruptionDamage)`
5. `EnemyStats.TakeDamage()` fires `OnDamaged` → `EnemyAI` enters Hit state
6. If HP <= 0, `EnemyStats.CheckDeathState()` evaluates corruption ratio for Purify vs Die

**Enemy Purification Flow:**

1. `EnemyStats.CheckDeathState()` checks `CorruptionRatio` against purification window
2. If in window: calls `Purify()`, fires `OnDeath`, disables self, adds/activates `PurifiedNPC`
3. `PurifiedNPC.Activate()` changes tag to "NPC", layer to "Interactable", disables attack colliders
4. NPC becomes interactive: proximity check via `Physics2D.OverlapCircle`, dialogue on [F]

**Enemy Attack to Player:**

1. `EnemyAI.UpdateAttack()` calls `EnemyAttack.AttackPlayer()` when `attackTimer` expires
2. `EnemyAttack` calls `EnemyStats.SpendHpOnAttack()` (enemy consumes own HP to attack)
3. `PlayerWaterStats.ReceiveAttack(damage, corruptionTransferRatio)` reduces player HP and adds corruption
4. `PlayerWaterStats.CheckDeath()`: if HP <= 0 or corruption >= threshold → `OnDeath` + `GameStateManager.SetState(GameOver)`

**State Management:**

- Player state: stored in `PlayerController.CurrentState` (`PlayerState` enum), exposed via `IPlayerContext`
- Enemy state: stored in `EnemyAI.CurrentState` (`EnemyState` enum)
- Game state: stored in `GameStateManager.Instance.CurrentState` (`GameState` enum), global singleton

## Key Abstractions

**IPlayerContext:**
- Purpose: Decouples `PlayerMovement` and all `SkillBase` subclasses from the concrete `PlayerController`
- Examples: `Assets/Player/IPlayerContext.cs`
- Pattern: Interface with properties (`FacingRight`, `Rigidbody`, `Stats`, `CurrentState`) and methods (`ChangeState`, `SetInvincible`)

**ISkill:**
- Purpose: Uniform skill contract allowing `PlayerController` to call skills without knowing their type
- Examples: `Assets/Player/ISkill.cs`
- Pattern: Interface with `Initialize(IPlayerContext)`, `TryUse()`, `SetStage(int)`, `IsOnCooldown`

**IDamageable:**
- Purpose: Any game object that can receive damage (player, enemy, destructible environment)
- Examples: `Assets/GameScript/IDamageable.cs` — implemented by `PlayerWaterStats`, `PlayerStats`, `EnemyStats`
- Pattern: Single-method interface `TakeDamage(float hpDamage, float corruptionDamage)`

**SkillBase:**
- Purpose: Abstract MonoBehaviour providing cooldown, Attacking state lifecycle, box indicator, corruption damage helpers
- Examples: `Assets/Player/SkillBase.cs`
- Pattern: Template Method — concrete skills implement `ExecuteSkill()` coroutine; optional `OnInitialize()` and `CanUse()` hooks

**WaterTier System:**
- Purpose: Player scales skill power and corruption output via tier (0–3), switched with O key
- Implementation: `PlayerWaterStats.CycleWaterTier()` → `PlayerController.HandleWaterTierSwitchInput()` → `(skill as SkillBase).SetStage(tier)`
- All three skills plus `PlayerMovement.dashStage` share the same 0–3 tier concept

## Entry Points

**Scene Entry:**
- Location: `Assets/Scenes/SampleScene.unity`
- Triggers: Unity Editor Play or build launch
- Responsibilities: Contains all GameObjects; `GameStateManager` uses `DontDestroyOnLoad`

**Player GameObject:**
- Location: Configured in scene with components `PlayerController`, `PlayerMovement`, `InputHandler`, `PlayerWaterStats`, `BasicAttackSkill`, `WideSlashSkill`, `ProjectileSkill`, `Rigidbody2D`, `Animator`
- Triggers: `Awake` / `Start` chain initializes all components
- Responsibilities: Root of player subsystem; `PlayerController.Awake()` wires all references

**Enemy GameObject:**
- Location: Scene-placed prefab(s) with components `EnemyAI`, `EnemyAttack`, `EnemyStats`, `Rigidbody2D`, `SpriteRenderer`
- Triggers: `Start()` calls `FindFirstObjectByType<PlayerController>()` to locate player
- Responsibilities: Self-contained AI; subscribes to `EnemyStats` events for state transitions

## Error Handling

**Strategy:** Defensive null checks with `Debug.LogWarning`/`Debug.LogError`, no exceptions thrown

**Patterns:**
- `if (component == null) Debug.LogError(...)` at call sites (e.g., `EnemyAttack.AttackPlayer()`)
- `FindFirstObjectByType` results checked for null before use
- `isDead` guard flag prevents double-death processing in `EnemyStats` and `PlayerWaterStats`
- `OnDestroy` unsubscribes all C# events to prevent null reference leaks

## Cross-Cutting Concerns

**Logging:** `Debug.Log` used throughout with `[ClassName]` prefix for filtering in Unity Console. Editor-only `OnGUI` overlays on `PlayerController` and `EnemyStats` for runtime debugging.

**Validation:** Inspector `[SerializeField]` + `[Range]` attributes enforce value bounds at edit time. Runtime guards (e.g., `Mathf.Clamp`, `Mathf.Min/Max`) protect resource values.

**Authentication:** Not applicable (single-player local game).

---

*Architecture analysis: 2026-03-27*
