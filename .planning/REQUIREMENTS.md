# Requirements: Bleeding Spring (혈연)

**Defined:** 2026-03-27
**Core Value:** 적 오염도 바 위의 Sweet Spot 범위를 시각적으로 명확히 표시해, 플레이어가 자원(물)과 오염도를 전략적으로 조율하는 긴장감 있는 의사결정을 체험할 수 있어야 한다.

---

## v1 Requirements

### Player HUD

- [x] **HUD-01**: 플레이어 오염도(Corruption) 바가 화면에 표시되고 `PlayerWaterStats.OnCorruptionChanged` 이벤트에 실시간으로 반응한다
- [x] **HUD-02**: 오염도 바는 수치가 80% 이상이 되면 색상이 점진적으로 위험 색(적색/주황)으로 변화해 두 번째 즉사 축을 시각적으로 경고한다
- [x] **HUD-03**: 워터 티어 표시기(0~3단계)가 HUD에 표시되고 O키 전환 시 즉시 업데이트된다
- [x] **HUD-04**: 플레이어 HP가 25% 이하일 때 화면 가장자리에 빨간 펄스/비네트 효과가 나타나 위험 상태를 알린다 (HP 바 없이 HP 값 기반으로 트리거)

### Enemy World Space UI

- [ ] **ENM-01**: 적 머리 위 월드 스페이스 캔버스에 HP 바가 표시되고 적 이동에 따라 추종한다
- [ ] **ENM-02**: 적 HP 바는 적이 데미지를 받는 순간 표시되고, 적이 죽으면 사라진다
- [ ] **ENM-03**: 적 HP 바 아래에 오염도(Corruption) 바가 함께 표시되고 `EnemyStats.OnCorruptionChanged` 이벤트에 실시간으로 반응한다
- [ ] **ENM-04**: 오염도 바 위에 Sweet Spot 유효 범위(예: 10~20%)가 색상이 다른 구간으로 강조 표시된다 — `EnemyStats.sweetSpotMin/Max` 값을 읽어 RectTransform 앵커로 위치를 결정한다
- [ ] **ENM-05**: Sweet Spot 구간 표시는 적 타입별로 다른 범위를 올바르게 반영한다
- [ ] **ENM-06**: 적 처치 시 정화/파괴 결과에 따라 "PURIFIED" 또는 "DESTROYED" 텍스트/아이콘이 적 위치에 잠깐 표시된 후 사라진다

### Boss UI

- [ ] **BOSS-01**: 보스 방 진입 시 화면 하단에 보스 전용 HP 바(Screen Space)가 활성화되어 보스 HP를 실시간으로 표시한다
- [ ] **BOSS-02**: 보스 HP 바 활성화 시 보스 이름 텍스트가 함께 표시된다
- [ ] **BOSS-03**: 보스가 죽으면 보스 HP 바가 비활성화된다
- [ ] **BOSS-04**: 보스 전투 중에는 적 월드 스페이스 HP 바가 보스 위에 표시되지 않는다 (보스는 Screen Space 바만 사용)

### Technical Prerequisites

- [x] **TECH-01**: `ISkill` 인터페이스에 `CooldownRemaining`과 `CooldownDuration` 프로퍼티가 추가된다 (스킬 쿨다운 UI가 v2에서 이 인터페이스를 사용할 수 있도록 준비)
- [ ] **TECH-02**: `EnemyStats`에 `OnHpChanged(float current, float max)` 이벤트가 추가된다 (기존 `OnDamaged` 대신 명시적 이벤트로 HP 바 업데이트)
- [ ] **TECH-03**: 모든 UI 바 스크립트는 `Bind(stats)` / `Unbind()` 패턴을 구현해 이벤트 구독 누수를 방지한다 (특히 Purify 경로에서 `OnDestroy` 미호출 문제 대응)

---

## v2 Requirements

### Skill Cooldown UI

- **SKILL-01**: 3개 스킬 각각에 대해 아이콘 + 쿨다운 필 애니메이션이 HUD에 표시된다
- **SKILL-02**: 쿨다운 중인 스킬 아이콘은 시각적으로 비활성 상태(어둡게 처리)로 표시된다
- **SKILL-03**: 쿨다운 완료 시 아이콘이 강조 표시(플래시 또는 바운스)로 사용 가능함을 알린다

### Polish & Feedback

- **POL-01**: 오염도가 100%에 도달하면 오염도 바가 깜박이는 경고 애니메이션을 표시한다
- **POL-02**: HP 바 (맑은 물)를 선택적으로 HUD에 추가하는 설정 옵션 (플레이테스트 후 필요 여부 결정)
- **POL-03**: 스킬 아이콘에 실제 Aseprite 에셋 적용 (아트 파이프라인 확정 후)
- **POL-04**: 적 HP/오염도 바에 HP 감소 지연 트레일(ghost bar) 추가 — 시각적 폴리시

### Roguelike Progression UI

- **PROG-01**: 런 내 정화/파괴 누적 카운터가 HUD에 표시된다 (Phase 2 스토리 분기 시스템 완성 후)
- **PROG-02**: 런 종료 시 결과 요약 화면 (정화/파괴 수, 생존 시간 등)

---

## Out of Scope

| Feature | Reason |
|---------|--------|
| 플레이어 HP 바 (맑은 물) | 의도적 디자인 결정 — 오염도가 주요 지표, HP는 위험 신호로만 표현 |
| 부유 데미지 숫자 | Phase 2+ 게임플레이 피드백 패스; 현재 적 바 가독성 저해 우려 |
| 적 상태 효과 아이콘 | 상태 이상 시스템 미구현; 해당 시스템 완성 후 함께 설계 |
| 미니맵 / 룸 맵 | 던전 구조가 Phase 2+ 범위; 의존 시스템 없음 |
| 보스 HP 바 연출 애니메이션 (시네마틱 등장) | 구현 비용 대비 효과 낮음; 코어 바 안정화 후 폴리시 패스 |
| 멀티플레이어 UI | 싱글플레이어 전용 게임 |
| 수치 HP/오염도 텍스트 표시 | 빠른 전투 중 가독성 저하; 일시 정지 화면이나 설정에서 옵션으로 고려 |

---

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| HUD-01 | Phase 1 | Complete |
| HUD-02 | Phase 1 | Complete |
| HUD-03 | Phase 1 | Complete |
| HUD-04 | Phase 1 | Complete |
| TECH-01 | Phase 1 | Complete |
| ENM-01 | Phase 2 | Pending |
| ENM-02 | Phase 2 | Pending |
| ENM-03 | Phase 2 | Pending |
| ENM-04 | Phase 2 | Pending |
| ENM-05 | Phase 2 | Pending |
| ENM-06 | Phase 2 | Pending |
| TECH-02 | Phase 2 | Pending |
| TECH-03 | Phase 2 | Pending |
| BOSS-01 | Phase 3 | Pending |
| BOSS-02 | Phase 3 | Pending |
| BOSS-03 | Phase 3 | Pending |
| BOSS-04 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 17 total
- Mapped to phases: 17
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-27*
*Last updated: 2026-03-27 — Roadmap created, traceability confirmed*
