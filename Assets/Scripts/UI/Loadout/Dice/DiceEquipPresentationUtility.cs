using DG.Tweening;
using UnityEngine;

internal static class DiceEquipPresentationUtility
{
    public static void CaptureCombatSlotBaseY(RectTransform[] linkedCombatSlotAnchors, RectTransform[] combatSlotIdentity, float[] combatSlotBaseY, ref bool capturedCombatSlotY)
    {
        if (capturedCombatSlotY || linkedCombatSlotAnchors == null)
            return;

        for (int i = 0; i < linkedCombatSlotAnchors.Length && i < combatSlotBaseY.Length; i++)
        {
            RectTransform rt = linkedCombatSlotAnchors[i];
            combatSlotIdentity[i] = rt;
            combatSlotBaseY[i] = rt != null ? rt.anchoredPosition.y : 0f;
        }

        capturedCombatSlotY = true;
    }

    public static void RefreshCombatSlotPreview(
        bool mirrorAdaptiveLayoutToCombatSlots,
        RectTransform[] linkedCombatSlotAnchors,
        RectTransform[] combatDisplayBuffer,
        RectTransform draggingCombatSlot,
        int dragSourceIndex,
        int previewInsertIndex,
        int equippedCount,
        bool hideEmptyCombatSlotAnchors,
        float rowY,
        bool useAdaptiveCenterLayout,
        float pairHalfSpacing,
        float trioSideOffset,
        float combatSlotsXOffset,
        RectTransform[] combatSlotIdentity,
        float[] combatSlotBaseY,
        bool instant,
        float combatSlotPreviewDuration,
        Ease combatSlotPreviewEase)
    {
        if (!mirrorAdaptiveLayoutToCombatSlots)
            return;
        if (linkedCombatSlotAnchors == null || linkedCombatSlotAnchors.Length == 0)
            return;

        DiceEquipLayoutUtility.BuildDisplayedCombatSlotOrder(
            linkedCombatSlotAnchors,
            combatDisplayBuffer,
            draggingCombatSlot,
            dragSourceIndex,
            previewInsertIndex,
            equippedCount);

        Vector2[] dicePositions = DiceEquipLayoutUtility.BuildAdaptivePositions(
            equippedCount,
            rowY,
            useAdaptiveCenterLayout,
            pairHalfSpacing,
            trioSideOffset);

        for (int i = 0; i < combatDisplayBuffer.Length; i++)
        {
            RectTransform slot = combatDisplayBuffer[i];
            if (slot == null)
                continue;

            bool occupied = i < equippedCount;
            slot.gameObject.SetActive(!hideEmptyCombatSlotAnchors || occupied);
            if (!occupied)
                continue;

            Vector2 pos = dicePositions[Mathf.Clamp(i, 0, dicePositions.Length - 1)];
            float y = DiceEquipLayoutUtility.GetCombatBaseY(slot, combatSlotIdentity, combatSlotBaseY);
            Vector2 target = new Vector2(pos.x + combatSlotsXOffset, y);

            DiceEquipLayoutUtility.MoveCombatSlot(slot, target, instant, combatSlotPreviewDuration, combatSlotPreviewEase);
        }
    }

    public static void ApplyAdaptiveLayout(
        RectTransform[] equipSlotAnchors,
        RectTransform[] linkedCombatSlotAnchors,
        int equippedCount,
        float rowY,
        bool useAdaptiveCenterLayout,
        float pairHalfSpacing,
        float trioSideOffset,
        bool hideEmptyAnchors,
        bool mirrorAdaptiveLayoutToCombatSlots,
        bool hideEmptyCombatSlotAnchors,
        float combatSlotsXOffset,
        float[] combatSlotBaseY,
        float anchorTweenDuration,
        Ease anchorEase,
        bool instant)
    {
        if (!useAdaptiveCenterLayout)
            return;

        Vector2[] dicePositions = DiceEquipLayoutUtility.BuildAdaptivePositions(
            equippedCount,
            rowY,
            useAdaptiveCenterLayout,
            pairHalfSpacing,
            trioSideOffset);

        DiceEquipLayoutUtility.ApplyPositionsToAnchors(
            equipSlotAnchors,
            dicePositions,
            equippedCount,
            hideEmptyAnchors,
            0f,
            false,
            null,
            anchorTweenDuration,
            anchorEase,
            instant);

        if (!mirrorAdaptiveLayoutToCombatSlots)
            return;

        DiceEquipLayoutUtility.ApplyPositionsToAnchors(
            linkedCombatSlotAnchors,
            dicePositions,
            equippedCount,
            hideEmptyCombatSlotAnchors,
            combatSlotsXOffset,
            true,
            combatSlotBaseY,
            anchorTweenDuration,
            anchorEase,
            instant);
    }

    public static void SyncOutputs(DiceDraggableUI[] equipped, RunInventoryManager runInventory, DiceSlotRig diceRig)
    {
        DiceSpinnerGeneric[] assets = new DiceSpinnerGeneric[3];
        for (int i = 0; i < 3; i++)
            assets[i] = equipped[i] != null ? equipped[i].dice : null;

        if (runInventory != null)
            runInventory.SetDiceLayout(assets);

        if (diceRig == null)
            return;

        for (int i = 0; i < 3; i++)
        {
            diceRig.AssignDiceToSlot(i, assets[i]);
            diceRig.SetSlotActive(i, assets[i] != null);
        }

        DiceEquipWorldSyncUtility.RefreshDiceRigRollInfosAfterReorder(diceRig);
    }
}
