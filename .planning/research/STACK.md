# 스택 리서치

**도메인:** Unity 6 UGUI 기반 HUD 및 2D 액션 로그라이크 월드 스페이스 UI
**조사일:** 2026-03-27
**신뢰도:** 높음 (모든 버전은 Packages/packages-lock.json 및 소스 파일에서 직접 확인됨)

---

## UGUI vs UI Toolkit: 결정

**UGUI를 사용합니다. 이 프로젝트에서 UI Toolkit을 사용하지 마세요.**

UI Toolkit(UIElements)은 이미 모듈로 설치되어 있지만 (`com.unity.modules.uielements` 1.0.0) 세 가지 구체적인 이유로 여기서는 잘못된 도구입니다:

1. **UI Toolkit에서 월드 스페이스를 지원하지 않습니다.** UI Toolkit은 `PanelSettings` 에셋과 `UIDocument` 컴포넌트로 렌더링되며 Screen Space에서만 작동합니다. 월드 스페이스 캔버스에 해당하는 것이 없습니다. 적 위에 떠있는 적 체력 바는 월드 스페이스 캔버스가 필요합니다 — UGUI 전용 기능입니다.

2. **UGUI 2.0.0이 이미 설치되어 잠겨있습니다.** 프로젝트 `manifest.json`에서 `"com.unity.ugui": "2.0.0"`을 직접 의존성으로 선언합니다. URP 17.3.0 (`com.unity.render-pipelines.core`)도 `com.unity.ugui: 2.0.0`을 자체 의존성으로 선언합니다. 추가 설치가 전혀 필요 없습니다.

3. **기존 아키텍처가 C# Action 이벤트에 바인딩됩니다.** `PlayerWaterStats.OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged`, `EnemyStats.OnDamaged`, `OnDeath`, `SkillBase.IsOnCooldown`은 모두 일반 C# API입니다. UGUI의 `Image.fillAmount`와 `Slider.value`는 MonoBehaviour에서 이것들에 간단하게 바인딩됩니다. UI Toolkit은 UXML/USS + 바인딩 디스크립터가 필요합니다 — 이점 없는 상당한 오버헤드입니다.

**신뢰도: 높음** — 설치된 manifest에 대해 검증됨, EnemyStats.cs 및 PlayerWaterStats.cs 소스 코드에 대해 검증됨, 알려진 Unity 6 아키텍처 제약 사항에 대해 검증됨 (UI Toolkit 월드 스페이스 제한은 Unity 6에서 문서화된 엔진 제약 사항).

---

## 권장 스택

### 핵심 기술

| 기술 | 버전 | 목적 | 권장 이유 |
|------------|---------|---------|-----------------|
| Unity UGUI | 2.0.0 (내장) | 모든 런타임 UI: HUD, 월드 스페이스 바, 보스 바, 스킬 쿨다운 | 이미 설치됨; 월드 스페이스 캔버스를 지원하는 유일한 시스템; 직접 MonoBehaviour 통합이 기존 C# 이벤트 아키텍처와 일치 |
| Unity URP | 17.3.0 (내장) | 모든 UI 캔버스를 그리는 렌더 파이프라인 | 이미 구성됨; 월드 스페이스 캔버스는 렌더 모드 = 월드 스페이스와 카메라 참조로 URP 2D에서 올바르게 그려짐 |
| Unity Input System | 1.19.0 | UI에서 직접 사용되지 않지만 기존 `InputHandler`가 `GameStateManager`를 통해 이미 모든 입력을 게이팅 — UI 레이어가 별도로 입력을 등록할 필요 없음 | 이미 연결됨; UI 레이어에 추가 설정 불필요 |
| Unity Engine 코루틴 | 내장 | UI에서 스킬 쿨다운 fill 애니메이션 구동 | `SkillBase` 쿨다운은 코루틴으로 구동됨; UI 쿨다운 오버레이가 `SkillBase.IsOnCooldown`을 폴링하거나 `SkillBase`에 추가된 새 `OnCooldownChanged(float ratio)` 이벤트를 통해 미러링 |

### 캔버스 아키텍처: 세 개의 별도 캔버스

| 캔버스 | 렌더 모드 | 소트 오더 / 레이어 | 목적 |
|--------|------------|-------------------|---------|
| `HUDCanvas` | Screen Space - Overlay | Sort Order: 10 | 플레이어 HP 바, 오염도 바, 워터 티어 표시기, 스킬 쿨다운 슬롯 |
| `EnemyWorldCanvas` (프리팹별) | World Space | Sorting Layer: "UI", Order: 5 | 적별 HP 바 + Sweet Spot 구간이 있는 오염도 바; 적 GameObject의 자식 |
| `BossHUDCanvas` | Screen Space - Overlay | Sort Order: 20 | 화면 하단에 앵커된 큰 보스 HP 바; 보스 만남 중에만 활성화 |

**왜 하나가 아닌 별도 캔버스인가:**
- 월드 스페이스 캔버스는 각 적 인스턴스의 부모가 되어야 합니다. 단일 글로벌 월드 스페이스 캔버스는 수동 위치 추적 없이 여러 적을 따를 수 없습니다.
- 보스 바는 자체 캔버스에 있어 일반 HUD 캔버스를 건드리지 않고 깔끔하게 활성화/비활성화할 수 있습니다.
- Screen Space - Overlay는 카메라 의존성 없이 모든 것 위에 렌더링됩니다 — 2D 게임의 플레이어 HUD와 보스 바에 올바릅니다.

### 지원 라이브러리

| 라이브러리 | 버전 | 목적 | 사용 시점 |
|---------|---------|---------|-------------|
| `UnityEngine.UI` (UGUI 모듈) | 2.0.0 | `Image`, `Slider`, `Canvas`, `CanvasScaler`, `GraphicRaycaster` | 모든 UI 요소 |
| `TextMeshPro` | Unity 6 내장 (`com.unity.textmeshpro`로, 번들됨) | 숫자 값, 티어 레이블의 텍스트 렌더링 | 모든 텍스트 요소; 레거시 `Text` 컴포넌트 대신 TMP 사용 |
| `UnityEngine.EventSystems` | UGUI 2.0.0의 일부 | 모든 UGUI 씬에 필요한 `EventSystem` 싱글톤 | HUDCanvas 또는 전용 GameObject에 씬당 하나의 `EventSystem` |

**TextMeshPro 참고:** Unity 6은 TMP를 핵심 모듈로 번들합니다. 별도 항목으로 `manifest.json`에 나타나지 않는데 에디터와 함께 제공되기 때문입니다. `Window > TextMeshPro > Import TMP Essential Resources`를 통해 TMP 필수 리소스를 한 번 가져오세요.

### 개발 도구

| 도구 | 목적 | 참고 |
|------|---------|-------|
| Unity Editor 인스펙터 | UI 스크립트에서 UGUI 컴포넌트로 `[SerializeField]` 참조 연결 | 모든 UI 컨트롤러 참조 (Image, Slider)는 인스펙터를 통해 설정됨, 기존 프로젝트 패턴과 일치 |
| Unity Animator (선택적) | 피해 시 바 플래시, 보스 바 인트로 애니메이션 | 절제해서 사용; `DOTween` 또는 코루틴이 단순한 fill tween에 더 가벼움 |
| `CanvasScaler` 컴포넌트 | 1920x1080 목표 해상도에서 HUD 비율 유지 | Scale Mode를 `Scale With Screen Size`로, Reference Resolution `1920x1080`, Match `0.5`로 설정 |

---

## 설치

추가 패키지 불필요. 필요한 모든 것이 이미 있습니다:

```
com.unity.ugui: 2.0.0          -- 설치됨 (manifest.json 19번 줄)
com.unity.modules.ui: 1.0.0    -- 설치됨 (내장)
com.unity.modules.uielements: 1.0.0 -- 설치됨 (내장, 런타임 UI에 미사용)
```

TextMeshPro 필수 에셋 가져오기 (일회성, 에디터 전용):
```
Window > TextMeshPro > Import TMP Essential Resources
```

---

## 컴포넌트 패턴

### 플레이어 HUD: HP 및 오염도 바

**컴포넌트:** `Image Type = Filled`, `Fill Method = Horizontal`의 `Image`

`Start()`에서 이벤트 구독, `OnDestroy()`에서 구독 해제:

```csharp
// PlayerWaterStats.cs 39-42번 줄 시그니처:
// public event Action<float, float> OnWaterChanged;         // (current, max)
// public event Action<float, float> OnCorruptionChanged;    // (current, max)
// public event Action<int> OnWaterTierChanged;              // (tier 0-3)

stats.OnWaterChanged     += (cur, max) => hpFill.fillAmount = cur / max;
stats.OnCorruptionChanged += (cur, max) => corruptionFill.fillAmount = cur / max;
stats.OnWaterTierChanged  += tier => UpdateTierIndicator(tier);
```

**`Slider` 대신 filled `Image`를 사용하는 이유:** `Slider`에는 입력을 위한 인터랙티브 핸들과 인터랙터블 상태가 있습니다. filled `Image`는 입력 표면 영역이 없는 순수 표시 컴포넌트입니다 — 클릭을 받지 않는 HUD 바에 올바릅니다.

### 워터 티어 표시기

**컴포넌트:** 티어 0–3을 나타내는 네 개의 `Image` 객체 (또는 스프라이트 시트 애니메이션 `Image`). `tier` 인덱스의 아이콘을 켜고 나머지를 끕니다. 대안: 현재 티어 번호를 보여주는 `TMP_Text` 레이블.

**패턴:** `OnWaterTierChanged(int tier)`가 `PlayerWaterStats.CycleWaterTier()`에서 발생합니다. UI 컨트롤러가 티어 인덱스를 받아 활성 상태를 설정합니다.

### 적 월드 스페이스 UI: HP + 오염도 + Sweet Spot

**캔버스 설정:** 적 프리팹의 자식 GameObject에 `Canvas` 컴포넌트 `Render Mode = World Space`. 픽셀당 유닛에 맞게 RectTransform 스케일 조정 (예: 적 머리 위에 1 Unity 단위 너비 100px 바를 위해 스케일 `0.01`).

**Sweet Spot 시각화:** 오염도 바는 가로 레이아웃의 세 개 레이어 `Image` 컴포넌트의 조합입니다:
- 기본 레이어: 전체 너비 배경 (어두운 회색)
- 오염도 fill: `Image Type = Filled`, `EnemyStats.CorruptionRatio`로 구동
- Sweet Spot 하이라이트: 바 너비의 `[basePurificationMin, basePurificationMax]`를 커버하도록 위치와 크기가 지정된 별도 `Image`

```csharp
// EnemyStats.cs 23-29번 줄 (확인됨):
// public float basePurificationMin     = 0.3f;
// public float basePurificationMax     = 0.7f;
// public float bonusPurificationMargin = 0f;
// public float CorruptionRatio => CurrentCorruption / maxCorruption;

float effectiveMin = stats.basePurificationMin - stats.bonusPurificationMargin;
float effectiveMax = stats.basePurificationMax + stats.bonusPurificationMargin;

// 하이라이트 RectTransform 앵커 위치 지정:
sweetSpotImage.rectTransform.anchorMin = new Vector2(effectiveMin, 0f);
sweetSpotImage.rectTransform.anchorMax = new Vector2(effectiveMax, 1f);
sweetSpotImage.rectTransform.offsetMin = Vector2.zero;
sweetSpotImage.rectTransform.offsetMax = Vector2.zero;
```

**업데이트 트리거:** `EnemyStats`는 현재 `OnDamaged`와 `OnDeath`만 발생시킵니다 (44-47번 줄). 실시간 바 업데이트를 위해 UI 컨트롤러는:
- `OnDamaged`를 구독하고 `CurrentHp`, `MaxHp`, `CurrentCorruption`, `MaxCorruption` 프로퍼티를 직접 읽습니다 (이벤트 폴링 — 피해가 이산적이므로 허용)
- 또는 `PlayerWaterStats` 패턴과 일치하는 `Action<float,float> OnHpChanged`와 `Action<float,float> OnCorruptionChanged` 이벤트를 `EnemyStats`에 추가합니다 (더 깔끔함, EnemyStats 수정 시 권장)

**빌보드/방향:** 2D 사이드 스크롤러에서 월드 스페이스 캔버스는 적 스프라이트의 X 뒤집기에 반응하여 역회전해서는 **안 됩니다**. URP 2D에서 적절한 깊이 소팅을 보장하기 위해 `Canvas.worldCamera`를 메인 카메라로 설정하세요.

### 보스 Screen Space HP 바

**캔버스 설정:** `Render Mode = Screen Space - Overlay`, Sort Order 20의 별도 `Canvas`. 바 RectTransform을 `Bottom Center`에 앵커, 패딩을 두고 가로로 늘리기.

**활성화:** 보스 캔버스 `GameObject.SetActive(false)` 기본값; 보스가 스폰될 때 보스 만남 매니저가 활성화.

**데이터 바인딩:** 적 바와 같은 패턴이지만 보스 `EnemyStats` 인스턴스를 구독합니다. 보스 EnemyStats는 같은 프로퍼티를 노출합니다 — 특별한 보스 클래스 불필요.

### 스킬 쿨다운 UI

**문제:** `SkillBase` (26번 줄)는 `bool IsOnCooldown`을 노출하지만 진행 비율 이벤트가 없습니다. `cooldownDuration` 필드는 `protected`이고 코루틴이 내부적으로 추적합니다.

**해결 방법 (선호 순):**

1. **`SkillBase`에 `OnCooldownChanged(float ratio)` 이벤트 추가** — 쿨다운 코루틴 중 매 프레임 `(elapsed/duration)` 방출. 가장 깔끔함; 기존 이벤트 아키텍처와 일치.

2. **`SkillBase`에 `CooldownRatio` 프로퍼티 노출** — 코루틴에서 업데이트되는 `public float CooldownRatio { get; private set; }`. UI 컴포넌트가 `Update()`를 통해 폴링.

3. **UI가 `IsOnCooldown` bool 폴링** — "쿨다운 중" 이진 상태만 표시, fill 진행 없음. fill 애니메이션이 필요 없는 경우 허용.

**권장:** 옵션 1. 기존 코드베이스는 stat-to-UI 통신에 일관되게 이벤트를 사용합니다 (`PlayerWaterStats` 패턴). `SkillBase`에 `public event Action<float> OnCooldownProgress`를 추가하고 `UseCoroutine()`의 매 프레임에서 발생.

**컴포넌트:** `Image Type = Filled`, `Fill Method = Radial 360`, `Clockwise = true`의 `Image`. 스킬 아이콘 위에 오버레이. Fill amount = `1 - cooldownRatio` (준비 시 가득, 쿨다운이 경과함에 따라 비어가기).

---

## 고려된 대안

| 권장 | 대안 | 사용하지 않는 이유 |
|-------------|-------------|---------|
| UGUI `Image` filled | UI Toolkit `ProgressBar` | UI Toolkit에 월드 스페이스 지원 없음; 이 범위에 과도함 |
| 월드 스페이스 캔버스 (적별) | 스크린 스페이스 캔버스 + `Camera.WorldToScreenPoint`로 수동 위치 지정 | 수동 위치 지정이 지터 생성, 카메라 줌으로 깨짐, 별도 시스템에서 매 프레임 업데이트 필요. 월드 스페이스 캔버스가 적 transform을 자동으로 따름 |
| 별도 HUD + 보스 캔버스 | 모든 스크린 UI를 위한 단일 스크린 스페이스 캔버스 | 보스 UI를 독립적으로 활성화/비활성화하기 어려움; 많은 요소가 하나의 캔버스를 공유하면 소트 오더 충돌 |
| C# Action 이벤트 → UI MonoBehaviour | ScriptableObject 이벤트 채널 | SO 채널은 이 규모에서 이점 없는 간접화 추가; 프로젝트가 이미 직접 C# 이벤트를 일관되게 사용 |
| `CanvasScaler` Scale With Screen Size | 스케일러 없음 / 고정 픽셀 크기 | 고정 픽셀 크기가 1920x1080이 아닌 해상도에서 레이아웃 깨짐; Scale With Screen Size가 자동으로 조정 |

---

## 사용하지 말 것

| 피할 것 | 이유 | 대신 사용 |
|-------|-----|-------------|
| UI Toolkit (`UIDocument`, `VisualElement`)을 런타임 게임 UI에 | Unity 6에서 월드 스페이스 캔버스 지원 없음; UXML/USS 워크플로우가 필요해 이벤트 기반 게임 HUD에 잘못된 추상화 레이어인 마찰 추가 | UGUI `Canvas` + `Image` + `TMP_Text` |
| 레거시 `Text` 컴포넌트 | Unity 5.x 시대에 폐기됨; 작은 크기에서 렌더링 품질 낮음; 해결 방법 없이 리치 텍스트 없음 | `TextMeshPro - Text (UI)` (`TMP_Text`) |
| `Slider` 컴포넌트를 체력 바에 | 인터랙티브 핸들, 인터랙터블 상태, 내비게이션 이벤트가 있음 — 표시 전용 바에는 모두 관련 없는 노이즈; `Image`보다 약간 무거움 | `Image Type = Filled`의 `Image` |
| HUD에 `Canvas.renderMode = Screen Space - Camera` | 카메라 할당 필요; 거리/FOV 의존성 생성; Screen Space - Overlay가 2D HUD에 더 단순하고 항상 위에 렌더링 | HUD/보스 캔버스에 `Screen Space - Overlay` |
| 글로벌 "적 UI" 부모의 월드 스페이스 캔버스 | 월드 스페이스가 제거하려던 복잡성을 재도입하는 커스텀 추적 스크립트 없이 개별 적 위치를 따를 수 없음 | 각 적 프리팹의 자식으로 월드 스페이스 캔버스 |
| UI 스크립트에서 `PlayerWaterStats` 위치 찾기 위해 `FindObjectOfType` | `FindObjectOfType`은 느리고 취약; 기존 아키텍처가 이를 피함 | `[SerializeField]`로 인스펙터를 통해 참조 연결, 또는 `PlayerController` / `GameStateManager`가 참조 제공 |

---

## 버전 호환성

| 패키지 | 호환 버전 | 참고 |
|---------|-----------------|-------|
| `com.unity.ugui` 2.0.0 | URP 17.3.0 | URP core가 packages-lock.json에서 `com.unity.ugui: 2.0.0`을 직접 의존성으로 선언; 버전이 엔진에 의해 매칭됨 |
| `com.unity.ugui` 2.0.0 | Unity 6000.3.11f1 | UGUI 2.0.0이 Unity 6과 함께 제공되는 버전; packages-lock.json에서 `source: "builtin"` |
| `TextMeshPro` | Unity 6000.3.11f1 | Unity 6 에디터와 번들됨; 별도 UPM 항목 불필요 |
| 월드 스페이스 캔버스 | URP 2D Renderer 17.3.0 | 월드 스페이스 캔버스가 URP 2D에서 올바르게 렌더링됨; 올바른 소팅 레이어 깊이를 위해 `Canvas.worldCamera`를 씬 카메라로 설정 |

---

## 이벤트 바인딩 참조 (소스에서 확인됨)

| 이벤트 | 소스 파일 | 시그니처 | UI 소비자 |
|-------|------------|-----------|-------------|
| `OnWaterChanged` | `PlayerWaterStats.cs:39` | `Action<float current, float max>` | 플레이어 HP 바 `fillAmount = current/max` |
| `OnCorruptionChanged` | `PlayerWaterStats.cs:42` | `Action<float current, float max>` | 플레이어 오염도 바 `fillAmount = current/max` |
| `OnWaterTierChanged` | `PlayerWaterStats.cs:45` | `Action<int tier>` | 티어 표시기: `tier` 인덱스의 아이콘 활성화 |
| `OnDamaged` | `EnemyStats.cs:44` | `Action` | 바 갱신 트리거; `CurrentHp/MaxHp`, `CurrentCorruption/MaxCorruption` 읽기 |
| `OnDeath` | `EnemyStats.cs:47` | `Action` | 월드 스페이스 캔버스 숨기기/파괴 |
| `IsOnCooldown` | `SkillBase.cs:26` | `bool` 프로퍼티 | 스킬 쿨다운 — 아직 진행 이벤트 없음; `SkillBase` 수정 필요 |

---

## 출처

- `C:\Users\MSI\Projeect_A.E\My project\Packages\manifest.json` — UGUI 2.0.0, URP 17.3.0, Unity Input System 1.19.0 버전 확인 (높은 신뢰도)
- `C:\Users\MSI\Projeect_A.E\My project\Packages\packages-lock.json` — UGUI 내장 소스, 전이적 의존성 체인 확인 (높은 신뢰도)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Player\PlayerWaterStats.cs` — 이벤트 시그니처 `OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged` 확인 (높은 신뢰도)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Enemy\EnemyStats.cs` — `OnDamaged`, `OnDeath` 이벤트; `basePurificationMin/Max`, `CorruptionRatio` 프로퍼티 확인 (높은 신뢰도)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Player\SkillBase.cs` — `IsOnCooldown` 프로퍼티, 쿨다운 진행 이벤트 부재 확인 (높은 신뢰도)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Player\ISkill.cs` — `ISkill` 인터페이스 계약 확인 (높은 신뢰도)
- Unity 6 엔진 제약: UI Toolkit 월드 스페이스 미지원 — `UIDocument` 컴포넌트 아키텍처의 문서화된 제한 (중간 신뢰도 — 훈련 데이터, 실시간 문서로 확인 불가; 그러나 이것은 Unity 버전 전반에서 안정적인 기본 아키텍처적 사실)

---

*스택 리서치 대상: Bleeding Spring — UI 시스템 마일스톤*
*조사일: 2026-03-27*
