# Testing Patterns

**Analysis Date:** 2026-03-27

## Test Framework

**Runner:**
- Unity Test Framework `1.6.0` (`com.unity.test-framework`) is installed as a package
- Config: `Packages/manifest.json` — dependency entry `"com.unity.test-framework": "1.6.0"`
- No custom test assembly definition (`.asmdef`) found under `Assets/` (relying on default assembly behavior or needs `.asmdef` for isolated tests)

**Assertion Library:**
- Unity Test Framework uses NUnit 3 (bundled)

**Run Commands:**
```
Unity Editor → Window → General → Test Runner  (Edit Mode or Play Mode tests)
```

## Test File Organization

**Location:**
- Project-authored tests are located under `Assets/Tests/`.
- Edit Mode tests: `Assets/Tests/Editor/`
  - Example: `Assets/Tests/Editor/BossUIManagerTests.cs`
- Play Mode tests: `Assets/Tests/PlayMode/` (Not yet present, but recommended for physics/coroutines)

**Naming:**
- Unity convention: `<TestedClass>Tests.cs` (e.g., `BossUIManagerTests.cs`, `PlayerWaterStatsTests.cs`)
- Test methods: `<MethodName>_<Scenario>_<ExpectedResult>` (e.g., `TakeDamage_WhenHpReachesZero_InvokesOnDeath`)

**Structure:**
```
Assets/
└── Tests/
    ├── Editor/               # Edit Mode (no scene required, fast)
    │   └── BossUIManagerTests.cs
    └── PlayMode/             # Play Mode (requires scene, tests coroutines/physics)
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
public class BossUIManagerTests
{
    [Test]
    public void Singleton_Instance_IsNotNull()
    {
        GameObject go = new GameObject("BossUIManager");
        var manager = go.AddComponent<BossUIManager>();
        
        Assert.IsNotNull(BossUIManager.Instance);
        
        Object.DestroyImmediate(go);
    }
}
```

**Patterns:**
- `[SetUp]` / `[TearDown]` for per-test `GameObject` creation and cleanup (though many tests do this inline currently)
- `new GameObject().AddComponent<T>()` to instantiate components in Edit Mode without a scene
- `Object.DestroyImmediate(go)` in `[TearDown]` or inline to prevent test leakage in Editor
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
- `GameStateManager.Instance` — be careful with singletons; they may need to be cleared or guarded

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
    // Use reflection or public properties if needed
    return stats;
}
```

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
  - `Assets/Enemy/EnemyStats.cs` — `TakeDamage`, `SpendHpOnAttack`, `WidenPurificationRange`
  - `Assets/GameScript/GameStateManager.cs` — `SetState` state transitions, singleton behavior

**Integration / Play Mode Tests:**
- Scope: Behaviour requiring scene, `Update()` loop, physics, or coroutines
- Best candidates:
  - `Assets/Player/PlayerMovement.cs` — `TryJump`, `TryDash`, ground detection (needs `Rigidbody2D` and physics scene)
  - `Assets/Player/SkillBase.cs` — `TryUse` → coroutine flow → cooldown reset (requires `yield return`)
  - `Assets/Enemy/EnemyAI.cs` — state machine transitions over multiple frames

## Common Patterns

**Async / Coroutine Testing:**
```csharp
[UnityTest]
public IEnumerator DashCoroutine_SetsIsDashingTrue_ThenFalseAfterDuration()
{
    // Requires Play Mode
    var go       = new GameObject();
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

---

*Testing analysis: 2026-03-27*
