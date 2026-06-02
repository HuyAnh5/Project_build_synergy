using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

public partial class DiceSlotRig : MonoBehaviour
{
    public const float GenericCritPercent = 0.30f;
    public const float GenericFailPercent = 0.00f;
    public const float PhysicalCritPercent = 0.50f;
    private const float OpeningRollAccelTime = 0.10f;
    private const float OpeningRollBaseTotalTime = 1.25f;
    private const float OpeningRollFinishStaggerPerSlot = 0.75f;

    [Serializable]
    [InlineProperty, HideLabel]
    public class Entry
    {
        public bool active = true;

        [Tooltip("Dice spinner for this slot (1-1 mapping).")]
        public DiceSpinnerGeneric dice;

        [Tooltip("Optional: root object for dice visuals (leave null to use dice.gameObject).")]
        public GameObject diceRoot;

        [Tooltip("Optional: root object for the matching skill slot UI (Slot1/Slot2/Slot3).")]
        public GameObject slotRoot;
    }

    [Serializable]
    public struct RollInfo
    {
        public int rolledValue;
        public int minFaceAtRoll;
        public int maxFaceAtRoll;
        public DiceFaceEnchantKind faceEnchant;
        public bool isCrit;
        public bool isFail;
        public bool grantsCritBonus;
        public bool appliesFailPenalty;
        public bool isNumericFace;
        public bool isBrokenFace;
        public bool isUsable;

        [LabelText("Generic Added")]
        public int genericAddedValue;

        [LabelText("Generic Resolved")]
        public int genericResolvedValue;

        public int Contribution => isUsable ? genericResolvedValue : 0;
    }

    [Serializable]
    public struct ResolvedDieBreakdown
    {
        public int baseValue;
        public int outputBaseValue;
        public int critFailAddedValue;
        public int faceEnchantAddedValue;
        public int passiveAddedValue;
        public int totalAddedValue;
        public int resolvedValue;
        public bool isCrit;
        public bool isFail;
        public bool appliesFailPenalty;
    }

    [Serializable]
    public struct SpanStats
    {
        public int sumContribution;
        public bool critAny;
        public bool critAll;
        public bool failAny;
    }

    [ListDrawerSettings(Expanded = true, DraggableItems = false, ShowIndexLabels = false)]
    [LabelText("Slots (1..3)")]
    public Entry[] slots = new Entry[3];

    [Title("Options")]
    [Tooltip("Disable DiceSpinnerGeneric self input so ONLY TurnManager controls rolling.")]
    public bool disableDiceSelfInput = true;

    [Title("Debug/Test")]
    [Tooltip("Enables debug keyboard roll controls on this rig: Space = roll all, A/S/D = reroll slot 1/2/3.")]
    public bool enableDebugRollHotkeys = false;
    [Tooltip("If enabled, A/S/D can reroll a slot even after the rig has already rolled once this turn.")]
    public bool allowDebugRerollThisTurn = false;
    public KeyCode debugRollAllKey = KeyCode.Space;
    public KeyCode debugRollSlot1Key = KeyCode.A;
    public KeyCode debugRollSlot2Key = KeyCode.S;
    public KeyCode debugRollSlot3Key = KeyCode.D;

    [Title("Auto Roll")]
    [Tooltip("Delay between starting each active slot roll during player-turn auto roll.")]
    [Min(0f)] public float sequentialRollSlotDelay = 0.5f;

    [ShowInInspector, ReadOnly]
    public bool HasRolledThisTurn { get; private set; }

    [ShowInInspector, ReadOnly]
    public RollInfo[] LastRollInfos { get; private set; } = new RollInfo[3];

    [ShowInInspector, ReadOnly]
    public bool IsRolling
    {
        get
        {
            EnsureSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                if (!IsSlotActive(i)) continue;
                DiceSpinnerGeneric d = GetDice(i);
                if (d != null && d.IsRolling) return true;
            }
            return false;
        }
    }

    public event Action onAllDiceRolled;
    public event Func<CombatActor, int> onComputeAllDiceDelta;

    private void Awake()
    {
        EnsureSlots();
        ClearRollInfos();
        ApplyActiveStates();
        BindDieRollCallbacks();
    }

    private void OnValidate()
    {
        EnsureSlots();
        ApplyActiveStates();
        BindDieRollCallbacks();
    }

    private void Update()
    {
        if (!enableDebugRollHotkeys)
            return;

        if (Input.GetKeyDown(debugRollAllKey))
        {
            TryDebugRollAll();
            return;
        }

        if (Input.GetKeyDown(debugRollSlot1Key))
        {
            TryDebugRollSlot(0);
            return;
        }

        if (Input.GetKeyDown(debugRollSlot2Key))
        {
            TryDebugRollSlot(1);
            return;
        }

        if (Input.GetKeyDown(debugRollSlot3Key))
            TryDebugRollSlot(2);
    }
    public bool TryDebugRollAll()
    {
        EnsureSlots();

        if (!enableDebugRollHotkeys)
            return false;
        if (HasRolledThisTurn && !allowDebugRerollThisTurn)
            return false;

        ApplyActiveStates();

        bool rolledAny = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = GetDice(i);
            if (die == null)
                continue;

            die.RollRandomFace();
            rolledAny = true;
        }

        if (!rolledAny)
            return false;

        HasRolledThisTurn = true;
        CacheRollInfos();
        onAllDiceRolled?.Invoke();
        return true;
    }

    public void RefreshRollInfoCache()
    {
        EnsureSlots();
        if (!HasRolledThisTurn)
            return;

        CacheRollInfos();
    }

    public bool IsSlotActive(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return false;

        Entry e = slots[slot0];
        if (e == null) return false;
        if (!e.active) return false;
        return true;
    }

    public void SetSlotActive(int slot0, bool on)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return;
        slots[slot0].active = on;
        ApplyActiveStates();
    }

    public void AssignDiceToSlot(int slot0, DiceSpinnerGeneric dice)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return;

        Entry entry = slots[slot0];
        DiceSpinnerGeneric previousDice = entry.dice;
        entry.dice = dice;

        // Keep custom inspector-assigned roots intact, but refresh auto-bound roots when the die changes.
        bool rootWasAutoBound =
            entry.diceRoot == null ||
            (previousDice != null && entry.diceRoot == previousDice.gameObject);

        if (rootWasAutoBound)
            entry.diceRoot = dice != null ? dice.gameObject : null;

        slots[slot0] = entry;
        ApplyActiveStates();
        BindDieRollCallbacks();
    }

    public void ApplyDiceLayout(DiceSpinnerGeneric[] layout)
    {
        EnsureSlots();
        if (layout == null)
            return;

        DiceSpinnerGeneric[] previousDice = new DiceSpinnerGeneric[slots.Length];
        GameObject[] previousDiceRoots = new GameObject[slots.Length];
        GameObject[] previousSlotRoots = new GameObject[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            Entry entry = slots[i];
            previousDice[i] = entry != null ? entry.dice : null;
            previousDiceRoots[i] = entry != null ? entry.diceRoot : null;
            previousSlotRoots[i] = entry != null ? entry.slotRoot : null;
        }

        bool[] slotRootUsed = new bool[slots.Length];
        bool[] diceRootUsed = new bool[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            Entry entry = slots[i] ?? new Entry();
            DiceSpinnerGeneric nextDice = i < layout.Length ? layout[i] : null;
            entry.dice = nextDice;

            if (nextDice == null)
            {
                slots[i] = entry;
                continue;
            }

            int previousIndex = FindPreviousSlotIndex(previousDice, nextDice);
            if (previousIndex >= 0)
            {
                GameObject mappedDiceRoot = previousDiceRoots[previousIndex];
                if (mappedDiceRoot == null || diceRootUsed[previousIndex])
                {
                    mappedDiceRoot = nextDice.gameObject;
                }
                else
                {
                    diceRootUsed[previousIndex] = true;
                }

                entry.diceRoot = mappedDiceRoot;

                GameObject mappedSlotRoot = previousSlotRoots[previousIndex];
                if (mappedSlotRoot != null && !slotRootUsed[previousIndex])
                {
                    entry.slotRoot = mappedSlotRoot;
                    slotRootUsed[previousIndex] = true;
                }
                else if (entry.slotRoot == null || IsRootAlreadyUsed(previousSlotRoots, slotRootUsed, entry.slotRoot))
                {
                    entry.slotRoot = TakeFirstUnusedRoot(previousSlotRoots, slotRootUsed);
                }
            }
            else
            {
                entry.diceRoot = nextDice.gameObject;
                if (entry.slotRoot == null || IsRootAlreadyUsed(previousSlotRoots, slotRootUsed, entry.slotRoot))
                    entry.slotRoot = TakeFirstUnusedRoot(previousSlotRoots, slotRootUsed);
            }

            slots[i] = entry;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            Entry entry = slots[i] ?? new Entry();
            if (entry.dice != null)
                continue;

            entry.diceRoot = null;
            if (entry.slotRoot == null || IsRootAlreadyUsed(previousSlotRoots, slotRootUsed, entry.slotRoot))
                entry.slotRoot = TakeFirstUnusedRoot(previousSlotRoots, slotRootUsed);

            slots[i] = entry;
        }

        ApplyActiveStates();
        BindDieRollCallbacks();
    }

    public void SwapDiceSlots(int a, int b)
    {
        EnsureSlots();
        if (a < 0 || a >= slots.Length) return;
        if (b < 0 || b >= slots.Length) return;
        if (a == b) return;

        DiceSpinnerGeneric tmpDice = slots[a].dice;
        slots[a].dice = slots[b].dice;
        slots[b].dice = tmpDice;

        GameObject tmpRoot = slots[a].diceRoot;
        slots[a].diceRoot = slots[b].diceRoot;
        slots[b].diceRoot = tmpRoot;

        GameObject tmpSlotRoot = slots[a].slotRoot;
        slots[a].slotRoot = slots[b].slotRoot;
        slots[b].slotRoot = tmpSlotRoot;
        ApplyActiveStates();
        BindDieRollCallbacks();
    }

    public int GetDieValue(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        DiceSpinnerGeneric d = GetDice(slot0);
        return d != null ? d.GetDisplayedRolledValue() : 0;
    }

}


