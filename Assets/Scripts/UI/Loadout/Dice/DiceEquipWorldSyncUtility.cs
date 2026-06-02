using UnityEngine;

internal static class DiceEquipWorldSyncUtility
{
    private static readonly System.Collections.Generic.Dictionary<Transform, int> ReleasedRoots = new System.Collections.Generic.Dictionary<Transform, int>();

    public static void RefreshDiceRigRollInfosAfterReorder(DiceSlotRig diceRig)
    {
        if (diceRig == null || !diceRig.HasRolledThisTurn || diceRig.IsRolling)
            return;

        // Combat roll state must come from DiceSlotRig's own cache logic only.
        diceRig.RefreshRollInfoCache();
    }

    public static void RebindWorldSlotOwnersFromCurrentOrder(
        DiceDraggableUI[] equipped,
        DiceDraggableUI[] worldSlotOwners,
        Transform[] worldSlotRoots,
        DiceSlotRig diceRig)
    {
        if (worldSlotOwners == null || worldSlotRoots == null)
            return;

        for (int i = 0; i < worldSlotOwners.Length; i++)
        {
            worldSlotOwners[i] = equipped != null && i < equipped.Length ? equipped[i] : null;
            worldSlotRoots[i] = null;

            if (diceRig != null && diceRig.slots != null && i < diceRig.slots.Length && diceRig.slots[i] != null)
            {
                GameObject slotRootGo = diceRig.slots[i].slotRoot;
                if (slotRootGo != null)
                    worldSlotRoots[i] = slotRootGo.transform;
                else if (diceRig.slots[i].diceRoot != null)
                    worldSlotRoots[i] = diceRig.slots[i].diceRoot.transform;
            }
        }
    }

    public static void SyncWorldSlotRootsToUI(
        bool mirrorDiceRigSlotsWithLiveUI,
        DiceSlotRig diceRig,
        DiceDraggableUI[] worldSlotOwners,
        Transform[] worldSlotRoots,
        bool instant,
        Camera uiCamera,
        Camera worldCameraToUse)
    {
        if (!mirrorDiceRigSlotsWithLiveUI || diceRig == null || worldSlotOwners == null || worldSlotRoots == null)
            return;
        if (worldCameraToUse == null)
            return;

        for (int i = 0; i < worldSlotRoots.Length; i++)
        {
            Transform slotRoot = worldSlotRoots[i];
            DiceDraggableUI owner = i < worldSlotOwners.Length ? worldSlotOwners[i] : null;
            if (slotRoot == null || owner == null || !owner.gameObject.activeInHierarchy)
                continue;
            if (IsTemporarilyReleased(slotRoot))
                continue;

            if (!TryGetDiceUICenterWorldPosition(owner, uiCamera, worldCameraToUse, slotRoot.position, out Vector3 targetWorld))
                continue;

            if (instant)
                slotRoot.position = targetWorld;
            else
                slotRoot.position = Vector3.Lerp(slotRoot.position, targetWorld, 1f);
        }
    }

    public static void BeginTemporaryRelease(Transform root)
    {
        if (root == null)
            return;

        if (ReleasedRoots.TryGetValue(root, out int count))
            ReleasedRoots[root] = count + 1;
        else
            ReleasedRoots[root] = 1;
    }

    public static void EndTemporaryRelease(Transform root)
    {
        if (root == null)
            return;

        if (!ReleasedRoots.TryGetValue(root, out int count))
            return;

        if (count <= 1)
            ReleasedRoots.Remove(root);
        else
            ReleasedRoots[root] = count - 1;
    }

    public static bool IsTemporarilyReleased(Transform root)
        => root != null && ReleasedRoots.ContainsKey(root);

    public static Camera GetUICamera(Canvas rootCanvas)
    {
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && rootCanvas.worldCamera != null)
            return rootCanvas.worldCamera;

        return Camera.main;
    }

    public static Camera GetWorldFollowCamera(Camera explicitWorldFollowCamera, Camera uiCamera)
    {
        if (explicitWorldFollowCamera != null)
            return explicitWorldFollowCamera;
        if (uiCamera != null)
            return uiCamera;

        return Camera.main;
    }

    private static bool TryGetDiceUICenterWorldPosition(
        DiceDraggableUI owner,
        Camera uiCamera,
        Camera worldCameraToUse,
        Vector3 currentWorld,
        out Vector3 targetWorld)
    {
        targetWorld = currentWorld;
        if (owner == null)
            return false;

        RectTransform rt = owner.GetComponent<RectTransform>();
        if (rt == null)
            return false;

        Vector3 uiWorldCenter = rt.TransformPoint(rt.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, uiWorldCenter);

        float depth = Vector3.Dot(currentWorld - worldCameraToUse.transform.position, worldCameraToUse.transform.forward);
        if (depth <= 0.001f)
            depth = Mathf.Abs(worldCameraToUse.transform.InverseTransformPoint(currentWorld).z);
        if (depth <= 0.001f)
            depth = 10f;

        targetWorld = worldCameraToUse.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        return true;
    }
}
