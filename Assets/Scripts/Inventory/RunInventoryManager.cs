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
    public const int PASSIVE_SLOT_COUNT = 1;

    [Title("Runtime Links (Optional)")]
    [SerializeField] private DiceSlotRig diceRig;

    [Title("Build State - Dice")]
    [SerializeField] private DiceSpinnerGeneric[] equippedDice = new DiceSpinnerGeneric[EQUIPPED_DICE_COUNT];

    [Title("Build State - Passive")]
    [InfoBox("Passive now uses its own dedicated single-slot binding. Passive is no longer stored in owned skill slots.", InfoMessageType.Info)]
    [SerializeField] private PassiveSlotBinding[] passiveSlots = new PassiveSlotBinding[PASSIVE_SLOT_COUNT];

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

    public string GetSkillDisplayName(SkillSource source, int index)
    {
        ScriptableObject asset = GetSkill(source, index);
        bool isFixed = source == SkillSource.Fixed;
        return ResolveSkillDisplayName(asset, isFixed, index);
    }

    public void SetSkill(SkillSource source, int index, ScriptableObject assetOrNull)
    {
        if (source == SkillSource.Fixed)
        {
            if (index < 0 || index >= FIXED_SKILL_COUNT) return;
            fixedSlots[index].skillAsset = assetOrNull;
            RunInventoryBindingUtility.PushSlotToIcon(fixedSlots, ownedSlots, isFixed: true, index);
        }
        else
        {
            if (index < 0 || index >= OWNED_SKILL_COUNT) return;
            ownedSlots[index].skillAsset = assetOrNull;
            RunInventoryBindingUtility.PushSlotToIcon(fixedSlots, ownedSlots, isFixed: false, index);
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
               asset is SkillBuffDebuffSO;
    }

    // =========================================================
    // ======================== DICE ===========================
    // =========================================================

    public DiceSpinnerGeneric GetEquippedDice(int index)
        => RunInventoryLoadoutUtility.GetAt(equippedDice, index);

    public void FillEquippedDice(List<DiceSpinnerGeneric> buffer)
        => RunInventoryLoadoutUtility.Fill(equippedDice, buffer);

    public int FindFirstEmptyEquippedDiceSlot()
        => RunInventoryLoadoutUtility.FindFirstEmpty(equippedDice);

    public bool IsDiceLoadoutFull() => FindFirstEmptyEquippedDiceSlot() < 0;

    public bool TryAddDiceToFirstEmptySlot(DiceSpinnerGeneric dice)
    {
        if (!RunInventoryLoadoutUtility.TryAddToFirstEmpty(equippedDice, dice, out _)) return false;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetEquippedDice(int index, DiceSpinnerGeneric dice)
    {
        if (!RunInventoryLoadoutUtility.SetAt(equippedDice, index, dice)) return;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SwapEquippedDice(int a, int b)
    {
        if (!RunInventoryLoadoutUtility.Swap(equippedDice, a, b)) return;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void ClearEquippedDice(int index)
    {
        if (!RunInventoryLoadoutUtility.SetAt<DiceSpinnerGeneric>(equippedDice, index, null)) return;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public bool RemoveEquippedDice(DiceSpinnerGeneric dice)
    {
        if (!RunInventoryLoadoutUtility.RemoveReference(equippedDice, dice)) return false;
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetDiceLayout(DiceSpinnerGeneric[] equipped)
    {
        EnsureSizes();
        RunInventoryLoadoutUtility.CopyLayout(equippedDice, equipped);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SyncDiceRigFromInventory()
        => RunInventoryLoadoutUtility.SyncDiceRig(diceRig, equippedDice);

    public bool ContainsEquippedDice(DiceSpinnerGeneric dice)
        => RunInventoryLoadoutUtility.ContainsReference(equippedDice, dice);

    // =========================================================
    // ====================== PASSIVES =========================
    // =========================================================

    public SkillPassiveSO GetEquippedPassive(int index)
        => RunInventoryLoadoutUtility.GetPassiveAt(passiveSlots, index);

    public void FillEquippedPassives(List<SkillPassiveSO> buffer)
        => RunInventoryLoadoutUtility.FillPassiveAssets(passiveSlots, buffer);

    public int FindFirstEmptyEquippedPassiveSlot()
        => RunInventoryLoadoutUtility.FindFirstEmptyPassiveSlot(passiveSlots);

    public bool IsPassiveLoadoutFull() => FindFirstEmptyEquippedPassiveSlot() < 0;

    public bool TryAddPassiveToFirstEmptySlot(SkillPassiveSO passive)
    {
        if (!RunInventoryLoadoutUtility.TryAddPassiveToFirstEmptySlot(passiveSlots, passive, out int addedIndex)) return false;
        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, addedIndex);
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetEquippedPassive(int index, SkillPassiveSO passive)
    {
        if (!RunInventoryLoadoutUtility.SetPassiveAt(passiveSlots, index, passive)) return;
        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, index);
        InventoryChanged?.Invoke();
    }

    public void SwapEquippedPassives(int a, int b)
    {
        if (!RunInventoryLoadoutUtility.SwapPassiveSlots(passiveSlots, a, b)) return;
        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, a);
        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, b);
        InventoryChanged?.Invoke();
    }

    public void ClearEquippedPassive(int index)
    {
        if (!RunInventoryLoadoutUtility.SetPassiveAt(passiveSlots, index, null)) return;
        RunInventoryBindingUtility.PushPassiveSlotToIcon(passiveSlots, index);
        InventoryChanged?.Invoke();
    }

    public bool RemoveEquippedPassive(SkillPassiveSO passive)
    {
        if (!RunInventoryLoadoutUtility.RemovePassiveReference(passiveSlots, passive, out int removedIndex)) return false;
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
        => RunInventoryLoadoutUtility.FillPassiveAssets(passiveSlots, buffer);

    public bool HasAnyPassive()
        => RunInventoryLoadoutUtility.HasAnyPassive(passiveSlots);

    public bool ContainsEquippedPassive(SkillPassiveSO passive)
        => RunInventoryLoadoutUtility.ContainsPassiveReference(passiveSlots, passive);

    // =========================================================
    // ======================== GOLD ===========================
    // =========================================================

    public void AddGold(int amount)
    {
        gold = RunInventoryLoadoutUtility.AddGoldClamped(gold, amount);
        InventoryChanged?.Invoke();
    }

    public bool TrySpendGold(int amount)
    {
        if (!RunInventoryLoadoutUtility.TrySpendGold(ref gold, amount)) return false;
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

    [Serializable]
    public class PassiveSlotBinding
    {
        [LabelText("UI Icon")]
        [Tooltip("Optional passive UI binding for this passive slot.")]
        public PassiveDraggableUI uiIcon;

        [LabelText("Passive")]
        [Tooltip("The passive asset stored in this dedicated passive slot.")]
        public SkillPassiveSO passiveAsset;
    }

    [Button(ButtonSizes.Medium)]
    private void ApplyBindingsToIcons()
    {
        EnsureSizes();
        RunInventoryBindingUtility.ApplyBindingsToIcons(this, fixedSlots, ownedSlots, passiveSlots);

        InventoryChanged?.Invoke();
        Debug.Log("[RunInventoryManager] Applied slot bindings to UI icons.");
    }

    // =========================================================
    // ===================== UNITY / SAFETY ====================
    // =========================================================

    private void Awake()
    {
        EnsureSizes();
        RunInventorySetupUtility.BootstrapEquippedDiceFromRigIfNeeded(diceRig, equippedDice);
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
        RunInventorySetupUtility.BootstrapEquippedDiceFromRigIfNeeded(diceRig, equippedDice);
        SyncDiceRigFromInventory();
    }
#endif

    private void EnsureSizes()
    {
        RunInventorySetupUtility.EnsureSizes(
            ref fixedSlots,
            ref ownedSlots,
            ref relicSlots,
            ref equippedDice,
            ref passiveSlots);
    }

    private static string ResolveSkillDisplayName(ScriptableObject asset, bool isFixed, int index)
    {
        if (asset is SkillDamageSO damage)
        {
            if (damage.coreAction == CoreAction.BasicStrike)
                return "Basic Attack";
            if (damage.coreAction == CoreAction.BasicGuard)
                return "Basic Guard";

            if (!string.IsNullOrWhiteSpace(damage.displayName))
                return damage.displayName;
        }
        else if (asset is SkillBuffDebuffSO buffDebuff)
        {
            if (!string.IsNullOrWhiteSpace(buffDebuff.displayName))
                return buffDebuff.displayName;
        }
        else if (asset is SkillPassiveSO passive)
        {
            if (!string.IsNullOrWhiteSpace(passive.displayName))
                return passive.displayName;
        }

        if (isFixed)
        {
            if (index == 0)
                return "Basic Attack";
            if (index == 1)
                return "Basic Guard";
        }

        return string.Empty;
    }
}
