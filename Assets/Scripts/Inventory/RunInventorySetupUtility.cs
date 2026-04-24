using UnityEngine;

public static class RunInventorySetupUtility
{
    public static void EnsureSizes(
        ref RunInventoryManager.SlotBinding[] fixedSlots,
        ref RunInventoryManager.SlotBinding[] ownedSlots,
        ref RunInventoryManager.ConsumableSlot[] consumableSlots,
        ref DiceSpinnerGeneric[] equippedDicePrefabs,
        ref DiceSpinnerGeneric[] equippedDice,
        ref RunInventoryManager.PassiveSlotBinding[] passiveSlots)
    {
        if (fixedSlots == null || fixedSlots.Length != RunInventoryManager.FIXED_SKILL_COUNT)
            fixedSlots = new RunInventoryManager.SlotBinding[RunInventoryManager.FIXED_SKILL_COUNT];

        if (ownedSlots == null || ownedSlots.Length != RunInventoryManager.OWNED_SKILL_COUNT)
            ownedSlots = new RunInventoryManager.SlotBinding[RunInventoryManager.OWNED_SKILL_COUNT];

        for (int i = 0; i < RunInventoryManager.FIXED_SKILL_COUNT; i++)
            if (fixedSlots[i] == null) fixedSlots[i] = new RunInventoryManager.SlotBinding();

        for (int i = 0; i < RunInventoryManager.OWNED_SKILL_COUNT; i++)
            if (ownedSlots[i] == null) ownedSlots[i] = new RunInventoryManager.SlotBinding();

        if (consumableSlots == null || consumableSlots.Length != RunInventoryManager.RELIC_SLOT_COUNT)
            consumableSlots = new RunInventoryManager.ConsumableSlot[RunInventoryManager.RELIC_SLOT_COUNT];

        if (equippedDicePrefabs == null || equippedDicePrefabs.Length != RunInventoryManager.EQUIPPED_DICE_COUNT)
            equippedDicePrefabs = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];

        if (equippedDice == null || equippedDice.Length != RunInventoryManager.EQUIPPED_DICE_COUNT)
            equippedDice = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];

        if (passiveSlots == null || passiveSlots.Length != RunInventoryManager.PASSIVE_SLOT_COUNT)
            passiveSlots = new RunInventoryManager.PassiveSlotBinding[RunInventoryManager.PASSIVE_SLOT_COUNT];

        for (int i = 0; i < RunInventoryManager.PASSIVE_SLOT_COUNT; i++)
            if (passiveSlots[i] == null) passiveSlots[i] = new RunInventoryManager.PassiveSlotBinding();
    }

    public static void BootstrapEquippedDiceFromRigIfNeeded(DiceSlotRig diceRig, DiceSpinnerGeneric[] equippedDicePrefabs, DiceSpinnerGeneric[] equippedDice)
    {
        if (diceRig == null || equippedDice == null) return;

        if (equippedDicePrefabs != null)
        {
            for (int i = 0; i < equippedDicePrefabs.Length; i++)
            {
                if (equippedDicePrefabs[i] != null)
                    return;
            }
        }

        bool anyAssigned = false;
        for (int i = 0; i < RunInventoryManager.EQUIPPED_DICE_COUNT; i++)
        {
            if (equippedDice[i] != null)
            {
                anyAssigned = true;
                break;
            }
        }

        if (anyAssigned) return;
        if (diceRig.slots == null) return;

        for (int i = 0; i < RunInventoryManager.EQUIPPED_DICE_COUNT && i < diceRig.slots.Length; i++)
            equippedDice[i] = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
    }
}
