using System;
using UnityEngine;

internal static class TurnManagerPlanningUtility
{
    public static bool TryAssignSkillToSlot(
        bool isPlanning,
        CombatActor player,
        DiceSlotRig diceRig,
        SkillPlanBoard board,
        int slotIndex1Based,
        ScriptableObject activeSkill,
        Func<int, bool> isSlotAssignable0,
        Func<int, int, bool> areSlotsActiveInRange,
        Action refreshPreviews,
        Action updateIconsDim,
        bool clearExistingGroups)
    {
        if (!isPlanning || player == null || activeSkill == null || board == null)
            return false;
        if (activeSkill is SkillPassiveSO)
            return false;

        int drop0 = slotIndex1Based - 1;
        if (drop0 < 0 || drop0 > 2)
            return false;

        int span = GetSkillSpan(activeSkill);
        if (span <= 0)
            return false;

        if (!board.ResolvePlacementForDrop(drop0, span, out int start0, out int anchor0))
            return false;
        if (isSlotAssignable0 != null)
        {
            for (int i = start0; i < start0 + span; i++)
                if (!isSlotAssignable0(i))
                    return false;
        }
        if (areSlotsActiveInRange != null && !areSlotsActiveInRange(start0, span))
            return false;
        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = board.Capture(player);
        if (clearExistingGroups)
            board.ClearGroupsInRange(start0, span, player);
        board.PlaceGroup(start0, anchor0, span, activeSkill);

        if (!board.RecalculateRuntimesAndRebalance(player, diceRig))
        {
            board.Restore(snap, player);
            refreshPreviews?.Invoke();
            updateIconsDim?.Invoke();
            return false;
        }

        refreshPreviews?.Invoke();
        updateIconsDim?.Invoke();
        return true;
    }

    public static bool TryAutoAssignFromClick(
        bool isPlanning,
        CombatActor player,
        DiceSlotRig diceRig,
        SkillPlanBoard board,
        ScriptableObject activeSkill,
        Func<int, bool> isSlotActive0,
        Func<int, bool> isSlotAssignable0,
        Func<int, int, bool> areSlotsActiveInRange,
        Action refreshPreviews,
        Action updateIconsDim)
    {
        if (!isPlanning || player == null || activeSkill == null || board == null)
            return false;
        if (activeSkill is SkillPassiveSO)
            return false;

        int span = GetSkillSpan(activeSkill);
        if (span <= 0)
            return false;

        if (!board.TryFindEmptyPlacement(span, isSlotActive0, out int start0, out int anchor0))
            return false;
        if (isSlotAssignable0 != null)
        {
            for (int i = start0; i < start0 + span; i++)
                if (!isSlotAssignable0(i))
                    return false;
        }
        if (areSlotsActiveInRange != null && !areSlotsActiveInRange(start0, span))
            return false;
        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = board.Capture(player);
        board.PlaceGroup(start0, anchor0, span, activeSkill);

        if (!board.RecalculateRuntimesAndRebalance(player, diceRig))
        {
            board.Restore(snap, player);
            refreshPreviews?.Invoke();
            updateIconsDim?.Invoke();
            return false;
        }

        refreshPreviews?.Invoke();
        updateIconsDim?.Invoke();
        return true;
    }

    public static void ClearSlot(
        bool isPlanning,
        CombatActor player,
        DiceSlotRig diceRig,
        SkillPlanBoard board,
        int slotIndex1Based,
        Action refreshPreviews,
        Action updateIconsDim)
    {
        if (!isPlanning || board == null)
            return;

        int i0 = slotIndex1Based - 1;
        if (i0 < 0 || i0 > 2)
            return;

        board.ClearGroupAtSlot0(i0, player);
        board.RecalculateRuntimesAndRebalance(player, diceRig);
        refreshPreviews?.Invoke();
        updateIconsDim?.Invoke();
    }

    private static int GetSkillSpan(ScriptableObject activeSkill)
    {
        switch (activeSkill)
        {
            case SkillDamageSO dmg:
                return Mathf.Clamp(dmg.slotsRequired, 1, 3);
            case SkillBuffDebuffSO buffDebuff:
                return Mathf.Clamp(buffDebuff.slotsRequired, 1, 3);
            default:
                return 0;
        }
    }
}
