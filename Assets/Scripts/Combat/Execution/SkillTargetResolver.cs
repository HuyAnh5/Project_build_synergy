using System.Collections.Generic;

public static class SkillTargetResolver
{
    public static CombatActor ResolveTarget(SkillRuntime rt, CombatActor caster, CombatActor clicked, IReadOnlyList<CombatActor> aoeTargets)
    {
        if (rt == null) return null;

        if (rt.useV2Targeting)
        {
            switch (rt.targetRuleV2)
            {
                case SkillTargetRule.Self:
                    return caster;

                case SkillTargetRule.SingleAlly:
                    return clicked != null ? clicked : caster;

                case SkillTargetRule.AllAllies:
                    return caster;

                case SkillTargetRule.AllEnemies:
                case SkillTargetRule.AllUnits:
                    if (clicked != null) return clicked;
                    if (aoeTargets != null && aoeTargets.Count > 0) return aoeTargets[0];
                    return null;

                case SkillTargetRule.SingleEnemy:
                default:
                    return clicked;
            }
        }

        if (rt.target == TargetRule.Self) return caster;
        if (clicked != null) return clicked;
        if (aoeTargets != null && aoeTargets.Count > 0) return aoeTargets[0];
        return null;
    }
}
