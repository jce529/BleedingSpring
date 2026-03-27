# Coding Conventions

**Analysis Date:** 2026-03-27

## Naming Patterns

**Files:**
- PascalCase for all `.cs` files, matching the class name: `PlayerController.cs`, `EnemyAI.cs`, `SkillBase.cs`
- Interfaces prefixed with `I`: `IPlayerContext.cs`, `ISkill.cs`, `IDamageable.cs`
- Enums in their own file, named after the enum: `PlayerState.cs`, `EnemyState.cs`
- Skill classes suffixed with `Skill`: `BasicAttackSkill.cs`, `WideSlashSkill.cs`, `ProjectileSkill.cs`

**Classes:**
- PascalCase: `PlayerController`, `EnemyAI`, `GameStateManager`
- MonoBehaviour components use descriptive compound nouns: `PlayerWaterStats`, `InputHandler`, `CameraFollow`

**Methods:**
- PascalCase for public methods: `TakeDamage()`, `Initialize()`, `AttackPlayer()`, `SacrificeWater()`
- PascalCase for private methods: `HandleDeath()`, `UpdateIdle()`, `CheckGround()`, `FireProjectile()`
- Private event handlers prefixed with `Handle`: `HandleDeath()`, `HandleHit()`, `HandleJumpInput()`, `HandleMoveInput()`
- Input system callbacks prefixed with `On` + action name: `OnMovePerformed()`, `OnJumpPerformed()`
- Unity lifecycle methods follow standard Unity casing: `Awake()`, `Start()`, `Update()`, `OnDestroy()`

**Variables:**
- camelCase for private fields: `moveInput`, `attackCombo`, `lastAttackTime`, `patrolCenter`
- Private serialized Inspector fields use `[SerializeField] private`: `detectionRadius`, `moveSpeed`, `dashDuration`
- Public properties use PascalCase: `CurrentState`, `IsGrounded`, `FacingRight`, `WaterTier`
- Local variables use camelCase: `elapsed`, `direction`, `hits`, `dist`
- Static readonly arrays use PascalCase: `DashInvincibilityDuration`, `WidthBonus`, `DamageMult`

**Constants / Static Fields:**
- `static readonly` arrays in PascalCase: `static readonly float[] WidthBonus = { ... }`
- Animator parameter hashes: `static readonly int AnimState = Animator.StringToHash("AnimState")`

**Interfaces:**
- `I` prefix, PascalCase: `IPlayerContext`, `ISkill`, `IDamageable`

**Enums:**
- PascalCase for enum type and all values: `PlayerState.Idle`, `EnemyState.Chase`, `GameStateManager.GameState.Playing`

## Code Style

**Formatting:**
- No `.editorconfig` or `.prettierrc` detected; formatting is manually consistent
- Alignment padding used for multi-line assignments and property lists (columns aligned with spaces):
  ```csharp
  Rigidbody    = GetComponent<Rigidbody2D>();
  Stats        = GetComponent<PlayerWaterStats>();
  movement     = GetComponent<PlayerMovement>();
  ```
- Opening braces on same line as declaration: `public class Foo {`
- Inline `return` guards at the start of methods for early exits:
  ```csharp
  if (CurrentState == PlayerState.Dead) return;
  if (isDead) return;
  ```
- Null-conditional operator used for optional components: `animator?.SetTrigger("Jump")`

**Section Separators:**
- Methods and field groups are visually separated with ASCII box-drawing comment headers:
  ```csharp
  // ─── 초기화 ──────────────────────────────────────────────────────────────
  // ─── 메인 루프 ───────────────────────────────────────────────────────────
  // ─── 상태 전이 ───────────────────────────────────────────────────────────
  ```
  All separators use Korean section labels. Apply this pattern when adding new sections.

**Inspector Attributes:**
- `[Header("...")]` used in Korean for logical groups of `[SerializeField]` fields
- `[Tooltip("...")]` used in Korean for fields needing explanation
- `[Range(min, max)]` used when a numeric field has known bounds: `[SerializeField, Range(0, 3)]`
- `[TextArea(min, max)]` used for multiline string arrays (e.g., dialogue lines)
- `[RequireComponent(typeof(X))]` placed above class declaration to enforce component dependencies

## Import Organization

**Order:**
1. `using System;` / `using System.Collections;` / `using System.Collections.Generic;`
2. `using UnityEngine;`
3. `using UnityEngine.InputSystem;` (Input System specific)
4. No project-specific namespace imports — the codebase uses no custom namespaces; all types are in the global namespace

**Namespaces:**
- No custom namespaces used. All project scripts are in the global namespace.

## Error Handling

**Patterns:**
- Null checks before using optional component references: `if (playerStats == null) return;`
- `Debug.LogWarning()` for expected-missing but non-fatal references: `Debug.LogWarning($"[EnemyAttack] {gameObject.name}: ...")`
- `Debug.LogError()` for programming errors (required component missing): `Debug.LogError($"[EnemyAttack] {gameObject.name}: EnemyStats 컴포넌트가 없습니다.")`
- Guard clauses with `return` or `return false` before any logic that can fail:
  ```csharp
  if (isDead) return;
  if (hpDamage <= 0f) return;
  ```
- Boolean return methods (`TryJump()`, `TryDash()`, `SacrificeWater()`, `SpendHpOnAttack()`) return `false` on failure — callers always check the return value

**`TryXxx` Pattern:**
- Methods that may fail return `bool`: `TryJump()`, `TryDash()`, `SacrificeWater(float)`, `SpendHpOnAttack(float)`
- Methods that always succeed are `void` or return a direct value

## Logging

**Framework:** `UnityEngine.Debug`

**Patterns:**
- All `Debug.Log` messages prefixed with the class name in brackets: `[EnemyAI]`, `[PlayerWaterStats]`, `[SkillBase]`
- Format: `Debug.Log($"[ClassName] {gameObject.name} — description: {value:F1}")`
- Debug state machines: `Debug.Log($"[EnemyAI] {gameObject.name} → {next}")` when changing state
- `Debug.DrawRay()` used in editor to visualize raycasts: `Assets/Player/ProjectileSkill.cs`
- Skill activations logged in `SkillBase.UseCoroutine()` with stage, HP, and corruption values
- `#if UNITY_EDITOR` guard used when debug UI (`OnGUI`) has performance cost: `Assets/Enemy/EnemyStats.cs`, `Assets/Enemy/PurifiedNPC.cs`

## Comments

**When to Comment:**
- Korean-language XML doc comments (`/// <summary>`) on every public class and public method
- Inline Korean comments on non-obvious logic: combo reset, patrol direction reversal, corruption mechanics
- Inspector field `[Tooltip]` in Korean for all configurable public floats with non-obvious meaning
- Section headers in Korean mark logical groupings within a class

**XML Documentation (C# `///`):**
- Applied to: all classes, all public methods, events, and public properties with non-obvious purpose
- Format:
  ```csharp
  /// <summary>
  /// 한글 설명. 여러 줄로 기계적 행동을 설명합니다.
  /// </summary>
  /// <param name="paramName">설명</param>
  /// <returns>반환값 설명</returns>
  ```
- `<summary>` blocks may include `[에디터 설정]` sections listing Inspector fields that must be configured

## Function Design

**Size:** Methods are kept small and single-purpose (SRP). State-update methods like `UpdateIdle()`, `UpdatePatrol()` average 4–8 lines.

**Parameters:** Prefer named parameters over ambiguous booleans. Long parameter lists are avoided; use component references stored as fields instead.

**Return Values:**
- `bool` for Try-pattern methods
- `void` for fire-and-forget actions
- `IEnumerator` for coroutine-based timed logic (skill execution, dash, afterimages)

## Module Design

**Exports:**
- Public API is explicit and minimal: most fields are `private` with public read-only properties
- `{ get; private set; }` is the standard property pattern for externally-readable, internally-mutable state
- Expression-body properties used for simple computed values: `public float HpRatio => CurrentHp / maxHp;`

**Component Composition (Unity-specific):**
- `[RequireComponent]` enforces hard dependencies between MonoBehaviours
- Components communicate via interfaces (`IPlayerContext`, `IDamageable`) not concrete types — follows DIP
- Events (`System.Action`) used for loose coupling: `OnDeath`, `OnHpChanged`, `OnWaterTierChanged`
- Event subscriptions added in `Start()` and removed symmetrically in `OnDestroy()`:
  ```csharp
  // Start()
  Stats.OnDeath += HandleDeath;
  // OnDestroy()
  if (Stats != null) Stats.OnDeath -= HandleDeath;
  ```

**SOLID Principles:**
- SRP enforced: `PlayerMovement` handles only physics; `InputHandler` handles only input; `PlayerWaterStats` handles only resources
- DIP enforced: skills depend on `IPlayerContext`, not `PlayerController`; enemies depend on `IDamageable`, not concrete player types
- ISP enforced: `ISkill` is a small contract with only `TryUse()`, `Initialize()`, `SetStage()`, `IsOnCooldown`, `Stage`

**Singleton Pattern:**
- `GameStateManager` uses `Instance` singleton with `DontDestroyOnLoad`: `Assets/GameScript/GameStateManager.cs`
- Pattern: null check in `Awake()`, destroy duplicates

**Abstract Base Classes:**
- `SkillBase : MonoBehaviour, ISkill` — provides cooldown, state management, and helper methods; concrete skills override `ExecuteSkill()` (Template Method pattern)
- `OnInitialize()` is a virtual hook for subclass setup, called from `Initialize()`

---

*Convention analysis: 2026-03-27*
