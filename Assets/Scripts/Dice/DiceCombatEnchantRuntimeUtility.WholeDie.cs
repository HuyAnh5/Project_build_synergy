using System.Collections.Generic;
using UnityEngine;

public static partial class DiceCombatEnchantRuntimeUtility
{
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

        RunInventoryManager inventory = RunInventoryManagerRegistry.Get();
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
