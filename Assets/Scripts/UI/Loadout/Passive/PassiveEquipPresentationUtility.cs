using UnityEngine;

internal static class PassiveEquipPresentationUtility
{
    public static void RefreshSingleSlot(
        PassiveDraggableUI[] equipped,
        RectTransform[] equipSlotAnchors,
        bool instant,
        PassiveEquipUIManager manager)
    {
        if (equipSlotAnchors == null || equipSlotAnchors.Length == 0)
            return;

        for (int i = 0; i < equipSlotAnchors.Length; i++)
        {
            RectTransform anchor = equipSlotAnchors[i];
            if (anchor == null)
                continue;

            bool shouldShow = i == 0;
            if (anchor.gameObject.activeSelf != shouldShow)
                anchor.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            PassiveDraggableUI passive = equipped != null && equipped.Length > 0 ? equipped[0] : null;
            if (passive == null)
                continue;

            manager.Register(passive);
            RectTransform rt = passive.GetComponent<RectTransform>();
            rt.SetParent(anchor, worldPositionStays: false);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            passive.CacheHome();
            passive.RefreshVisual();
        }
    }

    public static void SyncToRunInventory(PassiveDraggableUI[] equipped, RunInventoryManager runInventory)
    {
        if (runInventory == null)
            return;

        SkillPassiveSO[] assets = new SkillPassiveSO[RunInventoryManager.PASSIVE_SLOT_COUNT];
        for (int i = 0; i < assets.Length && i < equipped.Length; i++)
            assets[i] = equipped[i] != null ? equipped[i].passive : null;

        runInventory.SetPassiveLayout(assets);
    }
}
