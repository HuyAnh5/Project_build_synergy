# Duplicate Logic Report

This report tracks duplicated logic found under `Assets/Scripts`.

## Highest Priority Duplicates

| Area | Files | Duplication | Refactor direction | Status |
|---|---|---|---|---|
| Combat status row rendering | `ActorWorldUI.StatusIntent.cs`, `CombatHUD.PlayerVitals.cs` | Slot show/hide, background color, icon sprite/enabled/color, short label, value text | Shared `CombatStatusRowRenderer` | Started in P0-1 |
| HP/Guard UI | `ActorWorldUI.cs`, `CombatHUD.PlayerVitals.cs` | HP text/fill/outline/guard root logic | Shared vitals view/apply-state helper later | Not started |
| Status tooltip content | `ActorWorldUI.StatusIntent.cs`, `CombatHUD.PlayerVitals.cs` | Guard/stagger/status tooltip text patterns | Shared tooltip content builder after status row renderer stabilizes | Not started |
| Target/resource preview | `TargetClickable2D.cs`, `SkillIconPreviewController.cs`, `ActorWorldUI.Preview.cs`, `CombatHUD.PlayerVitals.cs`, `TargetPreviewBuilder.cs` | Preview discovery, preview show/clear, HUD resource preview | Shared `CombatPreviewPresenter` | Not started |
| Tooltip layout/keyword tooltip | `SkillTooltipUI*`, `ActorWorldKeywordTooltipUI.cs`, `ConsumableBarUIManager.TooltipPresentation.cs`, `DiceDraggableUI.Tooltip.cs` | Popup positioning, keyword tooltip display, content refresh | Shared tooltip presenter/positioner | Not started |
| Dice resolution/payment | `DiceSlotRig.Resolve.cs`, `DiceCombatEnchantRuntimeUtility*.cs`, `TurnManagerCombatUtility.cs`, `SkillRuntimeEvaluator.DiceGather.cs`, `SkillPlanBoardStateUtility.cs` | Dice sums, crit/fail flags, payment masks, preview breakdowns | Shared dice evaluation snapshot/service | Not started |
| Drag/select UI item behavior | `DraggableSkillIcon*`, `DiceDraggableUI*`, `ConsumableBarUIManager.LayoutDrag.cs`, `DiceEquipUIManager.Selection.cs` | Selection state, drag state, hover/snap/tween behavior | Shared small input helpers only after view split | Not started |

## Completed Duplicate Reduction

### P0-1: Combat status row rendering

Changed files:

* `Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs`
* `Assets/Scripts/UI/Combat/CombatHUD.PlayerVitals.cs`

What changed:

* Shared internal `CombatStatusRowRenderer.Apply` now owns common status slot rendering.
* Actor world UI and player HUD still build their own buffers and tooltip content.
* No gameplay, scene, prefab, or serialized field changes.

Why this is intentionally small:

* Build buffer logic contains player/enemy-specific labels and preview data.
* Tooltip logic should be extracted separately to reduce risk.

### Hot lookup registry consolidation

Changed files include:

* `ActorWorldUI.cs`
* `TargetClickable2D.cs`
* `SkillIconPreviewController.cs`
* `DraggableSkillIcon.cs`
* `DraggableSkillIcon.Interaction.cs`
* `DiceDraggableUI.cs`
* `TurnManagerViewUtility.cs`
* `DiceSlotRig.ConsumePreview.cs`
* `DiceSpinnerGeneric.Visuals.cs`
* `PlayerDiceCastAnimator.Utility.cs`
* `CombatActor.cs`
* `SkillGameplayResolver.Targeting.cs`
* `SkillBehaviorRuntimeUtility.cs`
* `EnemyTurnCoordinator.cs`
* `TurnManagerCombatUtility.cs`
* `ActorWorldUI.StatusIntent.cs`
* `DamagePopupSystem.cs`
* `CombatHUD.cs`
* `PassiveSystem.cs`
* `SkillExecutor.cs`

What changed:

* Repeated local scene scans for the same runtime objects were consolidated behind small internal registries.
* Callers now use shared lookup paths for actor world UI, skill icons, dice UI cards, combat actors, and popup systems.
* No gameplay calculations were changed.

## Next Duplicate Target

Recommended next safe target:

* Extract shared tooltip/keyword tooltip/positioning helpers, starting with content-signature/position-only update helpers.

Reason:

* Tooltip layout and keyword tooltip logic still overlaps across skill, actor/world, consumable, and dice UI.
* This is the next high-value duplicate area after status rows and hot lookup consolidation.
