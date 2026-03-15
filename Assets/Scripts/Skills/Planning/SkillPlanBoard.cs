using System;
using UnityEngine;

/// <summary>
/// Owns the 3-slot plan grid, multi-slot grouping, reserved focus costs, and per-anchor runtime evaluation.
/// TurnManager delegates planning logic to this class to stay smaller.
/// 
/// Supports active skills:
/// - SkillDamageSO
/// - SkillBuffDebuffSO
/// Passive skills are not stored here.
/// </summary>
public class SkillPlanBoard
{
    // 3 cells: 0..2
    private readonly ScriptableObject[] _cellSkill = new ScriptableObject[3];
    private readonly int[] _cellAnchor = new int[3];          // -1 empty, else anchor index
    private readonly int[] _anchorSpan = new int[3];          // CURRENT occupied span (1..3)
    private readonly int[] _anchorStart0 = new int[3];        // CURRENT start slot for that anchor
    private readonly int[] _anchorBaseStart0 = new int[3];    // BASE start slot (used for condition evaluation)
    private readonly int[] _anchorReservedCost = new int[3];  // valid only at anchor: reserved focus cost
    private readonly SkillRuntime[] _anchorRuntime = new SkillRuntime[3];

    public struct Snapshot
    {
        public ScriptableObject[] cellSkill;
        public int[] cellAnchor;
        public int[] anchorSpan;
        public int[] anchorStart0;
        public int[] anchorBaseStart0;
        public int[] anchorReservedCost;
        public SkillRuntime[] anchorRuntime;
        public int playerFocus;
    }

    public void Reset()
    {
        for (int i = 0; i < 3; i++)
        {
            _cellSkill[i] = null;
            _cellAnchor[i] = -1;
            _anchorSpan[i] = 0;
            _anchorStart0[i] = -1;
            _anchorBaseStart0[i] = -1;
            _anchorReservedCost[i] = 0;
            _anchorRuntime[i] = default;
        }
    }

    public Snapshot Capture(CombatActor player)
    {
        return new Snapshot
        {
            cellSkill = (ScriptableObject[])_cellSkill.Clone(),
            cellAnchor = (int[])_cellAnchor.Clone(),
            anchorSpan = (int[])_anchorSpan.Clone(),
            anchorStart0 = (int[])_anchorStart0.Clone(),
            anchorBaseStart0 = (int[])_anchorBaseStart0.Clone(),
            anchorReservedCost = (int[])_anchorReservedCost.Clone(),
            anchorRuntime = (SkillRuntime[])_anchorRuntime.Clone(),
            playerFocus = (player != null) ? player.focus : 0
        };
    }

    public void Restore(Snapshot s, CombatActor player)
    {
        Array.Copy(s.cellSkill, _cellSkill, 3);
        Array.Copy(s.cellAnchor, _cellAnchor, 3);
        Array.Copy(s.anchorSpan, _anchorSpan, 3);
        if (s.anchorStart0 != null) Array.Copy(s.anchorStart0, _anchorStart0, 3);
        if (s.anchorBaseStart0 != null) Array.Copy(s.anchorBaseStart0, _anchorBaseStart0, 3);
        Array.Copy(s.anchorReservedCost, _anchorReservedCost, 3);
        Array.Copy(s.anchorRuntime, _anchorRuntime, 3);

        if (player != null) player.focus = s.playerFocus;
    }

    public bool TryApplyLanePermutation(int[] permutation, CombatActor player, DiceSlotRig diceRig)
    {
        if (permutation == null || permutation.Length < 3)
            return false;

        bool[] seen = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            int v = permutation[i];
            if (v < 0 || v > 2 || seen[v])
                return false;
            seen[v] = true;
        }

        Snapshot snap = Capture(player);

        ScriptableObject[] oldCellSkill = (ScriptableObject[])_cellSkill.Clone();
        int[] oldCellAnchor = (int[])_cellAnchor.Clone();
        int[] oldAnchorSpan = (int[])_anchorSpan.Clone();
        int[] oldAnchorReservedCost = (int[])_anchorReservedCost.Clone();

        Reset();
        if (player != null)
            player.focus = snap.playerFocus;

        for (int oldAnchor = 0; oldAnchor < 3; oldAnchor++)
        {
            if (oldAnchorSpan[oldAnchor] <= 0)
                continue;
            if (oldCellSkill[oldAnchor] == null || oldCellAnchor[oldAnchor] != oldAnchor)
                continue;

            bool[] oldOccupied = new bool[3];
            for (int oldCell = 0; oldCell < 3; oldCell++)
            {
                if (oldCellAnchor[oldCell] == oldAnchor && oldCellSkill[oldCell] != null)
                    oldOccupied[oldCell] = true;
            }

            int[] newOccupied = new int[3];
            int newCount = 0;
            for (int newCell = 0; newCell < 3; newCell++)
            {
                int oldCell = permutation[newCell];
                if (oldOccupied[oldCell])
                    newOccupied[newCount++] = newCell;
            }

            if (newCount != oldAnchorSpan[oldAnchor])
            {
                Restore(snap, player);
                return false;
            }

            for (int i = 1; i < newCount; i++)
            {
                if (newOccupied[i] != newOccupied[i - 1] + 1)
                {
                    Restore(snap, player);
                    return false;
                }
            }

            int newStart0 = newOccupied[0];
            int newAnchor0;
            switch (newCount)
            {
                case 1:
                    newAnchor0 = newStart0;
                    break;
                case 2:
                    if (newStart0 == 0 && newOccupied[1] == 1) newAnchor0 = 0;
                    else if (newStart0 == 1 && newOccupied[1] == 2) newAnchor0 = 1;
                    else
                    {
                        Restore(snap, player);
                        return false;
                    }
                    break;
                case 3:
                    if (newStart0 != 0 || newOccupied[1] != 1 || newOccupied[2] != 2)
                    {
                        Restore(snap, player);
                        return false;
                    }
                    newAnchor0 = 1;
                    break;
                default:
                    Restore(snap, player);
                    return false;
            }

            ScriptableObject skill = oldCellSkill[oldAnchor];
            PlaceGroup(newStart0, newAnchor0, newCount, skill);
            _anchorReservedCost[newAnchor0] = oldAnchorReservedCost[oldAnchor];
            _anchorRuntime[newAnchor0] = snap.anchorRuntime[oldAnchor];
        }

        if (!RecalculateRuntimesAndRebalance(player, diceRig))
        {
            Restore(snap, player);
            return false;
        }

        return true;
    }

    // ---------------------------
    // Queries
    // ---------------------------

    public bool IsSkillEquipped(ScriptableObject skill)
    {
        if (skill == null) return false;
        for (int i = 0; i < 3; i++)
            if (_cellSkill[i] == skill) return true;
        return false;
    }

    public bool IsSkillEquipped(SkillDamageSO skill) => IsSkillEquipped((ScriptableObject)skill);
    public bool IsSkillEquipped(SkillBuffDebuffSO skill) => IsSkillEquipped((ScriptableObject)skill);

    public ScriptableObject GetCellSkillAsset(int i0) => (i0 >= 0 && i0 < 3) ? _cellSkill[i0] : null;

    public int GetCellAnchor(int i0) => (i0 >= 0 && i0 < 3) ? _cellAnchor[i0] : -1;
    public int GetAnchorSpan(int anchor0) => (anchor0 >= 0 && anchor0 < 3) ? _anchorSpan[anchor0] : 0;
    public SkillRuntime GetAnchorRuntime(int anchor0) => (anchor0 >= 0 && anchor0 < 3) ? _anchorRuntime[anchor0] : default;
    public int GetAnchorReservedCost(int anchor0) => (anchor0 >= 0 && anchor0 < 3) ? _anchorReservedCost[anchor0] : 0;

    public bool IsAnchorSlot(int i0) => (i0 >= 0 && i0 < 3) && _cellSkill[i0] != null && _cellAnchor[i0] == i0;

    // ---------------------------
    // Placement resolution
    // ---------------------------

    public bool ResolvePlacementForDrop(int drop0, int span, out int start0, out int anchor0)
    {
        start0 = -1;
        anchor0 = -1;

        span = Mathf.Clamp(span, 1, 3);

        if (span == 1)
        {
            start0 = drop0;
            anchor0 = drop0;
            return true;
        }

        if (span == 3)
        {
            start0 = 0;
            anchor0 = 1; // center slot
            return true;
        }

        // span == 2 (must be 0-1 or 1-2)
        if (drop0 == 0) { start0 = 0; anchor0 = 0; return true; }
        if (drop0 == 2) { start0 = 1; anchor0 = 1; return true; }

        // drop0 == 1: choose range that clears fewer groups
        int c01 = ComputeClearGroupCountInRange(0, 2);
        int c12 = ComputeClearGroupCountInRange(1, 2);
        if (c01 <= c12) { start0 = 0; anchor0 = 0; }
        else { start0 = 1; anchor0 = 1; }
        return true;
    }

    public bool TryFindEmptyPlacement(int span, Func<int, bool> isSlotActive0, out int start0, out int anchor0)
    {
        start0 = -1;
        anchor0 = -1;

        span = Mathf.Clamp(span, 1, 3);
        bool A(int i) => isSlotActive0 == null || isSlotActive0(i);

        if (span == 1)
        {
            for (int i = 0; i < 3; i++)
                if (A(i) && _cellSkill[i] == null) { start0 = i; anchor0 = i; return true; }
            return false;
        }

        if (span == 2)
        {
            if (A(0) && A(1) && _cellSkill[0] == null && _cellSkill[1] == null)
            { start0 = 0; anchor0 = 0; return true; }

            if (A(1) && A(2) && _cellSkill[1] == null && _cellSkill[2] == null)
            { start0 = 1; anchor0 = 1; return true; }

            return false;
        }

        // span == 3
        if (A(0) && A(1) && A(2) &&
            _cellSkill[0] == null && _cellSkill[1] == null && _cellSkill[2] == null)
        {
            start0 = 0; anchor0 = 1; return true;
        }

        return false;
    }

    private int ComputeClearGroupCountInRange(int start0, int span)
        => SkillPlanBoardStateUtility.ComputeClearGroupCountInRange(_cellAnchor, start0, span);

    public int ComputeRefundInRange(int start0, int span)
        => SkillPlanBoardStateUtility.ComputeRefundInRange(_cellAnchor, _anchorReservedCost, start0, span);

    // ---------------------------
    // Clear / Place
    // ---------------------------

    public void ClearGroupsInRange(int start0, int span, CombatActor player)
        => SkillPlanBoardStateUtility.ClearGroupsInRange(_cellSkill, _cellAnchor, _anchorSpan, _anchorStart0, _anchorBaseStart0, _anchorReservedCost, _anchorRuntime, start0, span, player);

    public void ClearGroupAtSlot0(int anySlot0, CombatActor player)
    {
        int a = _cellAnchor[anySlot0];
        if (a < 0) return;
        ClearGroupAtAnchor(a, player);
    }

    public void ClearGroupAtAnchor(int anchor0, CombatActor player)
        => SkillPlanBoardStateUtility.ClearGroupAtAnchor(_cellSkill, _cellAnchor, _anchorSpan, _anchorStart0, _anchorBaseStart0, _anchorReservedCost, _anchorRuntime, anchor0, player);

    /// <summary>
    /// Remove a group after casting. NO refund.
    /// </summary>
    public void ConsumeGroupAtAnchor_NoRefund(int anchor0)
        => SkillPlanBoardStateUtility.ConsumeGroupAtAnchorNoRefund(_cellSkill, _cellAnchor, _anchorSpan, _anchorStart0, _anchorBaseStart0, _anchorReservedCost, _anchorRuntime, anchor0);

    /// <summary>
    /// Place a group and let RecalculateRuntimesAndRebalance() reserve the cost.
    /// </summary>
    public void PlaceGroup(int start0, int anchor0, int span, ScriptableObject skill)
        => SkillPlanBoardStateUtility.PlaceGroup(_cellSkill, _cellAnchor, _anchorSpan, _anchorStart0, _anchorBaseStart0, _anchorReservedCost, _anchorRuntime, start0, anchor0, span, skill);

    // ---------------------------
    // Dice sum / runtime eval
    // ---------------------------

    public int GetStartForAnchor(int anchor0)
        => SkillPlanBoardStateUtility.GetStartForAnchor(_anchorStart0, anchor0);

    public int GetDieSumForAnchor(int anchor0, DiceSlotRig diceRig)
        => SkillPlanBoardStateUtility.GetDieSumForAnchor(_anchorSpan, _anchorStart0, anchor0, diceRig);

    private int SanitizeStart0ForSpan(int start0, int span)
        => SkillPlanBoardStateUtility.SanitizeStart0ForSpan(start0, span);

    /// <summary>
    /// Resize the CURRENT occupied group for an anchor to a new span (1..3) without changing anchor index.
    /// - Shrink is always allowed.
    /// - Expand only succeeds if the newly needed cells are empty (or already belong to the same anchor) and active.
    /// </summary>
    private bool TryResizeGroupToSpan(int anchor0, int desiredSpan, DiceSlotRig diceRig)
        => SkillPlanBoardStateUtility.TryResizeGroupToSpan(_cellSkill, _cellAnchor, _anchorSpan, _anchorStart0, anchor0, desiredSpan, diceRig);

    /// <summary>
    /// Re-evaluate conditional runtimes for all anchors, then rebalance reserved focus costs.
    /// Returns false if not enough focus to satisfy new costs (caller should rollback).
    /// </summary>
    public bool RecalculateRuntimesAndRebalance(CombatActor player, DiceSlotRig diceRig)
        => SkillPlanBoardStateUtility.RecalculateRuntimesAndRebalance(_cellSkill, _cellAnchor, _anchorSpan, _anchorStart0, _anchorBaseStart0, _anchorReservedCost, _anchorRuntime, player, diceRig);
}
