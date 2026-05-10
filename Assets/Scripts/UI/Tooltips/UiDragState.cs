using System;
using System.Collections.Generic;

public static class UiDragState
{
    private static readonly HashSet<object> ActiveDragOwners = new HashSet<object>();

    public static bool IsDragging => ActiveDragOwners.Count > 0;
    public static event Action DragStateChanged;

    // --- Click-to-Select state ---
    public static DraggableSkillIcon SelectedSkill { get; private set; }
    public static event Action SelectedSkillChanged;

    public static void SelectSkill(DraggableSkillIcon icon)
    {
        if (SelectedSkill == icon) return;
        DraggableSkillIcon prev = SelectedSkill;
        SelectedSkill = icon;
        prev?.OnDeselected();
        SelectedSkill?.OnSelected();
        SelectedSkillChanged?.Invoke();
    }

    public static void DeselectSkill()
    {
        if (SelectedSkill == null) return;
        DraggableSkillIcon prev = SelectedSkill;
        SelectedSkill = null;
        prev?.OnDeselected();
        SelectedSkillChanged?.Invoke();
    }

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
