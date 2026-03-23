using UnityEngine;

public static class RunInventoryBindingUtility
{
    public static void ApplyBindingsToIcons(
        RunInventoryManager inventory,
        RunInventoryManager.SlotBinding[] fixedSlots,
        RunInventoryManager.SlotBinding[] ownedSlots,
        RunInventoryManager.PassiveSlotBinding[] passiveSlots)
    {
        if (inventory == null) return;

        if (fixedSlots != null)
        {
            for (int i = 0; i < fixedSlots.Length; i++)
                BindIconToSlot(inventory, fixedSlots[i].uiIcon, isFixed: true, index: i);
        }

        if (ownedSlots != null)
        {
            for (int i = 0; i < ownedSlots.Length; i++)
                BindIconToSlot(inventory, ownedSlots[i].uiIcon, isFixed: false, index: i);
        }

        PushAllPassiveSlotsToIcons(passiveSlots);
    }

    public static void PushSlotToIcon(RunInventoryManager.SlotBinding[] fixedSlots, RunInventoryManager.SlotBinding[] ownedSlots, bool isFixed, int index)
    {
        RunInventoryManager.SlotBinding binding = isFixed ? fixedSlots[index] : ownedSlots[index];
        if (binding == null || binding.uiIcon == null) return;
        binding.uiIcon.Refresh();
    }

    private static void BindIconToSlot(RunInventoryManager inventory, DraggableSkillIcon icon, bool isFixed, int index)
    {
        if (!icon) return;
        icon.SetBindToInventory(inventory, isFixed, index);
        icon.Refresh();
    }

    public static void PushAllPassiveSlotsToIcons(RunInventoryManager.PassiveSlotBinding[] passiveSlots)
    {
        if (passiveSlots == null) return;

        for (int i = 0; i < passiveSlots.Length; i++)
            PushPassiveSlotToIcon(passiveSlots, i);
    }

    public static void PushPassiveSlotToIcon(RunInventoryManager.PassiveSlotBinding[] passiveSlots, int index)
    {
        if (passiveSlots == null || index < 0 || index >= passiveSlots.Length)
            return;

        RunInventoryManager.PassiveSlotBinding binding = passiveSlots[index];
        if (binding == null || binding.uiIcon == null)
            return;

        binding.uiIcon.passive = binding.passiveAsset;
        binding.uiIcon.RefreshVisual();
    }
}
