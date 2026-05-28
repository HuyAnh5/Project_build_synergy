using System.Collections.Generic;
using UnityEngine;

public class SkillResolveContext
{
    public SkillDamageSO skill;
    public SkillRuntime runtime;
    public CombatActor caster;
    public CombatActor target;
    public SkillConditionContext conditionContext;
    public int totalAddedValue;
    public int consumedBurnStacks;
    public int consumedBleedStacks;
}

public class ResolvedEffect
{
    public SkillEffectType type;
    public SkillEffectTarget target;
    public CombatActor targetActor;
    public StatusKind status;
    public int value;
    public bool isBlueValue;
    public bool previewable;
    public bool sameActionFollowUp;
    public SkillEffectData source;
}

public struct StatusDelta
{
    public SkillEffectTarget target;
    public StatusKind status;
    public int amount;
}

public class SkillResolvedResult
{
    public bool canCast = true;
    public string failureReason = string.Empty;
    public int resolvedAPCost;
    public int resolvedDiceCost;
    public readonly List<ResolvedEffect> effects = new List<ResolvedEffect>();
    public int damageDelta;
    public int guardDelta;
    public int healDelta;
    public readonly List<StatusDelta> statusDeltas = new List<StatusDelta>();
}

public static class SkillGameplayResolver
{
    public static bool CanResolveWithNewPipeline(SkillDamageSO skill)
        => skill != null && skill.gameplay != null && skill.gameplay.useNewGameplayPipeline;

    public static SkillResolvedResult Resolve(SkillDamageSO skill, SkillRuntime runtime, CombatActor caster, CombatActor target, SkillConditionContext conditionContext)
    {
        var context = BuildContext(skill, runtime, caster, target, conditionContext);
        return Resolve(context);
    }

    public static SkillResolvedResult Resolve(SkillRuntime runtime, CombatActor caster, CombatActor target)
    {
        SkillDamageSO skill = GetSourceSkill(runtime);
        return Resolve(skill, runtime, caster, target, BuildConditionContext(runtime, caster, target));
    }

    public static SkillDamageSO GetSourceSkill(SkillRuntime runtime)
        => runtime != null ? runtime.sourceAsset as SkillDamageSO : null;

    public static SkillResolvedResult Resolve(SkillResolveContext context)
    {
        var result = new SkillResolvedResult();
        if (context == null || context.skill == null || context.skill.gameplay == null)
        {
            result.canCast = false;
            result.failureReason = "Missing skill gameplay data.";
            return result;
        }

        SkillGameplayData gameplay = context.skill.gameplay;
        result.resolvedAPCost = context.runtime != null ? Mathf.Max(0, context.runtime.focusCost) : Mathf.Max(0, context.skill.focusCost);
        result.resolvedDiceCost = context.runtime != null ? Mathf.Clamp(context.runtime.slotsRequired, 1, 3) : Mathf.Clamp(context.skill.slotsRequired, 1, 3);

        if (!CheckRequirements(gameplay, context, result))
            return result;

        ResolveEffects(gameplay.baseEffects, context, result, sameActionFollowUp: false);

        if (gameplay.conditionalOutcomes != null)
        {
            for (int i = 0; i < gameplay.conditionalOutcomes.Count; i++)
            {
                SkillConditionalOutcomeDataV2 branch = gameplay.conditionalOutcomes[i];
                if (branch == null || branch.condition == null || !branch.condition.Evaluate(context.conditionContext))
                    continue;

                ResolveEffects(branch.effects, context, result, sameActionFollowUp: true);
            }
        }

        return result;
    }

    public static int ResolveValue(SkillValueData value, SkillResolveContext context)
    {
        if (value == null)
            return 0;

        int baseAmount = Mathf.Max(0, value.baseAmount);
        switch (value.mode)
        {
            case SkillValueMode.AddedValueScaled:
                return SkillOutputValueUtility.AddActionAddedValue(baseAmount, context != null ? context.runtime : null);
            case SkillValueMode.TargetStatusStacksScaled:
                return Mathf.Max(0, baseAmount * GetTargetStatusStacks(value.status, context));
            case SkillValueMode.ConsumedStatusStacksScaled:
                return Mathf.Max(0, baseAmount * GetConsumedStatusStacks(value.status, context));
            case SkillValueMode.ConsumedStatusStacksDividedScaled:
                return Mathf.Max(0, baseAmount * (GetConsumedStatusStacks(value.status, context) / Mathf.Max(1, value.divisor)));
            case SkillValueMode.MatchingBaseValueCountScaled:
                return Mathf.Max(0, baseAmount * CountMatchingBaseValues(value.matchBaseValue, context));
            case SkillValueMode.ActionX:
                return context != null ? SkillOutputValueUtility.ResolveXValue(0, context.runtime) : 0;
            case SkillValueMode.HighestBaseValueScaled:
                return SkillOutputValueUtility.AddActionAddedValue(GetHighestBaseValue(context), context != null ? context.runtime : null);
            case SkillValueMode.FirstResolvedValueInGroup:
                return GetResolvedValueAtIndex(context, 0);
            case SkillValueMode.LastResolvedValueInGroup:
                return GetResolvedValueAtIndex(context, GetLastResolvedIndex(context));
            case SkillValueMode.TotalBleedOnBoardScaled:
                return SkillOutputValueUtility.AddActionAddedValue(GetTotalBleedOnBoard(context), context != null ? context.runtime : null);
            case SkillValueMode.LastEnemyTurnHpLostScaled:
                return SkillOutputValueUtility.AddActionAddedValue(GetLastEnemyTurnHpLost(context), context != null ? context.runtime : null);
            case SkillValueMode.Fixed:
            default:
                return baseAmount;
        }
    }

    private static int GetHighestBaseValue(SkillResolveContext context)
    {
        if (context == null || context.runtime == null)
            return 0;

        return Mathf.Max(0, SkillBehaviorRuntimeUtility.GetHighestBaseValue(context.runtime));
    }

    private static int GetResolvedValueAtIndex(SkillResolveContext context, int index)
    {
        if (context == null || context.runtime == null || index < 0)
            return 0;

        return Mathf.Max(0, SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(context.runtime, index));
    }

    private static int GetLastResolvedIndex(SkillResolveContext context)
    {
        if (context == null || context.runtime == null || context.runtime.localResolvedValues == null)
            return -1;

        return context.runtime.localResolvedValues.Count - 1;
    }

    private static int GetTotalBleedOnBoard(SkillResolveContext context)
    {
        if (context != null && context.conditionContext != null)
            return Mathf.Max(0, context.conditionContext.totalBleedOnBoard);

        return CountTotalBleed(context != null ? context.caster : null);
    }

    private static int GetLastEnemyTurnHpLost(SkillResolveContext context)
    {
        if (context == null || context.caster == null)
            return 0;

        SkillCombatState state = context.caster.GetComponent<SkillCombatState>();
        return state == null ? 0 : Mathf.Max(0, state.LastEnemyTurnHpLost);
    }

    private static int CountMatchingBaseValues(int matchBaseValue, SkillResolveContext context)
    {
        if (context == null || context.conditionContext == null || context.conditionContext.localBaseValues == null)
            return 0;

        int count = 0;
        IReadOnlyList<int> bases = context.conditionContext.localBaseValues;
        IReadOnlyList<bool> numericFlags = context.conditionContext.localNumericFlags;
        for (int i = 0; i < bases.Count; i++)
        {
            if (numericFlags != null && i < numericFlags.Count && !numericFlags[i])
                continue;
            if (bases[i] == matchBaseValue)
                count++;
        }

        return count;
    }

    private static SkillResolveContext BuildContext(SkillDamageSO skill, SkillRuntime runtime, CombatActor caster, CombatActor target, SkillConditionContext conditionContext)
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

    public static SkillConditionContext BuildConditionContext(SkillRuntime runtime, CombatActor caster, CombatActor target)
    {
        return new SkillConditionContext
        {
            scope = SkillConditionScope.SlotBound,
            localBaseValues = runtime != null ? runtime.localBaseValues : null,
            localNumericFlags = runtime != null ? runtime.localNumericFlags : null,
            localResolvedValues = runtime != null ? runtime.localResolvedValues : null,
            localCritFlags = runtime != null ? runtime.localCritFlags : null,
            localFailFlags = runtime != null ? runtime.localFailFlags : null,
            currentFocus = caster != null ? caster.focus : 0,
            currentGuard = caster != null ? caster.guardPool : 0,
            targetGuard = target != null ? target.guardPool : 0,
            occupiedSlots = runtime != null ? Mathf.Clamp(runtime.slotsRequired, 1, 3) : 0,
            remainingSlots = runtime != null ? Mathf.Max(0, 3 - Mathf.Clamp(runtime.slotsRequired, 1, 3)) : 0,
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

    private static bool CheckRequirements(SkillGameplayData gameplay, SkillResolveContext context, SkillResolvedResult result)
    {
        if (gameplay.requirements == null)
            return true;

        for (int i = 0; i < gameplay.requirements.Count; i++)
        {
            SkillRequirementData requirement = gameplay.requirements[i];
            if (requirement == null || requirement.condition == null)
                continue;

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

    private static void ResolveEffects(List<SkillEffectData> effects, SkillResolveContext context, SkillResolvedResult result, bool sameActionFollowUp)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffectData effect = effects[i];
            if (effect == null)
                continue;

            List<CombatActor> targets = ResolveEffectTargets(effect.target, context);
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

                var resolved = new ResolvedEffect
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
            return null;

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

    public static List<CombatActor> ResolveEffectTargets(SkillEffectTarget effectTarget, SkillResolveContext context)
    {
        var targets = new List<CombatActor>();
        if (context == null)
            return targets;

        switch (effectTarget)
        {
            case SkillEffectTarget.Self:
                AddTarget(targets, context.caster);
                break;

            case SkillEffectTarget.RowEnemies:
                AddEnemyRowTargets(targets, context.caster, context.target);
                break;

            case SkillEffectTarget.AllEnemies:
                AddAllEnemyTargets(targets, context.caster, context.target);
                break;

            case SkillEffectTarget.SelectedEnemy:
            default:
                AddTarget(targets, context.target);
                break;
        }

        return targets;
    }

    private static void AddAllEnemyTargets(List<CombatActor> targets, CombatActor caster, CombatActor fallbackTarget)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead)
                continue;
            if (caster != null && actor.team == caster.team)
                continue;
            if (caster == null && fallbackTarget != null && actor.team != fallbackTarget.team)
                continue;
            AddTarget(targets, actor);
        }

        if (targets.Count == 0)
            AddTarget(targets, fallbackTarget);
    }

    private static void AddEnemyRowTargets(List<CombatActor> targets, CombatActor caster, CombatActor rowAnchor)
    {
        if (rowAnchor == null)
        {
            AddTarget(targets, null);
            return;
        }

        CombatActor.RowTag row = rowAnchor.row;
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead || actor.row != row)
                continue;
            if (caster != null && actor.team == caster.team)
                continue;
            if (caster == null && actor.team != rowAnchor.team)
                continue;
            AddTarget(targets, actor);
        }

        if (targets.Count == 0)
            AddTarget(targets, rowAnchor);
    }

    private static void AddTarget(List<CombatActor> targets, CombatActor target)
    {
        if (target == null || target.IsDead || targets.Contains(target))
            return;

        targets.Add(target);
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

    private static void CaptureConsumedStacks(StatusKind status, SkillResolveContext context)
    {
        if (context == null)
            return;

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
            return 0;

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
            return 0;

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

    private static int CountAliveEnemies(CombatActor caster)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor != null && actor != caster && !actor.IsDead && actor.team == CombatActor.TeamSide.Enemy)
                count++;
        }
        return count;
    }

    private static int CountEnemiesWithStatus(CombatActor caster, StatusKind status)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor == caster || actor.IsDead || actor.team != CombatActor.TeamSide.Enemy || actor.status == null)
                continue;
            if (status == StatusKind.Burn && actor.status.burnStacks > 0) count++;
            else if (status == StatusKind.Mark && actor.status.marked) count++;
            else if (status == StatusKind.Bleed && actor.status.bleedStacks > 0) count++;
            else if (status == StatusKind.Freeze && actor.status.frozen) count++;
            else if (status == StatusKind.Chilled && actor.status.chilledTurns > 0) count++;
        }
        return count;
    }

    private static int CountTotalBleed(CombatActor caster)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int total = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor != null && actor != caster && !actor.IsDead && actor.team == CombatActor.TeamSide.Enemy && actor.status != null)
                total += Mathf.Max(0, actor.status.bleedStacks);
        }
        return total;
    }

    private static int CountEnemiesWithAnyStatus(CombatActor caster)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor == caster || actor.IsDead || actor.team != CombatActor.TeamSide.Enemy || actor.status == null)
                continue;
            if (actor.status.burnStacks > 0 || actor.status.marked || actor.status.bleedStacks > 0 || actor.status.frozen || actor.status.chilledTurns > 0 || actor.status.staggered)
                count++;
        }
        return count;
    }
}

