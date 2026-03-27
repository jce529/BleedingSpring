# Bleeding Spring (혈연)

## What This Is

Bleeding Spring은 Unity 6 (2D URP) + C#으로 제작 중인 2D 하드코어 액션 로그라이크 게임이다.
플레이어는 체력(맑은 물)을 소비해 스킬을 사용하고, 오염도(Corruption)가 쌓이면 즉사하는 이중 자원 구조 속에서
적을 특정 오염도 범위(Sweet Spot) 안에서 처치해 [정화]하거나, 범위를 벗어나 [파괴]하는 선택적 전투를 펼친다.
정화/파괴 누적은 스토리와 세계관(엔딩/맵 분기)에 직접 영향을 미친다.

## Core Value

**Sweet Spot 정화 메커닉** — 적의 오염도 게이지 위에 Sweet Spot 범위를 시각적으로 명확히 표시해,
플레이어가 자원(물)과 오염도를 전략적으로 조율하는 긴장감 있는 의사결정을 체험할 수 있어야 한다.

## Current Milestone: v1.0 UI 시스템 구축

**Goal:** 게임의 핵심 전투 정보(플레이어 오염도, 적 HP/Sweet Spot, 보스 HP)를 실시간으로 시각화하는 UI 인프라를 구축한다.

**Target features:**
- 플레이어 HUD — 오염도 바(위험 경고 포함), 워터 티어(0~3단계), 위험 비네트
- 적 월드 스페이스 UI — HP 바 + 오염도 바 + Sweet Spot 구간 하이라이트
- 보스 전용 UI — 화면 하단 Screen Space HP 바 + 이름 텍스트
- 기술 전제조건 — ISkill 쿨다운 프로퍼티, OnHpChanged 이벤트, Bind/Unbind 패턴

## Requirements

### Validated

*기존 코드베이스에서 구현 확인된 기능들*

- ✓ 플레이어 이동 (좌우 이동, 점프, 대시 + 잔상 VFX) — 기존 코드베이스
- ✓ 입력 시스템 (Unity Input System, `InputHandler`) — 기존 코드베이스
- ✓ 이중 자원 시스템 (맑은 물 HP + 오염도 Corruption, `PlayerWaterStats`) — 기존 코드베이스
- ✓ 워터 티어 시스템 (0~3단계, O키 전환, 스킬 위력/오염도 출력 조절) — 기존 코드베이스
- ✓ 스킬 3종 (기본 공격, 광역 베기, 투사체) + `ISkill` 인터페이스 — 기존 코드베이스
- ✓ 적 AI FSM (Idle/Patrol/Chase/Attack/Hit/Dead) + `IDamageable` — 기존 코드베이스
- ✓ 적 이중 자원 (HP + 오염도) 및 Sweet Spot 정화/파괴 로직 (`EnemyStats`) — 기존 코드베이스
- ✓ 정화된 적의 NPC 변환 (`PurifiedNPC`) — 기존 코드베이스
- ✓ 게임 상태 관리 (Playing/Paused/GameOver/GameClear, `GameStateManager`) — 기존 코드베이스

### Active

*현재 마일스톤에서 구축할 기능들*

**[Phase 1] UI 시스템**
- [ ] 플레이어 HUD: HP 바 (맑은 물), 오염도 바 — 실시간 이벤트 연동
- [ ] 플레이어 HUD: 워터 티어 표시기 (현재 0~3단계)
- [ ] 적 월드 스페이스 UI: HP 바 + 오염도 바 (적 머리 위 추종)
- [ ] **Sweet Spot 시각화**: 오염도 바 위에 정화 유효 범위를 색상 구간으로 표시
- [ ] 보스 전용 UI: 화면 하단 고정 대형 HP 바 (Screen Space)
- [ ] 스킬 쿨다운 UI: 스킬별 쿨다운 진행 표시

**[Phase 2+] 게임플레이 확장**
- [ ] 던전/맵 기반 런 구조 (룸 이동 → 전투 → 보상 → 보스)
- [ ] 로그라이크 진행 시스템: 런 내 아이템/능력 획득
- [ ] 정화/파괴 누적 카운터 → 스토리 분기 / 맵 변화
- [ ] 메타 진행 (런 간 영구 업그레이드)

### Out of Scope

- 멀티플레이어 — 싱글플레이어 전용 게임; UPM에 Multiplayer Center 설치되어 있으나 비활성화됨
- 모바일/콘솔 빌드 — 현재 Windows PC 64-bit 단일 타겟; Android 설정이 있으나 주목표 아님
- 3D / 원근 렌더링 — URP 2D 고정
- 온라인 리더보드/클라우드 저장 — 현재 마일스톤 범위 밖

## Context

**기존 코드베이스 현황:**
- Unity 6000.3.11f1 (LTS), URP 17.3.0, C# 9.0, .NET Standard 2.1
- SOLID 지향 컴포넌트 아키텍처: `IPlayerContext`, `ISkill`, `IDamageable` 인터페이스로 시스템 간 결합도 낮춤
- 플레이어·적 모두 FSM 패턴 사용
- C# `Action` 이벤트로 스탯 변화 → UI/AI 반응 디커플링 (`OnWaterChanged`, `OnCorruptionChanged`, `OnDamaged` 등)
- **UI 인프라 부재**: UGUI 패키지는 설치되어 있으나 게임 UI 스크립트/프리팹 미구현 상태
- 2D Aseprite Importer 설치 — 아트 에셋 직접 임포트 가능
- Tilemap 시스템 설치 — 레벨 디자인 준비됨

**게임 디자인 핵심:**
- 플레이어 자원이 HP = 스킬 소비 재료라는 고위험 구조 (맑은 물 소비)
- 적 처치 방식이 이진 선택 (정화 vs 파괴): 오염도 컨트롤이 전투 스킬의 핵심
- 정화 누적 → 세계관/엔딩 분기 (선한 루트), 파괴 누적 → 다른 세계 변화

## Constraints

- **Tech Stack**: Unity 6 (2D URP) + C# — 변경 불가
- **플랫폼**: Windows PC Standalone 64-bit 우선
- **아키텍처**: 기존 인터페이스(`IPlayerContext`, `ISkill`, `IDamageable`) 호환 유지
- **UI 렌더링**: Unity UGUI 2.0.0 (기 설치) — 기존 패키지 활용
- **에셋 파이프라인**: Aseprite → Unity 직접 임포트 (`com.unity.2d.aseprite`)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 플레이어 HP = 스킬 소비 재료 | 자원 관리와 전투 공격성을 동일 축으로 연결, 고위험 고보상 플레이 강제 | — Pending |
| Sweet Spot 범위: 구간 하이라이트 방식 | 마커/삼각형보다 직관적; 바 위의 색상 구간으로 목표 범위를 즉시 파악 가능 | — Pending |
| 적 UI: 월드 스페이스 (일반) + 스크린 스페이스 (보스) | 일반 전투의 몰입감 유지, 보스 전의 긴장감 강조 | — Pending |
| 정화/파괴 결과: 스토리/세계관 영향 | 보상 차별화보다 깊은 플레이어 선택 의미 부여; 누적 수치가 엔딩/맵 분기 | — Pending |
| 런 구조: 던전/맵 기반 (Hades/Dead Cells 스타일) | 액션 로그라이크 장르 표준; 룸 단위 진행으로 난이도 조절 용이 | — Pending |

## Evolution

이 문서는 페이즈 전환과 마일스톤 경계에서 진화한다.

**각 페이즈 전환 후** (`/gsd:transition`):
1. 무효화된 요구사항 → Out of Scope로 이동 (이유 기재)
2. 검증된 요구사항 → Validated로 이동 (페이즈 참조)
3. 새로 발생한 요구사항 → Active에 추가
4. 기록할 결정 → Key Decisions에 추가
5. "What This Is"가 여전히 정확한가 → 드리프트 발생 시 수정

**각 마일스톤 후** (`/gsd:complete-milestone`):
1. 전체 섹션 리뷰
2. Core Value 점검 — 여전히 올바른 우선순위인가?
3. Out of Scope 감사 — 제외 이유가 여전히 유효한가?
4. Context를 현재 상태로 업데이트

---
*Last updated: 2026-03-27 — Milestone v1.0 started*
