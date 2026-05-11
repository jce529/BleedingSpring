# Editor Guide: Boss UI (Phase 3)

이 가이드는 화면 우측에 고정되어 표시되는 보스 전용 Screen Space HUD(수축하는 오염도 바 및 동적 Sweet Spot 가이드)를 구성하는 방법을 설명합니다.

## Hierarchy Setup (UI Canvas)

씬에 존재하는 전용 UI Canvas (Screen Space - Overlay) 하위에 다음과 같은 구조를 생성합니다.

- [ ] **BossUI_Manager** (GameObject: `BossUIManager` 부착)
    - [ ] **BossHUD_Panel** (CanvasGroup 부착, Anchor: Middle Right, Pivot: (1, 0.5))
        - [ ] **BossName_Text** (TextMeshPro - Text: 보스 이름)
        - [ ] **Phase_Text** (TextMeshPro - Text: "PHASE 1")
        - [ ] **Shrinking_Container** (RectTransform: 오염도에 따라 세로로 수축하는 부모)
            - [ ] **Corruption_Tray** (Image: 배경색 #1E1E23CC)
            - [ ] **Purification_Fill** (Image: Type=Filled, Method=Vertical, Origin=Bottom, 색상 #40B8FF)
            - [ ] **SweetSpot_Guide** (Image: 반투명 가이드 영역 #F2D94044, Anchor=Bottom Stretch)
        - [ ] **HP_Bar_Classic** (선택 사항: 보스의 절대 HP를 보여주는 작은 바)

## Component Settings

### BossUIManager
- [ ] **Boss UI Canvas Group**: `BossHUD_Panel`의 `CanvasGroup` 연결.
- [ ] **Boss HUD Bar**: `BossHUD_Panel`에 부착된 `BossHUDBar` 컴포넌트 연결.
- [ ] **Fade Duration**: 0.5 (기본값).

### BossHUDBar (inherits from PlayerHUDBar)
- [ ] **Container Rect**: `Shrinking_Container` 연결.
- [ ] **Corruption Fill Image**: `Purification_Fill` 이미지 연결.
- [ ] **Boss Name Text**: `BossName_Text` 연결.
- [ ] **Phase Text**: `Phase_Text` 연결.
- [ ] **Sweet Spot Guide**: `SweetSpot_Guide`의 RectTransform 연결.

## Visual Tokens

- **Position**: 화면 오른쪽 끝에서 약 40px 안쪽에 배치.
- **Size**: 너비 약 80~100px, 기본 높이(Max Corruption 시) 약 600~800px.
- **Sweet Spot Guide**: 컨테이너 안에서 HP에 따라 위치와 크기가 동적으로 변하므로, 초기 앵커는 (0, 0, 1, 1)로 설정하고 코드가 제어하게 함.
- **Pulse Speed**: 8.0 (Sweet Spot 진입 시 맥동 속도).

## Integration Test Scenarios

1.  **진입 트리거**: `BossRoomTrigger`가 배치된 구역에 플레이어가 들어가면 우측 UI가 서서히 페이드 인 되는지 확인.
2.  **수축 및 정화**: 보스 공격 시 오염도에 따라 전체 바가 짧아지고(수축), 안쪽의 파란색 게이지가 차오르는지 확인.
3.  **동적 가이드**: 보스의 HP가 깎임에 따라 `SweetSpot_Guide`의 영역이 점점 좁아지며 중심부(0.5)로 수렴하는지 확인.
4.  **페이즈 갱신**: 보스의 페이즈가 전환될 때 "PHASE X" 텍스트가 즉시 업데이트되는지 확인.
5.  **처치 및 페이드**: 보스 사망 시 UI가 서서히 페이드 아웃되며 비활성화되는지 확인.

---
*Created for Phase 3: Boss UI Milestone*
