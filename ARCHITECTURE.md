# Architecture Notes

This document describes the intended target structure for the Unity C# project. It is a direction for staged refactor, not a mandate to rewrite everything at once.

## Current Direction

The project should move toward small, explicit, testable systems:

* data/config in ScriptableObjects;
* runtime gameplay logic in non-UI services/utilities where possible;
* MonoBehaviours as scene/prefab adapters and visual controllers;
* UI views that apply state instead of querying gameplay every frame;
* shared preview/runtime calculation paths where safe.

## Target Folder Shape

The current folder layout can remain in place during early refactor. Long term, aim for this shape under Assets/Scripts:

    Assets/Scripts
    - Combat
      - Actors
      - Execution
      - Preview
      - Status
      - Turn
    - Consumables
    - Dice
      - Runtime
      - Resolution
      - Visuals
    - Inventory
    - Run
      - Rewards
    - Skills
      - Definitions
      - Planning
      - Runtime
    - UI
      - Combat
        - ActorWorld
        - HUD
        - Popups
        - Preview
        - Targeting
      - Loadout
        - Dice
        - Passive
      - Planning
      - SkillPresentation
      - Tooltips
    - Prototype
    - Editor

Do not move files into this layout without a specific refactor task and a clear dependency check.

## Target Runtime Boundaries

### Combat runtime

Owns:

* actors;
* turn phases;
* command queue;
* skill execution;
* status changes;
* dice usage during combat.

Should not own:

* tooltip layout;
* UI object lookup;
* prefab hierarchy creation;
* visual animation details beyond calling a presenter/feedback service.

### Preview runtime

Owns:

* simulated target/resource outcomes;
* target preview data;
* consistency between preview and execution rules.

Should not own:

* UI positioning;
* tooltip layout;
* direct scene-wide UI search.

Preferred flow:

    Skill/Dice/Target input
        -> Preview request/context
        -> Runtime/preview resolver
        -> Preview data
        -> UI preview presenter
        -> View apply-state

### UI views

Own:

* serialized UI references;
* color/text/image changes;
* local layout details;
* show/hide state for their own visuals.

Should not own:

* gameplay rules;
* actor discovery;
* dice payment rules;
* skill effect resolution.

Preferred UI contract:

    public void Apply(SomeUiState state)
    {
        if (_lastState.Equals(state))
            return;

        _lastState = state;
        // update visuals
    }

### UI presenters/controllers

Own:

* translating runtime state into UI state;
* caching dependencies;
* dirty flags;
* event subscriptions.

Should not own:

* low-level text/image assignment if a view can own it;
* gameplay effect application.

## Target Combat UI Structure

### Actor world UI

Target components/classes:

* ActorWorldUiController
* ActorWorldVitalsView
* ActorWorldStatusRowView
* ActorWorldIntentView
* ActorWorldTooltipPresenter
* ActorTargetOverlayView
* ActorWorldPreviewView

Current source files to refactor gradually:

* Assets/Scripts/UI/Combat/ActorWorldUI.cs
* Assets/Scripts/UI/Combat/ActorWorldUI.Setup.cs
* Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs
* Assets/Scripts/UI/Combat/ActorWorldUI.Preview.cs

### Player HUD

Target components/classes:

* PlayerFocusView
* PlayerVitalsView
* shared CombatStatusRowRenderer
* shared tooltip content builder where safe

Current source files:

* Assets/Scripts/UI/Combat/CombatHUD.cs
* Assets/Scripts/UI/Combat/CombatHUD.PlayerVitals.cs

### Consumable bar

Target components/classes:

* ConsumableBarController
* ConsumableBarSelectionModel
* ConsumableSlotListView
* ConsumableActionPanelPresenter
* ConsumableTooltipPresenter
* ConsumableDragController
* ConsumableUseController

Current source files:

* Assets/Scripts/UI/Combat/ConsumableBarUIManager*.cs

## Target Dice UI Structure

### Dice equip UI

Target components/classes:

* DiceEquipController
* DiceEquipSelectionModel
* DiceEquipRuntimeVisualSync
* DiceEquipWorldMirror
* DiceEquipInventorySync

Current source files:

* Assets/Scripts/UI/Loadout/Dice/DiceEquipUIManager*.cs

### Dice card UI

Target components/classes:

* DiceCardView
* DiceCardDragHandler
* DiceCardTooltipPresenter
* DiceCombatVisualStateView

Current source files:

* Assets/Scripts/UI/Loadout/Dice/DiceDraggableUI*.cs

### Dice runtime

Target components/classes:

* DiceRollState
* DiceFaceState
* DiceResolutionService
* DicePaymentPlanService
* DiceWorldTooltipPresenter
* DiceRollFeedbackView

Current source files:

* Assets/Scripts/Dice/DiceSpinnerGeneric*.cs
* Assets/Scripts/Dice/DiceSlotRig*.cs
* Assets/Scripts/Dice/DiceCombatEnchantRuntimeUtility*.cs

## Target Skill/Execution Structure

### Skill execution

Prefer request/context objects over long overloads. A future SkillExecutionRequest should contain the skill asset, caster, target, dice rig, start slot, span, and payment mask.

Do not introduce this globally until a focused refactor task is approved.

### Skill preview

Target components/classes:

* SkillPreviewRequest
* CombatPreviewPresenter
* PreviewPresentationData
* shared runtime/preview resolver path where safe

Current source files:

* Assets/Scripts/Combat/Execution/TargetPreviewBuilder*.cs
* Assets/Scripts/UI/Combat/TargetClickable2D.cs
* Assets/Scripts/UI/Planning/SkillIconPreviewController*.cs

## Event and Dirty-Flag Direction

Prefer:

* specific dirty flags;
* narrow events;
* cached dependencies;
* apply-state views.

Avoid:

* broad RefreshAll calls from many entry points;
* FindObjectsOfType during hover/preview/update;
* rebuilding tooltip/layout content every frame;
* global event systems that are not required by an immediate task.

Good intermediate pattern:

    private bool _dirty;

    public void MarkDirty()
    {
        _dirty = true;
    }

    private void LateUpdate()
    {
        if (!_dirty)
            return;

        _dirty = false;
        Refresh();
    }

## Serialized Field Policy

During refactor:

* preserve serialized field names;
* preserve public fields used by existing prefabs/scenes;
* prefer wrapping existing fields rather than renaming them;
* only move serialized data after explicit approval and prefab/scene validation.

## Prototype and Demo Systems

Prototype/demo systems should eventually be isolated from production gameplay:

* Assets/Scripts/MapPrototypeDemo
* Assets/Scripts/Prototype
* Assets/Scripts/DiceEditSandbox if confirmed prototype-only

Do not move them until their runtime dependencies are known.

## First Refactor Slice

Recommended first slice:

* Extract shared combat status row rendering for ActorWorldUI and CombatHUD.

Expected files:

* Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs
* Assets/Scripts/UI/Combat/CombatHUD.PlayerVitals.cs
* new shared renderer utility in Assets/Scripts/UI/Combat/

Constraints:

* no gameplay change;
* no scene/prefab change;
* no serialized field rename;
* no tooltip behavior change in the first slice;
* compile check after the change.
