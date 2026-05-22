using UnityEngine;

public static class SkillOutputValueUtility
{
    public static int ResolvePerDieOutput(int baseValue, int resolvedValue, bool appliesFailPenalty)
    {
        int safeBase = Mathf.Max(0, baseValue);
        int safeResolved = Mathf.Max(0, resolvedValue);
        int addedValue = Mathf.Max(0, safeResolved - safeBase);
        int unmodifiedTotal = safeBase + addedValue;
        int total = appliesFailPenalty ? Mathf.FloorToInt(unmodifiedTotal * 0.5f) : unmodifiedTotal;

        if (unmodifiedTotal > 0 && total < 1)
            total = 1;

        return Mathf.Max(0, total);
    }

    public static int GetTotalActionAddedValue(SkillRuntime rt)
    {
        if (rt == null || rt.localBaseValues == null || rt.localResolvedValues == null)
            return 0;

        int count = Mathf.Min(rt.localBaseValues.Count, rt.localResolvedValues.Count);
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            total += Mathf.Max(0, rt.localResolvedValues[i] - rt.localBaseValues[i]);
        }

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
        int unmodifiedTotal = safeBase + GetTotalActionAddedValue(rt);
        int total = (rt != null && rt.localFailPenaltyAny)
            ? Mathf.FloorToInt(unmodifiedTotal * 0.5f)
            : unmodifiedTotal;

        if (unmodifiedTotal > 0 && total < 1)
            total = 1;

        return Mathf.Max(0, total);
    }

    public static int ResolveXValue(int resolvedValue)
    {
        return Mathf.Max(0, resolvedValue);
    }

    public static int ResolveXValue(int resolvedValue, SkillRuntime rt)
    {
        if (rt == null || rt.localBaseValues == null || rt.localBaseValues.Count == 0)
            return ResolveXValue(resolvedValue);

        int baseOutput = 0;
        for (int i = 0; i < rt.localBaseValues.Count; i++)
            baseOutput += Mathf.Max(0, rt.localBaseValues[i]);

        int unmodifiedTotal = baseOutput + GetTotalActionAddedValue(rt);
        int total = rt.localFailPenaltyAny
            ? Mathf.FloorToInt(unmodifiedTotal * 0.5f)
            : unmodifiedTotal;

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
