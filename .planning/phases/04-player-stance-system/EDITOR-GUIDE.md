# Editor Guide: Phase 04 — Player Stance System

## Hierarchy Setup

- [ ] **Player** GameObject에 `PlayerStanceManager` 컴포넌트 추가 (Add Component → Player Stance Manager)
- [ ] **Player** GameObject에 보조 태세용 스킬 컴포넌트 3개 추가 (각각 별도 인스턴스):
  - Add Component → `BasicAttackSkill` (두 번째 인스턴스 — "Sub"용)
  - Add Component → `WideSlashSkill` (두 번째 인스턴스 — "Sub"용)
  - Add Component → `ProjectileSkill` (두 번째 인스턴스 — "Sub"용)

완료 후 Player GameObject의 컴포넌트 목록은 다음과 같아야 합니다:

```
Player
  ├── PlayerController
  ├── PlayerStanceManager        ← 신규
  ├── BasicAttackSkill  (Main)   ← 기존
  ├── WideSlashSkill    (Main)   ← 기존
  ├── ProjectileSkill   (Main)   ← 기존
  ├── BasicAttackSkill  (Sub)    ← 신규
  ├── WideSlashSkill    (Sub)    ← 신규
  └── ProjectileSkill   (Sub)    ← 신규
```

---

## Inspector Settings

| Object | Component | Property | Value / Action |
|--------|-----------|----------|----------------|
| Player | PlayerStanceManager | Main Stance | `Water` (드롭다운) |
| Player | PlayerStanceManager | Sub Stance | `Water` (드롭다운) |
| Player | PlayerStanceManager | Main Skills [0] | 기존 `BasicAttackSkill (Main)` 드래그 |
| Player | PlayerStanceManager | Main Skills [1] | 기존 `WideSlashSkill (Main)` 드래그 |
| Player | PlayerStanceManager | Main Skills [2] | 기존 `ProjectileSkill (Main)` 드래그 |
| Player | PlayerStanceManager | Sub Skills [0] | 새로 추가한 `BasicAttackSkill (Sub)` 드래그 |
| Player | PlayerStanceManager | Sub Skills [1] | 새로 추가한 `WideSlashSkill (Sub)` 드래그 |
| Player | PlayerStanceManager | Sub Skills [2] | 새로 추가한 `ProjectileSkill (Sub)` 드래그 |
| Player | BasicAttackSkill **(Sub)** | Cost Multiplier | `0.5` |
| Player | BasicAttackSkill **(Sub)** | Effect Multiplier | `0.5` |
| Player | WideSlashSkill **(Sub)** | Cost Multiplier | `0.5` |
| Player | WideSlashSkill **(Sub)** | Effect Multiplier | `0.5` |
| Player | ProjectileSkill **(Sub)** | Cost Multiplier | `0.5` |
| Player | ProjectileSkill **(Sub)** | Effect Multiplier | `0.5` |
| Player | BasicAttackSkill **(Main)** | Cost Multiplier | `1.0` (기본값 유지) |
| Player | BasicAttackSkill **(Main)** | Effect Multiplier | `1.0` (기본값 유지) |
| Player | WideSlashSkill **(Main)** | Cost Multiplier | `1.0` (기본값 유지) |
| Player | WideSlashSkill **(Main)** | Effect Multiplier | `1.0` (기본값 유지) |
| Player | ProjectileSkill **(Main)** | Cost Multiplier | `1.0` (기본값 유지) |
| Player | ProjectileSkill **(Main)** | Effect Multiplier | `1.0` (기본값 유지) |

---

## Reference Connections

- [ ] **PlayerStanceManager > Main Skills 배열 크기**를 `3`으로 먼저 설정한 뒤 슬롯에 드래그합니다.
- [ ] **PlayerStanceManager > Sub Skills 배열 크기**를 `3`으로 먼저 설정한 뒤 슬롯에 드래그합니다.
- [ ] 같은 타입 컴포넌트가 2개 존재하므로, Inspector에서 드래그 시 **올바른 인스턴스**(Main/Sub)를 확인하세요.  
  Unity Inspector는 동일 타입 컴포넌트를 위에서부터 순서대로 표시합니다 — 먼저 추가된 것이 Main, 나중에 추가된 것이 Sub입니다.
- [ ] **보조 스킬 컴포넌트의 Enemy Layer 마스크**를 메인 스킬과 동일하게 설정합니다.  
  (BasicAttackSkill, WideSlashSkill, ProjectileSkill의 `Enemy Layer` 필드 → 기존 설정과 같은 레이어 선택)

---

## Manual Test (Play Mode)

- [ ] Play Mode 진입 후 Console에 `NullReferenceException` 없음 확인
- [ ] **메인 스킬 하위 호환 검증**: BasicAttack / WideSlash / Projectile 각각 사용 시 기존과 동일한 데미지와 HP 소모량 확인 (costMultiplier=1.0, effectMultiplier=1.0)
- [ ] **보조 스킬 소모량 절반 검증**: Debug.Log에서 보조 WideSlash / Projectile 사용 시 HP 소모량이 메인의 50%인지 확인 (`tsunamiHpCost * 0.5`, `sniperHpCost * 0.5`)
- [ ] **보조 스킬 데미지 절반 검증**: 보조 스킬 적중 시 Enemy HP가 메인 대비 50% 감소하는지 확인

---

*Phase: 04-player-stance-system*  
*Created by unity-editor-guide skill*
