# Phase 4: Player Stance System - Research

**Researched:** 2026-05-11
**Domain:** Unity 2D C# — MonoBehaviour component architecture, skill multiplier pattern
**Confidence:** HIGH (all findings from direct codebase inspection)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- 별도 MonoBehaviour `PlayerStanceManager`를 만든다. `PlayerStats`나 `PlayerWaterStats`에 합치지 않는다.
- `PlayerStanceManager`는 `MainStance`(StanceType)와 `SubStance`(StanceType) 두 프로퍼티를 가진다.
- 각 태세는 3개의 `SkillBase` 슬롯을 Inspector에서 할당한다 (`SkillBase[] mainSkills`, `SkillBase[] subSkills`, 각 크기 3).
- 래퍼/데코레이터 패턴은 채택하지 않는다.
- `SkillBase`에 `[SerializeField] protected float costMultiplier = 1f`와 `[SerializeField] protected float effectMultiplier = 1f` 필드를 추가한다.
- 보조 태세용 스킬 컴포넌트는 메인과 별도 인스턴스로 플레이어 GameObject에 붙인다. Inspector에서 `costMultiplier = 0.5`, `effectMultiplier = 0.5`로 설정한다.
- `IPlayerContext` 인터페이스에 `PlayerStanceManager StanceManager { get; }` 프로퍼티를 추가한다.
- `PlayerController`가 이를 구현한다.
- 별도 파일 `StanceType.cs`로 StanceType enum을 정의한다.
- MVP 범위에서는 enum 값 정의만 한다 (예: `None`, `Water`).

### Claude's Discretion
- `PlayerStanceManager.SetStances(main, sub)` 메서드 시그니처의 구체적 설계
- `GetMainSkill(int slot)` / `GetSubSkill(int slot)` 접근자 설계
- 슬롯 범위 유효성 검사 방식 (Assert vs. Clamp)
- `StanceType` 초기 enum 값 목록 (최소한의 값만 정의)

### Deferred Ideas (OUT OF SCOPE)
- 태세 전환 입력 키 바인딩
- 태세별 HUD 표시 (현재 태세 아이콘 등)
- 구체적인 태세 콘텐츠 (Water 태세, Fire 태세 등 실제 스킬 세트 구성)
- 태세별 패시브 효과
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| STN-01 | `StanceType` enum이 정의되고, 태세 종류를 코드와 Inspector에서 식별할 수 있다 | 신규 파일 `StanceType.cs` — 의존성 없음, 단순 enum 정의 |
| STN-02 | `PlayerStanceManager` MonoBehaviour가 메인/보조 태세를 저장하며 각각 3개의 `SkillBase` 슬롯을 Inspector에서 할당할 수 있다 | 신규 파일 `PlayerStanceManager.cs` — `SkillBase[]` 배열 직렬화 가능 확인됨 |
| STN-03 | `SkillBase`에 `costMultiplier`와 `effectMultiplier` 필드(기본값 1.0)가 추가되고, 스킬 실행 시 소모량과 효과 계산에 배율이 적용된다 | SacrificeWater 호출 위치 2곳, 데미지 변수 위치 전수 조사 완료 (아래 상세) |
| STN-04 | `IPlayerContext` 인터페이스에 `PlayerStanceManager StanceManager` 프로퍼티가 추가되어 스킬 클래스에서 현재 태세 정보를 참조할 수 있다 | `IPlayerContext.cs` 현재 구조 파악 완료, 추가 위치 명확 |
| STN-05 | 기존 `BasicAttackSkill`, `WideSlashSkill`, `ProjectileSkill`이 기본값(1.0)으로 기존과 동일하게 동작한다 (하위 호환) | 기본값 1.0 → 모든 계산에 ×1.0 적용 = 동일 결과. 단, 초기화 흐름의 함정 확인됨 (아래 Critical 섹션) |
</phase_requirements>

---

## Summary

Phase 4는 순수 C# 코드 변경 Phase다. Unity Editor에서 새 GameObject나 Canvas를 만드는 작업은 없고, 기존 Player GameObject에 컴포넌트를 추가하고 Inspector에서 참조를 연결하는 수준의 에디터 작업만 수반된다.

핵심 변경은 세 레이어다. (1) `SkillBase`에 배율 필드 2개 추가 + 각 구체 스킬의 cost/effect 적용 위치 수정, (2) `StanceType.cs` + `PlayerStanceManager.cs` 신규 생성, (3) `IPlayerContext` / `PlayerController` 확장.

**가장 중요한 발견:** `PlayerController.Awake()`는 `GetComponent<BasicAttackSkill>()` 등 타입별 단일 조회로 메인 스킬을 찾는다. `PlayerStanceManager.subSkills[]`에 할당된 보조 태세 스킬 인스턴스들은 PlayerController의 `InitSkill()` 흐름에 포함되지 않는다. 따라서 **`PlayerStanceManager`가 직접 `Initialize(IPlayerContext)`를 보조 스킬들에게 호출해야 한다.** 이 초기화 책임이 Plan에 반드시 반영되어야 한다.

**Primary recommendation:** `PlayerStanceManager.Initialize(IPlayerContext context)` 메서드를 만들고, `PlayerController.Start()`에서 메인 스킬 초기화 직후에 `StanceManager.Initialize(this)`를 호출해 보조 스킬들까지 한 번에 초기화한다.

---

## Standard Stack

이 Phase는 외부 라이브러리를 도입하지 않는다. 사용 기술은 프로젝트에 이미 존재하는 것들이다.

| 기술 | 버전 | 용도 |
|------|------|------|
| Unity 6 (6000.x) | 기존 | MonoBehaviour, SerializeField, GetComponent |
| C# 12 | 기존 | enum, interface, abstract class |
| UnityEngine.Assertions | 기존 | 슬롯 범위 유효성 검사 (Assert.IsTrue) |

설치 필요 패키지: 없음.

---

## Architecture Patterns

### 권장 파일 구조 (신규 생성 파일)

```
Assets/Player/
├── StanceType.cs           (신규) — enum 정의
└── PlayerStanceManager.cs  (신규) — MonoBehaviour
```

수정 파일:
- `Assets/Player/SkillBase.cs` — 배율 필드 추가
- `Assets/Player/BasicAttackSkill.cs` — effectMultiplier 적용
- `Assets/Player/WideSlashSkill.cs` — costMultiplier + effectMultiplier 적용
- `Assets/Player/ProjectileSkill.cs` — costMultiplier + effectMultiplier 적용
- `Assets/Player/IPlayerContext.cs` — StanceManager 프로퍼티 추가
- `Assets/Player/PlayerController.cs` — StanceManager 구현 + 보조 스킬 초기화 호출

### Pattern 1: SkillBase 배율 필드 추가

`SkillBase`는 추상 기반이므로 protected 필드로 추가한다. 하위 클래스가 직접 `costMultiplier`, `effectMultiplier`를 참조한다.

```csharp
// SkillBase.cs — [Header("태세 배율")] 섹션 추가 위치:
// 기존 [Header("공통 스킬 설정")] 바로 아래에 삽입

[Header("태세 배율 (보조 태세용 인스턴스는 0.5 설정)")]
[SerializeField] protected float costMultiplier   = 1f;
[SerializeField] protected float effectMultiplier = 1f;
```

### Pattern 2: SacrificeWater 호출 수정 — 정확한 위치

**WideSlashSkill.cs (line 92):**
```csharp
// 현재
Context.Stats.SacrificeWater(tsunamiHpCost);
// 수정 후
Context.Stats.SacrificeWater(tsunamiHpCost * costMultiplier);
```

**ProjectileSkill.cs (line 86):**
```csharp
// 현재
Context.Stats.SacrificeWater(sniperHpCost);
// 수정 후
Context.Stats.SacrificeWater(sniperHpCost * costMultiplier);
```

**BasicAttackSkill.cs:** SacrificeWater 호출 없음 — costMultiplier 적용 대상이 아니다.

### Pattern 3: 데미지 effectMultiplier 적용 — 정확한 위치

**BasicAttackSkill.cs:**
- `DamageBox(center, size, baseDamage)` — `baseDamage * effectMultiplier`로 변경
- Echo 타격 `baseDamage * 0.5f` — `baseDamage * effectMultiplier * 0.5f`로 변경
- `GetCorruptionDamage(damage)` 인수도 동일하게 effectMultiplier가 적용된 damage를 전달

**WideSlashSkill.cs (0~2단계 ExecuteSlash):**
- `float damage = baseDamage * DamageMult[stage]` — `baseDamage * DamageMult[stage] * effectMultiplier`로 변경

**WideSlashSkill.cs (3단계 ExecuteTsunamiSlash):**
- `float dmg = baseDamage * DamageMult[3]` — `baseDamage * DamageMult[3] * effectMultiplier`로 변경

**ProjectileSkill.cs (1단계):**
- `FireProjectile(baseDamage, ...)` — `FireProjectile(baseDamage * effectMultiplier, ...)`

**ProjectileSkill.cs (2단계):**
- `FireProjectile(baseDamage * 1.3f, ...)` — `FireProjectile(baseDamage * 1.3f * effectMultiplier, ...)`

**ProjectileSkill.cs (3단계 FireLaser):**
- `sniperDamage`는 별도 SerializeField 필드다. `sniperDamage * effectMultiplier`로 변경

### Pattern 4: PlayerStanceManager 초기화 책임

```csharp
// PlayerStanceManager.cs
public class PlayerStanceManager : MonoBehaviour
{
    public StanceType MainStance { get; private set; }
    public StanceType SubStance  { get; private set; }

    [SerializeField] private SkillBase[] mainSkills = new SkillBase[3];
    [SerializeField] private SkillBase[] subSkills  = new SkillBase[3];

    // PlayerController.Start()에서 메인 스킬 초기화 직후 호출
    public void Initialize(IPlayerContext context)
    {
        foreach (var s in subSkills)
            if (s != null) s.Initialize(context);
    }

    public ISkill GetMainSkill(int slot) => mainSkills[slot];
    public ISkill GetSubSkill(int slot)  => subSkills[slot];

    public void SetStances(StanceType main, StanceType sub)
    {
        MainStance = main;
        SubStance  = sub;
    }
}
```

```csharp
// PlayerController.cs — Start() 메서드 수정
private void Start()
{
    movement.Initialize(this);

    InitSkill(basicAttack);   // 기존 메인 스킬 초기화 (유지)
    InitSkill(wideSlash);
    InitSkill(projectile);

    StanceManager?.Initialize(this);   // 보조 스킬 초기화 추가

    // ... 나머지 이벤트 구독 (유지)
}
```

```csharp
// PlayerController.cs — IPlayerContext 구현 부분에 추가
public PlayerStanceManager StanceManager { get; private set; }

// Awake() 내에 추가
StanceManager = GetComponent<PlayerStanceManager>();
```

### Pattern 5: IPlayerContext 확장

```csharp
// IPlayerContext.cs — 기존 프로퍼티 목록 끝에 추가
PlayerStanceManager StanceManager { get; }
```

### Anti-Patterns to Avoid

- **GetComponent<WideSlashSkill>() 배열 조회로 서브 스킬 접근 시도:** GetComponent<T>()는 첫 번째 인스턴스만 반환한다. 반드시 Inspector 직렬화 참조(`subSkills[]`)를 사용해야 한다.
- **PlayerStanceManager.Awake()에서 보조 스킬 자동 Initialize:** PlayerStanceManager의 Awake는 PlayerController의 Awake보다 먼저 실행될 수 있다. IPlayerContext(this)가 아직 준비되지 않은 시점에 Initialize를 호출하면 Context가 null이 된다. 반드시 PlayerController.Start()에서 명시적으로 호출한다.
- **mainSkills[]와 subSkills[] 배열 크기 하드코딩 강제:** `new SkillBase[3]` 기본값은 Inspector에서 자유롭게 변경 가능하므로, 코드에서 `[3]`을 고정 인덱스로 직접 참조하지 않는다. 항상 배열 길이 확인 또는 null 체크를 한다.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| 보조 스킬 효과 감소 | 래퍼 클래스, Decorator 패턴 | SkillBase의 SerializeField 배율 필드 | ExecuteSkill() 내부에서 SacrificeWater를 직접 호출하므로 래퍼가 비용을 가로챌 수 없다 (CONTEXT.md에서 이미 결론) |
| 여러 동일 컴포넌트 중 특정 인스턴스 선택 | GetComponents<T>() + LINQ 필터링 | Inspector SerializeField 직렬화 참조 | Inspector 직렬화가 더 명확하고 Unity 스타일이다 |

---

## Critical Integration Points (함정)

### 함정 1: GetComponent 단일 인스턴스 반환

**현상:** `PlayerController.Awake()`의 아래 코드는 타입별 첫 번째 컴포넌트만 반환한다.

```csharp
basicAttack = GetComponent<BasicAttackSkill>() as ISkill;   // 첫 번째 BasicAttackSkill만
wideSlash   = GetComponent<WideSlashSkill>()   as ISkill;   // 첫 번째 WideSlashSkill만
projectile  = GetComponent<ProjectileSkill>()  as ISkill;   // 첫 번째 ProjectileSkill만
```

**결론:** 보조 태세 스킬들(두 번째 WideSlashSkill 등)은 PlayerController의 `basicAttack`, `wideSlash`, `projectile` 변수에 절대 담기지 않는다. 이것은 **의도된 동작이다.** PlayerStanceManager가 `subSkills[]` Inspector 참조로 직접 보유한다. PlayerController는 메인 스킬만 다루는 현재 구조 그대로 유지된다.

**위험:** PlayerController가 `GetComponent<WideSlashSkill>()`을 재호출해 보조 스킬 인스턴스를 가져오려 하면 항상 메인 슬롯 인스턴스를 반환한다. 이런 코드를 작성하지 않는다.

### 함정 2: 보조 스킬 Initialize 누락

**현상:** `PlayerController.Start()`의 `InitSkill()` 호출은 `basicAttack`, `wideSlash`, `projectile` (메인 3개)에만 적용된다. `subSkills[]`는 PlayerController가 모른다.

**결론:** `PlayerStanceManager.Initialize(IPlayerContext)`를 만들어서 `PlayerController.Start()`에서 명시적으로 호출해야 한다. 누락 시 보조 스킬의 `Context`가 null이어서 `TryUse()` 호출 시 NullReferenceException이 발생한다.

**검증 방법:** Plan의 검증 단계에서 보조 스킬 컴포넌트를 직접 `TryUse()` 호출해 NullRef 없이 실행되는지 확인한다.

### 함정 3: WideSlashSkill.CanUse()의 Stage 3 조건

`WideSlashSkill.CanUse()`에서 `Context.Stats.CurrentCleanWater <= tsunamiHpCost`를 체크한다. 보조 태세에서는 `tsunamiHpCost * costMultiplier`가 실제 소모량이므로, `CanUse()`의 조건도 함께 수정해야 한다. 그렇지 않으면 메인 슬롯 기준 HP 체크로 보조 스킬의 사용 가능 여부를 잘못 판정한다.

```csharp
// WideSlashSkill.CanUse() 수정 필요
if (Stage >= 3 && Context.Stats.CurrentCleanWater <= tsunamiHpCost * costMultiplier)
```

동일하게 `ProjectileSkill.CanUse()`도:
```csharp
if (Stage >= 3 && Context.Stats.CurrentCleanWater <= sniperHpCost * costMultiplier)
```

### 함정 4: BasicAttackSkill의 GetCorruptionDamage 인수

`BasicAttackSkill.DamageBox()` 내에서:
```csharp
h.GetComponent<IDamageable>()?.TakeDamage(damage, GetCorruptionDamage(damage));
```
`damage`를 effectMultiplier 적용 후 값으로 넘겨야 `GetCorruptionDamage()`도 비례해서 계산된다. 이미 배율이 적용된 `damage` 변수를 `TakeDamage`와 `GetCorruptionDamage` 양쪽에 동일하게 전달하면 자동으로 정렬된다.

---

## Common Pitfalls

### Pitfall 1: Coroutine 중복 실행 (중요도 낮음)

**What goes wrong:** 같은 GameObject에 동일 타입 스킬이 두 개 붙어 있을 때, 두 인스턴스 모두 `IsOnCooldown`을 독립적으로 관리한다. 하나의 인스턴스가 Coroutine 중일 때 다른 인스턴스의 `TryUse()`를 호출하면 두 Coroutine이 동시 실행될 수 있다.
**Why it happens:** 메인/보조 슬롯은 서로 다른 입력 이벤트로 발동될 예정이라 Phase 4 범위에서는 실제로 발생하지 않는다 (태세 전환 입력은 Deferred).
**How to avoid:** Phase 4에서는 Inspector 참조 직접 설정으로만 테스트하므로 무관. 태세 전환 입력 Phase에서 이 점을 고려한다.

### Pitfall 2: SkillBase.Anim 초기화 (알려진 제약)

**What goes wrong:** `SkillBase.Initialize()`에서 `Anim = GetComponent<Animator>()`를 호출한다. Player GameObject에 Animator가 하나뿐이고, 두 WideSlashSkill 인스턴스 모두 같은 Animator를 찾는다. 이것은 정상 동작이다.
**How to avoid:** 문제 없음 — 같은 Animator를 공유하는 것은 의도된 설계다.

### Pitfall 3: Inspector 배열 크기 초기화

**What goes wrong:** `SkillBase[] mainSkills`를 Inspector에서 할당하지 않으면 `mainSkills[0]`가 null이어서 `GetMainSkill(0)?.TryUse()`에서 null을 반환한다.
**How to avoid:** `PlayerStanceManager.GetMainSkill()`과 `GetSubSkill()`에서 null 체크 또는 배열 범위 체크를 수행한다. Phase 4 EDITOR-GUIDE에서 3개 슬롯 모두 연결 확인을 체크리스트 항목으로 추가한다.

---

## Code Examples

### StanceType enum (완전 코드)

```csharp
// Assets/Player/StanceType.cs
// Source: CONTEXT.md 결정사항 — MVP 최소 값만 정의

public enum StanceType
{
    None  = 0,
    Water = 1,
}
```

### PlayerStanceManager 완전 골격

```csharp
// Assets/Player/PlayerStanceManager.cs
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerStanceManager : MonoBehaviour
{
    public StanceType MainStance { get; private set; }
    public StanceType SubStance  { get; private set; }

    [SerializeField] private SkillBase[] mainSkills = new SkillBase[3];
    [SerializeField] private SkillBase[] subSkills  = new SkillBase[3];

    public void Initialize(IPlayerContext context)
    {
        foreach (var s in subSkills)
            if (s != null) s.Initialize(context);
    }

    public ISkill GetMainSkill(int slot)
    {
        Assert.IsTrue(slot >= 0 && slot < mainSkills.Length, $"[StanceManager] 잘못된 슬롯: {slot}");
        return mainSkills[slot];
    }

    public ISkill GetSubSkill(int slot)
    {
        Assert.IsTrue(slot >= 0 && slot < subSkills.Length, $"[StanceManager] 잘못된 슬롯: {slot}");
        return subSkills[slot];
    }

    public void SetStances(StanceType main, StanceType sub)
    {
        MainStance = main;
        SubStance  = sub;
    }
}
```

---

## Validation Architecture

nyquist_validation이 true이므로 이 섹션을 포함한다.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework (EditMode + PlayMode) |
| Config file | 별도 config 없음 — Unity Test Runner 내장 |
| Quick run command | Unity Test Runner > EditMode > Run All |
| Full suite command | Unity Test Runner > All Tests > Run All |

이 프로젝트에는 현재 자동화 테스트 파일이 없다 (Phase 3까지 EditMode 테스트 인프라 미구축). STN 요구사항들은 Unity MonoBehaviour 생명주기에 의존하므로 EditMode 단독 테스트보다 **PlayMode 수동 검증**이 현실적이다.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | 검증 방법 |
|--------|----------|-----------|-----------|
| STN-01 | StanceType enum 컴파일 및 Inspector 표시 | 컴파일 검증 | Unity 컴파일 오류 없음 확인 |
| STN-02 | PlayerStanceManager 슬롯 Inspector 할당 | PlayMode manual | Inspector에서 3개 슬롯 연결 후 Play Mode 진입 — NullRef 없음 |
| STN-03 | 배율 0.5 시 실제 SacrificeWater 50% 감소 | PlayMode manual | Debug.Log 출력에서 HP 소모량 절반 확인 |
| STN-04 | IPlayerContext.StanceManager 접근 | 컴파일 검증 | 스킬 코드에서 Context.StanceManager 접근 시 오류 없음 |
| STN-05 | 기존 스킬 기본값 동작 동일 | PlayMode manual | costMultiplier=1, effectMultiplier=1로 기존 데미지/소모량 동일 |

### Wave 0 Gaps

현재 Assets/Tests/ 디렉토리 없음. 이 Phase는 MonoBehaviour 의존성이 강해 EditMode 유닛 테스트 구축이 어렵다. PlayMode 수동 검증으로 대체한다.

- [ ] 자동화 테스트 없음 — 모든 검증은 Unity Play Mode 수동 확인으로 수행

---

## Environment Availability

Step 2.6: 이 Phase는 외부 CLI, 서비스, 런타임 도구에 의존하지 않는다. Unity 에디터와 C# 컴파일러만 필요하며 이미 동작 중인 것이 확인되었다.

SKIPPED (no external dependencies beyond Unity Editor already in use)

---

## Project Constraints (from CLAUDE.md)

| Directive | 영향 |
|-----------|------|
| 추측하지 말 것 — 불확실하면 질문 | 연구 결과 불확실한 항목은 Open Questions에 명시 |
| 계획 없이 코드 작성 금지 | 이 RESEARCH.md는 PLAN.md 생성 전 단계 |
| 현재 Phase 범위만 구현 | 태세 전환 입력, HUD, 콘텐츠는 Deferred — 코드에서 제외 |
| 기존 코드 존중 — 불필요한 리팩토링 금지 | SkillBase / 각 스킬 파일의 포맷팅, 주석, 구조는 유지 |
| 필요한 부분만 수정 (Surgical Changes) | SacrificeWater 호출 2곳, 데미지 변수 적용 위치 정확히 특정됨 |
| 변경은 PLAN의 Directive와 1:1 추적 가능 | 각 수정 위치를 파일명+라인 수준으로 특정함 |

---

## Open Questions

1. **PlayerStanceManager의 mainSkills[]와 PlayerController의 basicAttack/wideSlash/projectile의 중복**
   - What we know: PlayerController는 GetComponent로 자신의 변수(`basicAttack` 등)를 채우고, PlayerStanceManager는 Inspector로 `mainSkills[]`를 채운다. 두 참조가 같은 컴포넌트를 가리켜도 문제없다.
   - What's unclear: Phase 4 범위에서 PlayerController가 skill input을 처리할 때 계속 `basicAttack?.TryUse()`를 호출하는가, 아니면 `StanceManager.GetMainSkill(0)?.TryUse()`로 리디렉션하는가?
   - Recommendation: Phase 4 범위에서는 PlayerController의 기존 입력 핸들러를 변경하지 않는다. 태세 전환 입력은 Deferred이므로 현재는 메인 슬롯 직접 참조가 곧 메인 태세 스킬이다. StanceManager는 데이터 구조만 갖추는 것으로 충분하다.

2. **GetSubSkill()의 null 슬롯 처리**
   - What we know: subSkills[]는 Inspector에서 할당하지 않으면 null이다.
   - What's unclear: null 슬롯을 Assert로 막을지, null을 허용하고 호출자가 null 체크할지?
   - Recommendation: Assert.IsNotNull보다는 null을 반환하고 호출자가 `?.TryUse()`로 처리. Phase 4에서 GetSubSkill 직접 호출 코드가 없으므로 방어적 설계가 맞다.

---

## Sources

### Primary (HIGH confidence)
- `Assets/Player/SkillBase.cs` — 직접 코드 검사. Initialize 흐름, Coroutine 구조, protected 필드 목록 확인
- `Assets/Player/BasicAttackSkill.cs` — 직접 코드 검사. SacrificeWater 호출 없음, 데미지 변수 `baseDamage` 위치 확인
- `Assets/Player/WideSlashSkill.cs` — 직접 코드 검사. SacrificeWater 호출 line 92, CanUse 조건 확인
- `Assets/Player/ProjectileSkill.cs` — 직접 코드 검사. SacrificeWater 호출 line 86, CanUse 조건, sniperDamage 별도 필드 확인
- `Assets/Player/PlayerController.cs` — 직접 코드 검사. GetComponent 단일 인스턴스 반환, InitSkill 흐름, Start() 구조 확인
- `Assets/Player/IPlayerContext.cs` — 직접 코드 검사. 현재 프로퍼티 목록 확인
- `Assets/Player/PlayerWaterStats.cs` — 직접 코드 검사. `SacrificeWater(float cost)` 시그니처 확인

### Secondary (MEDIUM confidence)
- `.planning/phases/04-player-stance-system/04-CONTEXT.md` — 설계 결정사항
- `.planning/REQUIREMENTS.md` — STN-01~05 요구사항 정의

---

## Metadata

**Confidence breakdown:**
- SacrificeWater 호출 위치: HIGH — 직접 코드 검사로 2개 정확히 특정
- 데미지 변수 위치: HIGH — 직접 코드 검사로 각 스킬별 정확히 특정
- GetComponent 단일 반환 동작: HIGH — Unity 공식 동작, 코드에서 확인됨
- 초기화 흐름 함정: HIGH — PlayerController.Start() 코드 직접 확인
- CanUse 함정 (STN-03 연관): HIGH — WideSlashSkill, ProjectileSkill 코드 직접 확인

**Research date:** 2026-05-11
**Valid until:** 2026-06-10 (코드베이스 안정적, 30일 유효)
