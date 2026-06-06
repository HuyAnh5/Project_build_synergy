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
        => GatherDiceForScope(scope, diceRig, start0, span, -1);

    private static List<int> GatherDiceForScope(SkillConditionScope scope, DiceSlotRig diceRig, int start0, int span, int paymentMask)
    {
        var list = new List<int>(3);

        if (diceRig == null) return list;

        if (scope == SkillConditionScope.SlotBound)
        {
            for (int i = start0; i < start0 + span; i++)
            {
                if (i < 0 || i > 2) continue;
                if (!diceRig.IsSlotActive(i)) continue;
                if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
                list.Add(diceRig.IsNumericFaceForConditions(i) ? diceRig.GetOutputBaseValue(i) : 0);
            }
            return list;
        }

        for (int i = 0; i < 3; i++)
        {
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.IsNumericFaceForConditions(i) ? diceRig.GetOutputBaseValue(i) : 0);
        }
        return list;
    }

    // Gathers whether each local dice face is numeric for condition checks.
    private static List<bool> GatherNumericFlags(DiceSlotRig diceRig, int start0, int span)
        => GatherNumericFlags(diceRig, start0, span, -1);

    private static List<bool> GatherNumericFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
            list.Add(diceRig.IsNumericFaceForConditions(i));
        }

        return list;
    }

    // Gathers resolved die values after element/enchant rewrite rules.
    private static List<int> GatherResolvedDiceForScope(DiceSlotRig diceRig, CombatActor owner, int start0, int span, ElementType skillElement)
        => GatherResolvedDiceForScope(diceRig, owner, start0, span, skillElement, -1);

    private static List<int> GatherResolvedDiceForScope(DiceSlotRig diceRig, CombatActor owner, int start0, int span, ElementType skillElement, int paymentMask)
        => GatherResolvedDiceForScope(diceRig, owner, start0, span, skillElement, paymentMask, includeSyntheticRelayAdded: true);

    private static List<int> GatherResolvedDiceForScope(DiceSlotRig diceRig, CombatActor owner, int start0, int span, ElementType skillElement, int paymentMask, bool includeSyntheticRelayAdded)
    {
        var list = new List<int>(3);

        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
            int resolvedValue = paymentMask >= 0
                ? DiceCombatEnchantRuntimeUtility.GetCommittedPreviewResolvedBreakdown(diceRig, owner, i, skillElement, paymentMask, includeSyntheticRelayAdded).resolvedValue
                : diceRig.GetResolvedDieValue(i, owner, skillElement);
            list.Add(resolvedValue);
        }

        return list;
    }

    private static List<int> GatherOutputBaseValuesForScope(DiceSlotRig diceRig, int start0, int span)
        => GatherOutputBaseValuesForScope(diceRig, start0, span, -1);

    private static List<int> GatherOutputBaseValuesForScope(DiceSlotRig diceRig, int start0, int span, int paymentMask)
        => GatherOutputBaseValuesForScope(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded: true);

    private static List<int> GatherOutputBaseValuesForScope(DiceSlotRig diceRig, int start0, int span, int paymentMask, bool includeSyntheticRelayAdded)
    {
        var list = new List<int>(3);

        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
            list.Add(paymentMask >= 0
                ? DiceCombatEnchantRuntimeUtility.GetCommittedPreviewResolvedBreakdown(diceRig, null, i, ElementType.Neutral, paymentMask, includeSyntheticRelayAdded).outputBaseValue
                : diceRig.GetOutputBaseValue(i));
        }

        return list;
    }

    // Gathers Crit flags for local dice.
    private static List<bool> GatherCritFlags(DiceSlotRig diceRig, int start0, int span)
        => GatherCritFlags(diceRig, start0, span, -1);

    private static List<bool> GatherCritFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask)
        => GatherCritFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded: true);

    private static List<bool> GatherCritFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask, bool includeSyntheticRelayAdded)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
            list.Add(paymentMask >= 0
                ? DiceCombatEnchantRuntimeUtility.GetCommittedPreviewResolvedBreakdown(diceRig, null, i, ElementType.Neutral, paymentMask, includeSyntheticRelayAdded).isCrit
                : diceRig.IsCrit(i));
        }

        return list;
    }

    // Gathers Fail flags for local dice.
    private static List<bool> GatherFailFlags(DiceSlotRig diceRig, int start0, int span)
        => GatherFailFlags(diceRig, start0, span, -1);

    private static List<bool> GatherFailFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask)
        => GatherFailFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded: true);

    private static List<bool> GatherFailFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask, bool includeSyntheticRelayAdded)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
            list.Add(paymentMask >= 0
                ? DiceCombatEnchantRuntimeUtility.GetCommittedPreviewResolvedBreakdown(diceRig, null, i, ElementType.Neutral, paymentMask, includeSyntheticRelayAdded).isFail
                : diceRig.IsFail(i));
        }

        return list;
    }

    // Gathers whether each Fail should apply the output penalty.
    private static List<bool> GatherFailPenaltyFlags(DiceSlotRig diceRig, int start0, int span)
        => GatherFailPenaltyFlags(diceRig, start0, span, -1);

    private static List<bool> GatherFailPenaltyFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask)
        => GatherFailPenaltyFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded: true);

    private static List<bool> GatherFailPenaltyFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask, bool includeSyntheticRelayAdded)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
            list.Add(paymentMask >= 0
                ? DiceCombatEnchantRuntimeUtility.GetCommittedPreviewResolvedBreakdown(diceRig, null, i, ElementType.Neutral, paymentMask, includeSyntheticRelayAdded).appliesFailPenalty
                : diceRig.AppliesFailPenalty(i));
        }

        return list;
    }

    // Gathers aggregate Crit/Fail booleans for the runtime.
    private static void GatherCritFailFlags(DiceSlotRig diceRig, int start0, int span, out bool critAny, out bool failAny, out bool failPenaltyAny)
        => GatherCritFailFlags(diceRig, start0, span, -1, out critAny, out failAny, out failPenaltyAny);

    private static void GatherCritFailFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask, out bool critAny, out bool failAny, out bool failPenaltyAny)
        => GatherCritFailFlags(diceRig, start0, span, paymentMask, includeSyntheticRelayAdded: true, out critAny, out failAny, out failPenaltyAny);

    private static void GatherCritFailFlags(DiceSlotRig diceRig, int start0, int span, int paymentMask, bool includeSyntheticRelayAdded, out bool critAny, out bool failAny, out bool failPenaltyAny)
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
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0) continue;
            if (paymentMask >= 0)
            {
                DiceSlotRig.ResolvedDieBreakdown breakdown =
                    DiceCombatEnchantRuntimeUtility.GetCommittedPreviewResolvedBreakdown(diceRig, null, i, ElementType.Neutral, paymentMask, includeSyntheticRelayAdded);
                critAny |= breakdown.isCrit;
                failAny |= breakdown.isFail;
                failPenaltyAny |= breakdown.appliesFailPenalty;
            }
            else
            {
                critAny |= diceRig.IsCrit(i);
                failAny |= diceRig.IsFail(i);
                failPenaltyAny |= diceRig.AppliesFailPenalty(i);
            }
        }
    }

    // Treats missing numeric flags as numeric to preserve legacy condition behavior.
    private static bool IsNumeric(IReadOnlyList<bool> numericFlags, int index)
    {
        return numericFlags == null || (index >= 0 && index < numericFlags.Count && numericFlags[index]);
    }
}
