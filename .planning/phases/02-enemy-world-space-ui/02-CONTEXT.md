# Phase 2: Enemy World Space UI - Context

**Gathered:** 2026-04-06
**Status:** Ready for planning

<domain>
## Phase Boundary

모든 일반 적 머리 위에 HP 바 + 오염도 바 + Sweet Spot 구간을 World Space로 구현한다.
보스 억제 로직, 스킬 쿨다운 UI, 부유 데미지 숫자는 이 Phase 범위 밖이다.

</domain>

<decisions>
## Implementation Decisions

### 바 표시 타이밍 (ENM-01, ENM-02)
- **D-01:** 적이 첫 번째 데미지를 받는 순간(`OnDamaged` 이벤트) 바가 나타난다. 데미지를 받기 전에는 바가 표시되지 않는다.
- **D-02:** 적이 죽을 때(`OnDeath` 이벤트) 바가 사라진다. 자동 숨김 타이머 없음 — 한 번 표시되면 죽을 때까지 유지된다.

### Sweet Spot 시각화 (ENM-04, ENM-05)
- **D-03:** 오염도 바 위에 반투명 오버레이(별도 Image 컴포넌트)를 사용해 Sweet Spot 구간을 표시한다.
- **D-04:** 오버레이의 위치와 크기는 RectTransform anchors로 계산한다: `anchorMin.x = basePurificationMin`, `anchorMax.x = basePurificationMax` (bonus margin 포함).
- **D-05:** 오염도 바의 방향: 왼쪽 = 0% 오염(완전 정화), 오른쪽 = 100% 오염(완전 오염). Sweet Spot은 중간 구간에 위치.
- **D-06:** 적 타입마다 `EnemyStats.basePurificationMin/Max` 값이 다르므로, Bind 시 각 적 인스턴스의 값을 읽어 오버레이 위치를 동적으로 계산한다.

### 처치 결과 피드백 (ENM-06 스킵)
- **D-07:** "PURIFIED" / "DESTROYED" 텍스트 팝업은 구현하지 않는다. 죽으면 오브젝트가 제거되고, 정화되면 PurifiedNPC로 전환되는 것 자체가 충분한 시각적 피드백이다.
- ENM-06은 이 Phase에서 스킵되며 REQUIREMENTS.md 업데이트 필요 (v2 또는 deferred로 이동).

### 바 레이아웃 & 위치 (ENM-01, ENM-03)
- **D-08:** 바는 작고 요약적 — 두께 얇음, 너비는 적 스프라이트 폭 정도.
- **D-09:** 배치 순서: HP 바(위), 오염도 바(아래), 적 머리 위 고정 오프셋으로 위치.
- **D-10:** 적이 이동해도 바가 따라오도록 World Space Canvas를 적의 자식 오브젝트로 배치 (Phase 1 플레이어 HUD와 동일한 World Space 패턴).

### 기술 요구사항 (TECH-02, TECH-03)
- **D-11:** `EnemyStats`에 `OnHpChanged(float current, float max)` 이벤트를 추가한다. HP 바가 `OnDamaged` 대신 이 이벤트로 업데이트한다.
- **D-12:** 모든 적 UI 바 스크립트는 `Bind(EnemyStats stats)` / `Unbind()` 패턴을 구현한다. `Purify` 경로에서 `PurifiedNPC.Activate()`가 호출될 때 `OnDestroy`가 안 불릴 수 있으므로, `Bind()`/`Unbind()`를 명시적으로 관리해야 한다.

### Claude's Discretion
- 정확한 HP 바 두께와 오염도 바 높이 비율 (작고 읽기 쉬운 범위 내)
- Sweet Spot 오버레이 색상 (오염도 바와 구분되는 색)
- 바가 나타날 때 페이드인 여부 (있다면 0.1~0.2초 짧게)
- World Space Canvas의 Sorting Layer 설정

</decisions>

<specifics>
## Specific Ideas

- 정화는 NPC로 살아남는 것 자체가 결과 — 별도 텍스트 팝업 불필요
- Phase 1의 `PlayerHUDBar.cs` Bind/Unbind 패턴을 그대로 적용 (이벤트 누수 방지 우선)
- `EnemyStats.basePurificationMin/Max`는 Inspector에서 적 타입마다 다르게 설정됨 → 구간 표시가 자동으로 차별화됨

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Enemy 시스템
- `Assets/Enemy/EnemyStats.cs` — HP/오염도 데이터, 정화 판정 로직, `basePurificationMin/Max` 필드, `OnDamaged`/`OnDeath` 이벤트 (TECH-02로 `OnHpChanged` 추가 예정)
- `Assets/Enemy/EnemyAttack.cs` — 적 공격 로직 (Bind 패턴 적용 시 연관 컴포넌트)
- `Assets/Enemy/PurifiedNPC.cs` — `Activate()` 호출 시 `EnemyStats.enabled = false` → Unbind 타이밍 주의

### UI 패턴 레퍼런스 (Phase 1)
- `Assets/Player/PlayerHUDBar.cs` — World Space 바의 Bind/Unbind 패턴, 이벤트 구독 레퍼런스 구현
- `.planning/phases/01-player-hud/01-CONTEXT.md` — Phase 1 결정 (World Space Canvas, 이벤트 패턴)

### 요구사항
- `.planning/REQUIREMENTS.md` — ENM-01~06, TECH-02, TECH-03 상세 명세

</canonical_refs>

<code_context>
## Existing Code Insights

### 재사용 가능한 패턴
- `PlayerHUDBar.cs`: `Start()`에서 이벤트 구독, `OnDestroy()`에서 구독 해제 — 동일 패턴을 적 UI 바에 적용
- `EnemyStats.OnDamaged`: 이미 존재 — 이 이벤트로 바를 최초 표시 트리거

### 주의 사항
- `EnemyStats.Purify()`: `enabled = false` 후 `PurifiedNPC.Activate()` 호출 → `OnDestroy` 미호출 가능 → Unbind를 `OnDeath` 콜백에서 명시적으로 처리해야 함
- Sweet Spot anchors: `basePurificationMin`/`basePurificationMax`는 0.0~1.0 범위 비율값 → RectTransform anchorMin/Max.x에 직접 사용 가능
- `bonusPurificationMargin` 런타임 변경 가능 → 오버레이를 정적으로 Bind 시 1회 계산해도 무방 (현재 v1 범위 내)

### 통합 지점
- 각 적 Prefab에 World Space Canvas 자식 추가 + EnemyHPBar 스크립트 연결
- `EnemyStats.TakeDamage()` 호출 시 `OnHpChanged` 이벤트 추가 발행 (TECH-02)

</code_context>

<deferred>
## Deferred Ideas

- **ENM-06 (PURIFIED/DESTROYED 텍스트):** 사용자 판단으로 이 Phase에서 스킵. 필요 시 v2 폴리시 패스에서 추가.
- **Ghost bar (트레일 효과):** REQUIREMENTS v2 POL-04 — HP 감소 지연 트레일은 이 Phase 범위 밖.
- **보스 위 월드 스페이스 바 억제:** Phase 3 범위 (BOSS-04).

</deferred>

---

*Phase: 02-enemy-world-space-ui*
*Context gathered: 2026-04-06*
