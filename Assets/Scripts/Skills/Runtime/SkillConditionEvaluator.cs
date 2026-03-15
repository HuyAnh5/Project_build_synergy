using System.Collections.Generic;

/// <summary>
/// Optional helper wrapper (kept for older code paths / readability).
/// </summary>
public static class SkillConditionEvaluator
{
    public static bool Evaluate(SkillConditionData cond, IReadOnlyList<int> diceValues)
    {
        if (cond == null) return false;
        return cond.Evaluate(diceValues);
    }
}
