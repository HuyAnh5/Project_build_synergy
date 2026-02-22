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

    [Title("Runtime Links (Optional)")]
    [SerializeField] private DiceSlotRig diceRig;

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
        else
        {
            if (index < 0 || index >= OWNED_SKILL_COUNT) return null;
            return ownedSlots[index].skillAsset;
        }
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
            if (ownedSlots[i].skillAsset == null) return i;
        return -1;
    }

    public void FillPassives(List<SkillPassiveSO> buffer)
    {
        buffer.Clear();
        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
        {
            if (ownedSlots[i].skillAsset is SkillPassiveSO p && p != null)
                buffer.Add(p);
        }
    }

    public bool HasAnyPassive()
    {
        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
            if (ownedSlots[i].skillAsset is SkillPassiveSO) return true;
        return false;
    }

    public bool IsActiveSkill(ScriptableObject asset)
    {
        return asset is SkillDamageSO ||
               asset is SkillBuffDebuffSO ||
               asset is SkillSO; // legacy optional
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

        var slot = relicSlots[index];
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
        // ✅ UI Icon first (rarely changes)
        [LabelText("UI Icon")]
        [Tooltip("Drag the DraggableSkillIcon for this slot here (usually fixed).")]
        public DraggableSkillIcon uiIcon;

        // ✅ Skill second (changes often)
        [LabelText("Skill")]
        [Tooltip("The actual skill asset stored in this slot (changes often).")]
        public ScriptableObject skillAsset;
    }

    [Button(ButtonSizes.Medium)]
    private void ApplyBindingsToIcons()
    {
        EnsureSizes();

        // Fixed 0..1
        for (int i = 0; i < FIXED_SKILL_COUNT; i++)
            BindIconToSlot(fixedSlots[i].uiIcon, isFixed: true, index: i);

        // Owned 0..5
        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
            BindIconToSlot(ownedSlots[i].uiIcon, isFixed: false, index: i);

        InventoryChanged?.Invoke();
        Debug.Log("[RunInventoryManager] Applied slot bindings to UI icons.");
    }

    private void PushSlotToIcon(bool isFixed, int index)
    {
        var binding = isFixed ? fixedSlots[index] : ownedSlots[index];
        if (binding == null || binding.uiIcon == null) return;

        // Icon reads from inventory slot, so just refresh.
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
        // If this feels too spammy, comment out this line and press the button manually.
        ApplyBindingsToIcons();
    }
#endif

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
    }
}
