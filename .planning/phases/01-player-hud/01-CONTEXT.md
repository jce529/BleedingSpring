# Phase 1: Player HUD - Context

**Gathered:** 2026-03-30 (updated from 2026-03-27)
**Status:** Ready for planning

<domain>
## Phase Boundary

플레이어의 위험 상태를 화면에 전달하는 비네트 시스템 + World Space 세로 HUD 바(물/오염 통합) + ISkill 쿨다운 인터페이스 추가.

**포함:** 비네트 UI (corruption ratio 기반), World Space 세로 HUD 바, 워터 티어 구슬 표시, PlayerWaterStats 사망 조건 수정, ISkill TECH-01 추가
**제외:** 스킬 쿨다운 UI (v2), 정화/파괴 누적 카운터, 미니맵, 보스 UI, 적 월드 스페이스 바

</domain>

<decisions>
## Implementation Decisions

### 사망 조건 변경 (PlayerWaterStats)
- **D-01:** 사망 조건을 `CurrentCorruption >= CurrentCleanWater` (비율 100%)로 변경
  - 기존: `CurrentCorruption >= maxCorruptionThreshold (100f)`
  - 변경: 오염도가 현재 HP와 같거나 넘으면 즉사
  - `CheckDeath()` 메서드 수정 필요, `maxCorruptionThreshold` 절댓값 조건 제거

### 비네트 시스템 (ambient danger warning)
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
- **D-15:** HP 단독 비네트 없음 — HP바(D-12/D-13)가 시각적 피드백을 담당. 비네트는 corruption ratio 전용

### World Space 세로 HUD 바
- **D-12:** HUD 바 위치 — 플레이어 우측팔 뒤쪽, World Space Canvas에 부착
  - 방향 전환 시 바가 반전되어 항상 팔 뒤쪽에 위치 (좌우 대칭 추종)
  - 플레이어 로컬 좌표계 기반으로 배치 → scale.x 반전에 자동 대응
- **D-13:** 바 디자인 — 단일 세로바, 오염이 물을 잠식하는 형태
  - 물(맑은 파란색): 위에서부터 채워짐
  - 오염(보라/검정): 아래에서 위로 잠식해 올라옴
  - 두 영역이 같은 바 공간을 공유 → 오염이 늘수록 물 영역이 줄어드는 직관적 시각화
  - 사망 조건(D-01)과 시각적으로 일치: 오염이 물과 만나면 즉사
- **D-14:** 워터 티어 표시 — 바 위에 구슬 3개
  - 티어 0: 0개 점등
  - 티어 1: 1개 점등
  - 티어 2: 2개 점등
  - 티어 3: 3개 점등
  - O키 입력 시 즉시 업데이트 (`OnWaterTierChanged` 이벤트 구독)

### HUD 요소 (이전 결정 업데이트)
- **D-06 (폐기):** ~~워터 티어 HUD 없음~~ → D-14 구슬 표시로 대체
- **D-07 (폐기):** ~~오염도 바 없음~~ → D-13 통합 바로 대체
- **D-08 (폐기):** ~~HP 바 없음~~ → D-13 통합 바로 대체

### TECH-01: ISkill 쿨다운 인터페이스
- **D-10:** `ISkill` 인터페이스에 `float CooldownRemaining { get; }` 및 `float CooldownDuration { get; }` 프로퍼티 추가
- **D-11:** `SkillBase`에서 구현 — `cooldownDuration` 필드는 이미 있음, `CooldownRemaining` 추적을 위해 코루틴 내 타이머 필드 추가

### Claude's Discretion
- 비네트 텍스처/셰이더 구현 방식 (UI Image vs URP Post Processing vs Custom Shader)
- 단계 전환 시 색상/강도 보간 방식 (즉시 전환 vs 부드러운 Lerp)
- 비율 계산 주기 (Update마다 vs 이벤트 기반)
- World Space HUD 바 크기 및 정확한 오프셋 값 (팔 위치 기준 픽셀 튜닝)
- 구슬 점등 비활성 상태 표현 방식 (흐리게 vs 빈 외곽선)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Design
- `.planning/REQUIREMENTS.md` — v1 요구사항 원문 (HUD-01~04, TECH-01); 이번 논의에서 HUD-01~04 설계가 변경됨 — 이 CONTEXT.md 결정이 우선함
- `.planning/PROJECT.md` — 프로젝트 핵심 원칙 및 제약 (Unity 6 URP, 기존 인터페이스 호환)

### 수정 대상 코드
- `Assets/Player/PlayerWaterStats.cs` — `CheckDeath()` 사망 조건 수정 대상; `OnCorruptionChanged`, `OnWaterChanged`, `OnWaterTierChanged` 이벤트 HUD 구독에 사용
- `Assets/Player/ISkill.cs` — TECH-01: `CooldownRemaining`, `CooldownDuration` 프로퍼티 추가 대상
- `Assets/Player/SkillBase.cs` — TECH-01 구현 대상; `cooldownDuration` 필드 및 `IsOnCooldown` 이미 존재

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `PlayerWaterStats.OnCorruptionChanged(float current, float max)` — 오염 잠식 시각화에 사용
- `PlayerWaterStats.OnWaterChanged(float current, float max)` — 물 영역 갱신 트리거
- `PlayerWaterStats.OnWaterTierChanged(int tier)` — 구슬 점등 업데이트 트리거
- `SkillBase.cooldownDuration` (float, SerializeField) — `CooldownDuration` 프로퍼티 반환에 그대로 사용 가능
- `SkillBase.IsOnCooldown` (bool) — 쿨다운 타이머 추적과 함께 사용

### Established Patterns
- C# `Action` 이벤트 기반 디커플링: UI는 `PlayerWaterStats` 이벤트를 구독해 업데이트
- `MonoBehaviour` 컴포넌트 패턴: HUD 스크립트는 별도 GameObject에 부착
- 캐릭터 방향: `scale.x` 부호로 좌우 반전 (World Space HUD 바 위치 계산에 활용)

### Integration Points
- 비네트 스크립트 구독: `PlayerWaterStats.OnCorruptionChanged` + `OnWaterChanged` (ratio = corruption / currentHP)
- HUD 바 스크립트 구독: `OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged`
- HUD 바 위치: 플레이어 Transform 자식으로 배치 → 자동 추종. 방향 반전은 로컬 X 오프셋 부호로 처리
- HP가 0일 때 ratio 계산 예외 처리 필요 (divide-by-zero)

</code_context>

<specifics>
## Specific Ideas

- 비네트 색상 테마: 연보라(lavender) → 남색(indigo) → 보라(deep purple) — 오염의 어둠이 점점 짙어지는 느낌
- 펄스 속도 단계: 느림(약 2초 주기) → 중간(약 1초) → 빠름(약 0.4초) — Claude 재량으로 정확한 수치 조정 가능
- HUD 바 시각 언어: 물(파란 투명감)이 오염(어두운 보라/검정)에 잠식당하는 형태 — D-01 사망 조건과 시각적으로 일치
- 사망 조건 변경이 핵심 게임플레이 루프에 영향: 스킬 사용(HP 소모)이 오염도 위험을 직접 높임 → 고위험 고보상 결정이 더 긴박해짐
- 참고 레퍼런스: 산나비(Sanabi) 스타일 — 캐릭터 옆에 붙어 움직이는 컴팩트한 World Space HUD

</specifics>

<deferred>
## Deferred Ideas

- **워터 티어 캐릭터 외형 변화** — 스프라이트 교체/셰이더로 0~3단계 외형 구분. 새로운 캐릭터 비주얼 시스템이므로 별도 Phase에서 구현
- **스킬 쿨다운 UI** — v2 Requirements(SKILL-01~03)에 이미 계획됨; TECH-01은 그 인터페이스 준비만
- **HUD 바 ghost bar (지연 트레일)** — 피격 시 이전 HP 값이 잠시 남아있다가 줄어드는 폴리시. v2 Polish 패스

</deferred>

---

*Phase: 01-player-hud*
*Context gathered: 2026-03-27 | Updated: 2026-03-30*
