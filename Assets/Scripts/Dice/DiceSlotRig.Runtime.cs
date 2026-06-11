using UnityEngine;

public partial class DiceSlotRig
{
    private void CacheRollInfos()
    {
        EnsureSlots();
        for (int i = 0; i < 3; i++)
        {
            CacheRollInfoForSlot(i);
        }
    }

    private void CacheRollInfoForSlot(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= LastRollInfos.Length)
            return;

        if (!IsSlotActive(slot0))
        {
            LastRollInfos[slot0] = default;
            return;
        }

        DiceSpinnerGeneric d = GetDice(slot0);
        if (d == null)
        {
            LastRollInfos[slot0] = default;
            return;
        }

        d.GetRollExtents(out int minFace, out int maxFace);
        int rolled = d.GetDisplayedRolledValue();
        DiceFaceEnchantKind faceEnchant = d.GetCurrentFaceEnchant();
        DiceFaceEnchantKind effectiveEnchant = GetEffectiveCurrentFaceEnchant(slot0);
        bool isBrokenFace = d.IsCurrentFaceBroken();
        bool isNumericFace = !isBrokenFace && DiceFaceEnchantUtility.IsNumericFace(faceEnchant);
        bool isUsable = !isBrokenFace;

        int outputBaseValue = faceEnchant == DiceFaceEnchantKind.Stone
            ? 0
            : rolled;
        int critFailValue = rolled;
        bool isCrit = isUsable && isNumericFace && (d.IsCritValue(critFailValue) || DiceFaceEnchantUtility.CountsAsCritForConditions(faceEnchant));
        bool isFail = isUsable && isNumericFace && (d.IsFailValue(critFailValue) || DiceFaceEnchantUtility.CountsAsFailForConditions(faceEnchant));
        bool grantsCritBonus = isCrit && !DiceFaceEnchantUtility.SuppressesCritBonus(faceEnchant);
        bool appliesFailPenalty = isFail && !DiceFaceEnchantUtility.SuppressesFailPenalty(faceEnchant);

        int genericAdded = 0;
        if (grantsCritBonus) genericAdded = FloorScaled(outputBaseValue, GenericCritPercent);
        genericAdded += d.GetCurrentPhaseValueModifier();
        genericAdded += DiceFaceEnchantUtility.GetOnUseAddedValue(effectiveEnchant);

        int genericResolved = isUsable ? outputBaseValue + genericAdded : 0;
        if (genericResolved < 1) genericResolved = 1;
        if (!isUsable) genericResolved = 0;

        LastRollInfos[slot0] = new RollInfo
        {
            rolledValue = rolled,
            minFaceAtRoll = minFace,
            maxFaceAtRoll = maxFace,
            faceEnchant = faceEnchant,
            isCrit = isCrit,
            isFail = isFail,
            grantsCritBonus = grantsCritBonus,
            appliesFailPenalty = appliesFailPenalty,
            isNumericFace = isNumericFace,
            isBrokenFace = isBrokenFace,
            isUsable = isUsable,
            genericAddedValue = genericAdded,
            genericResolvedValue = genericResolved
        };
    }

    private void ClearRollInfos()
    {
        EnsureSlots();
        for (int i = 0; i < LastRollInfos.Length; i++)
            LastRollInfos[i] = default;
    }

    private void BindDieRollCallbacks()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null)
                continue;

            die.onRollComplete -= HandleDieRollComplete;
            die.onRollComplete += HandleDieRollComplete;
        }
    }

    private void HandleDieRollComplete(DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        int slot0 = FindSlotIndex(die);
        if (slot0 < 0)
            return;

        CacheRollInfoForSlot(slot0);

        RollInfo info = LastRollInfos[slot0];
        die.SetCombatRollFeedback(info.isCrit, info.isFail);
    }

    private int FindSlotIndex(DiceSpinnerGeneric die)
    {
        if (die == null || slots == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].dice == die)
                return i;
        }

        return -1;
    }

    private void ClearCombatRollFeedback()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die != null)
                die.SetCombatRollFeedback(false, false);
        }
    }

    public bool HasAnySkillAffectingRollFeedbackThisTurn()
    {
        EnsureSlots();
        if (!HasRolledThisTurn)
            return false;

        for (int i = 0; i < LastRollInfos.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            RollInfo info = LastRollInfos[i];
            if (info.isCrit || info.isFail || info.faceEnchant != DiceFaceEnchantKind.None || info.isBrokenFace)
                return true;
        }

        return false;
    }

    public DiceSpinnerGeneric GetDice(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return null;
        return slots[slot0] != null ? slots[slot0].dice : null;
    }

    private static int FindPreviousSlotIndex(DiceSpinnerGeneric[] previousDice, DiceSpinnerGeneric target)
    {
        if (previousDice == null || target == null)
            return -1;

        for (int i = 0; i < previousDice.Length; i++)
        {
            if (previousDice[i] == target)
                return i;
        }

        return -1;
    }

    private static bool IsRootAlreadyUsed(GameObject[] previousRoots, bool[] used, GameObject candidate)
    {
        if (previousRoots == null || used == null || candidate == null)
            return false;

        for (int i = 0; i < previousRoots.Length && i < used.Length; i++)
        {
            if (previousRoots[i] == candidate)
                return used[i];
        }

        return false;
    }

    private static GameObject TakeFirstUnusedRoot(GameObject[] previousRoots, bool[] used)
    {
        if (previousRoots == null || used == null)
            return null;

        for (int i = 0; i < previousRoots.Length && i < used.Length; i++)
        {
            if (previousRoots[i] == null || used[i])
                continue;

            used[i] = true;
            return previousRoots[i];
        }

        return null;
    }

    private void EnsureSlots()
    {
        if (slots == null || slots.Length != 3)
            slots = new Entry[3];

        for (int i = 0; i < 3; i++)
            if (slots[i] == null)
                slots[i] = new Entry();

        if (LastRollInfos == null || LastRollInfos.Length != 3)
            LastRollInfos = new RollInfo[3];
    }
}
