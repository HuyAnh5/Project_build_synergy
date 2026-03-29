/// <summary>
/// Optional helper wrapper (kept for older code paths / readability).
/// </summary>
public static class SkillConditionEvaluator
{
    public static bool Evaluate(SkillConditionData cond, SkillConditionContext context)
    {
        if (cond == null) return false;
        return cond.Evaluate(context);
    }
}
