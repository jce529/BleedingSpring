# Phase 1: Player HUD - Context

**Gathered:** 2026-03-27
**Status:** Ready for planning

<domain>
## Phase Boundary

플레이어의 위험 상태를 화면에 전달하는 비네트 시스템 구축.
오염도/HP 비율 기반 3단계 비네트 + ISkill 쿨다운 인터페이스 추가.

**포함:** 비네트 UI, PlayerWaterStats 사망 조건 수정, ISkill TECH-01 추가
**제외:** 오염도 바 UI, HP 바 UI, 워터 티어 HUD (캐릭터 외형으로 별도 처리)

</domain>

<decisions>
## Implementation Decisions

### 사망 조건 변경 (PlayerWaterStats)
- **D-01:** 사망 조건을 `CurrentCorruption >= CurrentCleanWater` (비율 100%)로 변경
  - 기존: `CurrentCorruption >= maxCorruptionThreshold (100f)`
  - 변경: 오염도가 현재 HP와 같거나 넘으면 즉사
  - `CheckDeath()` 메서드 수정 필요, `maxCorruptionThreshold` 절댓값 조건 제거

### 비네트 시스템 (PlayerHUD의 유일한 시각 요소)
- **D-02:** 비네트 트리거 기준 = `CurrentCorruption / CurrentCleanWater` 비율
  - 25% 도달: 1단계 활성화
  - 50% 도달: 2단계 활성화
  - 75% 도달: 3단계 활성화
  - 비율 하락 시 해당 단계 비활성화 (예: 50% 미만이면 1단계로 복귀)
- **D-03:** 비네트 애니메이션 — 단계별 펄스 속도 변화
  - 1단계 (25%): 느린 펄스 (천천히 숨쉬는 느낌)
  - 2단계 (50%): 중간 속도 펄스
  - 3단계 (75%): 빠른 펄스 (긴박한 느낌)
- **D-04:** 비네트 색상 — 단계별 색상 적용
  - 1단계: 연보라색 (light lavender)
  - 2단계: 남색 (indigo/navy blue)
  - 3단계: 보라색 (deep purple)
- **D-05:** 비네트 구현 방식 — Claude's Discretion (전체화면 UI Image + 방사형 그라디언트 텍스처 또는 URP Custom Pass)

### 워터 티어
- **D-06:** 워터 티어(0~3)는 HUD UI 없음 — 캐릭터 외형 변화로만 표현 (해당 시스템은 별도 Phase에서 구현)

### HUD 요소 제거
- **D-07:** 오염도 바(Corruption Bar) 없음 — 비율 기반 비네트로 대체
- **D-08:** HP 바(물) 없음 — 기존 Out of Scope 결정 유지
- **D-09:** 워터 티어 HUD 표시기 없음 — D-06으로 대체

### TECH-01: ISkill 쿨다운 인터페이스
- **D-10:** `ISkill` 인터페이스에 `float CooldownRemaining { get; }` 및 `float CooldownDuration { get; }` 프로퍼티 추가
- **D-11:** `SkillBase`에서 구현 — `cooldownDuration` 필드는 이미 있음, `CooldownRemaining` 추적을 위해 코루틴 내 타이머 필드 추가

### Claude's Discretion
- 비네트 텍스처/셰이더 구현 방식 (UI Image vs URP Post Processing vs Custom Shader)
- 단계 전환 시 색상/강도 보간 방식 (즉시 전환 vs 부드러운 Lerp)
- 비율 계산 주기 (Update마다 vs 이벤트 기반)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Design
- `.planning/REQUIREMENTS.md` — v1 요구사항 원문 (HUD-01~04, TECH-01); 이번 논의에서 HUD-01~03 설계가 변경됨 — 이 CONTEXT.md 결정이 우선함
- `.planning/PROJECT.md` — 프로젝트 핵심 원칙 및 제약 (Unity 6 URP, 기존 인터페이스 호환)

### 수정 대상 코드
- `Assets/Player/PlayerWaterStats.cs` — `CheckDeath()` 사망 조건 수정 대상; `OnCorruptionChanged`, `OnWaterChanged` 이벤트 사용
- `Assets/Player/ISkill.cs` — TECH-01: `CooldownRemaining`, `CooldownDuration` 프로퍼티 추가 대상
- `Assets/Player/SkillBase.cs` — TECH-01 구현 대상; `cooldownDuration` 필드 및 `IsOnCooldown` 이미 존재

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `PlayerWaterStats.OnCorruptionChanged(float current, float max)` — 비율 계산에 사용 (`current / max` → 단, D-01에 따라 분모는 `CurrentCleanWater`로 변경)
- `PlayerWaterStats.OnWaterChanged(float current, float max)` — HP 변화 시 비율 재계산 트리거
- `SkillBase.cooldownDuration` (float, SerializeField) — `CooldownDuration` 프로퍼티 반환에 그대로 사용 가능
- `SkillBase.IsOnCooldown` (bool) — 쿨다운 타이머 추적과 함께 사용

### Established Patterns
- C# `Action` 이벤트 기반 디커플링: UI는 `PlayerWaterStats` 이벤트를 구독해 업데이트
- `MonoBehaviour` 컴포넌트 패턴: HUD 스크립트는 별도 GameObject에 부착

### Integration Points
- 비네트 스크립트가 구독할 이벤트: `PlayerWaterStats.OnCorruptionChanged` + `OnWaterChanged` (둘 다 비율에 영향)
- 비율 계산: `ratio = corruption / currentHP` (HP가 0이면 예외 처리 필요)
- 비네트 Canvas: Screen Space - Overlay, 전체화면 덮는 Image 컴포넌트

</code_context>

<specifics>
## Specific Ideas

- 비네트 색상 테마: 연보라(lavender) → 남색(indigo) → 보라(deep purple) — 오염의 어둠이 점점 짙어지는 느낌
- 펄스 속도 단계: 느림(약 2초 주기) → 중간(약 1초) → 빠름(약 0.4초) — Claude 재량으로 정확한 수치 조정 가능
- 사망 조건 변경이 핵심 게임플레이 루프에 영향: 스킬 사용(HP 소모)이 오염도 위험을 직접 높임 → 고위험 고보상 결정이 더 긴박해짐

</specifics>

<deferred>
## Deferred Ideas

- **워터 티어 캐릭터 외형 변화** — 스프라이트 교체/셰이더로 0~3단계 외형 구분. 새로운 캐릭터 비주얼 시스템이므로 별도 Phase에서 구현
- **스킬 쿨다운 UI** — v2 Requirements(SKILL-01~03)에 이미 계획됨; TECH-01은 그 인터페이스 준비만

</deferred>

---

*Phase: 01-player-hud*
*Context gathered: 2026-03-27*
