using UnityEngine;

internal static class PassiveEquipStateUtility
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
            reordered[write] = draggedItem;

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
}
