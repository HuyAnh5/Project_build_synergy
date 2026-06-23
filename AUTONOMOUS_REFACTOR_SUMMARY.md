# Autonomous Refactor Summary

This file is updated after autonomous refactor batches so the project is never left without a handoff.

## Current Batch: P0-1 Combat Status Row Rendering

Status: compile check passed.

Commit status:

* Not committed yet.
* Attempted to stage the completed batch, but sandbox escalation for `git add` was rejected by the approval reviewer before the command ran.
* No workaround was attempted. The working tree contains a compile-passing batch ready for manual review/stage/commit.

## Changed

* Extracted duplicate status row slot rendering into a shared internal helper.
* Updated enemy/world UI status row rendering to use the shared helper.
* Updated player HUD status row rendering to use the shared helper.

## Files Modified

* `Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs`
* `Assets/Scripts/UI/Combat/CombatHUD.PlayerVitals.cs`

## Files Created

* `DEAD_CODE_CANDIDATES.md`
* `DUPLICATE_LOGIC_REPORT.md`
* `FPS_RISK_REPORT.md`
* `AUTONOMOUS_REFACTOR_SUMMARY.md`

Previously created/updated planning docs:

* `AGENTS.md`
* `ARCHITECTURE.md`
* `REFACTOR_PLAN.md`

## Deleted

Nothing.

## Not Deleted Because Risk Is Unclear

* Prototype/demo systems.
* Dice edit sandbox systems.
* Public methods that may be used by UnityEvent, scene references, prefab references, animation events, or reflection.
* Serialized fields.

## Verification

Command run:

* `dotnet build Project_build_synergy.sln`

Result:

* 0 errors.
* Existing warnings remain, mostly Unity obsolete API warnings, TMP wrapping warnings, and pre-existing unreachable/empty switch warnings.

## Remaining Risks

* Unity Editor `.csproj` regeneration is needed before adding brand-new C# files if using dotnet build outside Unity. For P0-1, the shared helper was kept inside an existing source file to avoid that issue.
* Tooltip/status content is still duplicated and should be handled in a later batch.
* Scene-wide lookup remains in several preview/hot paths.
* FPS root cause may still be rendering/GPU/presentation wait, not only C# scripts.

## Unity Manual Tests Needed

1. Enter combat with player and at least one enemy.
2. Apply or simulate Burn, Bleed, Chilled, Frozen, Marked, Stagger, and Ailment.
3. Confirm enemy world status row icons, labels, colors, and values match previous behavior.
4. Confirm player HUD status row icons, labels, colors, and values match previous behavior.
5. Confirm status icons hide correctly when statuses expire or are removed.
6. Confirm status tooltips still show correct content on enemy/world UI and player HUD.
7. Confirm target preview still overlays status previews correctly.

## Recommended Next Batch

Small FPS-oriented batch:

* Add an `ActorWorldUI` registry/cache.
* Replace repeated `FindObjectsOfType<ActorWorldUI>` in target/preview paths.
* Candidate files: `ActorWorldUI.cs`, `TargetClickable2D.cs`, `SkillIconPreviewController.cs`.

## Autonomous Continuation: Hot Lookup Registry Batches

Status: compile check passed.

Command run after the latest batch:

* `dotnet build Project_build_synergy.sln`

Result:

* 0 errors.
* Existing warnings reduced to 69 after replacing several obsolete scene-wide lookup paths.

Changed in this continuation:

* Added internal registries/caches for:
  * `ActorWorldUI`
  * `DraggableSkillIcon`
  * `DiceDraggableUI`
  * `CombatActor`
  * `DamagePopupSystem`
* Replaced repeated target/preview/visual scene scans with registry snapshots where semantics were clear.
* Kept inactive-object semantics where old code used `FindObjectsOfType<T>(true)`.
* Kept active-only semantics where old code used `FindObjectsOfType<T>()`.
* Did not change gameplay math, balance, serialized fields, scenes, or prefabs.

Files changed by continuation:

* `Assets/Scripts/Combat/Actors/CombatActor.cs`
* `Assets/Scripts/Combat/Execution/PlayerDiceCastAnimator/PlayerDiceCastAnimator.Utility.cs`
* `Assets/Scripts/Combat/Execution/SkillExecutor.cs`
* `Assets/Scripts/Combat/Turn/EnemyTurnCoordinator.cs`
* `Assets/Scripts/Combat/Turn/TurnManagerCombatUtility.cs`
* `Assets/Scripts/Combat/Turn/TurnManagerViewUtility.cs`
* `Assets/Scripts/Dice/DiceSlotRig.ConsumePreview.cs`
* `Assets/Scripts/Dice/DiceSpinnerGeneric.Visuals.cs`
* `Assets/Scripts/Skills/Definitions/PassiveSystem.cs`
* `Assets/Scripts/Skills/Runtime/SkillBehaviorRuntimeUtility.cs`
* `Assets/Scripts/Skills/Runtime/SkillGameplayResolver.Targeting.cs`
* `Assets/Scripts/Run/RunManager.cs`
* `Assets/Scripts/UI/Combat/ActorWorldUI.StatusIntent.cs`
* `Assets/Scripts/UI/Combat/ActorWorldUI.cs`
* `Assets/Scripts/UI/Combat/CombatHUD.cs`
* `Assets/Scripts/UI/Combat/DamagePopupSystem.cs`
* `Assets/Scripts/UI/Combat/TargetClickable2D.cs`
* `Assets/Scripts/UI/Loadout/Dice/DiceDraggableUI.cs`
* `Assets/Scripts/UI/Planning/DraggableSkillIcon.Interaction.cs`
* `Assets/Scripts/UI/Planning/DraggableSkillIcon.cs`
* `Assets/Scripts/UI/Planning/SkillIconPreviewController.cs`

Not part of this continuation:

* `AGENTS.md` was already modified before this continuation and was not intentionally edited in these batches.
* Untracked tool/generated folders were not touched.

Unity manual tests added for this continuation:

1. Enter combat, hover/select/drag skills over enemies and self.
2. Confirm target overlays, target preview HP/Guard/status, and HUD focus preview still appear and clear correctly.
3. Roll dice, hover skills, consume dice, and confirm dice consume preview/tints/crit-fail preview still work.
4. Cast damage/heal/focus gain actions and confirm popups still spawn.
5. End/start turns and confirm skill icon dimming, dice dimming, enemy guard clear, and enemy intents still update.
6. Kill an enemy and confirm inactive/dead actors do not remain targetable.
