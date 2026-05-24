using UnityEngine;
using System;

public partial class DiceSlotRig
{
    public RollInfo GetRollInfo(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= LastRollInfos.Length) return default;
        return LastRollInfos[slot0];
    }

    public int GetBaseValue(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        return GetRollInfo(slot0).rolledValue;
    }

    public int GetContribution(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        return GetRollInfo(slot0).Contribution;
    }

    public bool IsCrit(int slot0) => GetRollInfo(slot0).isCrit;
    public bool IsFail(int slot0) => GetRollInfo(slot0).isFail;
    public bool AppliesFailPenalty(int slot0) => GetRollInfo(slot0).appliesFailPenalty;
    public bool IsNumericFaceForConditions(int slot0) => GetRollInfo(slot0).isNumericFace;

    public int GetAddedValue(int slot0, CombatActor owner, ElementType skillElement = ElementType.Neutral)
    {
        if (!HasRolledThisTurn) return 0;
        ResolvedDieBreakdown b = GetResolvedBreakdown(slot0, owner, skillElement);
        return b.totalAddedValue;
    }

    public int GetResolvedContribution(int slot0, CombatActor owner)
    {
        return GetResolvedContribution(slot0, owner, ElementType.Neutral);
    }

    public int GetResolvedContribution(int slot0, CombatActor owner, ElementType skillElement)
    {
        if (!HasRolledThisTurn) return 0;
        return GetResolvedBreakdown(slot0, owner, skillElement).resolvedValue;
    }

    public ResolvedDieBreakdown GetResolvedBreakdown(int slot0, CombatActor owner, ElementType skillElement = ElementType.Neutral)
    {
        RollInfo info = GetRollInfo(slot0);
        if (!HasRolledThisTurn) return default;

        int critFailAdded = ComputeCritFailAddedValue(info, skillElement);
        int faceEnchantAdded = ComputeFaceEnchantAddedValue(slot0);
        int passiveAdded = ComputeAllDiceDelta(owner);
        PassiveSystem ps = owner != null ? owner.GetComponent<PassiveSystem>() : null;
        if (ps != null)
            passiveAdded += ps.GetAddedValueForDie(this, slot0);
        int totalAdded = critFailAdded + faceEnchantAdded + passiveAdded;
        int resolved = info.rolledValue + totalAdded;
        if (resolved < 1) resolved = 1;

        return new ResolvedDieBreakdown
        {
            baseValue = info.rolledValue,
            critFailAddedValue = critFailAdded,
            faceEnchantAddedValue = faceEnchantAdded,
            passiveAddedValue = passiveAdded,
            totalAddedValue = totalAdded,
            resolvedValue = resolved,
            isCrit = info.isCrit,
            isFail = info.isFail,
            appliesFailPenalty = info.appliesFailPenalty
        };
    }

    public int ActiveSlotCount()
    {
        EnsureSlots();
        int c = 0;
        for (int i = 0; i < slots.Length; i++)
            if (IsSlotActive(i)) c++;
        return c;
    }

    public bool CanFitAtDrop(int dropSlot0, int requiredSlots)
    {
        EnsureSlots();
        requiredSlots = Mathf.Clamp(requiredSlots, 1, 3);

        if (requiredSlots == 1)
            return IsSlotActive(dropSlot0);

        if (requiredSlots == 2)
        {
            if (dropSlot0 == 0) return IsSlotActive(0) && IsSlotActive(1);
            if (dropSlot0 == 1) return IsSlotActive(1) && IsSlotActive(2);
            if (dropSlot0 == 2) return IsSlotActive(1) && IsSlotActive(2);
            return false;
        }

        return IsSlotActive(0) && IsSlotActive(1) && IsSlotActive(2);
    }

    public void ApplyActiveStates()
    {
        EnsureSlots();
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            Entry e = slots[i];
            if (e == null) continue;

            if (e.diceRoot == null && e.dice != null)
                e.diceRoot = e.dice.gameObject;

            if (disableDiceSelfInput && e.dice != null)
                e.dice.enableSpaceKey = false;

            if (e.diceRoot != null)
                e.diceRoot.SetActive(e.active);

            if (e.slotRoot != null)
                e.slotRoot.SetActive(e.active);
        }
    }

    public int GetResolvedDieValue(int slot0, CombatActor owner)
    {
        return GetResolvedContribution(slot0, owner, ElementType.Neutral);
    }

    public int GetResolvedDieValue(int slot0, CombatActor owner, ElementType skillElement)
    {
        return GetResolvedContribution(slot0, owner, skillElement);
    }

    public int GetMinFaceValue(int slot0)
    {
        DiceSpinnerGeneric d = GetDice(slot0);
        if (d == null) return 1;
        return d.GetMinFaceValue();
    }

    public int GetMaxFaceValue(int slot0)
    {
        DiceSpinnerGeneric d = GetDice(slot0);
        if (d == null) return 6;
        return d.GetMaxFaceValue();
    }

    public SpanStats ComputeSpanStats(int start0, int span, CombatActor owner = null)
    {
        return ComputeSpanStats(start0, span, owner, ElementType.Neutral);
    }

    public SpanStats ComputeSpanStats(int start0, int span, CombatActor owner, ElementType skillElement)
    {
        EnsureSlots();

        span = Mathf.Clamp(span, 1, 3);
        int end0 = Mathf.Min(2, start0 + span - 1);

        int count = 0;
        int sum = 0;
        bool critAny = false;
        bool critAll = true;
        bool failAny = false;

        for (int i = start0; i <= end0; i++)
        {
            if (!IsSlotActive(i)) continue;
            count++;

            RollInfo info = GetRollInfo(i);
            sum += GetResolvedContribution(i, owner, skillElement);
            critAny |= info.isCrit;
            critAll &= info.isCrit;
            failAny |= info.isFail;
        }

        if (count == 0) critAll = false;

        return new SpanStats
        {
            sumContribution = sum,
            critAny = critAny,
            critAll = critAll,
            failAny = failAny
        };
    }

    private int ComputeAllDiceDelta(CombatActor owner)
    {
        int delta = 0;
        if (onComputeAllDiceDelta != null)
        {
            foreach (Func<CombatActor, int> f in onComputeAllDiceDelta.GetInvocationList())
            {
                try { delta += f(owner); }
                catch { }
            }
        }
        return delta;
    }

    private int ComputeCritFailAddedValue(RollInfo info, ElementType skillElement)
    {
        if (info.rolledValue <= 0) return 0;
        if (!info.grantsCritBonus) return 0;
        if (!info.isCrit) return 0;

        float critPercent = (skillElement == ElementType.Physical) ? PhysicalCritPercent : GenericCritPercent;
        return FloorScaled(info.rolledValue, critPercent);
    }

    private int ComputeFaceEnchantAddedValue(int slot0)
    {
        DiceSpinnerGeneric die = GetDice(slot0);
        if (die == null)
            return 0;

        return die.GetCurrentFaceAddedValue();
    }

    private static int FloorScaled(int value, float factor)
    {
        return Mathf.FloorToInt(value * factor);
    }

}

