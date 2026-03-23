using UnityEngine;

internal static class DiceEquipStateUtility
{
    public static T[] InsertDraggedItem<T>(T[] currentOrder, T draggedItem, int insertIndex)
        where T : class
    {
        if (currentOrder == null || currentOrder.Length == 0 || draggedItem == null)
            return currentOrder;

        T[] reordered = new T[currentOrder.Length];
        int write = 0;
        bool inserted = false;

        for (int i = 0; i < currentOrder.Length; i++)
        {
            T current = currentOrder[i];
            if (current == null || ReferenceEquals(current, draggedItem))
                continue;

            if (!inserted && write == insertIndex)
            {
                reordered[write++] = draggedItem;
                inserted = true;
            }

            if (write < reordered.Length)
                reordered[write++] = current;
        }

        if (!inserted && write < reordered.Length)
            reordered[write++] = draggedItem;

        return reordered;
    }

    public static T[] Compact<T>(T[] items)
        where T : class
    {
        if (items == null || items.Length == 0)
            return items;

        T[] compact = new T[items.Length];
        int write = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (write >= compact.Length)
                break;

            compact[write++] = items[i];
        }

        return compact;
    }

    public static T[] ApplyPermutation<T>(T[] currentOrder, int[] permutation)
        where T : class
    {
        if (currentOrder == null || permutation == null || currentOrder.Length != permutation.Length)
            return currentOrder;

        T[] reordered = new T[currentOrder.Length];
        for (int newIndex = 0; newIndex < permutation.Length; newIndex++)
        {
            int oldIndex = permutation[newIndex];
            if (oldIndex < 0 || oldIndex >= currentOrder.Length)
                return currentOrder;

            reordered[newIndex] = currentOrder[oldIndex];
        }

        return reordered;
    }

    public static T[] SwapItems<T>(T[] currentOrder, int a, int b)
        where T : class
    {
        if (currentOrder == null || currentOrder.Length == 0)
            return currentOrder;
        if (a < 0 || a >= currentOrder.Length || b < 0 || b >= currentOrder.Length)
            return currentOrder;
        if (a == b)
            return (T[])currentOrder.Clone();

        T[] swapped = (T[])currentOrder.Clone();
        T temp = swapped[a];
        swapped[a] = swapped[b];
        swapped[b] = temp;
        return swapped;
    }

    public static int[] BuildPermutation<T>(T[] oldOrder, T draggedItem, int insertIndex)
        where T : class
    {
        int slotCount = oldOrder != null ? oldOrder.Length : 0;
        int[] permutation = new int[slotCount];
        for (int i = 0; i < slotCount; i++)
            permutation[i] = i;

        if (oldOrder == null || draggedItem == null)
            return permutation;

        T[] newOrder = InsertDraggedItem((T[])oldOrder.Clone(), draggedItem, insertIndex);
        for (int newIndex = 0; newIndex < slotCount; newIndex++)
        {
            T item = newOrder[newIndex];
            if (item == null)
                continue;

            for (int oldIndex = 0; oldIndex < slotCount; oldIndex++)
            {
                if (ReferenceEquals(oldOrder[oldIndex], item))
                {
                    permutation[newIndex] = oldIndex;
                    break;
                }
            }
        }

        return permutation;
    }

    public static RectTransform[] ReorderCombatSlots(RectTransform[] currentOrder, RectTransform draggedSlot, int insertIndex, int occupiedCount)
    {
        if (currentOrder == null || currentOrder.Length == 0 || draggedSlot == null)
            return currentOrder;

        int count = Mathf.Min(occupiedCount, currentOrder.Length);
        RectTransform[] occupied = new RectTransform[count];
        for (int i = 0; i < count; i++)
            occupied[i] = currentOrder[i];

        RectTransform[] reorderedOccupied = InsertDraggedItem(occupied, draggedSlot, insertIndex);
        RectTransform[] reordered = new RectTransform[currentOrder.Length];

        for (int i = 0; i < reorderedOccupied.Length && i < reordered.Length; i++)
            reordered[i] = reorderedOccupied[i];

        for (int i = count; i < currentOrder.Length; i++)
            reordered[i] = currentOrder[i];

        return reordered;
    }

    public static RectTransform[] MoveCombatSlot(RectTransform[] currentOrder, int fromSlot, int toSlot)
    {
        if (currentOrder == null || currentOrder.Length == 0)
            return currentOrder;
        if (fromSlot < 0 || fromSlot >= currentOrder.Length)
            return currentOrder;
        if (toSlot < 0 || toSlot >= currentOrder.Length)
            return currentOrder;

        RectTransform[] reordered = (RectTransform[])currentOrder.Clone();
        RectTransform moving = reordered[fromSlot];
        RectTransform destination = reordered[toSlot];

        reordered[fromSlot] = destination;
        reordered[toSlot] = moving;
        return Compact(reordered);
    }

    public static RectTransform GetCombatSlotAt(RectTransform[] linkedCombatSlotAnchors, int index)
    {
        if (linkedCombatSlotAnchors == null || index < 0 || index >= linkedCombatSlotAnchors.Length)
            return null;
        return linkedCombatSlotAnchors[index];
    }

    public static void RebindCombatSlotLaneIndices(RectTransform[] linkedCombatSlotAnchors, TurnManager turnManager)
    {
        if (linkedCombatSlotAnchors == null || linkedCombatSlotAnchors.Length == 0)
            return;

        for (int lane0 = 0; lane0 < linkedCombatSlotAnchors.Length; lane0++)
        {
            RectTransform laneRoot = linkedCombatSlotAnchors[lane0];
            if (laneRoot == null)
                continue;

            ActionSlotDrop drop = laneRoot.GetComponent<ActionSlotDrop>();
            if (drop == null)
                drop = laneRoot.GetComponentInChildren<ActionSlotDrop>(true);

            if (drop != null)
            {
                if (drop.turn == null && turnManager != null)
                    drop.turn = turnManager;

                drop.SetVisualLaneIndex(lane0 + 1);
            }

            SlotIconDragToClear clear = laneRoot.GetComponent<SlotIconDragToClear>();
            if (clear == null)
                clear = laneRoot.GetComponentInChildren<SlotIconDragToClear>(true);

            if (clear != null)
            {
                if (clear.turn == null && turnManager != null)
                    clear.turn = turnManager;

                clear.SetVisualLaneIndex(lane0 + 1);
            }
        }
    }
}
