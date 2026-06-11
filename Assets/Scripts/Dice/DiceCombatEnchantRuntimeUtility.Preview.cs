using UnityEngine;

public static partial class DiceCombatEnchantRuntimeUtility
{
    public static int ComputeCommittedPreviewDieSum(
        DiceSlotRig diceRig,
        CombatActor owner,
        int start0,
        int diceCost,
        ElementType skillElement)
    {
        if (diceRig == null || owner == null)
            return 0;

        CommittedFaceUsePlan plan = BuildPaymentPlan(diceRig, start0, diceCost);
        if (plan.selectedMask == 0)
            return 0;

        int sum = 0;
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!plan.IsSelected(slot0))
                continue;

            sum += GetCommittedPreviewResolvedBreakdown(diceRig, owner, slot0, skillElement, plan).resolvedValue;
        }

        return sum;
    }

    public static DiceSlotRig.ResolvedDieBreakdown GetCommittedPreviewResolvedBreakdown(
        DiceSlotRig diceRig,
        CombatActor owner,
        int slot0,
        ElementType skillElement,
        int paymentMask)
        => GetCommittedPreviewResolvedBreakdown(diceRig, owner, slot0, skillElement, paymentMask, includeSyntheticExternalAdded: true);

    public static DiceSlotRig.ResolvedDieBreakdown GetCommittedPreviewResolvedBreakdown(
        DiceSlotRig diceRig,
        CombatActor owner,
        int slot0,
        ElementType skillElement,
        int paymentMask,
        bool includeSyntheticExternalAdded)
    {
        CommittedFaceUsePlan plan = new CommittedFaceUsePlan
        {
            selectedMask = Mathf.Max(0, paymentMask)
        };
        return GetCommittedPreviewResolvedBreakdown(diceRig, owner, slot0, skillElement, plan, includeSyntheticExternalAdded);
    }

    public static DiceSlotRig.ResolvedDieBreakdown GetCommittedPreviewResolvedBreakdown(
        DiceSlotRig diceRig,
        CombatActor owner,
        int slot0,
        ElementType skillElement,
        CommittedFaceUsePlan plan)
        => GetCommittedPreviewResolvedBreakdown(diceRig, owner, slot0, skillElement, plan, includeSyntheticExternalAdded: true);

    public static DiceSlotRig.ResolvedDieBreakdown GetCommittedPreviewResolvedBreakdown(
        DiceSlotRig diceRig,
        CombatActor owner,
        int slot0,
        ElementType skillElement,
        CommittedFaceUsePlan plan,
        bool includeSyntheticExternalAdded)
    {
        if (diceRig == null)
            return default;

        DiceSlotRig.ResolvedDieBreakdown breakdown = diceRig.GetResolvedBreakdown(slot0, owner, skillElement);
        if (breakdown.outputBaseValue <= 0)
            return breakdown;

        bool isCrit = breakdown.isCrit;
        bool isFail = breakdown.isFail;
        bool appliesFailPenalty = breakdown.appliesFailPenalty;
        int critFailAdded = breakdown.critFailAddedValue;

        DiceSpinnerGeneric die = diceRig.GetDice(slot0);
        DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
        bool usesCommittedDouble =
            plan != null &&
            plan.IsSelected(slot0) &&
            effective == DiceFaceEnchantKind.Double &&
            die != null &&
            die.IsCurrentFaceUsable();

        if (usesCommittedDouble)
        {
            int outputBaseValue = breakdown.outputBaseValue;
            int maxFace = die.GetMaxFaceValue();
            int minFace = die.GetMinFaceValue();
            isCrit = outputBaseValue >= maxFace;
            isFail = minFace != maxFace && outputBaseValue <= minFace;

            bool grantsCritBonus = isCrit && !DiceFaceEnchantUtility.SuppressesCritBonus(effective);
            appliesFailPenalty = isFail && !DiceFaceEnchantUtility.SuppressesFailPenalty(effective);
            float critPercent = skillElement == ElementType.Physical
                ? DiceSlotRig.PhysicalCritPercent
                : DiceSlotRig.GenericCritPercent;
            critFailAdded = grantsCritBonus
                ? Mathf.FloorToInt(outputBaseValue * critPercent)
                : 0;
        }

        int externalAdded = includeSyntheticExternalAdded
            ? ComputeCommittedExternalAddedValue(diceRig, slot0, plan)
            : 0;
        int totalAdded = breakdown.faceEnchantAddedValue + breakdown.passiveAddedValue + critFailAdded + externalAdded;
        int resolved = breakdown.outputBaseValue + totalAdded;
        if (resolved < 1)
            resolved = 1;

        breakdown.critFailAddedValue = critFailAdded;
        breakdown.totalAddedValue = totalAdded;
        breakdown.resolvedValue = resolved;
        breakdown.isCrit = isCrit;
        breakdown.isFail = isFail;
        breakdown.appliesFailPenalty = appliesFailPenalty;
        return breakdown;
    }

    private static int ComputeCommittedExternalAddedValue(
        DiceSlotRig diceRig,
        int targetSlot0,
        CommittedFaceUsePlan plan)
    {
        int added = 0;
        int sourceSlot0 = targetSlot0 - 1;
        if (sourceSlot0 >= 0 && plan.IsSelected(sourceSlot0))
        {
            DiceSpinnerGeneric target = diceRig.GetDice(targetSlot0);
            DiceFaceEnchantKind source = diceRig.GetEffectiveCurrentFaceEnchant(sourceSlot0);
            if (source == DiceFaceEnchantKind.Relay && target != null && target.IsCurrentFaceUsable())
                added += DiceFaceEnchantUtility.RelayValueModifier;
        }

        return added;
    }
}
