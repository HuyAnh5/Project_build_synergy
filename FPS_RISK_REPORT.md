# FPS Risk Report

This report tracks likely FPS and frame-time jitter risks under `Assets/Scripts`.

## Current Build Baseline

Latest check:

* `dotnet build Project_build_synergy.sln`
* 0 errors
* 35 warnings

Remaining warnings are editor/inspector obsolete API warnings and are not currently treated as runtime FPS blockers.

## High-Risk Patterns

| Pattern | Current examples | Risk | Suggested mitigation | Status |
|---|---|---|---|---|
| Per-frame UI polling | `ActorWorldUI.LateUpdate`, `CombatHUD.Update`, `ConsumableBarUIManager.Update`, `DraggableSkillIcon.Update`, `DiceDraggableUI.Update`, `DiceEquipUIManager.LateUpdate` | Baseline jitter, repeated UI churn | Dirty flags, signatures, event/state-driven refresh | Started |
| Scene-wide lookup in hot/preview paths | Targeting, preview, HUD, dice, popup, singleton-like managers | Spikes during hover/target/preview/cast | Registries/caches, explicit references, owner-passed context | Significantly reduced |
| Tooltip/layout rebuild | `SkillTooltipUI*`, `ActorWorldKeywordTooltipUI`, `ConsumableBarUIManager.TooltipPresentation`, `DiceDraggableUI.Tooltip` | TMP/layout spikes while hovering | Content signatures, position-only updates, shared tooltip presenter | Still high-value |
| Popup/tween bursts | `DamagePopupSystem`, `CombatHitFeedback`, `PlayerDiceCastAnimator`, dice roll popups | Hit/status bursts can create frame spikes | Pooling caps, batching, avoid runtime particle creation | Guard-hit mitigated |
| Runtime object creation | `new GameObject`, `AddComponent` in feedback/setup/runtime UI paths | GC/allocation and initialization spikes | Pool/prewarm or keep setup-only | Not broadly started |

## Completed FPS-Adjacent Work

### Status row rendering

`ActorWorldUI` and `CombatHUD` now share `CombatStatusRowRenderer`.

This is primarily maintainability work, but it prepares future status-row state snapshots and reduces the chance that enemy/player status UI diverge.

### Hot lookup registries/caches

Completed registry/cache coverage:

* `ActorWorldUI`
* `DraggableSkillIcon`
* `DiceDraggableUI`
* `CombatActor`
* `DamagePopupSystem`
* `TurnManager`
* `BattlePartyManager2D`
* `CombatHUD`
* `DiceEquipUIManager`
* `ConsumableBarUIManager`
* `DiceSlotRig`
* `GameplayDiceEditController`
* `RunInventoryManager`
* `SelfCastDropZone`
* `SkillTooltipPrefabProvider`

Effect:

* Preview/target/UI code now uses cached/registered instances where semantics were clear.
* Fallback scene scan remains for startup and missing registry states.

### Canvas lookup cache

`SceneCanvasLookup` now caches canvas discovery per frame and is shared by:

* `SkillTooltipUI`
* `ActorWorldKeywordTooltipUI`
* `DamagePopupSystem`
* `DiceSpinnerGeneric.Visuals`

### UI polling/churn reductions

Completed:

* `CombatHUD.Update()` gates player focus/vitals ensure calls.
* `ActorWorldUI.LateUpdate()` gates repeated reference resolution.
* `ConsumableBarUIManager` hover handling skips floating-presentation refresh when the hovered slot did not change.
* `ConsumableBarUIManager` presentation paths now use dirty setters for slot active state, icon state/color, labels, backgrounds, tooltip active state, and action button presentation.
* Consumable tooltip auto-size is gated by content changes instead of recalculating on every same-content refresh.
* `DraggableSkillIcon` idle metadata polling is throttled while hover/selected/drag remains immediate.
* `DiceDraggableUI` idle pointer safety polling is throttled while hover/hold/drag remains immediate.
* Skill and consumable keyword tooltip link detection cache TMP mesh generation by text content instead of forcing mesh generation on unchanged text every frame.
* `TurnManager` continue-button lookup no longer repeatedly scans all buttons after a miss.
* Skill and actor keyword tooltip prefab-provider lookup now uses a shared registry/cache with fallback scan.

### Guard-hit feedback spike mitigation

Completed:

* `DamagePopupSystem.SpawnGuardSCurve()` no longer uses a custom `DOTween.To` lambda that recalculated S-curve motion every tween update.
* Guard/focus popup motion keeps the old S-curve feel with a lightweight cubic curve evaluator plus fade/scale sequence.
* The Guard popup path avoids DOTween path allocation and avoids per-frame trigonometry.
* `CombatHitFeedback` no longer creates procedural runtime `ParticleSystem` bursts for `FeedbackKind.Guard`.
* Guard UI roots now skip redundant `SetActive` calls when the active state is already correct.
* Player HUD and enemy world UI HP/Guard refresh now avoid reassigning unchanged TMP text, colors, outlines, and fill amounts.
* HP-hit behavior remains unchanged; Guard still flashes and still shows the blocked popup value.

Why:

* Enemy attacks into HP only used the HP popup path.
* Enemy attacks into Guard additionally spawned a Guard popup and could trigger Guard feedback paths.
* The old Guard popup path and runtime particle creation were plausible script-side spike multipliers during enemy attacks.

## Remaining Notable FPS Risks

1. `DraggableSkillIcon.Update()` still polls runtime/aura/preview/pointer bridge per icon, though idle metadata refresh is throttled.
2. `DiceDraggableUI.Update()` still has per-card hover/inspect/tooltip/tween-adjacent behavior, though idle pointer safety polling is throttled.
3. `DiceEquipUIManager.LateUpdate()` still refreshes combat dice runtime state and world-slot sync while active; world-slot sync appears visually intentional.
4. Tooltip/keyword/layout systems are improved but still contain active hover/link positioning work.
5. `ActorWorldUI.RefreshIntent()` still runs through normal runtime refresh and should only be optimized with a safe intent signature.
6. Popup/tween feedback bursts remain unbounded from a profiling perspective for non-Guard burst types.
7. Prototype/sandbox systems still contain setup/path scene scans; lower priority unless active in production flow.

## Recommended Next FPS Batches

1. Shared tooltip presenter/positioner to reduce duplicated active hover/layout work.
2. `DraggableSkillIcon` runtime-state dirty gate beyond metadata throttling.
3. `DiceEquipUIManager` active/mode gate for `LateUpdate` only after confirming world dice mirror expectations in Unity.
4. Shared `CombatPreviewPresenter` to avoid repeated preview clear/show logic.
5. Damage popup active cap or backpressure after profiling confirms popup spikes.

## Unity Profiling Checklist

After each UI/FPS batch, profile:

* CPU Usage: Scripts, UI, Rendering.
* UI: Canvas rebuilds, Layout rebuilds, TMP allocations.
* Rendering/GPU: present/vsync waits vs actual script time.
* GC Alloc in hover, targeting, hit popup, and turn start/end.
