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

## Completed Hot Lookup Work

The autonomous continuation added small registries/caches for hot UI/runtime lookup targets:

* `ActorWorldUI` registry for target/preview UI lookup.
* `DraggableSkillIcon` registry for icon dimming and pulse feedback.
* `DiceDraggableUI` registry for dice dimming, consume preview, dice roll popup anchoring, and cast animation UI lookup.
* `CombatActor` registry for skill targeting/counting utilities and turn fallback utilities.
* `DamagePopupSystem` registry for damage/heal/focus popup callers.

Latest compile check:

* `dotnet build Project_build_synergy.sln`
* 0 errors.
* Existing warnings reduced to 69.

Remaining notable lookup risks:

* Several singleton-like manager lookups remain (`TurnManager`, `BattlePartyManager2D`, `DiceEquipUIManager`, `RunInventoryManager`).
* Canvas discovery remains in tooltip/popup placement paths.
* Some prototype/sandbox systems still use scene-wide lookup and should be classified before cleanup.

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
