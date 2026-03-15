using DG.Tweening;
using UnityEngine;

internal static class PassiveEquipLayoutUtility
{
    public static int CountOccupied(PassiveDraggableUI[] equipped)
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

    public static int FindIndex(PassiveDraggableUI[] equipped, PassiveDraggableUI passive)
    {
        if (equipped == null || passive == null)
            return -1;

        for (int i = 0; i < equipped.Length; i++)
        {
            if (equipped[i] == passive)
                return i;
        }

        return -1;
    }

    public static void BuildDisplayedOrder(
        PassiveDraggableUI[] equipped,
        PassiveDraggableUI[] buffer,
        PassiveDraggableUI draggingPassive,
        int dragSourceIndex,
        int previewInsertIndex)
    {
        if (buffer == null)
            return;

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = null;

        if (equipped == null)
            return;

        if (draggingPassive == null || dragSourceIndex < 0 || previewInsertIndex < 0)
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
            PassiveDraggableUI current = equipped[i];
            if (current == null || current == draggingPassive)
                continue;

            if (!inserted && write == insertIndex)
            {
                buffer[write++] = draggingPassive;
                inserted = true;
            }

            if (write < buffer.Length)
                buffer[write++] = current;
        }

        if (!inserted && write < buffer.Length)
            buffer[write] = draggingPassive;
    }

    public static int GetInsertIndexFromScreenPosition(
        PassiveDraggableUI[] equipped,
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

            Vector2 target = positions[Mathf.Clamp(i, 0, positions.Length - 1)];
            anchor.DOKill();
            if (instant || duration <= 0f)
                anchor.anchoredPosition = target;
            else
                anchor.DOAnchorPos(target, duration).SetEase(ease).SetUpdate(true);

            anchor.gameObject.SetActive(!hideEmpty || occupied);
        }
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
