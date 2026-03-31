# 프로젝트 리서치 요약

**프로젝트:** Bleeding Spring (혈연) — UI 시스템 마일스톤
**도메인:** 2D 하드코어 액션 로그라이크를 위한 Unity 6 UGUI 통합
**조사일:** 2026-03-27
**신뢰도:** 높음

## 핵심 요약

Bleeding Spring은 Unity 6 (URP 2D) 기반으로 구축된 2D 하드코어 액션 로그라이크로, 성숙하고 SOLID 지향적인 코드베이스를 갖추고 있습니다 — 플레이어 이동, 이중 리소스 stats, 스킬 시스템, 적 AI, 게임 상태 관리가 모두 구현되어 있습니다. 현재 마일스톤은 단 하나의 목표를 가집니다: 게임을 읽을 수 있게 만들기. 플레이어가 해야 하는 모든 게임플레이 결정 (HP 보존, 오염 관리, Sweet Spot 타겟팅, 스킬 쿨다운 타이밍)은 현재 UI가 없어서 보이지 않습니다. UI 시스템 구축은 폴리시 작업이 아닙니다 — 게임이 플레이 가능하기 위한 전제 조건입니다.

권장 접근법은 UGUI 전용, 이벤트 기반이며 코드베이스에 이미 있는 것을 미러링하는 Presenter-View 패턴을 중심으로 구조화되어 있습니다. 필요한 모든 패키지가 이미 설치되어 있습니다 (UGUI 2.0.0, URP 17.3.0). 추가 의존성이 없습니다. 세 캔버스 아키텍처 — 플레이어 HUD와 보스 바를 위한 Screen Space Overlay, 적 바를 위한 적별 월드 스페이스 — 가 올바른 분리입니다. 월드 스페이스가 개별 적을 따르는 바에 대한 유일하게 실행 가능한 솔루션이며 Unity 6 UI Toolkit에는 대안이 없기 때문입니다. Sweet Spot 하이라이트 구간이 있는 적 오염도 바가 가장 복잡하고 가장 가치 있는 항목입니다: 장르 선례가 없고, 플레이어에게 자가 교습이어야 하며, 첫 번째 프리팹이 구축되기 전에 결정해야 하는 특정 구조적 제약이 있습니다 (오버레이는 fill의 자식이 아닌 형제이어야 함).

주요 위험은 Phase 1에서 잘못 만들면 되돌리기 비용이 많이 드는 모든 아키텍처 결정입니다: 적별 캔버스 컴포넌트 (규모에서 드로우콜 폭발), Bind/Unbind 패턴 없는 이벤트 구독 (적 사망/정화 시 메모리 누수와 MissingReferenceException), 잘못된 RectTransform에 부모화된 Sweet Spot 오버레이 (오염도 fill 변경에 따라 구간 이동). 세 가지 모두 단 하나의 바 스크립트를 작성하기 전에 올바른 패턴을 확립하면 완전히 예방할 수 있습니다.

---

## 주요 발견

### 권장 스택

스택 결정이 고정됩니다: UGUI 2.0.0이 설치되어 있으며 유일하게 실행 가능한 옵션입니다. UI Toolkit은 Unity 6에서 월드 스페이스로 렌더링할 수 없어 적별 바에 부적합합니다. UGUI의 `Image.fillAmount`는 `PlayerWaterStats`와 `EnemyStats`의 기존 C# `Action` 이벤트에 간단한 MonoBehaviour 코드로 직접 바인딩됩니다 — 중간 버스도, 프레임워크 오버헤드도 없습니다. TextMeshPro는 Unity 6과 번들되어 있으며 (일회성 필수 리소스 가져오기 필요) 모든 텍스트 요소에 사용해야 합니다; 레거시 `Text` 컴포넌트는 폐기되었습니다.

**핵심 기술:**
- **UGUI 2.0.0** (이미 설치됨): 모든 런타임 UI — 바를 위한 `Image.fillAmount`, 적별 월드 스페이스 캔버스, 플레이어 HUD와 보스 바를 위한 Screen Space Overlay
- **Unity URP 17.3.0** (이미 설치됨): 렌더 파이프라인; 월드 스페이스 캔버스는 `Canvas.worldCamera`가 씬 카메라로 설정된 URP 2D에서 올바르게 그려짐
- **TextMeshPro** (Unity 6과 번들됨): 모든 텍스트 요소 (티어 레이블, 보스 이름, 쿨다운 숫자 사용 시); 사용 전 한 번 필수 리소스 가져오기
- **Unity 코루틴** (내장): 스킬 쿨다운 fill 애니메이션 구동; `SkillBase` 쿨다운이 내부적으로 코루틴으로 구동됨

**세 캔버스 레이아웃:**

| 캔버스 | 모드 | 목적 |
|--------|------|---------|
| `HUDCanvas` | Screen Space - Overlay, Sort Order 10 | 플레이어 HP 바, 오염도 바, 워터 티어 표시기, 스킬 쿨다운 슬롯 |
| `EnemyCanvas` (프리팹별) | World Space | 적별 HP 바 + Sweet Spot 구간이 있는 오염도 바; 적 GameObject의 자식 |
| `BossHUDCanvas` (HUDCanvas의 패널) | Screen Space - Overlay, Sort Order 20 | 화면 하단 중앙에 앵커된 큰 보스 HP 바; 보스 만남까지 비활성화 |

전체 이벤트 바인딩 참조, 컴포넌트 패턴, 버전 호환성 테이블은 `.planning/research/STACK.md` 참조.

---

### 기대 기능

참고 게임 분석 (Dead Cells, Hades, Hollow Knight, Enter the Gungeon)이 명확한 장르 기대를 확립합니다. 적 오염도 바의 Sweet Spot 하이라이트 범위는 진정으로 새로운 UI 패턴입니다 — 어떤 참고 게임도 stat 바에서 멈춰야 할 목표 구간을 보여주지 않습니다. 이것은 UI가 완전히 자가 교습이어야 함을 의미합니다; 시각적 폴리시보다 구현의 명확성이 더 중요합니다.

**필수 (기본 요구사항) — P1:**
- 플레이어 HP 바 (맑은 물) — 1차 사망 축; 없으면 게임을 읽을 수 없음
- 100% 근접 위험 색상의 플레이어 오염도 바 — 2차 사망 축; 다른 시각적 처리 필요, 그냥 또 다른 바가 아님
- 워터 티어 표시기 (0–3) — 티어가 스킬 동작을 직접 변경; 플레이어는 항상 현재 상태 알아야 함
- 스킬 쿨다운 표시 (3 슬롯) — Dead Cells가 장르 표준으로 확립; 이것 없이는 전략적 스킬 사용 불가
- 적 월드 스페이스 HP 바 (첫 히트 시 나타남) — 히트 확인; Dead Cells 사용; 장르 기대
- Sweet Spot 하이라이트 밴드가 있는 적 오염도 바 — 이것이 핵심 메카닉; 없으면 정화/파괴 루프가 플레이어에게 보이지 않음
- 보스 Screen Space HP 바 — 네 가지 참고 게임 모두 전체 너비 화면 하단 보스 바를 가짐; 없으면 미완성으로 느껴짐
- 위기 HP 위험 신호 (화면 가장자리 비네트 또는 펄스) — Dead Cells/Hades 표준; 빠른 전투 중 위험 상태 읽기 지원

**있어야 함 (경쟁력) — P2 (시간이 허락하면 같은 마일스톤에):**
- Sweet Spot 정화 결과 피드백 (적 사망 시 정화/파괴 팝업) — 킬 직후 전략적 루프 즉시 강화
- 오염도 바 최대 fill 알람 (100%에서 깜빡임 또는 색상 변화) — 이중 사망 조건 명확화
- 스킬 아이콘 아트 (Aseprite 에셋이 플레이스홀더 사각형 교체) — 아트 파이프라인 검증

**연기 (Phase 2+):**
- 애니메이션 HP 고스트/트레일 — 순수 폴리시; 이 단계에서 가독성 이점 없이 복잡도 추가
- 플로팅 데미지 숫자 — 상당한 구현 작업; 월드 스페이스 바와 시각적 충돌
- 정화/파괴 카운터 표시 — Phase 2 스토리 분기 시스템이 있어야 의미 있음
- 미니맵 / 방 지도 — 아직 존재하지 않는 던전 구조 필요

전체 경쟁사 기능 매트릭스와 의존성 그래프는 `.planning/research/FEATURES.md` 참조.

---

### 아키텍처 접근법

아키텍처는 가벼운 MVP 패턴을 따릅니다: Stats 클래스가 이벤트를 소유하고, Presenter MonoBehaviour가 해당 이벤트를 구독하고 View 스크립트의 타입 setter를 호출하며, View 스크립트가 UGUI 컴포넌트 참조를 소유하고 내부적으로 시각 효과를 처리합니다. 프레임워크도, ScriptableObject 이벤트 버스도 없습니다 — 기존 코드베이스가 이미 직접 C# 이벤트를 일관되게 사용하며 새 UI 레이어가 해당 패턴과 일치해야 합니다. 기존 게임 스크립트에 대한 유일한 중요한 추가사항은 `SkillBase`의 `CooldownRatio` 프로퍼티 (또는 `OnCooldownProgress` 이벤트)입니다. `IsOnCooldown`(bool)은 방사형 오버레이 채우기에 충분하지 않기 때문입니다.

**주요 컴포넌트:**

1. **`PlayerHUDPresenter`** — `PlayerWaterStats` 이벤트 구독; `WaterBarView`, `CorruptionBarView`, `WaterTierIndicator` 구동; Stats에 대한 인스펙터 연결 참조
2. **`EnemyUIPresenter`** — `EnemyStats.OnDamaged` / `OnDeath` 구독; `EnemyHpBarView`, `EnemyCorruptionBarView`, `SweetSpotOverlay` 구동; 적 루트에 위치, `Awake`에서 `GetComponent<EnemyStats>()` 사용
3. **`SweetSpotOverlay`** — 상태 없는 뷰; `(normalizedMin, normalizedMax)` 주어지면 정화 범위를 커버하도록 `RectTransform.anchorMin/anchorMax` 설정; 바 배경의 자식이어야 하며 fill 이미지의 자식이 아님
4. **`SkillCooldownView`** — 스킬 슬롯별 방사형 fill 오버레이; `Update`에서 `ISkill.IsOnCooldown` 폴링하여 쿨다운 시작 감지, 경과 시간 추적으로 fill 구동; `SkillBase` 확장 후 이벤트 기반으로 업그레이드
5. **`BossBarPresenter`** — 보스 만남에서 `BindToBoss(EnemyStats)`를 통해 활성화; 스크린 스페이스 바 구동; 기본적으로 비활성화
6. **`GameOverUI` / `PauseUI`** — `GameStateManager.OnGameStateChange` 구독; 패널 표시/숨기기; 기존 `InputHandler` 구독 패턴 미러링

**빌드 순서:** View 스크립트 먼저 (상태 없음, 플레이스홀더 값으로 테스트 가능) → Presenter (뷰가 확인된 후 연결) → 보스 바 마지막 (아직 구축되지 않은 씬 트리거 필요).

전체 프리팹 계층, 데이터 흐름 다이어그램, 컴포넌트 경계, 안티 패턴 분석은 `.planning/research/ARCHITECTURE.md` 참조.

---

### 치명적 함정

1. **Destroy/정화 경로를 통한 이벤트 구독 누수** — `EnemyStats.Die()`가 즉시 `Destroy(gameObject)`를 호출; `EnemyStats.Purify()`가 `enabled = false`를 호출 (`Destroy` 아님), 따라서 정화 경로에서 `OnDestroy()`가 절대 발생하지 않음. `Start()`에서 구독하고 `OnDestroy()`에서만 구독 해제하면 정화 시 참조 보유와 빠른 사망 시 `MissingReferenceException`이 보장됨. **예방:** 모든 바 스크립트에 `Bind(EnemyStats)` / `Unbind()` API 구현; `OnDeath` 핸들러는 항상 시각적 전환 전에 첫 번째 행동으로 `Unbind()`를 호출.

2. **적별 캔버스 컴포넌트가 드로우콜을 폭발시킴** — 적 프리팹당 하나의 `Canvas` 컴포넌트가 적당 하나의 드로우콜 배치를 생성. Unity는 캔버스 경계 전반에 배칭할 수 없음. 20개 이상의 적에서 성능이 조용히 저하되며 모든 적 프리팹을 리팩토링하지 않고는 고칠 수 없음. **예방:** 첫 번째 바 프리팹이 구축되기 전에 캔버스 아키텍처를 결정. 알려진 적 수 한계가 있는 Phase 1 프로토타입에는 적별 캔버스가 허용됨; 10개 이상의 동시 적이 예상된다면 공유 월드 스페이스 캔버스와 바 follower가 더 잘 확장됨.

3. **fill 이미지 대신 바 배경에 부모화된 Sweet Spot 오버레이** — `Image.fillAmount`가 RectTransform을 크기 조정하지 않음; fill 이미지의 rect는 항상 전체 너비. fill 이미지의 자식 오버레이는 100% 오염도에서 올바른 비율을 커버하는 것처럼 보이지만 fill에 앵커되어 오염도가 떨어지면 구간이 시각적으로 이동함. **예방:** `SweetSpotOverlay`를 바 배경 RectTransform의 자식으로 부모화하고, `anchorMin.x = effectiveMin`, `anchorMax.x = effectiveMax`, `offsetMin = offsetMax = Vector2.zero` 설정. 프리팹 레이아웃 결정; 적 프리팹에 배포된 후 소급 수정 비용이 많이 듦.

4. **Update() lerp로 캔버스 더티 마킹** — 매 프레임 `Image.fillAmount` 구동 (변경 없어도)이 전체 캔버스 재빌드를 트리거. 보이는 모든 적 바에서 부드러운 lerp 애니메이션이 실행되면 `Canvas.BuildBatch`가 매 프레임 급등함. **예방:** 이벤트 기반 업데이트 사용 (이벤트 콜백 내에서만 `fillAmount` 쓰기); 부드러운 애니메이션에는 지속적인 `Update()` 루프가 아닌 자가 종료 코루틴 사용.

5. **보스 바의 Screen Space - Overlay가 URP 포스트 프로세싱을 우회** — Overlay 캔버스가 URP 카메라 스택 외부에서 렌더링; Volume을 통해 적용된 모든 블룸, 색상 그레이딩, 비네트가 보스 바에 영향을 주지 않음. **예방:** 보스 바 캔버스에 Screen Space - Camera 모드를 사용하고 `Awake()`에서 `Canvas.worldCamera = Camera.main` 할당. 플레이어 HUD는 Overlay로 허용됨 (HUD 자체에 포스트 프로세싱이 적용되지 않으므로).

전체 함정 카탈로그, 성능 함정, UX 함정, "완성처럼 보이지만 아닌 것" 검증 체크리스트는 `.planning/research/PITFALLS.md` 참조.

---

## 로드맵에 대한 시사점

이 마일스톤은 단일 단계를 다룹니다: UI 시스템. FEATURES.md의 의존성 분석과 ARCHITECTURE.md의 빌드 순서를 기반으로 단계는 명확한 크리티컬 패스를 가진 세 개의 순차적 하위 단계로 자연스럽게 분해됩니다.

### Phase 1-A: 기반 및 플레이어 HUD

**근거:** View 컴포넌트는 게임 스크립트 의존성이 없으며 Presenter가 존재하기 전에 플레이스홀더 값으로 빌드하고 테스트할 수 있습니다. 플레이어 HUD는 가장 낮은 복잡도의 연결입니다 (이벤트가 이미 존재하고 모든 시그니처가 확인됨) — 더 복잡한 적 바에 적용하기 전에 Presenter-View 패턴을 검증합니다.

**결과물:** 작동하는 플레이어 HUD — HP 바, 위험 색상이 있는 오염도 바, 워터 티어 표시기, 스킬 쿨다운 슬롯. EventSystem과 CanvasScaler 구성됨. TMP 필수 리소스 가져와짐.

**다루는 기능:** 플레이어 HP 바, 플레이어 오염도 바, 워터 티어 표시기, 스킬 쿨다운 표시 (가장 낮은 위험의 P1 기능).

**피하는 함정:** `[SerializeField]` 참조 연결 패턴 확립 (`FindObjectOfType` 없음); 레이아웃 작업 전에 1920x1080에서 `CanvasScaler` Scale With Screen Size 확인.

**필요한 코드베이스 변경:** `SkillBase`와 `ISkill` 인터페이스에 `CooldownRatio` 프로퍼티 (또는 `OnCooldownProgress` 이벤트) 추가.

---

### Phase 1-B: 적 월드 스페이스 UI 및 Sweet Spot 시각화

**근거:** 이것이 마일스톤에서 가장 가치 있고 가장 위험한 결과물입니다. Sweet Spot 시각화는 핵심 메카닉의 유일한 플레이어 소통 채널입니다. 복사할 장르 선례가 없고, 특정 프리팹 구조가 필요하며 (오버레이가 fill의 형제, 자식 아님), 여기서 내린 아키텍처 결정 (적별 캔버스 vs 공유 캔버스, Bind/Unbind 패턴)이 적 프리팹에 배포된 후 되돌리기 비용이 많이 듭니다.

**결과물:** 모든 적 프리팹에 작동하는 월드 스페이스 바 — HP 바, 올바르게 위치된 Sweet Spot 구간이 있는 오염도 바, 히트/사망/정화 시 표시/숨기기. Bind/Unbind 패턴 확립 및 테스트됨.

**다루는 기능:** 적 월드 스페이스 HP 바, Sweet Spot 밴드가 있는 적 오염도 바 (가장 높은 우선순위 차별화 요소).

**피하는 함정:** Bind/Unbind 패턴이 Destroy와 정화 경로에서 이벤트 누수 방지; Sweet Spot 오버레이 부모 계층이 프리팹 배포 전에 올바르게 확립; 규모가 강요하기 전에 캔버스 아키텍처 결정.

**필요한 검증:** `EnemyStats`가 `basePurificationMin`, `basePurificationMax`, `bonusPurificationMargin`을 접근 가능한 프로퍼티로 노출하는지 확인 (내부 필드만이 아님). `OnDeath`가 Die()와 Purify() 경로 모두에서 발생하는지 확인 (PITFALLS.md에서 EnemyStats.cs 154번 줄로 확인됨 — 예, 발생함).

---

### Phase 1-C: 보스 바 및 폴리시

**근거:** 보스 바는 아직 존재하지 않는 보스 감지/방 트리거가 필요하기 때문에 마지막입니다. 스텁 `BindToBoss(EnemyStats)` 메서드로 Phase 1-C를 전체 방 시스템 없이 독립적으로 빌드하고 테스트할 수 있습니다. 이 단계에는 시간이 허락하면 P2 폴리시 항목도 포함됩니다.

**결과물:** 작동하는 Screen Space 보스 HP 바 (Overlay 아닌 Screen Space - Camera 모드), `BossBarPresenter.BindToBoss()`를 통해 활성화됨. 선택적: Sweet Spot 정화 결과 피드백 팝업, 오염도 바 최대 fill 알람.

**다루는 기능:** 보스 Screen Space HP 바 (P1); Sweet Spot 정화 결과 피드백, 오염도 바 최대 fill 알람 (P2).

**피하는 함정:** URP 카메라 스택에 참여하고 포스트 프로세싱을 올바르게 받기 위해 Screen Space - Camera 모드 (Overlay 아님)의 보스 캔버스.

---

### 단계 순서 근거

- **Presenter 전에 View:** 상태 없는 View 컴포넌트는 게임 스크립트 연결이 추가되기 전에 인스펙터의 플레이스홀더 값으로 검증 가능. 이벤트 구독이 추가될 때 두 시스템을 동시에 디버깅하는 것을 방지.
- **적 바 전에 플레이어 HUD:** Presenter-View 패턴은 플레이어 stats 경로에서 검증하기 더 간단함 (하나의 stats 객체, 모든 이벤트가 이미 존재하고 확인됨) — 구조적으로 더 복잡한 적 바 (월드 스페이스, Bind/Unbind 수명 주기, 풀링 우려)에 적용하기 전.
- **보스 바 전에 적 바:** 보스 바는 확립된 Presenter 패턴을 재사용할 수 있는 적 바 패턴의 단순화된 버전; 적 바를 먼저 구축하면 보스 바 연결이 해결된 문제가 됨.
- **아키텍처 결정 앞당김:** 모든 되돌릴 수 없는 레이아웃 결정 (Sweet Spot 오버레이 부모, 캔버스 아키텍처)이 어떤 프리팹도 여러 적 타입에 배포되기 전인 Phase 1-B에서 다루어짐.

---

### 리서치 플래그

계획 중 더 깊은 리서치가 필요한 단계:

- **Phase 1-B (적 월드 스페이스 UI):** Sweet Spot 오버레이는 참조 구현이 없는 새로운 UI 패턴입니다. RectTransform 앵커 계산과 Bind/Unbind 수명 주기는 구현 전에 신중한 사양이 필요합니다. Sweet Spot 프리팹 레이아웃과 정화 경로의 이벤트 시퀀싱에 특별히 집중된 짧은 리서치 패스를 권장합니다.
- **Phase 1-B (캔버스 아키텍처):** 적별 캔버스 vs 공유 캔버스 결정이 예상 적 수에 따라 다릅니다. 게임 디자인이 15개 이상의 동시 적을 목표로 한다면 적별 캔버스에 커밋하기 전에 공유 캔버스 + follower 접근법을 리서치하세요.

표준 패턴을 가진 단계 (리서치 단계 건너뜀):

- **Phase 1-A (플레이어 HUD):** 모든 이벤트 시그니처가 소스에서 확인됨, 모든 패턴 (filled Image, Presenter-View)이 잘 확립된 Unity 패턴. 새로운 결정 불필요.
- **Phase 1-C (보스 바):** 스크린 스페이스 캔버스에 적용된 적 바와 동일한 데이터 바인딩 패턴. 캔버스 모드 선택 (Screen Space - Camera)이 문서화됨. 표준 구현.

---

## 신뢰도 평가

| 영역 | 신뢰도 | 참고 |
|------|------------|-------|
| 스택 | 높음 | 모든 패키지 버전이 `manifest.json`과 `packages-lock.json`에서 직접 확인됨; UI Toolkit 월드 스페이스 제한은 문서화된 Unity 6 엔진 제약 사항 |
| 기능 | 높음 | 참고 게임들은 성숙하고 안정적인 타이틀; Dead Cells, Hades, Hollow Knight, Enter the Gungeon의 UI 패턴은 훈련 데이터를 통해 잘 문서화됨; Sweet Spot 참신성 발견은 도출된 결론, 조회가 아님 |
| 아키텍처 | 높음 | `EnemyStats.cs`, `PlayerWaterStats.cs`, `SkillBase.cs`, `GameStateManager.cs`의 직접 소스 코드 감사에 기반; UGUI RectTransform 앵커 시스템은 Unity 5–6에서 안정적 |
| 함정 | 높음 | 치명적 함정 (Destroy/정화 이벤트 누수, 캔버스 드로우콜 배칭, fillAmount 더티 마킹)이 `EnemyStats.cs`의 직접 코드 감사에서 도출되고 Unity UGUI 문서 동작에 대해 확인됨; 추론 아님 |

**전체 신뢰도:** 높음

### 처리할 갭

- **`SkillBase.cooldownDuration` 접근:** 필드가 `protected`이고 `SkillCooldownView`는 방사형 fill 구동을 위해 이것이 필요. `SkillBase`에 `public` 프로퍼티로 노출하거나 `ISkill`에 `public float CooldownDuration { get; }` 추가. Phase 1-A 스킬 쿨다운 구현 전에 필요한 인터페이스 변경. `SkillCooldownView` 작성 전에 결정하고 구현.

- **`EnemyStats` 정화 결과 이벤트:** Sweet Spot 정화 결과 피드백 (정화/파괴 팝업)은 `EnemyStats`가 사망 시 `PurificationResult` 값을 전달하는 이벤트를 발생시킬 것을 요구. 이 이벤트가 존재하는지 확인; 없으면 Phase 1-C 폴리시 작업 전에 추가. 핵심 Phase 1-B 바는 이것에 의존하지 않습니다 — P2 항목.

- **보스 만남 트리거 메커니즘:** `BossBarPresenter.BindToBoss(EnemyStats)`는 외부 호출자 (보스 방 트리거 또는 만남 매니저)가 필요. 현재 코드베이스에 존재하지 않음. Phase 1-C의 경우 직접 인스펙터 할당이나 테스트 스크립트로 충분; 실제 트리거는 Phase 2 범위. Phase 1-C를 이것에 블로킹하지 마세요 — 스텁 처리.

- **`bonusPurificationMargin` 실시간 업데이트:** `SweetSpotOverlay.SetRange()`는 `EnemyStats.WidenPurificationRange()`가 호출될 때 런타임에 호출 가능해야 함 (미래 아이템 시스템). Phase 1은 `Start()`에서 한 번만 호출하면 됨; 하지만 API는 재호출을 지원하도록 설계되어야 함. `SweetSpotOverlay`에 `SetRange(float, float)`를 공개 메서드로 노출하는 것으로 충분 (ARCHITECTURE.md가 이미 명세함) — 아이템 시스템이 구축될 때 호출 위치에 재호출이 추가됨.

---

## 출처

### 1차 (높은 신뢰도 — 직접 코드 감사)
- `Assets/Player/PlayerWaterStats.cs` — 이벤트 시그니처 확인: `OnWaterChanged(float, float)`, `OnCorruptionChanged(float, float)`, `OnWaterTierChanged(int)`
- `Assets/Enemy/EnemyStats.cs` — `OnDamaged`, `OnDeath` 이벤트; `basePurificationMin`, `basePurificationMax`, `bonusPurificationMargin`, `CorruptionRatio` 프로퍼티; 정화 경로가 컴포넌트 비활성화 (`enabled = false`) 확인
- `Assets/Player/SkillBase.cs` — `IsOnCooldown` bool 프로퍼티 확인; 쿨다운 진행 이벤트 부재 확인
- `Assets/Player/ISkill.cs` — 인터페이스 계약 확인
- `Packages/manifest.json` + `packages-lock.json` — UGUI 2.0.0, URP 17.3.0, Input System 1.19.0 버전 및 내장 소스 확인
- `.planning/codebase/CONCERNS.md` — 알려진 버그 확인: `EnemyStats.TakeDamage`가 이미 사망했을 때도 `OnDamaged` 발생

### 1차 (높은 신뢰도 — 안정적인 Unity API)
- Unity UGUI 2.0.0: `Image.fillAmount`, `Image.fillMethod`, RectTransform 앵커 시스템, 월드 스페이스 캔버스, CanvasScaler Scale With Screen Size — Unity 5–6에서 변경되지 않은 동작
- Unity URP 17.3.0: Screen Space - Camera vs Overlay 캔버스 모드, 카메라 스택 참여, URP 2D에서 월드 스페이스 캔버스 깊이 소팅

### 2차 (높은 신뢰도 — 훈련 데이터, 안정적인 타이틀)
- Dead Cells (Motion Twin, 2018): 월드 스페이스 적 바, 스킬 쿨다운 아이콘 오버레이, 화면 가장자리 위험 펄스
- Hades (Supergiant Games, 2020): 페이즈 틱 마크가 있는 화면 하단 보스 바, 낮은 HP에서 화면 비네트
- Hollow Knight (Team Cherry, 2017): 이산 마스크 HP 아이콘, 소울 미터 원형 게이지, 적 바 없음
- Enter the Gungeon (Dodge Roll, 2016): 하트 HP 아이콘, 화면 상단 보스 바, 페이즈 마커

---
*리서치 완료: 2026-03-27*
*로드맵 준비: 예*
