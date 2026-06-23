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
