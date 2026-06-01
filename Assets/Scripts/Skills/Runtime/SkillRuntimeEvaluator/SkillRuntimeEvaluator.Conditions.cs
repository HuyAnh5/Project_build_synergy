using System.Collections.Generic;

// Evaluates inspector-authored standard condition presets.
public static partial class SkillRuntimeEvaluator
{
    // Chooses the correct standard condition family evaluator.
    private static bool EvaluateBuilderCondition(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, SkillConditionContext context)
    {
        if (skill == null || context == null)
            return false;

        switch (skill.standardConditionFamily)
        {
            case SkillConditionFamily.DiceParity:
                return skill.diceParityConditionPreset == DiceParityConditionPreset.Even
                    ? AllEven(context.localBaseValues, context.localNumericFlags)
                    : AllOdd(context.localBaseValues, context.localNumericFlags);

            case SkillConditionFamily.CritFail:
                return skill.critFailConditionPreset == CritFailConditionPreset.Crit
                    ? AllTrue(context.localCritFlags)
                    : AllTrue(context.localFailFlags);

            case SkillConditionFamily.ExactValue:
                return EvaluateExactBuilderCondition(skill, owner, diceRig, context.localBaseValues, context.localNumericFlags);

            case SkillConditionFamily.LocalGroupRelation:
                return EvaluateLocalGroupCondition(skill, context);

            case SkillConditionFamily.Resource:
            case SkillConditionFamily.TargetState:
            case SkillConditionFamily.BoardState:
                return skill.condition != null && SkillConditionEvaluator.Evaluate(skill.condition, context);

            default:
                return false;
        }
    }

    // Evaluates exact-value condition modes, including per-combat random exact values.
    private static bool EvaluateExactBuilderCondition(
        SkillDamageSO skill,
        CombatActor owner,
        DiceSlotRig diceRig,
        IReadOnlyList<int> localBaseValues,
        IReadOnlyList<bool> localNumericFlags)
    {
        switch (skill.exactConditionMode)
        {
            case SkillExactConditionMode.DieEqualsX:
                return AllMatch(localBaseValues, localNumericFlags, skill.exactValueX);

            case SkillExactConditionMode.GroupContainsPattern:
                return ContainsAnyPatternValue(localBaseValues, localNumericFlags, ParseExactPattern(skill.exactValuePattern, ownedOnly: false, diceRig: diceRig));

            case SkillExactConditionMode.RandomExactNumberOwned:
            {
                int target = GetOrCreateRandomExactValue(skill, owner, diceRig, ownedOnly: true, fallbackValue: skill.exactValueX);
                return ContainsAnyPatternValue(localBaseValues, localNumericFlags, new List<int> { target });
            }

            case SkillExactConditionMode.RandomExactNumberRandom:
            {
                int target = GetOrCreateRandomExactValue(skill, owner, diceRig, ownedOnly: false, fallbackValue: skill.exactValueX);
                return ContainsAnyPatternValue(localBaseValues, localNumericFlags, new List<int> { target });
            }

            default:
                return false;
        }
    }

    // Evaluates local lane/group relation presets.
    private static bool EvaluateLocalGroupCondition(SkillDamageSO skill, SkillConditionContext context)
    {
        switch (skill.localGroupRelationMode)
        {
            case LocalGroupRelationMode.SelfPosition:
                return skill.localGroupRelationSide == LocalGroupRelationSide.Left
                    ? context.isLeftmostAction
                    : context.isRightmostAction;

            case LocalGroupRelationMode.NeighborRelation:
                return skill.localGroupRelationSide == LocalGroupRelationSide.Left
                    ? !context.isLeftmostAction
                    : !context.isRightmostAction;

            case LocalGroupRelationMode.SplitRole:
                return skill.localGroupConditionPreset == LocalGroupConditionPreset.Highest
                    ? context.localBaseValues != null && context.localBaseValues.Count > 0
                    : context.localBaseValues != null && context.localBaseValues.Count > 0;

            default:
                return false;
        }
    }

    // Gets a stable combat-scoped exact value, or falls back when no combat state exists.
    private static int GetOrCreateRandomExactValue(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, bool ownedOnly, int fallbackValue)
    {
        SkillCombatState state = owner != null ? owner.GetComponent<SkillCombatState>() : null;
        List<int> candidates = ownedOnly ? GatherOwnedFaceValues(diceRig) : null;
        int min = 1;
        int max = 99;

        if (state != null)
            return state.GetOrCreateExactConditionValue(skill != null ? skill.GetInstanceID() : 0, candidates, min, max, fallbackValue);

        if (candidates != null && candidates.Count > 0)
            return candidates[0];

        return fallbackValue;
    }

    // Parses exact-value patterns like "1-3,7" into unique integers.
    private static List<int> ParseExactPattern(string pattern, bool ownedOnly, DiceSlotRig diceRig)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(pattern))
            return result;

        List<int> owned = ownedOnly ? GatherOwnedFaceValues(diceRig) : null;
        string[] parts = pattern.Split(new[] { '-', ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int parsed))
                continue;

            if (ownedOnly && (owned == null || !owned.Contains(parsed)))
                continue;

            if (!result.Contains(parsed))
                result.Add(parsed);
        }

        return result;
    }

    // Checks whether any numeric local die matches the pattern list.
    private static bool ContainsAnyPatternValue(IReadOnlyList<int> localBaseValues, IReadOnlyList<bool> numericFlags, List<int> patternValues)
    {
        if (localBaseValues == null || localBaseValues.Count == 0 || patternValues == null || patternValues.Count == 0)
            return false;

        for (int i = 0; i < localBaseValues.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;
            if (patternValues.Contains(localBaseValues[i]))
                return true;
        }

        return false;
    }

    // Requires every local numeric die to equal the target value.
    private static bool AllMatch(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags, int target)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                return false;
            if (values[i] != target)
                return false;
        }

        return true;
    }

    // Requires every local numeric die to be even.
    private static bool AllEven(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                return false;
            if ((values[i] % 2) != 0)
                return false;
        }

        return true;
    }

    // Requires every local numeric die to be odd.
    private static bool AllOdd(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                return false;
            if ((values[i] % 2) == 0)
                return false;
        }

        return true;
    }

    // Requires every boolean flag to be true.
    private static bool AllTrue(IReadOnlyList<bool> values)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (!values[i])
                return false;
        }

        return true;
    }
}
