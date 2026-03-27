# Testing Patterns

**Analysis Date:** 2026-03-27

## Test Framework

**Runner:**
- Unity Test Framework `1.6.0` (`com.unity.test-framework`) is installed as a package
- Config: `Packages/manifest.json` — dependency entry `"com.unity.test-framework": "1.6.0"`
- No custom test assembly definition (`.asmdef`) found under `Assets/`

**Assertion Library:**
- Unity Test Framework uses NUnit 3 (bundled)

**Run Commands:**
```
Unity Editor → Window → General → Test Runner  (Edit Mode or Play Mode tests)
```

## Test File Organization

**Location:**
- No project-authored test files exist under `Assets/`. The test framework package is present but no tests have been written.
- Unity Test Framework convention (to follow when adding tests):
  - Edit Mode tests: `Assets/Tests/Editor/`
  - Play Mode tests: `Assets/Tests/PlayMode/`
  - Each folder requires its own `.asmdef` file with test references

**Naming:**
- Unity convention: `<TestedClass>Tests.cs` (e.g., `PlayerWaterStatsTests.cs`)
- Test methods: `<MethodName>_<Scenario>_<ExpectedResult>` (e.g., `TakeDamage_WhenHpReachesZero_InvokesOnDeath`)

**Structure:**
```
Assets/
└── Tests/                    # Does not exist yet — must be created
    ├── Editor/               # Edit Mode (no scene required, fast)
    │   ├── AssemblyRef.asmdef
    │   └── <Class>Tests.cs
    └── PlayMode/             # Play Mode (requires scene, tests coroutines/physics)
        ├── AssemblyRef.asmdef
        └── <Class>Tests.cs
```

## Test Structure

**Suite Organization:**
Unity Test Framework uses NUnit `[TestFixture]` / `[Test]` / `[UnityTest]` attributes:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

[TestFixture]
public class PlayerWaterStatsTests
{
    private GameObject go;
    private PlayerWaterStats stats;

    [SetUp]
    public void SetUp()
    {
        go    = new GameObject();
        stats = go.AddComponent<PlayerWaterStats>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TakeDamage_ReducesCurrentCleanWater()
    {
        stats.TakeDamage(10f, 0f);
        Assert.That(stats.CurrentCleanWater, Is.EqualTo(90f));
    }
}
```

**Patterns:**
- `[SetUp]` / `[TearDown]` for per-test `GameObject` creation and cleanup
- `new GameObject().AddComponent<T>()` to instantiate components in Edit Mode without a scene
- `Object.DestroyImmediate(go)` in `[TearDown]` to prevent test leakage
- `[UnityTest]` + `IEnumerator` for tests that must advance frames (coroutines, physics, `Time.deltaTime`)

## Mocking

**Framework:** No mocking library detected (no Moq, NSubstitute, or similar). Not currently used.

**Recommended pattern for this codebase:**
The codebase is well-structured for testability via interfaces. To test without Unity objects:

```csharp
// Create a manual fake of IPlayerContext
public class FakePlayerContext : IPlayerContext
{
    public bool             FacingRight  { get; set; } = true;
    public Rigidbody2D      Rigidbody    { get; set; }
    public PlayerWaterStats Stats        { get; set; }
    public PlayerState      CurrentState { get; private set; } = PlayerState.Idle;

    public void ChangeState(PlayerState s) => CurrentState = s;
    public void SetInvincible(bool v) { }
    public bool IsInvincible => false;
}
```

**What to Mock:**
- `IPlayerContext` — when testing `SkillBase` subclasses (`BasicAttackSkill`, `WideSlashSkill`, `ProjectileSkill`)
- `IDamageable` targets — when testing hit detection without real enemy `GameObjects`
- `GameStateManager.Instance` — set to `null` to avoid singleton side effects in isolation tests

**What NOT to Mock:**
- `PlayerWaterStats`, `EnemyStats` — these are pure data+event classes with no Unity physics; instantiate directly with `AddComponent<>`
- `PlayerState`, `EnemyState` — plain enums; use directly

## Fixtures and Factories

**Test Data:**
No shared factories exist yet. Recommended pattern matching codebase style:

```csharp
// Inline factory helper inside test class
private static PlayerWaterStats CreateStats(float maxWater = 100f, float maxCorruption = 100f)
{
    var go    = new GameObject();
    var stats = go.AddComponent<PlayerWaterStats>();
    // Use reflection or Awake call if private fields need setting
    return stats;
}
```

**Location:**
- Fixtures should go in `Assets/Tests/Editor/Helpers/` or `Assets/Tests/PlayMode/Helpers/`

## Coverage

**Requirements:** None enforced. No coverage configuration found.

**View Coverage:**
Unity Test Framework does not provide built-in coverage reports. To enable:
```
Unity Editor → Edit → Project Settings → Code Coverage (requires com.unity.testtools.codecoverage package)
```

## Test Types

**Unit Tests (Edit Mode):**
- Scope: Pure logic classes with no physics or scene dependency
- Best candidates for immediate unit testing:
  - `Assets/Player/PlayerWaterStats.cs` — `TakeDamage`, `SacrificeWater`, `Heal`, `Purify`, `CheckDeath`, `CycleWaterTier`
  - `Assets/Player/PlayerStats.cs` — `TakeDamage`, `TrySacrificeHp`, `Heal`
  - `Assets/Enemy/EnemyStats.cs` — `TakeDamage`, `SpendHpOnAttack`, `WidenPurificationRange`, purify vs die boundary
  - `Assets/GameScript/GameStateManager.cs` — `SetState` state transitions, singleton behavior

**Integration / Play Mode Tests:**
- Scope: Behaviour requiring scene, `Update()` loop, physics, or coroutines
- Best candidates:
  - `Assets/Player/PlayerMovement.cs` — `TryJump`, `TryDash`, ground detection (needs `Rigidbody2D` and physics scene)
  - `Assets/Player/SkillBase.cs` — `TryUse` → coroutine flow → cooldown reset (requires `yield return`)
  - `Assets/Enemy/EnemyAI.cs` — state machine transitions over multiple frames

**E2E Tests:**
- Not used. No scene-level test framework configured.

## Common Patterns

**Async / Coroutine Testing:**
```csharp
[UnityTest]
public IEnumerator DashCoroutine_SetsIsDashingTrue_ThenFalseAfterDuration()
{
    // Requires Play Mode — physics scene needed
    var go       = new GameObject();
    var rb       = go.AddComponent<Rigidbody2D>();
    var movement = go.AddComponent<PlayerMovement>();
    // ... initialize, call TryDash()
    yield return new WaitForSeconds(0.3f);
    Assert.IsFalse(movement.IsDashing);
    Object.Destroy(go);
}
```

**Event Testing:**
```csharp
[Test]
public void TakeDamage_WhenHpReachesZero_InvokesOnDeath()
{
    var go    = new GameObject();
    var stats = go.AddComponent<PlayerWaterStats>();

    bool deathFired = false;
    stats.OnDeath += () => deathFired = true;

    stats.TakeDamage(999f, 0f);

    Assert.IsTrue(deathFired);
    Object.DestroyImmediate(go);
}
```

**Error / Guard Testing:**
```csharp
[Test]
public void SacrificeWater_WhenCostExceedsCurrentWater_ReturnsFalse()
{
    var go    = new GameObject();
    var stats = go.AddComponent<PlayerWaterStats>();

    bool result = stats.SacrificeWater(200f);  // exceeds default 100f max

    Assert.IsFalse(result);
    Object.DestroyImmediate(go);
}
```

## Key Testability Notes

- All stats classes (`PlayerWaterStats`, `PlayerStats`, `EnemyStats`) fire `System.Action` events — straightforward to assert in tests by subscribing a flag
- Skills depend on `IPlayerContext` (interface) — fake implementations are cheap to write
- `GameStateManager` singleton (`Instance`) must be cleared or guarded between tests to avoid cross-test contamination
- `SkillBase.ExecuteSkill()` is `protected abstract IEnumerator` — skill behaviour is isolated and testable via Play Mode `[UnityTest]`
- No dependency injection container — component wiring is via `GetComponent<>` in `Awake()`/`Start()`; use `AddComponent<>` in tests to replicate

---

*Testing analysis: 2026-03-27*
