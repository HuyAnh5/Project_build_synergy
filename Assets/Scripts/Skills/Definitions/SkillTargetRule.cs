// SkillTargetRule.cs
public enum SkillTargetRule
{
    Self,
    SingleEnemy,
    SingleAlly,
    RowEnemies,
    RowAllies,
    AllEnemies,
    AllAllies,
}

public static class SkillTargetRuleUtility
{
    public static bool IsEnemySideTarget(SkillTargetRule rule)
    {
        return rule == SkillTargetRule.SingleEnemy ||
               rule == SkillTargetRule.RowEnemies ||
               rule == SkillTargetRule.AllEnemies;
    }

    public static bool IsAllySideTarget(SkillTargetRule rule)
    {
        return rule == SkillTargetRule.Self ||
               rule == SkillTargetRule.SingleAlly ||
               rule == SkillTargetRule.RowAllies ||
               rule == SkillTargetRule.AllAllies;
    }

    public static bool IsRowTarget(SkillTargetRule rule)
    {
        return rule == SkillTargetRule.RowEnemies ||
               rule == SkillTargetRule.RowAllies;
    }

    public static bool IsMultiTarget(SkillTargetRule rule)
    {
        return rule == SkillTargetRule.RowEnemies ||
               rule == SkillTargetRule.RowAllies ||
               rule == SkillTargetRule.AllEnemies ||
               rule == SkillTargetRule.AllAllies;
    }

    public static bool IsFullSideTarget(SkillTargetRule rule)
    {
        return rule == SkillTargetRule.AllEnemies ||
               rule == SkillTargetRule.AllAllies;
    }
}
