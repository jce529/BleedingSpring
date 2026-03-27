# Codebase Concerns

**Analysis Date:** 2026-03-27

## Tech Debt

**Abandoned `PlayerCombat.cs` file still present:**
- Issue: File is not deleted despite the comment at the top declaring it replaced by `BasicAttackSkill.cs`, `WideSlashSkill.cs`, and `ProjectileSkill.cs`
- Files: `Assets/Player/PlayerCombat.cs`
- Impact: Stale file causes confusion about which scripts are active; Unity will compile it, producing a class name `PlayerCombat` that references nothing and is never used
- Fix approach: Delete `Assets/Player/PlayerCombat.cs` and its `.meta` file

**`NewEmptyCSharpScript.cs` placeholder file committed:**
- Issue: Auto-generated empty Unity script template (`public class NewEmptyCSharpScript {}`) was never filled in or deleted
- Files: `Assets/NewEmptyCSharpScript.cs`
- Impact: Dead code, compiles to an unused type, clutters the project
- Fix approach: Delete `Assets/NewEmptyCSharpScript.cs` and its `.meta` file

**`PlayerStats.cs` is superseded but not removed:**
- Issue: `PlayerStats.cs` implements `IDamageable` and manages HP independently, but `PlayerWaterStats.cs` also implements `IDamageable` and is the class actually used by `PlayerController`, `EnemyAttack`, and `WideSlashSkill`. The two stat systems coexist with no connection between them.
- Files: `Assets/Player/PlayerStats.cs`, `Assets/Player/PlayerWaterStats.cs`
- Impact: Any future contributor may add `PlayerStats` to the player prefab by mistake, producing a second independent HP bar that never triggers death. Also, `IPlayerContext` only exposes `PlayerWaterStats`, making `PlayerStats` permanently unreachable through the interface.
- Fix approach: Confirm `PlayerStats` is not attached to any prefab, then delete it; or document explicitly that it is reserved for non-water entities

**`EnemyAttack` has two competing attack entry points:**
- Issue: `AttackPlayer()` (no args) uses the cached `playerStats` field. `AttackPlayer(PlayerWaterStats target)` overwrites `playerStats` as a side effect. `OnTriggerEnter2D` calls the overloaded version, which permanently mutates the cached reference.
- Files: `Assets/Enemy/EnemyAttack.cs` (lines 88–93, 101–109)
- Impact: If a trigger contact fires after `EnemyAI.AttackPlayer()` is in use, the cached target is overwritten. In multi-player or test scenarios this causes attacks to always target the last collided object.
- Fix approach: Make `AttackPlayer(PlayerWaterStats target)` use a local variable instead of mutating `playerStats`

**`bonusPurificationMargin` on `EnemyStats` is a mutable public field:**
- Issue: `bonusPurificationMargin` is declared `public float` with no validation, allowing any external code to set it to negative values, which would invert the purification window (effectiveMin > effectiveMax)
- Files: `Assets/Enemy/EnemyStats.cs` (line 29)
- Impact: Negative margin silently makes purification impossible by producing an invalid range; no guard exists in `CheckDeathState()`
- Fix approach: Make field `[SerializeField] private` and expose only the `WidenPurificationRange()` method; add a clamp so effectiveMin never exceeds effectiveMax

**`PurifiedNPC` dialogue only outputs to `Debug.Log`:**
- Issue: `ShowNextDialogue()` calls `Debug.Log` with the dialogue text. There is no UI system, no text component, and no dialogue manager.
- Files: `Assets/Enemy/PurifiedNPC.cs` (line 85)
- Impact: Dialogue system is non-functional in builds (Debug.Log is stripped or invisible to players). The `#if UNITY_EDITOR` block shows a GUI box in-editor only, so players in builds see no dialogue at all.
- Fix approach: Implement a canvas-based dialogue UI or integrate with a dialogue manager; remove the `#if UNITY_EDITOR` gate on the display block

**`PurifiedNPC` interaction uses legacy `Input.GetKeyDown`:**
- Issue: `PurifiedNPC.Update()` uses `Input.GetKeyDown(interactKey)` (old Input Manager) while the rest of the project uses the new Unity Input System via `InputSystem_Actions` and `InputHandler`
- Files: `Assets/Enemy/PurifiedNPC.cs` (line 76)
- Impact: The interaction key will silently fail if the project disables the legacy Input Manager in Player Settings; inconsistent input backend increases maintenance burden
- Fix approach: Replace with an Input System action or have `InputHandler` expose an `OnInteract` event

## Known Bugs

**Attacking state is never exited when a skill is interrupted by death:**
- Symptoms: If the player dies (HP = 0 or corruption overflow) while a skill coroutine is running (`SkillBase.UseCoroutine`), `Context.ChangeState(PlayerState.Idle)` at the end of the coroutine will not be reached because `PlayerController.Update()` returns early for `Dead` state, but the coroutine itself continues until `cooldownDuration` elapses and then attempts to call `Context.ChangeState(PlayerState.Idle)` on a dead player
- Files: `Assets/Player/SkillBase.cs` (lines 106–126), `Assets/Player/PlayerController.cs` (line 101)
- Trigger: Use any skill, then die before the cooldown timer expires
- Workaround: None currently in place; the state is set to `Idle` after the skill completes but the player is already dead and animation is already playing `Death`

**`EnemyAI.UpdateHit()` always transitions to Chase regardless of context:**
- Symptoms: After hit-stun ends, the enemy always enters Chase state even if the player has moved outside detection range during the stun
- Files: `Assets/Enemy/EnemyAI.cs` (lines 149–152)
- Trigger: Hit an enemy while standing far away (edge of detection range) and quickly run out of range before stun ends
- Workaround: None; enemy will immediately chase then transition back to Patrol on the next frame once it re-checks distance

**`ProjectileSkill` stage 0 fires no projectile but still triggers Attack animation:**
- Symptoms: Stage 0 logs "발사 없음 (어그로 모션)" and does nothing, but `PlayerController` still triggers `Attack2` animation and locks the player in `Attacking` state for the full `cooldownDuration`
- Files: `Assets/Player/ProjectileSkill.cs` (lines 42–43), `Assets/Player/PlayerController.cs` (lines 215–222)
- Trigger: Press [I] at water tier 0
- Workaround: Cycle to tier 1+ before using the projectile skill

**`EnemyStats.TakeDamage` fires `OnDamaged` even when already dead:**
- Symptoms: The `isDead` guard at line 73 returns early, but `OnDamaged` is invoked at line 83 after the HP/corruption update — if `CheckDeathState()` sets `isDead = true` inside the same call, subsequent fast calls with `isDead` true do return early. However, the first call that kills the enemy still fires `OnDamaged`, causing `EnemyAI` to enter `Hit` state simultaneously with `Dead` state via `HandleHit()` and `HandleDeath()` both being called.
- Files: `Assets/Enemy/EnemyStats.cs` (lines 73–87), `Assets/Enemy/EnemyAI.cs` (lines 169–178)
- Trigger: Kill an enemy with a single hit that reduces HP to 0
- Workaround: `EnemyAI.HandleHit()` checks `if (CurrentState == EnemyState.Dead) return;`, so the Hit state entry is blocked after death — but the order of event invocation means both handlers fire in the same frame

## Performance Bottlenecks

**`Physics2D.OverlapBoxAll` allocates per attack:**
- Problem: Every skill execution (BasicAttackSkill, WideSlashSkill, ProjectileSkill laser) calls `Physics2D.OverlapBoxAll`, which allocates a new `Collider2D[]` array on every invocation
- Files: `Assets/Player/BasicAttackSkill.cs` (line 58), `Assets/Player/WideSlashSkill.cs` (lines 64, 99, 106), `Assets/Player/ProjectileSkill.cs` (line 95)
- Cause: No pre-allocated buffer; Unity's non-alloc variants (`OverlapBoxNonAlloc`) are not used
- Improvement path: Replace with `Physics2D.OverlapBoxNonAlloc` and a shared static `Collider2D[]` buffer per skill class

**`PurifiedNPC.Update()` calls `Physics2D.OverlapCircle` every frame:**
- Problem: Every `PurifiedNPC` runs a physics query every frame regardless of proximity
- Files: `Assets/Enemy/PurifiedNPC.cs` (line 74)
- Cause: No spatial culling or event-based trigger alternative
- Improvement path: Use `OnTriggerEnter2D`/`OnTriggerExit2D` with a dedicated trigger collider, or check only every 0.1s using a timer

**`EnemyAI` uses `FindFirstObjectByType<PlayerController>()` in `Start()`:**
- Problem: `FindFirstObjectByType` searches all active objects in the scene; fine for a small scene but will slow scene load as enemy count grows
- Files: `Assets/Enemy/EnemyAI.cs` (line 64)
- Cause: No central player reference registry
- Improvement path: Create a static `PlayerRegistry` singleton or expose the player transform through `GameStateManager`

**Dash afterimage spawns `Instantiate/Destroy` pairs every 0.03s:**
- Problem: `SpawnAfterimages()` creates and destroys GameObjects at 33Hz during every dash, generating garbage collection pressure
- Files: `Assets/Player/PlayerMovement.cs` (lines 152–169)
- Cause: No object pool for afterimage instances
- Improvement path: Implement a small object pool for afterimage GameObjects

## Fragile Areas

**`SkillBase.UseCoroutine` locks `PlayerState.Attacking` for the entire cooldown duration:**
- Files: `Assets/Player/SkillBase.cs` (lines 106–126)
- Why fragile: Movement, jump, and dash are all blocked while `CurrentState == PlayerState.Attacking` (see `PlayerController` lines 146–147, 172, 182). The Attacking state persists for `cooldownDuration` seconds, not just the animation length. If a skill's `cooldownDuration` is long, the player is completely frozen for that duration even after the attack animation finishes.
- Safe modification: Separate the animation lock duration from the cooldown duration; exit Attacking state after the animation ends, allow movement, but keep `IsOnCooldown` true until the cooldown expires
- Test coverage: No automated tests; must be verified manually in Play Mode

**`GameStateManager` uses `DontDestroyOnLoad` with no scene-reset logic:**
- Files: `Assets/GameScript/GameStateManager.cs` (line 38)
- Why fragile: `Time.timeScale` is set to 0 on GameOver. If the scene is reloaded without going through `SetState(Playing)`, `timeScale` remains 0 and the game appears frozen. No reset hook exists.
- Safe modification: Subscribe to `SceneManager.sceneLoaded` and call `SetState(GameState.Playing)` to reset time scale on scene load
- Test coverage: None

**`EnemyStats.Purify()` calls `gameObject.AddComponent<PurifiedNPC>()` at runtime:**
- Files: `Assets/Enemy/EnemyStats.cs` (line 158)
- Why fragile: `AddComponent` at runtime is expensive and will throw if called on an already-destroyed object or if the `Interactable` layer doesn't exist in the project Layer settings. The `PurifiedNPC.Activate()` call on line 159 also sets `gameObject.layer = LayerMask.NameToLayer("Interactable")` which silently returns -1 if the layer is not registered, producing incorrect layer assignment.
- Safe modification: Pre-attach `PurifiedNPC` to enemy prefabs with `enabled = false`; check that the "Interactable" layer exists in Project Settings
- Test coverage: None

**`WideSlashSkill` 3단계 ("해일참") uses a hardcoded `LayerMask.GetMask("Contamination")` string:**
- Files: `Assets/Player/WideSlashSkill.cs` (line 106)
- Why fragile: If the "Contamination" layer is renamed or missing, `GetMask` returns 0 (matches nothing) with no error. The skill will silently fail to clear contamination tiles.
- Safe modification: Expose a `[SerializeField] LayerMask contaminationLayer` field instead of using the string-based lookup

**`EnemyAttack.OnTriggerEnter2D` and `EnemyAI.UpdateAttack` can both fire in the same frame:**
- Files: `Assets/Enemy/EnemyAttack.cs` (lines 101–109), `Assets/Enemy/EnemyAI.cs` (lines 134–146)
- Why fragile: `EnemyAI` calls `AttackPlayer()` on a timer. If the enemy collider is also `isTrigger`, `OnTriggerEnter2D` fires a second attack on contact. A single player step into range can trigger two attacks simultaneously.
- Safe modification: Choose one attack trigger per enemy type; comment in `EnemyAttack.cs` notes that `OnTriggerEnter2D` is "선택적 사용" (optional), but there is no runtime guard preventing both from being active simultaneously

## Scaling Limits

**Single-scene architecture with no save/load system:**
- Current capacity: One scene (`Assets/Scenes/SampleScene.unity`); `GameStateManager` persists across loads but game world state (enemy positions, purification status, player stats) does not
- Limit: Adding more levels requires either additive scene loading or a complete save/load system; neither is implemented
- Scaling path: Implement a `GameDataManager` with serializable save state; define scene transition triggers

**No enemy type variation system:**
- Current capacity: Single enemy type via `EnemyAI` + `EnemyStats` + `EnemyAttack`; stat variation is only per-prefab via Inspector values
- Limit: Adding new enemy behaviors requires subclassing or modifying `EnemyAI` directly; no strategy pattern or component-swap system exists
- Scaling path: Extract enemy behavior into a strategy pattern; define enemy type scriptable objects

## Missing Critical Features

**No UI system (HP bar, corruption gauge, game over screen):**
- Problem: `PlayerWaterStats` fires `OnWaterChanged` and `OnCorruptionChanged` events, and `GameStateManager` fires `OnGameStateChange`, but no subscriber exists in the codebase. There is no canvas, no UI script, and no game over screen.
- Blocks: Player cannot see their health or corruption level; game over state freezes time with no feedback or restart option

**No audio system:**
- Problem: No audio source, audio manager, or sound effect calls exist anywhere in the project scripts
- Blocks: Attack feedback, death sounds, ambient audio are all absent

**No respawn / restart mechanism:**
- Problem: `HandleDeath()` in `PlayerController` stops movement and triggers the death animation, and `GameStateManager` sets state to GameOver and freezes time. There is no restart button, no respawn point, and no scene-reload call.
- Blocks: After dying, the only way to continue is to stop and restart Play Mode in the editor

**No persistent data between sessions:**
- Problem: No `PlayerPrefs`, no serialization, no save file. All skill stages, stats, and world state reset on every Play Mode start.
- Blocks: Any progression or upgrade system

## Test Coverage Gaps

**No automated tests exist:**
- What's not tested: All gameplay systems — player movement, skill execution, enemy AI state machine, purification logic, damage/death flow
- Files: Entire `Assets/` directory; no `*.test.cs` or `*.spec.cs` files found; no `Tests/` directory; no Unity Test Framework assembly definitions
- Risk: Any refactor to core systems (SkillBase, EnemyStats, PlayerWaterStats) can silently break existing behavior with no safety net
- Priority: High — especially for `EnemyStats.CheckDeathState()` (purification window logic) and `PlayerWaterStats.CheckDeath()` (dual death condition)

---

*Concerns audit: 2026-03-27*
