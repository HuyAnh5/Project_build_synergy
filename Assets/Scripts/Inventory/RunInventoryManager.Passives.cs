using System.Collections.Generic;

public partial class RunInventoryManager
{
    public void FillPassives(List<SkillPassiveSO> buffer)
    {
        if (buffer == null)
            return;

        buffer.Clear();
        if (ownedSlots == null)
            return;

        for (int i = 0; i < ownedSlots.Length; i++)
        {
            SlotBinding slot = ownedSlots[i];
            SkillPassiveSO passive = slot != null ? slot.skillAsset as SkillPassiveSO : null;
            if (passive != null && !buffer.Contains(passive))
                buffer.Add(passive);
        }
    }

    public bool RemoveOwnedPassive(SkillPassiveSO passive)
    {
        if (passive == null || ownedSlots == null)
            return false;

        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i] == null || ownedSlots[i].skillAsset != passive)
                continue;

            ownedSlots[i].skillAsset = null;
            RunInventoryBindingUtility.PushSlotToIcon(ownedSlots, i);
            InventoryChanged?.Invoke();
            return true;
        }

        return false;
    }
}
