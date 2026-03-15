using UnityEngine;

internal static class SkillPlanRuntimeUtility
{
    public static int GetSlotsRequired(ScriptableObject skill)
    {
        switch (skill)
        {
            case SkillDamageSO d: return Mathf.Clamp(d.slotsRequired, 1, 3);
            case SkillBuffDebuffSO b: return Mathf.Clamp(b.slotsRequired, 1, 3);
            default: return 1;
        }
    }

    public static SkillRuntime EvaluateRuntimeForSkillAsset(ScriptableObject skill, DiceSlotRig diceRig, int anchor0, int baseSpan, int baseStart0)
    {
        switch (skill)
        {
            case SkillDamageSO dmg:
                return SkillRuntimeEvaluator.Evaluate(dmg, diceRig, anchor0, baseSpan, baseStart0);

            case SkillBuffDebuffSO buff:
                return BuildRuntimeFromBuffDebuffSkill(buff);

            default:
                return null;
        }
    }

    private static TargetRule MapTargetRule(SkillTargetRule tr, out bool hitAllEnemies, out bool hitAllAllies)
    {
        hitAllEnemies = tr == SkillTargetRule.AllEnemies || tr == SkillTargetRule.AllUnits;
        hitAllAllies = tr == SkillTargetRule.AllAllies || tr == SkillTargetRule.AllUnits;

        if (tr == SkillTargetRule.Self) return TargetRule.Self;
        if (tr == SkillTargetRule.SingleEnemy) return TargetRule.Enemy;
        if (tr == SkillTargetRule.SingleAlly) return TargetRule.Self;
        if (tr == SkillTargetRule.AllEnemies) return TargetRule.Enemy;
        if (tr == SkillTargetRule.AllAllies) return TargetRule.Self;

        return TargetRule.Enemy;
    }

    private static ElementType MapElementTag(ElementTag tag)
    {
        switch (tag)
        {
            case ElementTag.Neutral: return ElementType.Neutral;
            case ElementTag.Physical: return ElementType.Physical;
            case ElementTag.Fire: return ElementType.Fire;
            case ElementTag.Ice: return ElementType.Ice;
            case ElementTag.Lightning: return ElementType.Lightning;
            default: return ElementType.Neutral;
        }
    }

    private static SkillRuntime BuildRuntimeFromDamageSkill(SkillDamageSO s)
    {
        if (s == null) return null;

        var rt = new SkillRuntime
        {
            sourceAsset = s,
            useV2Targeting = true,
            targetRuleV2 = s.target,
            coreAction = s.coreAction,
            kind = s.kind,
            group = s.group,
            element = MapElementTag(s.element),
            range = s.range,
            slotsRequired = Mathf.Clamp(s.slotsRequired, 1, 3),
            focusCost = Mathf.Max(0, s.focusCost),
            focusGainOnCast = s.focusGainOnCast,
            dieMultiplier = s.dieMultiplier,
            flatDamage = s.flatDamage,
            sunderBonusIfTargetHasGuard = s.sunderBonusIfTargetHasGuard,
            sunderGuardDamageMultiplier = s.sunderGuardDamageMultiplier,
            guardDieMultiplier = s.guardDieMultiplier,
            guardFlat = s.guardFlat,
            bypassGuard = s.bypassGuard,
            clearsGuard = s.clearsGuard,
            canUseMarkMultiplier = s.canUseMarkMultiplier,
            consumesBurn = s.consumesBurn,
            burnDamagePerStack = s.burnDamagePerStack,
            applyBurn = s.applyBurn,
            burnAddStacks = s.burnAddStacks,
            burnRefreshTurns = s.burnRefreshTurns,
            applyMark = s.applyMark,
            applyBleed = s.applyBleed,
            bleedTurns = s.bleedTurns,
            applyFreeze = s.applyFreeze,
            freezeChance = s.freezeChance,
            projectilePrefab = s.projectilePrefab,
            conditionMet = false
        };

        rt.target = MapTargetRule(s.target, out rt.hitAllEnemies, out rt.hitAllAllies);

        if (rt.kind == SkillKind.Guard)
        {
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }
        if (rt.target == TargetRule.Self) rt.hitAllEnemies = false;
        if (rt.target == TargetRule.Enemy) rt.hitAllAllies = false;

        return rt;
    }

    private static SkillRuntime BuildRuntimeFromBuffDebuffSkill(SkillBuffDebuffSO b)
    {
        if (b == null) return null;

        var rt = new SkillRuntime
        {
            sourceAsset = b,
            useV2Targeting = true,
            targetRuleV2 = b.target,
            kind = SkillKind.Utility,
            group = DamageGroup.Effect,
            element = ElementType.Neutral,
            range = RangeType.Ranged,
            slotsRequired = Mathf.Clamp(b.slotsRequired, 1, 3),
            focusCost = Mathf.Max(0, b.focusCost),
            focusGainOnCast = b.focusGainOnCast,
            dieMultiplier = 0,
            flatDamage = 0,
            conditionMet = false
        };

        rt.target = MapTargetRule(b.target, out rt.hitAllEnemies, out rt.hitAllAllies);
        if (rt.target == TargetRule.Self) rt.hitAllEnemies = false;
        if (rt.target == TargetRule.Enemy) rt.hitAllAllies = false;

        return rt;
    }
}
