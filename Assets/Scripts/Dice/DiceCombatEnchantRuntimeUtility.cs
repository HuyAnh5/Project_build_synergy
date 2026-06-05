using System.Collections.Generic;
using UnityEngine;

public static class DiceCombatEnchantRuntimeUtility
{
    private sealed class WholeDieCombatState
    {
        public bool usedThisCombat;
        public readonly HashSet<int> goldMarkedFaces = new HashSet<int>();
    }

    public sealed class CommittedFaceUsePlan
    {
        public int selectedMask;
        public int breakMask;
        public int reloadMask;
        public int repeatCount;
        public int paidCost;
        public readonly int[] committedFaceIndices = { -1, -1, -1 };

        public bool IsSelected(int slot0) => (selectedMask & (1 << slot0)) != 0;
        public bool ShouldBreak(int slot0) => (breakMask & (1 << slot0)) != 0;
        public bool ShouldReload(int slot0) => (reloadMask & (1 << slot0)) != 0;
    }

    public struct SimpleEnchantPreview
    {
        public int focusGain;
        public int guardGain;
    }

    private static readonly Dictionary<DiceSpinnerGeneric, WholeDieCombatState> WholeDieStates = new Dictionary<DiceSpinnerGeneric, WholeDieCombatState>();

    public static void BeginCombat(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return;

        WholeDieStates.Clear();

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null || WholeDieStates.ContainsKey(die))
                continue;

            WholeDieStates[die] = new WholeDieCombatState();
        }
    }

    public static void MarkDieUsedInCombat(DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        if (!WholeDieStates.TryGetValue(die, out WholeDieCombatState state))
        {
            state = new WholeDieCombatState();
            WholeDieStates[die] = state;
        }

        state.usedThisCombat = true;
    }

    public static CommittedFaceUsePlan BuildPaymentPlan(DiceSlotRig diceRig, int start0, int diceCost)
    {
        CommittedFaceUsePlan plan = new CommittedFaceUsePlan();
        if (diceRig == null || diceRig.slots == null)
            return plan;

        int end = Mathf.Min(diceRig.slots.Length, start0 + Mathf.Clamp(diceCost, 1, 3));
        int begin = Mathf.Clamp(start0, 0, 2);
        int required = Mathf.Clamp(diceCost, 1, 3);

        for (int i = begin; i < end && plan.paidCost < required; i++)
        {
            DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(i);
            AddPaymentDie(diceRig, plan, i, effective);
        }

        return plan;
    }

    public static CommittedFaceUsePlan BuildPaymentPlanFromMask(DiceSlotRig diceRig, int paymentMask)
    {
        CommittedFaceUsePlan plan = new CommittedFaceUsePlan();
        if (diceRig == null || diceRig.slots == null || paymentMask <= 0)
            return plan;

        int count = Mathf.Min(diceRig.slots.Length, 3);
        for (int i = 0; i < count; i++)
        {
            if ((paymentMask & (1 << i)) == 0)
                continue;

            DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(i);
            AddPaymentDie(diceRig, plan, i, effective);
        }

        return plan;
    }

    private static void AddPaymentDie(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        int slot0,
        DiceFaceEnchantKind effective)
    {
        if (diceRig == null || plan == null)
            return;
        if (!diceRig.IsSlotActive(slot0) || !diceRig.IsCurrentFaceUsableForPayment(slot0))
            return;

        DiceSpinnerGeneric die = diceRig.slots[slot0] != null ? diceRig.slots[slot0].dice : null;
        if (die == null)
            return;

        DiceFaceEnchantKind stored = die.GetCurrentFaceEnchant();
        int contribution = effective == DiceFaceEnchantKind.Heavy
            ? DiceFaceEnchantUtility.HeavyPaymentContribution
            : 1;

        plan.selectedMask |= 1 << slot0;
        plan.committedFaceIndices[slot0] = die.LastFaceIndex;
        plan.paidCost += Mathf.Max(1, contribution);
        if (DiceFaceEnchantUtility.BreaksAfterCommittedUse(stored))
            plan.breakMask |= 1 << slot0;
        if (effective == DiceFaceEnchantKind.Reload)
            plan.reloadMask |= 1 << slot0;
        if (effective == DiceFaceEnchantKind.Repeat)
            plan.repeatCount++;
    }

    public static int ResolveCommittedSelfFaceEnchants(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        CombatActor caster)
    {
        if (diceRig == null || plan == null || caster == null)
            return 0;

        int popupCount = 0;
        for (int slot0 = 0; slot0 < 3; slot0++)
            popupCount += ResolveCommittedSelfFaceEnchant(diceRig, plan, caster, slot0) ? 1 : 0;

        diceRig.RefreshRollInfoCache();
        return popupCount;
    }

    public static int ResolveCommittedRelayFaceEnchants(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan)
    {
        if (diceRig == null || plan == null)
            return 0;

        int popupCount = 0;
        for (int slot0 = 0; slot0 < 3; slot0++)
            popupCount += ResolveCommittedRelayFaceEnchant(diceRig, plan, slot0);

        diceRig.RefreshRollInfoCache();
        return popupCount;
    }

    public static bool CanResolveCommittedPreSkillFaceEnchant(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        CombatActor caster,
        int slot0)
    {
        if (diceRig == null || plan == null || !plan.IsSelected(slot0))
            return false;

        DiceSpinnerGeneric die = diceRig.GetDice(slot0);
        if (die == null || !die.IsCurrentFaceUsable())
            return false;

        DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
        switch (effective)
        {
            case DiceFaceEnchantKind.Power:
            case DiceFaceEnchantKind.Heavy:
            case DiceFaceEnchantKind.Charge:
            case DiceFaceEnchantKind.Gold:
            case DiceFaceEnchantKind.Double:
            case DiceFaceEnchantKind.Stone:
            case DiceFaceEnchantKind.Repeat:
            case DiceFaceEnchantKind.Relay:
                return true;
            case DiceFaceEnchantKind.Guard:
                return caster != null;
            default:
                return false;
        }
    }

    public static int ResolveCommittedPreSkillFaceEnchant(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        CombatActor caster,
        int slot0)
    {
        if (diceRig == null || plan == null || !plan.IsSelected(slot0))
            return 0;

        DiceSpinnerGeneric die = diceRig.GetDice(slot0);
        if (die == null || !die.IsCurrentFaceUsable())
            return 0;

        DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
        if (effective == DiceFaceEnchantKind.Relay)
            return ResolveCommittedRelayFaceEnchant(diceRig, plan, slot0);

        return ResolveCommittedSelfFaceEnchant(diceRig, plan, caster, slot0) ? 1 : 0;
    }

    public static SimpleEnchantPreview ComputeCommittedSimpleEnchantPreview(
        DiceSlotRig diceRig,
        CombatActor caster,
        int start0,
        int diceCost)
    {
        SimpleEnchantPreview preview = default;
        if (diceRig == null || caster == null)
            return preview;

        CommittedFaceUsePlan plan = BuildPaymentPlan(diceRig, start0, diceCost);
        if (plan.selectedMask == 0)
            return preview;

        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!plan.IsSelected(slot0))
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null || !die.IsCurrentFaceUsable())
                continue;

            DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
            switch (effective)
            {
                case DiceFaceEnchantKind.Guard:
                    preview.guardGain += Mathf.Max(0, diceRig.GetResolvedDieValue(slot0, caster));
                    break;
                case DiceFaceEnchantKind.Charge:
                    preview.focusGain += 1;
                    break;
            }
        }

        return preview;
    }

    private static bool ResolveCommittedSelfFaceEnchant(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        CombatActor caster,
        int slot0)
    {
        if (!plan.IsSelected(slot0))
            return false;

        DiceSpinnerGeneric die = diceRig.GetDice(slot0);
        if (die == null || !die.IsCurrentFaceUsable())
            return false;

        DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
        DiceFaceEnchantKind stored = die.GetCurrentFaceEnchant();
        switch (effective)
        {
            case DiceFaceEnchantKind.Power:
                die.PlayFaceEnchantPopup(stored, $"+{DiceFaceEnchantUtility.PowerAddedValue} Value");
                break;
            case DiceFaceEnchantKind.Heavy:
                die.PlayFaceEnchantPopup(stored, "+1 dice");
                break;
            case DiceFaceEnchantKind.Guard:
                int guard = diceRig.GetResolvedDieValue(slot0, caster);
                caster.AddGuard(guard);
                die.PlayFaceEnchantPopup(stored, $"+{guard} Guard");
                break;
            case DiceFaceEnchantKind.Charge:
                caster.GainFocus(1);
                die.PlayFaceEnchantPopup(stored, "+1 AP");
                break;
            case DiceFaceEnchantKind.Gold:
                MarkGoldFace(die);
                die.PlayFaceEnchantPopup(stored, "+Gold");
                break;
            case DiceFaceEnchantKind.Double:
                die.PlayFaceEnchantPopup(stored);
                break;
            case DiceFaceEnchantKind.Stone:
                die.PlayFaceEnchantPopup(stored, $"+{DiceFaceEnchantUtility.StoneAddedValue} Value");
                break;
            case DiceFaceEnchantKind.Repeat:
                die.PlayFaceEnchantPopup(stored);
                break;
            default:
                return false;
        }

        return true;
    }

    private static int ResolveCommittedRelayFaceEnchant(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        int slot0)
    {
        if (!plan.IsSelected(slot0))
            return 0;

        DiceSpinnerGeneric die = diceRig.GetDice(slot0);
        if (die == null || !die.IsCurrentFaceUsable())
            return 0;

        DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
        if (effective != DiceFaceEnchantKind.Relay)
            return 0;

        DiceFaceEnchantKind stored = die.GetCurrentFaceEnchant();
        DiceSpinnerGeneric right = diceRig.GetDice(slot0 + 1);
        int popupCount = 1;
        if (right != null && right.IsCurrentFaceUsable())
        {
            right.AddPhaseValueModifier(DiceFaceEnchantUtility.RelayValueModifier);
            die.PlayFaceEnchantPopup(stored);
            right.PlayFaceEnchantEffectPopup($"+{DiceFaceEnchantUtility.RelayValueModifier}");
            popupCount++;
        }
        else
        {
            die.PlayFaceEnchantPopup(stored, "No target");
        }

        return popupCount;
    }

    public static bool PlayRepeatAgainPopup(DiceSlotRig diceRig, CommittedFaceUsePlan plan)
    {
        if (diceRig == null || plan == null)
            return false;

        bool played = false;
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!plan.IsSelected(slot0))
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null)
                continue;

            DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
            if (effective != DiceFaceEnchantKind.Repeat)
                continue;

            die.PlayFaceEnchantEffectPopup("Again");
            played = true;
        }

        return played;
    }

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

    public static void ResolveCommittedPostSkillFaceEnchants(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        TurnManager turnManager)
    {
        if (diceRig == null || plan == null)
            return;

        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!plan.IsSelected(slot0))
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null)
                continue;

            DiceFaceEnchantKind stored = die.GetCurrentFaceEnchant();
            DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
            if (stored == DiceFaceEnchantKind.Echo && effective == DiceFaceEnchantKind.None)
                die.PlayFaceEnchantPopup(stored, "No copy");

            if (plan.ShouldBreak(slot0) && !plan.ShouldReload(slot0))
                die.SetFaceBroken(plan.committedFaceIndices[slot0], true);
        }

        diceRig.RefreshRollInfoCache();
    }

    public static bool PlayCommittedReloadPopups(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan)
    {
        if (diceRig == null || plan == null)
            return false;

        bool played = false;
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!plan.ShouldReload(slot0))
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null)
                continue;

            die.PlayFaceEnchantPopup(die.GetCurrentFaceEnchant());
            played = true;
        }

        return played;
    }

    public static void ApplyCommittedReloadFaceEnchants(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        TurnManager turnManager)
    {
        if (diceRig == null || plan == null)
            return;

        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!plan.ShouldReload(slot0))
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null)
                continue;

            die.SetFaceBroken(plan.committedFaceIndices[slot0], true);

            if (turnManager != null)
            {
                turnManager.RestoreDieToAvailableThisTurn(die);
                die.onRollComplete -= turnManager.RefreshPlanningAfterDiceAvailabilityChanged;
                die.onRollComplete += turnManager.RefreshPlanningAfterDiceAvailabilityChanged;
            }
            die.RollRandomFace();
        }

        diceRig.RefreshRollInfoCache();
    }

    private static bool PlayPostSkillPopupForEffectiveEnchant(
        DiceSlotRig diceRig,
        CommittedFaceUsePlan plan,
        DiceFaceEnchantKind effectiveEnchant,
        string effectText)
    {
        if (diceRig == null || plan == null)
            return false;

        bool played = false;
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!plan.IsSelected(slot0))
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null)
                continue;

            DiceFaceEnchantKind effective = diceRig.GetEffectiveCurrentFaceEnchant(slot0);
            if (effective != effectiveEnchant)
                continue;

            die.PlayFaceEnchantPopup(die.GetCurrentFaceEnchant(), effectText);
            played = true;
        }

        return played;
    }

    private static int ComputePreviewResolvedDieValue(
        DiceSlotRig diceRig,
        CombatActor owner,
        int slot0,
        ElementType skillElement,
        CommittedFaceUsePlan plan)
    {
        return GetCommittedPreviewResolvedBreakdown(diceRig, owner, slot0, skillElement, plan).resolvedValue;
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

    public static bool ApplyVictoryWholeDieEffects(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return false;

        bool changed = false;
        HashSet<DiceSpinnerGeneric> visited = new HashSet<DiceSpinnerGeneric>();

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null || !visited.Add(die))
                continue;

            if (!WholeDieStates.TryGetValue(die, out WholeDieCombatState state) || !state.usedThisCombat)
            {
                if (state == null || state.goldMarkedFaces.Count <= 0)
                    continue;
            }

            if (state.goldMarkedFaces.Count > 0)
                changed |= TryGrantGold(state.goldMarkedFaces.Count * DiceFaceEnchantUtility.GuardGoldAmount);

            switch (die.GetWholeDieTag())
            {
                case DiceWholeDieTag.Patina:
                    changed |= TryApplyPatina(die);
                    break;
            }
        }

        return changed;
    }

    private static void MarkGoldFace(DiceSpinnerGeneric die)
    {
        if (die == null || die.LastFaceIndex < 0)
            return;

        if (!WholeDieStates.TryGetValue(die, out WholeDieCombatState state))
        {
            state = new WholeDieCombatState();
            WholeDieStates[die] = state;
        }

        state.goldMarkedFaces.Add(die.LastFaceIndex);
    }

    private static bool TryGrantGold(int amount)
    {
        if (amount <= 0)
            return false;

        RunInventoryManager inventory = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        if (inventory == null)
            return false;

        inventory.AddGold(amount);
        return true;
    }

    private static bool TryApplyPatina(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null || die.faces.Length <= 0)
            return false;

        int minValue = int.MaxValue;
        int maxValue = int.MinValue;
        int minCount = 0;
        int minIndex = -1;

        for (int i = 0; i < die.faces.Length; i++)
        {
            int value = die.faces[i].value;
            if (value < minValue)
            {
                minValue = value;
                minIndex = i;
                minCount = 1;
            }
            else if (value == minValue)
            {
                minCount++;
            }

            if (value > maxValue)
                maxValue = value;
        }

        if (minIndex < 0)
            return false;
        if (minCount != 1)
            return false;
        if (minValue == maxValue)
            return false;

        return die.SetFaceValue(minIndex, minValue + 1);
    }
}
