using UnityEngine;

internal static class PassiveEquipPresentationUtility
{
    public static void RefreshAllSlots(
        PassiveDraggableUI[] equipped,
        PassiveDraggableUI[] displayOrderBuffer,
        PassiveDraggableUI draggingPassive,
        int dragSourceIndex,
        int previewInsertIndex,
        RectTransform[] equipSlotAnchors,
        bool instant,
        PassiveEquipUIManager manager)
    {
        PassiveEquipLayoutUtility.BuildDisplayedOrder(
            equipped,
            displayOrderBuffer,
            draggingPassive,
            dragSourceIndex,
            previewInsertIndex);

        for (int i = 0; i < displayOrderBuffer.Length; i++)
        {
            PassiveDraggableUI passive = displayOrderBuffer[i];
            if (passive == null || passive == draggingPassive)
                continue;

            RectTransform anchor = equipSlotAnchors != null && i >= 0 && i < equipSlotAnchors.Length
                ? equipSlotAnchors[i]
                : null;
            if (anchor == null)
            {
                passive.ReturnToCachedHome();
                continue;
            }

            manager.Register(passive);
            if (instant)
            {
                RectTransform rt = passive.GetComponent<RectTransform>();
                rt.SetParent(anchor, worldPositionStays: false);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            else
            {
                passive.SnapToAnchorAnimated(anchor, Vector2.zero);
            }
        }
    }

    public static void ApplyAdaptiveLayout(
        RectTransform[] equipSlotAnchors,
        int equippedCount,
        float rowY,
        bool useAdaptiveCenterLayout,
        float pairHalfSpacing,
        float trioSideOffset,
        bool hideEmptyAnchors,
        float anchorTweenDuration,
        DG.Tweening.Ease anchorEase,
        bool instant)
    {
        if (!useAdaptiveCenterLayout || equipSlotAnchors == null || equipSlotAnchors.Length == 0)
            return;

        Vector2[] positions = PassiveEquipLayoutUtility.BuildAdaptivePositions(
            equippedCount,
            rowY,
            useAdaptiveCenterLayout,
            pairHalfSpacing,
            trioSideOffset);
        PassiveEquipLayoutUtility.ApplyPositionsToAnchors(
            equipSlotAnchors,
            positions,
            equippedCount,
            hideEmptyAnchors,
            anchorTweenDuration,
            anchorEase,
            instant);
    }

    public static void SyncToRunInventory(PassiveDraggableUI[] equipped, RunInventoryManager runInventory)
    {
        if (runInventory == null)
            return;

        SkillPassiveSO[] assets = new SkillPassiveSO[3];
        for (int i = 0; i < assets.Length && i < equipped.Length; i++)
            assets[i] = equipped[i] != null ? equipped[i].passive : null;

        runInventory.SetPassiveLayout(assets);
    }
}
