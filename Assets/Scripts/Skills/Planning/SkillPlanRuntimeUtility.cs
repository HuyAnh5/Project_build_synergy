using System.Collections.Generic;
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

    public static SkillRuntime EvaluateRuntimeForSkillAsset(ScriptableObject skill, CombatActor owner, DiceSlotRig diceRig, int anchor0, int baseSpan, int baseStart0)
    {
        switch (skill)
        {
            case SkillDamageSO dmg:
                return SkillRuntimeEvaluator.Evaluate(dmg, owner, diceRig, anchor0, baseSpan, baseStart0);

            case SkillBuffDebuffSO buff:
                return BuildRuntimeFromBuffDebuffSkill(buff, owner, diceRig, baseSpan, baseStart0);

            default:
                return null;
        }
    }

    private static TargetRule MapTargetRule(SkillTargetRule tr, out bool hitAllEnemies, out bool hitAllAllies)
    {
        hitAllEnemies = tr == SkillTargetRule.RowEnemies || tr == SkillTargetRule.AllEnemies;
        hitAllAllies = tr == SkillTargetRule.RowAllies || tr == SkillTargetRule.AllAllies;

        if (tr == SkillTargetRule.Self) return TargetRule.Self;
        if (tr == SkillTargetRule.SingleEnemy) return TargetRule.Enemy;
        if (tr == SkillTargetRule.SingleAlly) return TargetRule.Self;
        if (tr == SkillTargetRule.RowEnemies) return TargetRule.Enemy;
        if (tr == SkillTargetRule.RowAllies) return TargetRule.Self;
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
            baseDamageValueMode = s.baseDamageValueMode,
            sunderBonusIfTargetHasGuard = s.sunderBonusIfTargetHasGuard,
            sunderGuardDamageMultiplier = s.sunderGuardDamageMultiplier,
            guardValueMode = s.guardValueMode,
            guardFlat = s.guardFlat,
            bypassGuard = s.bypassGuard,
            clearsGuard = s.clearsGuard,
            canUseMarkMultiplier = s.canUseMarkMultiplier,
            consumesBurn = s.consumesBurn,
            burnDamagePerStack = s.burnDamagePerStack,
            fireUseXFormula = s.fireModules != null && s.fireModules.useXFormula,
            fireApplyBurnFromResolvedValue = s.fireModules != null && s.fireModules.applyBurnFromResolvedValue,
            fireGrantBonusBurnOnOddBase = s.fireModules != null && s.fireModules.grantBonusBurnOnOddBase,
            fireOddBaseBonusBurn = s.fireModules != null ? s.fireModules.oddBaseBonusBurn : 0,
            fireApplyBurnFromLowestBase = s.fireModules != null && s.fireModules.applyBurnFromLowestBase,
            fireGainGuardFromHighestBase = s.fireModules != null && s.fireModules.gainGuardFromHighestBase,
            fireReapplyBurnPerExactBase = s.fireModules != null && s.fireModules.reapplyBurnPerExactBase,
            fireExactBaseForReapply = s.fireModules != null ? s.fireModules.exactBaseForReapply : 0,
            fireBurnPerExactMatch = s.fireModules != null ? s.fireModules.burnPerExactMatch : 0,
            fireRequireBurnBeforeHitForReapply = s.fireModules == null || s.fireModules.requireBurnBeforeHitForReapply,
            fireApplyConsumeBonusDebuff = s.fireModules != null && s.fireModules.applyConsumeBonusDebuff,
            fireConsumeBonusPerBurn = s.fireModules != null ? s.fireModules.consumeBonusPerBurn : 0,
            fireConsumeBonusDebuffTurns = s.fireModules != null ? s.fireModules.consumeBonusDebuffTurns : 0,
            applyBurn = s.applyBurn,
            burnAddStacks = s.burnAddStacks,
            burnRefreshTurns = s.burnRefreshTurns,
            baseBurnValueMode = s.baseBurnValueMode,
            applyMark = s.applyMark,
            applyBleed = s.applyBleed,
            bleedTurns = s.bleedTurns,
            applyFreeze = s.applyFreeze,
            freezeChance = s.freezeChance,
            projectilePrefab = s.projectilePrefab,
            conditionalOutcomeEnabled = s.conditionalOutcome != null && s.conditionalOutcome.enabled,
            conditionalOutcomeType = s.conditionalOutcome != null ? s.conditionalOutcome.type : ConditionalOutcomeType.None,
            conditionalOutcomeValueMode = s.conditionalOutcome != null ? s.conditionalOutcome.valueMode : ConditionalOutcomeValueMode.Flat,
            conditionalOutcomeFlatValue = s.conditionalOutcome != null ? s.conditionalOutcome.flatValue : 0,
            conditionalOutcomeBurnTurns = s.conditionalOutcome != null ? s.conditionalOutcome.burnTurns : 3,
            splitRoleEnabled = s.splitRole != null && s.splitRole.enabled,
            splitRoleLowestOutcome = s.splitRole != null ? s.splitRole.lowestOutcome : SplitRoleBranchOutcome.None,
            splitRoleHighestOutcome = s.splitRole != null ? s.splitRole.highestOutcome : SplitRoleBranchOutcome.None,
            splitRoleBurnTurns = s.splitRole != null ? s.splitRole.burnTurns : 3,
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

    private static SkillRuntime BuildRuntimeFromBuffDebuffSkill(SkillBuffDebuffSO b, CombatActor owner, DiceSlotRig diceRig, int baseSpan, int baseStart0)
    {
        if (b == null) return null;

        int span = Mathf.Clamp(baseSpan > 0 ? baseSpan : b.slotsRequired, 1, 3);
        var rt = new SkillRuntime
        {
            sourceAsset = b,
            useV2Targeting = true,
            targetRuleV2 = b.target,
            kind = SkillKind.Utility,
            group = DamageGroup.Effect,
            element = ElementType.Neutral,
            range = RangeType.Ranged,
            slotsRequired = span,
            focusCost = Mathf.Max(0, b.focusCost),
            focusGainOnCast = b.focusGainOnCast,
            dieMultiplier = 0,
            flatDamage = 0,
            conditionMet = false
        };

        rt.target = MapTargetRule(b.target, out rt.hitAllEnemies, out rt.hitAllAllies);
        if (rt.target == TargetRule.Self) rt.hitAllEnemies = false;
        if (rt.target == TargetRule.Enemy) rt.hitAllAllies = false;

        PopulateLocalDiceSnapshot(rt, owner, diceRig, baseStart0, span);

        return rt;
    }

    private static void PopulateLocalDiceSnapshot(SkillRuntime rt, CombatActor owner, DiceSlotRig diceRig, int start0, int span)
    {
        if (rt == null || diceRig == null || !diceRig.HasRolledThisTurn)
            return;

        start0 = Mathf.Clamp(start0, 0, 2);
        span = Mathf.Clamp(span, 1, 3);

        rt.localBaseValues = new List<int>(span);
        rt.localResolvedValues = new List<int>(span);
        rt.localNumericFlags = new List<bool>(span);
        rt.localCritFlags = new List<bool>(span);
        rt.localFailFlags = new List<bool>(span);
        rt.localFailPenaltyFlags = new List<bool>(span);

        for (int i = start0; i < start0 + span && i < 3; i++)
        {
            rt.localBaseValues.Add(diceRig.GetBaseValue(i));
            rt.localResolvedValues.Add(diceRig.GetResolvedDieValue(i, owner));
            rt.localNumericFlags.Add(diceRig.IsNumericFaceForConditions(i));
            bool isCrit = diceRig.IsCrit(i);
            bool isFail = diceRig.IsFail(i);
            bool appliesFailPenalty = diceRig.AppliesFailPenalty(i);
            rt.localCritFlags.Add(isCrit);
            rt.localFailFlags.Add(isFail);
            rt.localFailPenaltyFlags.Add(appliesFailPenalty);
            rt.localCritAny |= isCrit;
            rt.localFailAny |= isFail;
            rt.localFailPenaltyAny |= appliesFailPenalty;
        }
    }
}
