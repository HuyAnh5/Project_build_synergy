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

    public static SkillPassiveSO GetPassiveAt(RunInventoryManager.PassiveSlotBinding[] passiveSlots, int index)
    {
        if (passiveSlots == null || index < 0 || index >= passiveSlots.Length)
            return null;
        return passiveSlots[index]?.passiveAsset;
    }

    public static void FillPassiveAssets(RunInventoryManager.PassiveSlotBinding[] passiveSlots, List<SkillPassiveSO> buffer)
    {
        if (passiveSlots == null || buffer == null)
            return;

        buffer.Clear();
        for (int i = 0; i < passiveSlots.Length; i++)
        {
            SkillPassiveSO passive = passiveSlots[i]?.passiveAsset;
            if (passive != null)
                buffer.Add(passive);
        }
    }

    public static int FindFirstEmptyPassiveSlot(RunInventoryManager.PassiveSlotBinding[] passiveSlots)
    {
        if (passiveSlots == null)
            return -1;

        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i] == null || passiveSlots[i].passiveAsset == null)
                return i;
        }

        return -1;
    }

    public static bool TryAddPassiveToFirstEmptySlot(RunInventoryManager.PassiveSlotBinding[] passiveSlots, SkillPassiveSO passive, out int addedIndex)
    {
        addedIndex = -1;
        if (passiveSlots == null || passive == null || ContainsPassiveReference(passiveSlots, passive))
            return false;

        addedIndex = FindFirstEmptyPassiveSlot(passiveSlots);
        if (addedIndex < 0)
            return false;

        if (passiveSlots[addedIndex] == null)
            passiveSlots[addedIndex] = new RunInventoryManager.PassiveSlotBinding();

        passiveSlots[addedIndex].passiveAsset = passive;
        return true;
    }

    public static bool SetPassiveAt(RunInventoryManager.PassiveSlotBinding[] passiveSlots, int index, SkillPassiveSO passive)
    {
        if (passiveSlots == null || index < 0 || index >= passiveSlots.Length)
            return false;

        if (passiveSlots[index] == null)
            passiveSlots[index] = new RunInventoryManager.PassiveSlotBinding();

        passiveSlots[index].passiveAsset = passive;
        return true;
    }

    public static bool SwapPassiveSlots(RunInventoryManager.PassiveSlotBinding[] passiveSlots, int a, int b)
    {
        if (passiveSlots == null || a < 0 || a >= passiveSlots.Length || b < 0 || b >= passiveSlots.Length || a == b)
            return false;

        if (passiveSlots[a] == null) passiveSlots[a] = new RunInventoryManager.PassiveSlotBinding();
        if (passiveSlots[b] == null) passiveSlots[b] = new RunInventoryManager.PassiveSlotBinding();

        SkillPassiveSO tmp = passiveSlots[a].passiveAsset;
        passiveSlots[a].passiveAsset = passiveSlots[b].passiveAsset;
        passiveSlots[b].passiveAsset = tmp;
        return true;
    }

    public static bool RemovePassiveReference(RunInventoryManager.PassiveSlotBinding[] passiveSlots, SkillPassiveSO passive, out int removedIndex)
    {
        removedIndex = -1;
        if (passiveSlots == null || passive == null)
            return false;

        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i]?.passiveAsset != passive)
                continue;

            passiveSlots[i].passiveAsset = null;
            removedIndex = i;
            return true;
        }

        return false;
    }

    public static void CopyPassiveLayout(RunInventoryManager.PassiveSlotBinding[] passiveSlots, SkillPassiveSO[] source)
    {
        if (passiveSlots == null)
            return;

        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i] == null)
                passiveSlots[i] = new RunInventoryManager.PassiveSlotBinding();
            passiveSlots[i].passiveAsset = source != null && i < source.Length ? source[i] : null;
        }
    }

    public static bool ContainsPassiveReference(RunInventoryManager.PassiveSlotBinding[] passiveSlots, SkillPassiveSO passive)
    {
        if (passiveSlots == null || passive == null)
            return false;

        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i]?.passiveAsset == passive)
                return true;
        }

        return false;
    }

    public static bool HasAnyPassive(RunInventoryManager.PassiveSlotBinding[] passiveSlots)
    {
        if (passiveSlots == null)
            return false;

        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i]?.passiveAsset != null)
                return true;
        }

        return false;
    }

    public static void SyncDiceRig(DiceSlotRig diceRig, DiceSpinnerGeneric[] equippedDice)
    {
        if (diceRig == null || equippedDice == null)
            return;

        diceRig.ApplyDiceLayout(equippedDice);

        for (int i = 0; i < equippedDice.Length; i++)
        {
            DiceSpinnerGeneric dice = equippedDice[i];
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
