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
}
