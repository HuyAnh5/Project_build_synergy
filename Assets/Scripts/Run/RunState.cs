using System;
using UnityEngine;

[Serializable]
public class RunState
{
    [SerializeField] private bool runActive;
    [SerializeField] private int currentAct = 1;
    [SerializeField] private string currentNodeId;
    [SerializeField] private int playerMaxHp = 30;
    [SerializeField] private int playerHp = 30;
    [SerializeField] private int gold;
    [SerializeField] private int bossIntelCount;
    [SerializeField] private bool bossRevealed;
    [SerializeField] private string pendingEncounterNodeId;
    [SerializeField] private MapPrototypeNodeType pendingEncounterType;
    [SerializeField] private RunCombatResult lastCombatResult;
    [SerializeField] private RunInventoryState inventoryState = new RunInventoryState();
    [SerializeField] private RunShopState shopState = new RunShopState();
    [SerializeField] private RunForgeState forgeState = new RunForgeState();
    [SerializeField] private RunRewardQueueState rewardQueue = new RunRewardQueueState();

    [NonSerialized] private MapPrototypeData currentMap;

    public bool RunActive => runActive;
    public int CurrentAct => currentAct;
    public MapPrototypeData CurrentMap => currentMap;
    public string CurrentNodeId => currentNodeId;
    public int PlayerMaxHp => playerMaxHp;
    public int PlayerHp => playerHp;
    public int Gold => gold;
    public int BossIntelCount => bossIntelCount;
    public bool BossRevealed => bossRevealed;
    public string PendingEncounterNodeId => pendingEncounterNodeId;
    public MapPrototypeNodeType PendingEncounterType => pendingEncounterType;
    public RunCombatResult LastCombatResult => lastCombatResult;
    public RunInventoryState InventoryState => inventoryState;
    public RunShopState ShopState => shopState;
    public RunForgeState ForgeState => forgeState;
    public RunRewardQueueState RewardQueue => rewardQueue;
    public bool HasPendingEncounter => !string.IsNullOrWhiteSpace(pendingEncounterNodeId);

    public DiceSpinnerGeneric[] DiceInventory => inventoryState.DiceInventory;
    public DiceSpinnerGeneric[] EquippedDice => inventoryState.EquippedDice;
    public ScriptableObject[] SkillInventory => inventoryState.SkillInventory;
    public ScriptableObject[] EquippedSkills => inventoryState.EquippedSkills;
    public SkillPassiveSO[] PassiveInventory => inventoryState.PassiveInventory;
    public SkillPassiveSO[] EquippedPassive => inventoryState.EquippedPassive;
    public RunConsumableSlotState[] Consumables => inventoryState.Consumables;
    public RunConsumableSlotState[] RelicSlots => inventoryState.RelicSlots;

    public void ResetForNewRun(int startingAct, int maxHp)
    {
        EnsureNestedState();
        runActive = true;
        currentAct = Mathf.Max(1, startingAct);
        currentMap = null;
        currentNodeId = null;
        playerMaxHp = Mathf.Max(1, maxHp);
        playerHp = playerMaxHp;
        gold = 0;
        bossIntelCount = 0;
        bossRevealed = false;
        pendingEncounterNodeId = null;
        pendingEncounterType = default;
        lastCombatResult = RunCombatResult.None;
        inventoryState.Reset();
        shopState.Reset();
        forgeState.Reset();
        rewardQueue.Clear();
    }

    public void SetMapSnapshot(MapPrototypeData map, string nodeId, int intelCount, bool revealed)
    {
        currentMap = map;
        currentNodeId = nodeId;
        bossIntelCount = Mathf.Max(0, intelCount);
        bossRevealed = revealed;
    }

    public void SetPlayerHp(int hp, int maxHp)
    {
        playerMaxHp = Mathf.Max(1, maxHp);
        playerHp = Mathf.Clamp(hp, 0, playerMaxHp);
    }

    public void SetGold(int value)
    {
        gold = Mathf.Max(0, value);
    }

    public void SetInventoryState(RunInventoryState snapshot)
    {
        EnsureNestedState();
        inventoryState.CopyFrom(snapshot);
    }

    public void BeginEncounter(string nodeId, MapPrototypeNodeType nodeType)
    {
        pendingEncounterNodeId = nodeId;
        pendingEncounterType = nodeType;
        lastCombatResult = RunCombatResult.None;
    }

    public void CompleteEncounter(RunCombatResult result)
    {
        lastCombatResult = result;
        pendingEncounterNodeId = null;
        pendingEncounterType = default;

        if (result == RunCombatResult.Defeat)
            runActive = false;
    }

    public void AdvanceAct()
    {
        currentAct += 1;
        currentMap = null;
        currentNodeId = null;
        bossIntelCount = 0;
        bossRevealed = false;
        pendingEncounterNodeId = null;
        pendingEncounterType = default;
        lastCombatResult = RunCombatResult.None;
        shopState.Reset();
        forgeState.Reset();
        rewardQueue.Clear();
    }

    public void EnsureNestedState()
    {
        if (inventoryState == null)
            inventoryState = new RunInventoryState();
        if (shopState == null)
            shopState = new RunShopState();
        if (forgeState == null)
            forgeState = new RunForgeState();
        if (rewardQueue == null)
            rewardQueue = new RunRewardQueueState();
    }
}

[Serializable]
public sealed class RunInventoryState
{
    [SerializeField] private DiceSpinnerGeneric[] diceInventory = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];
    [SerializeField] private DiceSpinnerGeneric[] equippedDice = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];
    [SerializeField] private ScriptableObject[] skillInventory = new ScriptableObject[RunInventoryManager.OWNED_SKILL_COUNT];
    [SerializeField] private ScriptableObject[] equippedSkills = new ScriptableObject[RunInventoryManager.OWNED_SKILL_COUNT];
    [SerializeField] private SkillPassiveSO[] passiveInventory = new SkillPassiveSO[RunInventoryManager.PASSIVE_SLOT_COUNT];
    [SerializeField] private SkillPassiveSO[] equippedPassive = new SkillPassiveSO[RunInventoryManager.PASSIVE_SLOT_COUNT];
    [SerializeField] private RunConsumableSlotState[] consumables = new RunConsumableSlotState[RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY];
    [SerializeField] private RunConsumableSlotState[] relicSlots = new RunConsumableSlotState[RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY];

    public DiceSpinnerGeneric[] DiceInventory => diceInventory;
    public DiceSpinnerGeneric[] EquippedDice => equippedDice;
    public ScriptableObject[] SkillInventory => skillInventory;
    public ScriptableObject[] EquippedSkills => equippedSkills;
    public SkillPassiveSO[] PassiveInventory => passiveInventory;
    public SkillPassiveSO[] EquippedPassive => equippedPassive;
    public RunConsumableSlotState[] Consumables => consumables;
    public RunConsumableSlotState[] RelicSlots => relicSlots;

    public void Reset()
    {
        diceInventory = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];
        equippedDice = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];
        skillInventory = new ScriptableObject[RunInventoryManager.OWNED_SKILL_COUNT];
        equippedSkills = new ScriptableObject[RunInventoryManager.OWNED_SKILL_COUNT];
        passiveInventory = new SkillPassiveSO[RunInventoryManager.PASSIVE_SLOT_COUNT];
        equippedPassive = new SkillPassiveSO[RunInventoryManager.PASSIVE_SLOT_COUNT];
        consumables = new RunConsumableSlotState[RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY];
        relicSlots = new RunConsumableSlotState[RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY];
    }

    public void CopyFrom(RunInventoryState source)
    {
        if (source == null)
        {
            Reset();
            return;
        }

        diceInventory = CopyArray(source.diceInventory);
        equippedDice = CopyArray(source.equippedDice);
        skillInventory = CopyArray(source.skillInventory);
        equippedSkills = CopyArray(source.equippedSkills);
        passiveInventory = CopyArray(source.passiveInventory);
        equippedPassive = CopyArray(source.equippedPassive);
        consumables = CopyArray(source.consumables);
        relicSlots = CopyArray(source.relicSlots);
        EnsureSlotSizes();
    }

    public void SetDiceState(DiceSpinnerGeneric[] inventory, DiceSpinnerGeneric[] equipped)
    {
        diceInventory = CopyArray(inventory);
        equippedDice = CopyArray(equipped);
        EnsureSlotSizes();
    }

    public void SetSkillState(ScriptableObject[] inventory, ScriptableObject[] equipped)
    {
        skillInventory = CopyArray(inventory);
        equippedSkills = CopyArray(equipped);
        EnsureSlotSizes();
    }

    public void SetPassiveState(SkillPassiveSO[] inventory, SkillPassiveSO[] equipped)
    {
        passiveInventory = CopyArray(inventory);
        equippedPassive = CopyArray(equipped);
        EnsureSlotSizes();
    }

    public void SetConsumableState(RunConsumableSlotState[] inventory, RunConsumableSlotState[] equippedRelics)
    {
        consumables = CopyArray(inventory);
        relicSlots = CopyArray(equippedRelics);
        EnsureSlotSizes();
    }

    private void EnsureSlotSizes()
    {
        EnsureLength(ref diceInventory, RunInventoryManager.EQUIPPED_DICE_COUNT);
        EnsureLength(ref equippedDice, RunInventoryManager.EQUIPPED_DICE_COUNT);
        EnsureLength(ref skillInventory, RunInventoryManager.OWNED_SKILL_COUNT);
        EnsureLength(ref equippedSkills, RunInventoryManager.OWNED_SKILL_COUNT);
        EnsureLength(ref passiveInventory, RunInventoryManager.PASSIVE_SLOT_COUNT);
        EnsureLength(ref equippedPassive, RunInventoryManager.PASSIVE_SLOT_COUNT);
        EnsureMinLength(ref consumables, RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY);
        EnsureMinLength(ref relicSlots, RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY);
    }

    private static void EnsureLength<T>(ref T[] items, int length)
    {
        if (items != null && items.Length == length)
            return;

        T[] resized = new T[length];
        if (items != null)
        {
            int count = Mathf.Min(items.Length, length);
            for (int i = 0; i < count; i++)
                resized[i] = items[i];
        }

        items = resized;
    }

    private static void EnsureMinLength<T>(ref T[] items, int minLength)
    {
        if (items != null && items.Length >= minLength)
            return;

        EnsureLength(ref items, minLength);
    }

    private static T[] CopyArray<T>(T[] source)
    {
        if (source == null)
            return null;

        T[] copy = new T[source.Length];
        for (int i = 0; i < source.Length; i++)
            copy[i] = source[i];

        return copy;
    }
}

[Serializable]
public struct RunConsumableSlotState
{
    [SerializeField] private ConsumableDataSO asset;
    [SerializeField] private int charges;

    public RunConsumableSlotState(ConsumableDataSO asset, int charges)
    {
        this.asset = asset;
        this.charges = Mathf.Max(0, charges);
    }

    public ConsumableDataSO Asset => asset;
    public int Charges => asset != null ? Mathf.Max(0, charges) : 0;
    public bool IsEmpty => asset == null || Charges <= 0;
}

[Serializable]
public sealed class RunShopState
{
    [SerializeField] private string shopNodeId;
    [SerializeField] private bool bossIntelPurchased;
    [SerializeField] private ScriptableObject[] offeredSkills = Array.Empty<ScriptableObject>();
    [SerializeField] private DiceSpinnerGeneric[] offeredDice = Array.Empty<DiceSpinnerGeneric>();
    [SerializeField] private SkillPassiveSO[] offeredPassives = Array.Empty<SkillPassiveSO>();
    [SerializeField] private RunConsumableSlotState[] offeredConsumables = Array.Empty<RunConsumableSlotState>();

    public string ShopNodeId => shopNodeId;
    public bool BossIntelPurchased => bossIntelPurchased;
    public ScriptableObject[] OfferedSkills => offeredSkills;
    public DiceSpinnerGeneric[] OfferedDice => offeredDice;
    public SkillPassiveSO[] OfferedPassives => offeredPassives;
    public RunConsumableSlotState[] OfferedConsumables => offeredConsumables;

    public void Reset()
    {
        shopNodeId = null;
        bossIntelPurchased = false;
        offeredSkills = Array.Empty<ScriptableObject>();
        offeredDice = Array.Empty<DiceSpinnerGeneric>();
        offeredPassives = Array.Empty<SkillPassiveSO>();
        offeredConsumables = Array.Empty<RunConsumableSlotState>();
    }
}

[Serializable]
public sealed class RunForgeState
{
    [SerializeField] private string forgeNodeId;
    [SerializeField] private int gems;
    [SerializeField] private bool hasPendingEdit;

    public string ForgeNodeId => forgeNodeId;
    public int Gems => gems;
    public bool HasPendingEdit => hasPendingEdit;

    public void Reset()
    {
        forgeNodeId = null;
        gems = 0;
        hasPendingEdit = false;
    }
}

[Serializable]
public sealed class RunRewardQueueState
{
    [SerializeField] private RunRewardState[] rewards = Array.Empty<RunRewardState>();

    public RunRewardState[] Rewards => rewards;
    public bool HasRewards => rewards != null && rewards.Length > 0;

    public void Clear()
    {
        rewards = Array.Empty<RunRewardState>();
    }

    public void SetRewards(RunRewardState[] queuedRewards)
    {
        if (queuedRewards == null)
        {
            Clear();
            return;
        }

        rewards = new RunRewardState[queuedRewards.Length];
        for (int i = 0; i < queuedRewards.Length; i++)
            rewards[i] = queuedRewards[i];
    }
}

[Serializable]
public struct RunRewardState
{
    [SerializeField] private RunRewardKind kind;
    [SerializeField] private ScriptableObject asset;
    [SerializeField] private int amount;
    [SerializeField] private string sourceNodeId;

    public RunRewardState(RunRewardKind kind, ScriptableObject asset, int amount, string sourceNodeId)
    {
        this.kind = kind;
        this.asset = asset;
        this.amount = Mathf.Max(0, amount);
        this.sourceNodeId = sourceNodeId;
    }

    public RunRewardKind Kind => kind;
    public ScriptableObject Asset => asset;
    public int Amount => amount;
    public string SourceNodeId => sourceNodeId;
}

public enum RunRewardKind
{
    None = 0,
    Gold = 1,
    Dice = 2,
    Skill = 3,
    Passive = 4,
    Consumable = 5,
    Relic = 6,
    BossIntel = 7
}
