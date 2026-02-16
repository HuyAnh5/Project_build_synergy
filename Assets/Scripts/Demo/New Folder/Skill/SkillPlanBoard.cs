using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the 3-slot plan grid, multi-slot grouping, reserved focus costs, and per-anchor runtime evaluation.
///
/// Batch-2 goal:
/// - Slot storage is no longer SkillSO-only.
/// - Cells store an "active skill" as ScriptableObject (SkillSO legacy OR SkillDamageSO OR SkillBuffDebuffSO).
/// - Passive skills are forbidden here.
/// - Keep legacy API so existing UI / prefabs / TurnManager (batch 3) won't break.
///
/// IMPORTANT:
/// - This board is a pure C# class (NOT a MonoBehaviour). Do NOT FindObjectOfType on it.
/// </summary>
public class SkillPlanBoard
{
    // 3 cells: 0..2
    private readonly ScriptableObject[] _cellSkill = new ScriptableObject[3];
    private readonly int[] _cellAnchor = new int[3];          // -1 empty, else anchor index
    private readonly int[] _anchorSpan = new int[3];          // CURRENT occupied span (1..3)
    private readonly int[] _anchorStart0 = new int[3];        // CURRENT start slot for that anchor
    private readonly int[] _anchorBaseStart0 = new int[3];    // BASE start slot (stable condition scope)
    private readonly int[] _anchorReservedCost = new int[3];  // reserved focus cost (anchor-only)
    private readonly SkillRuntime[] _anchorRuntime = new SkillRuntime[3];

    public struct Snapshot
    {
        // Legacy snapshot (kept so older code that reads this field won't break)
        public SkillSO[] cellSkill;

        // New snapshot (true source of truth)
        public ScriptableObject[] cellSkillObj;

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
            _anchorRuntime[i] = null;
        }
    }

    public Snapshot Capture(CombatActor player)
    {
        // Build legacy-only view (derived)
        SkillSO[] legacy = new SkillSO[3];
        for (int i = 0; i < 3; i++) legacy[i] = _cellSkill[i] as SkillSO;

        return new Snapshot
        {
            cellSkill = legacy,
            cellSkillObj = (ScriptableObject[])_cellSkill.Clone(),
            cellAnchor = (int[])_cellAnchor.Clone(),
            anchorSpan = (int[])_anchorSpan.Clone(),
            anchorStart0 = (int[])_anchorStart0.Clone(),
            anchorBaseStart0 = (int[])_anchorBaseStart0.Clone(),
            anchorReservedCost = (int[])_anchorReservedCost.Clone(),
            anchorRuntime = (SkillRuntime[])_anchorRuntime.Clone(),
            playerFocus = (player != null) ? player.focus : 0,
        };
    }

    public void Restore(Snapshot s, CombatActor player)
    {
        if (s.cellSkillObj != null)
            Array.Copy(s.cellSkillObj, _cellSkill, 3);
        else
        {
            // Back-compat restore (legacy only)
            for (int i = 0; i < 3; i++)
                _cellSkill[i] = (s.cellSkill != null && i < s.cellSkill.Length) ? s.cellSkill[i] : null;
        }

        if (s.cellAnchor != null) Array.Copy(s.cellAnchor, _cellAnchor, 3);
        if (s.anchorSpan != null) Array.Copy(s.anchorSpan, _anchorSpan, 3);
        if (s.anchorStart0 != null) Array.Copy(s.anchorStart0, _anchorStart0, 3);
        if (s.anchorBaseStart0 != null) Array.Copy(s.anchorBaseStart0, _anchorBaseStart0, 3);
        if (s.anchorReservedCost != null) Array.Copy(s.anchorReservedCost, _anchorReservedCost, 3);
        if (s.anchorRuntime != null) Array.Copy(s.anchorRuntime, _anchorRuntime, 3);

        if (player != null) player.focus = s.playerFocus;
    }

    // ---------------------------
    // Queries
    // ---------------------------

    public bool IsSkillEquipped(SkillSO skill)
    {
        if (skill == null) return false;
        for (int i = 0; i < 3; i++)
            if (_cellSkill[i] == skill) return true;
        return false;
    }

    public bool IsSkillEquipped(SkillDamageSO skill)
    {
        if (skill == null) return false;
        for (int i = 0; i < 3; i++)
            if (_cellSkill[i] == skill) return true;
        return false;
    }

    public bool IsSkillEquipped(SkillBuffDebuffSO skill)
    {
        if (skill == null) return false;
        for (int i = 0; i < 3; i++)
            if (_cellSkill[i] == skill) return true;
        return false;
    }

    // Legacy getter (kept): returns SkillSO or null if a V2 skill is in that cell.
    public SkillSO GetCellSkill(int i0) => (i0 >= 0 && i0 < 3) ? (_cellSkill[i0] as SkillSO) : null;

    // New getter: returns actual stored object (SkillSO / SkillDamageSO / SkillBuffDebuffSO)
    public ScriptableObject GetCellSkillObject(int i0) => (i0 >= 0 && i0 < 3) ? _cellSkill[i0] : null;
    public SkillDamageSO GetCellDamageSkill(int i0) => (i0 >= 0 && i0 < 3) ? (_cellSkill[i0] as SkillDamageSO) : null;
    public SkillBuffDebuffSO GetCellBuffDebuffSkill(int i0) => (i0 >= 0 && i0 < 3) ? (_cellSkill[i0] as SkillBuffDebuffSO) : null;

    public int GetCellAnchor(int i0) => (i0 >= 0 && i0 < 3) ? _cellAnchor[i0] : -1;
    public int GetAnchorSpan(int anchor0) => (anchor0 >= 0 && anchor0 < 3) ? _anchorSpan[anchor0] : 0;
    public SkillRuntime GetAnchorRuntime(int anchor0) => (anchor0 >= 0 && anchor0 < 3) ? _anchorRuntime[anchor0] : null;
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
    {
        bool[] counted = new bool[3];
        int count = 0;

        for (int j = start0; j < start0 + span; j++)
        {
            int a = _cellAnchor[j];
            if (a >= 0 && !counted[a])
            {
                counted[a] = true;
                count++;
            }
        }
        return count;
    }

    public int ComputeRefundInRange(int start0, int span)
    {
        bool[] counted = new bool[3];
        int refund = 0;

        for (int j = start0; j < start0 + span; j++)
        {
            int a = _cellAnchor[j];
            if (a >= 0 && !counted[a])
            {
                counted[a] = true;
                refund += _anchorReservedCost[a];
            }
        }
        return refund;
    }

    // ---------------------------
    // Clear / Place
    // ---------------------------

    public void ClearGroupsInRange(int start0, int span, CombatActor player)
    {
        bool[] cleared = new bool[3];
        for (int j = start0; j < start0 + span; j++)
        {
            int a = _cellAnchor[j];
            if (a >= 0 && !cleared[a])
            {
                cleared[a] = true;
                ClearGroupAtAnchor(a, player);
            }
        }
    }

    public void ClearGroupAtSlot0(int anySlot0, CombatActor player)
    {
        int a = _cellAnchor[anySlot0];
        if (a < 0) return;
        ClearGroupAtAnchor(a, player);
    }

    public void ClearGroupAtAnchor(int anchor0, CombatActor player)
    {
        if (anchor0 < 0 || anchor0 > 2) return;

        int refund = _anchorReservedCost[anchor0];
        if (refund > 0 && player != null) player.GainFocus(refund);

        for (int i = 0; i < 3; i++)
        {
            if (_cellAnchor[i] == anchor0)
            {
                _cellSkill[i] = null;
                _cellAnchor[i] = -1;
            }
        }

        _anchorReservedCost[anchor0] = 0;
        _anchorSpan[anchor0] = 0;
        _anchorStart0[anchor0] = -1;
        _anchorBaseStart0[anchor0] = -1;
        _anchorRuntime[anchor0] = null;
    }

    /// <summary>
    /// Remove a group after casting. NO refund.
    /// </summary>
    public void ConsumeGroupAtAnchor_NoRefund(int anchor0)
    {
        if (anchor0 < 0 || anchor0 > 2) return;

        for (int i = 0; i < 3; i++)
        {
            if (_cellAnchor[i] == anchor0)
            {
                _cellSkill[i] = null;
                _cellAnchor[i] = -1;
            }
        }

        _anchorReservedCost[anchor0] = 0;
        _anchorSpan[anchor0] = 0;
        _anchorStart0[anchor0] = -1;
        _anchorBaseStart0[anchor0] = -1;
        _anchorRuntime[anchor0] = null;
    }

    /// <summary>
    /// Place a group and let RecalculateRuntimesAndRebalance() reserve the cost.
    /// Supports legacy SkillSO and V2 active skills (SkillDamageSO / SkillBuffDebuffSO).
    /// Passive skills must never be placed.
    /// </summary>
    public void PlaceGroup(int start0, int anchor0, int span, ScriptableObject skillObj)
    {
        if (skillObj == null) return;
        if (skillObj is SkillPassiveSO) return; // safety

        for (int j = start0; j < start0 + span; j++)
        {
            _cellSkill[j] = skillObj;
            _cellAnchor[j] = anchor0;
        }

        _anchorSpan[anchor0] = span;
        _anchorStart0[anchor0] = start0;
        _anchorBaseStart0[anchor0] = start0;
        _anchorReservedCost[anchor0] = 0; // reserved later
        _anchorRuntime[anchor0] = null;
    }

    // Legacy overload kept so older call sites don't break
    public void PlaceGroup(int start0, int anchor0, int span, SkillSO skill)
        => PlaceGroup(start0, anchor0, span, (ScriptableObject)skill);

    // ---------------------------
    // Dice sum / runtime eval
    // ---------------------------

    public int GetStartForAnchor(int anchor0)
    {
        if (anchor0 < 0 || anchor0 > 2) return 0;
        int s = _anchorStart0[anchor0];
        return Mathf.Clamp(s, 0, 2);
    }

    public int GetDieSumForAnchor(int anchor0, DiceSlotRig diceRig)
    {
        if (diceRig == null) return 0;
        if (anchor0 < 0 || anchor0 > 2) return 0;

        int sp = Mathf.Clamp(_anchorSpan[anchor0], 0, 3);
        if (sp <= 0) return 0;

        int start0 = Mathf.Clamp(_anchorStart0[anchor0], 0, 2);
        if (sp == 1)
        {
            return diceRig.GetDieValue(anchor0);
        }
        if (sp == 2)
        {
            int a = start0;
            int b = start0 + 1;
            return diceRig.GetDieValue(a) + diceRig.GetDieValue(b);
        }

        // sp == 3
        return diceRig.GetDieValue(0) + diceRig.GetDieValue(1) + diceRig.GetDieValue(2);
    }

    private int SanitizeStart0ForSpan(int start0, int span)
    {
        if (span <= 1) return Mathf.Clamp(start0, 0, 2);
        if (span == 2) return Mathf.Clamp(start0, 0, 1);
        return 0;
    }

    private static int GetSlotsRequired(ScriptableObject skillObj)
    {
        if (skillObj == null) return 1;
        if (skillObj is SkillSO s) return s.slotsRequired;
        if (skillObj is SkillDamageSO d) return d.slotsRequired;
        if (skillObj is SkillBuffDebuffSO b) return b.slotsRequired;
        return 1;
    }

    private static int GetFocusCost(ScriptableObject skillObj, SkillRuntime rt)
    {
        if (rt != null) return rt.focusCost;
        if (skillObj is SkillSO s) return s.focusCost;
        if (skillObj is SkillDamageSO d) return d.focusCost;
        if (skillObj is SkillBuffDebuffSO b) return b.focusCost;
        return 0;
    }

    private static SkillRuntime EvaluateRuntimeForPlanning(ScriptableObject skillObj, DiceSlotRig diceRig, int anchor0, int span, int start0)
    {
        if (skillObj == null) return null;

        // Legacy path
        if (skillObj is SkillSO legacy)
            return SkillRuntimeEvaluator.Evaluate(legacy, diceRig, anchor0, span, start0);

        // V2 Damage path (minimal runtime: focus/slots + condition overrides for those only)
        if (skillObj is SkillDamageSO dmg)
        {
            var rt = new SkillRuntime
            {
                source = null,
                kind = dmg.kind,
                slotsRequired = Mathf.Clamp(dmg.slotsRequired, 1, 3),
                focusCost = Mathf.Max(0, dmg.focusCost),
                focusGainOnCast = dmg.focusGainOnCast,
                conditionMet = false,
            };

            if (dmg.hasCondition && dmg.condition != null && diceRig != null)
            {
                var dice = GatherDiceForScope(dmg.condition.scope, diceRig, start0, span);
                bool met = dmg.condition.Evaluate(dice);
                rt.conditionMet = met;

                if (met && dmg.whenConditionIsMet != null)
                {
                    var ov = dmg.whenConditionIsMet;
                    if (ov.overrideCost)
                    {
                        rt.focusCost = Mathf.Max(0, ov.focusCost);
                        rt.focusGainOnCast = ov.focusGainOnCast;
                    }
                    if (ov.overrideSlotsRequired)
                    {
                        rt.slotsRequired = Mathf.Clamp(ov.slotsRequired, 1, 3);
                    }
                    if (ov.overrideIdentity)
                    {
                        rt.kind = ov.kind;
                    }
                }
            }

            return rt;
        }

        // V2 Buff/Debuff path (no condition)
        if (skillObj is SkillBuffDebuffSO bd)
        {
            return new SkillRuntime
            {
                source = null,
                kind = SkillKind.Attack, // placeholder (not used by board)
                slotsRequired = Mathf.Clamp(bd.slotsRequired, 1, 3),
                focusCost = Mathf.Max(0, bd.focusCost),
                focusGainOnCast = bd.focusGainOnCast,
                conditionMet = false,
            };
        }

        return null;
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

    /// <summary>
    /// Resize the CURRENT occupied group for an anchor to a new span (1..3) without changing anchor index.
    /// - Shrink is always allowed.
    /// - Expand only succeeds if newly needed cells are empty (or already belong to the same anchor) and active.
    /// </summary>
    private bool TryResizeGroupToSpan(int anchor0, int desiredSpan, DiceSlotRig diceRig)
    {
        if (anchor0 < 0 || anchor0 > 2) return false;
        if (!IsAnchorSlot(anchor0)) return false;

        desiredSpan = Mathf.Clamp(desiredSpan, 1, 3);
        int currentSpan = Mathf.Clamp(_anchorSpan[anchor0], 1, 3);
        if (desiredSpan == currentSpan) return true;

        ScriptableObject skillObj = _cellSkill[anchor0];
        if (skillObj == null) return false;

        int currentStart0 = SanitizeStart0ForSpan(_anchorStart0[anchor0], currentSpan);

        // choose desired start0 that keeps anchor inside range and is stable
        int desiredStart0;
        if (desiredSpan == 3)
        {
            desiredStart0 = 0;
        }
        else if (desiredSpan == 2)
        {
            int optA = Mathf.Clamp(anchor0, 0, 1);
            int optB = Mathf.Clamp(anchor0 - 1, 0, 1);

            bool currentOk = (currentStart0 >= 0 && currentStart0 <= 1 && anchor0 >= currentStart0 && anchor0 <= currentStart0 + 1);
            if (currentOk) desiredStart0 = currentStart0;
            else
            {
                // prefer the option closer to currentStart0
                desiredStart0 = (Mathf.Abs(currentStart0 - optA) <= Mathf.Abs(currentStart0 - optB)) ? optA : optB;
            }
        }
        else
        {
            desiredStart0 = anchor0;
        }

        desiredStart0 = SanitizeStart0ForSpan(desiredStart0, desiredSpan);

        // Validate target range
        for (int i = desiredStart0; i < desiredStart0 + desiredSpan; i++)
        {
            if (diceRig != null && !diceRig.IsSlotActive(i)) return false;
            int occ = _cellAnchor[i];
            if (occ != -1 && occ != anchor0) return false;
        }

        // Clear old cells outside new range
        for (int i = 0; i < 3; i++)
        {
            if (_cellAnchor[i] != anchor0) continue;
            bool keep = (i >= desiredStart0 && i < desiredStart0 + desiredSpan);
            if (!keep)
            {
                _cellSkill[i] = null;
                _cellAnchor[i] = -1;
            }
        }

        // Fill new range
        for (int i = desiredStart0; i < desiredStart0 + desiredSpan; i++)
        {
            _cellSkill[i] = skillObj;
            _cellAnchor[i] = anchor0;
        }

        _anchorSpan[anchor0] = desiredSpan;
        _anchorStart0[anchor0] = desiredStart0;
        return true;
    }

    /// <summary>
    /// Re-evaluate all anchor runtimes (planning view) then rebalance reserved focus costs.
    /// Returns false if player doesn't have enough focus to satisfy new costs (caller should rollback).
    /// </summary>
    public bool RecalculateRuntimesAndRebalance(CombatActor player, DiceSlotRig diceRig)
    {
        // 1) Evaluate all anchor runtimes using BASE placement (stable condition), then apply slot resize for gameplay.
        for (int a = 0; a < 3; a++)
        {
            if (!IsAnchorSlot(a))
            {
                _anchorRuntime[a] = null;
                continue;
            }

            ScriptableObject skillObj = _cellSkill[a];
            if (skillObj == null)
            {
                _anchorRuntime[a] = null;
                continue;
            }

            int baseSpan = Mathf.Clamp(GetSlotsRequired(skillObj), 1, 3);
            int baseStart0 = SanitizeStart0ForSpan(_anchorBaseStart0[a], baseSpan);

            _anchorRuntime[a] = EvaluateRuntimeForPlanning(skillObj, diceRig, a, baseSpan, baseStart0);

            int desiredSpan = Mathf.Clamp((_anchorRuntime[a] != null ? _anchorRuntime[a].slotsRequired : baseSpan), 1, 3);
            int currentSpan = Mathf.Clamp(_anchorSpan[a], 1, 3);
            if (desiredSpan != currentSpan)
            {
                bool ok = TryResizeGroupToSpan(a, desiredSpan, diceRig);
                if (!ok && _anchorRuntime[a] != null)
                {
                    // Can't expand -> keep gameplay span consistent
                    _anchorRuntime[a].slotsRequired = currentSpan;
                }
            }
        }

        // 2) Rebalance reserved focus cost (only in planning)
        for (int a = 0; a < 3; a++)
        {
            int desired = 0;
            if (IsAnchorSlot(a))
            {
                desired = Mathf.Max(0, GetFocusCost(_cellSkill[a], _anchorRuntime[a]));
            }

            int current = _anchorReservedCost[a];
            if (desired == current) continue;

            if (desired > current)
            {
                int need = desired - current;
                if (player != null)
                {
                    if (!player.TrySpendFocus(need))
                        return false;
                }
                _anchorReservedCost[a] = desired;
            }
            else
            {
                int refund = current - desired;
                if (refund > 0 && player != null) player.GainFocus(refund);
                _anchorReservedCost[a] = desired;
            }
        }

        return true;
    }
}
