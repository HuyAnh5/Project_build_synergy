using UnityEngine;

public static class RunInventoryBindingUtility
{
    public static void ApplyBindingsToIcons(
        RunInventoryManager inventory,
        RunInventoryManager.SlotBinding[] ownedSlots)
    {
        if (inventory == null) return;

        if (ownedSlots != null)
        {
            for (int i = 0; i < ownedSlots.Length; i++)
                BindIconToSlot(inventory, ownedSlots[i].uiIcon, i);
        }
    }

    public static void PushSlotToIcon(RunInventoryManager.SlotBinding[] ownedSlots, int index)
    {
        RunInventoryManager.SlotBinding binding = ownedSlots[index];
        if (binding == null || binding.uiIcon == null) return;
        binding.uiIcon.Refresh();
    }

    private static void BindIconToSlot(RunInventoryManager inventory, DraggableSkillIcon icon, int index)
    {
        if (!icon) return;
        icon.SetBindToInventory(inventory, index);
        icon.Refresh();
    }
}
