using UnityEngine;

internal static class DiceEquipWorldSyncUtility
{
    public static void RefreshDiceRigRollInfosAfterReorder(DiceSlotRig diceRig)
    {
        if (diceRig == null || !diceRig.HasRolledThisTurn || diceRig.IsRolling)
            return;
        if (diceRig.LastRollInfos == null || diceRig.LastRollInfos.Length < 3)
            return;

        for (int i = 0; i < 3; i++)
        {
            if (!diceRig.IsSlotActive(i))
            {
                diceRig.LastRollInfos[i] = default;
                continue;
            }

            DiceSpinnerGeneric d = (diceRig.slots != null && i < diceRig.slots.Length && diceRig.slots[i] != null)
                ? diceRig.slots[i].dice
                : null;

            if (d == null)
            {
                diceRig.LastRollInfos[i] = default;
                continue;
            }

            d.GetRollExtents(out int minFace, out int maxFace);
            int rolled = d.GetDisplayedRolledValue();
            DiceFaceEnchantKind faceEnchant = d.GetCurrentFaceEnchant();
            bool isCrit = d.IsCritValue(rolled) || DiceFaceEnchantUtility.CountsAsCritForConditions(faceEnchant);
            bool isFail = d.IsFailValue(rolled) || DiceFaceEnchantUtility.CountsAsFailForConditions(faceEnchant);
            bool grantsCritBonus = isCrit && !DiceFaceEnchantUtility.SuppressesCritBonus(faceEnchant);
            bool appliesFailPenalty = isFail && !DiceFaceEnchantUtility.SuppressesFailPenalty(faceEnchant);
            bool isNumericFace = DiceFaceEnchantUtility.IsNumericFace(faceEnchant);

            int genericAdded = 0;
            if (grantsCritBonus) genericAdded = Mathf.FloorToInt(rolled * DiceSlotRig.GenericCritPercent);
            genericAdded += DiceFaceEnchantUtility.GetFlatAddedValue(faceEnchant);

            int genericResolved = rolled + genericAdded;
            if (genericResolved < 1)
                genericResolved = 1;

            diceRig.LastRollInfos[i] = new DiceSlotRig.RollInfo
            {
                rolledValue = rolled,
                minFaceAtRoll = minFace,
                maxFaceAtRoll = maxFace,
                faceEnchant = faceEnchant,
                isCrit = isCrit,
                isFail = isFail,
                grantsCritBonus = grantsCritBonus,
                appliesFailPenalty = appliesFailPenalty,
                isNumericFace = isNumericFace,
                genericAddedValue = genericAdded,
                genericResolvedValue = genericResolved,
            };
        }
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

            if (!TryGetDiceUICenterWorldPosition(owner, uiCamera, worldCameraToUse, slotRoot.position, out Vector3 targetWorld))
                continue;

            if (instant)
                slotRoot.position = targetWorld;
            else
                slotRoot.position = Vector3.Lerp(slotRoot.position, targetWorld, 1f);
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
