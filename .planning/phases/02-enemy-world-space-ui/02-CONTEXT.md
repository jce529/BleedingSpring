# Phase 2: Enemy World Space UI - Context (Revised)

**Gathered:** 2026-04-10
**Status:** Ready for planning (Refactoring for Vertical HUD)

<domain>
## Phase Boundary

모든 일반 적 머리 위에 수직형 수축 바(Vertical Shrinking Bar)를 구현한다.
이 바는 적의 오염도(Corruption)를 베이스로 하며, 정화(Purification) 정도를 채움(Fill)으로 표시한다.
보스 억제 로직, 스킬 쿨다운 UI, 부유 데미지 숫자는 이 Phase 범위 밖이다.

</domain>

<decisions>
## Implementation Decisions

### 수직형 수축 HUD (Vertical Shrinking HUD)
- **D-01 (Layout):** **수직형 바(Vertical Bar)** 형태를 사용하며, 적 스프라이트 **뒤쪽(Behind)**에 World Space로 배치한다.
- **D-02 (Container Height):** 컨테이너의 전체 높이는 **현재 오염도(Current Corruption)**에 따라 수축/팽창한다.
  - `Scale.y = CurrentCorruption / MaxCorruption`.
- **D-03 (Purification Fill):** 바 아래쪽에서부터 **정화율(Purification)**이 파란색/맑은 물 색상으로 차오른다.
  - `Fill Amount = (MaxCorruption - CurrentCorruption) / CurrentCorruption` (현재 컨테이너 높이 대비 상대적 비율).
- **D-04 (HP Indicator):** 별도의 HP 바는 표시하지 않는다. 수축하는 오염도 바가 적의 실질적인 '체력'과 '상태'를 동시에 나타내는 통합 지표(Combined Indicator) 역할을 한다.

### Sweet Spot 시각화
- **D-05 (Markers):** 오염도 바 위에 Sweet Spot 구간을 표시한다.
- **D-06 (Scaling):** Sweet Spot 마커는 바가 수축하더라도 **현재 높이 대비 상대적 비율(Proportional)**로 위치를 유지한다 (`RectTransform` anchors 사용).
- **D-07 (Feedback):** 적의 현재 오염도가 Sweet Spot 범위 안에 들어오면 오버레이 색상이 밝아지거나 반짝이며, 진입하는 순간 **트리거 플래시(Trigger Flash)** 효과를 준다.

### 레이어링 & 렌더링
- **D-08 (Layering):** **Standard UI Layer**를 사용한다 (환경 오브젝트에 의해 가려질 수 있음).
- **D-09 (Sorting):** **동적 Y-정렬(Dynamic Y-Sorting)**을 적용한다. 카메라와 가까운(Y값이 낮은) 적의 UI가 위에 렌더링되도록 `Canvas.sortingOrder`를 업데이트한다.
- **D-10 (Scale):** **World Proportional** 방식을 사용한다. 적 스프라이트 크기에 비례하여 UI 바의 크기도 함께 스케일링된다.

### 표시 타이밍 & 페이드
- **D-11 (Reveal):** 적이 첫 데미지를 입는 순간(`OnDamaged`) 나타난다.
- **D-12 (Fade):** 나타날 때 **0.15초 Snap-In** 페이드인 효과를 적용한다.
- **D-13 (Death):** 적이 죽거나 정화될 때 즉시 사라지며, 이벤트 구독을 해제(`Unbind`)한다.

### 기술 요구사항 (TECH-02, TECH-03)
- **D-14:** `EnemyStats`의 `OnHpChanged`, `OnCorruptionChanged` 이벤트를 모두 활용한다.
- **D-15:** `Bind(stats)` / `Unbind()` 패턴을 엄격히 준수하여 정화된 적이 NPC로 남을 때 메모리 누수를 방지한다.

</decisions>

<specifics>
## Specific Ideas

- 플레이어 HUD(수직 수축형)와 대칭되는 시각적 언어를 적에게도 부여하여 통일감 형성.
- 플레이어는 '물'이 베이스고 아래서부터 '오염'이 차오른다면, 적은 '오염'이 베이스고 아래서부터 '맑은 물(정화)'이 차오르는 역설적 구조.
- Sweet Spot 범위가 컨테이너와 함께 수축하므로, 플레이어는 점점 작아지는 바 안에서 정확한 정화 타이밍을 노려야 함.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Enemy 시스템
- `Assets/Enemy/EnemyStats.cs` — 오염도(Corruption) 데이터 모델 및 정화 로직.
- `Assets/Enemy/EnemyWorldSpaceUI.cs` — 기존 수평형 바 구현체 (수직형으로 리팩토링 대상).

### UI 패턴 레퍼런스 (Phase 1)
- `Assets/Player/PlayerHUDBar.cs` — 수직 수축형 HUD의 참조 구현.
- `.planning/phases/01-player-hud/01-CONTEXT.md` — 플레이어 HUD 결정 사항.

### 요구사항
- `.planning/REQUIREMENTS.md` — ENM-01~06 (수직형으로 재해석 필요).

</canonical_refs>

<code_context>
## Existing Code Insights

### 리팩토링 포인트
- `EnemyWorldSpaceUI.cs`의 `HandleHpChanged`/`HandleCorruptionChanged` 로직을 수직 수축형으로 변경해야 함.
- `containerRect`를 추가하여 `localScale.y`를 `CurrentCorruption` 기반으로 조절.
- `FillAmount` 계산식을 `(Max - Current) / Current`로 변경.

### 주의 사항
- `CurrentCorruption`이 0에 가까워질 때 나눗셈 오류 방지 로직 필요.
- World Space Canvas의 `RectTransform` 설정이 적 스프라이트 뒤쪽을 향하도록 Z-offset 조정.

</code_context>

<deferred>
## Deferred Ideas

- **ENM-06 (PURIFIED/DESTROYED 텍스트):** 현재 결정에 따라 스킵.
- **Ghost bar (트레일 효과):** v2 폴리시 패스로 연기.

</deferred>

---

*Phase: 02-enemy-world-space-ui*
*Context gathered: 2026-04-10 (Revised for Vertical HUD)*
