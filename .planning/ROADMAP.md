# Roadmap: Bleeding Spring — v1.0 UI 시스템 구축

## Overview

이 마일스톤은 게임의 핵심 전투 정보를 실시간으로 시각화하는 UI 인프라를 처음부터 구축한다.
Phase 1에서 플레이어 자신의 위험 상태를 화면에 드러내고, Phase 2에서 적의 오염도와 Sweet Spot을 월드 스페이스로 표시해 핵심 전투 의사결정 루프를 완성한다. Phase 3에서 보스 전투 전용 Screen Space UI를 추가해 마일스톤 목표를 달성한다.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Player HUD** - 플레이어 오염도/워터 티어/위험 비네트를 HUD에 표시하고 기존 이벤트에 연동
- [ ] **Phase 2: Enemy World Space UI** - 적 머리 위 HP/오염도 바와 Sweet Spot 구간 하이라이트를 월드 스페이스로 구현
- [ ] **Phase 3: Boss UI** - 보스 전투 전용 Screen Space HP 바와 적 월드 스페이스 바 억제 로직 추가
- [ ] **Phase 4: Player Stance System** - 플레이어 메인/보조 태세 저장 구조와 태세별 스킬 슬롯 시스템 구현

## Phase Details

### Phase 1: Player HUD
**Goal**: 플레이어가 자신의 오염도, 워터 티어, HP 위험 상태를 즉시 읽을 수 있다
**Depends on**: Nothing (first phase)
**Requirements**: HUD-01, HUD-02, HUD-03, HUD-04, TECH-01
**Success Criteria** (what must be TRUE):
  1. 플레이어가 스킬을 사용해 오염도가 오르면 HUD의 오염도 바가 실시간으로 증가한다
  2. 오염도가 80% 이상이 되면 오염도 바 색상이 점진적으로 붉은색/주황으로 변해 위험을 경고한다
  3. O키를 눌러 워터 티어를 전환하면 HUD의 티어 표시기가 즉시 해당 단계(0~3)로 업데이트된다
  4. 플레이어 HP가 25% 이하로 떨어지면 화면 가장자리에 빨간 비네트/펄스가 나타나고, HP가 회복되면 사라진다
  5. `ISkill` 인터페이스에 `CooldownRemaining`과 `CooldownDuration` 프로퍼티가 존재해 컴파일 오류 없이 빌드된다
**Plans**: 3 plans
**UI hint**: yes

### Phase 2: Enemy World Space UI
**Goal**: 플레이어가 모든 일반 적 위에서 HP, 오염도, Sweet Spot 구간을 즉시 파악할 수 있다
**Depends on**: Phase 1
**Requirements**: ENM-01, ENM-02, ENM-03, ENM-04, ENM-05, ENM-06, TECH-02, TECH-03
**Success Criteria** (what must be TRUE):
  1. 적에게 데미지를 주는 순간 적 머리 뒤쪽에 수직형 수축 오염도 바가 나타나고, 적이 이동해도 바가 따라온다
  2. 오염도 바의 높이가 현재 오염도에 비례하여 수축하며, 아래서부터 파란색 정화율이 차오른다
  3. 오염도 바 위에 Sweet Spot 구간이 강조 표시되고, 바가 수축하더라도 구간 위치가 상대적으로 유지된다
  4. `EnemyStats`의 `OnCorruptionChanged` 이벤트에 맞춰 바의 높이와 채움이 실시간으로 업데이트된다
  5. 모든 UI 바 스크립트가 `Bind()` / `Unbind()` 패턴을 따르며, 정화 경로에서 적 오브젝트가 NPC로 변환될 때 이벤트 누수가 발생하지 않는다
**Plans**: 3 plans
**UI hint**: yes

### Phase 3: Boss UI
**Goal**: 보스 전투 중 화면 우측 고정 세로형 수축 바로 보스 상태가 표시되고 일반 적 바와 혼재되지 않는다
**Depends on**: Phase 2
**Requirements**: BOSS-01, BOSS-02, BOSS-03, BOSS-04
**Success Criteria** (what must be TRUE):
  1. 보스 방에 진입하면 화면 우측에 보스 전용 HUD 바와 이름이 활성화되고 실시간 상태를 반영한다
  2. 보스 전투 중에는 보스 캐릭터 위에 일반 월드 스페이스 바가 나타나지 않는다
  3. 보스가 Sweet Spot 구간에 진입하면 UI 바에 Glow 펄스 시각 피드백이 발생한다
  4. 보스가 죽으면 화면 우측 HUD 바가 페이드 아웃되며 비활성화된다
**Plans**: 3 plans
- [x] 03-01-PLAN.md — 핵심 데이터(BossStats) 및 억제 로직 구현
- [x] 03-02-PLAN.md — UI 관리(BossUIManager) 및 진입 트리거 구현
- [ ] 03-03-PLAN.md — UI 프리팹 구성(EDITOR-GUIDE) 및 최종 UAT
**UI hint**: yes

### Phase 4: Player Stance System
**Goal**: 플레이어가 메인 태세와 보조 태세를 장착하고, 각 태세의 스킬 슬롯(1~3)이 독립적으로 동작하며 보조 태세 스킬은 소모량과 효과가 감소한다
**Depends on**: Phase 1
**Requirements**: STN-01, STN-02, STN-03, STN-04, STN-05
**Success Criteria** (what must be TRUE):
  1. `PlayerStanceManager`가 메인 태세와 보조 태세(StanceType)를 저장하고, 각각 3개의 ISkill 슬롯을 Inspector에서 할당할 수 있다
  2. 보조 태세 스킬 슬롯의 `costMultiplier`와 `effectMultiplier`가 1.0 미만(예: 0.5)으로 설정되면, `SacrificeWater()` 소모량과 실제 효과(데미지/범위)가 해당 배율만큼 감소한다
  3. `IPlayerContext`에 `StanceManager` 프로퍼티가 추가되어 스킬 클래스에서 현재 태세 정보를 참조할 수 있다
  4. 같은 종류의 스킬 컴포넌트(예: WideSlashSkill)가 메인 슬롯과 보조 슬롯에 각각 배치될 수 있으며, 배율만 다르게 동작한다
  5. 기존 BasicAttackSkill / WideSlashSkill / ProjectileSkill의 로직이 변경 없이 유지된다 (하위 호환)
**Plans**: 2 plans
- [ ] 04-01-PLAN.md — 태세 인프라 구축 (StanceType, PlayerStanceManager, SkillBase 배율 필드, IPlayerContext/PlayerController 확장)
- [ ] 04-02-PLAN.md — 스킬 배율 적용 (costMultiplier/effectMultiplier 통합) 및 EDITOR-GUIDE

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Player HUD | 3/3 | Completed | 2026-04-03 |
| 2. Enemy World Space UI | 2/3 | In progress | - |
| 3. Boss UI | 1/3 | In progress | - |
| 4. Player Stance System | 0/2 | Not started | - |
