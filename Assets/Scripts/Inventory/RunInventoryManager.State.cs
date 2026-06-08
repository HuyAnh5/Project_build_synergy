using UnityEngine;

public partial class RunInventoryManager
{
    /// <summary>
    /// Captures the current inventory into a serializable snapshot suitable for a run save.
    /// </summary>
    public RunInventoryState CaptureState()
    {
        EnsureSizes();

        RunInventoryState snapshot = new RunInventoryState();
        DiceSpinnerGeneric[] dice = CaptureEquippedDicePrefabLayout();
        ScriptableObject[] skills = CaptureOwnedSkillAssets();
        RunConsumableSlotState[] relics = CaptureConsumableState();

        snapshot.SetDiceState(dice, dice);
        snapshot.SetSkillState(skills, skills);
        snapshot.SetConsumableState(relics, relics);
        return snapshot;
    }

    public void WriteStateTo(RunState state)
    {
        if (state == null)
        {
            return;
        }

        state.SetGold(gold);
        state.SetInventoryState(CaptureState());
    }

    public void ApplyState(RunState state, bool notifyChanged = true)
    {
        if (state == null)
        {
            return;
        }

        ApplyInventoryState(state.InventoryState, notifyChanged: false);
        gold = Mathf.Max(0, state.Gold);

        if (notifyChanged)
        {
            InventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// Applies a captured inventory snapshot back into this runtime inventory.
    /// </summary>
    public void ApplyInventoryState(RunInventoryState snapshot, bool notifyChanged = true)
    {
        if (snapshot == null)
        {
            return;
        }

        EnsureSizes();
        ApplyDiceState(snapshot.EquippedDice);
        ApplyOwnedSkills(snapshot.EquippedSkills);
        ApplyConsumables(HasAnyConsumable(snapshot.RelicSlots) ? snapshot.RelicSlots : snapshot.Consumables);

        RunInventoryBindingUtility.ApplyBindingsToIcons(this, ownedSlots);
        if (notifyChanged)
        {
            InventoryChanged?.Invoke();
        }
    }

    private DiceSpinnerGeneric[] CaptureEquippedDicePrefabLayout()
    {
        DiceSpinnerGeneric[] snapshot = new DiceSpinnerGeneric[EQUIPPED_DICE_COUNT];
        for (int i = 0; i < snapshot.Length; i++)
        {
            DiceSpinnerGeneric prefab = i < equippedDicePrefabs.Length ? equippedDicePrefabs[i] : null;
            if (prefab != null)
            {
                snapshot[i] = prefab;
                continue;
            }

            DiceSpinnerGeneric runtime = i < equippedDice.Length ? equippedDice[i] : null;
            snapshot[i] = ResolveTrackedPrefabForRuntime(runtime) ?? runtime;
        }

        return snapshot;
    }

    private ScriptableObject[] CaptureOwnedSkillAssets()
    {
        ScriptableObject[] snapshot = new ScriptableObject[OWNED_SKILL_COUNT];
        for (int i = 0; i < snapshot.Length; i++)
        {
            snapshot[i] = ownedSlots[i]?.skillAsset;
        }

        return snapshot;
    }

    private RunConsumableSlotState[] CaptureConsumableState()
    {
        RunConsumableSlotState[] snapshot = new RunConsumableSlotState[ConsumableCapacity];
        for (int i = 0; i < snapshot.Length; i++)
        {
            ConsumableSlot slot = consumableSlots[i];
            snapshot[i] = new RunConsumableSlotState(slot.asset, slot.asset != null ? 1 : 0);
        }

        return snapshot;
    }

    private void ApplyDiceState(DiceSpinnerGeneric[] dicePrefabs)
    {
        ClearSpawnedEquippedDiceInstances();
        RunInventoryLoadoutUtility.CopyLayout(equippedDicePrefabs, dicePrefabs);

        if (diceRig != null && HasAnyAssignedDicePrefabs())
        {
            RebuildSpawnedEquippedDiceFromPrefabs();
        }
        else
        {
            RunInventoryLoadoutUtility.CopyLayout(equippedDice, dicePrefabs);
        }

        SyncDiceRigFromInventory();
    }

    private void ApplyOwnedSkills(ScriptableObject[] skills)
    {
        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i] == null)
            {
                ownedSlots[i] = new SlotBinding();
            }

            ownedSlots[i].skillAsset = skills != null && i < skills.Length ? skills[i] : null;
        }
    }

    private void ApplyConsumables(RunConsumableSlotState[] slots)
    {
        EnsureSizes();
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (slots == null || i >= slots.Length || slots[i].IsEmpty)
            {
                consumableSlots[i] = default;
                continue;
            }

            consumableSlots[i] = new ConsumableSlot
            {
                asset = slots[i].Asset,
                charges = slots[i].Asset != null ? 1 : 0
            };
        }

        CompactConsumables();
    }

    private static bool HasAnyConsumable(RunConsumableSlotState[] slots)
    {
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty)
            {
                return true;
            }
        }

        return false;
    }
}
