using UnityEngine;

internal static class SkillPlanBoardStateUtility
{
    public static int ComputeClearGroupCountInRange(int[] cellAnchor, int start0, int span)
    {
        bool[] counted = new bool[3];
        int count = 0;

        for (int j = start0; j < start0 + span; j++)
        {
            int a = cellAnchor[j];
            if (a >= 0 && !counted[a])
            {
                counted[a] = true;
                count++;
            }
        }

        return count;
    }

    public static int ComputeRefundInRange(int[] cellAnchor, int[] anchorReservedCost, int start0, int span)
    {
        bool[] counted = new bool[3];
        int refund = 0;

        for (int j = start0; j < start0 + span; j++)
        {
            int a = cellAnchor[j];
            if (a >= 0 && !counted[a])
            {
                counted[a] = true;
                refund += anchorReservedCost[a];
            }
        }

        return refund;
    }

    public static void ClearGroupsInRange(
        ScriptableObject[] cellSkill,
        int[] cellAnchor,
        int[] anchorSpan,
        int[] anchorStart0,
        int[] anchorBaseStart0,
        int[] anchorReservedCost,
        SkillRuntime[] anchorRuntime,
        int start0,
        int span,
        CombatActor player)
    {
        bool[] cleared = new bool[3];
        for (int j = start0; j < start0 + span; j++)
        {
            int a = cellAnchor[j];
            if (a >= 0 && !cleared[a])
            {
                cleared[a] = true;
                ClearGroupAtAnchor(cellSkill, cellAnchor, anchorSpan, anchorStart0, anchorBaseStart0, anchorReservedCost, anchorRuntime, a, player);
            }
        }
    }

    public static void ClearGroupAtAnchor(
        ScriptableObject[] cellSkill,
        int[] cellAnchor,
        int[] anchorSpan,
        int[] anchorStart0,
        int[] anchorBaseStart0,
        int[] anchorReservedCost,
        SkillRuntime[] anchorRuntime,
        int anchor0,
        CombatActor player)
    {
        int refund = anchorReservedCost[anchor0];
        if (refund > 0 && player != null)
            player.GainFocus(refund);

        for (int i = 0; i < 3; i++)
        {
            if (cellAnchor[i] == anchor0)
            {
                cellSkill[i] = null;
                cellAnchor[i] = -1;
            }
        }

        anchorReservedCost[anchor0] = 0;
        anchorSpan[anchor0] = 0;
        anchorStart0[anchor0] = -1;
        anchorBaseStart0[anchor0] = -1;
        anchorRuntime[anchor0] = default;
    }

    public static void ConsumeGroupAtAnchorNoRefund(
        ScriptableObject[] cellSkill,
        int[] cellAnchor,
        int[] anchorSpan,
        int[] anchorStart0,
        int[] anchorBaseStart0,
        int[] anchorReservedCost,
        SkillRuntime[] anchorRuntime,
        int anchor0)
    {
        if (anchor0 < 0 || anchor0 > 2)
            return;

        for (int i = 0; i < 3; i++)
        {
            if (cellAnchor[i] == anchor0)
            {
                cellSkill[i] = null;
                cellAnchor[i] = -1;
            }
        }

        anchorReservedCost[anchor0] = 0;
        anchorSpan[anchor0] = 0;
        anchorStart0[anchor0] = -1;
        anchorBaseStart0[anchor0] = -1;
        anchorRuntime[anchor0] = default;
    }

    public static void PlaceGroup(
        ScriptableObject[] cellSkill,
        int[] cellAnchor,
        int[] anchorSpan,
        int[] anchorStart0,
        int[] anchorBaseStart0,
        int[] anchorReservedCost,
        SkillRuntime[] anchorRuntime,
        int start0,
        int anchor0,
        int span,
        ScriptableObject skill)
    {
        for (int j = start0; j < start0 + span; j++)
        {
            cellSkill[j] = skill;
            cellAnchor[j] = anchor0;
        }

        anchorSpan[anchor0] = span;
        anchorStart0[anchor0] = start0;
        anchorBaseStart0[anchor0] = start0;
        anchorReservedCost[anchor0] = 0;
        anchorRuntime[anchor0] = default;
    }

    public static int GetStartForAnchor(int[] anchorStart0, int anchor0)
    {
        if (anchor0 < 0 || anchor0 > 2)
            return 0;
        return Mathf.Clamp(anchorStart0[anchor0], 0, 2);
    }

    public static int GetDieSumForAnchor(int[] anchorSpan, int[] anchorStart0, int anchor0, DiceSlotRig diceRig)
    {
        if (diceRig == null || anchor0 < 0 || anchor0 > 2)
            return 0;

        int sp = Mathf.Clamp(anchorSpan[anchor0], 0, 3);
        if (sp <= 0)
            return 0;

        int start0 = Mathf.Clamp(anchorStart0[anchor0], 0, 2);
        if (sp == 1)
            return diceRig.GetDieValue(anchor0);
        if (sp == 2)
            return diceRig.GetDieValue(start0) + diceRig.GetDieValue(start0 + 1);
        return diceRig.GetDieValue(0) + diceRig.GetDieValue(1) + diceRig.GetDieValue(2);
    }

    public static int SanitizeStart0ForSpan(int start0, int span)
    {
        if (span <= 1) return Mathf.Clamp(start0, 0, 2);
        if (span == 2) return Mathf.Clamp(start0, 0, 1);
        return 0;
    }

    public static bool TryResizeGroupToSpan(
        ScriptableObject[] cellSkill,
        int[] cellAnchor,
        int[] anchorSpan,
        int[] anchorStart0,
        int anchor0,
        int desiredSpan,
        DiceSlotRig diceRig)
    {
        if (anchor0 < 0 || anchor0 > 2)
            return false;
        if (cellSkill[anchor0] == null || cellAnchor[anchor0] != anchor0)
            return false;

        desiredSpan = Mathf.Clamp(desiredSpan, 1, 3);
        int currentSpan = Mathf.Clamp(anchorSpan[anchor0], 1, 3);
        if (desiredSpan == currentSpan)
            return true;

        ScriptableObject skill = cellSkill[anchor0];
        int currentStart = SanitizeStart0ForSpan(anchorStart0[anchor0], currentSpan);

        int desiredStart0;
        if (desiredSpan == 3)
        {
            desiredStart0 = 0;
        }
        else if (desiredSpan == 2)
        {
            int optA = Mathf.Clamp(anchor0, 0, 1);
            int optB = Mathf.Clamp(anchor0 - 1, 0, 1);
            bool optAValid = anchor0 >= optA && anchor0 <= optA + 1;
            bool optBValid = anchor0 >= optB && anchor0 <= optB + 1;

            if (currentStart >= 0 && currentStart <= 1 && anchor0 >= currentStart && anchor0 <= currentStart + 1)
                desiredStart0 = currentStart;
            else if (optAValid && optBValid)
                desiredStart0 = Mathf.Abs(currentStart - optA) <= Mathf.Abs(currentStart - optB) ? optA : optB;
            else if (optAValid)
                desiredStart0 = optA;
            else
                desiredStart0 = optB;
        }
        else
        {
            desiredStart0 = anchor0;
        }

        desiredStart0 = SanitizeStart0ForSpan(desiredStart0, desiredSpan);

        for (int i = desiredStart0; i < desiredStart0 + desiredSpan; i++)
        {
            if (diceRig != null && !diceRig.IsSlotActive(i))
                return false;
            int occ = cellAnchor[i];
            if (occ != -1 && occ != anchor0)
                return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (cellAnchor[i] != anchor0)
                continue;
            bool keep = i >= desiredStart0 && i < desiredStart0 + desiredSpan;
            if (!keep)
            {
                cellSkill[i] = null;
                cellAnchor[i] = -1;
            }
        }

        for (int i = desiredStart0; i < desiredStart0 + desiredSpan; i++)
        {
            cellSkill[i] = skill;
            cellAnchor[i] = anchor0;
        }

        anchorSpan[anchor0] = desiredSpan;
        anchorStart0[anchor0] = desiredStart0;
        return true;
    }

    public static bool RecalculateRuntimesAndRebalance(
        ScriptableObject[] cellSkill,
        int[] cellAnchor,
        int[] anchorSpan,
        int[] anchorStart0,
        int[] anchorBaseStart0,
        int[] anchorReservedCost,
        SkillRuntime[] anchorRuntime,
        CombatActor player,
        DiceSlotRig diceRig)
    {
        for (int a = 0; a < 3; a++)
        {
            bool isAnchor = cellSkill[a] != null && cellAnchor[a] == a;
            if (!isAnchor)
            {
                anchorRuntime[a] = default;
                continue;
            }

            ScriptableObject skill = cellSkill[a];
            int baseSpan = Mathf.Clamp(SkillPlanRuntimeUtility.GetSlotsRequired(skill), 1, 3);
            int baseStart0 = SanitizeStart0ForSpan(anchorBaseStart0[a], baseSpan);

            anchorRuntime[a] = SkillPlanRuntimeUtility.EvaluateRuntimeForSkillAsset(skill, diceRig, a, baseSpan, baseStart0);
            if (anchorRuntime[a] == null)
            {
                anchorRuntime[a] = default;
                continue;
            }

            int desiredSpan = Mathf.Clamp(anchorRuntime[a].slotsRequired, 1, 3);
            int currentSpan = Mathf.Clamp(anchorSpan[a], 1, 3);
            if (desiredSpan != currentSpan)
            {
                bool ok = TryResizeGroupToSpan(cellSkill, cellAnchor, anchorSpan, anchorStart0, a, desiredSpan, diceRig);
                if (!ok)
                {
                    if (cellSkill[a] is SkillDamageSO && desiredSpan > currentSpan)
                        return false;

                    anchorRuntime[a].slotsRequired = currentSpan;
                }
            }
        }

        for (int a = 0; a < 3; a++)
        {
            int desired = 0;
            if (cellSkill[a] != null && cellAnchor[a] == a)
                desired = Mathf.Max(0, anchorRuntime[a].focusCost);

            int current = anchorReservedCost[a];
            if (desired == current)
                continue;

            if (desired > current)
            {
                int need = desired - current;
                if (player != null && !player.TrySpendFocus(need))
                    return false;
                anchorReservedCost[a] = desired;
            }
            else
            {
                int refund = current - desired;
                if (refund > 0 && player != null)
                    player.GainFocus(refund);
                anchorReservedCost[a] = desired;
            }
        }

        return true;
    }
}
