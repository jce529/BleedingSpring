# Phase 3: Boss UI - Context

**Gathered:** 2026-04-13
**Status:** Ready for Research & Planning

<domain>
## Phase Boundary

보스 전투 전용 Screen Space UI를 구현한다. 
이 UI는 화면 오른쪽에 고정된 **세로형 수축 바(Vertical Shrinking Bar)** 형태를 취하며, 보스의 이름과 실시간 상태(오염도/정화율)를 표시한다.
보스전 중 일반 적의 월드 스페이스 UI가 보스 위에 나타나지 않도록 억제하는 로직을 포함한다.
보스 AI 패턴이나 페이즈 전환 로직 자체는 이 UI Phase의 범위 밖이다.

</domain>

<decisions>
## Implementation Decisions

### 보스 UI 구조 (Layout & Resource Mapping)
- **D-BOSS-01 (Screen Space):** 보스 UI는 **Screen Space (Overlay)** 캔버스를 사용하며, 화면 **오른쪽(Right-aligned)**에 수직으로 길게 고정된다.
- **D-BOSS-02 (Vertical Shrinking):** 플레이어 및 일반 적과 동일한 시각적 언어를 유지하기 위해 **세로형 수축 바** 형식을 사용한다.
  - **컨테이너 높이:** 보스의 현재 오염도(`CurrentCorruption`)에 따라 바의 전체 높이가 위에서 아래로 수축한다.
  - **정화 게이지:** 수축된 바 내부에서 아래에서 위로 파란색 정화 게이지가 차오른다.
- **D-BOSS-03 (Boss Name):** 바 옆 혹은 위쪽에 보스의 이름(`bossName`)을 표시하는 텍스트 UI를 배치한다.

### 데이터 및 클래스 구조 (Data & Identification)
- **D-BOSS-04 (BossStats):** 기존 `EnemyStats`를 상속받는 **`BossStats`** 클래스를 생성한다. 
  - `string bossName` 필드를 추가로 가진다.
  - 보스만의 특수 이벤트(Phase 전환 등)를 확장할 수 있는 구조를 마련한다.
- **D-BOSS-05 (UI Suppression):** `EnemyWorldSpaceUI`는 초기화 시 부모 오브젝트에서 `BossStats`가 감지되면 자신(`gameObject`)을 비활성화한다. 보스는 오직 화면 우측의 Screen Space UI만 사용한다.

### 활성화 및 연출 (Activation & Feedback)
- **D-BOSS-06 (Trigger):** 보스 방 진입로에 배치된 **`BossRoomTrigger`** 오브젝트가 플레이어 감지 시 이벤트를 발생시켜 UI를 활성화한다.
- **D-BOSS-07 (Transition):** UI가 나타날 때 **Fade In** 효과를 적용하며, 보스가 처치(`OnDeath`)되면 **Fade Out** 후 UI를 비활성화한다.
- **D-BOSS-08 (Sweet Spot Feedback):** 보스가 정화 가능 구간(Sweet Spot)에 진입하면 다음의 이중 피드백을 제공한다:
  1. **UI 바:** 화면 우측의 보스 바 전체가 화이트 플래시 후 펄스(Glow) 효과를 유지한다.
  2. **보스 본체:** 보스 캐릭터 스프라이트 자체가 밝게 빛나거나 특정 이펙트가 발생하여 직접적인 타격 기회임을 알린다.

### 기술 요구사항 (Technical Requirements)
- **D-BOSS-09:** `BossStats`는 `EnemyStats`의 모든 이벤트를 그대로 상속받아 UI와 바인딩한다.
- **D-BOSS-10:** `UIManager` 혹은 전용 `BossUIManager`를 통해 보스 UI의 생명주기(활성/비활성)를 중앙 제어한다.

</decisions>

<specifics>
## Specific Ideas

- 화면 왼쪽(플레이어 HUD), 중앙(전투/적 UI), 오른쪽(보스 UI)으로 정보 레이아웃을 분산하여 보스전 중에도 가독성을 확보함.
- 보스 바의 높이가 수축함에 따라 보스 이름 텍스트의 위치도 함께 이동하거나, 고정된 위치에서 바의 수축을 강조하는 연출 고려.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 핵심 시스템
- `Assets/Enemy/EnemyStats.cs` — 기본 적 스탯 및 정화 로직 (상속 대상).
- `Assets/Enemy/EnemyWorldSpaceUI.cs` — 월드 스페이스 UI 및 억제 로직 참조.
- `Assets/Player/PlayerHUDBar.cs` — 세로형 수축 바의 표준 구현체.

### 이전 Phase Context
- `.planning/phases/01-player-hud/01-CONTEXT.md` — 플레이어 HUD 세로 수축 로직.
- `.planning/phases/02-enemy-world-space-ui/02-CONTEXT.md` — 적 UI 및 Sweet Spot 시각화 방식.

### 요구사항
- `.planning/REQUIREMENTS.md` — BOSS-01~04 상세 내용.

</canonical_refs>

<deferred>
## Deferred Ideas

- **Boss Phase Transition Visuals:** 보스 페이즈 전환 시 UI 바의 색상이 변하거나 연출이 추가되는 기능은 Phase 3 이후 폴리시 패스로 연기.
- **Boss Kill Cinematic:** 보스 사망 시의 슬로우 모션이나 특수 카메라 연출은 UI 범위 밖으로 간주.

</deferred>

---
*Phase: 03-boss-ui*
*Context gathered: 2026-04-13 — Screen-space vertical shrinking bar on the right.*
