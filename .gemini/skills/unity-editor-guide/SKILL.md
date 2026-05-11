---
name: unity-editor-guide
description: Generates a structured EDITOR-GUIDE.md file within a phase directory to document required Unity Editor manual tasks (e.g., prefab setup, component assignment, UI layout). Use this whenever a task involves manual steps in the Unity Editor that cannot be automated via code.
---

# Unity Editor Guide

이 스킬은 Unity 에디터에서 수동으로 작업해야 하는 항목(프리팹 구성, 컴포넌트 할당, 캔버스 설정 등)을 표준화된 양식으로 문서화합니다.

## Workflow

Unity 에디터 작업이 필요한 Directive를 받았을 때, 다음 단계를 따릅니다.

1.  **Context 분석**: 현재 Phase의 `.planning/phases/XX-NAME/` 폴더 내의 `UI-SPEC.md` 및 `PLAN.md`를 읽고 에디터 작업을 파악합니다.
2.  **가이드 생성**: 해당 Phase 폴더 안에 `EDITOR-GUIDE.md` 파일을 생성합니다.
3.  **내용 포함**: 다음 섹션들을 반드시 포함합니다.

### 1. Hierarchy & Prefab Structure
- 새로 생성하거나 수정해야 할 게임 오브젝트의 계층 구조를 명시합니다.
- 예: `Canvas (World Space) > HP_Bar_Container > Fill_Image`

### 2. Inspector Configuration
- 각 오브젝트에 부착해야 할 컴포넌트와 설정값을 명시합니다.
- 특히 **레퍼런스 연결**(어떤 필드에 어떤 오브젝트를 드래그 앤 드롭해야 하는지)을 강조합니다.
- 예: `EnemyWorldSpaceUI` 컴포넌트의 `Hp Fill Image` 필드에 `Fill_Image`를 할당.

### 3. Visual & Transform Tokens
- `UI-SPEC.md`에 정의된 Spacing, Color, Typography 토큰을 에디터에 적용하는 방법을 설명합니다.
- 예: RectTransform의 `Pos Y`를 `0.16`으로 설정, 색상을 `#40B859`로 설정.

### 4. Play Mode Validation
- 작업 완료 후 에디터 Play Mode에서 확인해야 할 수동 테스트 항목(UAT 연계)을 나열합니다.

## Output Format

`EDITOR-GUIDE.md` 파일은 항상 다음 형식을 유지합니다.

```markdown
# Editor Guide: [Phase Name]

## Hierarchy Setup
- [ ] Create ...
- [ ] Set Parent to ...

## Inspector Settings
| Object | Component | Property | Value / Action |
|--------|-----------|----------|----------------|
| ...    | ...       | ...      | ...            |

## Reference Connections
- [ ] Drag [Source] to [Target] field.

## Manual Test (Play Mode)
- [ ] Check if ...
```

---
*Created by unity-editor-guide skill*
