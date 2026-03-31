# 함정 리서치

**도메인:** Unity 6 UGUI — 2D 로그라이크 UI 시스템 (월드 스페이스 적 바, 이벤트 기반 HUD, Sweet Spot 시각화, 바 풀링, 보스 전환)
**조사일:** 2026-03-27
**신뢰도:** 높음 (Unity UGUI 동작은 잘 확립됨; 코드베이스별 함정은 EnemyStats.cs, EnemyAI.cs, CONCERNS.md 직접 코드 감사에서 도출)

---

## 치명적 함정

### 함정 1: UI 스크립트가 Start()에서 구독하지만 적이 Destroy(gameObject)로 파괴됨 — 누수 경로 보장

**무슨 일이 일어나는가:**
`EnemyStats.Die()`는 즉시 `Destroy(gameObject)`를 호출합니다 (EnemyStats.cs 171번 줄). 월드 스페이스 체력 바 스크립트가 `Start()` 또는 `OnEnable()`에서 `EnemyStats.OnDamaged`와 `EnemyStats.OnDeath`를 구독하면, 구독은 다음 GC 사이클까지 파괴된 객체의 델리게이트에 남아있습니다. 바가 적 GameObject의 자식이라면 `OnDestroy()`가 올바르게 발생합니다. 하지만 바가 별도로 인스턴스화된 경우 (예: 매니저를 통해 부착된 풀링 바), 적이 파괴되기 전에 바의 `OnDestroy()`가 발생하지 않을 수 있어 델리게이트 목록에 바의 콜백이 남습니다. `TakeDamage`가 `Destroy`와 같은 프레임에 발생하면 `OnDamaged`와 `OnDeath` 모두 발생하고 (CONCERNS.md "알려진 버그: EnemyStats.TakeDamage가 이미 사망했을 때도 OnDamaged를 발생시킴"에서 확인됨) — UI 바 콜백이 파괴된 RectTransform에서 `SetFillAmount`를 호출하려 해 `MissingReferenceException`을 던집니다.

**왜 일어나는가:**
개발자들이 유연성을 위해 바를 형제/별도 프리팹으로 만들고 (적 위에 떠 있을 수 있도록), 구독 해제 순서가 엄격하게 제어되지 않은 채 부모의 이벤트를 구독합니다. Unity는 루트에서 `Destroy(gameObject)` 호출 시 부모에 대해 미정의 순서로 자식 오브젝트를 파괴합니다.

**회피 방법:**
- 바 스크립트는 항상 `OnEnable()`에서 구독하고 `OnDisable()`에서 구독 해제합니다 (`Start()`/`OnDestroy()` 아님). 이것이 풀링 객체를 올바르게 처리합니다.
- 대안: `Start()`에서만 구독하고, `OnDestroy()`에서만 구독 해제하되, 모든 콜백 상단에 null 체크 가드 추가: `if (stats == null || stats.gameObject == null) return;`
- 풀링 바에 가장 안전한 패턴: 바 스크립트에 `void Bind(EnemyStats target)` / `void Unbind()` API 노출; 풀 매니저가 바를 풀에 반환하기 전에 `Unbind()`를 호출.

**경고 징후:**
- 적 사망 시 콘솔에 `MissingReferenceException: The object of type 'RectTransform'`.
- 적이 빠르게 죽은 후 UI 콜백 내 `NullReferenceException`.
- 메모리 프로파일러가 적 웨이브 후 델리게이트 목록이 증가함을 보임.

**처리 단계:** Phase 1 (월드 스페이스 적 UI) — 첫 번째 바 스크립트가 이벤트에 연결되기 전에 Bind/Unbind 패턴 확립.

---

### 함정 2: 적별 월드 스페이스 캔버스 — 드로우콜 폭발

**무슨 일이 일어나는가:**
가장 일반적인 구현은 각 적 프리팹의 자식으로 `Canvas` 컴포넌트 (렌더 모드: 월드 스페이스)를 생성합니다. 시각적으로는 쉽지만 캔버스당 하나의 드로우콜 배치를 생성합니다. 화면에 20개의 적이 있으면 20개 이상의 별도 캔버스 배치가 됩니다. Unity의 캔버스 배칭은 단일 캔버스 내에서만 작동합니다; 다른 캔버스의 요소들은 절대 함께 배칭될 수 없습니다. 많은 적이 있는 로그라이크에서 이것은 조용히 성능을 저하시킵니다.

**왜 일어나는가:**
Unity 문서와 대부분의 튜토리얼이 "적 프리팹에 캔버스 추가" 접근법을 보여주는 것이 가장 간단한 설정이기 때문입니다. 배칭 결과는 캔버스 최적화 가이드를 읽기 전까지 언급되지 않습니다.

**회피 방법:**
씬에 **단일 월드 스페이스 캔버스**를 사용합니다 (고정 Z에 배치, 예: Z = -1로 스프라이트 위, 카메라 근거리 평면 아래 렌더링). 모든 적 바를 이 단일 캔버스의 자식으로 인스턴스화/풀링합니다. 각 바는 `LateUpdate()` 또는 `rectTransform.position = Camera.main.WorldToScreenPoint(enemy.position) + offset`를 설정하는 전용 `EnemyBarFollower` 컴포넌트를 통해 적을 따릅니다. 이것이 모든 적 바를 하나의 배치로 유지합니다.

**경고 징후:**
- Frame Debugger가 모든 적마다 별도의 "Canvas.RenderOverlays" 또는 "Draw Dynamic" 호출을 보임.
- 프로파일러가 `Canvas.SendWillRenderCanvases`가 적 수에 선형적으로 비례함을 보임.

**처리 단계:** Phase 1 — 첫 번째 바 프리팹이 구축되기 전에 아키텍처 결정이 이루어져야 합니다. 소급 수정은 비용이 많이 듭니다.

---

### 함정 3: 스크립트 기반 fill 업데이트로 매 프레임 캔버스 더티 마킹

**무슨 일이 일어나는가:**
매 `Update()` 프레임마다 UGUI Image에서 `image.fillAmount = value`를 설정하면 — 값이 변경되지 않았더라도 — 캔버스를 더티로 표시하고 전체 캔버스 재빌드 (메시 재생성, 배칭)를 트리거합니다. `OnDamaged` 이벤트에서 업데이트되는 월드 스페이스 적 바의 경우 이것은 히트 시에만 발생하므로 괜찮습니다. 하지만 개발자가 `Update()`에서 smooth lerp 애니메이션을 추가하면 (예: `currentFill = Mathf.Lerp(currentFill, targetFill, t * Time.deltaTime)`), 보이는 모든 적 바에 대해 매 프레임 캔버스가 재빌드됩니다.

**왜 일어나는가:**
Smooth 체력 바 애니메이션이 스냅보다 더 좋아 보입니다. lerp 접근법은 튜토리얼에서 보편적으로 시연됩니다. 캔버스 재빌드 비용은 많은 바가 동시에 실행될 때까지 보이지 않습니다.

**회피 방법:**
- 값이 실제로 변경될 때만 `fillAmount` 업데이트: `lastFill`을 캐시하고 `Mathf.Approximately(newFill, lastFill)`이면 할당 건너뜀.
- Smooth 애니메이션의 경우: `Update()`에서 폴링하는 대신 애니메이션 기간 동안만 실행되고 목표에 도달하면 멈추는 코루틴이나 `DOTween`을 사용.
- `Update()` lerp를 사용하는 경우: `if (Mathf.Abs(currentFill - targetFill) < 0.001f) { currentFill = targetFill; enabled = false; }`를 추가하여 안정화되면 컴포넌트를 자가 비활성화.

**경고 징후:**
- 프로파일러가 매 프레임, 타격 프레임뿐만 아니라 적 수에 비례하여 `Canvas.BuildBatch`가 급등함을 보임.
- 많은 적이 있는 유휴 씬에서도 `WillRenderCanvases`가 지속적으로 높음.

**처리 단계:** Phase 1 — 처음부터 이벤트 기반 (폴링 아님) 업데이트 패턴 확립.

---

### 함정 4: Sweet Spot 구간 RectTransform 앵커가 잘못된 공간에서 계산됨

**무슨 일이 일어나는가:**
Sweet Spot 구간 (예: `EnemyStats`의 `basePurificationMin`과 `basePurificationMax`로 정의된 오염도 바의 30%–70%)은 바 위의 색상 오버레이로 렌더링되어야 합니다. 일반적인 접근법: 바 위에 자식 `Image` RectTransform을 배치하고 `anchorMin.x`와 `anchorMax.x`를 Sweet Spot 백분율로 설정. 이것은 바의 RectTransform 너비가 앵커로 구동될 때만 작동합니다 (고정 픽셀 너비 아님). 바가 오염도 fill에 `Image.fillAmount`를 사용하면 기본 RectTransform은 항상 전체 너비입니다 — Sweet Spot 오버레이는 채워진 부분이 아닌 전체 바 너비에 상대적으로 앵커되어야 합니다. "fill 공간"을 "바 공간"으로 혼동하면 오염도가 변경됨에 따라 구간이 이동합니다.

**왜 일어나는가:**
오염도 바는 시각적으로 줄어들지만 (`fillAmount`를 통해) RectTransform은 줄어들지 않습니다. 개발자들이 Sweet Spot 오버레이를 바 배경의 형제가 아닌 fill 이미지의 자식으로 배치하여 오버레이가 fill과 함께 크기가 조정됩니다.

**회피 방법:**
- Sweet Spot 오버레이를 **바 배경**의 자식으로 만듭니다 (fill 이미지 아님).
- 오버레이의 RectTransform에 `anchorMin = new Vector2(sweetSpotMin, 0)`과 `anchorMax = new Vector2(sweetSpotMax, 1)` 설정 (정규화 값은 `EnemyStats.basePurificationMin`과 `basePurificationMax`에서, `bonusPurificationMargin`으로 조정).
- `offsetMin = offsetMax = Vector2.zero`로 픽셀 완벽하게 만들기.
- fill 바가 줄어들어도 오버레이는 배경에 앵커되어 있으므로 고정 상태를 유지합니다.

**경고 징후:**
- Sweet Spot 구간이 고정되지 않고 오염도 fill의 가장자리를 시각적으로 추적.
- `WidenPurificationRange()`가 전투 중 UI 업데이트 없이 호출될 때 구간 위치가 이동.
- 기본 오염도 (100%)에서는 구간이 올바르지만 낮은 오염도 값에서 떠내려감.

**처리 단계:** Phase 1 — 바 프리팹에 대한 구조적 레이아웃 결정. 프리팹이 구축되고 적 프리팹에 배포된 후 쉽게 수정할 수 없음.

---

### 함정 5: 정화 시 EnemyStats가 파괴되지 않고 비활성화될 때 이벤트 구독 해제 실패

**무슨 일이 일어나는가:**
`EnemyStats.Purify()`는 객체를 파괴하는 대신 `enabled = false`를 호출합니다 (EnemyStats.cs 155번 줄). 이는 `EnemyStats`나 그 형제에서 `OnDestroy()`가 호출되지 **않음을** 의미합니다. 적 UI 바가 `EnemyStats.OnDamaged`/`OnDeath`를 구독하고 `OnDestroy()`에서 구독 해제한다면 구독은 무기한 유지됩니다. `PurifiedNPC` 컴포넌트가 추가되고 (158번 줄) 활성화되지만, 비활성화된 `EnemyStats`의 이벤트에 여전히 구독된 바 스크립트는 이제 비활성화되어 다시는 발생하지 않을 컴포넌트의 이벤트에 대한 라이브 참조를 보유합니다. 이것은 조용한 참조 보유입니다 (크래시 아님), 하지만 바의 메모리 할당을 유지하고 델리게이트에 캡처된 클로저의 GC를 방지합니다.

**왜 일어나는가:**
정화 경로가 GameObject를 살려두기 위해 컴포넌트를 비활성화합니다 (객체가 NPC가 되므로 올바름). 하지만 UI 작성자는 `OnDestroy` 또는 명확한 "이 적은 끝났음" 신호를 기대합니다. 기존 `OnDeath` 이벤트는 정화 시에도 발생하므로, 바가 구독 해제를 위해 `OnDeath`를 구독하면 회피 가능합니다 — 하지만 바의 `OnDeath` 핸들러가 명시적으로 자체 `Unbind()`를 호출하는 경우에만.

**회피 방법:**
- 바의 `OnDeath` 콜백 핸들러에서 항상 `Unbind()`를 시각적 전환보다 먼저 첫 번째 행동으로 호출합니다.
- 정화 경로가 GameObject를 살려 둘 때 구독 해제를 `OnDestroy()`에만 의존하지 마세요.
- 패턴: `void HandleDeath() { Unbind(); StartCoroutine(DeathFadeOut()); }`

**경고 징후:**
- 적을 정화한 후 바가 메모리에 남음 (메모리 프로파일러에서 라이브 `EnemyBarController` 인스턴스로 표시).
- 정화된 적의 UI 바가 정화 후 숨겨지지 않음 (OnDestroy가 절대 발생하지 않기 때문).

**처리 단계:** Phase 1 — 함정 1을 위해 확립된 `Bind/Unbind` 패턴이 `OnDeath`가 `Unbind()`를 트리거하면 자연스럽게 이것을 커버합니다.

---

### 함정 6: 보스 바 Screen Space 캔버스가 월드 스페이스 적 바와 충돌

**무슨 일이 일어나는가:**
적 바가 월드 스페이스 캔버스를 사용하는 동안 보스 바를 위한 Screen Space — Overlay 캔버스를 추가하면 Unity 6 URP 2D에서 z-소팅 문제가 발생합니다. URP 2D는 카메라의 지오메트리 패스에서 월드 스페이스 캔버스를 렌더링하고; 오버레이 캔버스는 항상 위에 렌더링됩니다. 이것은 보통 원하는 것이지만, 파티클 효과나 URP 포스트 프로세싱이 적용되면 Screen Space Overlay 캔버스는 모든 카메라 스택과 포스트 프로세싱을 우회합니다 — 게임의 나머지 부분에는 블룸/색상 그레이딩이 있지만 보스 바는 없게 됩니다. 또한 Screen Space — Overlay에서 Screen Space — Camera 모드 (카메라 스택에 참여)로 전환하려면 렌더 카메라를 할당해야 하는데, 씬 설정 중 자주 잊어버려 빈 보스 바가 됩니다.

**왜 일어나는가:**
"Screen Space Overlay"가 Unity 기본값이고 카메라 구성 없이 즉시 작동합니다. 포스트 프로세싱 제외 문제는 포스트 프로세싱이 추가된 후에만 나타납니다.

**회피 방법:**
- 보스 바 캔버스에 **Screen Space — Camera** (Overlay 아님)를 사용합니다. 메인 카메라를 할당합니다. Plane Distance를 1로 설정합니다 (카메라 바로 앞).
- 이것이 URP 카메라 스택에 참여하고 포스트 프로세싱을 올바르게 받습니다.
- 대안: 보스 바를 적 바와 동일한 월드 스페이스 캔버스에 배치하되 뷰포트 좌표로 매 프레임 계산된 고정 화면 상대 위치에 배치. 더 복잡하지만 완전히 통합된 렌더링.

**경고 징후:**
- 보스 바는 보이지만 URP 포스트 프로세싱을 무시함 (블룸, 색상 보정).
- 보스 바가 2D/3D 지오메트리 위에 렌더링되는 대신 클리핑됨.
- `Camera.main`을 변경하거나 URP 카메라 오버레이를 추가한 후 보스 바가 완전히 사라짐.

**처리 단계:** Phase 1 (보스 UI 설정) — 캔버스 생성 시 카메라 모드를 올바르게 설정해야 합니다. 나중에 변경하면 모든 UI 위치 지정을 재테스트해야 합니다.

---

### 함정 7: 적 바 풀링 — 풀 반환 후 바에 오래된 EnemyStats 참조 남음

**무슨 일이 일어나는가:**
적이 죽고 바가 풀에 반환될 때 바의 캐시된 `EnemyStats stats` 필드는 여전히 파괴 (또는 비활성화 후 재사용)된 적을 참조합니다. 풀이 새 적을 위해 바를 재사용할 때 바가 보이기 전에 `Bind(newStats)`가 호출되지 않으면 바는 이전 적의 stat에서 읽습니다 — 1 프레임 동안 잘못된 HP/오염도 값 표시, 또는 더 나쁘게는 파괴된 `MonoBehaviour` 참조에서 메서드를 호출합니다.

**왜 일어나는가:**
풀링 프레임워크는 객체를 재사용하기 위해 종종 `gameObject.SetActive(true)`만 호출합니다. 명시적인 재설정 단계 없이 이전 참조가 유지됩니다. 이것은 특히 교란적인데, 바가 대부분의 경우 "작동"하기 때문입니다 (새 `Bind` 호출이 곧 도착함), 하지만 1 프레임 오래된 읽기가 눈에 보이는 깜빡임이나 이전 적의 GameObject가 파괴된 경우 `MissingReferenceException`을 유발할 수 있습니다.

**회피 방법:**
- 풀 수명 주기: `Acquire()` → `bar.Bind(enemyStats)` → `bar.gameObject.SetActive(true)`. 바인딩 전에 절대 활성화하지 마세요.
- `Release()` → `bar.Unbind()` → `bar.gameObject.SetActive(false)`.
- `Unbind()`에서: `stats = null`을 명시적으로 설정. 모든 콜백 상단에 null 체크 추가: `if (stats == null) return;`.

**경고 징후:**
- 새 적이 스폰될 때 잘못된 체력 값의 1 프레임 깜빡임.
- 빠른 적 스폰 후 `stats.CurrentHp` 접근 시 `MissingReferenceException`.
- 새 적 생애 초기에 0/0 HP를 보여주는 바.

**처리 단계:** Phase 1 — 바 풀링 시스템 구축 시 풀 acquire/release 계약을 정의해야 합니다.

---

## 기술 부채 패턴

| 지름길 | 즉각적인 이점 | 장기 비용 | 허용 가능한 경우 |
|----------|-------------------|----------------|-----------------|
| 적 프리팹별 캔버스 컴포넌트 | 설정 비용 없음, 자체 포함 프리팹 | 적당 하나의 드로우콜 배치; 20개 적 = 20개+ 배치; 모든 적 프리팹 리팩토링 없이 수정 불가 | 프로덕션에서는 절대 아님 |
| fill 값의 `Update()` lerp | 부드러운 애니메이션, 쉽게 작성 | 모든 애니메이션 바에 대해 매 프레임 캔버스 재빌드 | 많은 바를 풀링할 때는 절대 아님 |
| `OnDestroy()`에서만 구독 해제 | Unity 컴포넌트 수명 주기 패턴과 일치 | 정화 경로 (`enabled = false`) 실패, 파괴 전에 반환된 풀링 바 실패 | 풀링되지 않고 정화 불가능한 적에만 허용 |
| 보스 바에 Screen Space — Overlay | 카메라 구성 없이 즉시 작동 | URP 포스트 프로세싱 우회; 카메라 스택 변경 시 깨짐 | 포스트 프로세싱 없는 프로토타입에는 허용 |
| `Start()`에서 한 번 읽는 하드코딩된 Sweet Spot 값 | 간단 | `WidenPurificationRange()` 업그레이드가 플레이 중 반영되지 않음 | 절대 아님 — 로그라이크는 런 중 업그레이드가 있음 |

---

## 통합 함정

| 통합 | 일반적인 실수 | 올바른 접근법 |
|-------------|----------------|------------------|
| `EnemyStats` 이벤트 + UI 바 | `Start()`에서 구독, `OnDestroy()`에서만 구독 해제 | `Bind(EnemyStats)` / `Unbind()` API 사용; `OnDeath`가 시각적 페이드 전에 `Unbind()` 트리거 |
| URP 2D + 월드 스페이스 캔버스 | 캔버스에 Sorting Layer "Default" 할당 | 적 스프라이트 레이어와 일치하거나 초과하는 Sorting Layer 할당 |
| URP 2D + Screen Space Camera 캔버스 | 캔버스 생성 후 렌더 카메라 할당 잊기 | `Awake()`에서 항상 `Canvas.worldCamera = Camera.main` 설정; null이면 오류를 로그하는 검증 체크 추가 |
| `EnemyStats.bonusPurificationMargin` + Sweet Spot 오버레이 | 바 스폰 시 한 번 `basePurificationMin/Max` 읽기 | `WidenPurificationRange()`가 호출될 때 Sweet Spot 오버레이가 업데이트되도록 새 `OnPurificationRangeChanged` 이벤트 구독 (또는 폴링) |
| `EnemyStats.Die()`가 즉시 `Destroy(gameObject)` 호출 | 정리를 위한 유예 프레임 기대 | 바는 즉시 구독 해제하여 `OnDeath` 처리; `OnDeath` 발생 후 EnemyStats 참조가 유효하다고 가정하지 마세요 |

---

## 성능 함정

| 함정 | 증상 | 예방 | 깨지는 시점 |
|------|----------|------------|----------------|
| 적별 캔버스 컴포넌트 | Frame Debugger가 N개 적에 N개 드로우콜 배치 보임 | 단일 공유 월드 스페이스 캔버스, 풀링된 자식으로 바 | ~10개 적에서 눈에 띔; ~30개 이상에서 심각 |
| 모든 보이는 바에 `Update()` fill lerp | 프로파일러에 매 프레임 `Canvas.BuildBatch` 나타남 | 이벤트 기반 업데이트 + 애니메이션을 위한 자가 비활성화 코루틴 | 1개 바는 괜찮음; 20개 바는 지속적인 1–3ms 오버헤드 유발 |
| 바 `LateUpdate()`에서 `Camera.main` 조회 | 매 프레임 GC 할당 (Camera.main은 Unity 6.1까지 내부적으로 `FindFirstObjectByType` 사용) | `Awake()`에서 바 매니저에 `Camera.main` 참조 캐시 | 모든 바가 독립적으로 이것을 수행하면 비용이 곱해짐 |
| 비에디터 빌드에서 `EnemyStats`의 `OnGUI()` 남아있음 | `OnGUI`는 (`#if UNITY_EDITOR` 가드가 메서드 본문만 감싸고 있지만 `OnGUI` 자체는 여전히 등록됨) 빌드에서도 모든 적마다 매 프레임 호출됨 | `#if UNITY_EDITOR`로 전체 `OnGUI` 메서드 정의 감싸기 | 디버그 드로우가 활성화된 모든 빌드 |
| 로그라이크에서 적 스폰 시 바 프리팹 인스턴스화 | 많은 적이 동시에 스폰될 때 방 진입 시 GC 급등 | 씬 로드 시 바 풀 사전 준비; `EnemyAI.Start()`에서 풀에서 획득 | 8개 이상 동시 스폰에서 눈에 띔 |

---

## UX 함정

| 함정 | 사용자 영향 | 더 나은 접근법 |
|---------|-------------|-----------------|
| Sweet Spot 구간이 오염도 바 fill과 같은 색상 | 플레이어가 한 눈에 목표 구간을 구분할 수 없음 — 핵심 메카닉을 읽을 수 없게 됨 | 낮은 오염도와 높은 오염도 fill 색상 모두에서 깔끔하게 오버레이되는 강한 대비 색상과 알파 사용 (예: 밝은 금색 또는 60% 불투명도의 흰색) |
| Sweet Spot 구간에 가장자리 마커 없음 | 구간 경계가 모호함; 플레이어가 킬 타이밍을 잘못 판단 | 구간 최소 및 최대 경계에 1–2px 세로 선 추가 (가장자리 위치에 앵커된 너비 ~2px의 자식 Image) |
| 적 바가 최대 체력에서도 항상 보임 | 시각적 노이즈; 플레이어가 교전 전에 바가 나타나 화면 복잡도 증가 | 첫 번째 히트 후에만 바 표시, 또는 어그로 상태 진입 시 페이드인; `EnemyAI.ChangeState(Chase)`가 자연스러운 표시 트리거 |
| 보스 바가 즉시 활성화 (전환 없음) | 고긴장 순간에 몰입감을 깨는 갑작스러운 시각적 팝 | 보스 만남 트리거에서 코루틴이나 DOTween tween으로 보스 바 Canvas Group 알파를 0.5–1초에 걸쳐 페이드인 |
| 적 사망 후 1–2 프레임 동안 0% fill 상태 유지 | 플레이어가 "죽었지만 살아있는" 시각적 아티팩트를 잠깐 봄 | `OnDeath`에서 즉시 fill을 0으로 설정하고 페이드아웃 애니메이션 시작 전 같은 콜백에서 바 숨김 |

---

## "완성처럼 보이지만 아닌 것" 체크리스트

- [ ] **적 바가 적을 따라감:** 바 위치가 물리 기반 이동 뒤 1 프레임 지연 방지를 위해 `Update()` 아닌 `LateUpdate()`에서 업데이트됨.
- [ ] **Sweet Spot 구간 업데이트:** `WidenPurificationRange()`가 호출될 때 — 바 스폰 시만이 아니라 — 구간 오버레이가 재계산되는지 확인. 전투 중 `WidenPurificationRange(0.1f)` 호출로 테스트.
- [ ] **정화 경로에서 바 숨김:** 정화 후 바가 사라짐. `OnDeath`가 정화 시 발생함 (EnemyStats.cs 154번 줄에서 확인됨), 따라서 바가 `OnDeath`를 구독해야만 숨겨짐. 직접 확인.
- [ ] **풀링 바 바인딩 확인:** 바 획득, 적 죽임, 풀에 바 반환, 새 적 스폰, 바가 이전 적이 아닌 새 적의 올바른 stat을 보여주는지 확인.
- [ ] **보스 사망 시 보스 바 비활성화:** 보스 `OnDeath` 발생; 보스 바 Canvas Group이 페이드아웃되고 캔버스가 비활성화되는지 확인 (배칭 리소스 소비를 막기 위해 단순히 보이지 않는 것이 아님).
- [ ] **정화된 NPC에 바 없음:** 정화 후 `PurifiedNPC`가 동일 GameObject에서 활성화됨. `PurifiedNPC.Activate()`가 호출될 때 바가 다시 표시되지 않아야 함 — `EnemyStats.enabled = false`가 추가 이벤트 발생을 방지하는지 확인.
- [ ] **카메라 뷰 밖에서 바 숨김:** 카메라 프러스텀 밖의 바는 컬링되지 않으면 여전히 캔버스 배칭을 소비함. `CanvasRenderer.cull = true` 사용 또는 바 follower 스크립트에서 카메라까지 거리 확인.

---

## 복구 전략

| 함정 | 복구 비용 | 복구 단계 |
|---------|---------------|----------------|
| 적별 캔버스 아키텍처 | 높음 | 모든 적 프리팹에서 캔버스 컴포넌트 제거; 씬에 공유 월드 스페이스 캔버스 생성; 모든 바 follower 스크립트를 카메라에 대해 상대적으로 위치 지정으로 업데이트; 모든 적 바 재테스트 |
| 구독 해제되지 않은 이벤트로 인한 메모리 누수 | 중간 | 누락된 `OnDisable`/`Unbind` 호출에 대해 모든 UI 스크립트 감사; 모든 콜백에 null 가드 추가; 5분 플레이 후 메모리 프로파일러 실행하여 증가 없음 확인 |
| 잘못된 부모의 Sweet Spot 오버레이 | 중간 | fill Image에서 바 배경 Image로 오버레이 RectTransform 재부모화; 앵커 값 재계산; 오염도 0%, 50%, 100%에서 회귀 테스트 |
| 포스트 프로세싱을 차단하는 보스 바 Screen Space Overlay | 낮음 | 캔버스 렌더 모드를 Overlay에서 Camera로 변경; `Camera.main` 할당; 모든 UI 위치 지정 오프셋 재테스트 |
| 풀링 후 오래된 바 참조 | 낮음 | `Unbind()`에 명시적 `stats = null` 추가; 모든 콜백에 null 체크 가드 추가; 빠른 적 스폰/사망 주기 재테스트 |

---

## 함정-단계 매핑

| 함정 | 예방 단계 | 검증 |
|---------|------------------|--------------|
| UI 이벤트 구독 누수 (Bind/Unbind 패턴) | Phase 1 — 첫 번째 바 스크립트 전에 확립 | 메모리 프로파일러: 30적 전투 세션 후 할당이 증가하지 않음 |
| 적별 캔버스 드로우콜 폭발 | Phase 1 — 단일 공유 캔버스 아키텍처 결정 | Frame Debugger: 적 바 배치가 라이브 적 수에 관계없이 일정 |
| Update() lerp로 매 프레임 캔버스 더티 | Phase 1 — 이벤트 기반 업데이트 패턴 | 프로파일러: `Canvas.BuildBatch`가 매 프레임이 아닌 히트 프레임에만 급등 |
| Sweet Spot 오버레이 부모 계층 | Phase 1 — 바 프리팹 레이아웃 결정 | 직접 테스트: 오염도 fill이 100%에서 0%로 감소하는 동안 구간이 고정 상태 유지 |
| 정화 경로가 OnDestroy 구독 해제 건너뜀 | Phase 1 — OnDeath가 Unbind() 트리거 | 직접 테스트: 적 정화; 바가 숨겨지고 MissingReferenceException 없음 확인 |
| 보스 바 캔버스 모드 (URP 포스트 프로세싱) | Phase 1 — 보스 UI 캔버스 설정 | QA: URP Volume 포스트 프로세싱 프로파일 적용; 보스 바가 색상 그레이딩과 함께 렌더링되는지 확인 |
| 풀링 바가 오래된 EnemyStats 참조 보유 | Phase 1 — 풀 acquire/release 계약 | 자동 또는 직접: 10개 적 빠르게 스폰, 모두 죽이기, 10개 더 스폰; 오래된 값이나 예외 없음 확인 |
| `bonusPurificationMargin` Sweet Spot 실시간 미업데이트 | Phase 2+ (업그레이드) — 바가 Phase 1에서 업데이트 API 노출해야 함 | 업그레이드 시스템 추가 시: `WidenPurificationRange()` 호출; 보이는 모든 바가 즉시 업데이트되는지 확인 |

---

## 출처

- 직접 코드 감사: `Assets/Enemy/EnemyStats.cs` — 정화 경로, 이벤트 선언, `Die()`의 `Destroy(gameObject)` (2026-03-27)
- 직접 코드 감사: `Assets/Enemy/EnemyAI.cs` — `Start()`에서 구독, `OnDestroy()`에서 구독 해제 패턴 (2026-03-27)
- `.planning/codebase/CONCERNS.md` — "EnemyStats.TakeDamage가 이미 사망했을 때도 OnDamaged를 발생시킴" 알려진 버그; "EnemyStats.Purify()가 런타임에 AddComponent 호출" 취약 영역 (2026-03-27)
- Unity UGUI 문서: 캔버스 배칭 동작, 렌더 모드 옵션, fillAmount 더티 마킹 — 높은 신뢰도 (Unity 5에서 안정적인 동작, Unity 6 UGUI 2.0.0에서 확인됨)
- Unity URP 2D 문서: Screen Space — Camera vs Overlay, 카메라 스택 참여 — 높은 신뢰도
- Unity 캔버스 성능 가이드: "동적 요소당 하나의 캔버스" 안티 패턴 — 높은 신뢰도 (Unity 매뉴얼에 잘 문서화됨)

---
*함정 리서치 대상: Unity 6 UGUI — 2D 액션 로그라이크 UI 시스템 (Bleeding Spring)*
*조사일: 2026-03-27*
