# Editor Guide: Enemy World Space UI (Phase 2)

이 가이드는 적 머리 위에 표시되는 "수축하는 생존 컨테이너" 스타일의 HP/오염도 UI를 유니티 에디터에서 구성하는 방법을 설명합니다.

## Hierarchy Setup (Prefab Structure)

적(Enemy) 프리팹 하위에 다음과 같은 구조로 UI를 생성합니다.

- [ ] **Enemy_Root (EnemyStats 부착됨)**
    - [ ] **EnemyWorldSpaceUI_Canvas** (GameObject: `EnemyWorldSpaceUI` 및 `CanvasGroup` 부착)
        - [ ] **HP_Bar_Tray** (Image: 배경색 #1E1E23CC)
            - [ ] **HP_Fill** (Image: Type=Filled, Method=Horizontal, 색상 #40B859)
        - [ ] **Shrinking_Container** (RectTransform: 오염도에 따라 세로로 수축하는 부모)
            - [ ] **Corruption_Tray** (Image: 배경색 #1E1E23CC)
            - [ ] **Purification_Fill** (Image: Type=Filled, Method=Vertical, Origin=Bottom, 색상 #40B8FF)
            - [ ] **SweetSpot_Overlay** (RectTransform: 코드에서 앵커가 자동 제어되는 가이드 영역)

## Inspector Settings

| Object | Component | Property | Value / Action |
|--------|-----------|----------|----------------|
| **EnemyWorldSpaceUI_Canvas** | Canvas | Render Mode | **World Space** |
| | Canvas | Sorting Layer | **Default** (Order: 10) |
| | RectTransform | Scale | (0.01, 0.01, 0.01) 추천 (픽셀 단위 작업 시) |
| | EnemyWorldSpaceUI | Fade In Duration | 0.15 |
| **HP_Fill** | Image | Image Type | **Filled** |
| | Image | Fill Method | **Horizontal** |
| **Purification_Fill** | Image | Image Type | **Filled** |
| | Image | Fill Method | **Vertical** (Origin: **Bottom**) |
| **SweetSpot_Overlay** | RectTransform | Anchors | Min(0, 0.3), Max(1, 0.7) - 세로형 기준 기본값 |
| | RectTransform | Offsets | Min(0, 0), Max(0, 0) - 코드에서 자동 제어됨 |

## Reference Connections

`EnemyWorldSpaceUI` 컴포넌트의 인스펙터에서 다음 항목들을 연결하세요.

- [ ] **Container Rect**: `Shrinking_Container` 연결 (세로 스케일링 대상).
- [ ] **HP Fill Image**: `HP_Fill` 이미지 연결 (체력 표시).
- [ ] **Corruption Fill Image**: `Purification_Fill` 이미지 연결 (정화 진행도 표시).
- [ ] **Canvas Group**: 자기 자신의 `CanvasGroup` 컴포넌트 연결.

## Visual Tokens (UI-SPEC 준수)

- **HP Bar**: 상단에 고정된 얇은 가로 바. (너비 1.2, 높이 0.1)
- **Corruption Bar (Container)**: 하단에 위치하며 오염도에 따라 세로 길이가 변함. (너비 1.2, 기본 높이 0.5)
- **Purification Fill**: 컨테이너 안에서 아래서부터 차오르는 파란색 게이지.
- **Offset**: 적 머리 위로부터 약 0.2 units 띄워서 배치.

## Manual Test (Play Mode)

- [ ] **최초 숨김**: 게임 시작 시 UI가 보이지 않아야 함.
- [ ] **피격 시 표시**: 첫 타격 시 0.15초간 페이드 인.
- [ ] **수축 로직**: 적의 오염도가 낮아질수록 `Shrinking_Container`의 세로 길이가 줄어드는지 확인.
- [ ] **정화 게이지**: 컨테이너 내에서 파란색 게이지가 상대적으로 차오르는지 확인.
- [ ] **HP 색상**: HP가 낮아짐에 따라 상단 HP 바가 초록(#40B859)에서 빨강(#D93838)으로 변하는지 확인.
- [ ] **Sweet Spot**: 정화 범위 진입 시 Flash 효과 및 Pulse(맥동) 효과 확인.

---
*Updated for Shrinking Container Logic*
