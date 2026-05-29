using System.Collections.Generic;
using UnityEngine;

public static partial class SkillGameplayResolver
{
    /// <summary>
    /// Resolves an authored value block into a runtime number using the current resolve context.
    /// </summary>
    public static int ResolveValue(SkillValueData value, SkillResolveContext context)
    {
        if (value == null)
        {
            return 0;
        }

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
        {
            return 0;
        }

        return Mathf.Max(0, SkillBehaviorRuntimeUtility.GetHighestBaseValue(context.runtime));
    }

    private static int GetResolvedValueAtIndex(SkillResolveContext context, int index)
    {
        if (context == null || context.runtime == null || index < 0)
        {
            return 0;
        }

        return Mathf.Max(0, SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(context.runtime, index));
    }

    private static int GetLastResolvedIndex(SkillResolveContext context)
    {
        if (context == null || context.runtime == null || context.runtime.localResolvedValues == null)
        {
            return -1;
        }

        return context.runtime.localResolvedValues.Count - 1;
    }

    private static int GetTotalBleedOnBoard(SkillResolveContext context)
    {
        if (context != null && context.conditionContext != null)
        {
            return Mathf.Max(0, context.conditionContext.totalBleedOnBoard);
        }

        return CountTotalBleed(context != null ? context.caster : null);
    }

    private static int GetLastEnemyTurnHpLost(SkillResolveContext context)
    {
        if (context == null || context.caster == null)
        {
            return 0;
        }

        SkillCombatState state = context.caster.GetComponent<SkillCombatState>();
        return state == null ? 0 : Mathf.Max(0, state.LastEnemyTurnHpLost);
    }

    private static int CountMatchingBaseValues(int matchBaseValue, SkillResolveContext context)
    {
        if (context == null || context.conditionContext == null || context.conditionContext.localBaseValues == null)
        {
            return 0;
        }

        int count = 0;
        IReadOnlyList<int> baseValues = context.conditionContext.localBaseValues;
        IReadOnlyList<bool> numericFlags = context.conditionContext.localNumericFlags;
        for (int i = 0; i < baseValues.Count; i++)
        {
            if (numericFlags != null && i < numericFlags.Count && !numericFlags[i])
            {
                continue;
            }

            if (baseValues[i] == matchBaseValue)
            {
                count++;
            }
        }

        return count;
    }
}
