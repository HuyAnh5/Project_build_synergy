using System.Collections.Generic;

public partial class RunInventoryManager
{
    public SkillPassiveSO GetEquippedPassive(int index)
    {
        return RunInventoryLoadoutUtility.GetPassiveAt(passiveSlots, index);
    }

    public void FillEquippedPassives(List<SkillPassiveSO> buffer)
    {
        RunInventoryLoadoutUtility.FillPassiveAssets(passiveSlots, buffer);
    }

    public int FindFirstEmptyEquippedPassiveSlot()
    {
        return RunInventoryLoadoutUtility.FindFirstEmptyPassiveSlot(passiveSlots);
    }

    public bool IsPassiveLoadoutFull()
    {
        return FindFirstEmptyEquippedPassiveSlot() < 0;
    }

    public bool TryAddPassiveToFirstEmptySlot(SkillPassiveSO passive)
    {
        if (!RunInventoryLoadoutUtility.TryAddPassiveToFirstEmptySlot(passiveSlots, passive, out int addedIndex))
        {
            return false;
        }

        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, addedIndex);
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetEquippedPassive(int index, SkillPassiveSO passive)
    {
        if (!RunInventoryLoadoutUtility.SetPassiveAt(passiveSlots, index, passive))
        {
            return;
        }

        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, index);
        InventoryChanged?.Invoke();
    }

    public void SwapEquippedPassives(int a, int b)
    {
        if (!RunInventoryLoadoutUtility.SwapPassiveSlots(passiveSlots, a, b))
        {
            return;
        }

        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, a);
        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, b);
        InventoryChanged?.Invoke();
    }

    public void ClearEquippedPassive(int index)
    {
        if (!RunInventoryLoadoutUtility.SetPassiveAt(passiveSlots, index, null))
        {
            return;
        }

        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, index);
        InventoryChanged?.Invoke();
    }

    public bool RemoveEquippedPassive(SkillPassiveSO passive)
    {
        if (!RunInventoryLoadoutUtility.RemovePassiveReference(passiveSlots, passive, out int removedIndex))
        {
            return false;
        }

        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, removedIndex);
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetPassiveLayout(SkillPassiveSO[] equipped)
    {
        EnsureSizes();
        RunInventoryLoadoutUtility.CopyPassiveLayout(passiveSlots, equipped);
        RunInventoryBindingUtility.PushAllPassiveSlotsToIcons(passiveSlots);
        InventoryChanged?.Invoke();
    }

    public void FillPassives(List<SkillPassiveSO> buffer)
    {
        RunInventoryLoadoutUtility.FillPassiveAssets(passiveSlots, buffer);
    }

    public bool HasAnyPassive()
    {
        return RunInventoryLoadoutUtility.HasAnyPassive(passiveSlots);
    }

    public bool ContainsEquippedPassive(SkillPassiveSO passive)
    {
        return RunInventoryLoadoutUtility.ContainsPassiveReference(passiveSlots, passive);
    }
}
