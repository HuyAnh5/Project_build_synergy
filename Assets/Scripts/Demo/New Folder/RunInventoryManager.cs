using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[DisallowMultipleComponent]
public class RunInventoryManager : MonoBehaviour
{
    public const int FIXED_SKILL_COUNT = 2;
    public const int OWNED_SKILL_COUNT = 6;
    public const int RELIC_SLOT_COUNT = 3;
    public const int EQUIPPED_DICE_COUNT = 3;
    public const int PASSIVE_SLOT_COUNT = 3;

    [Title("Runtime Links (Optional)")]
    [SerializeField] private DiceSlotRig diceRig;

    [Title("Build State - Dice")]
    [SerializeField] private DiceSpinnerGeneric[] equippedDice = new DiceSpinnerGeneric[EQUIPPED_DICE_COUNT];

    [Title("Build State - Passive")]
    [SerializeField] private SkillPassiveSO[] equippedPassives = new SkillPassiveSO[PASSIVE_SLOT_COUNT];

    [Title("Skill + UI Bindings")]
    [InfoBox("Each slot contains BOTH:\n- UI Icon (usually fixed)\n- Skill Asset (changes often)\n\n" +
             "Fixed = 2 default skills (Strike/Guard)\nOwned = 6 flexible slots\n\n" +
             "Use 'Apply Bindings To Icons' after assigning icons.", InfoMessageType.Info)]
    [SerializeField] private SlotBinding[] fixedSlots = new SlotBinding[FIXED_SKILL_COUNT];

    [SerializeField] private SlotBinding[] ownedSlots = new SlotBinding[OWNED_SKILL_COUNT];

    [Title("Relics (Future)")]
    [SerializeField] private RelicSlot[] relicSlots = new RelicSlot[RELIC_SLOT_COUNT];

    [Title("Currency")]
    [SerializeField] private int gold;

    public event Action InventoryChanged;

    public DiceSlotRig DiceRig => diceRig;
    public int Gold => gold;

    public enum SkillSource { Fixed, Owned }

    // =========================================================
    // ===================== PUBLIC API ========================
    // =========================================================

    public ScriptableObject GetSkill(SkillSource source, int index)
    {
        if (source == SkillSource.Fixed)
        {
            if (index < 0 || index >= FIXED_SKILL_COUNT) return null;
            return fixedSlots[index].skillAsset;
        }

        if (index < 0 || index >= OWNED_SKILL_COUNT) return null;
        return ownedSlots[index].skillAsset;
    }

    public void SetSkill(SkillSource source, int index, ScriptableObject assetOrNull)
    {
        if (source == SkillSource.Fixed)
        {
            if (index < 0 || index >= FIXED_SKILL_COUNT) return;
            fixedSlots[index].skillAsset = assetOrNull;
            PushSlotToIcon(isFixed: true, index);
        }
        else
        {
            if (index < 0 || index >= OWNED_SKILL_COUNT) return;
            ownedSlots[index].skillAsset = assetOrNull;
            PushSlotToIcon(isFixed: false, index);
        }

        InventoryChanged?.Invoke();
    }

    public int FindFirstEmptyOwnedSlot()
    {
        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
        {
            if (ownedSlots[i].skillAsset == null)
                return i;
        }

        return -1;
    }

    public bool IsActiveSkill(ScriptableObject asset)
    {
        return asset is SkillDamageSO ||
               asset is SkillBuffDebuffSO ||
               asset is SkillSO;
    }

    // =========================================================
    // ======================== DICE ===========================
    // =========================================================

    public DiceSpinnerGeneric GetEquippedDice(int index)
    {
        if (index < 0 || index >= EQUIPPED_DICE_COUNT) return null;
        return equippedDice[index];
    }

    public void FillEquippedDice(List<DiceSpinnerGeneric> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();

        for (int i = 0; i < EQUIPPED_DICE_COUNT; i++)
        {
            if (equippedDice[i] != null)
                buffer.Add(equippedDice[i]);
        }
    }

    public int FindFirstEmptyEquippedDiceSlot()
    {
        for (int i = 0; i < EQUIPPED_DICE_COUNT; i++)
        {
            if (equippedDice[i] == null)
                return i;
        }

        return -1;
    }

    public bool IsDiceLoadoutFull() => FindFirstEmptyEquippedDiceSlot() < 0;

    public bool TryAddDiceToFirstEmptySlot(DiceSpinnerGeneric dice)
    {
        if (dice == null) return false;
        if (ContainsEquippedDice(dice)) return false;

        int empty = FindFirstEmptyEquippedDiceSlot();
        if (empty < 0) return false;

        equippedDice[empty] = dice;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetEquippedDice(int index, DiceSpinnerGeneric dice)
    {
        if (index < 0 || index >= EQUIPPED_DICE_COUNT) return;
        equippedDice[index] = dice;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SwapEquippedDice(int a, int b)
    {
        if (a < 0 || a >= EQUIPPED_DICE_COUNT) return;
        if (b < 0 || b >= EQUIPPED_DICE_COUNT) return;
        if (a == b) return;

        DiceSpinnerGeneric tmp = equippedDice[a];
        equippedDice[a] = equippedDice[b];
        equippedDice[b] = tmp;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void ClearEquippedDice(int index)
    {
        if (index < 0 || index >= EQUIPPED_DICE_COUNT) return;
        equippedDice[index] = null;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public bool RemoveEquippedDice(DiceSpinnerGeneric dice)
    {
        if (dice == null) return false;

        for (int i = 0; i < EQUIPPED_DICE_COUNT; i++)
        {
            if (equippedDice[i] != dice) continue;
            equippedDice[i] = null;
            SyncDiceRigFromInventory();
            InventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void SetDiceLayout(DiceSpinnerGeneric[] equipped)
    {
        EnsureSizes();

        for (int i = 0; i < EQUIPPED_DICE_COUNT; i++)
            equippedDice[i] = equipped != null && i < equipped.Length ? equipped[i] : null;

        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SyncDiceRigFromInventory()
    {
        if (diceRig == null) return;

        for (int i = 0; i < EQUIPPED_DICE_COUNT; i++)
        {
            DiceSpinnerGeneric dice = equippedDice[i];
            diceRig.AssignDiceToSlot(i, dice);
            diceRig.SetSlotActive(i, dice != null);
        }
    }

    public bool ContainsEquippedDice(DiceSpinnerGeneric dice)
    {
        if (dice == null) return false;
        for (int i = 0; i < EQUIPPED_DICE_COUNT; i++)
            if (equippedDice[i] == dice) return true;
        return false;
    }

    // =========================================================
    // ====================== PASSIVES =========================
    // =========================================================

    public SkillPassiveSO GetEquippedPassive(int index)
    {
        if (index < 0 || index >= PASSIVE_SLOT_COUNT) return null;
        return equippedPassives[index];
    }

    public void FillEquippedPassives(List<SkillPassiveSO> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();

        for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
        {
            if (equippedPassives[i] != null)
                buffer.Add(equippedPassives[i]);
        }
    }

    public int FindFirstEmptyEquippedPassiveSlot()
    {
        for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
        {
            if (equippedPassives[i] == null)
                return i;
        }

        return -1;
    }

    public bool IsPassiveLoadoutFull() => FindFirstEmptyEquippedPassiveSlot() < 0;

    public bool TryAddPassiveToFirstEmptySlot(SkillPassiveSO passive)
    {
        if (passive == null) return false;
        if (ContainsEquippedPassive(passive)) return false;

        int empty = FindFirstEmptyEquippedPassiveSlot();
        if (empty < 0) return false;

        equippedPassives[empty] = passive;
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetEquippedPassive(int index, SkillPassiveSO passive)
    {
        if (index < 0 || index >= PASSIVE_SLOT_COUNT) return;
        equippedPassives[index] = passive;
        InventoryChanged?.Invoke();
    }

    public void SwapEquippedPassives(int a, int b)
    {
        if (a < 0 || a >= PASSIVE_SLOT_COUNT) return;
        if (b < 0 || b >= PASSIVE_SLOT_COUNT) return;
        if (a == b) return;

        SkillPassiveSO tmp = equippedPassives[a];
        equippedPassives[a] = equippedPassives[b];
        equippedPassives[b] = tmp;
        InventoryChanged?.Invoke();
    }

    public void ClearEquippedPassive(int index)
    {
        if (index < 0 || index >= PASSIVE_SLOT_COUNT) return;
        equippedPassives[index] = null;
        InventoryChanged?.Invoke();
    }

    public bool RemoveEquippedPassive(SkillPassiveSO passive)
    {
        if (passive == null) return false;

        for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
        {
            if (equippedPassives[i] != passive) continue;
            equippedPassives[i] = null;
            InventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void SetPassiveLayout(SkillPassiveSO[] equipped)
    {
        EnsureSizes();

        for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
            equippedPassives[i] = equipped != null && i < equipped.Length ? equipped[i] : null;

        InventoryChanged?.Invoke();
    }

    public void FillPassives(List<SkillPassiveSO> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();

        bool anyEquipped = false;
        for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
        {
            SkillPassiveSO passive = equippedPassives[i];
            if (passive == null) continue;
            anyEquipped = true;
            buffer.Add(passive);
        }

        // Backward compatibility:
        // if new passive slots are still empty, keep reading legacy passives from owned skill slots.
        if (anyEquipped) return;

        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
        {
            if (ownedSlots[i].skillAsset is SkillPassiveSO passive && passive != null)
                buffer.Add(passive);
        }
    }

    public bool HasAnyPassive()
    {
        for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
            if (equippedPassives[i] != null) return true;

        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
            if (ownedSlots[i].skillAsset is SkillPassiveSO) return true;

        return false;
    }

    public bool ContainsEquippedPassive(SkillPassiveSO passive)
    {
        if (passive == null) return false;
        for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
            if (equippedPassives[i] == passive) return true;
        return false;
    }

    // =========================================================
    // ======================== GOLD ===========================
    // =========================================================

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        InventoryChanged?.Invoke();
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        InventoryChanged?.Invoke();
        return true;
    }

    // =========================================================
    // ======================== RELICS =========================
    // =========================================================

    [Serializable]
    public struct RelicSlot
    {
        public ScriptableObject asset;
        public int charges;
        public bool IsEmpty => asset == null;
    }

    public bool TrySetRelic(int index, ScriptableObject asset, int charges)
    {
        if (index < 0 || index >= RELIC_SLOT_COUNT) return false;
        if (asset == null) return false;

        relicSlots[index] = new RelicSlot { asset = asset, charges = Mathf.Max(0, charges) };
        if (relicSlots[index].charges <= 0) relicSlots[index] = default;

        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryConsumeRelicCharge(int index, int amount = 1)
    {
        if (index < 0 || index >= RELIC_SLOT_COUNT) return false;
        if (amount <= 0) return true;

        RelicSlot slot = relicSlots[index];
        if (slot.asset == null) return false;

        slot.charges -= amount;
        if (slot.charges <= 0) slot = default;

        relicSlots[index] = slot;
        InventoryChanged?.Invoke();
        return true;
    }

    public void ClearRelic(int index)
    {
        if (index < 0 || index >= RELIC_SLOT_COUNT) return;
        relicSlots[index] = default;
        InventoryChanged?.Invoke();
    }

    // =========================================================
    // ===================== SLOT BINDING ======================
    // =========================================================

    [Serializable]
    public class SlotBinding
    {
        [LabelText("UI Icon")]
        [Tooltip("Drag the DraggableSkillIcon for this slot here (usually fixed).")]
        public DraggableSkillIcon uiIcon;

        [LabelText("Skill")]
        [Tooltip("The actual skill asset stored in this slot (changes often).")]
        public ScriptableObject skillAsset;
    }

    [Button(ButtonSizes.Medium)]
    private void ApplyBindingsToIcons()
    {
        EnsureSizes();

        for (int i = 0; i < FIXED_SKILL_COUNT; i++)
            BindIconToSlot(fixedSlots[i].uiIcon, isFixed: true, index: i);

        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
            BindIconToSlot(ownedSlots[i].uiIcon, isFixed: false, index: i);

        InventoryChanged?.Invoke();
        Debug.Log("[RunInventoryManager] Applied slot bindings to UI icons.");
    }

    private void PushSlotToIcon(bool isFixed, int index)
    {
        SlotBinding binding = isFixed ? fixedSlots[index] : ownedSlots[index];
        if (binding == null || binding.uiIcon == null) return;
        binding.uiIcon.Refresh();
    }

    private void BindIconToSlot(DraggableSkillIcon icon, bool isFixed, int index)
    {
        if (!icon) return;
        icon.SetBindToInventory(this, isFixed, index);
        icon.Refresh();
    }

    // =========================================================
    // ===================== UNITY / SAFETY ====================
    // =========================================================

    private void Awake()
    {
        EnsureSizes();
        BootstrapEquippedDiceFromRigIfNeeded();
        SyncDiceRigFromInventory();
    }

    private void Reset()
    {
        diceRig = GetComponentInChildren<DiceSlotRig>(true);
        EnsureSizes();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSizes();
        ApplyBindingsToIcons();
        BootstrapEquippedDiceFromRigIfNeeded();
        SyncDiceRigFromInventory();
    }
#endif

    private void BootstrapEquippedDiceFromRigIfNeeded()
    {
        if (diceRig == null) return;

        bool anyAssigned = false;
        for (int i = 0; i < EQUIPPED_DICE_COUNT; i++)
        {
            if (equippedDice[i] != null)
            {
                anyAssigned = true;
                break;
            }
        }

        if (anyAssigned) return;
        if (diceRig.slots == null) return;

        for (int i = 0; i < EQUIPPED_DICE_COUNT && i < diceRig.slots.Length; i++)
            equippedDice[i] = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
    }

    private void EnsureSizes()
    {
        if (fixedSlots == null || fixedSlots.Length != FIXED_SKILL_COUNT)
            fixedSlots = new SlotBinding[FIXED_SKILL_COUNT];

        if (ownedSlots == null || ownedSlots.Length != OWNED_SKILL_COUNT)
            ownedSlots = new SlotBinding[OWNED_SKILL_COUNT];

        for (int i = 0; i < FIXED_SKILL_COUNT; i++)
            if (fixedSlots[i] == null) fixedSlots[i] = new SlotBinding();

        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
            if (ownedSlots[i] == null) ownedSlots[i] = new SlotBinding();

        if (relicSlots == null || relicSlots.Length != RELIC_SLOT_COUNT)
            relicSlots = new RelicSlot[RELIC_SLOT_COUNT];

        if (equippedDice == null || equippedDice.Length != EQUIPPED_DICE_COUNT)
            equippedDice = new DiceSpinnerGeneric[EQUIPPED_DICE_COUNT];

        if (equippedPassives == null || equippedPassives.Length != PASSIVE_SLOT_COUNT)
            equippedPassives = new SkillPassiveSO[PASSIVE_SLOT_COUNT];
    }
}
