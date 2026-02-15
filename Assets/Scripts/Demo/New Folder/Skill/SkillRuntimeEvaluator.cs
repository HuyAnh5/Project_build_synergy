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

    private static List<int> GatherDiceForScope(
    SkillConditionScope scope,
    DiceSlotRig diceRig,
    int start0,
    int span,
    int allDiceDelta = 0)
    {
        var list = new List<int>(3);
        if (diceRig == null) return list;

        if (scope == SkillConditionScope.SlotBound)
        {
            for (int i = start0; i < start0 + span; i++)
            {
                if (i < 0 || i > 2) continue;
                if (!diceRig.IsSlotActive(i)) continue;
                list.Add(diceRig.GetEffectiveDieValue(i, allDiceDelta));
            }
            return list;
        }

        // Global
        for (int i = 0; i < 3; i++)
        {
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.GetEffectiveDieValue(i, allDiceDelta));
        }
        return list;
    }

}
