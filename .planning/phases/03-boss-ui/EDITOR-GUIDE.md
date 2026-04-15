# Editor Guide: Phase 03 Boss UI

이 가이드는 보스 전용 HUD UI 프리팹을 구성하고 씬에 배치하여 `BossStats` 및 `BossRoomTrigger`와 연결하는 절차를 설명합니다.

## Hierarchy Setup (Canvas & Prefab)

1.  **Canvas 생성**: 씬에 새로운 Canvas를 생성하거나 기존 UI Canvas를 사용합니다.
    -   `Render Mode`: `Screen Space - Overlay` 권장.
2.  **Boss UI Panel 생성**: Canvas 하위에 `BossUI_Panel` 오브젝트를 생성합니다.
    -   `RectTransform`: 우측 상단 또는 우측 중앙 정렬 (Anchor: `Middle Right`, Pivot: `1, 0.5`).
    -   `CanvasGroup`: 컴포넌트 추가 (Fade In/Out 및 억제 로직에 사용됨).
3.  **보스 이름 텍스트 생성**: `BossUI_Panel` 하위에 `Name_Text (TMP)`를 생성합니다.
    -   보스 이름이 표시될 위치를 잡습니다.
4.  **바 컨테이너 생성**: `BossUI_Panel` 하위에 `Bar_Container` (Image)를 생성합니다.
    -   `Image`: 배경색(어두운 투명색)을 설정합니다.
    -   `RectTransform`: 세로로 긴 직사각형 형태. Pivot을 `(0.5, 0)`으로 설정하여 아래에서 위로 수축하도록 합니다 (D-BOSS-04).
5.  **정화 게이지 생성**: `Bar_Container` 하위에 `Purification_Fill` (Image)를 생성합니다.
    -   `Image Type`: `Filled`
    -   `Fill Method`: `Vertical`
    -   `Fill Origin`: `Bottom`
    -   `Color`: 정화된 깨끗한 물의 색상(예: 연한 파란색 또는 에메랄드색).

## Inspector Settings

| Object | Component | Property | Value / Action |
|--------|-----------|----------|----------------|
| **BossUI_Panel** | `BossHUDBar` | `Warning Color` | Gold/Yellow (Sweet Spot 강조색) |
| | | `Pulse Speed` | 5.0 (부드러운 맥동 효과) |
| | `BossUIManager` | `Fade Duration` | 0.5 |
| **Bar_Container**| `Image` | `Color` | Dark Gray/Black with Alpha (배경) |
| **Purification_Fill** | `Image` | `Color` | #40B859 (정화 완료 색상) |
| **Dummy Boss** | `BossStats` | `Boss Name` | "Corrupted Elder" (테스트용 이름) |
| | | `Max Corruption` | 100.0 |
| | | `Purification Range` | X: 30, Y: 70 (Sweet Spot 범위) |

## Reference Connections

### 1. BossHUDBar (BossUI_Panel에 위치)
-   `Boss Name Text`: `Name_Text (TMP)` 오브젝트 드래그 앤 드롭.
-   `Canvas Group`: `BossUI_Panel` (자기 자신)의 `CanvasGroup` 드래그 앤 드롭.
-   `Container Rect`: `Bar_Container`의 `RectTransform` 드래그 앤 드롭.
-   `Corruption Fill Image`: `Purification_Fill`의 `Image` 드래그 앤 드롭.

### 2. BossUIManager (씬의 Manager 오브젝트 또는 BossUI_Panel에 위치)
-   `Boss UI Canvas Group`: `BossUI_Panel`의 `CanvasGroup` 할당.
-   `Boss HUD Bar`: `BossUI_Panel`의 `BossHUDBar` 할당.

### 3. BossRoomTrigger (보스 방 입구 트리거 오브젝트)
-   `Collider2D`: `Is Trigger` 체크.
-   `Boss Stats`: 테스트용 보스 오브젝트의 `BossStats` 할당.

## Visual Tokens & URP Bloom
-   **Glow 효과 (Sweet Spot)**:
    -   `Purification_Fill` 이미지의 Material에 `Emission`을 활성화하거나, 쉐이더에서 HDR 색상을 지원하도록 설정합니다.
    -   전역 `Volume` (Global Volume)에서 `Bloom` 효과를 활성화하여 `Warning Color`가 맥동할 때 빛나는 효과가 나도록 조정합니다 (Intensity 1.0 이상).

## Manual Test (Play Mode / UAT)

1.  **진입 테스트**: 플레이어가 `BossRoomTrigger`에 닿았을 때 우측 UI 패널이 서서히 나타나는지 확인합니다.
2.  **체력 수축 테스트**: 보스에게 데미지를 주어 `Current Corruption`이 줄어들 때, `Bar_Container`의 전체 높이가 낮아지는지 확인합니다.
3.  **정화 게이지 테스트**: 남은 컨테이너 높이 대비 `Purification_Fill`이 차오르는 비율이 `(Max - Current) / Current`로 계산되어 시각적으로 상단부터 차오르는지 확인합니다.
4.  **Sweet Spot 테스트**: 보스의 체력이 `Purification Range` (예: 30~70) 내에 있을 때 바의 색상이 `Warning Color`로 부드럽게 깜빡이는지 확인합니다.
5.  **처치 테스트**: 보스의 체력이 0이 되었을 때 UI가 사라지는지 확인합니다.
6.  **억제 테스트**: 보스 전투 중 보스의 머리 위에 일반 적용 `EnemyWorldSpaceUI`가 나타나지 않는지 확인합니다 (Alpha 0 고정 여부).

---
*Created by unity-editor-guide skill*
