# Phase 1: Player HUD - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-27
**Phase:** 01-player-hud
**Areas discussed:** HUD 레이아웃 배치, 오염도 바 비주얼, 워터 티어 표시 방식, 위험 비네트 스타일

---

## HUD 레이아웃 / 오염도 바 비주얼

| Option | Description | Selected |
|--------|-------------|----------|
| 기존 요구사항 (오염도 바 + 워터 티어 HUD) | HUD-01~03 그대로 구현 | |
| 비율 기반 비네트로 대체 | 별도 바 없이 Corruption/HP 비율로 비네트 단계 결정 | ✓ |

**User's input:** "오염도를 별도로 바 형식으로 표현하기 보다는 HP는 물을 사용하는 자원이고 오염도와 HP의 비율이 일정이상 넘어가면 플레이어가 죽게끔하고싶어. 비율이 75% 50% 25%일때 비네트 효과로 생명의 위험함을 표시하고 싶어"
**Notes:** 비율 = `CurrentCorruption / CurrentCleanWater`. 오염도 바 UI 완전 제거 결정.

---

## 워터 티어 표시 방식

| Option | Description | Selected |
|--------|-------------|----------|
| HUD 표시기 (숫자/도트/세그먼트) | 화면 UI로 현재 티어 표시 | |
| 캐릭터 외형 변화 | 스프라이트/셰이더로 0~3단계 구분 | ✓ |

**User's input:** "워터 티어는 캐릭터의 모습 변화로 남기고 싶어"
**Notes:** 캐릭터 외형 시스템은 새로운 기능 — 별도 Phase로 기록.

---

## 사망 조건

| Option | Description | Selected |
|--------|-------------|----------|
| 기존 유지 (절댓값 100) | CurrentCorruption >= 100f | |
| 비율 기반으로 변경 | CurrentCorruption >= CurrentCleanWater | ✓ |

**User's choice:** 비율 기반으로 변경
**Notes:** `PlayerWaterStats.CheckDeath()` 수정 필요.

---

## 위험 비네트 애니메이션

| Option | Description | Selected |
|--------|-------------|----------|
| 단계별 펄스 속도 변화 | 25%=느린 펄스, 50%=중간, 75%=빠른 펄스 | ✓ |
| 단계별 강도 고정 (노 펄스) | 고정 비네트, 단계마다 강도만 증가 | |
| 단계 스냅 + 강소 펄스 | 진입 시 짧은 플래시 후 미세한 호흡 | |

**User's choice:** 단계별 펄스 속도 변화

---

## 비네트 색상

| Option | Description | Selected |
|--------|-------------|----------|
| 빨간색 단일 | 모든 단계 빨간색, 강도만 상승 | |
| 노란색 → 주황 → 빨간색 | 전통적 위험 색상 코드 | |
| 트레이드마크 컬러 테마 | 게임 팔레트 연계 | |
| 연보라 → 남색 → 보라색 (Other) | 오염의 어둠이 짙어지는 느낌 | ✓ |

**User's choice:** 연보라(light lavender) → 남색(indigo/navy) → 보라색(deep purple)

---

## Claude's Discretion

- 비네트 구현 방식 (UI Image vs URP Post Processing vs Custom Shader)
- 단계 전환 시 보간 (즉시 vs Lerp)
- 펄스의 정확한 주기 수치

## Deferred Ideas

- 워터 티어 캐릭터 외형 변화 시스템 — 별도 Phase
- 스킬 쿨다운 UI (SKILL-01~03) — v2 Requirements에 계획됨
