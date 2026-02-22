using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a SkillRuntime for the current placement and dice state.
/// This is what makes "condition" affect gameplay (not just UI/cost).
/// </summary>
public static class SkillRuntimeEvaluator
{
    // Back-compat overload (older call sites)
    public static SkillRuntime Evaluate(SkillSO skill, DiceSlotRig diceRig, int span, int start0)
    {
        return Evaluate(skill, diceRig, anchor0: start0, span: span, start0: start0);
    }


    /// <param name="skill">SkillSO base</param>
    /// <param name="diceRig">Dice rig (can be null)</param>
    /// <param name="anchor0">Anchor index (0..2)</param>
    /// <param name="span">Occupied slots count (1..3)</param>
    /// <param name="start0">Start slot (0..2)</param>
    public static SkillRuntime Evaluate(SkillSO skill, DiceSlotRig diceRig, int anchor0, int span, int start0)
    {
        if (skill == null) return null;

        var rt = SkillRuntime.FromSkill(skill);
        if (rt == null) return null;

        bool met = false;

        if (skill.hasCondition && skill.condition != null && diceRig != null)
        {
            var dice = GatherDiceForScope(skill.condition.scope, diceRig, start0, span);
            met = skill.condition.Evaluate(dice);
        }

        rt.conditionMet = met;

        if (met && skill.hasCondition && skill.whenConditionIsMet != null)
        {
            skill.whenConditionIsMet.ApplyTo(ref rt);
        }

        return rt;
    }

    // -------------------- NEW SYSTEM (SkillDamageSO) --------------------

    // Convenience overload (older call sites)
    public static SkillRuntime Evaluate(SkillDamageSO skill, DiceSlotRig diceRig, int span, int start0)
    {
        return Evaluate(skill, diceRig, anchor0: start0, span: span, start0: start0);
    }

    /// <summary>
    /// Evaluate a SkillDamageSO into a SkillRuntime for this turn.
    /// Condition + overrides apply ONLY for SkillDamageSO.
    /// </summary>
    public static SkillRuntime Evaluate(SkillDamageSO skill, DiceSlotRig diceRig, int anchor0, int span, int start0)
    {
        if (skill == null) return null;

        var rt = SkillRuntime.FromDamage(skill);
        if (rt == null) return null;

        bool met = false;

        if (skill.hasCondition && skill.condition != null && diceRig != null)
        {
            var dice = GatherDiceForScope(skill.condition.scope, diceRig, start0, span);
            met = SkillConditionEvaluator.Evaluate(skill.condition, dice);
        }

        rt.conditionMet = met;

        if (met && skill.hasCondition && skill.whenConditionIsMet != null)
        {
            ApplyDamageOverrides(ref rt, skill.whenConditionIsMet);
        }

        // Safety: Guard always Self and not AoE
        if (rt.kind == SkillKind.Guard || rt.coreAction == CoreAction.BasicGuard)
        {
            rt.kind = SkillKind.Guard;
            rt.targetRuleV2 = SkillTargetRule.Self;
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }

        return rt;
    }

    private static List<int> GatherDiceForScope(SkillConditionScope scope, DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<int>(3);

        if (diceRig == null) return list;

        if (scope == SkillConditionScope.SlotBound)
        {
            for (int i = start0; i < start0 + span; i++)
            {
                if (i < 0 || i > 2) continue;
                if (!diceRig.IsSlotActive(i)) continue;
                list.Add(diceRig.GetDieValue(i));
            }
            return list;
        }

        // Global
        for (int i = 0; i < 3; i++)
        {
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.GetDieValue(i));
        }
        return list;
    }

    private static void ApplyDamageOverrides(ref SkillRuntime rt, SkillDamageConditionalOverrides ov)
    {
        if (ov == null) return;

        // Slots
        if (ov.overrideSlotsRequired)
            rt.slotsRequired = Mathf.Clamp(ov.slotsRequired, 1, 3);

        // Identity
        if (ov.overrideIdentity)
        {
            rt.kind = ov.kind;
            rt.useV2Targeting = true;
            rt.targetRuleV2 = ov.target;

            rt.group = ov.group;
            rt.element = (ElementType)(int)ov.element;
            rt.range = ov.range;

            rt.hitAllEnemies = (ov.target == SkillTargetRule.AllEnemies || ov.target == SkillTargetRule.AllUnits);
            rt.hitAllAllies = (ov.target == SkillTargetRule.AllAllies || ov.target == SkillTargetRule.AllUnits);

            rt.target = (ov.target == SkillTargetRule.SingleEnemy || ov.target == SkillTargetRule.AllEnemies || ov.target == SkillTargetRule.AllUnits)
                ? TargetRule.Enemy
                : TargetRule.Self;
        }

        // Cost
        if (ov.overrideCost)
        {
            rt.focusCost = Mathf.Max(0, ov.focusCost);
            rt.focusGainOnCast = ov.focusGainOnCast;
        }

        // Damage
        if (ov.overrideDamage)
        {
            rt.dieMultiplier = ov.dieMultiplier;
            rt.flatDamage = ov.flatDamage;
        }

        // Sunder bonus
        if (ov.overrideSunderBonus)
        {
            rt.sunderBonusIfTargetHasGuard = ov.sunderBonusIfTargetHasGuard;
            rt.sunderGuardDamageMultiplier = ov.sunderGuardDamageMultiplier;
        }

        // Guard
        if (ov.overrideGuard)
        {
            rt.guardDieMultiplier = ov.guardDieMultiplier;
            rt.guardFlat = ov.guardFlat;
        }

        // Special combat
        if (ov.overrideSpecialCombat)
        {
            rt.bypassGuard = ov.bypassGuard;
            rt.clearsGuard = ov.clearsGuard;
            rt.canUseMarkMultiplier = ov.canUseMarkMultiplier;
        }

        // Burn spender
        if (ov.overrideBurnSpender)
        {
            rt.consumesBurn = ov.consumesBurn;
            rt.burnDamagePerStack = ov.burnDamagePerStack;
        }

        // Apply status
        if (ov.overrideApplyStatus)
        {
            rt.applyBurn = ov.applyBurn;
            rt.burnAddStacks = ov.burnAddStacks;
            rt.burnRefreshTurns = ov.burnRefreshTurns;

            rt.applyMark = ov.applyMark;

            rt.applyBleed = ov.applyBleed;
            rt.bleedTurns = ov.bleedTurns;

            rt.applyFreeze = ov.applyFreeze;
            rt.freezeChance = ov.freezeChance;
        }

        // VFX
        if (ov.overrideVfx)
            rt.projectilePrefab = ov.projectilePrefab;

        // safety
        if (rt.kind == SkillKind.Guard)
        {
            rt.targetRuleV2 = SkillTargetRule.Self;
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }
        if (rt.target == TargetRule.Self) rt.hitAllEnemies = false;
        if (rt.target == TargetRule.Enemy) rt.hitAllAllies = false;
    }
}
