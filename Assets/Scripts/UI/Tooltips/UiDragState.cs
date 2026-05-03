using System;
using System.Collections.Generic;

public static class UiDragState
{
    private static readonly HashSet<object> ActiveDragOwners = new HashSet<object>();

    public static bool IsDragging => ActiveDragOwners.Count > 0;
    public static event Action DragStateChanged;

    public static void BeginDrag(object owner)
    {
        if (owner == null)
            return;

        bool wasDragging = IsDragging;
        ActiveDragOwners.Add(owner);
        if (wasDragging != IsDragging)
            DragStateChanged?.Invoke();
    }

    public static void EndDrag(object owner)
    {
        if (owner == null)
            return;

        bool wasDragging = IsDragging;
        ActiveDragOwners.Remove(owner);
        if (wasDragging != IsDragging)
            DragStateChanged?.Invoke();
    }
}
