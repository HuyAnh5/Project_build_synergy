using System.Collections.Generic;

// Formats condition data into compact tooltip branch text.
public static partial class SkillTooltipFormatter
{
    // Converts a condition group into a readable "and/or" phrase.
    private static string FormatCondition(SkillConditionData condition)
    {
        if (condition == null || condition.clauses == null || condition.clauses.Count == 0)
            return "condition";

        var parts = new List<string>();
        for (int i = 0; i < condition.clauses.Count; i++)
        {
            SkillConditionClause clause = condition.clauses[i];
            if (clause == null)
                continue;
            parts.Add(FormatClause(clause));
        }

        string joiner = condition.logic == SkillConditionLogic.Any ? " or " : " and ";
        return string.Join(joiner, parts);
    }

    // Converts a single condition clause into short player-facing text.
    private static string FormatClause(SkillConditionClause clause)
    {
        if (clause.reference == SkillConditionReference.AnyBaseValue && clause.comparison == SkillConditionComparison.IsOdd)
            return "Odd";
        if (clause.reference == SkillConditionReference.AnyBaseValue && clause.comparison == SkillConditionComparison.IsEven)
            return "Even";
        if (clause.reference == SkillConditionReference.AnyBaseValue && clause.comparison == SkillConditionComparison.Equals)
            return $"Base = {clause.value}";

        string referenceText = FormatReference(clause.reference);
        string comparisonText = FormatComparison(clause.comparison);
        return clause.NeedsValue
            ? $"{referenceText} {comparisonText} {clause.value}"
            : $"{referenceText} {comparisonText}";
    }

    private static string FormatReference(SkillConditionReference reference)
    {
        switch (reference)
        {
            case SkillConditionReference.AnyBaseValue: return "Any Base";
            case SkillConditionReference.FirstBaseValueInGroup: return "First Base";
            case SkillConditionReference.MiddleBaseValueInGroup: return "Middle Base";
            case SkillConditionReference.LastBaseValueInGroup: return "Last Base";
            case SkillConditionReference.HighestBaseValueInGroup: return "Highest Base";
            case SkillConditionReference.LowestBaseValueInGroup: return "Lowest Base";
            case SkillConditionReference.TotalBaseValueInGroup: return "Base";
            case SkillConditionReference.TotalResolvedValueInGroup: return "Resolved";
            case SkillConditionReference.AllBaseValuesOdd: return "All Base Odd";
            case SkillConditionReference.AllBaseValuesEven: return "All Base Even";
            case SkillConditionReference.MixedParityInGroup: return "Mixed Odd/Even";
            case SkillConditionReference.AnyDieCrit: return "Crit";
            case SkillConditionReference.AnyDieFail: return "Fail";
            case SkillConditionReference.FirstDieCrit: return "First Crit";
            case SkillConditionReference.MiddleDieCrit: return "Middle Crit";
            case SkillConditionReference.LastDieCrit: return "Last Crit";
            case SkillConditionReference.FirstDieFail: return "First Fail";
            case SkillConditionReference.MiddleDieFail: return "Middle Fail";
            case SkillConditionReference.LastDieFail: return "Last Fail";
            case SkillConditionReference.AllDiceCrit: return "All Crit";
            case SkillConditionReference.AllDiceFail: return "All Fail";
            case SkillConditionReference.CurrentFocus: return "Focus";
            case SkillConditionReference.CurrentGuard: return "Guard";
            case SkillConditionReference.TargetGuard: return "Target Guard";
            case SkillConditionReference.OccupiedSlots: return "Occupied";
            case SkillConditionReference.RemainingSlots: return "Remaining";
            case SkillConditionReference.HasFirstDieInGroup: return "Has First";
            case SkillConditionReference.HasMiddleDieInGroup: return "Has Middle";
            case SkillConditionReference.HasLastDieInGroup: return "Has Last";
            case SkillConditionReference.HighestBaseIsInFirstSlot: return "Highest is First";
            case SkillConditionReference.HighestBaseIsInLastSlot: return "Highest is Last";
            case SkillConditionReference.LowestBaseIsInFirstSlot: return "Lowest is First";
            case SkillConditionReference.LowestBaseIsInLastSlot: return "Lowest is Last";
            case SkillConditionReference.EnemiesWithBurnCount: return "Burn Enemies";
            case SkillConditionReference.MarkedEnemiesCount: return "Marked Enemies";
            case SkillConditionReference.TotalBleedOnBoard: return "Bleed";
            case SkillConditionReference.AliveEnemiesCount: return "Alive Enemies";
            case SkillConditionReference.EnemiesWithStatusCount: return "Status Enemies";
            case SkillConditionReference.IsLeftmostAction: return "Leftmost";
            case SkillConditionReference.IsRightmostAction: return "Rightmost";
            case SkillConditionReference.TargetHasBurn: return "Target Burn";
            case SkillConditionReference.TargetHasFreeze: return "Target Freeze";
            case SkillConditionReference.TargetHasChilled: return "Target Chilled";
            case SkillConditionReference.TargetHasMark: return "Target Mark";
            case SkillConditionReference.TargetHasBleed: return "Target Bleed";
            case SkillConditionReference.TargetHasStagger: return "Target Stagger";
            case SkillConditionReference.AnyAddedValueInGroup: return "Any Added";
            case SkillConditionReference.FirstAddedValueInGroup: return "First Added";
            case SkillConditionReference.MiddleAddedValueInGroup: return "Middle Added";
            case SkillConditionReference.LastAddedValueInGroup: return "Last Added";
            case SkillConditionReference.HighestAddedValueInGroup: return "Highest Added";
            case SkillConditionReference.TotalAddedValueInGroup: return "Added";
            default: return reference.ToString();
        }
    }

    private static string FormatComparison(SkillConditionComparison comparison)
    {
        switch (comparison)
        {
            case SkillConditionComparison.IsOdd: return "is Odd";
            case SkillConditionComparison.IsEven: return "is Even";
            case SkillConditionComparison.Equals: return "=";
            case SkillConditionComparison.NotEquals: return "!=";
            case SkillConditionComparison.GreaterOrEqual: return ">=";
            case SkillConditionComparison.LessOrEqual: return "<=";
            case SkillConditionComparison.IsTrue: return "is true";
            case SkillConditionComparison.IsFalse: return "is false";
            default: return comparison.ToString();
        }
    }
}
