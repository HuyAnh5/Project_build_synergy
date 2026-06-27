# Duplicate Logic Report

This report tracks duplicated logic found under `Assets/Scripts`.

## Highest Priority Duplicates

| Area | Files | Duplication | Refactor direction | Status |
|---|---|---|---|---|
| Combat status row rendering | `ActorWorldUI.StatusIntent.cs`, `CombatHUD.PlayerVitals.cs` | Slot show/hide, background color, icon sprite/enabled/color, short label, value text | Shared `CombatStatusRowRenderer` | Completed first slice |
| HP/Guard UI | `ActorWorldUI.cs`, `CombatHUD.PlayerVitals.cs` | HP text/fill/outline/guard root logic | Shared vitals apply-state helper later | Partially reduced via `CombatUiDirtySetUtility` |
| Status tooltip content | `ActorWorldUI.StatusIntent.cs`, `CombatHUD.PlayerVitals.cs` | Guard/stagger/status tooltip text patterns | Shared tooltip content builder after status row renderer stabilizes | Not started |
| Target/resource preview | `TargetClickable2D.cs`, `SkillIconPreviewController.cs`, `ActorWorldUI.Preview.cs`, `CombatHUD.PlayerVitals.cs`, `TargetPreviewBuilder.cs` | Preview discovery, preview show/clear, HUD resource preview | Shared `CombatPreviewPresenter` | Not started |
| Tooltip layout/keyword tooltip | `SkillTooltipUI*`, `ActorWorldKeywordTooltipUI.cs`, `ConsumableBarUIManager.TooltipPresentation.cs`, `DiceDraggableUI.Tooltip.cs` | Popup positioning, keyword tooltip display, content refresh | Shared tooltip presenter/positioner and content signatures | Partially reduced via canvas lookup, content signatures, dirty setters, and TMP mesh cache |
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

Follow-up cleanup removed the now-duplicated per-tooltip private provider caches and `GetPrefabProvider()` wrappers, so provider lookup has one owner.

The skill keyword tooltip prefab getter was also simplified to resolve layout, registry provider, then settings once per access instead of calling the registry repeatedly inside a nested expression.

### Consumable hover presentation gate

`ConsumableBarUIManager` now uses a single `SetHoveredSlot` path so repeated enter/update/drag-clear events do not refresh floating tooltip/action/presentation layers when hover state is unchanged.

### Dirty UI apply helper

`CombatUiDirtySetUtility` now centralizes common "set only if changed" operations for active state, TMP text, graphic color, outline color, and image fill amount.

Covered areas include:

* actor world HP/Guard preview/runtime UI
* player HUD HP/Guard UI
* actor keyword tooltip views
* skill tooltip layout/keyword tooltip views
* consumable slots, action buttons, tooltip roots, and keyword tooltip views

This does not remove all duplicated presentation code yet, but it reduces repeated canvas/TMP dirtying and gives later presenter extraction a smaller target.

### Tooltip content/mesh gating

Skill and consumable keyword tooltip systems now avoid forcing TMP mesh generation when the same text block content is unchanged.

Skill, actor, and consumable keyword tooltip views also use content signatures so layout rebuilds happen mainly when content changes or a view is newly shown.

## Next Duplicate Targets

Recommended next safe target:

* Tooltip/keyword tooltip/layout helpers.

Reason:

* Skill tooltip, actor keyword tooltip, consumable tooltip, and dice tooltip still duplicate positioning/content-refresh patterns.
* Canvas lookup, dirty setters, content signatures, and TMP mesh caching are now shared/partially applied, so the next step can focus on extracting an actual shared presenter/positioner.

### Combat target/guard preview presentation

First shared-preview slice is now complete:

* `CombatTargetPreviewPresenter` centralizes target preview show/clear across `ActorWorldUI` and `CombatHUD`.
* `CombatGuardPreviewUtility` centralizes guard die-value resolution and self-guard final preview data.
* `CombatPreviewBundleUtility.BuildActionBundleWithSelfGuard()` centralizes the duplicated bundle-build flow used by `TargetClickable2D` and `SkillIconPreviewController`.
* Removed leftover private self-guard wrapper helpers from `TargetClickable2D` after call sites moved to the shared utility.

Remaining preview duplication:

* Resource/focus preview ownership is still split between skill icons, target clickables, and HUD.
* Target input/raycast/drop handling is still owned by `TargetClickable2D` and should not be moved until covered by manual tests.

Next medium-risk target:

* Shared resource/focus preview owner for `SkillIconPreviewController`, `TargetClickable2D`, and `CombatHUD`.

Reason:

* Target preview presentation is now shared, but resource preview ownership and restore behavior still has duplicated edge-case logic.
* This touches hover/drag/select behavior, so it should be staged with explicit Unity tests.
