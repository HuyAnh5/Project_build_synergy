using System.Collections.Generic;
using UnityEngine;

internal static class RunInventoryLoadoutUtility
{
    public static T GetAt<T>(T[] items, int index)
        where T : class
    {
        if (items == null || index < 0 || index >= items.Length)
            return null;
        return items[index];
    }

    public static void Fill<T>(T[] items, List<T> buffer)
        where T : class
    {
        if (items == null || buffer == null)
            return;

        buffer.Clear();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                buffer.Add(items[i]);
        }
    }

    public static int FindFirstEmpty<T>(T[] items)
        where T : class
    {
        if (items == null)
            return -1;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                return i;
        }

        return -1;
    }

    public static bool ContainsReference<T>(T[] items, T item)
        where T : class
    {
        if (items == null || item == null)
            return false;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == item)
                return true;
        }

        return false;
    }

    public static bool TryAddToFirstEmpty<T>(T[] items, T item, out int addedIndex)
        where T : class
    {
        addedIndex = -1;
        if (items == null || item == null || ContainsReference(items, item))
            return false;

        addedIndex = FindFirstEmpty(items);
        if (addedIndex < 0)
            return false;

        items[addedIndex] = item;
        return true;
    }

    public static bool SetAt<T>(T[] items, int index, T item)
    {
        if (items == null || index < 0 || index >= items.Length)
            return false;

        items[index] = item;
        return true;
    }

    public static bool Swap<T>(T[] items, int a, int b)
    {
        if (items == null || a < 0 || a >= items.Length || b < 0 || b >= items.Length || a == b)
            return false;

        T tmp = items[a];
        items[a] = items[b];
        items[b] = tmp;
        return true;
    }

    public static bool RemoveReference<T>(T[] items, T item)
        where T : class
    {
        if (items == null || item == null)
            return false;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != item)
                continue;

            items[i] = null;
            return true;
        }

        return false;
    }

    public static void CopyLayout<T>(T[] destination, T[] source)
    {
        if (destination == null)
            return;

        for (int i = 0; i < destination.Length; i++)
            destination[i] = source != null && i < source.Length ? source[i] : default;
    }

    public static void FillPassivesWithLegacyFallback(
        SkillPassiveSO[] equippedPassives,
        RunInventoryManager.SlotBinding[] ownedSlots,
        List<SkillPassiveSO> buffer)
    {
        if (buffer == null)
            return;

        buffer.Clear();

        bool anyEquipped = false;
        if (equippedPassives != null)
        {
            for (int i = 0; i < equippedPassives.Length; i++)
            {
                SkillPassiveSO passive = equippedPassives[i];
                if (passive == null)
                    continue;

                anyEquipped = true;
                buffer.Add(passive);
            }
        }

        if (anyEquipped || ownedSlots == null)
            return;

        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i].skillAsset is SkillPassiveSO passive && passive != null)
                buffer.Add(passive);
        }
    }

    public static bool HasAnyPassive(SkillPassiveSO[] equippedPassives, RunInventoryManager.SlotBinding[] ownedSlots)
    {
        if (equippedPassives != null)
        {
            for (int i = 0; i < equippedPassives.Length; i++)
            {
                if (equippedPassives[i] != null)
                    return true;
            }
        }

        if (ownedSlots == null)
            return false;

        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i].skillAsset is SkillPassiveSO)
                return true;
        }

        return false;
    }

    public static void SyncDiceRig(DiceSlotRig diceRig, DiceSpinnerGeneric[] equippedDice)
    {
        if (diceRig == null || equippedDice == null)
            return;

        for (int i = 0; i < equippedDice.Length; i++)
        {
            DiceSpinnerGeneric dice = equippedDice[i];
            diceRig.AssignDiceToSlot(i, dice);
            diceRig.SetSlotActive(i, dice != null);
        }
    }

    public static int AddGoldClamped(int currentGold, int amount)
        => Mathf.Max(0, currentGold + amount);

    public static bool TrySpendGold(ref int currentGold, int amount)
    {
        if (amount <= 0)
            return true;
        if (currentGold < amount)
            return false;

        currentGold -= amount;
        return true;
    }
}
