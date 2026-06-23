# FPS Risk Report

This report tracks likely FPS and frame-time jitter risks under `Assets/Scripts`.

## Current Build Baseline

Latest check:

* `dotnet build Project_build_synergy.sln`
* 0 errors
* 35 warnings

Remaining warnings are mostly editor/inspector obsolete API warnings and are not currently treated as runtime FPS blockers.

## High-Risk Patterns

| Pattern | Current examples | Risk | Suggested mitigation | Status |
|---|---|---|---|---|
| Per-frame UI polling | `ActorWorldUI.LateUpdate`, `CombatHUD.Update`, `ConsumableBarUIManager.Update`, `DraggableSkillIcon.Update`, `DiceDraggableUI.Update`, `DiceEquipUIManager.LateUpdate` | Baseline jitter, repeated UI churn | Dirty flags, signatures, event/state-driven refresh | Started |
| Scene-wide lookup in hot/preview paths | Targeting, preview, HUD, dice, popup, singleton-like managers | Spikes during hover/target/preview/cast | Registries/caches, explicit references, owner-passed context | Significantly reduced |
| Tooltip/layout rebuild | `SkillTooltipUI*`, `ActorWorldKeywordTooltipUI`, `ConsumableBarUIManager.TooltipPresentation`, `DiceDraggableUI.Tooltip` | TMP/layout spikes while hovering | Content signatures, position-only updates, shared tooltip presenter | Still high-value |
| Popup/tween bursts | `DamagePopupSystem`, `CombatHitFeedback`, `PlayerDiceCastAnimator`, dice roll popups | Hit/status bursts can create frame spikes | Pooling caps, batching, avoid runtime particle creation | Not started |
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
* `TurnManager` continue-button lookup no longer repeatedly scans all buttons after a miss.
* Skill and actor keyword tooltip prefab-provider lookup now uses a shared registry/cache with fallback scan.

## Remaining Notable FPS Risks

1. `DraggableSkillIcon.Update()` still polls metadata/runtime/aura/preview/pointer bridge per icon.
2. `DiceDraggableUI.Update()` still has per-card hover/inspect/tooltip/tween-adjacent behavior.
3. `DiceEquipUIManager.LateUpdate()` still refreshes combat dice runtime state and world-slot sync while active.
4. Tooltip/keyword/layout systems still rebuild more than ideal during hover.
5. `ActorWorldUI.RefreshIntent()` still runs through normal runtime refresh and should only be optimized with a safe intent signature.
6. Popup/tween feedback bursts remain unbounded from a profiling perspective.
7. Prototype/sandbox systems still contain setup/path scene scans; lower priority unless active in production flow.

## Recommended Next FPS Batches

1. Tooltip content-signature and position-only updates.
2. `DraggableSkillIcon` dirty/runtime-state gate.
3. `DiceEquipUIManager` active/mode gate for `LateUpdate`.
4. Shared `CombatPreviewPresenter` to avoid repeated preview clear/show logic.
5. Damage popup active cap or backpressure after profiling confirms popup spikes.

## Unity Profiling Checklist

After each UI/FPS batch, profile:

* CPU Usage: Scripts, UI, Rendering.
* UI: Canvas rebuilds, Layout rebuilds, TMP allocations.
* Rendering/GPU: present/vsync waits vs actual script time.
* GC Alloc in hover, targeting, hit popup, and turn start/end.
