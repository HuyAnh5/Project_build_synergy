using UnityEngine;

internal static class DiceEquipWorldFollowUtility
{
    public static void SyncWorldSlotRootsToUI(
        bool mirrorDiceRigSlotsWithLiveUI,
        DiceSlotRig diceRig,
        DiceDraggableUI[] worldSlotOwners,
        Transform[] worldSlotRoots,
        bool instant,
        ref Canvas rootCanvas,
        Component context,
        Camera explicitWorldFollowCamera)
    {
        if (!mirrorDiceRigSlotsWithLiveUI || diceRig == null)
            return;
        if (worldSlotOwners == null || worldSlotRoots == null)
            return;

        if (worldSlotOwners.Length >= 3 &&
            worldSlotOwners[0] == null &&
            worldSlotOwners[1] == null &&
            worldSlotOwners[2] == null)
        {
            return;
        }

        Camera uiCamera = GetUICamera(ref rootCanvas, context);
        Camera worldCameraToUse = DiceEquipWorldSyncUtility.GetWorldFollowCamera(explicitWorldFollowCamera, uiCamera);

        DiceEquipWorldSyncUtility.SyncWorldSlotRootsToUI(
            mirrorDiceRigSlotsWithLiveUI,
            diceRig,
            worldSlotOwners,
            worldSlotRoots,
            instant,
            uiCamera,
            worldCameraToUse);
    }

    public static Camera GetUICamera(ref Canvas rootCanvas, Component context)
    {
        if (rootCanvas == null && context != null)
            rootCanvas = context.GetComponentInParent<Canvas>();

        return DiceEquipWorldSyncUtility.GetUICamera(rootCanvas);
    }
}
