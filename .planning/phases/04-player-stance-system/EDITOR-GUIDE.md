# Editor Guide: Player Stance System (Phase 4)

이 가이드는 플레이어의 메인 태세와 보조 태세를 설정하고, 각 태세별 스킬 슬롯에 컴포넌트를 할당하는 방법을 설명합니다.

## Hierarchy Setup (Player Prefab)

플레이어(Player) 프리팹 또는 오브젝트에 다음과 같은 컴포넌트들을 추가합니다.

1.  **PlayerStanceManager**: 태세 전환 및 스킬 슬롯 관리를 담당합니다.
2.  **스킬 컴포넌트 (메인용)**:
    *   `BasicAttackSkill`: 기본 공격 (슬롯 0)
    *   `WaterStanceMain`: 참격 스킬 (슬롯 1)
    *   `WaterStanceSub`: 투사체 스킬 (슬롯 2)
3.  **스킬 컴포넌트 (보조용)**:
    *   `WaterStanceMain` (별도 인스턴스): 보조 태세용 참격
    *   `WaterStanceSub` (별도 인스턴스): 보조 태세용 투사체

> **주의**: 유니티 에디터에서 같은 타입의 컴포넌트가 여러 개 있을 경우, `PlayerStanceManager`의 리스트에 드래그 앤 드롭할 때 올바른 인스턴스를 선택해야 합니다.

## Inspector Settings

### 1. PlayerStanceManager 설정
- **Stances (List)**: 태세 정의를 추가합니다.
    - **Stance 0 (Water - Main)**:
        - **Stance Type**: Water
        - **Main Skill**: 메인용 `WaterStanceMain` 컴포넌트 드래그
        - **Sub Skill**: 메인용 `WaterStanceSub` 컴포넌트 드래그
    - **Stance 1 (Water - Sub)**:
        - **Stance Type**: Water
        - **Main Skill**: 보조용 `WaterStanceMain` 컴포넌트 드래그
        - **Sub Skill**: 보조용 `WaterStanceSub` 컴포넌트 드래그

### 2. 보조 태세용 스킬 배율 설정
보조 태세로 할당된 스킬 컴포넌트들의 인스펙터에서 다음 값을 수정합니다.

| Component | Property | Value |
|-----------|----------|-------|
| **WaterStanceMain (Sub Instance)** | Cost Multiplier | **0.5** |
| | Effect Multiplier | **0.5** |
| **WaterStanceSub (Sub Instance)** | Cost Multiplier | **0.5** |
| | Effect Multiplier | **0.5** |

*메인 태세용 스킬들은 기본값인 1.0을 유지합니다.*

## Component Connections (PlayerController)

- `PlayerController`의 `StanceManager` 필드가 자동으로 할당되는지 확인합니다. (Awake에서 `GetComponent<PlayerStanceManager>()` 호출됨)
- `InputHandler`의 스킬 입력 이벤트(`OnWideSlash`, `OnProjectile`)가 `PlayerController`를 통해 `StanceManager`의 현재 활성 스킬을 호출하는지 확인합니다.

## Manual Test (Play Mode)

1.  **태세 전환 테스트**: `ToggleStance()` 호출 시 (또는 현재 입력 설정에 따라) 메인/보조 태세가 정상적으로 스위칭되는지 확인.
2.  **메인 스킬 검증**: 메인 태세 상태에서 스킬 사용 시 정상 데미지 및 HP 소모량 확인.
3.  **보조 스킬 검증**: 보조 태세 상태에서 스킬 사용 시 **데미지와 HP 소모량이 메인의 절반(0.5배)** 인지 확인 (Debug.Log 및 HP 바 확인).
4.  **HP 부족 체크**: 보조 태세일 때 해일참(3단계) 사용에 필요한 HP가 절반(예: 40 -> 20)만 있어도 발동되는지 확인.

---
*Created for Phase 4: Player Stance System Milestone*
