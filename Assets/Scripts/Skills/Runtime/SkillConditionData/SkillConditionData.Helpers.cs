using System.Collections.Generic;

// Helper math and collection checks used by SkillConditionData clause evaluation.
public partial class SkillConditionData
{
    // Checks whether any numeric base value satisfies the clause comparison.
    private static bool EvaluateAnyBaseValue(SkillConditionClause clause, IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;
            if (EvaluateInt(clause, values[i]))
                return true;
        }

        return false;
    }

    // Applies integer comparisons, including parity checks.
    private static bool EvaluateInt(SkillConditionClause clause, int actual)
    {
        switch (clause.comparison)
        {
            case SkillConditionComparison.IsOdd: return (actual % 2) != 0;
            case SkillConditionComparison.IsEven: return (actual % 2) == 0;
            case SkillConditionComparison.Equals: return actual == clause.value;
            case SkillConditionComparison.NotEquals: return actual != clause.value;
            case SkillConditionComparison.GreaterOrEqual: return actual >= clause.value;
            case SkillConditionComparison.LessOrEqual: return actual <= clause.value;
            default: return false;
        }
    }

    // Applies boolean comparisons.
    private static bool EvaluateBool(SkillConditionClause clause, bool actual)
    {
        switch (clause.comparison)
        {
            case SkillConditionComparison.IsTrue: return actual;
            case SkillConditionComparison.IsFalse: return !actual;
            default: return false;
        }
    }

    // Returns the highest numeric value in a local dice group.
    private static int GetHighest(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
    {
        if (values == null || values.Count == 0)
            return 0;

        bool found = false;
        int best = 0;
        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;
            if (!found || values[i] > best)
            {
                best = values[i];
                found = true;
            }
        }

        return found ? best : 0;
    }

    // Returns the lowest numeric value in a local dice group.
    private static int GetLowest(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
    {
        if (values == null || values.Count == 0)
            return 0;

        bool found = false;
        int best = 0;
        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;
            if (!found || values[i] < best)
            {
                best = values[i];
                found = true;
            }
        }

        return found ? best : 0;
    }

    // Sums only numeric base values.
    private static int Sum(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
    {
        if (values == null || values.Count == 0)
            return 0;

        int sum = 0;
        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;
            sum += values[i];
        }
        return sum;
    }

    // Sums resolved values where numeric flags no longer apply.
    private static int Sum(IReadOnlyList<int> values)
    {
        if (values == null || values.Count == 0)
            return 0;

        int sum = 0;
        for (int i = 0; i < values.Count; i++)
            sum += values[i];
        return sum;
    }

    // Checks whether any Added Value in the group satisfies the clause comparison.
    private static bool EvaluateAnyAddedValue(
        SkillConditionClause clause,
        IReadOnlyList<int> outputBaseValues,
        IReadOnlyList<int> baseValues,
        IReadOnlyList<int> resolvedValues,
        IReadOnlyList<bool> numericFlags)
    {
        int count = GetAddedValueCount(outputBaseValues, baseValues, resolvedValues);
        for (int i = 0; i < count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;
            if (EvaluateInt(clause, GetAddedValueAt(outputBaseValues, baseValues, resolvedValues, i)))
                return true;
        }

        return false;
    }

    // Evaluates Added Value at a specific local dice index.
    private static bool EvaluateIndexedAddedValue(
        SkillConditionClause clause,
        IReadOnlyList<int> outputBaseValues,
        IReadOnlyList<int> baseValues,
        IReadOnlyList<int> resolvedValues,
        IReadOnlyList<bool> numericFlags,
        int index)
    {
        if (index < 0 || index >= GetAddedValueCount(outputBaseValues, baseValues, resolvedValues))
            return false;
        if (!IsNumeric(numericFlags, index))
            return false;

        return EvaluateInt(clause, GetAddedValueAt(outputBaseValues, baseValues, resolvedValues, index));
    }

    // Returns the highest Added Value in the group.
    private static int GetHighestAddedValue(
        IReadOnlyList<int> outputBaseValues,
        IReadOnlyList<int> baseValues,
        IReadOnlyList<int> resolvedValues,
        IReadOnlyList<bool> numericFlags)
    {
        int count = GetAddedValueCount(outputBaseValues, baseValues, resolvedValues);
        bool found = false;
        int best = 0;
        for (int i = 0; i < count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;

            int added = GetAddedValueAt(outputBaseValues, baseValues, resolvedValues, i);
            if (!found || added > best)
            {
                best = added;
                found = true;
            }
        }

        return found ? best : 0;
    }

    // Sums Added Value across the group.
    private static int SumAddedValues(
        IReadOnlyList<int> outputBaseValues,
        IReadOnlyList<int> baseValues,
        IReadOnlyList<int> resolvedValues,
        IReadOnlyList<bool> numericFlags)
    {
        int count = GetAddedValueCount(outputBaseValues, baseValues, resolvedValues);
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;

            sum += GetAddedValueAt(outputBaseValues, baseValues, resolvedValues, i);
        }

        return sum;
    }

    private static int GetLastAddedValueIndex(IReadOnlyList<int> outputBaseValues, IReadOnlyList<int> baseValues, IReadOnlyList<int> resolvedValues)
    {
        return GetAddedValueCount(outputBaseValues, baseValues, resolvedValues) - 1;
    }

    private static int GetAddedValueCount(IReadOnlyList<int> outputBaseValues, IReadOnlyList<int> baseValues, IReadOnlyList<int> resolvedValues)
    {
        if (resolvedValues == null || resolvedValues.Count == 0)
            return 0;

        IReadOnlyList<int> baseSource = outputBaseValues != null && outputBaseValues.Count == resolvedValues.Count
            ? outputBaseValues
            : baseValues;
        if (baseSource == null || baseSource.Count == 0)
            return 0;

        return resolvedValues.Count < baseSource.Count ? resolvedValues.Count : baseSource.Count;
    }

    private static int GetAddedValueAt(IReadOnlyList<int> outputBaseValues, IReadOnlyList<int> baseValues, IReadOnlyList<int> resolvedValues, int index)
    {
        IReadOnlyList<int> baseSource = outputBaseValues != null && outputBaseValues.Count == resolvedValues.Count
            ? outputBaseValues
            : baseValues;
        int added = resolvedValues[index] - baseSource[index];
        return added > 0 ? added : 0;
    }

    // Checks whether any flag is true.
    private static bool AnyTrue(IReadOnlyList<bool> flags)
    {
        if (flags == null || flags.Count == 0)
            return false;

        for (int i = 0; i < flags.Count; i++)
        {
            if (flags[i])
                return true;
        }

        return false;
    }

    // Checks whether every flag is true.
    private static bool AllTrue(IReadOnlyList<bool> flags)
    {
        if (flags == null || flags.Count == 0)
            return false;

        for (int i = 0; i < flags.Count; i++)
        {
            if (!flags[i])
                return false;
        }

        return true;
    }

    // Checks whether every numeric base value is odd.
    private static bool AreAllValuesOdd(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
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

    // Checks whether every numeric base value is even.
    private static bool AreAllValuesEven(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
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

    // Evaluates an indexed dice value, returning false when the slot is missing or non-numeric.
    private static bool EvaluateIndexedValue(SkillConditionClause clause, IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags, int index)
    {
        if (!HasIndex(values, index))
            return false;
        if (!IsNumeric(numericFlags, index))
            return false;

        return EvaluateInt(clause, values[index]);
    }

    // Evaluates an indexed boolean flag.
    private static bool EvaluateIndexedBool(SkillConditionClause clause, IReadOnlyList<bool> values, int index)
    {
        if (!HasIndex(values, index))
            return false;

        return EvaluateBool(clause, values[index]);
    }

    // Checks whether a local dice group contains both odd and even numeric values.
    private static bool HasMixedParity(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags)
    {
        if (values == null || values.Count < 2)
            return false;

        bool sawOdd = false;
        bool sawEven = false;
        for (int i = 0; i < values.Count; i++)
        {
            if (!IsNumeric(numericFlags, i))
                continue;
            if ((values[i] % 2) == 0) sawEven = true;
            else sawOdd = true;

            if (sawOdd && sawEven)
                return true;
        }

        return false;
    }

    // Checks whether a list contains an index.
    private static bool HasIndex<T>(IReadOnlyList<T> values, int index)
    {
        return values != null && index >= 0 && index < values.Count;
    }

    // Returns the last valid index, or -1 when the list is empty.
    private static int GetLastIndex<T>(IReadOnlyList<T> values)
    {
        return values != null && values.Count > 0 ? values.Count - 1 : -1;
    }

    // Checks whether the indexed value equals the group maximum.
    private static bool IsHighestAtIndex(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags, int index)
    {
        if (!HasIndex(values, index))
            return false;
        if (!IsNumeric(numericFlags, index))
            return false;

        return values[index] == GetHighest(values, numericFlags);
    }

    // Checks whether the indexed value equals the group minimum.
    private static bool IsLowestAtIndex(IReadOnlyList<int> values, IReadOnlyList<bool> numericFlags, int index)
    {
        if (!HasIndex(values, index))
            return false;
        if (!IsNumeric(numericFlags, index))
            return false;

        return values[index] == GetLowest(values, numericFlags);
    }

    // Treats missing numeric flags as numeric for legacy conditions.
    private static bool IsNumeric(IReadOnlyList<bool> numericFlags, int index)
    {
        return numericFlags == null || (index >= 0 && index < numericFlags.Count && numericFlags[index]);
    }
}
