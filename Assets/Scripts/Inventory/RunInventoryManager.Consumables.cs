using System.Collections.Generic;
using UnityEngine;

public partial class RunInventoryManager
{
    public ConsumableDataSO GetConsumable(int index)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity)
        {
            return null;
        }

        return consumableSlots[index].asset;
    }

    public int GetConsumableCharges(int index)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity)
        {
            return 0;
        }

        return consumableSlots[index].asset != null ? 1 : 0;
    }

    public int GetConsumableCount()
    {
        EnsureSizes();

        int count = 0;
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i].asset != null)
            {
                count++;
            }
        }

        return count;
    }

    public bool TrySetConsumable(int index, ConsumableDataSO asset, int charges = -1)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity)
        {
            return false;
        }

        if (asset == null)
        {
            return false;
        }

        consumableSlots[index] = new ConsumableSlot { asset = asset, charges = 1 };
        CompactConsumables();
        InventoryChanged?.Invoke();
        return true;
    }

    public int FindFirstEmptyConsumableSlot()
    {
        EnsureSizes();
        for (int i = 0; i < ConsumableCapacity; i++)
        {
            if (consumableSlots[i].asset == null)
            {
                return i;
            }
        }

        return -1;
    }

    public bool TryAddConsumableToFirstEmptySlot(ConsumableDataSO asset, int charges = -1)
    {
        int index = FindFirstEmptyConsumableSlot();
        return index >= 0 && TrySetConsumable(index, asset, charges);
    }

    public bool TrySwapConsumables(int a, int b)
    {
        EnsureSizes();
        if (a < 0 || a >= ConsumableCapacity || b < 0 || b >= ConsumableCapacity)
        {
            return false;
        }

        if (a == b)
        {
            return true;
        }

        ConsumableSlot temp = consumableSlots[a];
        consumableSlots[a] = consumableSlots[b];
        consumableSlots[b] = temp;
        CompactConsumables();
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryMoveConsumable(int fromIndex, int insertIndex)
    {
        EnsureSizes();
        if (fromIndex < 0 || fromIndex >= ConsumableCapacity)
        {
            return false;
        }

        if (consumableSlots[fromIndex].asset == null)
        {
            return false;
        }

        List<ConsumableSlot> ordered = new List<ConsumableSlot>(ConsumableCapacity);
        int compactSourceIndex = -1;
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i].asset == null)
            {
                continue;
            }

            if (i == fromIndex)
            {
                compactSourceIndex = ordered.Count;
            }

            ordered.Add(consumableSlots[i]);
        }

        if (compactSourceIndex < 0)
        {
            return false;
        }

        ConsumableSlot moving = ordered[compactSourceIndex];
        ordered.RemoveAt(compactSourceIndex);
        insertIndex = Mathf.Clamp(insertIndex, 0, ordered.Count);
        ordered.Insert(insertIndex, moving);
        ApplyOrderedConsumables(ordered);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryConsumeConsumableCharge(int index, int amount = 1)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity)
        {
            return false;
        }

        if (amount <= 0)
        {
            return true;
        }

        ConsumableSlot slot = consumableSlots[index];
        if (slot.asset == null)
        {
            return false;
        }

        slot = default;
        consumableSlots[index] = slot;
        CompactConsumables();
        InventoryChanged?.Invoke();
        return true;
    }

    public void ClearConsumable(int index)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity)
        {
            return;
        }

        consumableSlots[index] = default;
        CompactConsumables();
        InventoryChanged?.Invoke();
    }

    public bool TrySetRelic(int index, ScriptableObject asset, int charges)
    {
        return TrySetConsumable(index, asset as ConsumableDataSO, charges);
    }

    public bool TryConsumeRelicCharge(int index, int amount = 1)
    {
        return TryConsumeConsumableCharge(index, amount);
    }

    public void ClearRelic(int index)
    {
        ClearConsumable(index);
    }

    private void CompactConsumables()
    {
        if (consumableSlots == null || consumableSlots.Length == 0)
        {
            return;
        }

        List<ConsumableSlot> ordered = new List<ConsumableSlot>(consumableSlots.Length);
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i].asset != null)
            {
                ordered.Add(consumableSlots[i]);
            }
        }

        ApplyOrderedConsumables(ordered);
    }

    private void ApplyOrderedConsumables(List<ConsumableSlot> ordered)
    {
        if (consumableSlots == null)
        {
            return;
        }

        for (int i = 0; i < consumableSlots.Length; i++)
        {
            consumableSlots[i] = i < ordered.Count ? ordered[i] : default;
        }
    }
}
