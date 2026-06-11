using UnityEngine;

public partial class PassiveSystem
{
    private void CaptureKnownDiceFaces(bool refreshTrackedBaseValues = false)
    {
        DiceSlotRig diceRig = GetDiceRig();
        if (diceRig == null || diceRig.slots == null)
            return;

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots != null && i < diceRig.slots.Length && diceRig.slots[i] != null
                ? diceRig.slots[i].dice
                : null;
            if (die == null || die.faces == null)
                continue;

            EnsureDieTracked(die, refreshTrackedBaseValues);
        }
    }

    private void EnsureDieTracked(DiceSpinnerGeneric die, bool refreshBaseValues = false)
    {
        if (die == null || die.faces == null)
            return;

        if (!_baseFaceValues.TryGetValue(die, out int[] baseValues) || baseValues == null || baseValues.Length != die.faces.Length)
        {
            baseValues = new int[die.faces.Length];
            for (int i = 0; i < die.faces.Length; i++)
                baseValues[i] = die.faces[i].value;
            _baseFaceValues[die] = baseValues;
        }

        if (!_permanentFaceBonuses.TryGetValue(die, out int[] permanentBonuses) || permanentBonuses == null || permanentBonuses.Length != die.faces.Length)
            _permanentFaceBonuses[die] = new int[die.faces.Length];

        if (!_combatAllFaceBonuses.ContainsKey(die))
            _combatAllFaceBonuses[die] = 0;

        if (refreshBaseValues)
            SyncBaseFaceValuesFromCurrent(die);
    }

    public void SyncTrackedBaseFaceValues(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null)
            return;

        EnsureDieTracked(die, refreshBaseValues: true);
        ReapplyTrackedFaceBonuses(die);
    }

    private void SyncBaseFaceValuesFromCurrent(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null || !_baseFaceValues.TryGetValue(die, out int[] baseValues) || baseValues == null)
            return;

        int[] permanentBonuses = _permanentFaceBonuses.TryGetValue(die, out int[] bonuses) ? bonuses : null;
        int combatAdd = _combatAllFaceBonuses.TryGetValue(die, out int add) ? add : 0;

        int count = Mathf.Min(die.faces.Length, baseValues.Length);
        for (int i = 0; i < count; i++)
        {
            int permanentAdd = permanentBonuses != null && i < permanentBonuses.Length ? permanentBonuses[i] : 0;
            baseValues[i] = Mathf.Max(1, die.faces[i].value - permanentAdd - combatAdd);
        }
    }

    private void AddPermanentFaceBonus(DiceSpinnerGeneric die, int faceIndex, int amount)
    {
        if (die == null || die.faces == null || amount == 0)
            return;

        EnsureDieTracked(die);
        int[] permanentBonuses = _permanentFaceBonuses[die];
        if (faceIndex < 0 || faceIndex >= permanentBonuses.Length)
            return;

        permanentBonuses[faceIndex] += amount;
        ReapplyTrackedFaceBonuses(die);
    }

    private void AddCombatAllFacesBonus(DiceSpinnerGeneric die, int amount)
    {
        if (die == null || die.faces == null || amount == 0)
            return;

        EnsureDieTracked(die);
        _combatAllFaceBonuses[die] += amount;
        ReapplyTrackedFaceBonuses(die);
    }

    private void ReapplyAllTrackedFaceBonuses()
    {
        foreach (var pair in _baseFaceValues)
            ReapplyTrackedFaceBonuses(pair.Key);
    }

    private void ReapplyTrackedFaceBonuses(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null || !_baseFaceValues.TryGetValue(die, out int[] baseValues))
            return;

        int[] permanentBonuses = _permanentFaceBonuses.TryGetValue(die, out int[] bonuses) ? bonuses : null;
        int combatAdd = _combatAllFaceBonuses.TryGetValue(die, out int add) ? add : 0;

        for (int i = 0; i < die.faces.Length && i < baseValues.Length; i++)
        {
            DiceFace face = die.faces[i];
            int permanentAdd = permanentBonuses != null && i < permanentBonuses.Length ? permanentBonuses[i] : 0;
            face.value = Mathf.Max(1, baseValues[i] + permanentAdd + combatAdd);
            die.faces[i] = face;
        }

        die.RefreshDisplayedState();
    }
}
