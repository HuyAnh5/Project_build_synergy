using System.Collections.Generic;
using UnityEngine;

public static class SkillOutputValueUtility
{
    public static bool IsMeleeAttack(SkillRuntime rt)
    {
        return rt != null && rt.kind == SkillKind.Attack && rt.range == RangeType.Melee;
    }

    public static int ResolvePerDieOutput(int baseValue, int resolvedValue, bool appliesFailPenalty)
    {
        int safeBase = Mathf.Max(0, baseValue);
        int safeResolved = Mathf.Max(0, resolvedValue);
        int addedValue = Mathf.Max(0, safeResolved - safeBase);
        int reducedBase = appliesFailPenalty ? Mathf.FloorToInt(safeBase * 0.5f) : safeBase;
        int unmodifiedTotal = safeBase + addedValue;
        int total = reducedBase + addedValue;

        if (unmodifiedTotal > 0 && total < 1)
            total = 1;

        return Mathf.Max(0, total);
    }

    public static int GetTotalActionAddedValue(SkillRuntime rt)
    {
        if (rt == null || rt.localBaseValues == null || rt.localResolvedValues == null)
            return 0;

        IReadOnlyList<int> outputBaseValues = rt.localOutputBaseValues != null && rt.localOutputBaseValues.Count == rt.localResolvedValues.Count
            ? rt.localOutputBaseValues
            : rt.localBaseValues;
        int count = Mathf.Min(outputBaseValues.Count, rt.localResolvedValues.Count);
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            total += Mathf.Max(0, rt.localResolvedValues[i] - outputBaseValues[i]);
        }

        if (IsMeleeAttack(rt))
            total += Mathf.Max(0, rt.ownerFlatDamageBonus);

        total += Mathf.Max(0, rt.ownerActionAddedValueBonus);

        if (rt.conditionMet && rt.conditionalOutcomeEnabled && rt.conditionalOutcomeType == ConditionalOutcomeType.GainAddedValue)
        {
            int bonusAddedValue = rt.conditionalOutcomeValueMode == ConditionalOutcomeValueMode.X
                ? ResolveXValue(GetHighestResolvedValue(rt))
                : Mathf.Max(0, rt.conditionalOutcomeFlatValue);
            total += bonusAddedValue;
        }

        return Mathf.Max(0, total);
    }

    public static int AddActionAddedValue(int baseAmount, SkillRuntime rt)
    {
        int safeBase = Mathf.Max(0, baseAmount);
        int addedValue = GetTotalActionAddedValue(rt);
        int reducedBase = (rt != null && rt.localFailPenaltyAny)
            ? Mathf.FloorToInt(safeBase * 0.5f)
            : safeBase;
        int unmodifiedTotal = safeBase + addedValue;
        int total = reducedBase + addedValue;

        if (unmodifiedTotal > 0 && total < 1)
            total = 1;

        return Mathf.Max(0, total);
    }

    public static int ResolveFlatStatusStacks(int baseStacks)
    {
        return Mathf.Max(0, baseStacks);
    }

    public static int ResolveStatusStacks(int baseStacks, SkillRuntime rt, BaseEffectValueMode valueMode, int resolvedDieValue, bool forceResolvedValue = false)
    {
        if (valueMode == BaseEffectValueMode.X || forceResolvedValue)
            return ResolveXValue(resolvedDieValue, rt);

        return ResolveFlatStatusStacks(baseStacks);
    }

    public static int ResolveConditionalStatusStacks(int baseStacks, SkillRuntime rt, ConditionalOutcomeValueMode valueMode, int resolvedDieValue)
    {
        if (valueMode == ConditionalOutcomeValueMode.X)
            return ResolveXValue(resolvedDieValue, rt);

        return ResolveFlatStatusStacks(baseStacks);
    }

    public static int ResolveXValue(int resolvedValue)
    {
        return Mathf.Max(0, resolvedValue);
    }

    public static int ResolveXValue(int resolvedValue, SkillRuntime rt)
    {
        if (rt == null)
            return ResolveXValue(resolvedValue);

        IReadOnlyList<int> outputBaseValues = rt.localOutputBaseValues != null && rt.localOutputBaseValues.Count > 0
            ? rt.localOutputBaseValues
            : rt.localBaseValues;
        if (outputBaseValues == null || outputBaseValues.Count == 0)
            return ResolveXValue(resolvedValue);

        int baseOutput = 0;
        for (int i = 0; i < outputBaseValues.Count; i++)
            baseOutput += Mathf.Max(0, outputBaseValues[i]);

        int addedValue = GetTotalActionAddedValue(rt);
        int reducedBase = rt.localFailPenaltyAny
            ? Mathf.FloorToInt(baseOutput * 0.5f)
            : baseOutput;
        int unmodifiedTotal = baseOutput + addedValue;
        int total = reducedBase + addedValue;

        if (unmodifiedTotal > 0 && total < 1)
            total = 1;

        return Mathf.Max(0, total);
    }

    private static int GetHighestResolvedValue(SkillRuntime rt)
    {
        if (rt == null || rt.localResolvedValues == null || rt.localResolvedValues.Count == 0)
            return 0;

        int best = rt.localResolvedValues[0];
        for (int i = 1; i < rt.localResolvedValues.Count; i++)
            best = Mathf.Max(best, rt.localResolvedValues[i]);
        return best;
    }
}
