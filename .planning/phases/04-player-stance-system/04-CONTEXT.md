# Phase 4: Player Stance System - Context

**Gathered:** 2026-05-11
**Status:** Ready for planning
**Source:** Design discussion (2026-05-11 session)

<domain>
## Phase Boundary

플레이어가 메인 태세와 보조 태세를 갖추고, 각 태세에 연결된 스킬 슬롯(1~3번)이 독립적으로 동작한다.
보조 태세 스킬은 메인 태세와 동일한 스킬 종류지만 소모량과 효과가 모두 감소한다.

이 Phase는 태세 데이터 구조와 스킬 배율 인프라를 확립한다.
태세 전환 입력, 태세 UI, 콘텐츠(구체적인 태세 종류 추가)는 이 Phase 범위 밖이다.

</domain>

<decisions>
## Implementation Decisions

### 태세 저장 방식
- 별도 MonoBehaviour `PlayerStanceManager`를 만든다. `PlayerStats`나 `PlayerWaterStats`에 합치지 않는다.
- `PlayerStanceManager`는 `MainStance`(StanceType)와 `SubStance`(StanceType) 두 프로퍼티를 가진다.
- 각 태세는 3개의 `SkillBase` 슬롯을 Inspector에서 할당한다 (`SkillBase[] mainSkills`, `SkillBase[] subSkills`, 각 크기 3).

### 스킬 배율 방식 (Option A 선택)
- **래퍼/데코레이터 패턴은 채택하지 않는다.** 현재 `ExecuteSkill()` 내부에서 `SacrificeWater()`를 직접 호출하므로 래퍼가 소모량을 가로챌 수 없다.
- `SkillBase`에 `[SerializeField] protected float costMultiplier = 1f`와 `[SerializeField] protected float effectMultiplier = 1f` 필드를 추가한다.
- 보조 태세용 스킬 컴포넌트는 메인과 **별도 인스턴스**로 플레이어 GameObject에 붙인다. Inspector에서 `costMultiplier = 0.5`, `effectMultiplier = 0.5`로 설정한다.
- 기본값 1.0이므로 기존 스킬(메인 슬롯)은 코드 변경 없이 동일하게 동작한다 (하위 호환).

### IPlayerContext 확장
- `IPlayerContext` 인터페이스에 `PlayerStanceManager StanceManager { get; }` 프로퍼티를 추가한다.
- `PlayerController`가 이를 구현한다.

### StanceType enum
- 별도 파일 `StanceType.cs`로 정의한다.
- MVP 범위에서는 값 정의만 한다 (예: `None`, `Water`). 구체적 태세 콘텐츠는 이 Phase 범위 밖.

### 스킬 배율 적용 위치
- `SacrificeWater(cost)` 호출 시: `cost * costMultiplier`
- 각 스킬의 데미지/범위 계산 시: 해당 값 × `effectMultiplier`
- `SkillBase`가 공통 헬퍼를 제공하거나, 각 구체 스킬 클래스가 `effectMultiplier`를 직접 참조한다.

### Claude's Discretion
- `PlayerStanceManager.SetStances(main, sub)` 메서드 시그니처의 구체적 설계
- `GetMainSkill(int slot)` / `GetSubSkill(int slot)` 접근자 설계
- 슬롯 범위 유효성 검사 방식 (Assert vs. Clamp)
- `StanceType` 초기 enum 값 목록 (최소한의 값만 정의)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 핵심 수정 대상 파일
- `Assets/Player/SkillBase.cs` — costMultiplier / effectMultiplier 필드 추가 대상. 현재 SacrificeWater 호출 위치 확인 필수
- `Assets/Player/ISkill.cs` — 스킬 인터페이스. 배율 필드는 인터페이스에 노출하지 않음 (SkillBase 전용)
- `Assets/Player/IPlayerContext.cs` — StanceManager 프로퍼티 추가 대상
- `Assets/Player/PlayerController.cs` — IPlayerContext 구현체. StanceManager 반환 추가 필요
- `Assets/Player/BasicAttackSkill.cs` — 하위 호환 검증 대상 스킬 #1
- `Assets/Player/WideSlashSkill.cs` — 하위 호환 검증 대상 스킬 #2
- `Assets/Player/ProjectileSkill.cs` — 하위 호환 검증 대상 스킬 #3

### 새로 생성할 파일
- `Assets/Player/StanceType.cs` — StanceType enum
- `Assets/Player/PlayerStanceManager.cs` — 태세 상태 관리 컴포넌트

</canonical_refs>

<specifics>
## Specific Ideas

### PlayerStanceManager 골격 (논의 기반)
```csharp
public class PlayerStanceManager : MonoBehaviour
{
    public StanceType MainStance { get; private set; }
    public StanceType SubStance  { get; private set; }

    [SerializeField] private SkillBase[] mainSkills; // 크기 3
    [SerializeField] private SkillBase[] subSkills;  // 동일 종류, 배율만 다름

    public ISkill GetMainSkill(int slot) => mainSkills[slot];
    public ISkill GetSubSkill(int slot)  => subSkills[slot];

    public void SetStances(StanceType main, StanceType sub) { ... }
}
```

### Inspector 세팅 예시
```
Player GameObject
  ├── WideSlashSkill (Main)   costMultiplier=1.0  effectMultiplier=1.0
  ├── WideSlashSkill (Sub)    costMultiplier=0.5  effectMultiplier=0.5
  ├── ProjectileSkill (Main)  costMultiplier=1.0  effectMultiplier=1.0
  └── ProjectileSkill (Sub)   costMultiplier=0.5  effectMultiplier=0.5
```

### SkillBase 배율 적용 예시
```csharp
// SkillBase에 추가
[Header("태세 배율 (보조 태세용 인스턴스는 0.5 설정)")]
[SerializeField] protected float costMultiplier   = 1f;
[SerializeField] protected float effectMultiplier = 1f;

// 각 스킬의 SacrificeWater 호출 수정
Context.Stats.SacrificeWater(baseCost * costMultiplier);

// 데미지 계산 수정
float damage = baseDamage * effectMultiplier;
```

</specifics>

<deferred>
## Deferred Ideas

- 태세 전환 입력 키 바인딩 — 태세 전환 UX 설계 후 별도 Phase
- 태세별 HUD 표시 (현재 태세 아이콘 등) — UI Phase와 통합 예정
- 구체적인 태세 콘텐츠 (Water 태세, Fire 태세 등 실제 스킬 세트 구성) — 콘텐츠 Phase 범위
- 태세별 패시브 효과 — 전투 시스템 확장 단계에서 설계

</deferred>

---

*Phase: 04-player-stance-system*
*Context gathered: 2026-05-11 via design discussion*
