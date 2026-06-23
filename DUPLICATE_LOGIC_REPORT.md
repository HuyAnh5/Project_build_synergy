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

## Next Duplicate Target

Recommended next safe target:

* Add a registry/cache for `ActorWorldUI` and possibly `CombatHUD` lookup in preview/targeting paths.

Reason:

* Many hot/preview paths still use scene-wide lookup.
* A registry can reduce FPS risk without changing gameplay rules.
