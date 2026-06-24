# Autonomous Refactor Summary

This file is the live handoff for the staged Unity C# refactor.

## Current Status

Status: build/compile check passes.

Latest verification command:

* `dotnet build Project_build_synergy.sln`

Latest result:

* 0 errors
* 35 warnings

The remaining warnings are currently treated as non-blocking:

* Odin `ListDrawerSettingsAttribute.Expanded` obsolete warnings in data/definition files.
* Editor-only obsolete `FindObjectOfType` warnings.
* Editor-only TMP `enableWordWrapping` warnings.

No gameplay behavior was intentionally changed.

## Git / Staging Status

The user asked to continue without staging every step. No `git add`, commit, or push was performed in this continuation.

Untracked tooling/generated files were intentionally not touched:

* `.cgcignore`
* `.serena/`
* `.understand-anything/`
* `DemoWeb/Buildemo/`
* `mcp.json`
* `unity_batchmode_combatlab_setup.log`

## Completed Work

### P0-1: shared combat status row rendering

Changed:

* Extracted duplicate status slot apply/render behavior into shared internal `CombatStatusRowRenderer`.
* `ActorWorldUI` and `CombatHUD` now share the same status slot rendering helper.
* Tooltip/content building remains separate to avoid mixing behavior changes into the first slice.

Primary files:

* `Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs`
* `Assets/Scripts/UI/Combat/CombatHUD.PlayerVitals.cs`

### Hot lookup registry/cache batches

Added small internal registries/caches to reduce repeated scene-wide lookup in runtime, preview, and UI paths.

Registries added or expanded:

* `ActorWorldUiRegistry`
* `DraggableSkillIconRegistry`
* `DiceDraggableUiRegistry`
* `CombatActorRegistry`
* `DamagePopupSystemRegistry`
* `TurnManagerRegistry`
* `BattlePartyManagerRegistry`
* `CombatHudRegistry`
* `DiceEquipUiManagerRegistry`
* `ConsumableBarUiManagerRegistry`
* `DiceSlotRigRegistry`
* `GameplayDiceEditControllerRegistry`
* `RunInventoryManagerRegistry`
* `SelfCastDropZoneRegistry`
* `SkillTooltipPrefabProviderRegistry`

Design:

* Register on normal Unity lifecycle entry points where available.
* Unregister on destroy/disable where appropriate.
* Preserve inactive-object lookup semantics when old code used inactive-inclusive search.
* Keep fallback scene scan for early initialization or missing registry state.
* Keep helpers internal and colocated in existing compiled files to avoid Unity `.csproj` regeneration issues during dotnet-only checks.

### Canvas lookup cache

Added `SceneCanvasLookup` inside `Assets/Scripts/UI/Tooltips/SkillTooltipUI.cs`.

Used by:

* `SkillTooltipUI`
* `ActorWorldKeywordTooltipUI`
* `DamagePopupSystem`
* `DiceSpinnerGeneric.Visuals`

Effect:

* Avoids repeated canvas scene scans in the same frame.
* Refreshes once on name miss so newly-created canvases can still be found.

### UI polling/churn reductions

Changed:

* `CombatHUD.Update()` no longer re-ensures player focus/vitals UI every frame when references are already valid.
* `CombatHUD.PlayerVitals` ensures vitals UI only when references are missing.
* `ActorWorldUI.LateUpdate()` no longer resolves runtime references every frame when they are already valid.
* `ConsumableBarUIManager` hover handling now avoids refreshing tooltip/action/presentation layers when hovered slot state does not actually change.
* `ConsumableBarUIManager` slot/action/tooltip presentation now skips unchanged `SetActive`, TMP text, image color, icon sprite/enabled, and action-button label/color writes.
* Consumable tooltip auto-sizing now runs only when the tooltip was newly shown or tooltip title/body content changed.
* `DraggableSkillIcon.Update()` now throttles idle metadata refresh while keeping hover/selected/drag states immediate.
* `DiceDraggableUI.Update()` now throttles idle pointer safety polling while keeping hover, drag, and hold-inspect states immediate.
* `SkillTooltipUI` and `ActorWorldKeywordTooltipUI` now resolve `SkillTooltipPrefabProvider` through a shared registry/cache instead of direct scene scans.
* Skill and consumable keyword tooltip link detection now caches TMP mesh generation by text content so pointer movement over unchanged text does not force a mesh rebuild each frame.

Left intentionally unchanged:

* `ActorWorldUI.RefreshIntent()` still runs through the normal runtime refresh path because intent can depend on dynamic combat/player state.
* `DiceEquipUIManager.LateUpdate()` still mirrors world dice roots to the live UI every frame when that mirror feature is enabled; changing that requires a deeper visual-contract decision.

### Guard-hit FPS mitigation

Changed:

* `DamagePopupSystem.SpawnGuardSCurve()` was changed from a trigonometric S-curve tween to a lightweight cubic S-curve plus fade/scale sequence.
* `CombatHitFeedback` no longer spawns procedural runtime particle bursts for `FeedbackKind.Guard`.
* Player HUD and actor world UI Guard roots now skip redundant `SetActive` calls when Guard remains visible/hidden.
* Player HUD and actor world UI HP/Guard refresh now avoid reassigning unchanged TMP text, colors, outlines, and fill amounts.

Why:

* HP damage did not show the same drop because it used the HP popup path only.
* Damage blocked by Guard additionally showed a Guard popup and could run Guard feedback.
* The old Guard popup tween did extra per-update math and the Guard feedback path could create a `GameObject` + `ParticleSystem` at runtime.

Gameplay/visual intent:

* Guard still blocks damage.
* Guard blocked amount popup still appears with a curved path.
* Guard feedback still flashes blue.
* The expensive Guard-only burst, DOTween path allocation, redundant UI dirtying, and per-frame trigonometry were reduced to avoid hit-time spikes.

### Warning/dead no-op cleanup

Changed:

* `TurnManager.ResolveContinueButtonUi()` now avoids repeated all-button scans after a miss.
* Runtime TMP wrapping calls were modernized where safe.
* `PassiveSystem.Rebuild()` no longer calls a private no-op `Accumulate()` path.
* Dice edit debug gates were changed from `const bool false` to `static readonly bool false` to avoid unreachable-code warnings while preserving the disabled debug path.
* `MapPrototypeController.UiBuild.LogMap()` now respects existing serialized `verboseLogging`.
* Removed redundant per-tooltip `SkillTooltipPrefabProvider` private caches/helpers after introducing the shared provider registry.
* Replaced a local ignored `unusedSkip` variable with an out discard in player turn-start status ticking.
* Removed additional private unused helpers/fields from target clickable, skill keyword tooltip fallback, dice world tooltip, dice enchant hover-zone setup, passive context building, and dice face icon cache paths.

Latest cleanup verification:

* `dotnet build Project_build_synergy.sln`
* 0 errors
* 35 warnings

## Files Modified

The current dirty code set includes refactor changes in these areas:

* Combat actor/party registries and lookup call sites.
* Turn manager registry and continue-button lookup guard.
* Skill execution/runtime lookup call sites.
* Dice slot, dice UI, and dice edit registry/lookup call sites.
* Inventory, run, and consumable runtime lookup call sites.
* Combat HUD, actor world UI, target UI, tooltip, popup, and consumable bar UI.
* Map prototype UI logging/wrapping cleanup.
* Report docs.

Use `git status --short` and `git diff --stat` for the exact current file list.

## Files Created

No new gameplay `.cs` files were created in the latest continuation. Shared helpers were placed inside existing compiled files.

Report/planning files already created or updated:

* `AGENTS.md`
* `ARCHITECTURE.md`
* `REFACTOR_PLAN.md`
* `DEAD_CODE_CANDIDATES.md`
* `DUPLICATE_LOGIC_REPORT.md`
* `FPS_RISK_REPORT.md`
* `AUTONOMOUS_REFACTOR_SUMMARY.md`

## Deleted

No files were deleted in this continuation.

The only code deletion was a verified private no-op path in `PassiveSystem`.

## Not Deleted Because Risk Is Unclear

* Prototype/demo systems.
* Dice edit sandbox systems.
* Editor setup tools.
* Public methods that may be referenced by UnityEvent, scenes, prefabs, animation events, or reflection.
* Serialized fields.

## Remaining High-Value Refactor Tasks

1. Tooltip/keyword tooltip/layout consolidation.
2. Further dirty-flag reduction in `ConsumableBarUIManager`, `DraggableSkillIcon`, `DiceDraggableUI`, and `DiceEquipUIManager`.
3. Shared target/resource preview presenter for `TargetClickable2D`, `SkillIconPreviewController`, `CombatHUD`, and `ActorWorldUI`.
4. Dice payment/value/crit/fail snapshot service.
5. Runtime/preview convergence around `SkillExecutor`, `TargetPreviewBuilder`, and `SkillGameplayResolver`.
6. Classification of prototype/demo/dice-edit systems before moving or deleting anything.

## Remaining Risks

* FPS root cause may still be render/GPU/presentation wait rather than C# script time.
* UI tooltip/layout rebuilds remain a likely script-side spike source.
* Popup/tween bursts still need profiling under real combat hit/status scenarios.
* Registries reduce repeated scene scanning but do not replace proper explicit dependencies everywhere.
* Prototype/demo code still adds maintenance noise and still needs classification.

## Unity Manual Tests Needed

Please test these in Unity after pulling/opening the refactor work:

1. Enter combat from normal flow.
2. Verify player HUD HP, guard, focus/AP, status row, and status tooltip.
3. Verify enemy/world UI HP, guard, status row, intent, intent tooltip, and target overlay.
4. Hover/select/drag skills over enemies and the self-cast zone; previews should appear and clear correctly.
5. Roll dice; verify dice result UI, consume preview/tints, crit/fail labels, and roll popups.
6. Hover/click/use consumables; verify tooltip, keyword tooltip, action panel, sell/use buttons, target consumables, and Zodiac/dice-edit consumables if available.
7. Cast damage/heal/focus actions; verify popups and hit feedback.
8. End turn, enemy turn, next player turn; verify views refresh and dead actors are not targetable.
9. If used, open dice edit/sandbox UI and verify selection, panel, and consumable interaction.
10. If used, open map/prototype flow and verify map UI still builds; `verboseLogging` should gate map logs.
