# Phase 3: Boss UI - Research

**Researched:** 2024-04-13
**Domain:** Unity UI (UGUI), Boss Management, Visual Feedback (Glow)
**Confidence:** HIGH

## Summary

이 연구는 보스 전투 전용 Screen Space UI 구현을 위한 기술적 설계를 다룹니다. 핵심은 기존 `EnemyStats`의 상속을 통한 데이터 확장, 플레이어 HUD와 통일된 시각 언어(세로 수축 바)를 유지하면서도 화면 우측에 고정된 보스만의 레이아웃을 구축하는 것입니다. 또한 보스전의 몰입도를 위해 일반 적 UI를 억제하고, 정화 가능 구간(Sweet Spot) 진입 시 UI와 보스 본체가 동시에 반응하는 이중 피드백 시스템을 설계합니다.

**Primary recommendation:** `BossStats`를 통해 데이터를 공급받고, `BossUIManager`가 UI 프리팹의 생명주기와 이벤트 바인딩을 전담하는 '관찰자 패턴'을 적용합니다.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-BOSS-01 (Screen Space):** Screen Space (Overlay) 캔버스를 사용하며 화면 오른쪽에 고정됨.
- **D-BOSS-02 (Vertical Shrinking):** 세로형 수축 바 형식을 사용 (컨테이너 높이 = 오염도, 내부 게이지 = 정화율).
- **D-BOSS-03 (Boss Name):** 보스 이름을 표시하는 텍스트 UI 포함.
- **D-BOSS-04 (BossStats):** `EnemyStats` 상속 및 `bossName` 필드 추가.
- **D-BOSS-05 (UI Suppression):** 보스전 시 `EnemyWorldSpaceUI` 자동 비활성화.
- **D-BOSS-06 (Trigger):** `BossRoomTrigger`를 통한 UI 활성화.
- **D-BOSS-07 (Transition):** Fade In/Out 효과 적용.
- **D-BOSS-08 (Sweet Spot Feedback):** UI 바와 보스 본체의 이중 Glow 효과.

### the agent's Discretion
- 화면 왼쪽(플레이어), 중앙(전투), 오른쪽(보스) 레이아웃 분산을 통한 가독성 확보.
- 보스 바 수축에 따른 이름 텍스트 위치 연동 여부.

### Deferred Ideas (OUT OF SCOPE)
- 보스 페이즈 전환 연출 (색상 변경 등).
- 보스 처치 시네마틱 (슬로우 모션 등).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| BOSS-01 | BossStats 클래스 설계 | EnemyStats 상속 및 bossName 필드 확장 설계 완료 |
| BOSS-02 | 우측 고정 세로형 수축 바 UI | PlayerHUDBar의 스케일링 로직을 우측 정렬 레이아웃에 이식 |
| BOSS-03 | BossUIManager 제어 로직 | CanvasGroup 기반 페이드 및 이벤트 바인딩/해제 로직 설계 |
| BOSS-04 | 일반 적 UI(EnemyWorldSpaceUI) 억제 | Awake 시점의 BossStats 감지 및 비활성화 로직 설계 |
| BOSS-05 | Sweet Spot 이중 피드백 (Glow) | UI 펄스 로직과 Sprite HDR Color 동기화 기법 확보 |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity UI (UGUI) | Built-in | UI 배치 및 이벤트 시스템 | 가장 안정적이고 수정이 용이함 |
| TextMeshPro | 3.x | 고해상도 텍스트 표시 | 가독성 및 다양한 텍스트 효과 지원 |
| URP 2D | 17.x (Unity 6) | 2D 렌더링 및 포스트 프로세싱 | Bloom 효과를 통한 Glow 구현의 표준 |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|--------------|
| CanvasGroup | Built-in | UI Alpha 제어 | Fade In/Out 연출 시 필수 |
| Vertical Layout Group | Built-in | UI 요소 수직 정렬 | 이름과 바의 관계 설정 시 유용 |

**Installation:**
기존 프로젝트에 모두 포함되어 있으므로 추가 설치 불필요.

## Architecture Patterns

### Recommended Project Structure
```
Assets/
├── Enemy/
│   ├── BossStats.cs             # EnemyStats 상속 클래스
│   └── BossRoomTrigger.cs       # 플레이어 감지 트리거
├── UI/
│   ├── Boss/
│   │   ├── BossUIManager.cs     # UI 전체 관리 (Singleton)
│   │   ├── BossHUDBar.cs        # 실제 UI 업데이트 로직
│   │   └── BossUI_Prefab.prefab # 우측 고정 UI 프리팹
```

### Pattern 1: Observer Pattern (Event-based Binding)
**What:** `BossStats`가 상태를 변경하면 이벤트를 발생시키고, `BossHUDBar`가 이를 구독하여 UI를 갱신합니다.
**When to use:** 보스 체력/오염도가 실시간으로 변할 때 `Update()`에서 매번 체크하는 비용을 줄이기 위해 사용합니다.

### Pattern 2: Component Suppression (Self-Disabling)
**What:** `EnemyWorldSpaceUI`가 부모에게서 `BossStats`를 찾으면 스스로 `gameObject`를 끕니다.
**When to use:** 보스 객체에 일반 적 UI 로직이 붙어있더라도 런타임에 보스 UI만 남기기 위해 사용합니다.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| UI Fade | Custom Color Loop | CanvasGroup.alpha | 하위 모든 요소의 투명도를 일괄적으로 가장 효율적으로 제어함 |
| Sprite Glow | Custom Shader | URP Bloom + HDR Color | Unity 6의 표준 워크플로우이며 성능 최적화가 잘 되어 있음 |
| UI Layout | Manual Positioning | Anchors & Vertical Layout Group | 해상도 대응(Responsive UI)을 위해 필수적임 |

## Architecture Details

### 1. BossStats 설계
`EnemyStats`의 모든 이벤트를 그대로 활용합니다.
```csharp
public class BossStats : EnemyStats 
{
    [SerializeField] private string bossName = "Corrupted Guardian";
    public string BossName => bossName;

    // Sweet Spot 진입 여부 노출 (Glow 연동용)
    public bool IsInSweetSpot { get; private set; }

    // OnCorruptionChanged를 오버라이드하거나 구독하여 IsInSweetSpot 상태 업데이트
}
```

### 2. UI 프리팹 구조 (Right-Aligned)
- **Root (Canvas - Overlay)**
  - **Panel (Rect: Right/Middle, Width: 100, Height: 80%)**
    - **NameText (TMP)**: 상단 배치
    - **BarContainer (Vertical Layout Group, Pivot: 1.0, 1.0)**
      - **Background (Image)**
      - **ScalingContainer (RectTransform)**: `localScale.y`가 Corruption Ratio에 따라 조절됨.
        - **PurificationFill (Image)**: `fillAmount`가 정화율에 따라 조절됨.

### 3. BossUIManager 로직
- **`Show(BossStats boss)`**:
  1. UI 프리팹 활성화 (`gameObject.SetActive(true)`)
  2. `CanvasGroup.alpha`를 0에서 1로 페이드.
  3. `BossHUDBar.Bind(boss)` 호출하여 이벤트 연결.
- **`Hide()`**:
  1. `CanvasGroup.alpha` 페이드 아웃.
  2. 비활성화 및 `Unbind()`.

## Common Pitfalls

### Pitfall 1: Event Memory Leaks
**What goes wrong:** 보스가 죽거나 씬을 이동한 후에도 UI가 이벤트를 계속 구독하고 있어 참조가 남음.
**How to avoid:** `OnDeath` 발생 시 혹은 `Hide()` 호출 시 반드시 이벤트를 해제(`-=`)해야 함.

### Pitfall 2: Overlapping HUDs
**What goes wrong:** 보스 위에 일반 적 머리 위 UI가 같이 떠서 가독성을 해침.
**How to avoid:** `EnemyWorldSpaceUI`의 `Awake`에서 `BossStats` 존재 여부를 확인하여 조기 차단.

### Pitfall 3: Sweet Spot Sync
**What goes wrong:** 보스 본체의 Glow와 UI의 Glow 타이밍이 어긋남.
**How to avoid:** `BossStats`에서 계산된 `IsInSweetSpot` 플래그 하나를 두 오브젝트가 공유하여 참조하도록 함.

## Code Examples

### UI Suppression (EnemyWorldSpaceUI.cs)
```csharp
// Source: D-BOSS-05 Implementation
protected override void Awake()
{
    base.Awake();
    // 부모에 BossStats가 있다면 일반 월드 스페이스 UI는 필요 없음
    if (GetComponentInParent<BossStats>() != null)
    {
        gameObject.SetActive(false);
        return;
    }
}
```

### Double Glow Sync Logic
```csharp
// UI 및 Boss Sprite에서 공통으로 사용할 펄스 계산
float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
Color glowColor = Color.Lerp(originalColor, sweetSpotColor, pulse);

// UI에 적용 (Image Color)
fillImage.color = glowColor;

// 보스 본체에 적용 (SpriteRenderer Color - HDR)
// _EmissionColor 프로퍼티가 있는 셰이더 사용 시 intensity 조절 가능
spriteRenderer.color = glowColor * intensity; 
```

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| URP 2D | Graphics/Glow | ✓ | Unity 6 | Standard Sprite Shader (Glow 제한적) |
| TextMeshPro | UI Text | ✓ | 3.x | Legacy Text |
| CanvasGroup | Fade In/Out | ✓ | Built-in | Image Alpha Loop |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework (EditMode/PlayMode) |
| Quick run command | `Unity.exe -runTests -testPlatform PlayMode` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| REQ-03 | 보스 진입 트리거 시 UI 활성화 | PlayMode | N/A | ❌ Wave 0 |
| REQ-04 | 보스전 중 일반 UI 억제 | PlayMode | N/A | ❌ Wave 0 |
| REQ-05 | Sweet Spot 진입 시 Glow 발생 | PlayMode | N/A | ❌ Wave 0 |

## Sources

### Primary (HIGH confidence)
- `Assets/Enemy/EnemyStats.cs` - 상속 기반 검토
- `Assets/Enemy/EnemyWorldSpaceUI.cs` - 억제 로직 삽입 위치 확인
- Unity 6 Official Manual - URP 2D Bloom & HDR 워크플로우

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - 프로젝트 기반 확인 완료
- Architecture: HIGH - 기존 패턴(PlayerHUDBar)과의 호환성 확인
- Pitfalls: MEDIUM - 실제 씬 배치 시 레이아웃 겹침 가능성 존재

**Research date:** 2024-04-13
**Valid until:** 2024-05-13
