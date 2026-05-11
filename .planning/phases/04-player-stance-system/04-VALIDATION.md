---
phase: 4
slug: player-stance-system
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-11
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (EditMode + PlayMode) |
| **Config file** | 별도 config 없음 — Unity Test Runner 내장 |
| **Quick run command** | Unity Test Runner > EditMode > Run All |
| **Full suite command** | Unity Test Runner > All Tests > Run All |
| **Estimated runtime** | ~수동 (자동화 테스트 없음 — PlayMode 수동 검증) |

---

## Sampling Rate

- **After every task commit:** Unity 컴파일 오류 없음 확인 (Build 또는 편집기 재진입)
- **After every plan wave:** Unity Play Mode 진입 후 수동 검증 체크리스트 실행
- **Before `/gsd:verify-work`:** 전체 수동 검증 체크리스트 통과
- **Max feedback latency:** N/A (수동)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------|-------------------|--------|
| STN-01 | 01 | 1 | STN-01 | 컴파일 | Unity 편집기 재로드 후 오류 없음 | ⬜ pending |
| STN-03a | 01 | 1 | STN-03 | 컴파일 | Unity 편집기 재로드 후 오류 없음 | ⬜ pending |
| STN-04 | 01 | 1 | STN-04 | 컴파일 | Unity 편집기 재로드 후 오류 없음 | ⬜ pending |
| STN-02 | 02 | 2 | STN-02 | PlayMode manual | Inspector 슬롯 연결 후 Play Mode — NullRef 없음 | ⬜ pending |
| STN-03b | 02 | 2 | STN-03 | PlayMode manual | Debug.Log로 HP 소모량 50% 확인 | ⬜ pending |
| STN-05 | 02 | 2 | STN-05 | PlayMode manual | 기존 스킬 데미지/소모량 동일 확인 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

현재 Assets/Tests/ 디렉토리 없음. 이 Phase의 요구사항은 Unity MonoBehaviour 생명주기에 의존하므로 EditMode 단독 테스트보다 PlayMode 수동 검증이 현실적이다.

- [ ] 자동화 테스트 없음 — 모든 검증은 Unity Play Mode 수동 확인으로 수행

*기존 인프라 없음 — 전 Phase에서 테스트 인프라 미구축*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PlayerStanceManager Inspector 슬롯 할당 및 NullRef 없음 | STN-02 | MonoBehaviour 생명주기 의존 | Inspector에서 mainSkills[0~2], subSkills[0~2] 슬롯 연결 후 Play Mode 진입. Console에 NullRef 없음 확인 |
| 보조 태세 HP 소모량 50% 감소 | STN-03 | 런타임 SacrificeWater 호출값 확인 | sub 슬롯 스킬(costMultiplier=0.5) 사용 시 Debug.Log에서 HP 소모량이 main의 절반임을 확인 |
| 보조 태세 데미지 50% 감소 | STN-03 | 런타임 데미지 값 확인 | sub 슬롯 스킬(effectMultiplier=0.5) 사용 시 적 HP 감소량이 main의 절반임을 확인 |
| 기존 스킬 하위 호환 | STN-05 | 기존 동작 유지 확인 | costMultiplier=1, effectMultiplier=1인 main 슬롯 스킬로 기존과 동일한 데미지/소모량 확인 |

---

## Validation Sign-Off

- [ ] 모든 태스크에 컴파일 검증 또는 PlayMode 수동 검증 지정
- [ ] Wave별 수동 검증 체크리스트 완료
- [ ] STN-01~STN-05 전체 커버
- [ ] `nyquist_compliant: true` 설정 (수동 검증 완료 후)

**Approval:** pending
