# FPS Risk Report

This report tracks likely FPS and frame-time jitter risks under `Assets/Scripts`.

## High-Risk Patterns

| Pattern | Current examples | Risk | Suggested mitigation |
|---|---|---|---|
| Per-frame UI polling | `ActorWorldUI.LateUpdate`, `CombatHUD.Update`, `ConsumableBarUIManager.Update`, `DraggableSkillIcon.Update`, `DiceDraggableUI.Update`, `DiceEquipUIManager.LateUpdate` | Baseline jitter, repeated UI churn | Dirty flags, state snapshots, event-driven refresh |
| Scene-wide lookup in hot/preview paths | `TargetClickable2D`, `SkillIconPreviewController`, `CombatHUD`, `ConsumableBarUIManager.Presentation`, `SkillGameplayResolver.Targeting`, `SkillExecutor` | Spikes during hover/target/preview/cast | Registries/caches, dependency injection, cached owner references |
| Tooltip/layout rebuild | `SkillTooltipUI*`, `ActorWorldKeywordTooltipUI`, `ConsumableBarUIManager.TooltipPresentation`, `DiceDraggableUI.Tooltip` | TMP/layout spikes while hovering | Content signatures, position-only updates, shared tooltip presenter |
| Popup/tween bursts | `DamagePopupSystem`, `CombatHitFeedback`, `PlayerDiceCastAnimator`, `DiceSpinnerGeneric.RollPopup` | Hit/status bursts can create frame spikes | Pooling caps, batching, shorter active counts, avoid runtime particle creation |
| Runtime object creation | `new GameObject`, `AddComponent` in feedback/setup/runtime UI paths | GC/allocation and initialization spikes | Prewarm/pool or move to setup-only paths |

## Current Hot Lookup Findings

Examples found in `Assets/Scripts`:

* `TargetClickable2D` repeatedly finds `TurnManager`, `ConsumableBarUIManager`, `CombatHUD`, and actor world UIs.
* `SkillIconPreviewController` caches but still discovers `ActorWorldUI` and `CombatHUD`.
* `ConsumableBarUIManager.Presentation` auto-resolves several managers.
* `SkillGameplayResolver.Targeting` uses actor discovery in resolver paths.
* `CombatActor`, `SkillExecutor`, `PassiveSystem`, and `CombatHUD` find `DamagePopupSystem`.

## Completed FPS-Adjacent Work

P0-1 is primarily maintainability work, not a major FPS fix. It reduces duplicate status slot rendering code and centralizes the apply path, which prepares future state-driven UI work.

## Recommended Next FPS Batch

Create a small `ActorWorldUI` registry/cache.

Candidate files:

* `Assets/Scripts/UI/Combat/ActorWorldUI.cs`
* `Assets/Scripts/UI/Combat/TargetClickable2D.cs`
* `Assets/Scripts/UI/Planning/SkillIconPreviewController.cs`

Goal:

* Reduce repeated scene-wide `ActorWorldUI` discovery in target/preview paths.

Constraints:

* Do not change target selection rules.
* Do not change preview output.
* Keep registration lifecycle simple: register on enable/bind, unregister on disable/destroy.

## Unity Profiling Checklist

After each UI/FPS batch, profile:

* CPU Usage: Scripts, UI, Rendering.
* UI: Canvas rebuilds, Layout rebuilds, TMP allocations.
* Rendering/GPU: present/vsync waits vs actual script time.
* GC Alloc in hover, targeting, hit popup, and turn start/end.
