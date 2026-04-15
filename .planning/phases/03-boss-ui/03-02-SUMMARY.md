---
phase: 03-boss-ui
plan: 02
subsystem: Boss UI
tags: [UI, Boss, Manager]
dependency_graph:
  requires: ["03-01"]
  provides: ["BossUIManager", "BossRoomTrigger"]
  affects: ["BossHUDBar"]
tech_stack:
  added: []
  patterns: [Singleton, Observer, Trigger-Based Activation]
key_files:
  created:
    - Assets/UI/Boss/BossUIManager.cs
    - Assets/Enemy/BossRoomTrigger.cs
    - Assets/Tests/Editor/BossUIManagerTests.cs
  modified:
    - Assets/UI/Boss/BossHUDBar.cs
decisions:
  - "보스 UI의 생명주기 관리를 BossUIManager 싱글톤으로 집중화하여 관리 포인트 일원화"
  - "BossHUDBar는 데이터 바인딩 및 표시 역할만 수행하도록 역할을 분리하고, 페이드 로직을 매니저로 이관"
metrics:
  duration: 40m
  completed_date: "2026-04-15"
---

# Phase 03 Plan 02: Boss UI Lifecycle Management Summary

## Objective
보스 UI의 활성화, 비활성화 및 화면 연출(Fade In/Out)을 관리하는 시스템을 구축하고, 보스 방 진입 시 UI를 트리거하도록 구현하였습니다.

## Key Changes
- **BossUIManager (Singleton)**
  - 보스 UI의 전체 생명주기를 관리하는 싱글톤 클래스를 구현하였습니다.
  - `ShowBossUI(BossStats boss)`와 `HideBossUI()` 메서드를 제공하며, CanvasGroup을 사용하여 부드러운 페이드 효과를 적용합니다.
  - 보스의 사망(OnDeath) 이벤트를 자동으로 구독하여 보스 처치 시 UI를 숨깁니다.
- **BossRoomTrigger**
  - 플레이어가 보스 구역에 진입하는 것을 감지하는 트리거 클래스를 구현하였습니다.
  - 플레이어 감지 시 `BossUIManager`를 통해 해당 보스의 HUD를 표시합니다.
  - 중복 발동 방지 로직이 포함되어 있습니다.
- **BossHUDBar Refactoring**
  - `BossHUDBar` 내부의 개별적인 페이드 로직 및 사망 감지 로직을 제거하고, `BossUIManager`가 제어하도록 역할을 단순화하였습니다.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1/3 - Bug/Interaction] BossHUDBar redundant fade logic removal**
- **Found during:** Task 1
- **Issue:** BossHUDBar와 BossUIManager가 각각 독립적으로 페이드 로직을 수행할 경우 애니메이션 충돌 및 중복 구독 문제가 발생할 수 있었습니다.
- **Fix:** BossHUDBar의 페이드 및 사망 구독 로직을 제거하고, BossUIManager가 전체 그룹의 페이드를 제어하며 BossHUDBar에는 바인딩만 수행하도록 구조를 개선하였습니다.
- **Files modified:** Assets/UI/Boss/BossHUDBar.cs
- **Commit:** 24a85a4

## Self-Check: PASSED
- [x] BossUIManager 싱글톤 구현 및 페이드 로직 확인
- [x] BossRoomTrigger 플레이어 감지 및 UI 호출 로직 확인
- [x] BossHUDBar와 BossUIManager 간의 역할 분담 확인
- [x] 컴파일 오류 여부 확인

## Known Stubs
- `BossUIManager`의 `ShowBossUI` 시점에 사운드나 연출 효과가 추가될 수 있는 여지가 있으나 현재는 시각적 페이드만 구현되었습니다.
- `BossRoomTrigger`에서 보스 조우 시 컷신이나 시네마틱 카메라 연출이 추가될 수 있습니다.
