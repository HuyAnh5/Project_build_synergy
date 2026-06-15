using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves authored skill gameplay data into concrete effects for preview and execution.
/// </summary>
public static partial class SkillGameplayResolver
{
    /// <summary>
    /// Returns true when the skill should use the authored gameplay pipeline instead of legacy logic.
    /// </summary>
    public static bool CanResolveWithNewPipeline(SkillDamageSO skill)
    {
        return skill != null && skill.gameplay != null && skill.gameplay.useNewGameplayPipeline;
    }

    /// <summary>
    /// Resolves a skill using an explicit condition context.
    /// </summary>
    public static SkillResolvedResult Resolve(
        SkillDamageSO skill,
        SkillRuntime runtime,
        CombatActor caster,
        CombatActor target,
        SkillConditionContext conditionContext)
    {
        SkillResolveContext context = BuildContext(skill, runtime, caster, target, conditionContext);
        return Resolve(context);
    }

    /// <summary>
    /// Resolves a runtime skill by building the local condition context from current combat state.
    /// </summary>
    public static SkillResolvedResult Resolve(SkillRuntime runtime, CombatActor caster, CombatActor target)
    {
        SkillDamageSO skill = GetSourceSkill(runtime);
        return Resolve(skill, runtime, caster, target, BuildConditionContext(runtime, caster, target));
    }

    /// <summary>
    /// Extracts the authored damage skill from a runtime instance when available.
    /// </summary>
    public static SkillDamageSO GetSourceSkill(SkillRuntime runtime)
    {
        return runtime != null ? runtime.sourceAsset as SkillDamageSO : null;
    }

    /// <summary>
    /// Resolves the full set of effects for the provided resolve context.
    /// </summary>
    public static SkillResolvedResult Resolve(SkillResolveContext context)
    {
        SkillResolvedResult result = new SkillResolvedResult();
        if (context == null || context.skill == null || context.skill.gameplay == null)
        {
            result.canCast = false;
            result.failureReason = "Missing skill gameplay data.";
            return result;
        }

        SkillGameplayData gameplay = context.skill.gameplay;
        result.resolvedAPCost = context.runtime != null
            ? Mathf.Max(0, context.runtime.focusCost)
            : Mathf.Max(0, context.skill.focusCost);
        result.resolvedDiceCost = context.runtime != null
            ? Mathf.Clamp(context.runtime.slotsRequired, 1, 3)
            : Mathf.Clamp(context.skill.slotsRequired, 1, 3);

        if (!CheckRequirements(gameplay, context, result))
        {
            return result;
        }

        ResolveEffects(gameplay.baseEffects, context, result, sameActionFollowUp: false);

        SkillConditionContext followUpConditionContext = BuildFollowUpConditionContext(context, result.effects);
        if (gameplay.conditionalOutcomes == null)
        {
            return result;
        }

        for (int i = 0; i < gameplay.conditionalOutcomes.Count; i++)
        {
            SkillConditionalOutcomeDataV2 branch = gameplay.conditionalOutcomes[i];
            if (branch == null || branch.condition == null || !branch.condition.Evaluate(followUpConditionContext))
            {
                continue;
            }

            ResolveEffects(branch.effects, context, result, sameActionFollowUp: true);
        }

        return result;
    }

    /// <summary>
    /// Builds a context snapshot used by condition checks for a specific action.
    /// </summary>
    public static SkillConditionContext BuildConditionContext(SkillRuntime runtime, CombatActor caster, CombatActor target)
    {
        int occupiedSlots = runtime != null ? Mathf.Clamp(runtime.slotsRequired, 1, 3) : 0;

        return new SkillConditionContext
        {
            scope = SkillConditionScope.SlotBound,
            localBaseValues = runtime != null ? runtime.localBaseValues : null,
            localOutputBaseValues = runtime != null ? runtime.localOutputBaseValues : null,
            localNumericFlags = runtime != null ? runtime.localNumericFlags : null,
            localResolvedValues = runtime != null ? runtime.localResolvedValues : null,
            localCritFlags = runtime != null ? runtime.localCritFlags : null,
            localFailFlags = runtime != null ? runtime.localFailFlags : null,
            currentFocus = caster != null ? caster.focus : 0,
            currentGuard = caster != null ? caster.guardPool : 0,
            targetGuard = target != null ? target.guardPool : 0,
            occupiedSlots = occupiedSlots,
            remainingSlots = runtime != null ? Mathf.Max(0, 3 - occupiedSlots) : 0,
            aliveEnemiesCount = CountAliveEnemies(caster),
            enemiesWithBurnCount = CountEnemiesWithStatus(caster, StatusKind.Burn),
            markedEnemiesCount = CountEnemiesWithStatus(caster, StatusKind.Mark),
            totalBleedOnBoard = CountTotalBleed(caster),
            enemiesWithStatusCount = CountEnemiesWithAnyStatus(caster),
            targetHasBurn = target != null && target.status != null && target.status.burnStacks > 0,
            targetHasFreeze = target != null && target.status != null && target.status.frozen,
            targetHasChilled = target != null && target.status != null && target.status.chilledTurns > 0,
            targetHasMark = target != null && target.status != null && target.status.marked,
            targetHasBleed = target != null && target.status != null && target.status.bleedStacks > 0,
            targetHasStagger = target != null && target.status != null && target.status.staggered
        };
    }

    private static SkillResolveContext BuildContext(
        SkillDamageSO skill,
        SkillRuntime runtime,
        CombatActor caster,
        CombatActor target,
        SkillConditionContext conditionContext)
    {
        return new SkillResolveContext
        {
            skill = skill,
            runtime = runtime,
            caster = caster,
            target = target,
            conditionContext = conditionContext,
            totalAddedValue = SkillOutputValueUtility.GetTotalActionAddedValue(runtime)
        };
    }

    private static bool CheckRequirements(SkillGameplayData gameplay, SkillResolveContext context, SkillResolvedResult result)
    {
        if (gameplay.requirements == null)
        {
            return true;
        }

        for (int i = 0; i < gameplay.requirements.Count; i++)
        {
            SkillRequirementData requirement = gameplay.requirements[i];
            if (requirement == null || requirement.condition == null)
            {
                continue;
            }

            if (!requirement.condition.Evaluate(context.conditionContext))
            {
                result.canCast = false;
                result.failureReason = string.IsNullOrWhiteSpace(requirement.failureText)
                    ? "Requirement not met."
                    : requirement.failureText;
                return false;
            }
        }

        return true;
    }

    private static void ResolveEffects(
        List<SkillEffectData> effects,
        SkillResolveContext context,
        SkillResolvedResult result,
        bool sameActionFollowUp)
    {
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffectData effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            List<CombatActor> targets = ResolveTargetsForEffect(effect, context);
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                CombatActor effectTarget = targets[targetIndex];
                SkillResolveContext targetContext = WithTarget(context, effectTarget);
                int resolvedValue = ResolveValue(effect.value, targetContext);
                if (effect.type == SkillEffectType.ConsumeStatus)
                {
                    CaptureConsumedStacks(effect.status, targetContext);
                    resolvedValue = GetConsumedStatusStacks(effect.status, targetContext);
                    context.consumedBurnStacks = targetContext.consumedBurnStacks;
                    context.consumedBleedStacks = targetContext.consumedBleedStacks;
                }

                ResolvedEffect resolved = new ResolvedEffect
                {
                    type = effect.type,
                    target = effect.target,
                    targetActor = effectTarget,
                    status = effect.status,
                    value = resolvedValue,
                    isBlueValue = effect.value != null &&
                                  (effect.value.mode == SkillValueMode.AddedValueScaled ||
                                   effect.value.mode == SkillValueMode.ActionX),
                    previewable = effect.previewable,
                    sameActionFollowUp = sameActionFollowUp,
                    source = effect
                };

                result.effects.Add(resolved);
                AccumulateResult(resolved, result);
            }
        }
    }

    private static SkillResolveContext WithTarget(SkillResolveContext context, CombatActor target)
    {
        if (context == null)
        {
            return null;
        }

        return new SkillResolveContext
        {
            skill = context.skill,
            runtime = context.runtime,
            caster = context.caster,
            target = target,
            conditionContext = context.conditionContext,
            totalAddedValue = context.totalAddedValue,
            consumedBurnStacks = context.consumedBurnStacks,
            consumedBleedStacks = context.consumedBleedStacks
        };
    }

    private static void AccumulateResult(ResolvedEffect effect, SkillResolvedResult result)
    {
        switch (effect.type)
        {
            case SkillEffectType.DealDamage:
            case SkillEffectType.DealSecondaryDamage:
                result.damageDelta += Mathf.Max(0, effect.value);
                break;

            case SkillEffectType.GainGuard:
                result.guardDelta += Mathf.Max(0, effect.value);
                break;

            case SkillEffectType.Heal:
                result.healDelta += Mathf.Max(0, effect.value);
                break;

            case SkillEffectType.ApplyStatus:
                result.statusDeltas.Add(new StatusDelta
                {
                    target = effect.target,
                    status = effect.status,
                    amount = Mathf.Max(0, effect.value)
                });
                break;

            case SkillEffectType.ConsumeStatus:
                result.statusDeltas.Add(new StatusDelta
                {
                    target = effect.target,
                    status = effect.status,
                    amount = -Mathf.Max(0, effect.value)
                });
                break;
        }
    }

    private static List<CombatActor> ResolveTargetsForEffect(SkillEffectData effect, SkillResolveContext context)
    {
        if (effect == null)
            return new List<CombatActor>();

        // AP gain is authored as a self resource reward even when the clicked target is an enemy.
        if (effect.type == SkillEffectType.GainAP)
            return ResolveEffectTargets(SkillEffectTarget.Self, context);

        return ResolveEffectTargets(effect.target, context);
    }

    private static void CaptureConsumedStacks(StatusKind status, SkillResolveContext context)
    {
        if (context == null)
        {
            return;
        }

        int stacks = GetTargetStatusStacks(status, context);
        switch (status)
        {
            case StatusKind.Burn:
                context.consumedBurnStacks = Mathf.Max(context.consumedBurnStacks, stacks);
                break;

            case StatusKind.Bleed:
                context.consumedBleedStacks = Mathf.Max(context.consumedBleedStacks, stacks);
                break;
        }
    }

    private static int GetTargetStatusStacks(StatusKind status, SkillResolveContext context)
    {
        if (context == null || context.target == null || context.target.status == null)
        {
            return 0;
        }

        switch (status)
        {
            case StatusKind.Burn:
                return Mathf.Max(0, context.target.status.burnStacks);
            case StatusKind.Bleed:
                return Mathf.Max(0, context.target.status.bleedStacks);
            case StatusKind.Mark:
                return context.target.status.marked ? 1 : 0;
            case StatusKind.Freeze:
                return context.target.status.frozen ? 1 : 0;
            case StatusKind.Chilled:
                return Mathf.Max(0, context.target.status.chilledTurns);
            default:
                return 0;
        }
    }

    private static int GetConsumedStatusStacks(StatusKind status, SkillResolveContext context)
    {
        if (context == null)
        {
            return 0;
        }

        switch (status)
        {
            case StatusKind.Burn:
                return Mathf.Max(0, context.consumedBurnStacks);
            case StatusKind.Bleed:
                return Mathf.Max(0, context.consumedBleedStacks);
            default:
                return 0;
        }
    }
}
