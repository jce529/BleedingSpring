# Phase 2: Enemy World Space UI — Research

**Researched:** 2026-04-06
**Domain:** Unity uGUI World Space Canvas, C# event patterns, 2D enemy UI
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** 적이 첫 번째 데미지를 받는 순간(`OnDamaged` 이벤트) 바가 나타난다. 데미지를 받기 전에는 바가 표시되지 않는다.
- **D-02:** 적이 죽을 때(`OnDeath` 이벤트) 바가 사라진다. 자동 숨김 타이머 없음 — 한 번 표시되면 죽을 때까지 유지된다.
- **D-03:** 오염도 바 위에 반투명 오버레이(별도 Image 컴포넌트)를 사용해 Sweet Spot 구간을 표시한다.
- **D-04:** 오버레이의 위치와 크기는 RectTransform anchors로 계산한다: `anchorMin.x = basePurificationMin`, `anchorMax.x = basePurificationMax` (bonus margin 포함).
- **D-05:** 오염도 바의 방향: 왼쪽 = 0% 오염(완전 정화), 오른쪽 = 100% 오염(완전 오염). Sweet Spot은 중간 구간에 위치.
- **D-06:** 적 타입마다 `EnemyStats.basePurificationMin/Max` 값이 다르므로, Bind 시 각 적 인스턴스의 값을 읽어 오버레이 위치를 동적으로 계산한다.
- **D-07:** "PURIFIED" / "DESTROYED" 텍스트 팝업은 구현하지 않는다 (ENM-06 스킵).
- **D-08:** 바는 작고 요약적 — 두께 얇음, 너비는 적 스프라이트 폭 정도.
- **D-09:** 배치 순서: HP 바(위), 오염도 바(아래), 적 머리 위 고정 오프셋으로 위치.
- **D-10:** World Space Canvas를 적의 자식 오브젝트로 배치 (Phase 1 플레이어 HUD와 동일한 패턴).
- **D-11:** `EnemyStats`에 `OnHpChanged(float current, float max)` 이벤트를 추가한다.
- **D-12:** 모든 적 UI 바 스크립트는 `Bind(EnemyStats stats)` / `Unbind()` 패턴을 구현한다.

### Claude's Discretion

- 정확한 HP 바 두께와 오염도 바 높이 비율 (작고 읽기 쉬운 범위 내)
- Sweet Spot 오버레이 색상 (오염도 바와 구분되는 색)
- 바가 나타날 때 페이드인 여부 (있다면 0.1~0.2초 짧게)
- World Space Canvas의 Sorting Layer 설정

### Deferred Ideas (OUT OF SCOPE)

- **ENM-06 (PURIFIED/DESTROYED 텍스트):** 사용자 판단으로 이 Phase에서 스킵. 필요 시 v2 폴리시 패스에서 추가.
- **Ghost bar (트레일 효과):** REQUIREMENTS v2 POL-04 — HP 감소 지연 트레일은 이 Phase 범위 밖.
- **보스 위 월드 스페이스 바 억제:** Phase 3 범위 (BOSS-04).
- **bonusPurificationMargin 런타임 업데이트:** 현재는 Bind 시 1회 계산으로 충분.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ENM-01 | 적 머리 위 월드 스페이스 캔버스에 HP 바가 표시되고 적 이동에 따라 추종한다 | World Space Canvas as child of enemy GameObject — inherits transform automatically |
| ENM-02 | 적 HP 바는 적이 데미지를 받는 순간 표시되고, 적이 죽으면 사라진다 | OnDamaged → CanvasGroup fade-in 0.15s; OnDeath → SetActive(false) |
| ENM-03 | 적 HP 바 아래에 오염도 바가 함께 표시되고 OnCorruptionChanged 이벤트에 반응한다 | Requires adding OnCorruptionChanged to EnemyStats (same pattern as OnHpChanged) |
| ENM-04 | 오염도 바 위에 Sweet Spot 유효 범위가 색상이 다른 구간으로 강조 표시된다 | RectTransform anchorMin.x/anchorMax.x = purification min/max ratios |
| ENM-05 | Sweet Spot 구간 표시는 적 타입별로 다른 범위를 올바르게 반영한다 | Read basePurificationMin/Max per-enemy-instance at Bind() time |
| ENM-06 | (DEFERRED) 처치 시 PURIFIED/DESTROYED 텍스트 팝업 — D-07로 스킵 | N/A — out of scope for this phase |
| TECH-02 | EnemyStats에 OnHpChanged(float current, float max) 이벤트 추가 | Add event + fire in TakeDamage() and SpendHpOnAttack() |
| TECH-03 | 모든 UI 바 스크립트에 Bind(stats)/Unbind() 패턴 구현 | Prevents event leaks on Purify path where OnDestroy may not be called |
</phase_requirements>

---

## Summary

Phase 2 extends the existing World Space Canvas pattern established in Phase 1 onto enemy GameObjects. The core mechanic is: a World Space Canvas is added as a child of each enemy prefab, starts hidden, and becomes visible on the first `OnDamaged` event. All data flows through C# `Action` events subscribed via an explicit `Bind()`/`Unbind()` pattern to handle the purification path (where `OnDestroy` is not guaranteed to fire).

The primary new work is: (1) adding two events to `EnemyStats` (`OnHpChanged` and `OnCorruptionChanged`), (2) creating `EnemyWorldSpaceUI.cs` (or split scripts) that implement the Bind/Unbind contract, and (3) setting up the prefab hierarchy with the Canvas, HP bar Image, Corruption bar Image, and Sweet Spot overlay Image. The Sweet Spot overlay is positioned purely via `RectTransform` anchor values read from `EnemyStats.basePurificationMin/Max` — no runtime math beyond a one-time calculation at `Bind()` time.

ENM-06 (PURIFIED/DESTROYED popup) is explicitly deferred and must NOT be implemented. The only death response is `Canvas.SetActive(false)` immediately on `OnDeath`.

**Primary recommendation:** Implement as a single `EnemyWorldSpaceUI.cs` that owns all three bar responsibilities (HP fill, Corruption fill, Sweet Spot overlay). This is simpler than three separate scripts and the UI-SPEC approves either approach. Each bar sub-element is a serialized reference. Follow the Phase 1 pattern precisely.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| UnityEngine.UI (uGUI) | Bundled with Unity 6 | Image fill, CanvasGroup, RectTransform | Already used in Phase 1 PlayerHUDBar — no new packages |
| TextMeshPro | Bundled with Unity 6 | (Deferred — no text in this phase) | Available if needed in v2 |
| System (C#) | Runtime | `Action<float, float>` event delegates | Used in existing EnemyStats events |
| UnityEngine | Unity 6 | MonoBehaviour, Coroutine, Color.Lerp | Core Unity |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| CanvasGroup | Bundled | Fade-in alpha control for entire Canvas | Use for show-on-first-hit 0.15s fade-in |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Single EnemyWorldSpaceUI.cs | Three separate scripts (EnemyHPBar.cs, EnemyCorruptionBar.cs, EnemySweetSpotOverlay.cs) | Simpler Bind/Unbind coordination in one script vs. three separate subscriptions; UI-SPEC approves either |
| RectTransform anchors for Sweet Spot | Custom shader or gradient texture | Anchors are simpler, no shader authoring, sufficient for v1 |
| CanvasGroup.alpha fade-in | Animator or DOTween | No extra dependency needed; Coroutine + `Mathf.Lerp` handles 0.15s easily |

**Installation:** No new packages required. All dependencies are bundled with Unity 6.

---

## Architecture Patterns

### Recommended Project Structure

```
Assets/
├── Enemy/
│   ├── EnemyStats.cs             # ADD: OnHpChanged, OnCorruptionChanged events
│   ├── EnemyWorldSpaceUI.cs      # NEW: Bind/Unbind, HP fill, Corruption fill, Sweet Spot
│   ├── EnemyAI.cs                # Unchanged
│   ├── EnemyAttack.cs            # Unchanged
│   ├── EnemyState.cs             # Unchanged
│   └── PurifiedNPC.cs            # Unchanged (but Unbind timing is a concern here)
├── Prefabs/
│   └── Enemy.prefab              # ADD: WorldSpaceUI child hierarchy (or configure in scene)
```

### Pattern 1: World Space Canvas as Enemy Child

**What:** Canvas (Render Mode = World Space) is a direct child of the enemy's root GameObject. No follow script needed — parenting handles position tracking automatically.

**When to use:** Always for enemy overhead bars in 2D. Phase 1 used this same pattern for the player HUD.

**Critical Unity settings:**
- Canvas: Render Mode = World Space
- Canvas Scaler: not needed for World Space (units are scene units, not pixels)
- Sorting Layer: "Default", Order in Layer: 10 (above sprites, below Screen Space Overlay)
- CanvasGroup component: for fade-in control (alpha 0 → 1 over 0.15s)

**Hierarchy:**
```
Enemy (GameObject)
├── SpriteRenderer
├── EnemyStats.cs
├── EnemyAI.cs
├── EnemyAttack.cs
└── EnemyWorldSpaceUI (GameObject)          ← local position: (0, <head_offset>, 0)
    ├── Canvas (World Space)
    ├── CanvasGroup                          ← alpha starts at 0
    ├── HP Bar (GameObject)
    │   ├── Background Image                ← fill: none, color #1E1E23CC
    │   └── HP Fill Image                   ← fill: Horizontal, Origin Left, color #40B859
    └── Corruption Bar (GameObject)
        ├── Background Image                ← fill: none, color #1E1E23CC
        ├── Corruption Fill Image           ← fill: Horizontal, Origin Left, color #8C38BF
        └── Sweet Spot Overlay Image        ← anchor-positioned, color #F2D940 alpha 0.55
```

### Pattern 2: Bind/Unbind Event Subscription

**What:** Explicit `Bind(EnemyStats)` / `Unbind()` methods replace `Start()`/`OnDestroy()` for event wiring. This is REQUIRED because `PurifiedNPC.Activate()` sets `EnemyStats.enabled = false` without destroying the GameObject, so `OnDestroy` is never called on the UI script.

**When to use:** Always when subscribing to events on a component that may be disabled (not destroyed) before the subscriber is destroyed.

**Implementation:**

```csharp
// Source: 02-UI-SPEC.md TECH-03, PlayerHUDBar.cs pattern (Phase 1)
public class EnemyWorldSpaceUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image corruptionFill;
    [SerializeField] private RectTransform sweetSpotOverlay;

    private EnemyStats _stats;
    private bool _revealed;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        // Auto-bind: the UI is always a child of the enemy that owns EnemyStats
        var stats = GetComponentInParent<EnemyStats>();
        if (stats != null) Bind(stats);
    }

    public void Bind(EnemyStats stats)
    {
        if (_stats != null) Unbind();
        _stats = stats;
        _stats.OnHpChanged         += HandleHpChanged;
        _stats.OnCorruptionChanged += HandleCorruptionChanged;
        _stats.OnDamaged           += HandleFirstDamage;
        _stats.OnDeath             += HandleDeath;

        // Initialize fills from current values
        HandleHpChanged(_stats.CurrentHp, _stats.MaxHp);
        HandleCorruptionChanged(_stats.CurrentCorruption, _stats.MaxCorruption);
        SetSweetSpotOverlay(_stats);

        // Start hidden
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        _revealed = false;
    }

    public void Unbind()
    {
        if (_stats == null) return;
        _stats.OnHpChanged         -= HandleHpChanged;
        _stats.OnCorruptionChanged -= HandleCorruptionChanged;
        _stats.OnDamaged           -= HandleFirstDamage;
        _stats.OnDeath             -= HandleDeath;
        _stats = null;
    }

    private void OnDestroy() => Unbind();
}
```

### Pattern 3: Sweet Spot Overlay via RectTransform Anchors

**What:** The Sweet Spot overlay Image's `anchorMin.x` and `anchorMax.x` are set at `Bind()` time to the effective purification range. Since the range is 0.0–1.0 and the corruption bar background spans the full width, anchors map directly to position without any pixel math.

**When to use:** Any time you need to mark a sub-range on a horizontal bar without a shader.

**Implementation:**

```csharp
// Source: CONTEXT.md D-03, D-04, D-06; UI-SPEC.md Interaction & Animation Contract
private void SetSweetSpotOverlay(EnemyStats stats)
{
    if (sweetSpotOverlay == null) return;

    float effectiveMin = stats.basePurificationMin - stats.bonusPurificationMargin;
    float effectiveMax = stats.basePurificationMax + stats.bonusPurificationMargin;

    // Clamp to valid range
    effectiveMin = Mathf.Clamp01(effectiveMin);
    effectiveMax = Mathf.Clamp01(effectiveMax);

    sweetSpotOverlay.anchorMin = new Vector2(effectiveMin, 0f);
    sweetSpotOverlay.anchorMax = new Vector2(effectiveMax, 1f);
    sweetSpotOverlay.offsetMin = Vector2.zero;
    sweetSpotOverlay.offsetMax = Vector2.zero;
}
```

**Critical:** After setting anchors, also zero out `offsetMin` and `offsetMax` (the pixel-based offsets). Otherwise Unity's retained pixel offsets will shift the overlay away from the anchor-defined position.

### Pattern 4: First-Hit Reveal with CanvasGroup Fade-In

**What:** `OnDamaged` is the trigger for revealing the canvas. A Coroutine lerps `CanvasGroup.alpha` from 0 to 1 over 0.15 seconds. Subsequent `OnDamaged` calls are ignored if already revealed.

**Implementation:**

```csharp
// Source: UI-SPEC.md Interaction & Animation Contract (0.15s linear fade-in)
private void HandleFirstDamage()
{
    if (_revealed) return;
    _revealed = true;
    if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
    _fadeCoroutine = StartCoroutine(FadeIn(0.15f));
}

private IEnumerator FadeIn(float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
        yield return null;
    }
    if (canvasGroup != null) canvasGroup.alpha = 1f;
}

private void HandleDeath()
{
    if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
    gameObject.SetActive(false);  // Immediate — D-02
}
```

### Pattern 5: HP Color Lerp (Healthy → Low)

**What:** HP fill color transitions from healthy green to danger red as HP approaches 0. The threshold is 50%.

**Implementation:**

```csharp
// Source: UI-SPEC.md Color State Rules
private void HandleHpChanged(float current, float max)
{
    if (hpFill == null) return;
    float ratio = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
    hpFill.fillAmount = ratio;

    // Color: lerp from danger red to healthy green based on ratio
    // At ratio=1.0 → healthy (#40B859), at ratio=0.0 → danger (#D93838)
    // Transition starts at 0.5
    Color healthyColor = new Color(0.25f, 0.72f, 0.35f, 1f);
    Color dangerColor  = new Color(0.85f, 0.22f, 0.22f, 1f);
    float colorT = (ratio <= 0.5f) ? (ratio / 0.5f) : 1f;
    hpFill.color = Color.Lerp(dangerColor, healthyColor, colorT);
}
```

### Anti-Patterns to Avoid

- **Subscribe in `Start()` only, unsubscribe in `OnDestroy()` only:** This breaks the Purify path. `EnemyStats.enabled = false` does not trigger `OnDestroy` on sibling/child scripts. Always use explicit `Bind()`/`Unbind()`.
- **Using `GetComponentInChildren<>` from `EnemyStats` to find the UI:** Couples data to presentation. Instead the UI finds `EnemyStats` via `GetComponentInParent<>` in `Awake()`.
- **Using `Animator` for the fade-in:** Over-engineered; a one-shot Coroutine is sufficient for a 0.15s alpha tween.
- **Forgetting `offsetMin = Vector2.zero; offsetMax = Vector2.zero` after setting anchors:** The overlay will be offset by residual pixel values retained from the Inspector.
- **Setting Canvas to Screen Space - Overlay:** Bars would not follow the enemy. Must be World Space.
- **Stacking Order in Layer = 0:** The canvas will render behind sprites. Must be Order in Layer ≥ 10.
- **Not clamping Sweet Spot anchor values to 0–1:** If `bonusPurificationMargin` is large, effective values may exceed 0–1, causing the overlay to extend outside the bar.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Alpha fade-in tween | Custom tween manager | Simple Coroutine + `Mathf.Lerp` over 0.15s | No dependency; sufficient for single-shot 0.15s fade |
| Bar fill rendering | Custom mesh or shader | `Image.fillAmount` (Fill Method: Horizontal, Origin: Left) | uGUI built-in, no shader required |
| Enemy position tracking | `LateUpdate` position copy script | Parent-child relationship (Canvas is enemy child) | Automatic, zero frame lag, handles scale flip |
| Sweet Spot pixel positioning | Runtime pixel calculation | `RectTransform.anchorMin/Max` | Anchors ARE the percentage — direct mapping from 0–1 ratio |
| Event safety on Purify | Try/catch or null-checks everywhere | Explicit `Unbind()` called before `EnemyStats.enabled = false` | The `Purify()` method fires `OnDeath` then sets `enabled = false` — subscribing to `OnDeath` and calling `Unbind()` in the handler is the canonical solution |

**Key insight:** The Purify path is the critical edge case. `EnemyStats.Purify()` fires `OnDeath?.Invoke()` on line 154, then sets `enabled = false` on line 155. If `HandleDeath()` (subscribed to `OnDeath`) calls `Unbind()` before returning, event cleanup is guaranteed — the `enabled = false` line that follows cannot cause leaks because `Unbind()` has already cleared all subscriptions.

---

## Critical Code Analysis: EnemyStats (Current State)

Reading `Assets/Enemy/EnemyStats.cs` reveals the following facts that directly affect the plan:

### Events That Exist Today

```csharp
public event Action OnDamaged;   // fired in TakeDamage() — line 83
public event Action OnDeath;     // fired in Purify() (line 154) and Die() (line 170)
```

### Events That Must Be Added (TECH-02)

```csharp
// These do NOT exist yet — must be added in the first task wave
public event Action<float, float> OnHpChanged;         // (current, max)
public event Action<float, float> OnCorruptionChanged; // (current, max)
```

### Fire Points for OnHpChanged

Must be fired in two locations:
1. `TakeDamage()` after `CurrentHp -= hpDamage` (line 75–76)
2. `SpendHpOnAttack()` after `CurrentHp -= cost` (line 99)

### Fire Points for OnCorruptionChanged

Must be fired in one location:
1. `TakeDamage()` after `CurrentCorruption -= corruptionDamage` (line 76)

### Purify Path — Exact Sequence

```
EnemyStats.CheckDeathState()
  → EnemyStats.Purify()
      → OnDeath?.Invoke()          ← UI HandleDeath() fires here, calls Unbind(), SetActive(false)
      → enabled = false            ← EnemyStats stops, but UI is already cleaned up
      → PurifiedNPC.Activate()     ← No OnDestroy on UI script from this call
```

`HandleDeath()` MUST call `Unbind()` before calling `gameObject.SetActive(false)`. The `SetActive(false)` on the UI object means no further coroutines run, which is correct — the bar should disappear. `Unbind()` clears subscriptions. This ordering is safe.

### Die Path — Exact Sequence

```
EnemyStats.Die()
  → OnDeath?.Invoke()          ← UI HandleDeath() fires, Unbind() + SetActive(false)
  → Destroy(gameObject)        ← destroys the enemy; child UI is also destroyed
                                  OnDestroy() will fire on the UI script here,
                                  but Unbind() already ran so _stats == null → safe no-op
```

---

## Common Pitfalls

### Pitfall 1: Sweet Spot Overlay Misaligned After Anchor Set

**What goes wrong:** The overlay appears at a wrong position or wrong size even though `anchorMin/Max` are set correctly.

**Why it happens:** Unity RectTransform retains `offsetMin`/`offsetMax` (pixel offsets from anchors). When you set anchors without zeroing offsets, the offsets shift the element away from the anchor boundaries.

**How to avoid:** Always follow anchor assignment with:
```csharp
sweetSpotOverlay.offsetMin = Vector2.zero;
sweetSpotOverlay.offsetMax = Vector2.zero;
```

**Warning signs:** Overlay appears narrower or wider than expected; one edge doesn't align with the bar edge.

### Pitfall 2: Canvas Renders Behind Sprites

**What goes wrong:** The HP/Corruption bars are invisible or occluded by the enemy sprite.

**Why it happens:** World Space Canvas default Sorting Layer is "Default" with Order in Layer 0, same as most sprites.

**How to avoid:** In the Canvas component Inspector, set Sorting Layer to "Default" and Order in Layer to 10 (above sprites at order 0). The project currently has only one sorting layer ("Default") defined in TagManager.asset, so no new layer needs to be created.

**Warning signs:** Nothing visible in Play Mode even though CanvasGroup.alpha > 0.

### Pitfall 3: Event Leak on Purify Path

**What goes wrong:** After an enemy is purified (becomes NPC), the UI script's event handlers remain subscribed to the now-disabled `EnemyStats`. When another enemy triggers the same events (unlikely but possible in future multi-enemy scenarios), stale references cause NullReferenceExceptions or wrong UI updates.

**Why it happens:** Using `OnDestroy()` only for unsubscription. `PurifiedNPC.Activate()` does not destroy the GameObject, so `OnDestroy()` never fires on the UI child.

**How to avoid:** Subscribe to `OnDeath` and call `Unbind()` inside `HandleDeath()`. Since `EnemyStats.Purify()` fires `OnDeath` before setting `enabled = false`, this is guaranteed to execute.

**Warning signs:** NullReferenceException referencing `EnemyStats` in bar Update/event methods after enemy purification.

### Pitfall 4: CanvasGroup.alpha vs. GameObject.SetActive for Visibility

**What goes wrong:** Using `SetActive(false)` to hide the canvas before first hit and `SetActive(true)` to show it means the Coroutine for fade-in cannot start (you can't `StartCoroutine` on an inactive GameObject).

**How to avoid:** Keep the GameObject active at all times. Use `CanvasGroup.alpha = 0` to hide and `CanvasGroup.alpha = 1` (via Coroutine) to show. Only use `SetActive(false)` on death — by that point, the Coroutine is stopped before the call.

**Warning signs:** "StartCoroutine can only be called on active GameObjects" error in console.

### Pitfall 5: EnemyStats.OnCorruptionChanged Does Not Exist Yet

**What goes wrong:** The planner or implementer assumes `EnemyStats` has an `OnCorruptionChanged` event — it does not. The current code has only `OnDamaged` and `OnDeath`.

**How to avoid:** TECH-02 must add BOTH `OnHpChanged` AND `OnCorruptionChanged` to `EnemyStats`. The plan must include this as a prerequisite task (Wave 1) before any UI script tasks.

**Warning signs:** Compiler error CS0117 or CS1061 when referencing `_stats.OnCorruptionChanged`.

### Pitfall 6: Enemy Flipping Distorts the Canvas

**What goes wrong:** If the enemy flips by negating `localScale.x` (common 2D pattern), all children — including the Canvas — mirror. The bars appear backwards.

**How to avoid:** Check how this project's enemy flips. Review `EnemyAI.cs` for flip logic. If scale-based flip is used, the World Space Canvas must either counter-scale (set its own `localScale.x` to `-1` when parent flips) or the Canvas must be positioned as a sibling rather than a child (but then a follow script is needed). Alternatively, anchor the canvas at Y offset only with X = 0 — since bars are symmetric, mirroring has no visual impact.

**Assessment:** HP bars and corruption bars are symmetric (horizontal fill, same appearance left-to-right). Sweet Spot overlay is also symmetric relative to the bar. So scale flip does NOT visually distort the bars — the fill direction and overlay position are self-consistent. **No action required** for flip handling.

---

## Code Examples

### Adding Events to EnemyStats (TECH-02)

```csharp
// Source: EnemyStats.cs analysis + CONTEXT.md D-11

// Add to event declarations section (after OnDeath):
public event Action<float, float> OnHpChanged;         // (current, max)
public event Action<float, float> OnCorruptionChanged; // (current, max)

// Modify TakeDamage() — add two fire calls after value updates:
public void TakeDamage(float hpDamage, float corruptionDamage)
{
    if (isDead) return;

    CurrentHp         -= hpDamage;
    CurrentCorruption -= corruptionDamage;

    OnHpChanged?.Invoke(CurrentHp, maxHp);                       // ADD
    OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruption); // ADD

    Debug.Log(/* existing */);
    OnDamaged?.Invoke();

    if (CurrentHp <= 0f)
        CheckDeathState();
}

// Modify SpendHpOnAttack() — add one fire call:
public bool SpendHpOnAttack(float cost)
{
    if (isDead) return false;
    CurrentHp -= cost;
    OnHpChanged?.Invoke(CurrentHp, maxHp);  // ADD

    if (CurrentHp <= 0f)
    {
        Die();
        return false;
    }
    return true;
}
```

### EnemyWorldSpaceUI Full Skeleton

```csharp
// Source: CONTEXT.md D-12, UI-SPEC.md Component Inventory + TECH-03 pattern
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyWorldSpaceUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("HP Bar")]
    [SerializeField] private Image hpFill;

    [Header("Corruption Bar")]
    [SerializeField] private Image corruptionFill;

    [Header("Sweet Spot")]
    [SerializeField] private RectTransform sweetSpotOverlay;

    private EnemyStats _stats;
    private bool _revealed;
    private Coroutine _fadeCoroutine;

    private static readonly Color HealthyColor = new Color(0.25f, 0.72f, 0.35f, 1f);  // #40B859
    private static readonly Color DangerColor  = new Color(0.85f, 0.22f, 0.22f, 1f);  // #D93838

    private void Awake()
    {
        var stats = GetComponentInParent<EnemyStats>();
        if (stats != null) Bind(stats);
    }

    public void Bind(EnemyStats stats)
    {
        if (_stats != null) Unbind();
        _stats = stats;
        _stats.OnHpChanged         += HandleHpChanged;
        _stats.OnCorruptionChanged += HandleCorruptionChanged;
        _stats.OnDamaged           += HandleFirstDamage;
        _stats.OnDeath             += HandleDeath;

        HandleHpChanged(_stats.CurrentHp, _stats.MaxHp);
        HandleCorruptionChanged(_stats.CurrentCorruption, _stats.MaxCorruption);
        SetSweetSpotOverlay();

        if (canvasGroup != null) { canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = false; }
        _revealed = false;
    }

    public void Unbind()
    {
        if (_stats == null) return;
        _stats.OnHpChanged         -= HandleHpChanged;
        _stats.OnCorruptionChanged -= HandleCorruptionChanged;
        _stats.OnDamaged           -= HandleFirstDamage;
        _stats.OnDeath             -= HandleDeath;
        _stats = null;
    }

    private void OnDestroy() => Unbind();

    private void HandleHpChanged(float current, float max)
    {
        if (hpFill == null) return;
        float ratio = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
        hpFill.fillAmount = ratio;
        float colorT = (ratio <= 0.5f) ? (ratio / 0.5f) : 1f;
        hpFill.color = Color.Lerp(DangerColor, HealthyColor, colorT);
    }

    private void HandleCorruptionChanged(float current, float max)
    {
        if (corruptionFill == null) return;
        corruptionFill.fillAmount = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
    }

    private void HandleFirstDamage()
    {
        if (_revealed) return;
        _revealed = true;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeIn(0.15f));
    }

    private void HandleDeath()
    {
        Unbind();  // Clean up subscriptions before SetActive
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        gameObject.SetActive(false);
    }

    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private void SetSweetSpotOverlay()
    {
        if (sweetSpotOverlay == null || _stats == null) return;
        float min = Mathf.Clamp01(_stats.basePurificationMin - _stats.bonusPurificationMargin);
        float max = Mathf.Clamp01(_stats.basePurificationMax + _stats.bonusPurificationMargin);
        sweetSpotOverlay.anchorMin = new Vector2(min, 0f);
        sweetSpotOverlay.anchorMax = new Vector2(max, 1f);
        sweetSpotOverlay.offsetMin = Vector2.zero;
        sweetSpotOverlay.offsetMax = Vector2.zero;
    }
}
```

### Corruption Bar Direction Note

Per D-05: left = 0% corruption (fully purified), right = 100% corruption (fully corrupted). Enemy starts at `CurrentCorruption = maxCorruption` (fully corrupted). As the player attacks, corruption decreases.

Fill setup: `Image.fillMethod = Horizontal`, `Image.fillOrigin = Right` (fills from the right, meaning high corruption = full bar; low corruption = bar shrinks from the right). This correctly represents "right = corrupted".

Alternatively, use `fillOrigin = Left` with `fillAmount = 1 - (currentCorruption / maxCorruption)`, which shows purification progress. **The UI-SPEC and CONTEXT.md say "left = 0% corruption, right = 100%"** — this means the fill represents corruption level directly: `fillAmount = currentCorruption / maxCorruption` with `fillOrigin = Left`.

Wait — re-reading: left = 0% corruption means left side is the "clean" end. If the bar fills left-to-right representing corruption level, then at start (100% corrupted) the bar is full. As corruption drops to 0%, the bar empties from the right. This is: `fillMethod = Horizontal`, `fillOrigin = Left`, `fillAmount = currentCorruption / maxCorruption`. The Sweet Spot overlay is then positioned at `[basePurificationMin, basePurificationMax]` fractions of bar width from the left, matching the corruption percentage value. This is internally consistent.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `OnDestroy()` for event cleanup | Explicit `Bind()`/`Unbind()` with cleanup in `OnDeath` handler | Phase 1 established; Phase 2 extends | Handles Purify path where `OnDestroy` is not guaranteed |
| `WaitForSeconds` coroutine | Per-frame countdown timer | Phase 1 (SkillBase) | Enables runtime progress tracking |
| Screen Space Overlay canvas for all UI | World Space Canvas as child | Phase 1 established | Bars follow world-space objects automatically |

**Deprecated/outdated patterns in this project context:**
- Subscribing only in `Start()` and `OnDestroy()`: safe for the player (never purified), unsafe for enemies.
- Using `FindObjectOfType<>` to find stats components: the child-to-parent relationship (`GetComponentInParent<>`) is cleaner and more robust.

---

## Open Questions

1. **Does EnemyAI flip the enemy via `localScale.x` negation?**
   - What we know: `EnemyAI.cs` exists but was not reviewed in detail for flip logic.
   - What's unclear: Whether bars appear visually mirrored when enemy faces left.
   - Recommendation: Check `EnemyAI.cs` at plan time. If scale-based flip: since HP/corruption bars are symmetric, no visual distortion occurs. No action needed.

2. **Is there a single enemy prefab or multiple enemy types?**
   - What we know: Scene analysis shows one "Enemy" object using `EnemyStats`. No enemy prefabs exist in `Assets/Prefabs/`.
   - What's unclear: Whether the enemy is scene-placed or if prefabs are planned. UI-SPEC notes "per-enemy-prefab Inspector-assigned width."
   - Recommendation: Add the World Space Canvas to the scene-placed enemy object for this phase. The design is prefab-ready (child hierarchy) so migration to prefabs later is trivial.

3. **Should EnemyWorldSpaceUI be a single script or three separate scripts?**
   - What we know: UI-SPEC approves either approach. Phase 1 used separate scripts (PlayerHUDBar.cs, PlayerTierDisplay.cs, PlayerVignette.cs).
   - Recommendation: Single `EnemyWorldSpaceUI.cs` for Phase 2. Three behaviors (HP, corruption, sweet spot) share one `Bind()`/`Unbind()` call and one `EnemyStats` reference. Splitting them adds complexity with no architectural benefit at this scale.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity 6 Editor | All tasks | Assumed present (project is active) | Unity 6 | — |
| UnityEngine.UI (uGUI) | HP bar, Corruption bar, Canvas | Bundled with Unity 6 | Built-in | — |
| TextMeshPro | (Deferred — no text this phase) | Bundled | Built-in | N/A |
| System.Action | Event delegates | .NET runtime | Built-in | — |

No external packages are required. No new package installations needed.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Unity Play Mode testing (manual UAT) — no automated test framework detected in project |
| Config file | none — Play Mode only |
| Quick run command | Enter Play Mode in Unity Editor, inspect via Debug.Log + visual |
| Full suite command | Play Mode with enemy in scene; trigger all damage and death scenarios |

No `pytest`, `jest`, or Unity Test Runner config files were found. Phase 1 used a Human UAT document (`01-HUMAN-UAT.md`) for verification. Phase 2 should follow the same pattern.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ENM-01 | HP bar appears above enemy head and follows enemy movement | Manual Play Mode | N/A — visual test | ❌ Wave 0 |
| ENM-02 | HP bar hidden before first hit; appears on hit; disappears on death | Manual Play Mode | N/A — event-triggered visual | ❌ Wave 0 |
| ENM-03 | Corruption bar displays and updates on corruption change | Manual Play Mode | N/A — visual test | ❌ Wave 0 |
| ENM-04 | Sweet Spot overlay positioned at correct fraction of corruption bar | Manual Play Mode + Inspector check of anchor values | N/A | ❌ Wave 0 |
| ENM-05 | Different enemy types show different Sweet Spot positions | Manual Play Mode — requires two enemy configs with different basePurificationMin/Max | N/A | ❌ Wave 0 |
| TECH-02 | OnHpChanged event exists and fires with correct (current, max) args | Compiler check (no error) + Debug.Log in handler | Open Unity, no compile errors | ❌ Wave 0 |
| TECH-03 | Bind/Unbind pattern; no event leak on Purify path | Manual Play Mode — purify enemy, verify no NRE in console | N/A — manual | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** Open Unity Editor, confirm zero compile errors
- **Per wave merge:** Enter Play Mode; test damage trigger, death trigger, visual correctness
- **Phase gate:** Full scenario test (damage → bars appear, purify → no NRE, destroy → bars disappear) before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `02-HUMAN-UAT.md` — manual test script covering all 7 behaviors above (match Phase 1 pattern of `01-HUMAN-UAT.md`)
- [ ] No automated test files needed (project uses manual Play Mode verification only, consistent with Phase 1)

---

## Sources

### Primary (HIGH confidence)

- `Assets/Enemy/EnemyStats.cs` — direct code analysis: existing events, event fire points, Purify sequence
- `Assets/Player/PlayerHUDBar.cs` — Phase 1 reference implementation for event subscription pattern
- `Assets/Enemy/PurifiedNPC.cs` — confirmed: `Activate()` sets `enabled = false` without `Destroy()`, `OnDestroy` not called on UI children
- `.planning/phases/02-enemy-world-space-ui/02-CONTEXT.md` — 12 locked decisions
- `.planning/phases/02-enemy-world-space-ui/02-UI-SPEC.md` — approved design contract with exact colors, dimensions, animation contract
- `ProjectSettings/TagManager.asset` — confirmed: one sorting layer ("Default") exists; no "EnemyHUD" layer present

### Secondary (MEDIUM confidence)

- Phase 1 RESEARCH.md — World Space Canvas attachment pattern, sorting layer approach (established and working in Phase 1)
- Phase 1 PLAN files (01-01, 01-02) — plan structure reference for granularity and task format

### Tertiary (LOW confidence)

- None — all findings are verified against actual project code or established Phase 1 patterns.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — uGUI bundled, no new packages, all verified against Phase 1 working code
- Architecture: HIGH — World Space Canvas child pattern verified in Phase 1; Bind/Unbind pattern explicitly mandated in CONTEXT.md D-12
- Pitfalls: HIGH — Purify path traced through actual `EnemyStats.Purify()` source code line-by-line; RectTransform anchor offset issue is a known Unity uGUI behavior
- Event design: HIGH — events traced through existing source; missing events (OnHpChanged, OnCorruptionChanged) confirmed absent from current EnemyStats.cs

**Research date:** 2026-04-06
**Valid until:** 2026-05-06 (stable Unity uGUI API; no external dependencies)
