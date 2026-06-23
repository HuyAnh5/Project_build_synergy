# Duplicate Logic Report

This report tracks duplicated logic found under `Assets/Scripts`.

## Highest Priority Duplicates

| Area | Files | Duplication | Refactor direction | Status |
|---|---|---|---|---|
| Combat status row rendering | `ActorWorldUI.StatusIntent.cs`, `CombatHUD.PlayerVitals.cs` | Slot show/hide, background color, icon sprite/enabled/color, short label, value text | Shared `CombatStatusRowRenderer` | Completed first slice |
| HP/Guard UI | `ActorWorldUI.cs`, `CombatHUD.PlayerVitals.cs` | HP text/fill/outline/guard root logic | Shared vitals apply-state helper later | Not started |
| Status tooltip content | `ActorWorldUI.StatusIntent.cs`, `CombatHUD.PlayerVitals.cs` | Guard/stagger/status tooltip text patterns | Shared tooltip content builder after status row renderer stabilizes | Not started |
| Target/resource preview | `TargetClickable2D.cs`, `SkillIconPreviewController.cs`, `ActorWorldUI.Preview.cs`, `CombatHUD.PlayerVitals.cs`, `TargetPreviewBuilder.cs` | Preview discovery, preview show/clear, HUD resource preview | Shared `CombatPreviewPresenter` | Not started |
| Tooltip layout/keyword tooltip | `SkillTooltipUI*`, `ActorWorldKeywordTooltipUI.cs`, `ConsumableBarUIManager.TooltipPresentation.cs`, `DiceDraggableUI.Tooltip.cs` | Popup positioning, keyword tooltip display, content refresh | Shared tooltip presenter/positioner and content signatures | Partially started via canvas lookup |
| Dice resolution/payment | `DiceSlotRig.Resolve.cs`, `DiceCombatEnchantRuntimeUtility*.cs`, `TurnManagerCombatUtility.cs`, `SkillRuntimeEvaluator.DiceGather.cs`, `SkillPlanBoardStateUtility.cs` | Dice sums, crit/fail flags, payment masks, preview breakdowns | Shared dice evaluation snapshot/service | Not started |
| Drag/select UI item behavior | `DraggableSkillIcon*`, `DiceDraggableUI*`, `ConsumableBarUIManager.LayoutDrag.cs`, `DiceEquipUIManager.Selection.cs` | Selection state, drag state, hover/snap/tween behavior | Shared small input helpers only after view split | Not started |

## Completed Duplicate Reduction

### Combat status row rendering

Changed:

* `ActorWorldUI.StatusIntent.cs`
* `CombatHUD.PlayerVitals.cs`

What changed:

* Shared internal `CombatStatusRowRenderer.Apply` now owns common status slot rendering.
* Actor world UI and player HUD still build their own buffers and tooltip content.
* No gameplay, scene, prefab, or serialized field changes.

### Hot lookup consolidation

Repeated local scene scans were consolidated behind small internal registries/caches for:

* actor world UI
* skill icons
* dice cards
* combat actors
* popup system
* turn manager
* party manager
* combat HUD
* dice equip manager
* consumable bar manager
* dice slot rig
* gameplay dice edit controller
* run inventory manager
* self-cast drop zone
* skill tooltip prefab provider

This is duplicate infrastructure reduction: callers now use shared lookup paths instead of each owning slightly different scene-search code.

### Canvas lookup consolidation

`SceneCanvasLookup` centralizes canvas discovery for tooltip/popup/visual placement paths.

Covered:

* `SkillTooltipUI`
* `ActorWorldKeywordTooltipUI`
* `DamagePopupSystem`
* `DiceSpinnerGeneric.Visuals`

`SkillTooltipPrefabProviderRegistry` centralizes tooltip prefab provider discovery for skill and actor keyword tooltip systems.

### Consumable hover presentation gate

`ConsumableBarUIManager` now uses a single `SetHoveredSlot` path so repeated enter/update/drag-clear events do not refresh floating tooltip/action/presentation layers when hover state is unchanged.

## Next Duplicate Targets

Recommended next safe target:

* Tooltip/keyword tooltip/layout helpers.

Reason:

* Skill tooltip, actor keyword tooltip, consumable tooltip, and dice tooltip still duplicate positioning/content-refresh patterns.
* Canvas lookup is now shared, so the next step can focus on content signatures and position-only updates.

Next medium-risk target:

* Shared `CombatPreviewPresenter` for target/resource preview.

Reason:

* Target previews are spread across target clickables, skill icon preview controller, HUD, actor world UI, and target preview builder.
* This touches gameplay-adjacent preview behavior, so it should be done after tooltip cleanup or with very explicit manual tests.
