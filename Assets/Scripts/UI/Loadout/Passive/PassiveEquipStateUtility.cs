using UnityEngine;

internal static class PassiveEquipStateUtility
{
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
