# Dead Code Candidates

This report tracks code that may be removable later. Nothing in this file is permission to delete immediately.

## Rules Before Deleting Anything

* Check static references with search and Serena where useful.
* Check Unity risks: public methods, serialized fields, UnityEvents, animation events, reflection, scene/prefab references.
* Prefer marking as candidate over deleting if unsure.
* Do not delete public APIs unless there is strong evidence they are not used by Unity serialization or external callers.
* Do not delete prototype/demo code until it is classified as unused in the current workflow.

## Confirmed Safe Deletions Already Done

| Removed item | Why safe | Verification |
|---|---|---|
| Private no-op `PassiveSystem.Accumulate(PassiveEffectEntry effect)` path | The method had no side effects and `Rebuild()` still clears/rebuilds state through active effect queries. Runtime effect value reads remain through `GetEffectValue`. | `dotnet build Project_build_synergy.sln` passes |
| Tooltip-local `SkillTooltipPrefabProvider` caches and private `GetPrefabProvider()` helpers in skill/actor keyword tooltip classes | A shared `SkillTooltipPrefabProviderRegistry` now owns provider lookup/cache; the duplicated private caches had no unique behavior. | `dotnet build Project_build_synergy.sln` passes |
| Local `unusedSkip` variable in `TurnManagerLifecycleUtility.BeginPlayerTurnStatusesAndFocus` | The out value was intentionally ignored; replaced with discard `_` without changing the `OnTurnStarted` call. | `dotnet build Project_build_synergy.sln` passes |
| `TargetClickable2D.GetWorldUI` and private `_worldUI` cache | Actor world UI lookup now goes through `ActorWorldUiRegistry`; the private wrapper/cache had no callers. | `dotnet build Project_build_synergy.sln` passes |
| Unused skill keyword tooltip fallback builder helpers | Keyword tooltip creation now requires prefab/settings and logs a clear error when missing; the private fallback builder path had no callers. | `dotnet build Project_build_synergy.sln` passes |
| Unused dice world-tooltip face-icon rect union helper path | Face-enchant tooltip rect path no longer used the combined face icon/value helper; the private combined rect helper and union helper had no callers. | `dotnet build Project_build_synergy.sln` passes |
| Unused dice enchant hover-zone auto-position builder helpers and fields | `EnsureManagedEnchantHoverZone()` no longer creates the managed hover zone; private creation/position helpers and their private fields had no callers/assignments. Existing proxy enter/exit handlers were kept. | `dotnet build Project_build_synergy.sln` passes |
| `PassiveSystem.BuildPassiveConditionContext`, `GetParty`, and `_cachedParty` | The passive condition context builder was left behind after the no-op accumulate path was removed; no callers remained. | `dotnet build Project_build_synergy.sln` passes |
| `DiceSpinnerGeneric.CacheFaceIconBaseColor` | Face icon base colors are maintained directly by active face/enchant rendering paths; this private cache helper had no callers. | `dotnet build Project_build_synergy.sln` passes |

## Candidates Requiring More Evidence

| Candidate | Why suspicious | Why not deleted yet | Required proof before deletion |
|---|---|---|---|
| Prototype/demo setup/runtime helpers under `Assets/Scripts/Prototype` | Prototype namespace/folder and demo naming | May still be wired into current combat lab flow | Confirm scene/prefab references and current workflow usage |
| `Assets/Scripts/MapPrototypeDemo/*` | Demo/prototype feature area, not core combat | May be part of run progression or current map prototype | Confirm production path and build usage |
| `Assets/Scripts/DiceEditSandbox/*` older sandbox pieces | Sandbox naming and overlap with GameplayDiceEdit systems | Some systems are used by consumables/dice edit UI | Classify each file as production, sandbox, or legacy |
| Editor setup tools in `Assets/Scripts/Editor/*` | Many one-shot setup tools | User did not ask to remove editor tooling | Only delete if superseded and no menu/tool workflow depends on it |
| Debug hotkey code in `DiceSlotRig.Update` and `TurnManager.Update` | Debug-only behavior | May be useful during development | Gate/keep unless user asks to strip debug behavior |
| Runtime particle creation inside feedback systems | Possible spike source | Could be intentional/rare visual feedback | Profile hit/feedback bursts and confirm replacement/pooling path |

## Private Unused Cleanup Strategy

When doing a focused batch:

1. Search for the private method/class name.
2. Use Serena references when it is code-visible.
3. If no references and no Unity callback naming convention applies, remove only within the same batch.
4. Compile immediately.

Do not run a broad private-method deletion sweep yet.

## Notes From Current Continuation

No files were deleted in the latest cleanup. The newest batches focused on FPS-safe dirty setters, idle polling throttles, tooltip mesh caching, and report updates.

Large candidate groups remain intentionally untouched because Unity scene/prefab/event references cannot be proven safe from C# search alone.
