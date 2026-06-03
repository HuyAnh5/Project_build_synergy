using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a SkillRuntime for the current placement and dice state.
/// This is what makes "condition" affect gameplay (not just UI/cost).
/// </summary>
public static partial class SkillRuntimeEvaluator
{
    private const int MaxCombatSlots = 3;

    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int span, int start0)
    {
        return Evaluate(skill, owner, diceRig, anchor0: start0, span: span, start0: start0, target: null);
    }

    /// <summary>
    /// Evaluate a SkillDamageSO into a SkillRuntime for this turn.
    /// Condition + overrides apply ONLY for SkillDamageSO.
    /// </summary>
    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int anchor0, int span, int start0)
    {
        return Evaluate(skill, owner, diceRig, anchor0, span, start0, target: null);
    }

    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int anchor0, int span, int start0, CombatActor target)
        => Evaluate(skill, owner, diceRig, anchor0, span, start0, target, -1);

    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int anchor0, int span, int start0, CombatActor target, int paymentMask)
        => Evaluate(skill, owner, diceRig, anchor0, span, start0, target, paymentMask, includeSyntheticRelayAdded: true);

    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int anchor0, int span, int start0, CombatActor target, int paymentMask, bool includeSyntheticRelayAdded)
    {
        if (skill == null) return null;

        var rt = SkillRuntime.FromDamage(skill);
        if (rt == null) return null;
        rt.localBaseValues = GatherDiceForScope(SkillConditionScope.SlotBound, diceRig, start0, span, paymentMask);
        rt.localOutputBaseValues = GatherOutputBaseValuesForScope(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded);
        rt.localNumericFlags = GatherNumericFlags(diceRig, start0, span, paymentMask);
        rt.localCritFlags = GatherCritFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded);
        rt.localFailFlags = GatherFailFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded);
        rt.localFailPenaltyFlags = GatherFailPenaltyFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded);
        GatherCritFailFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded, out rt.localCritAny, out rt.localFailAny, out rt.localFailPenaltyAny);

        bool met = false;

        if (skill.hasCondition && diceRig != null)
        {
            SkillConditionContext conditionContext = BuildConditionContext(skill.condition.scope, owner, diceRig, start0, span, rt.element, target, paymentMask, includeSyntheticRelayAdded);
            met = skill.conditionEditorMode == ConditionEditorMode.Builder
                ? EvaluateBuilderCondition(skill, owner, diceRig, conditionContext)
                : SkillConditionEvaluator.Evaluate(skill.condition, conditionContext);
        }

        rt.conditionMet = met;

        if (met)
            ApplyDamageOverrides(ref rt, skill.whenConditionIsMet);

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.NoName) && skill.hasCondition)
        {
            rt.focusCost = met ? 3 : 0;
            if (!met)
            {
                rt.dieMultiplier = 0f;
                rt.flatDamage = 0;
            }
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

        rt.localResolvedValues = GatherResolvedDiceForScope(diceRig, owner, start0, span, rt.element, paymentMask, includeSyntheticRelayAdded);
        ApplyOwnerCombatBonuses(rt, owner);

        return rt;
    }

    private static void ApplyOwnerCombatBonuses(SkillRuntime rt, CombatActor owner)
    {
        if (rt == null || owner == null || owner.status == null)
            return;

        if (rt.coreAction == CoreAction.BasicStrike && owner.status.emberWeaponTurns > 0)
            rt.ownerFlatDamageBonus = Mathf.Max(0, owner.status.emberWeaponBonusDamage);
    }

}
