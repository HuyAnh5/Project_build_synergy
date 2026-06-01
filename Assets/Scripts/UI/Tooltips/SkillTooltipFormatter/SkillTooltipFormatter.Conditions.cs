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
            return $"Base Value = {clause.value}";
        return clause.Summary;
    }
}
