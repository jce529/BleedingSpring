# Roadmap: Bleeding Spring — v1.0 UI 시스템 구축

## Overview

이 마일스톤은 게임의 핵심 전투 정보를 실시간으로 시각화하는 UI 인프라를 처음부터 구축한다.
Phase 1에서 플레이어 자신의 위험 상태를 화면에 드러내고, Phase 2에서 적의 오염도와 Sweet Spot을 월드 스페이스로 표시해 핵심 전투 의사결정 루프를 완성한다. Phase 3에서 보스 전투 전용 Screen Space UI를 추가해 마일스톤 목표를 달성한다.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Player HUD** - 플레이어 오염도/워터 티어/위험 비네트를 HUD에 표시하고 기존 이벤트에 연동
- [ ] **Phase 2: Enemy World Space UI** - 적 머리 위 HP/오염도 바와 Sweet Spot 구간 하이라이트를 월드 스페이스로 구현
- [ ] **Phase 3: Boss UI** - 보스 전투 전용 Screen Space HP 바와 적 월드 스페이스 바 억제 로직 추가

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
**Plans**: TBD
**UI hint**: yes

### Phase 2: Enemy World Space UI
**Goal**: 플레이어가 모든 일반 적 위에서 HP, 오염도, Sweet Spot 구간을 즉시 파악할 수 있다
**Depends on**: Phase 1
**Requirements**: ENM-01, ENM-02, ENM-03, ENM-04, ENM-05, ENM-06, TECH-02, TECH-03
**Success Criteria** (what must be TRUE):
  1. 적에게 데미지를 주는 순간 적 머리 위에 HP 바와 오염도 바가 나타나고, 적이 이동해도 바가 따라온다
  2. 오염도 바 위에 Sweet Spot 구간이 다른 색상의 구간으로 강조 표시되고, 적 타입마다 구간 위치가 다르게 표시된다
  3. 적이 죽으면 HP/오염도 바가 사라지고, 정화/파괴 결과에 따라 "PURIFIED" 또는 "DESTROYED" 텍스트가 잠깐 표시된 후 사라진다
  4. `EnemyStats`의 `OnHpChanged` 이벤트가 존재하고 HP 바가 이 이벤트에 구독해 업데이트된다
  5. 모든 UI 바 스크립트가 `Bind()` / `Unbind()` 패턴을 따르며, 정화 경로에서 적 오브젝트가 NPC로 변환될 때 이벤트 누수가 발생하지 않는다
**Plans**: TBD
**UI hint**: yes

### Phase 3: Boss UI
**Goal**: 보스 전투 중 보스 HP가 화면 하단 전용 바로 표시되고 일반 적 바와 혼재되지 않는다
**Depends on**: Phase 2
**Requirements**: BOSS-01, BOSS-02, BOSS-03, BOSS-04
**Success Criteria** (what must be TRUE):
  1. 보스 방에 진입하면 화면 하단에 보스 전용 HP 바와 보스 이름 텍스트가 활성화되고 실시간으로 HP를 반영한다
  2. 보스가 죽으면 화면 하단 보스 HP 바가 비활성화된다
  3. 보스 전투 중에는 보스 캐릭터 위에 월드 스페이스 HP/오염도 바가 나타나지 않는다
**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Player HUD | 0/TBD | Not started | - |
| 2. Enemy World Space UI | 0/TBD | Not started | - |
| 3. Boss UI | 0/TBD | Not started | - |
