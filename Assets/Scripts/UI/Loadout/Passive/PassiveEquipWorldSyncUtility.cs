using UnityEngine;

internal static class PassiveEquipWorldSyncUtility
{
    public static void RebindLinkedPassiveOwnersFromCurrentOrder(PassiveDraggableUI[] equipped, PassiveDraggableUI[] linkedPassiveOwners)
    {
        if (linkedPassiveOwners == null)
            return;

        for (int i = 0; i < linkedPassiveOwners.Length; i++)
            linkedPassiveOwners[i] = equipped != null && i < equipped.Length ? equipped[i] : null;
    }

    public static void SyncLinkedPassiveRootsToUI(
        bool mirrorLinkedPassiveRootsWithLiveUI,
        Transform[] linkedPassiveRoots,
        PassiveDraggableUI[] linkedPassiveOwners,
        bool instant,
        Camera uiCamera,
        Camera worldCameraToUse)
    {
        if (!mirrorLinkedPassiveRootsWithLiveUI || linkedPassiveRoots == null || linkedPassiveOwners == null)
            return;
        if (worldCameraToUse == null)
            return;

        for (int i = 0; i < linkedPassiveOwners.Length && i < linkedPassiveRoots.Length; i++)
        {
            Transform linkedRoot = linkedPassiveRoots[i];
            PassiveDraggableUI owner = linkedPassiveOwners[i];
            if (linkedRoot == null || owner == null || !owner.gameObject.activeInHierarchy)
                continue;

            if (!TryGetPassiveUICenterWorldPosition(owner, uiCamera, worldCameraToUse, linkedRoot.position, out Vector3 targetWorld))
                continue;

            if (instant)
                linkedRoot.position = targetWorld;
            else
                linkedRoot.position = Vector3.Lerp(linkedRoot.position, targetWorld, 1f);
        }
    }

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

    private static bool TryGetPassiveUICenterWorldPosition(
        PassiveDraggableUI owner,
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
