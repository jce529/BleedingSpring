# 아키텍처 리서치

**도메인:** Unity 6 (2D URP) — 기존 로그라이크에 UGUI 시스템 통합
**조사일:** 2026-03-27
**신뢰도:** 높음 (기존 코드베이스 기반 + Unity 5–6에서 안정적인 Unity UGUI 기초)

---

## 시스템 개요

```
┌──────────────────────────────────────────────────────────────────────┐
│                     스크린 스페이스 — 오버레이 캔버스                    │
│  ┌───────────────────┐        ┌──────────────────────────────────┐   │
│  │   플레이어 HUD     │        │   보스 바 (조건부 활성화)           │   │
│  │  - WaterBar       │        │  - BossHpBar (전체 너비)          │   │
│  │  - CorruptionBar  │        │  - BossNameLabel                 │   │
│  │  - WaterTierIcons │        └──────────────────────────────────┘   │
│  │  - SkillCooldowns │                                               │
│  └───────────────────┘                                               │
├──────────────────────────────────────────────────────────────────────┤
│                     월드 스페이스 캔버스 (적 프리팹별)                  │
│  ┌──────────────────────────────────────────────────┐                │
│  │  EnemyUIRoot (적 Transform 추적)                  │                │
│  │  - EnemyHpBar (Image fill)                       │                │
│  │  - EnemyCorruptionBar (Image fill)               │                │
│  │    └─ SweetSpotOverlay (RectTransform, 고정)     │                │
│  └──────────────────────────────────────────────────┘                │
├──────────────────────────────────────────────────────────────────────┤
│                     게임 레이어 (기존 코드베이스)                       │
│  ┌───────────────┐  ┌───────────────┐  ┌──────────────────────────┐ │
│  │PlayerWaterStats│  │  EnemyStats  │  │    GameStateManager      │ │
│  │ OnWaterChanged │  │  OnDamaged   │  │    OnGameStateChange     │ │
│  │ OnCorruption.. │  │  OnDeath     │  │                          │ │
│  │ OnWaterTier..  │  │  (+ props)   │  │                          │ │
│  └───────────────┘  └───────────────┘  └──────────────────────────┘ │
│          ↑                  ↑                        ↑               │
│    PlayerHUDPresenter  EnemyUIPresenter        GameOverUI /          │
│    (구독)              (구독)                  PauseUI               │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 컴포넌트 책임

| 컴포넌트 | 책임 | 스크립트 위치 |
|-----------|----------------|-----------------|
| `PlayerHUDPresenter` | `PlayerWaterStats` 이벤트 구독, HUD Image fill 및 티어 아이콘 구동 | `Assets/UI/Player/PlayerHUDPresenter.cs` |
| `WaterBarView` | 물(HP) fill 바의 `Image` 참조 보유 | `Assets/UI/Player/WaterBarView.cs` |
| `CorruptionBarView` | 플레이어 오염도 fill 바의 `Image` 참조 보유 | `Assets/UI/Player/CorruptionBarView.cs` |
| `WaterTierIndicator` | 0–3 티어 아이콘 표시 / 활성 티어 강조 | `Assets/UI/Player/WaterTierIndicator.cs` |
| `SkillCooldownView` | 스킬 슬롯별 방사형 fill 오버레이, `ISkill.IsOnCooldown` + 남은 시간으로 구동 | `Assets/UI/Player/SkillCooldownView.cs` |
| `EnemyUIPresenter` | `EnemyStats` 이벤트 구독, 적 바 구동, Sweet Spot 오버레이 위치 계산 | `Assets/UI/Enemy/EnemyUIPresenter.cs` |
| `EnemyHpBarView` | 월드 스페이스 캔버스의 HP fill `Image` 보유 | `Assets/UI/Enemy/EnemyHpBarView.cs` |
| `EnemyCorruptionBarView` | 오염도 fill `Image`와 Sweet Spot RectTransform 보유 | `Assets/UI/Enemy/EnemyCorruptionBarView.cs` |
| `SweetSpotOverlay` | 상태 없는 뷰: (min, max, barWidth)를 받아 하이라이트 rect 위치/크기 지정 | `Assets/UI/Enemy/SweetSpotOverlay.cs` |
| `BossBarPresenter` | 보스 방 진입 시 활성화, 보스 `EnemyStats` 구독, 스크린 스페이스 바 구동 | `Assets/UI/Boss/BossBarPresenter.cs` |
| `GameOverUI` | `GameStateManager.OnGameStateChange` 구독, `GameOver` 상태에서 패널 표시 | `Assets/UI/Overlay/GameOverUI.cs` |

---

## 권장 프로젝트 구조

```
Assets/
└── UI/
    ├── Player/
    │   ├── PlayerHUDPresenter.cs      # 이벤트 브릿지: PlayerWaterStats → HUD 뷰
    │   ├── WaterBarView.cs            # 물 HP Image.fillAmount 구동
    │   ├── CorruptionBarView.cs       # 플레이어 오염도 Image.fillAmount 구동
    │   ├── WaterTierIndicator.cs      # 활성 티어 아이콘 강조 (0–3)
    │   └── SkillCooldownView.cs       # 스킬 슬롯별 방사형 오버레이
    ├── Enemy/
    │   ├── EnemyUIPresenter.cs        # 이벤트 브릿지: EnemyStats → 적 바 뷰
    │   ├── EnemyHpBarView.cs          # HP fill 바
    │   ├── EnemyCorruptionBarView.cs  # 오염도 fill 바 + SweetSpotOverlay 포함
    │   └── SweetSpotOverlay.cs        # 하이라이트 rect 위치/크기 지정
    ├── Boss/
    │   ├── BossBarPresenter.cs        # 방 진입 시 보스 EnemyStats에 바인딩
    │   └── BossBarView.cs             # 스크린 스페이스 fill 바 + 이름 레이블
    └── Overlay/
        ├── GameOverUI.cs              # GameStateManager → 패널 표시/숨김
        └── PauseUI.cs                 # GameStateManager.Paused → 표시/숨김
```

---

## 아키텍처 패턴

### 패턴 1: Stats별 Presenter (MVP, 프레임워크 없음)

**내용:** 각 stats 클래스(`PlayerWaterStats`, `EnemyStats`)는 변경 시마다 발생하는 C# `Action` 이벤트를 보유합니다. 전용 Presenter MonoBehaviour가 `Start()`에서 해당 이벤트를 구독하고 데이터 값을 UGUI 프로퍼티 쓰기로 변환합니다. View 스크립트(Bar, Indicator)는 타입이 지정된 setter만 노출하며 자체 이벤트나 게임 레이어 참조는 없습니다.

**이 코드베이스에서의 이유:** 기존 이벤트(`OnWaterChanged(float, float)`, `OnCorruptionChanged(float, float)`, `OnWaterTierChanged(int)`)가 HUD에 필요한 정확한 데이터를 이미 전달하고 있습니다. 폴링이나 중간 버스가 필요 없습니다.

**사용 시점:** 이 프로젝트의 모든 UI. 결합 거리는 Stats → Presenter → View입니다. Stats는 UI에 대해 아무것도 모릅니다. View는 게임 로직에 대해 아무것도 모릅니다.

**컴포넌트 경계 규칙:**
- Presenter는 Stats 스크립트에 대한 `[SerializeField]` 참조를 보유합니다 (프리팹/인스펙터를 통해 주입).
- View는 UGUI 컴포넌트 참조(`Image`, `Text`, `RectTransform`)만 보유합니다.
- Presenter는 UGUI 컴포넌트에 직접 접근하지 않으며 View setter를 호출합니다.

**예시 구조:**
```csharp
// PlayerHUDPresenter.cs
public class PlayerHUDPresenter : MonoBehaviour
{
    [SerializeField] private PlayerWaterStats stats;
    [SerializeField] private WaterBarView     waterBar;
    [SerializeField] private CorruptionBarView corruptionBar;
    [SerializeField] private WaterTierIndicator tierIndicator;

    private void Start()
    {
        // 현재 값으로 초기화 (씬 로드 상태 처리)
        waterBar.SetFill(stats.WaterRatio);
        corruptionBar.SetFill(stats.CorruptionRatio);
        tierIndicator.SetTier(stats.WaterTier);

        stats.OnWaterChanged      += HandleWaterChanged;
        stats.OnCorruptionChanged += HandleCorruptionChanged;
        stats.OnWaterTierChanged  += tierIndicator.SetTier;
    }

    private void OnDestroy()
    {
        stats.OnWaterChanged      -= HandleWaterChanged;
        stats.OnCorruptionChanged -= HandleCorruptionChanged;
        stats.OnWaterTierChanged  -= tierIndicator.SetTier;
    }

    private void HandleWaterChanged(float current, float max)
        => waterBar.SetFill(current / max);

    private void HandleCorruptionChanged(float current, float max)
        => corruptionBar.SetFill(current / max);
}
```

**트레이드오프:**
- ScriptableObject 이벤트 버스 오버헤드 없음 — 소수의 stats 객체를 가진 싱글플레이어 게임에 적합.
- Presenter에는 Stats 스크립트에 대한 인스펙터 참조가 필요합니다. 적 프리팹의 경우 Presenter와 EnemyStats가 같은 프리팹에 있으므로 `Awake`에서 `GetComponent<EnemyStats>()`를 사용하는 것이 깔끔합니다.

---

### 패턴 2: 적 프리팹의 월드 스페이스 캔버스

**내용:** 각 적 프리팹은 **World Space** 렌더 모드로 설정된 `Canvas`를 가진 자식 GameObject를 갖습니다. 캔버스 `RectTransform`은 스프라이트 피벗 위의 고정 로컬 오프셋에 배치됩니다 (예: `localPosition = (0, 1.8f, 0)`). 2D URP에서 월드 스페이스의 경우 캔버스는 카메라 참조를 사용하지 않으며 월드 단위로 자동 렌더링됩니다.

**이유:** 자식 캔버스를 통해 적 Transform을 따라가는 것은 프레임별 코드가 전혀 필요 없습니다. Unity가 위치를 처리하며 바는 단순히 적 위의 로컬 스페이스에 유지됩니다. 이것은 2D 액션 게임의 표준 접근법입니다.

**캔버스 설정:**
- 렌더 모드: World Space
- Dynamic Pixels Per Unit: 10 (작은 월드 크기에서 텍스트/스프라이트 흐림 방지)
- Sorting Layer: "UI" (모든 스프라이트 레이어 위)
- Sort Order: 적별 설정으로 적이 겹칠 때 z-fighting 방지

**프리팹 계층:**
```
EnemyRoot (EnemyAI, EnemyAttack, EnemyStats, EnemyUIPresenter)
└── EnemyCanvas [Canvas — World Space, RectTransform 200×40px at (0, 1.8, 0)]
    ├── HpBarBG [Image — 어두운 배경]
    │   └── HpBarFill [Image — 초록, fillMethod=Horizontal, EnemyHpBarView]
    ├── CorruptionBarBG [Image — 어두운 배경]
    │   ├── CorruptionBarFill [Image — 보라, fillMethod=Horizontal, EnemyCorruptionBarView]
    │   └── SweetSpotHighlight [Image — 반투명 노란색, SweetSpotOverlay]
    └── (선택) EnemyNameLabel [TextMeshProUGUI]
```

**EnemyUIPresenter 연결:** Presenter는 `EnemyRoot`에 위치합니다. `Awake`에서 `GetComponent<EnemyStats>()`를 호출하므로 인스펙터 드래그가 필요 없습니다. Sweet Spot 오버레이 초기화를 위해 `EnemyStats`에서 `basePurificationMin`, `basePurificationMax`, `bonusPurificationMargin`을 읽습니다.

---

### 패턴 3: Sweet Spot 하이라이트 — RectTransform 앵커 오버레이

**내용:** Sweet Spot은 `CorruptionBarFill`의 부모(바 배경)의 자식인 반투명 색상 `Image`로, 정화 범위를 정확히 커버하도록 로컬 rect 공간에서 위치와 크기가 지정됩니다.

**커스텀 셰이더 대신 이 접근법을 사용하는 이유:** 오염도 바는 `Image.fillMethod = Horizontal`을 사용하므로 fill 영역 0→1이 `RectTransform.rect.width`에 직접 매핑됩니다. 바 위에 앉은 자식 RectTransform은 바 너비에 곱한 동일한 0–1 정규화 값을 사용하여 위치를 지정할 수 있습니다. 셰이더도, 커스텀 메시도 필요 없으며 순수한 UGUI입니다.

**계산 (`SweetSpotOverlay.cs`):**
```
barWidth  = corruptionBarRect.rect.width   // 예: 200px

// EnemyStats 프로퍼티:
float effectiveMin = stats.basePurificationMin - stats.bonusPurificationMargin;
float effectiveMax = stats.basePurificationMax + stats.bonusPurificationMargin;

// 위치: 바 너비의 effectiveMin 비율에서 하이라이트 왼쪽 가장자리
// SweetSpotHighlight의 Pivot = (0, 0.5) — 왼쪽 앵커
highlightRect.anchorMin = new Vector2(0, 0);
highlightRect.anchorMax = new Vector2(0, 1);
highlightRect.offsetMin = new Vector2(effectiveMin * barWidth, 0);
highlightRect.offsetMax = new Vector2(effectiveMax * barWidth, 0);
```

또는 부모가 바 배경 RectTransform이므로 두 오프셋을 0으로 설정하고 `anchorMin.x = effectiveMin`, `anchorMax.x = effectiveMax`를 사용하는 것이 **더 깔끔한 접근법**입니다:

```csharp
// SweetSpotOverlay.cs
public void SetRange(float normalizedMin, float normalizedMax)
{
    var rect = GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(normalizedMin, 0f);
    rect.anchorMax = new Vector2(normalizedMax, 1f);
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
}
```

**EnemyUIPresenter 호출 지점:**
```csharp
float min = stats.basePurificationMin - stats.bonusPurificationMargin;
float max = stats.basePurificationMax + stats.bonusPurificationMargin;
sweetSpotOverlay.SetRange(min, max);
```

이는 `Start()`에서 한 번 호출하고 `bonusPurificationMargin`이 변경될 때마다 다시 호출해야 합니다 (미래 아이템 시스템). 아이템 시스템이 구현될 때 `EnemyStats`에 `OnPurificationRangeChanged` 이벤트를 추가해야 하며 그 전까지는 `Start`에서 `SetRange`를 한 번 호출합니다.

**시각적 스펙:**
- 색상: `new Color(1f, 0.9f, 0.2f, 0.4f)` — 반투명 금색
- Raycast Target: false (입력 차단 없음)
- Image Type: Simple (스프라이트 불필요 — `Image.color`를 통한 단색)

---

### 패턴 4: 보스 바 — 스크린 스페이스, 조건부 활성화

**내용:** 보스 바는 플레이어 HUD와 동일한 Screen Space Overlay Canvas에 있지만 시작 시 **비활성화**(`gameObject.SetActive(false)`)된 전용 패널 안에 있습니다. 게임이 보스 방에 진입하면 `BossBarPresenter`가 활성화되어 패널에서 `SetActive(true)`를 호출하고 보스의 `EnemyStats`를 구독합니다.

**왜 적 월드 스페이스 캔버스와 분리하는가:** 보스 바는 보스 위치에 관계없이 항상 화면에 고정되어야 하며 크고 잘 보여야 합니다. 보스에 월드 스페이스 캔버스를 사용하면 transform을 따라가야 하는데, 보스가 화면 밖으로 이동하면 이것이 깨집니다.

**활성화 패턴:**
```csharp
// BossBarPresenter.cs — 미래 BossRoomTrigger에 의해 호출됨
public void BindToBoss(EnemyStats bossStats)
{
    this.bossStats = bossStats;
    bossBarPanel.SetActive(true);
    bossNameLabel.text = bossStats.gameObject.name;

    // 현재 값으로 fill 초기화
    bossHpBar.SetFill(bossStats.CurrentHp / bossStats.MaxHp);

    bossStats.OnDamaged += HandleBossDamaged;
    bossStats.OnDeath   += HandleBossDeath;
}

private void HandleBossDamaged()
{
    bossHpBar.SetFill(bossStats.CurrentHp / bossStats.MaxHp);
}

private void HandleBossDeath()
{
    bossStats.OnDamaged -= HandleBossDamaged;
    bossStats.OnDeath   -= HandleBossDeath;
    StartCoroutine(HidePanelAfterDelay(2f));
}
```

**`EnemyStats` 갭 참고:** `EnemyStats`는 현재 `OnDamaged`(데이터 없음)와 `OnDeath`를 발생시킵니다. `PlayerWaterStats`처럼 지속적인 stat 변경 이벤트는 발생시키지 않습니다. 보스 바는 `HandleBossDamaged` 내에서 `stats.CurrentHp / stats.MaxHp`를 다시 읽어야 합니다. `OnDamaged`가 모든 `TakeDamage` 호출 시 발생하므로 이것은 허용됩니다.

---

### 패턴 5: 스킬 쿨다운 — 방사형 Fill 오버레이

**내용:** 스킬 슬롯별로, `fillMethod = Radial360`을 사용하는 전체 크기의 반투명 어두운 `Image`가 스킬 아이콘 위에 오버레이됩니다. 스킬이 쿨다운에 진입하면 fill amount가 1.0(완전히 덮임)에서 시작하여 쿨다운 기간 동안 0.0으로 구동됩니다.

**방사형을 선형 대신 사용하는 이유:** 방사형은 액션 게임 스킬에서 "준비까지 남은 시간"을 더 직관적으로 전달합니다. 커스텀 셰이더 없이 UGUI 내장 기능입니다.

**쿨다운 데이터 갭:** `SkillBase`는 `IsOnCooldown`(bool)과 `cooldownDuration`(직렬화된 float)을 노출하지만 남은 시간 float 프로퍼티는 노출하지 않습니다. `SkillCooldownView`는 `IsOnCooldown`이 false에서 true로 전환될 때 코루틴을 시작하여 경과 시간을 직접 추적함으로써 fill을 구동해야 합니다.

**구현 접근법 — Update에서 폴링:**
```csharp
// SkillCooldownView.cs
private ISkill skill;
private bool wasOnCooldown;
private float cooldownStart;

private void Update()
{
    bool isOnCooldown = skill.IsOnCooldown;

    if (isOnCooldown && !wasOnCooldown)
        cooldownStart = Time.time;

    if (isOnCooldown)
    {
        float elapsed  = Time.time - cooldownStart;
        float fill     = 1f - Mathf.Clamp01(elapsed / cooldownDuration);
        radialOverlay.fillAmount = fill;
        radialOverlay.gameObject.SetActive(true);
    }
    else
    {
        radialOverlay.gameObject.SetActive(false);
    }

    wasOnCooldown = isOnCooldown;
}
```

**대안 (SkillBase 확장 시 선호):** `ISkill`과 `SkillBase`에 `public float CooldownRemaining { get; }`와 `public float CooldownDuration { get; }`를 추가합니다. 이로써 폴링이 제거되고 View가 순수하게 데이터 기반이 됩니다. 이 확장은 Phase 1에서 UI 구현과 함께 수행해야 합니다.

---

## 데이터 흐름

### 플레이어 Stats → HUD

```
PlayerWaterStats.SacrificeWater() / ReceiveAttack() / Heal()
    │
    ├── OnWaterChanged(float current, float max) ──────► PlayerHUDPresenter
    │                                                         └── WaterBarView.SetFill(current/max)
    │                                                              └── Image.fillAmount = value
    │
    ├── OnCorruptionChanged(float current, float max) ─► PlayerHUDPresenter
    │                                                         └── CorruptionBarView.SetFill(current/max)
    │
    └── OnWaterTierChanged(int tier) ──────────────────► WaterTierIndicator.SetTier(tier)
```

### 적 Stats → 월드 스페이스 바

```
EnemyStats.TakeDamage(hp, corruption)
    │
    ├── CurrentHp / CurrentCorruption 업데이트 (지속 값에 대한 이벤트 없음)
    │
    └── OnDamaged.Invoke() ─────────────────────────────► EnemyUIPresenter.HandleDamaged()
                                                               ├── EnemyHpBarView.SetFill(stats.CurrentHp / stats.MaxHp)
                                                               └── EnemyCorruptionBarView.SetFill(
                                                                       stats.CurrentCorruption / stats.MaxCorruption)
```

**데이터 흐름 갭 표시:** `EnemyStats`는 별도의 HP 변경 및 오염도 변경 이벤트를 발생시키지 않습니다. `TakeDamage`당 두 가지가 함께 변경되는 `OnDamaged`만 발생시킵니다. 이것으로 충분합니다: Presenter는 모든 `OnDamaged`에서 두 비율을 읽습니다. Phase 1을 위해 `EnemyStats` 리팩토링이 필요 없습니다.

### 게임 상태 → 오버레이 UI

```
PlayerWaterStats.CheckDeath()
    └── GameStateManager.SetState(GameOver)
            └── OnGameStateChange(GameState.GameOver) ──► GameOverUI.HandleStateChange()
                                                               └── gameOverPanel.SetActive(true)
```

---

## 컴포넌트 경계

| 경계 | 통신 | 방향 | 참고 |
|----------|---------------|-----------|-------|
| `PlayerWaterStats` ↔ `PlayerHUDPresenter` | C# Action 이벤트 | Stats → Presenter (단방향) | Presenter가 구독; Stats는 UI를 절대 참조하지 않음 |
| `EnemyStats` ↔ `EnemyUIPresenter` | C# Action 이벤트 + 직접 프로퍼티 읽기 | Stats → Presenter (단방향) | Presenter는 `OnDamaged`에서 `CurrentHp`, `MaxHp`, `CurrentCorruption`, `MaxCorruption` 읽음 |
| `EnemyUIPresenter` ↔ `SweetSpotOverlay` | 직접 메서드 호출 `SetRange(min, max)` | Presenter → View | `Start`에서 한 번 호출; 정화 마진 변경 시 반복 |
| `SkillBase` ↔ `SkillCooldownView` | `Update`에서 `ISkill.IsOnCooldown` 폴링 | View가 Skill 폴링 (3개 스킬에 허용) | ISkill 확장 시 이벤트/프로퍼티로 업그레이드 |
| `GameStateManager` ↔ `GameOverUI` / `PauseUI` | `OnGameStateChange` 이벤트 | Manager → UI | 기존 `InputHandler` 구독과 동일한 패턴 |
| `BossBarPresenter` ↔ `EnemyStats` (보스) | 직접 메서드 호출 `BindToBoss(EnemyStats)` + 이벤트 | 외부 트리거 → Presenter, 그 다음 Stats → Presenter | 보스 방 트리거가 `BindToBoss` 호출; 미래 마일스톤 |

---

## 빌드 순서

UI 컴포넌트 간의 의존성이 구현 순서를 결정합니다:

```
1. UGUI 캔버스 스캐폴드
   └── 플레이어 HUD 캔버스 생성 (Screen Space Overlay)
   └── 동일 캔버스 내 보스 바 패널 생성 (비활성화)

2. WaterBarView + CorruptionBarView (상태 없는 fill setter)
   └── 게임 스크립트에 대한 의존성 없음

3. PlayerHUDPresenter
   └── 의존: PlayerWaterStats (존재), WaterBarView, CorruptionBarView

4. WaterTierIndicator
   └── 의존: PlayerHUDPresenter 연결 (OnWaterTierChanged)

5. SweetSpotOverlay (상태 없는, 순수 뷰)
   └── 게임 스크립트 의존성 없음

6. EnemyCorruptionBarView + EnemyHpBarView
   └── 게임 스크립트 의존성 없음

7. EnemyUIPresenter (적 프리팹에)
   └── 의존: EnemyStats (존재), EnemyHpBarView, EnemyCorruptionBarView, SweetSpotOverlay

8. SkillCooldownView
   └── 의존: ISkill 인터페이스 (존재); 선택적으로 SkillBase 쿨다운 확장

9. BossBarPresenter + BossBarView
   └── 의존: EnemyStats (존재), BossBarView, BossBarPanel 활성화 로직
   └── 필요: 보스 방 트리거 (Phase 2 범위) — Phase 1 테스트를 위해 바인드 메서드 스텁
```

**이 순서의 근거:** 상태 없는 뷰 컴포넌트(2, 5, 6)는 Presenter가 존재하기 전에 인스펙터의 플레이스홀더 값으로 빌드하고 테스트할 수 있습니다. Presenter(3, 7, 8, 9)는 뷰 의존성이 확인된 후 마지막에 연결됩니다. 보스 바는 아직 존재하지 않는 씬 아키텍처(방 트리거)가 필요하므로 마지막입니다.

---

## 안티 패턴

### 안티 패턴 1: Presenter가 `Image.fillAmount`에 직접 접근

**사람들이 하는 것:** `[SerializeField] Image waterBarFill`을 보유하고 이벤트 핸들러 내에서 직접 `waterBarFill.fillAmount = stats.WaterRatio`를 설정하는 단일 `PlayerHUD` MonoBehaviour를 작성합니다.

**왜 잘못인가:** 하나의 바에서는 작동합니다. 다섯 개의 HUD 요소가 있으면 클래스가 레이아웃 관심사, stat 해석, 애니메이션을 혼합하는 200줄짜리 God Object가 됩니다. "HP가 낮을 때 빨갛게 깜빡이기" 효과를 추가하려면 단순한 fill setter여야 할 곳에 분기가 필요합니다.

**대신 이렇게:** Presenter를 얇게 유지하세요 (데이터 해석, 타입 setter 호출). View 스크립트를 얇게 유지하세요 (`Image` 참조 보유, `SetFill(float)` 노출, 내부적으로 깜빡임 같은 시각 효과 처리). 이렇게 하면 Presenter 로직을 건드리지 않고 시각적 요소를 교체할 수 있습니다 (예: 바를 아이콘 행으로 교체).

---

### 안티 패턴 2: 적의 자식이 아닌 월드 스페이스 캔버스

**사람들이 하는 것:** 적 바를 위한 별도의 UI GameObject를 만들고 `LateUpdate` 코드로 `Camera.WorldToScreenPoint`를 사용하여 적 위치를 얻고 스크린 스페이스 요소를 이동시킵니다.

**왜 잘못인가:** 스크린-월드 변환에는 카메라 참조가 필요하고, 카메라 줌으로 깨지며, 1프레임 지연 아티팩트가 발생합니다. 스크린 스페이스 적 바는 적이 화면 가장자리 근처에 있을 때 잘못 클리핑됩니다.

**대신 이렇게:** 캔버스를 적 프리팹에 직접 자식화합니다. Unity가 자동으로 이동시킵니다. 위치 지정을 위한 코드가 전혀 없습니다.

---

### 안티 패턴 3: 구독 해제 없이 Stats 이벤트 구독

**사람들이 하는 것:** `Start()`에서 구독하고 `OnDestroy()`에서 구독 해제를 잊습니다.

**왜 잘못인가:** 적이 `Destroy`될 때(`EnemyStats.Die()`가 즉시 수행), `EnemyStats` 컴포넌트는 `OnDeath`를 발생시키고 파괴됩니다. `EnemyUIPresenter`가 구독 해제를 하지 않으면 stats 객체의 델리게이트 목록이 다음 이벤트 발생 시 `MissingReferenceException`을 일으키거나 메모리를 조용히 누수시키는 참조를 보유합니다. 기존 코드베이스는 이미 `OnDestroy` 구독 해제 패턴을 따르고 있습니다 (ARCHITECTURE.md 오류 처리 섹션에 언급). 모든 새 UI Presenter는 이를 따라야 합니다.

**대신 이렇게:** `EnemyAI`가 사용하는 정확한 패턴을 따르세요: `Start()`에서 구독하고, `OnDestroy()`에서 모든 핸들러 구독 해제.

---

### 안티 패턴 4: HP 값을 위해 `Update`에서 `EnemyStats` 폴링

**사람들이 하는 것:** 이벤트 연결을 건너뛰고 대신 매 프레임 `Update()`에서 `stats.CurrentHp`를 읽습니다.

**왜 잘못인가:** 10개의 적이 활성화되면 공격 히트 시에만 변경되는 값에 대해 프레임당 10번의 불필요한 읽기가 발생합니다. 로그라이크 웨이브에서 30개의 적이 있으면 성능이 저하됩니다. 또한 확립된 이벤트 아키텍처를 우회합니다.

**대신 이렇게:** `OnDamaged`(이미 존재)를 트리거로 사용합니다. 핸들러 내에서 stat 값을 한 번 읽습니다.

---

### 안티 패턴 5: 별도 씬 캔버스의 보스 바

**사람들이 하는 것:** 보스 바가 항상 위에 렌더링되도록 두 번째 카메라와 두 번째 캔버스를 만듭니다.

**왜 잘못인가:** 단일 카메라를 가진 2D 게임에서는 불필요합니다. 캔버스의 Sort Order로 충분합니다. 여러 캔버스는 UI 계층을 복잡하게 만들고 드로우콜을 늘립니다.

**대신 이렇게:** 보스 바 패널을 플레이어 HUD와 동일한 Screen Space Overlay Canvas에 더 높은 Sort Order로 넣습니다 (또는 단순히 계층에서 HUD 아래에 — Screen Space Overlay는 계층 순서로 렌더링). `SetActive`를 통해 패널을 활성화/비활성화합니다.

---

## 통합 지점

### 기존 게임 스크립트 → 새 UI 스크립트

| 게임 스크립트 | UI 스크립트 | 연결 방법 |
|-------------|-----------|---------------|
| `PlayerWaterStats` | `PlayerHUDPresenter` | Inspector `[SerializeField]` 참조; Presenter가 3개 이벤트 구독 |
| `EnemyStats` | `EnemyUIPresenter` | `Awake`에서 `GetComponent<EnemyStats>()` (같은 프리팹); `OnDamaged` + `OnDeath` 구독 |
| `SkillBase` (×3) | `SkillCooldownView` (×3) | 각 스킬 MonoBehaviour에 대한 인스펙터 참조를 `ISkill`로 캐스팅 |
| `GameStateManager` | `GameOverUI`, `PauseUI` | `Start`에서 `GameStateManager.Instance.OnGameStateChange` 구독 |
| `EnemyStats` (보스) | `BossBarPresenter` | 미래 보스 방 트리거가 호출하는 `BindToBoss(EnemyStats)` |

### 내부 UI 경계

| 경계 | 통신 | 참고 |
|----------|---------------|-------|
| `PlayerHUDPresenter` ↔ `WaterBarView` | 직접 메서드 호출 | 같은 캔버스에; 인스펙터 참조 |
| `EnemyUIPresenter` ↔ `SweetSpotOverlay` | `SetRange(float, float)` 메서드 | SweetSpotOverlay는 프리팹에서 CorruptionBarBG의 자식 |
| `BossBarPresenter` ↔ `BossBarView` | 직접 메서드 호출 | 둘 다 플레이어 HUD 캔버스에 |

---

## 확장성 고려사항

이것은 싱글플레이어 게임입니다. 여기서 확장성은 "적 수와 스킬 수가 늘어나도 깔끔하게 유지됨"을 의미합니다:

| 관심사 | 5개 적 | 30개 적 | 참고 |
|---------|-------------|---------------|-------|
| 월드 스페이스 캔버스 드로우콜 | 5개 캔버스 | 30개 캔버스 | 각 월드 스페이스 캔버스는 별도의 드로우콜 배치. 30개 이상의 적에서는 단일 공유 월드 스페이스 캔버스 + 수동 바 위치 지정 고려. Phase 1에는 불필요. |
| Sweet Spot 오버레이 재계산 | Start 시 적별 O(1) | Start 시 적별 O(1) | 아이템 시스템이 마진을 변경하기 전까지 정적; 프레임별 비용 없음 |
| 스킬 쿨다운 폴링 | 3번의 Update 호출 | 3번의 Update 호출 (스킬 수 불변) | 무시할 수 있는 수준 |
| 보스 바 | 동시에 1개 활성 | 동시에 1개 활성 | 항상 O(1) |

---

## 출처

- 기존 코드베이스: `Assets/Enemy/EnemyStats.cs`, `Assets/Player/PlayerWaterStats.cs`, `Assets/Player/SkillBase.cs`, `Assets/GameScript/GameStateManager.cs` — 2026-03-27 직접 읽음
- 기존 아키텍처 문서: `.planning/codebase/ARCHITECTURE.md` — 2026-03-27 직접 읽음
- Unity UGUI Image.fillMethod / RectTransform 앵커 시스템 — 높은 신뢰도 (Unity 5–6에서 안정적인 API, docs.unity3d.com에서 문서화됨)
- 월드 스페이스 캔버스 프리팹-자식 패턴 — 높은 신뢰도 (기본 Unity 패턴, Unity 6에서 변경 없음)
- Unity용 MVP/Presenter 패턴 — 높은 신뢰도 (널리 확립됨; 코드베이스의 기존 이벤트 기반 구조와 일치)

---

*아키텍처 리서치 대상: Bleeding Spring — UGUI UI 시스템 통합*
*조사일: 2026-03-27*
