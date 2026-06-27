# Refactor Plan

This document captures the current refactor audit for Assets/Scripts.

Scope rules for this audit:

* Only Assets/Scripts was inspected.
* Library, Temp, Obj, Build, Builds, Logs, .git, .vs, and UserSettings are out of scope.
* No gameplay code was changed while creating this plan.
* Refactor must be staged; do not rewrite the whole project at once.
* Preserve current gameplay behavior.

## Summary

The main problem is not simply that the project has many files. The larger problem is that many partial class groups are still god objects: files are split, but private state and responsibilities are shared across many partial files.

The highest-risk areas are combat UI polling, tooltip/layout rebuilds, repeated scene-wide lookup, target preview duplication, and runtime/preview rule divergence.

## Audit Table

| File | Current responsibility | Problem | Duplicated logic | FPS risk | Dependent files/systems | Refactor proposal | Priority |
|---|---|---|---|---|---|---|---|
| Assets/Scripts/UI/Combat/ActorWorldUI.cs + .Setup.cs + .StatusIntent.cs + .Preview.cs | World UI for actor/enemy: bind actor, follow anchor, HP/Guard, status icons, intent, tooltip, target overlay, preview | One class owns too many responsibilities; LateUpdate still handles attach/resolve/tooltip/overlay/preview | Status icon rendering duplicates CombatHUD.PlayerVitals; tooltip logic overlaps HUD/SkillTooltip; target preview overlaps TargetClickable2D/SkillIconPreviewController | High: per-enemy LateUpdate, UI SetActive/text/color, tooltip hover checks, preview lookup paths | BattlePartyManager2D, TurnManagerViewUtility, TargetClickable2D, SkillIconPreviewController, CombatHUD.PlayerVitals, editor setup tools | Split into ActorWorldUiController, ActorWorldVitalsView, ActorWorldStatusRowView, ActorWorldIntentView, ActorWorldTooltipPresenter, ActorTargetOverlayView; use state snapshots/apply-if-changed | P0 |
| Assets/Scripts/UI/Combat/CombatHUD.cs + CombatHUD.PlayerVitals.cs | Player HUD: focus/AP bar, HP/Guard, statuses, tooltip, resource preview | Player vitals duplicate enemy/world vitals patterns; Update still performs ensure/tooltip work every frame | Status icon build/apply, guard/stagger tooltip, status signature, HP/guard render duplicate ActorWorldUI | Medium-high: Update every frame, tooltip hover/content rebuild path | DiceSlotRig.ConsumePreview, SkillIconPreviewController, TargetClickable2D, ConsumableBarUIManager | Split PlayerFocusView, PlayerVitalsView; share status row renderer/tooltip content builder with ActorWorldUI | P0 |
| Assets/Scripts/UI/Combat/ConsumableBarUIManager.cs + .Presentation.cs + .TooltipPresentation.cs + .Interaction.cs + .Consume.cs + .LayoutDrag.cs + .SlotViews.cs | Consumable row: inventory binding, slots, hover tooltip, action panel, drag/drop, execute consumable, reward-choice mode | Very large partial god object; selection/hover/drag/use/presentation state is mixed | Tooltip/action panel positioning overlaps other UI; selection model overlaps dice/skill UI; auto-resolve patterns repeat | High when tooltip/panel active: SetActive, layout rebuild, preferred values, keyword tooltip, sibling ordering | RunInventoryManager, TurnManager, CombatHUD, DiceEquipUIManager, GameplayDiceEditController, TargetClickable2D, reward prototype | First split ConsumableTooltipPresenter and ConsumableActionPanelPresenter; later split ConsumableBarSelectionModel, ConsumableSlotListView, ConsumableUseController | P0 |
| Assets/Scripts/UI/Planning/DraggableSkillIcon.cs + .Interaction.cs + .Drag.cs + .Badges.cs + .ActiveAura.cs | Skill icon UI: inventory binding, drag/drop, click select, tooltip, preview, active aura, badges | Icon is simultaneously view, input controller, preview owner, tooltip source, runtime active-state watcher | Drag/selection overlaps DiceDraggableUI; tooltip preview overlaps SkillIconPreviewController/TargetClickable2D | High if many icons: per-icon Update, metadata polling, aura tween, preview tick, pointer bridge check | RunInventoryManager, TurnManager, UiDragState, TargetingArrowUI, TargetClickable2D, SkillTooltipUI, SkillIconPreviewController | Split SkillIconView, SkillIconDragController, SkillIconSelectionController, SkillIconRuntimeStateWatcher; convert metadata refresh to dirty/event | P1 |
| Assets/Scripts/UI/Planning/SkillIconPreviewController.cs + .GuardPreview.cs | Preview skill target/resource/guard while hovering/dragging skill | Finds HUD/world UI and builds presentation itself | Overlaps TargetClickable2D.ShowBundlePreviews, CombatHUD resource preview, ActorWorldUI target preview | Medium-high: FindObjectsByType, preview refresh during hover/drag | DraggableSkillIcon, ActorWorldUI, CombatHUD, TargetPreviewBuilder, TurnManager | Create shared CombatPreviewPresenter for hover/drop/click target previews | P1 |
| Assets/Scripts/UI/Combat/TargetClickable2D.cs | Click/drop/hover target handling, target selection, preview build/clear | Input, validation, preview bundle, HUD preview, and UI lookup are mixed | Preview target overlaps SkillIconPreviewController; repeated FindObjectsOfType ActorWorldUI | High in targeting/hover paths | ActorWorldUI, CombatHUD, TurnManager, UiDragState, TargetPreviewBuilder, ConsumableBarUIManager | Split TargetInputHandler and CombatTargetPreviewPresenter; use cached ActorWorldUI registry | P0/P1 |
| Assets/Scripts/UI/Combat/ActorWorldKeywordTooltipUI.cs | Keyword tooltip for ActorWorldUI/HUD status and intent | Separate tooltip implementation from skill/consumable tooltip systems | Overlaps SkillTooltipUI keyword/layout/positioning | Medium-high when tooltip active | ActorWorldUI.StatusIntent, CombatHUD.PlayerVitals | Extract shared KeywordTooltipPresenter or merge gradually with common tooltip infrastructure | P1 |
| Assets/Scripts/UI/Tooltips/SkillTooltipUI.cs + subfiles | Skill tooltip popup, keyword glossary, layout, positioning, hover bridge | Large static/global tooltip object; many concerns in one class | Tooltip layout/keyword logic overlaps ActorWorldKeywordTooltipUI and consumable tooltip | High when tooltip visible: keyword updates, pointer checks, layout refresh | DraggableSkillIcon, SkillTooltipFormatter, SkillTooltipPrefabProvider, UiDragState | Split TooltipRootView, KeywordTooltipPresenter, TooltipPositioner, SkillTooltipContentBuilder | P1 |
| Assets/Scripts/UI/Combat/DamagePopupSystem.cs + .Animations.cs | Pool/spawn/animate damage/heal/focus popups | Pool exists, but active count/duration/backpressure are not clearly bounded | Popup/tween feedback overlaps dice roll popup and hit feedback | High during multi-hit/status popup bursts: TMP + DOTween + canvas sorting | CombatActor, PassiveSystem, SkillExecutor, combat feedback | Split DamagePopupPool, DamagePopupPresenter; add active cap/batching if profiler confirms | P1 |
| Assets/Scripts/UI/Loadout/Dice/DiceEquipUIManager.cs + .VisualLayout.cs + .Selection.cs | Dice equip UI: layout, drag reorder, inventory sync, combat visual state, world-slot mirror | Manager owns inventory sync, selection, drag, runtime combat visual, mirror world slots | Drag/selection overlaps DiceDraggableUI/DraggableSkillIcon; sync outputs partly duplicated with utility | High if active in combat: LateUpdate refreshes combat dice state and world sync | RunInventoryManager, DiceSlotRig, DiceDraggableUI, ConsumableBarUIManager, GameplayDiceEditController | Split DiceEquipSelectionModel, DiceEquipRuntimeVisualSync, DiceEquipWorldMirror; gate LateUpdate by active/mode | P1 |
| Assets/Scripts/UI/Loadout/Dice/DiceDraggableUI.cs + .Visuals.cs + .Tooltip.cs + .EnchantHoverZone.cs | Dice UI card: drag, click, selection, hover tooltip, hold inspect, combat visual state, tween feedback | One UI item owns too many UI/input/feedback concerns | Tooltip/drag/selection/tween feedback overlaps skill icon/consumable UI | High if many cards: per-item Update, active tooltip refresh, tweens/shakes | DiceEquipUIManager, DiceSpinnerGeneric, SkillTooltipUI, dice edit systems | Split DiceCardView, DiceCardDragHandler, DiceCardTooltipPresenter, DiceCombatVisualStateView | P1 |
| Assets/Scripts/Dice/DiceSpinnerGeneric.cs + subfiles | Dice runtime/visual: roll, face state, face preview, world tooltip, result popup, renderers | Runtime dice logic and visual/tooltip/popup/tween behavior are coupled | Roll feedback/value text/tooltip/popup overlaps DiceDraggableUI/DamagePopupSystem | Medium-high: world tooltip update per die; roll animation/tween spikes | DiceSlotRig, DiceEquipUIManager, SkillRuntimeEvaluator, DiceCombatEnchantRuntimeUtility, dice edit | Separate runtime model from visual presenters: DiceRollState, DiceFaceState, DiceWorldTooltipPresenter, DiceRollFeedbackView | P1 |
| Assets/Scripts/Dice/DiceSlotRig.cs + .Rolling.cs + .Runtime.cs + .Resolve.cs + .ConsumePreview.cs | Dice slot owner: assign slots, roll all, resolve values, consume preview, events | Rig is data owner, debug input owner, resolver, and preview HUD bridge | Dice value resolution overlaps TurnManagerCombatUtility, SkillRuntimeEvaluator.DiceGather, DiceCombatEnchantRuntimeUtility.Preview | Medium: debug Update is low, but resolve/preview paths are frequent | TurnManager, SkillRuntimeEvaluator, DiceCombatEnchantRuntimeUtility, CombatHUD, DiceEquipUIManager | Create shared DiceResolutionService/DicePaymentPlanService; split consume preview UI bridge | P1 |
| Assets/Scripts/Combat/Turn/TurnManager.cs + partials | Combat phase, planning, dice roll, command queue, enemy turn, UI refresh orchestration | Central class is too broad; many RefreshAllViews entry points; UI orchestration mixed with turn state | Skill preview refresh, dice dim, icon dim, target validation overlap utility/UI code | Medium: not heavy per frame, but broad refreshes happen from many events | CombatHUD, ActionSlotDrop, SkillPlanBoard, DiceSlotRig, SkillExecutor, EnemyTurnCoordinator, UI managers | Split CombatPhaseController, PlanningUiRefreshCoordinator, PlayerCommandQueue; keep public API stable | P1 |
| Assets/Scripts/Combat/Execution/SkillExecutor.cs + subfiles | Execute skill, cost, projectile/melee animation, damage/effect apply, dice cast animation | Many overloads and optional parameters; execution, animation, targeting, popup coupling | Runtime apply/preview rules relate closely to TargetPreviewBuilder/SkillAttackResolutionUtility/SkillGameplayResolver | Medium: coroutine/tween/action bursts; high maintainability risk | TurnManager, SkillAttackResolutionUtility, PlayerDiceCastAnimator, DamagePopupSystem, SkillTargetSelectionService | Introduce SkillExecutionRequest to replace long overloads; separate animation decisions from execution core | P1 |
| Assets/Scripts/Combat/Execution/TargetPreviewBuilder.cs + subfiles | Build target/self/resource previews; simulate final state | Preview simulator clones/copies status and must mirror runtime rules | Runtime effect logic overlaps SkillExecutor, SkillAttackResolutionUtility, SkillGameplayResolver | Medium: preview hover can call often; high divergence risk | TargetClickable2D, SkillIconPreviewController, CombatHUD, ActorWorldUI | Use shared SkillResolvedResult/dry-run path more clearly; separate PreviewPresentationData from simulator | P0/P1 |
| Assets/Scripts/Skills/Runtime/SkillGameplayResolver.cs + .Targeting.cs + .Values.cs | New gameplay pipeline resolver: requirements, targets, effects, values | Large static utility; targeting contains scene-wide actor lookup; context/effect/target/value concerns are mixed | Targeting/status stack/condition context overlaps SkillRuntimeEvaluator, SkillBehaviorRuntimeUtility, TargetPreviewBuilder | Medium-high if called in preview/intent frequently; FindObjectsOfType in resolver | SkillExecutor, TargetPreviewBuilder, AttackPreviewCalculator, SkillTooltipFormatter, TurnManager | Split SkillTargetProvider, SkillEffectResolver, SkillValueResolver; pass combat actor registry/context instead of finding all | P1 |
| Assets/Scripts/Skills/Runtime/SkillRuntimeEvaluator*.cs | Evaluate SkillRuntime from SO + dice/context/conditions | Many overloads and gather methods by scope/payment mask | Dice gather/crit/fail/fail penalty overlaps DiceCombatEnchantRuntimeUtility.Preview, TurnManagerCombatUtility | Medium: called during preview, tooltip, planning refresh | SkillPlanRuntimeUtility, TurnManager, SkillTooltipFormatter, TargetingArrowUI | Add DiceEvaluationSnapshot; reduce overloads with request/context object | P1 |
| Assets/Scripts/Consumables/ConsumableRuntimeUtility.cs | Apply consumable effects | Static utility with many cases; directly reaches TurnManager/dice/status | Target/dice/status resolution overlaps skill runtime/targeting | Medium action burst; maintainability risk | ConsumableBarUIManager, RunInventoryManager, TurnManager, dice edit | Split per-family handlers or light command pattern; keep current family switch initially | P2 |
| Assets/Scripts/Inventory/RunInventoryManager.cs + partials | Inventory/run loadout: skills, dice, passives, consumables, gold, bindings, prefab spawn | Multiple domains in one manager; InventoryChanged event is too broad | UI binding/update logic overlaps utility/UI | Low-medium: broad event causes wide UI refresh | DraggableSkillIcon, DiceEquipUIManager, ConsumableBarUIManager, PassiveSystem, RunManager | Later split specific events: SkillsChanged, DiceChanged, ConsumablesChanged, GoldChanged; preserve serialized fields first | P2 |
| Assets/Scripts/Combat/Actors/CombatActor.cs + CombatHitFeedback.cs | Actor stats/status/resource/damage feedback; hit visual feedback | Actor finds popup/feedback; feedback creates particle objects at runtime | Damage/heal popup path overlaps DamagePopupSystem; hit feedback overlaps popup/tween feedback | Medium-high during hit bursts: FindObject/popup/particle/tween | SkillExecutor, DamagePopupSystem, CombatHitFeedback, StatusController | Add CombatActorEvents or callback; cache popup/feedback service; pool particle burst if needed | P1 |
| Assets/Scripts/DiceEditSandbox/* | Dice edit sandbox runtime, panel, interactable, selection, tooltip, runtime UI | Sandbox/prototype and production gameplay dice edit concerns overlap | Drag/raycast/tooltip/selection overlaps loadout dice UI | Medium if active in combat; otherwise mostly codebase noise | GameplayDiceEditController, ConsumableBarUIManager, DiceEquipUIManager, dice UI | Decide prototype vs production; if production, share input/raycast/tooltip service; if prototype, isolate folder clearly | P2 |
| Assets/Scripts/MapPrototypeDemo/* | Map prototype generation/rendering/UI/demo flow | Prototype/demo files live in production script tree | Separate UI factory/render/controller patterns not related to combat | Low if inactive, but folder noise is high | RunManager, reward/combat demo flow | Later isolate under Assets/Scripts/Prototype or asmdef after confirming build dependencies | P2 |

## Top 5 Areas With Duplicate Logic

1. Status/HP/Guard UI rendering
   * ActorWorldUI.StatusIntent.cs
   * ActorWorldUI.cs
   * CombatHUD.PlayerVitals.cs

2. Target preview/resource preview
   * TargetClickable2D.cs
   * SkillIconPreviewController.cs
   * SkillIconPreviewController.GuardPreview.cs
   * TargetPreviewBuilder.cs
   * CombatHUD.PlayerVitals.cs
   * ActorWorldUI.Preview.cs

3. Tooltip/keyword tooltip/layout
   * SkillTooltipUI*
   * ActorWorldKeywordTooltipUI.cs
   * ConsumableBarUIManager.TooltipPresentation.cs
   * DiceDraggableUI.Tooltip.cs
   * DiceSpinnerGeneric.WorldTooltip.cs

4. Dice value/payment/crit/fail resolution
   * DiceSlotRig.Resolve.cs
   * DiceCombatEnchantRuntimeUtility*.cs
   * TurnManagerCombatUtility.cs
   * SkillRuntimeEvaluator.DiceGather.cs
   * SkillPlanBoardStateUtility.cs

5. Drag/select UI item patterns
   * DraggableSkillIcon*
   * DiceDraggableUI*
   * ConsumableBarUIManager.LayoutDrag.cs
   * DiceEquipUIManager.Selection.cs
   * UiDragState.cs

## Top 5 FPS Risks

1. ActorWorldUI.LateUpdate per enemy/world UI.
2. Tooltip/layout rebuild paths in skill, actor, consumable, and dice UI.
3. FindObjectOfType / FindObjectsOfType in hot or preview paths.
4. Per-item UI Update across skill icons, dice cards, dice spinners, and equip UI.
5. Hit/popup/tween bursts in DamagePopupSystem, CombatHitFeedback, PlayerDiceCastAnimator, and dice roll popups.

## Proposed Refactor Phases

### Phase 0: Guardrails and tiny cleanups

* List exact files before editing.
* Keep changes small and reversible.
* Avoid gameplay, scene, prefab, and serialized field changes.
* Add snapshots/dirty flags before class splitting.
* Build/check after each slice.

### Phase 1: Combat UI state-driven refactor

Focus:

* ActorWorldUI*
* CombatHUD*
* TargetClickable2D.cs
* SkillIconPreviewController*

Goals:

* Add HP/Guard/Status/Intent/Preview state snapshots.
* Extract shared status row rendering.
* Extract shared combat preview presenter.
* Replace repeated world UI lookup with a registry/cache.

### Phase 2: Tooltip unification

Focus:

* SkillTooltipUI*
* ActorWorldKeywordTooltipUI.cs
* ConsumableBarUIManager.TooltipPresentation.cs
* DiceDraggableUI.Tooltip.cs

Goals:

* Extract TooltipPositioner.
* Extract KeywordTooltipPresenter.
* Rebuild tooltip content only when content signature changes.
* Allow position-only updates when content is unchanged.

### Phase 3: Consumable/Dice/Skill UI item controllers

Focus:

* ConsumableBarUIManager*
* DraggableSkillIcon*
* DiceEquipUIManager*
* DiceDraggableUI*

Goals:

* Extract selection models.
* Extract drag controllers.
* Extract view apply-state objects.
* Convert broad refresh loops to dirty/event-driven updates.

### Phase 4: Runtime/preview gameplay convergence

Focus:

* TargetPreviewBuilder*
* SkillExecutor*
* SkillAttackResolutionUtility*
* SkillGameplayResolver*
* SkillRuntimeEvaluator*
* DiceCombatEnchantRuntimeUtility*

Goals:

* Introduce request/context objects where overload lists are too long.
* Extract dice resolution/payment service.
* Make preview reuse runtime resolved-result paths where safe.

### Phase 5: Folder/assembly cleanup

Focus:

* MapPrototypeDemo
* DiceEditSandbox
* Prototype/CombatLab

Goals:

* Isolate prototype/demo systems from production systems.
* Consider asmdefs only after dependencies are stable.

## Safest P0 Task To Start

### P0-1: Extract shared combat status row rendering

Goal:

* Share status icon row rendering between ActorWorldUI and CombatHUD.

Files expected to change:

* Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs
* Assets/Scripts/UI/Combat/CombatHUD.PlayerVitals.cs
* New file, likely Assets/Scripts/UI/Combat/CombatStatusRowRenderer.cs or Assets/Scripts/UI/Combat/CombatStatusRowViewUtility.cs

Constraints:

* Do not change gameplay.
* Do not change scenes or prefabs.
* Do not rename serialized fields.
* Do not change status effect rules.
* Do not change tooltip content in the first slice.

Why this first:

* Duplication is clear.
* Behavior is visual and easy to manually test.
* It reduces maintenance risk without touching combat math.
* It prepares ActorWorldUI and CombatHUD for later state-driven refactor.

### Alternative P0-2: Add ActorWorldUI registry/cache

Goal:

* Reduce repeated FindObjectsOfType ActorWorldUI in targeting/preview paths.

Files expected to change:

* Assets/Scripts/UI/Combat/ActorWorldUI.cs
* Assets/Scripts/UI/Combat/TargetClickable2D.cs
* Assets/Scripts/UI/Planning/SkillIconPreviewController.cs
* New file, likely Assets/Scripts/UI/Combat/ActorWorldUiRegistry.cs

Status:

* Completed as part of autonomous continuation.
* Registry helper was kept internal in an existing included `.cs` file to avoid Unity `.csproj` regeneration issues.
* Follow-up registry batches also covered `DraggableSkillIcon`, `DiceDraggableUI`, `CombatActor`, `DamagePopupSystem`, `TurnManager`, `BattlePartyManager2D`, `CombatHUD`, `DiceEquipUIManager`, `ConsumableBarUIManager`, `DiceSlotRig`, `GameplayDiceEditController`, `RunInventoryManager`, and `SelfCastDropZone`.
* `SceneCanvasLookup` now centralizes per-frame canvas discovery for tooltip/popup/visual placement paths.
* `CombatHUD`, `ActorWorldUI`, and `ConsumableBarUIManager` have small polling/churn gates in place.

Latest verification:

* `dotnet build Project_build_synergy.sln`
* 0 errors, 35 existing warnings.

Next safest refactor direction:

* Tooltip/keyword tooltip/layout consolidation.
* Start with small helpers for position-only updates or content signatures.
* Avoid rewriting `SkillTooltipUI` wholesale.

This has clearer FPS upside but touches more runtime preview behavior, so it should remain staged after the already-completed P0-1/P0-2 lookup slices.

---

## System Group Breakdown

Project size note:

* Current project has about 272 C# files total.
* Excluding Editor code, the working refactor scope is about 255 C# files.
* The objective is not to reduce file count by merging files. The objective is to make each file/class responsibility clearer.
* Do not refactor all non-Editor files at once.

Estimated group counts below are based on current Assets/Scripts folders and should be treated as planning estimates, not exact ownership boundaries. Some files are cross-cutting.

| Group | Estimated file count | Main problem | Duplicated logic | FPS risk | First refactor task |
|---|---:|---|---|---|---|
| Combat | ~40 | Turn/execution/actor systems are central and coupled to UI refresh, preview, dice, feedback, and skill execution | Runtime execution vs preview rules; target validation; dice value computation; popup/feedback calls | Medium. Not many pure per-frame loops, but broad refreshes, coroutines, tween bursts, and actor/popup lookup can spike | Do not start here. After UI P0, introduce request/context objects around preview/execution boundaries, starting with TargetPreviewBuilder and SkillExecutor only when behavior is covered by manual tests |
| UI Combat | ~19 | Highest immediate complexity. ActorWorldUI, CombatHUD, ConsumableBarUIManager, TargetClickable2D, tooltip, popup, and targeting are broad UI god objects | Status row rendering; tooltip content/positioning; target preview presentation; hover/selection refresh; FindObjectsOfType ActorWorldUI | High. Per-frame UI updates, tooltip/layout rebuilds, TMP changes, SetActive/sibling changes, target preview lookup, popup/tween bursts | P0-1: extract shared combat status row rendering for ActorWorldUI and CombatHUD without changing gameplay, scenes, prefabs, or serialized fields |
| Planning UI | ~11 | Skill icon and preview code mixes view, drag/drop, selection, tooltip, active runtime state, and target/resource preview | Skill target/resource preview overlaps TargetClickable2D and CombatHUD/ActorWorldUI; drag/select overlaps dice UI | Medium-high when many icons exist. Per-icon Update, aura tween, preview tick, tooltip/hover bridge checks | After UI Combat P0, split SkillIconPreviewController presentation from DraggableSkillIcon input/view responsibilities |
| Dice | ~22 | Dice runtime, visual, tooltip, roll popup, value resolution, preview, and enchant behavior are spread across partials/utilities | Dice value/payment/crit/fail logic overlaps DiceSlotRig, DiceCombatEnchantRuntimeUtility, TurnManagerCombatUtility, SkillRuntimeEvaluator | Medium-high. DiceSpinnerGeneric has tooltip update; roll animation/popup/tween bursts; dice resolution called often in preview/planning | Create a DiceResolution/DiceEvaluation snapshot plan before code changes; first actual slice should extract pure resolution helpers only, not visuals |
| Skills Runtime | ~19 | Static utilities and evaluators contain many overloads and mixed concerns: targeting, condition context, values, effects, dice gather | Targeting and status stack lookup overlap TargetPreviewBuilder and SkillBehaviorRuntimeUtility; dice gather overlaps Dice utilities | Medium-high if called by preview/tooltip/intent often; scene-wide actor lookup in targeting path is risky | After preview/UI cleanup, introduce narrow SkillTargetProvider/context to remove scene-wide lookup from resolver paths |
| Consumables | ~2 | Runtime utility handles many consumable cases directly; UI manager also owns selection/use flow | Target/dice/status resolution overlaps skills runtime and dice edit systems | Medium during action bursts; mostly maintainability risk unless UI refresh/panel active | Do not start here. First split consumable UI presenters in UI Combat, then revisit ConsumableRuntimeUtility family handlers |
| Inventory | ~10 | RunInventoryManager spans skills, dice, passives, consumables, gold, bindings, prefab spawn; InventoryChanged is broad | UI binding refresh logic repeats across skill/dice/consumable UI | Low-medium. Broad InventoryChanged can trigger unnecessary UI refreshes | Later split events into SkillsChanged, DiceChanged, ConsumablesChanged, GoldChanged while preserving serialized fields |
| Prototype/Demo | ~41 | Prototype, MapPrototypeDemo, and DiceEditSandbox add codebase noise and some production overlap | Drag/raycast/tooltip/selection overlaps production dice UI; demo UI patterns are separate | Low if inactive; medium if DiceEditSandbox/GamePrototype components stay active in combat | Classify each folder as production, prototype, or legacy before moving anything. No folder moves until dependencies are known |

## Group Notes

### Combat

Combat should remain behavior-stable while UI refactor begins. The risky part is not file count but coupling:

* TurnManager triggers many broad view refreshes.
* SkillExecutor has long overloads and animation/execution coupling.
* TargetPreviewBuilder must mirror runtime execution.
* CombatActor and CombatHitFeedback directly touch popup/feedback paths.

Refactor here only after UI preview/presentation responsibilities are clearer.

### UI Combat

This is the best first refactor area because it has both duplicate code and FPS risk, while still allowing visual-only slices.

Primary targets:

* ActorWorldUI partial group.
* CombatHUD partial group.
* ConsumableBarUIManager partial group.
* TargetClickable2D.
* ActorWorldKeywordTooltipUI.
* DamagePopupSystem.

Do not rewrite these at once. Start with shared status row rendering.

### Planning UI

Planning UI should become a thin input/view layer:

* DraggableSkillIcon should stop owning preview presentation.
* SkillIconPreviewController should become a presenter/client of common combat preview services.
* Tooltip and active aura should be separate from skill equip/drop behavior.

### Dice

Dice needs clearer boundaries:

* dice data/state;
* dice roll animation;
* dice visual feedback;
* dice value resolution;
* dice UI card behavior.

Avoid changing dice rules until shared tests/manual cases are defined.

### Skills Runtime

Skills runtime should be carefully refactored only after preview and dice resolution paths are understood. The main target is not fewer files, but fewer hidden paths for the same rule.

Desired direction:

* request/context objects instead of long overloads;
* injected or passed combat context instead of scene-wide lookup;
* one shared path for runtime and preview where safe.

### Consumables

Consumables are currently small by file count, but connected to many systems through UI, dice edit, inventory, and combat. Start by cleaning UI responsibilities before changing runtime consumable logic.

### Inventory

Inventory is a later-phase refactor. It is central and serialized, so preserve fields and public APIs first. The safest improvement is narrower change events, but only after UI listeners are ready.

### Prototype/Demo

Prototype/demo code should be classified before moving or deleting anything. Some prototype code may be production-adjacent. Do not reorganize folders until references are checked.

## Single Selected P0 Task

The single safest P0 task to implement first is:

### P0-1: Extract shared combat status row rendering for ActorWorldUI and CombatHUD

Why this is the first task:

* It uses the audit's clearest duplication.
* It is visual/UI-only.
* It does not require scene or prefab changes.
* It does not require renaming serialized fields.
* It reduces risk before deeper ActorWorldUI/CombatHUD state-driven refactor.
* It is easy to manually verify in Unity.

Files that would change for this task:

* Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs
* Assets/Scripts/UI/Combat/CombatHUD.PlayerVitals.cs
* New file under Assets/Scripts/UI/Combat, likely CombatStatusRowRenderer.cs or CombatStatusRowViewUtility.cs

Behavior that must remain unchanged:

* Status icons show the same statuses.
* Status icon colors, sprites, labels, and values remain the same.
* Player HUD and enemy/world UI status rows still update when status changes.
* Tooltip content and behavior remain unchanged in this first slice.
* No gameplay rule changes.

Verification for P0-1:

* Compile/build check.
* In Unity, enter combat with player and at least one enemy.
* Apply or simulate Burn, Bleed, Chilled, Frozen, Marked, Stagger, and Ailment.
* Confirm player HUD status row and enemy world status row match pre-refactor behavior.
* Confirm target preview still overlays statuses correctly.
* Confirm no scene/prefab references are lost.

## Latest Completed Safe Slices

Completed after the initial audit/P0 work:

* Shared combat status row rendering was extracted for player HUD and actor world UI.
* Actor/world/skill/dice UI lookup paths were moved toward registries and cached fallbacks.
* Guard-hit popup/feedback paths were simplified to reduce guard-specific FPS spikes while preserving curved popup motion.
* `CombatTargetPreviewPresenter` now centralizes target preview show/clear.
* `CombatGuardPreviewUtility` now centralizes guard preview die-value/self-guard calculations.
* `CombatPreviewBundleUtility.BuildActionBundleWithSelfGuard()` now centralizes duplicated target/guard preview bundle construction for `TargetClickable2D` and `SkillIconPreviewController`.
* UI and actor registries now cache stable snapshots to avoid repeated array allocation in preview/refresh paths.

Next safest continuation:

* Finish resource/focus preview ownership consolidation between skill icon preview, target clickables, and `CombatHUD`.
* Continue tooltip presenter/positioner extraction in small slices.
* Defer dice payment snapshot service and `SkillExecutor` request-object refactor until the UI preview paths are manually tested in Unity.
