# Phase 04 Plan 01: Player Stance Infrastructure Summary

## Objective
태세 시스템의 기반 인프라를 구축한다: StanceType enum, PlayerStanceManager 컴포넌트, SkillBase 배율 필드, IPlayerContext 확장, PlayerController 연동.

## Key Changes
- **StanceType Enum**: `Assets/Player/StanceType.cs` 생성. `None`, `Water` 태세 정의.
- **PlayerStanceManager**: `Assets/Player/PlayerStanceManager.cs` 생성. 메인/보조 태세 종류 및 각 3개의 스킬 슬롯(`SkillBase[]`) 관리.
- **SkillBase 확장**: `costMultiplier`, `effectMultiplier` 필드 추가. 태세별 스킬 배율 적용을 위한 토대 마련.
- **IPlayerContext 확장**: `StanceManager` 프로퍼티 추가하여 스킬에서 태세 정보 접근 가능하게 함.
- **PlayerController 연동**: `Awake()`에서 `PlayerStanceManager` 참조 획득 및 `Start()`에서 보조 스킬 초기화(`StanceManager.Initialize(this)`) 수행.

## Key Files
- `Assets/Player/StanceType.cs` (Created)
- `Assets/Player/PlayerStanceManager.cs` (Created)
- `Assets/Player/SkillBase.cs` (Modified)
- `Assets/Player/IPlayerContext.cs` (Modified)
- `Assets/Player/PlayerController.cs` (Modified)

## Key Decisions
- **보조 스킬 초기화 책임**: `PlayerController`는 메인 스킬들만 직접 초기화하고, 보조 스킬들은 `PlayerStanceManager`가 담당하도록 분리하되, 초기화 트리거는 `PlayerController.Start()`에서 수행하도록 설계함.
- **하위 호환성 유지**: `SkillBase`에 추가된 배율 필드의 기본값을 `1.0f`로 설정하여 기존 스킬들의 동작에 영향이 없도록 함.

## Deviations
None - plan executed exactly as written.

## Self-Check: PASSED
- [x] StanceType enum exists and has None, Water.
- [x] PlayerStanceManager class exists with Initialize, GetMainSkill, GetSubSkill, SetStances.
- [x] SkillBase has costMultiplier and effectMultiplier.
- [x] IPlayerContext has StanceManager property.
- [x] PlayerController initializes StanceManager in Awake and Start.
- [x] Commits made for each task.

## Commits
- `3b7a46c`: feat(04-01): add StanceType enum, PlayerStanceManager and stance multipliers
- `cf1c421`: feat(04-01): integrate PlayerStanceManager with PlayerController
