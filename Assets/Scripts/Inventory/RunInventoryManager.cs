using System;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Runtime source of truth for the player's run inventory, including skills, dice, consumables, and gold.
/// </summary>
[DisallowMultipleComponent]
public partial class RunInventoryManager : MonoBehaviour
{
    public const int OWNED_SKILL_COUNT = 5;
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

    [Title("Skill + UI Bindings")]
    [InfoBox("Each slot contains BOTH:\n- UI Icon binding\n- Skill Asset reference\n\nOwned = 5 flexible slots.\n\nUse 'Apply Bindings To Icons' after assigning icons.", InfoMessageType.Info)]
    [SerializeField] private SlotBinding[] ownedSlots = new SlotBinding[OWNED_SKILL_COUNT];

    [Title("Consumables")]
    [SerializeField, Min(1)] private int consumableCapacity = DEFAULT_CONSUMABLE_CAPACITY;
    [SerializeField] private ConsumableSlot[] consumableSlots = new ConsumableSlot[DEFAULT_CONSUMABLE_CAPACITY];

    [Title("Currency")]
    [SerializeField] private int gold;

    private readonly System.Collections.Generic.Dictionary<DiceSpinnerGeneric, DiceSpinnerGeneric> m_spawnedPrefabByRuntime =
        new System.Collections.Generic.Dictionary<DiceSpinnerGeneric, DiceSpinnerGeneric>();

    public event Action InventoryChanged;

    public DiceSlotRig DiceRig
    {
        get { return diceRig; }
    }

    public int Gold
    {
        get { return gold; }
    }

    public int ConsumableCapacity
    {
        get
        {
            return consumableSlots != null
                ? Mathf.Clamp(consumableSlots.Length, 1, MAX_CONSUMABLE_CAPACITY)
                : Mathf.Clamp(consumableCapacity, 1, MAX_CONSUMABLE_CAPACITY);
        }
    }

    public enum SkillSource
    {
        Owned
    }

    /// <summary>
    /// Returns the asset currently stored in the requested skill slot.
    /// </summary>
    public ScriptableObject GetSkill(SkillSource source, int index)
    {
        if (index < 0 || index >= OWNED_SKILL_COUNT)
        {
            return null;
        }

        return ownedSlots[index].skillAsset;
    }

    /// <summary>
    /// Returns the display name shown for a skill slot.
    /// </summary>
    public string GetSkillDisplayName(SkillSource source, int index)
    {
        ScriptableObject asset = GetSkill(source, index);
        return ResolveSkillDisplayName(asset);
    }

    /// <summary>
    /// Replaces the asset stored in a skill slot and pushes the update to the bound icon.
    /// </summary>
    public void SetSkill(SkillSource source, int index, ScriptableObject assetOrNull)
    {
        if (index < 0 || index >= OWNED_SKILL_COUNT)
        {
            return;
        }

        ownedSlots[index].skillAsset = assetOrNull;
        RunInventoryBindingUtility.PushSlotToIcon(ownedSlots, index);

        InventoryChanged?.Invoke();
    }

    public int FindFirstEmptyOwnedSlot()
    {
        for (int i = 0; i < OWNED_SKILL_COUNT; i++)
        {
            if (ownedSlots[i].skillAsset == null)
            {
                return i;
            }
        }

        return -1;
    }

    public bool IsActiveSkill(ScriptableObject asset)
    {
        return asset is SkillDamageSO || asset is SkillBuffDebuffSO;
    }

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
        if (!RunInventoryLoadoutUtility.TrySpendGold(ref gold, amount))
        {
            return false;
        }

        InventoryChanged?.Invoke();
        return true;
    }

    private void Awake()
    {
        EnsureSizes();
        RunInventorySetupUtility.BootstrapEquippedDiceFromRigIfNeeded(diceRig, equippedDicePrefabs, equippedDice);
        if (HasAnyAssignedDicePrefabs())
        {
            RebuildSpawnedEquippedDiceFromPrefabs();
        }

        SyncDiceRigFromInventory();
    }

    private void Reset()
    {
        diceRig = GetComponentInChildren<DiceSlotRig>(true);
        EnsureSizes();
    }

    private void EnsureSizes()
    {
        consumableCapacity = Mathf.Clamp(consumableCapacity, 1, MAX_CONSUMABLE_CAPACITY);
        RunInventorySetupUtility.EnsureSizes(
            ref ownedSlots,
            ref consumableSlots,
            ref equippedDicePrefabs,
            ref equippedDice,
            consumableCapacity);
    }

    private static string ResolveSkillDisplayName(ScriptableObject asset)
    {
        if (asset is SkillDamageSO damage)
        {
            if (!string.IsNullOrWhiteSpace(damage.displayName))
            {
                return damage.displayName;
            }
        }
        else if (asset is SkillBuffDebuffSO buffDebuff)
        {
            if (!string.IsNullOrWhiteSpace(buffDebuff.displayName))
            {
                return buffDebuff.displayName;
            }
        }
        else if (asset is SkillPassiveSO passive)
        {
            if (!string.IsNullOrWhiteSpace(passive.displayName))
            {
                return passive.displayName;
            }
        }
        return string.Empty;
    }
}
