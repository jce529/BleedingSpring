---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: completed
stopped_at: Completed Milestone v1.0
last_updated: "2026-05-11T05:20:00.000Z"
last_activity: 2026-05-11
progress:
  total_phases: 4
  completed_phases: 4
  total_plans: 11
  completed_plans: 11
---

# Project State — Bleeding Spring (혈연)

## Project Reference

**What This Is:** Unity 6 (2D URP) + C# 2D 하드코어 액션 로그라이크 게임.
플레이어의 생존 영역이 물리적으로 수축하는 독특한 HUD 시스템을 가진 하드코어 액션.

**Core Value:** 수축하는 생존 컨테이너 (Shrinking HUD) — 자원 소모 시 HUD 바가 직접 짧아지며 오염도가 차오르는 시각적 압박.

## Current Position

Phase: 04 (player-stance-system) — COMPLETED
Plan: 2 of 2

- **Milestone:** v1.0 — UI 시스템 구축
- **Phase:** 4 of 4 — Player Stance System
- **Plan:** 04-01 Complete, 04-02 Complete
- **Status:** Milestone Completed
- **Last activity:** 2026-05-11

## Progress

`[▓▓▓▓▓▓▓▓▓▓] 100% (All Phases Complete)`

- [x] 프로젝트 초기화 (PROJECT.md)
- [x] 요구사항 정의 (REQUIREMENTS.md)
- [x] ROADMAP.md 생성
- [x] Phase 1 — Player HUD
- [x] Phase 2 — Enemy World Space UI
- [x] Phase 3 — Boss UI
- [x] Phase 4 — Player Stance System

## Recent Decisions

- HUD 형태: 플레이어 뒤를 따르는 세로형 수축 바 (Vertical Shrinking Bar)
- 오염도 로직: 현재 물(HP) 대비 상대적 비율 (CurrentCorruption / CurrentCleanWater)
- 워터 티어: 캐릭터 머리 위 3개 구슬로 분리 배치
- 저체력 비네트: 25% 미만 시 화면 빨간색 펄스 효과 추가
- **보스 UI 관리:** BossUIManager 싱글톤을 통한 생명주기 및 페이드 효과 중앙 관리
- **태세 시스템:** StanceManager를 통한 메인/보조 태세 전환 및 스킬 배율(0.5x) 적용
- **HP 소모 로직:** SkillBase에 통합하여 모든 공격 시 워터 티어별 HP 소모(SacrificeWater) 적용 완료

## Session Continuity

Last session: 2026-05-11T05:20:00.000Z
Stopped at: Completed Milestone v1.0
Resume file: None
