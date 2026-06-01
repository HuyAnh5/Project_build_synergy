using System.Collections.Generic;
using UnityEngine;

// Reads dice slot values and dice flags without changing the dice rig.
public static partial class SkillRuntimeEvaluator
{
    // Collects unique base values across every equipped dice face.
    private static List<int> GatherOwnedFaceValues(DiceSlotRig diceRig)
    {
        var values = new List<int>();
        if (diceRig == null || diceRig.slots == null)
            return values;

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null || die.faces == null)
                continue;

            for (int f = 0; f < die.faces.Length; f++)
            {
                int value = die.faces[f].value;
                if (!values.Contains(value))
                    values.Add(value);
            }
        }

        return values;
    }

    // Finds the first active combat lane.
    private static int FindLeftmostActiveSlot(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return -1;

        for (int i = 0; i < diceRig.slots.Length && i < MaxCombatSlots; i++)
        {
            if (diceRig.IsSlotActive(i))
                return i;
        }

        return -1;
    }

    // Finds the last active combat lane.
    private static int FindRightmostActiveSlot(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return -1;

        for (int i = Mathf.Min(diceRig.slots.Length, MaxCombatSlots) - 1; i >= 0; i--)
        {
            if (diceRig.IsSlotActive(i))
                return i;
        }

        return -1;
    }

    // Gathers base values for either the skill-bound group or all active lanes.
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
                list.Add(diceRig.IsNumericFaceForConditions(i) ? diceRig.GetDieValue(i) : 0);
            }
            return list;
        }

        for (int i = 0; i < 3; i++)
        {
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.IsNumericFaceForConditions(i) ? diceRig.GetDieValue(i) : 0);
        }
        return list;
    }

    // Gathers whether each local dice face is numeric for condition checks.
    private static List<bool> GatherNumericFlags(DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.IsNumericFaceForConditions(i));
        }

        return list;
    }

    // Gathers resolved die values after element/enchant rewrite rules.
    private static List<int> GatherResolvedDiceForScope(DiceSlotRig diceRig, CombatActor owner, int start0, int span, ElementType skillElement)
    {
        var list = new List<int>(3);

        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.GetResolvedDieValue(i, owner, skillElement));
        }

        return list;
    }

    private static List<int> GatherOutputBaseValuesForScope(DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<int>(3);

        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.GetOutputBaseValue(i));
        }

        return list;
    }

    // Gathers Crit flags for local dice.
    private static List<bool> GatherCritFlags(DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.IsCrit(i));
        }

        return list;
    }

    // Gathers Fail flags for local dice.
    private static List<bool> GatherFailFlags(DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.IsFail(i));
        }

        return list;
    }

    // Gathers whether each Fail should apply the output penalty.
    private static List<bool> GatherFailPenaltyFlags(DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.AppliesFailPenalty(i));
        }

        return list;
    }

    // Gathers aggregate Crit/Fail booleans for the runtime.
    private static void GatherCritFailFlags(DiceSlotRig diceRig, int start0, int span, out bool critAny, out bool failAny, out bool failPenaltyAny)
    {
        critAny = false;
        failAny = false;
        failPenaltyAny = false;

        if (diceRig == null)
            return;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            critAny |= diceRig.IsCrit(i);
            failAny |= diceRig.IsFail(i);
            failPenaltyAny |= diceRig.AppliesFailPenalty(i);
        }
    }

    // Treats missing numeric flags as numeric to preserve legacy condition behavior.
    private static bool IsNumeric(IReadOnlyList<bool> numericFlags, int index)
    {
        return numericFlags == null || (index >= 0 && index < numericFlags.Count && numericFlags[index]);
    }
}
