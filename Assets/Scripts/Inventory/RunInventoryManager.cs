using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[DisallowMultipleComponent]
public class RunInventoryManager : MonoBehaviour
{
    public const int FIXED_SKILL_COUNT = 2;
    public const int OWNED_SKILL_COUNT = 4;
    public const int RELIC_SLOT_COUNT = 3;
    public const int DEFAULT_CONSUMABLE_CAPACITY = 5;
    public const int MAX_CONSUMABLE_CAPACITY = 10;
    public const int EQUIPPED_DICE_COUNT = 3;
    public const int PASSIVE_SLOT_COUNT = 1;

    [Title("Runtime Links (Optional)")]
    [SerializeField] private DiceSlotRig diceRig;

    [Title("Build State - Dice")]
    [InfoBox("Assign dice prefabs here. Runtime dice instances will be spawned into DiceRig from these prefab slots.", InfoMessageType.Info)]
    [SerializeField] private DiceSpinnerGeneric[] equippedDicePrefabs = new DiceSpinnerGeneric[EQUIPPED_DICE_COUNT];
    [SerializeField, HideInInspector] private DiceSpinnerGeneric[] equippedDice = new DiceSpinnerGeneric[EQUIPPED_DICE_COUNT];

    [Title("Build State - Passive")]
    [InfoBox("Passive now uses its own dedicated single-slot binding. Passive is no longer stored in owned skill slots.", InfoMessageType.Info)]
    [SerializeField] private PassiveSlotBinding[] passiveSlots = new PassiveSlotBinding[PASSIVE_SLOT_COUNT];

    [Title("Skill + UI Bindings")]
    [InfoBox("Each slot contains BOTH:\n- UI Icon (usually fixed)\n- Skill Asset (changes often)\n\n" +
             "Fixed = 2 default skills (Strike/Guard)\nOwned = 4 flexible slots\n\n" +
             "Use 'Apply Bindings To Icons' after assigning icons.", InfoMessageType.Info)]
    [SerializeField] private SlotBinding[] fixedSlots = new SlotBinding[FIXED_SKILL_COUNT];

    [SerializeField] private SlotBinding[] ownedSlots = new SlotBinding[OWNED_SKILL_COUNT];

    [Title("Consumables")]
    [SerializeField, Min(1)] private int consumableCapacity = DEFAULT_CONSUMABLE_CAPACITY;
    [SerializeField] private ConsumableSlot[] consumableSlots = new ConsumableSlot[DEFAULT_CONSUMABLE_CAPACITY];

    [Title("Currency")]
    [SerializeField] private int gold;

    public event Action InventoryChanged;

    public DiceSlotRig DiceRig => diceRig;
    public int Gold => gold;
    public int ConsumableCapacity => consumableSlots != null ? Mathf.Clamp(consumableSlots.Length, 1, MAX_CONSUMABLE_CAPACITY) : Mathf.Clamp(consumableCapacity, 1, MAX_CONSUMABLE_CAPACITY);

    public enum SkillSource { Fixed, Owned }

    public RunInventoryState CaptureState()
    {
        EnsureSizes();

        RunInventoryState snapshot = new RunInventoryState();
        DiceSpinnerGeneric[] dice = CaptureEquippedDicePrefabLayout();
        ScriptableObject[] skills = CaptureOwnedSkillAssets();
        SkillPassiveSO[] passives = CapturePassiveAssets();
        RunConsumableSlotState[] relics = CaptureConsumableState();

        snapshot.SetDiceState(dice, dice);
        snapshot.SetSkillState(skills, skills);
        snapshot.SetPassiveState(passives, passives);
        snapshot.SetConsumableState(relics, relics);
        return snapshot;
    }

    public void WriteStateTo(RunState state)
    {
        if (state == null)
            return;

        state.SetGold(gold);
        state.SetInventoryState(CaptureState());
    }

    public void ApplyState(RunState state, bool notifyChanged = true)
    {
        if (state == null)
            return;

        ApplyInventoryState(state.InventoryState, notifyChanged: false);
        gold = Mathf.Max(0, state.Gold);

        if (notifyChanged)
            InventoryChanged?.Invoke();
    }

    public void ApplyInventoryState(RunInventoryState snapshot, bool notifyChanged = true)
    {
        if (snapshot == null)
            return;

        EnsureSizes();
        ApplyDiceState(snapshot.EquippedDice);
        ApplyOwnedSkills(snapshot.EquippedSkills);
        ApplyPassives(snapshot.EquippedPassive);
        ApplyConsumables(HasAnyConsumable(snapshot.RelicSlots) ? snapshot.RelicSlots : snapshot.Consumables);

        RunInventoryBindingUtility.ApplyBindingsToIcons(this, fixedSlots, ownedSlots, passiveSlots);

        if (notifyChanged)
            InventoryChanged?.Invoke();
    }

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

    public DiceSpinnerGeneric GetEquippedDicePrefab(int index)
        => RunInventoryLoadoutUtility.GetAt(equippedDicePrefabs, index);

    public void FillEquippedDice(List<DiceSpinnerGeneric> buffer)
        => RunInventoryLoadoutUtility.Fill(equippedDice, buffer);

    public int FindFirstEmptyEquippedDiceSlot()
        => RunInventoryLoadoutUtility.FindFirstEmpty(equippedDice);

    public bool IsDiceLoadoutFull() => FindFirstEmptyEquippedDiceSlot() < 0;

    public bool TryAddDiceToFirstEmptySlot(DiceSpinnerGeneric dice)
    {
        if (IsPrefabAsset(dice))
            return TryAddDicePrefabToFirstEmptySlot(dice);

        if (!RunInventoryLoadoutUtility.TryAddToFirstEmpty(equippedDice, dice, out int addedIndex)) return false;
        equippedDicePrefabs[addedIndex] = ResolveTrackedPrefabForRuntime(dice);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddDicePrefabToFirstEmptySlot(DiceSpinnerGeneric dicePrefab)
    {
        if (!RunInventoryLoadoutUtility.TryAddToFirstEmpty(equippedDicePrefabs, dicePrefab, out _))
            return false;

        RebuildSpawnedEquippedDiceFromPrefabs();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetEquippedDice(int index, DiceSpinnerGeneric dice)
    {
        if (IsPrefabAsset(dice))
        {
            SetEquippedDicePrefab(index, dice);
            return;
        }

        DestroyIfSpawned(equippedDice[index]);
        if (!RunInventoryLoadoutUtility.SetAt(equippedDice, index, dice)) return;
        equippedDicePrefabs[index] = ResolveTrackedPrefabForRuntime(dice);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SetEquippedDicePrefab(int index, DiceSpinnerGeneric dicePrefab)
    {
        if (!RunInventoryLoadoutUtility.SetAt(equippedDicePrefabs, index, dicePrefab)) return;
        RebuildSpawnedEquippedDiceFromPrefabs();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SwapEquippedDice(int a, int b)
    {
        if (!RunInventoryLoadoutUtility.Swap(equippedDice, a, b)) return;
        RunInventoryLoadoutUtility.Swap(equippedDicePrefabs, a, b);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void ClearEquippedDice(int index)
    {
        DestroyIfSpawned(RunInventoryLoadoutUtility.GetAt(equippedDice, index));
        if (!RunInventoryLoadoutUtility.SetAt<DiceSpinnerGeneric>(equippedDice, index, null)) return;
        RunInventoryLoadoutUtility.SetAt<DiceSpinnerGeneric>(equippedDicePrefabs, index, null);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public bool RemoveEquippedDice(DiceSpinnerGeneric dice)
    {
        DestroyIfSpawned(dice);
        if (!RunInventoryLoadoutUtility.RemoveReference(equippedDice, dice)) return false;
        SyncPrefabLayoutFromRuntime();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetDiceLayout(DiceSpinnerGeneric[] equipped, bool notifyChanged = true)
    {
        EnsureSizes();
        DiceSpinnerGeneric[] previousLayout = new DiceSpinnerGeneric[equippedDice.Length];
        RunInventoryLoadoutUtility.CopyLayout(previousLayout, equippedDice);
        RunInventoryLoadoutUtility.CopyLayout(equippedDice, equipped);
        DestroyRemovedSpawnedDice(previousLayout, equippedDice);
        SyncPrefabLayoutFromRuntime();
        SyncDiceRigFromInventory();
        if (notifyChanged)
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

    public void SetGold(int amount)
    {
        gold = Mathf.Max(0, amount);
        InventoryChanged?.Invoke();
    }

    public bool TrySpendGold(int amount)
    {
        if (!RunInventoryLoadoutUtility.TrySpendGold(ref gold, amount)) return false;
        InventoryChanged?.Invoke();
        return true;
    }

    // =========================================================
    // ===================== CONSUMABLES ======================
    // =========================================================

    [Serializable]
    public struct ConsumableSlot
    {
        public ConsumableDataSO asset;
        public int charges;
        public bool IsEmpty => asset == null;
    }

    public ConsumableDataSO GetConsumable(int index)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity) return null;
        return consumableSlots[index].asset;
    }

    public int GetConsumableCharges(int index)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity) return 0;
        return consumableSlots[index].asset != null ? Mathf.Max(0, consumableSlots[index].charges) : 0;
    }

    public int GetConsumableCount()
    {
        EnsureSizes();

        int count = 0;
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i].asset != null)
                count++;
        }

        return count;
    }

    public bool TrySetConsumable(int index, ConsumableDataSO asset, int charges = -1)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity) return false;
        if (asset == null) return false;

        int resolvedCharges = charges > 0 ? charges : asset.GetStartingCharges();
        consumableSlots[index] = new ConsumableSlot { asset = asset, charges = Mathf.Max(0, resolvedCharges) };
        if (consumableSlots[index].charges <= 0) consumableSlots[index] = default;
        CompactConsumables();
        InventoryChanged?.Invoke();
        return true;
    }

    public int FindFirstEmptyConsumableSlot()
    {
        EnsureSizes();
        for (int i = 0; i < ConsumableCapacity; i++)
        {
            if (consumableSlots[i].asset == null)
                return i;
        }

        return -1;
    }

    public bool TryAddConsumableToFirstEmptySlot(ConsumableDataSO asset, int charges = -1)
    {
        int index = FindFirstEmptyConsumableSlot();
        return index >= 0 && TrySetConsumable(index, asset, charges);
    }

    public bool TrySwapConsumables(int a, int b)
    {
        EnsureSizes();
        if (a < 0 || a >= ConsumableCapacity) return false;
        if (b < 0 || b >= ConsumableCapacity) return false;
        if (a == b) return true;

        ConsumableSlot temp = consumableSlots[a];
        consumableSlots[a] = consumableSlots[b];
        consumableSlots[b] = temp;
        CompactConsumables();
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryMoveConsumable(int fromIndex, int insertIndex)
    {
        EnsureSizes();
        if (fromIndex < 0 || fromIndex >= ConsumableCapacity) return false;
        if (consumableSlots[fromIndex].asset == null) return false;

        List<ConsumableSlot> ordered = new List<ConsumableSlot>(ConsumableCapacity);
        int compactSourceIndex = -1;
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i].asset == null)
                continue;

            if (i == fromIndex)
                compactSourceIndex = ordered.Count;

            ordered.Add(consumableSlots[i]);
        }

        if (compactSourceIndex < 0)
            return false;

        ConsumableSlot moving = ordered[compactSourceIndex];
        ordered.RemoveAt(compactSourceIndex);
        insertIndex = Mathf.Clamp(insertIndex, 0, ordered.Count);
        ordered.Insert(insertIndex, moving);
        ApplyOrderedConsumables(ordered);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryConsumeConsumableCharge(int index, int amount = 1)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity) return false;
        if (amount <= 0) return true;

        ConsumableSlot slot = consumableSlots[index];
        if (slot.asset == null) return false;

        slot.charges -= amount;
        if (slot.charges <= 0) slot = default;

        consumableSlots[index] = slot;
        CompactConsumables();
        InventoryChanged?.Invoke();
        return true;
    }

    public void ClearConsumable(int index)
    {
        EnsureSizes();
        if (index < 0 || index >= ConsumableCapacity) return;
        consumableSlots[index] = default;
        CompactConsumables();
        InventoryChanged?.Invoke();
    }

    public bool TrySetRelic(int index, ScriptableObject asset, int charges)
    {
        return TrySetConsumable(index, asset as ConsumableDataSO, charges);
    }

    public bool TryConsumeRelicCharge(int index, int amount = 1)
    {
        return TryConsumeConsumableCharge(index, amount);
    }

    public void ClearRelic(int index)
    {
        ClearConsumable(index);
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

    [Button(ButtonSizes.Medium)]
    private void RebuildSpawnedEquippedDiceFromPrefabsButton()
    {
        EnsureSizes();
        RebuildSpawnedEquippedDiceFromPrefabs();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    // =========================================================
    // ===================== UNITY / SAFETY ====================
    // =========================================================

    private void Awake()
    {
        EnsureSizes();
        RunInventorySetupUtility.BootstrapEquippedDiceFromRigIfNeeded(diceRig, equippedDicePrefabs, equippedDice);
        if (HasAnyAssignedDicePrefabs())
            RebuildSpawnedEquippedDiceFromPrefabs();
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
        RunInventorySetupUtility.BootstrapEquippedDiceFromRigIfNeeded(diceRig, equippedDicePrefabs, equippedDice);
        if (!HasAnyAssignedDicePrefabs())
            SyncDiceRigFromInventory();
    }
#endif

    private void EnsureSizes()
    {
        consumableCapacity = Mathf.Clamp(consumableCapacity, 1, MAX_CONSUMABLE_CAPACITY);
        RunInventorySetupUtility.EnsureSizes(
            ref fixedSlots,
            ref ownedSlots,
            ref consumableSlots,
            ref equippedDicePrefabs,
            ref equippedDice,
            ref passiveSlots,
            consumableCapacity);
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
            snapshot[i] = ownedSlots[i]?.skillAsset;

        return snapshot;
    }

    private SkillPassiveSO[] CapturePassiveAssets()
    {
        SkillPassiveSO[] snapshot = new SkillPassiveSO[PASSIVE_SLOT_COUNT];

        for (int i = 0; i < snapshot.Length; i++)
            snapshot[i] = passiveSlots[i]?.passiveAsset;

        return snapshot;
    }

    private RunConsumableSlotState[] CaptureConsumableState()
    {
        RunConsumableSlotState[] snapshot = new RunConsumableSlotState[ConsumableCapacity];

        for (int i = 0; i < snapshot.Length; i++)
        {
            ConsumableSlot slot = consumableSlots[i];
            snapshot[i] = new RunConsumableSlotState(slot.asset, slot.asset != null ? slot.charges : 0);
        }

        return snapshot;
    }

    private void ApplyDiceState(DiceSpinnerGeneric[] dicePrefabs)
    {
        ClearSpawnedEquippedDiceInstances();
        RunInventoryLoadoutUtility.CopyLayout(equippedDicePrefabs, dicePrefabs);

        if (diceRig != null && HasAnyAssignedDicePrefabs())
            RebuildSpawnedEquippedDiceFromPrefabs();
        else
            RunInventoryLoadoutUtility.CopyLayout(equippedDice, dicePrefabs);

        SyncDiceRigFromInventory();
    }

    private void ApplyOwnedSkills(ScriptableObject[] skills)
    {
        for (int i = 0; i < ownedSlots.Length; i++)
        {
            if (ownedSlots[i] == null)
                ownedSlots[i] = new SlotBinding();

            ownedSlots[i].skillAsset = skills != null && i < skills.Length ? skills[i] : null;
        }
    }

    private void ApplyPassives(SkillPassiveSO[] passives)
    {
        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i] == null)
                passiveSlots[i] = new PassiveSlotBinding();

            passiveSlots[i].passiveAsset = passives != null && i < passives.Length ? passives[i] : null;
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
                charges = slots[i].Charges
            };
        }

        CompactConsumables();
    }

    private static bool HasAnyConsumable(RunConsumableSlotState[] slots)
    {
        if (slots == null)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty)
                return true;
        }

        return false;
    }

    private void CompactConsumables()
    {
        if (consumableSlots == null || consumableSlots.Length == 0)
            return;

        List<ConsumableSlot> ordered = new List<ConsumableSlot>(consumableSlots.Length);
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i].asset != null && consumableSlots[i].charges > 0)
                ordered.Add(consumableSlots[i]);
        }

        ApplyOrderedConsumables(ordered);
    }

    private void ApplyOrderedConsumables(List<ConsumableSlot> ordered)
    {
        if (consumableSlots == null)
            return;

        for (int i = 0; i < consumableSlots.Length; i++)
            consumableSlots[i] = i < ordered.Count ? ordered[i] : default;
    }

    private readonly Dictionary<DiceSpinnerGeneric, DiceSpinnerGeneric> _spawnedPrefabByRuntime = new Dictionary<DiceSpinnerGeneric, DiceSpinnerGeneric>();

    private bool HasAnyAssignedDicePrefabs()
    {
        for (int i = 0; i < equippedDicePrefabs.Length; i++)
        {
            if (equippedDicePrefabs[i] != null)
                return true;
        }

        return false;
    }

    private void RebuildSpawnedEquippedDiceFromPrefabs()
    {
        ClearSpawnedEquippedDiceInstances();

        if (diceRig == null)
            return;

        for (int i = 0; i < equippedDicePrefabs.Length; i++)
        {
            DiceSpinnerGeneric prefab = equippedDicePrefabs[i];
            if (prefab == null)
            {
                equippedDice[i] = null;
                continue;
            }

            DiceSpinnerGeneric instance = SpawnEquippedDiceInstance(i, prefab);
            equippedDice[i] = instance;
            if (instance != null)
                _spawnedPrefabByRuntime[instance] = prefab;
        }
    }

    private void ClearSpawnedEquippedDiceInstances()
    {
        for (int i = 0; i < equippedDice.Length; i++)
        {
            DiceSpinnerGeneric runtime = equippedDice[i];
            if (runtime == null)
                continue;

            if (_spawnedPrefabByRuntime.ContainsKey(runtime))
                DestroyDiceInstance(runtime.gameObject);

            equippedDice[i] = null;
        }

        _spawnedPrefabByRuntime.Clear();
    }

    private void DestroyRemovedSpawnedDice(DiceSpinnerGeneric[] previousLayout, DiceSpinnerGeneric[] nextLayout)
    {
        if (previousLayout == null)
            return;

        for (int i = 0; i < previousLayout.Length; i++)
        {
            DiceSpinnerGeneric previous = previousLayout[i];
            if (previous == null || !_spawnedPrefabByRuntime.ContainsKey(previous))
                continue;

            bool stillPresent = false;
            if (nextLayout != null)
            {
                for (int j = 0; j < nextLayout.Length; j++)
                {
                    if (nextLayout[j] == previous)
                    {
                        stillPresent = true;
                        break;
                    }
                }
            }

            if (!stillPresent)
                DestroyIfSpawned(previous);
        }
    }

    private DiceSpinnerGeneric SpawnEquippedDiceInstance(int slotIndex, DiceSpinnerGeneric prefab)
    {
        if (prefab == null)
            return null;

        Transform parent = diceRig != null &&
                           diceRig.slots != null &&
                           slotIndex >= 0 &&
                           slotIndex < diceRig.slots.Length &&
                           diceRig.slots[slotIndex] != null &&
                           diceRig.slots[slotIndex].slotRoot != null
            ? diceRig.slots[slotIndex].slotRoot.transform
            : (diceRig != null ? diceRig.transform : transform);

        GameObject instanceGo = Instantiate(prefab.gameObject, parent, false);
        instanceGo.name = prefab.gameObject.name;
        instanceGo.transform.localPosition = Vector3.zero;
        instanceGo.transform.localRotation = Quaternion.identity;
        instanceGo.transform.localScale = prefab.transform.localScale;
        return instanceGo.GetComponent<DiceSpinnerGeneric>();
    }

    private void SyncPrefabLayoutFromRuntime()
    {
        for (int i = 0; i < equippedDice.Length; i++)
            equippedDicePrefabs[i] = ResolveTrackedPrefabForRuntime(equippedDice[i]);
    }

    private DiceSpinnerGeneric ResolveTrackedPrefabForRuntime(DiceSpinnerGeneric runtime)
    {
        if (runtime == null)
            return null;

        if (_spawnedPrefabByRuntime.TryGetValue(runtime, out DiceSpinnerGeneric prefab))
            return prefab;

        return null;
    }

    private void DestroyIfSpawned(DiceSpinnerGeneric runtime)
    {
        if (runtime == null)
            return;

        if (!_spawnedPrefabByRuntime.Remove(runtime))
            return;

        DestroyDiceInstance(runtime.gameObject);
    }

    private static bool IsPrefabAsset(DiceSpinnerGeneric dice)
    {
        if (dice == null)
            return false;

        return !dice.gameObject.scene.IsValid();
    }

    private static void DestroyDiceInstance(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
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
