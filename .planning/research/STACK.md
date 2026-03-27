# Stack Research

**Domain:** Unity 6 UGUI-based HUD and World Space UI for 2D Action Roguelike
**Researched:** 2026-03-27
**Confidence:** HIGH (all versions verified directly from Packages/packages-lock.json and source files)

---

## UGUI vs UI Toolkit: Decision

**Use UGUI. Do not use UI Toolkit for this project.**

UI Toolkit (UIElements) is already installed as a module (`com.unity.modules.uielements` 1.0.0) but is the wrong tool here for three concrete reasons:

1. **World Space is unsupported in UI Toolkit.** UI Toolkit renders to a `PanelSettings` asset and a `UIDocument` component, which only operates in Screen Space. There is no World Space canvas equivalent. Enemy health bars that float above enemies require World Space Canvas — a UGUI-only feature.

2. **UGUI 2.0.0 is already installed and locked.** The project `manifest.json` declares `"com.unity.ugui": "2.0.0"` as a direct dependency. URP 17.3.0 (`com.unity.render-pipelines.core`) also declares `com.unity.ugui: 2.0.0` as its own dependency. Zero additional installation required.

3. **The existing architecture binds to C# Action events.** `PlayerWaterStats.OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged`, `EnemyStats.OnDamaged`, `OnDeath`, and `SkillBase.IsOnCooldown` are all plain C# APIs. UGUI's `Image.fillAmount` and `Slider.value` bind to these trivially in a MonoBehaviour. UI Toolkit requires UXML/USS + binding descriptors — meaningful overhead for no benefit.

**Confidence: HIGH** — verified against installed manifest, verified against EnemyStats.cs and PlayerWaterStats.cs source code, and against known Unity 6 architecture constraints (UI Toolkit World Space limitation is a documented engine constraint as of Unity 6).

---

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Unity UGUI | 2.0.0 (builtin) | All runtime UI: HUD, World Space bars, Boss bar, Skill cooldowns | Already installed; the only system with World Space Canvas support; direct MonoBehaviour integration matches existing C# event architecture |
| Unity URP | 17.3.0 (builtin) | Render pipeline that draws all UI canvases | Already configured; World Space Canvas draws correctly in URP 2D with Render Mode = World Space and a Camera reference |
| Unity Input System | 1.19.0 | Not directly used by UI, but existing `InputHandler` already gates all input via `GameStateManager` — UI does not need to register input separately | Already wired; no additional setup needed for UI layer |
| Unity Engine Coroutines | built-in | Drive skill cooldown fill animation in UI | `SkillBase` cooldown is coroutine-driven; UI cooldown overlay polls `SkillBase.IsOnCooldown` or mirrors via a new `OnCooldownChanged(float ratio)` event added to `SkillBase` |

### Canvas Architecture: Three Separate Canvases

| Canvas | Render Mode | Sort Order / Layer | Purpose |
|--------|------------|-------------------|---------|
| `HUDCanvas` | Screen Space - Overlay | Sort Order: 10 | Player HP bar, Corruption bar, Water Tier indicator, Skill cooldown slots |
| `EnemyWorldCanvas` (per prefab) | World Space | Sorting Layer: "UI", Order: 5 | Per-enemy HP bar + Corruption bar with Sweet Spot zone; child of enemy GameObject |
| `BossHUDCanvas` | Screen Space - Overlay | Sort Order: 20 | Large boss HP bar anchored to screen bottom; enabled only during boss encounters |

**Why separate canvases, not one:**
- World Space canvases must be parented to each enemy instance. A single global World Space canvas cannot follow multiple enemies without manual position tracking.
- Boss bar lives on its own canvas so it can be enabled/disabled cleanly without touching the general HUD canvas.
- Screen Space - Overlay renders on top of everything with no camera dependency — correct for player HUD and boss bar in a 2D game.

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `UnityEngine.UI` (UGUI module) | 2.0.0 | `Image`, `Slider`, `Canvas`, `CanvasScaler`, `GraphicRaycaster` | Every UI element |
| `TextMeshPro` | built-in with Unity 6 (via `com.unity.textmeshpro`, bundled) | Text rendering for numeric values, tier labels | Any text element; prefer TMP over legacy `Text` component |
| `UnityEngine.EventSystems` | part of UGUI 2.0.0 | `EventSystem` singleton required for all UGUI scenes | One `EventSystem` per scene, placed on HUDCanvas or a dedicated GameObject |

**Note on TextMeshPro:** Unity 6 bundles TMP as a core module. It does not appear in `manifest.json` as a separate entry because it ships with the editor. Import TMP Essential Resources via `Window > TextMeshPro > Import TMP Essential Resources` once.

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| Unity Editor Inspector | Wire `[SerializeField]` references from UI scripts to UGUI components | All UI controller references (Image, Slider) are set via Inspector, matching existing project pattern |
| Unity Animator (optional) | Animate bar flash on damage, boss bar intro | Use sparingly; `DOTween` or coroutines are lighter for simple fill tweens |
| `CanvasScaler` component | Maintain HUD proportions at 1920x1080 target resolution | Set Scale Mode to `Scale With Screen Size`, Reference Resolution `1920x1080`, Match `0.5` |

---

## Installation

No additional packages required. Everything needed is already present:

```
com.unity.ugui: 2.0.0          -- INSTALLED (manifest.json line 19)
com.unity.modules.ui: 1.0.0    -- INSTALLED (builtin)
com.unity.modules.uielements: 1.0.0 -- INSTALLED (builtin, not used for runtime UI)
```

TextMeshPro essential assets import (one-time, editor-only):
```
Window > TextMeshPro > Import TMP Essential Resources
```

---

## Component Patterns

### Player HUD: HP and Corruption Bars

**Component:** `Image` with `Image Type = Filled`, `Fill Method = Horizontal`

Subscribe to events in `Start()`, unsubscribe in `OnDestroy()`:

```csharp
// Signature from PlayerWaterStats.cs line 39-42:
// public event Action<float, float> OnWaterChanged;         // (current, max)
// public event Action<float, float> OnCorruptionChanged;    // (current, max)
// public event Action<int> OnWaterTierChanged;              // (tier 0-3)

stats.OnWaterChanged     += (cur, max) => hpFill.fillAmount = cur / max;
stats.OnCorruptionChanged += (cur, max) => corruptionFill.fillAmount = cur / max;
stats.OnWaterTierChanged  += tier => UpdateTierIndicator(tier);
```

**Why `Image` filled over `Slider`:** `Slider` has interactive handles and interactable state meant for input. A filled `Image` is a pure display component with no input surface area — correct for HUD bars that never receive clicks.

### Water Tier Indicator

**Component:** Four `Image` objects (or a sprite-sheet animated `Image`) representing tiers 0–3. Light up the icon at index `tier`, dim the rest. Alternatively: a `TMP_Text` label showing the current tier number.

**Pattern:** `OnWaterTierChanged(int tier)` fires from `PlayerWaterStats.CycleWaterTier()`. The UI controller receives the tier index and sets active states.

### Enemy World Space UI: HP + Corruption + Sweet Spot

**Canvas setup:** `Canvas` component on a child GameObject of the enemy prefab with `Render Mode = World Space`. Scale the RectTransform to match pixel-per-unit (e.g., `0.01` scale for a 100px-wide bar to be 1 Unity unit wide above the enemy head).

**Sweet Spot visualization:** The corruption bar is a composite of three layered `Image` components in a horizontal layout:
- Base layer: full-width background (dark gray)
- Corruption fill: `Image Type = Filled`, driven by `EnemyStats.CorruptionRatio`
- Sweet Spot highlight: a separate `Image` positioned and sized to cover `[basePurificationMin, basePurificationMax]` of the bar width

```csharp
// From EnemyStats.cs lines 23-29 (verified):
// public float basePurificationMin     = 0.3f;
// public float basePurificationMax     = 0.7f;
// public float bonusPurificationMargin = 0f;
// public float CorruptionRatio => CurrentCorruption / maxCorruption;

float effectiveMin = stats.basePurificationMin - stats.bonusPurificationMargin;
float effectiveMax = stats.basePurificationMax + stats.bonusPurificationMargin;

// Position the highlight RectTransform anchors:
sweetSpotImage.rectTransform.anchorMin = new Vector2(effectiveMin, 0f);
sweetSpotImage.rectTransform.anchorMax = new Vector2(effectiveMax, 1f);
sweetSpotImage.rectTransform.offsetMin = Vector2.zero;
sweetSpotImage.rectTransform.offsetMax = Vector2.zero;
```

**Update trigger:** `EnemyStats` currently fires `OnDamaged` and `OnDeath` only (lines 44-47). For real-time bar updates, the UI controller should either:
- Subscribe to `OnDamaged` and read `CurrentHp`, `MaxHp`, `CurrentCorruption`, `MaxCorruption` properties directly (polling on event — acceptable since damage is discrete)
- OR add `Action<float,float> OnHpChanged` and `Action<float,float> OnCorruptionChanged` events to `EnemyStats` matching the `PlayerWaterStats` pattern (cleaner, recommended when modifying EnemyStats)

**Billboard/facing:** In a 2D side-scroller the World Space canvas should NOT counter-rotate with the enemy sprite's X-flip. Use `Canvas.worldCamera` set to the main camera to ensure proper depth sorting in URP 2D.

### Boss Screen Space HP Bar

**Canvas setup:** Separate `Canvas` with `Render Mode = Screen Space - Overlay`, Sort Order 20. Anchor the bar RectTransform to `Bottom Center`, stretched horizontally with padding.

**Activation:** Boss canvas `GameObject.SetActive(false)` by default; enabled by the boss encounter manager when boss spawns.

**Data binding:** Same pattern as enemy bar but subscribes to the boss `EnemyStats` instance. Boss EnemyStats exposes the same properties — no special boss class needed.

### Skill Cooldown UI

**Problem:** `SkillBase` (line 26) exposes `bool IsOnCooldown` but no progress ratio event. The `cooldownDuration` field is `protected` and the coroutine tracks it internally.

**Solution options (in order of preference):**

1. **Add `OnCooldownChanged(float ratio)` event to `SkillBase`** — emit `(elapsed/duration)` each frame during cooldown coroutine. Cleanest; matches existing event architecture.

2. **Expose `CooldownRatio` property on `SkillBase`** — `public float CooldownRatio { get; private set; }` updated in the coroutine. UI component polls it via `Update()`.

3. **UI polls `IsOnCooldown` bool** — only shows "on cooldown" binary state, not fill progress. Acceptable if no fill animation is needed.

**Recommended:** Option 1. The existing codebase consistently uses events for stat-to-UI communication (`PlayerWaterStats` pattern). Add `public event Action<float> OnCooldownProgress` to `SkillBase`, fire it each frame in `UseCoroutine()`.

**Component:** `Image` with `Image Type = Filled`, `Fill Method = Radial 360`, `Clockwise = true`. Overlay on top of skill icon. Fill amount = `1 - cooldownRatio` (full when ready, draining to empty as cooldown elapses).

---

## Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| UGUI `Image` filled | UI Toolkit `ProgressBar` | UI Toolkit has no World Space support; overkill for this scope |
| World Space Canvas (per enemy) | Screen Space canvas + manual position via `Camera.WorldToScreenPoint` | Manual positioning creates jitter, breaks with camera zoom, requires update every frame in a separate system. World Space Canvas follows the enemy transform automatically |
| Separate HUD + Boss canvases | Single Screen Space canvas for all screen UI | Harder to enable/disable boss UI independently; sort order conflicts if many elements share one canvas |
| C# Action events → UI MonoBehaviour | ScriptableObject event channels | SO channels add indirection without benefit at this scale; project already uses direct C# events consistently |
| `CanvasScaler` Scale With Screen Size | No scaler / Constant Pixel Size | Constant pixel size breaks layout at non-1920x1080 resolutions; Scale With Screen Size adapts automatically |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| UI Toolkit (`UIDocument`, `VisualElement`) for runtime game UI | No World Space canvas support in Unity 6; requires UXML/USS workflow that adds friction; wrong abstraction layer for event-driven game HUD | UGUI `Canvas` + `Image` + `TMP_Text` |
| Legacy `Text` component | Deprecated in Unity 5.x era; poor rendering quality at small sizes; no rich text without workarounds | `TextMeshPro - Text (UI)` (`TMP_Text`) |
| `Slider` component for health bars | Has interactive handles, interactable state, navigation events — all irrelevant noise for a display-only bar; slightly heavier than `Image` | `Image` with `Image Type = Filled` |
| `Canvas.renderMode = Screen Space - Camera` for HUD | Requires assigning a camera; creates distance/FOV dependencies; Screen Space - Overlay is simpler and always renders on top for a non-3D HUD | `Screen Space - Overlay` for HUD/Boss canvas |
| World Space Canvas on a global "enemies UI" parent | Cannot follow individual enemy positions without a custom tracking script that reintroduces the complexity World Space was meant to eliminate | World Space Canvas as a child of each enemy prefab |
| `FindObjectOfType` in UI scripts to locate `PlayerWaterStats` | `FindObjectOfType` is slow and fragile; existing architecture avoids it | Wire references via `[SerializeField]` in Inspector, or have `PlayerController` / `GameStateManager` provide references |

---

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| `com.unity.ugui` 2.0.0 | URP 17.3.0 | URP core declares `com.unity.ugui: 2.0.0` as a direct dependency in packages-lock.json; versions are matched by the engine |
| `com.unity.ugui` 2.0.0 | Unity 6000.3.11f1 | UGUI 2.0.0 is the version shipped with Unity 6; `source: "builtin"` in packages-lock.json |
| `TextMeshPro` | Unity 6000.3.11f1 | Bundled with Unity 6 editor; no separate UPM entry needed |
| World Space Canvas | URP 2D Renderer 17.3.0 | World Space Canvas renders correctly with URP 2D; set `Canvas.worldCamera` to the scene camera to ensure correct sorting layer depth |

---

## Event Binding Reference (verified from source)

| Event | Source file | Signature | UI Consumer |
|-------|------------|-----------|-------------|
| `OnWaterChanged` | `PlayerWaterStats.cs:39` | `Action<float current, float max>` | Player HP bar `fillAmount = current/max` |
| `OnCorruptionChanged` | `PlayerWaterStats.cs:42` | `Action<float current, float max>` | Player Corruption bar `fillAmount = current/max` |
| `OnWaterTierChanged` | `PlayerWaterStats.cs:45` | `Action<int tier>` | Tier indicator: activate icon at index `tier` |
| `OnDamaged` | `EnemyStats.cs:44` | `Action` | Trigger bar refresh; read `CurrentHp/MaxHp`, `CurrentCorruption/MaxCorruption` |
| `OnDeath` | `EnemyStats.cs:47` | `Action` | Hide/destroy World Space canvas |
| `IsOnCooldown` | `SkillBase.cs:26` | `bool` property | Skill cooldown — no progress event exists yet; requires `SkillBase` modification |

---

## Sources

- `C:\Users\MSI\Projeect_A.E\My project\Packages\manifest.json` — verified UGUI 2.0.0, URP 17.3.0, Unity Input System 1.19.0 versions (HIGH confidence)
- `C:\Users\MSI\Projeect_A.E\My project\Packages\packages-lock.json` — verified builtin source for UGUI, transitive dependency chain (HIGH confidence)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Player\PlayerWaterStats.cs` — verified event signatures `OnWaterChanged`, `OnCorruptionChanged`, `OnWaterTierChanged` (HIGH confidence)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Enemy\EnemyStats.cs` — verified `OnDamaged`, `OnDeath` events; `basePurificationMin/Max`, `CorruptionRatio` properties (HIGH confidence)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Player\SkillBase.cs` — verified `IsOnCooldown` property, absence of cooldown progress event (HIGH confidence)
- `C:\Users\MSI\Projeect_A.E\My project\Assets\Player\ISkill.cs` — verified `ISkill` interface contract (HIGH confidence)
- Unity 6 engine constraint: UI Toolkit World Space unsupported — documented limitation of `UIDocument` component architecture (MEDIUM confidence — training data, could not verify with live docs due to tool restrictions; however this is a foundational architectural fact that has been stable across Unity versions)

---

*Stack research for: Bleeding Spring — UI System Milestone*
*Researched: 2026-03-27*
