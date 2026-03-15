using DG.Tweening;
using UnityEngine;

internal static class DiceEquipLayoutUtility
{
    public static int CountOccupied(DiceDraggableUI[] equipped)
    {
        if (equipped == null)
            return 0;

        int count = 0;
        for (int i = 0; i < equipped.Length; i++)
        {
            if (equipped[i] != null)
                count++;
        }

        return count;
    }

    public static int FindIndex(DiceDraggableUI[] equipped, DiceDraggableUI dice)
    {
        if (equipped == null || dice == null)
            return -1;

        for (int i = 0; i < equipped.Length; i++)
        {
            if (equipped[i] == dice)
                return i;
        }

        return -1;
    }

    public static void BuildDisplayedOrder(
        DiceDraggableUI[] equipped,
        DiceDraggableUI[] buffer,
        DiceDraggableUI draggingDice,
        int dragSourceIndex,
        int previewInsertIndex)
    {
        if (buffer == null)
            return;

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = null;

        if (equipped == null)
            return;

        if (draggingDice == null || dragSourceIndex < 0 || previewInsertIndex < 0)
        {
            for (int i = 0; i < equipped.Length && i < buffer.Length; i++)
                buffer[i] = equipped[i];
            return;
        }

        int count = CountOccupied(equipped);
        int insertIndex = Mathf.Clamp(previewInsertIndex, 0, Mathf.Max(0, count - 1));
        int write = 0;
        bool inserted = false;

        for (int i = 0; i < equipped.Length; i++)
        {
            DiceDraggableUI current = equipped[i];
            if (current == null || current == draggingDice)
                continue;

            if (!inserted && write == insertIndex)
            {
                buffer[write++] = draggingDice;
                inserted = true;
            }

            if (write < buffer.Length)
                buffer[write++] = current;
        }

        if (!inserted && write < buffer.Length)
            buffer[write] = draggingDice;
    }

    public static void BuildDisplayedCombatSlotOrder(
        RectTransform[] linkedCombatSlotAnchors,
        RectTransform[] buffer,
        RectTransform draggingCombatSlot,
        int dragSourceIndex,
        int previewInsertIndex,
        int equippedCount)
    {
        if (buffer == null)
            return;

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = null;

        if (linkedCombatSlotAnchors == null)
            return;

        if (draggingCombatSlot == null || dragSourceIndex < 0 || previewInsertIndex < 0)
        {
            for (int i = 0; i < linkedCombatSlotAnchors.Length && i < buffer.Length; i++)
                buffer[i] = linkedCombatSlotAnchors[i];
            return;
        }

        int count = Mathf.Min(equippedCount, linkedCombatSlotAnchors.Length);
        int insertIndex = Mathf.Clamp(previewInsertIndex, 0, Mathf.Max(0, count - 1));
        int write = 0;
        bool inserted = false;

        for (int i = 0; i < count; i++)
        {
            RectTransform current = linkedCombatSlotAnchors[i];
            if (current == null || current == draggingCombatSlot)
                continue;

            if (!inserted && write == insertIndex)
            {
                buffer[write++] = draggingCombatSlot;
                inserted = true;
            }

            if (write < buffer.Length)
                buffer[write++] = current;
        }

        if (!inserted && write < buffer.Length)
            buffer[write] = draggingCombatSlot;
    }

    public static int GetInsertIndexFromScreenPosition(
        DiceDraggableUI[] equipped,
        int dragSourceIndex,
        RectTransform[] equipSlotAnchors,
        Transform fallbackTransform,
        bool useAdaptiveCenterLayout,
        float pairHalfSpacing,
        float trioSideOffset,
        float rowY,
        Vector2 screenPosition,
        Camera eventCamera)
    {
        int count = CountOccupied(equipped);
        if (count <= 1)
            return 0;

        RectTransform reference = GetAnchorReferenceRect(equipSlotAnchors, fallbackTransform);
        if (reference == null)
            return Mathf.Clamp(dragSourceIndex, 0, Mathf.Max(0, count - 1));

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, screenPosition, eventCamera, out Vector2 local))
            return Mathf.Clamp(dragSourceIndex, 0, Mathf.Max(0, count - 1));

        Vector2[] positions = BuildAdaptivePositions(count, rowY, useAdaptiveCenterLayout, pairHalfSpacing, trioSideOffset);
        float x = local.x;

        for (int i = 0; i < count - 1; i++)
        {
            float midpoint = (positions[i].x + positions[i + 1].x) * 0.5f;
            if (x < midpoint)
                return i;
        }

        return count - 1;
    }

    public static Vector2[] BuildAdaptivePositions(
        int count,
        float targetY,
        bool useAdaptiveCenterLayout,
        float pairHalfSpacing,
        float trioSideOffset)
    {
        Vector2[] positions = new Vector2[3]
        {
            new Vector2(-trioSideOffset, targetY),
            new Vector2(0f, targetY),
            new Vector2(trioSideOffset, targetY)
        };

        if (!useAdaptiveCenterLayout || count <= 0)
            return positions;

        if (count == 1)
        {
            positions[0] = new Vector2(0f, targetY);
        }
        else if (count == 2)
        {
            positions[0] = new Vector2(-pairHalfSpacing, targetY);
            positions[1] = new Vector2(pairHalfSpacing, targetY);
        }

        return positions;
    }

    public static void ApplyPositionsToAnchors(
        RectTransform[] anchors,
        Vector2[] positions,
        int occupiedCount,
        bool hideEmpty,
        float xOffset,
        bool preserveAnchorY,
        float[] preservedY,
        float duration,
        Ease ease,
        bool instant)
    {
        if (anchors == null || anchors.Length == 0 || positions == null || positions.Length == 0)
            return;

        for (int i = 0; i < anchors.Length; i++)
        {
            RectTransform anchor = anchors[i];
            if (anchor == null)
                continue;

            bool occupied = i < occupiedCount;
            if (!anchor.gameObject.activeSelf && (!hideEmpty || occupied))
                anchor.gameObject.SetActive(true);

            Vector2 pos = positions[Mathf.Clamp(i, 0, positions.Length - 1)];
            float y = preserveAnchorY && preservedY != null && i < preservedY.Length ? preservedY[i] : pos.y;
            Vector2 target = new Vector2(pos.x + xOffset, y);

            anchor.DOKill();
            if (instant || duration <= 0f)
                anchor.anchoredPosition = target;
            else
                anchor.DOAnchorPos(target, duration).SetEase(ease).SetUpdate(true);

            anchor.gameObject.SetActive(!hideEmpty || occupied);
        }
    }

    public static float GetCombatBaseY(RectTransform slot, RectTransform[] combatSlotIdentity, float[] combatSlotBaseY)
    {
        if (slot == null)
            return 0f;

        if (combatSlotIdentity != null && combatSlotBaseY != null)
        {
            for (int i = 0; i < combatSlotIdentity.Length && i < combatSlotBaseY.Length; i++)
            {
                if (combatSlotIdentity[i] == slot)
                    return combatSlotBaseY[i];
            }
        }

        return slot.anchoredPosition.y;
    }

    public static void MoveCombatSlot(RectTransform slot, Vector2 target, bool instant, float duration, Ease ease)
    {
        if (slot == null)
            return;

        slot.DOKill();
        if (instant || duration <= 0f)
        {
            slot.anchoredPosition = target;
            return;
        }

        slot.DOAnchorPos(target, duration).SetEase(ease).SetUpdate(true);
    }

    private static RectTransform GetAnchorReferenceRect(RectTransform[] equipSlotAnchors, Transform fallbackTransform)
    {
        if (equipSlotAnchors != null)
        {
            for (int i = 0; i < equipSlotAnchors.Length; i++)
            {
                RectTransform anchor = equipSlotAnchors[i];
                if (anchor == null)
                    continue;

                RectTransform parent = anchor.parent as RectTransform;
                if (parent != null)
                    return parent;

                return anchor;
            }
        }

        return fallbackTransform as RectTransform;
    }
}
