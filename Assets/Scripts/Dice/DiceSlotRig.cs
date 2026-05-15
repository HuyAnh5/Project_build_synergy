using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

public class DiceSlotRig : MonoBehaviour
{
    public const float GenericCritPercent = 0.20f;
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

        [LabelText("Generic Added")]
        public int genericAddedValue;

        [LabelText("Generic Resolved")]
        public int genericResolvedValue;

        public int Contribution => genericResolvedValue;
    }

    [Serializable]
    public struct ResolvedDieBreakdown
    {
        public int baseValue;
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

    public void BeginNewTurn()
    {
        HasRolledThisTurn = false;
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die != null)
                die.ClearTemporaryTurnEffects();
        }
        ClearRollInfos();
        ApplyActiveStates();
        ClearCombatRollFeedback();
    }

    public void ShowRandomPresentationFaces()
    {
        EnsureSlots();
        ApplyActiveStates();

        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = GetDice(i);
            if (die == null)
                continue;

            die.ShowRandomPresentationFace();
        }
    }

    [Button(ButtonSizes.Medium)]
    public void RollOnce()
    {
        EnsureSlots();
        if (HasRolledThisTurn) return;
        if (IsRolling) return;

        StartCoroutine(RollRoutine());
    }

    public void RollOnceSequential()
    {
        EnsureSlots();
        if (HasRolledThisTurn) return;
        if (IsRolling) return;

        StartCoroutine(RollRoutineSequential());
    }

    public void RollOnceTurnStart()
    {
        EnsureSlots();
        if (HasRolledThisTurn) return;
        if (IsRolling) return;

        StartCoroutine(RollRoutineSimultaneousStart());
    }

    private IEnumerator RollRoutine()
    {
        EnsureSlots();
        ApplyActiveStates();

        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i)) continue;

            DiceSpinnerGeneric d = GetDice(i);
            if (d == null) continue;

            d.RollRandomFace();
        }

        while (IsRolling)
            yield return null;

        HasRolledThisTurn = true;
        CacheRollInfos();
        onAllDiceRolled?.Invoke();
    }

    private IEnumerator RollRoutineSequential()
    {
        EnsureSlots();
        ApplyActiveStates();

        bool rolledAny = false;
        float delay = Mathf.Max(0f, sequentialRollSlotDelay);

        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = GetDice(i);
            if (die == null)
                continue;

            die.RollRandomFace();
            rolledAny = true;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);
        }

        if (!rolledAny)
        {
            HasRolledThisTurn = true;
            CacheRollInfos();
            onAllDiceRolled?.Invoke();
            yield break;
        }

        while (IsRolling)
            yield return null;

        HasRolledThisTurn = true;
        CacheRollInfos();
        onAllDiceRolled?.Invoke();
    }

    private IEnumerator RollRoutineSimultaneousStart()
    {
        EnsureSlots();
        ApplyActiveStates();

        bool rolledAny = false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = GetDice(i);
            if (die == null)
                continue;

            // Turn-start roll uses its own timing profile from code so inspector tweaks on
            // individual dice do not change the opening cascade feel.
            float totalDuration = OpeningRollBaseTotalTime + (OpeningRollFinishStaggerPerSlot * i);
            die.RollRandomFaceWithTiming(OpeningRollAccelTime, totalDuration);
            rolledAny = true;
        }

        if (!rolledAny)
        {
            HasRolledThisTurn = true;
            CacheRollInfos();
            onAllDiceRolled?.Invoke();
            yield break;
        }

        while (IsRolling)
            yield return null;

        HasRolledThisTurn = true;
        CacheRollInfos();
        onAllDiceRolled?.Invoke();
    }

    public bool TryDebugRollSlot(int slot0)
    {
        EnsureSlots();

        if (!enableDebugRollHotkeys)
            return false;
        if (slot0 < 0 || slot0 >= slots.Length)
            return false;
        if (!IsSlotActive(slot0))
            return false;
        if (HasRolledThisTurn && !allowDebugRerollThisTurn)
            return false;

        DiceSpinnerGeneric die = GetDice(slot0);
        if (die == null)
            return false;

        die.RollRandomFace();
        HasRolledThisTurn = true;
        CacheRollInfos();
        onAllDiceRolled?.Invoke();
        return true;
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

    public RollInfo GetRollInfo(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= LastRollInfos.Length) return default;
        return LastRollInfos[slot0];
    }

    public int GetBaseValue(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        return GetRollInfo(slot0).rolledValue;
    }

    public int GetContribution(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        return GetRollInfo(slot0).Contribution;
    }

    public bool IsCrit(int slot0) => GetRollInfo(slot0).isCrit;
    public bool IsFail(int slot0) => GetRollInfo(slot0).isFail;
    public bool AppliesFailPenalty(int slot0) => GetRollInfo(slot0).appliesFailPenalty;
    public bool IsNumericFaceForConditions(int slot0) => GetRollInfo(slot0).isNumericFace;

    public int GetAddedValue(int slot0, CombatActor owner, ElementType skillElement = ElementType.Neutral)
    {
        if (!HasRolledThisTurn) return 0;
        ResolvedDieBreakdown b = GetResolvedBreakdown(slot0, owner, skillElement);
        return b.totalAddedValue;
    }

    public int GetResolvedContribution(int slot0, CombatActor owner)
    {
        return GetResolvedContribution(slot0, owner, ElementType.Neutral);
    }

    public int GetResolvedContribution(int slot0, CombatActor owner, ElementType skillElement)
    {
        if (!HasRolledThisTurn) return 0;
        return GetResolvedBreakdown(slot0, owner, skillElement).resolvedValue;
    }

    public ResolvedDieBreakdown GetResolvedBreakdown(int slot0, CombatActor owner, ElementType skillElement = ElementType.Neutral)
    {
        RollInfo info = GetRollInfo(slot0);
        if (!HasRolledThisTurn) return default;

        int critFailAdded = ComputeCritFailAddedValue(info, skillElement);
        int faceEnchantAdded = ComputeFaceEnchantAddedValue(slot0);
        int passiveAdded = ComputeAllDiceDelta(owner);
        PassiveSystem ps = owner != null ? owner.GetComponent<PassiveSystem>() : null;
        if (ps != null)
            passiveAdded += ps.GetAddedValueForDie(this, slot0);
        int totalAdded = critFailAdded + faceEnchantAdded + passiveAdded;
        int resolved = info.rolledValue + totalAdded;
        if (resolved < 1) resolved = 1;

        return new ResolvedDieBreakdown
        {
            baseValue = info.rolledValue,
            critFailAddedValue = critFailAdded,
            faceEnchantAddedValue = faceEnchantAdded,
            passiveAddedValue = passiveAdded,
            totalAddedValue = totalAdded,
            resolvedValue = resolved,
            isCrit = info.isCrit,
            isFail = info.isFail,
            appliesFailPenalty = info.appliesFailPenalty
        };
    }

    public int ActiveSlotCount()
    {
        EnsureSlots();
        int c = 0;
        for (int i = 0; i < slots.Length; i++)
            if (IsSlotActive(i)) c++;
        return c;
    }

    public bool CanFitAtDrop(int dropSlot0, int requiredSlots)
    {
        EnsureSlots();
        requiredSlots = Mathf.Clamp(requiredSlots, 1, 3);

        if (requiredSlots == 1)
            return IsSlotActive(dropSlot0);

        if (requiredSlots == 2)
        {
            if (dropSlot0 == 0) return IsSlotActive(0) && IsSlotActive(1);
            if (dropSlot0 == 1) return IsSlotActive(1) && IsSlotActive(2);
            if (dropSlot0 == 2) return IsSlotActive(1) && IsSlotActive(2);
            return false;
        }

        return IsSlotActive(0) && IsSlotActive(1) && IsSlotActive(2);
    }

    public void ApplyActiveStates()
    {
        EnsureSlots();
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            Entry e = slots[i];
            if (e == null) continue;

            if (e.diceRoot == null && e.dice != null)
                e.diceRoot = e.dice.gameObject;

            if (disableDiceSelfInput && e.dice != null)
                e.dice.enableSpaceKey = false;

            if (e.diceRoot != null)
                e.diceRoot.SetActive(e.active);

            if (e.slotRoot != null)
                e.slotRoot.SetActive(e.active);
        }
    }

    public int GetResolvedDieValue(int slot0, CombatActor owner)
    {
        return GetResolvedContribution(slot0, owner, ElementType.Neutral);
    }

    public int GetResolvedDieValue(int slot0, CombatActor owner, ElementType skillElement)
    {
        return GetResolvedContribution(slot0, owner, skillElement);
    }

    public int GetMinFaceValue(int slot0)
    {
        DiceSpinnerGeneric d = GetDice(slot0);
        if (d == null) return 1;
        return d.GetMinFaceValue();
    }

    public int GetMaxFaceValue(int slot0)
    {
        DiceSpinnerGeneric d = GetDice(slot0);
        if (d == null) return 6;
        return d.GetMaxFaceValue();
    }

    public SpanStats ComputeSpanStats(int start0, int span, CombatActor owner = null)
    {
        return ComputeSpanStats(start0, span, owner, ElementType.Neutral);
    }

    public SpanStats ComputeSpanStats(int start0, int span, CombatActor owner, ElementType skillElement)
    {
        EnsureSlots();

        span = Mathf.Clamp(span, 1, 3);
        int end0 = Mathf.Min(2, start0 + span - 1);

        int count = 0;
        int sum = 0;
        bool critAny = false;
        bool critAll = true;
        bool failAny = false;

        for (int i = start0; i <= end0; i++)
        {
            if (!IsSlotActive(i)) continue;
            count++;

            RollInfo info = GetRollInfo(i);
            sum += GetResolvedContribution(i, owner, skillElement);
            critAny |= info.isCrit;
            critAll &= info.isCrit;
            failAny |= info.isFail;
        }

        if (count == 0) critAll = false;

        return new SpanStats
        {
            sumContribution = sum,
            critAny = critAny,
            critAll = critAll,
            failAny = failAny
        };
    }

    private int ComputeAllDiceDelta(CombatActor owner)
    {
        int delta = 0;
        if (onComputeAllDiceDelta != null)
        {
            foreach (Func<CombatActor, int> f in onComputeAllDiceDelta.GetInvocationList())
            {
                try { delta += f(owner); }
                catch { }
            }
        }
        return delta;
    }

    private int ComputeCritFailAddedValue(RollInfo info, ElementType skillElement)
    {
        if (info.rolledValue <= 0) return 0;
        if (!info.grantsCritBonus) return 0;
        if (!info.isCrit) return 0;

        float critPercent = (skillElement == ElementType.Physical) ? PhysicalCritPercent : GenericCritPercent;
        return FloorScaled(info.rolledValue, critPercent);
    }

    private int ComputeFaceEnchantAddedValue(int slot0)
    {
        DiceSpinnerGeneric die = GetDice(slot0);
        if (die == null)
            return 0;

        return die.GetCurrentFaceAddedValue();
    }

    private static int FloorScaled(int value, float factor)
    {
        return Mathf.FloorToInt(value * factor);
    }

    private void CacheRollInfos()
    {
        EnsureSlots();
        for (int i = 0; i < 3; i++)
        {
            CacheRollInfoForSlot(i);
        }
    }

    private void CacheRollInfoForSlot(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= LastRollInfos.Length)
            return;

        if (!IsSlotActive(slot0))
        {
            LastRollInfos[slot0] = default;
            return;
        }

        DiceSpinnerGeneric d = GetDice(slot0);
        if (d == null)
        {
            LastRollInfos[slot0] = default;
            return;
        }

        d.GetRollExtents(out int minFace, out int maxFace);
        int rolled = d.GetDisplayedRolledValue();
        DiceFaceEnchantKind faceEnchant = d.GetCurrentFaceEnchant();
        bool isCrit = d.IsCritValue(rolled) || DiceFaceEnchantUtility.CountsAsCritForConditions(faceEnchant);
        bool isFail = d.IsFailValue(rolled) || DiceFaceEnchantUtility.CountsAsFailForConditions(faceEnchant);
        bool grantsCritBonus = isCrit && !DiceFaceEnchantUtility.SuppressesCritBonus(faceEnchant);
        bool appliesFailPenalty = isFail && !DiceFaceEnchantUtility.SuppressesFailPenalty(faceEnchant);
        bool isNumericFace = DiceFaceEnchantUtility.IsNumericFace(faceEnchant);

        int genericAdded = 0;
        if (grantsCritBonus) genericAdded = FloorScaled(rolled, GenericCritPercent);
        genericAdded += DiceFaceEnchantUtility.GetFlatAddedValue(faceEnchant);

        int genericResolved = rolled + genericAdded;
        if (genericResolved < 1) genericResolved = 1;

        LastRollInfos[slot0] = new RollInfo
        {
            rolledValue = rolled,
            minFaceAtRoll = minFace,
            maxFaceAtRoll = maxFace,
            faceEnchant = faceEnchant,
            isCrit = isCrit,
            isFail = isFail,
            grantsCritBonus = grantsCritBonus,
            appliesFailPenalty = appliesFailPenalty,
            isNumericFace = isNumericFace,
            genericAddedValue = genericAdded,
            genericResolvedValue = genericResolved
        };
    }

    private void ClearRollInfos()
    {
        EnsureSlots();
        for (int i = 0; i < LastRollInfos.Length; i++)
            LastRollInfos[i] = default;
    }

    private void BindDieRollCallbacks()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null)
                continue;

            die.onRollComplete -= HandleDieRollComplete;
            die.onRollComplete += HandleDieRollComplete;
        }
    }

    private void HandleDieRollComplete(DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        int slot0 = FindSlotIndex(die);
        if (slot0 < 0)
            return;

        CacheRollInfoForSlot(slot0);

        RollInfo info = LastRollInfos[slot0];
        die.SetCombatRollFeedback(info.isCrit, info.isFail);
    }

    private int FindSlotIndex(DiceSpinnerGeneric die)
    {
        if (die == null || slots == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].dice == die)
                return i;
        }

        return -1;
    }

    private void ClearCombatRollFeedback()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die != null)
                die.SetCombatRollFeedback(false, false);
        }
    }

    public DiceSpinnerGeneric GetDice(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return null;
        return slots[slot0] != null ? slots[slot0].dice : null;
    }

    private static int FindPreviousSlotIndex(DiceSpinnerGeneric[] previousDice, DiceSpinnerGeneric target)
    {
        if (previousDice == null || target == null)
            return -1;

        for (int i = 0; i < previousDice.Length; i++)
        {
            if (previousDice[i] == target)
                return i;
        }

        return -1;
    }

    private static bool IsRootAlreadyUsed(GameObject[] previousRoots, bool[] used, GameObject candidate)
    {
        if (previousRoots == null || used == null || candidate == null)
            return false;

        for (int i = 0; i < previousRoots.Length && i < used.Length; i++)
        {
            if (previousRoots[i] == candidate)
                return used[i];
        }

        return false;
    }

    private static GameObject TakeFirstUnusedRoot(GameObject[] previousRoots, bool[] used)
    {
        if (previousRoots == null || used == null)
            return null;

        for (int i = 0; i < previousRoots.Length && i < used.Length; i++)
        {
            if (previousRoots[i] == null || used[i])
                continue;

            used[i] = true;
            return previousRoots[i];
        }

        return null;
    }

    private void EnsureSlots()
    {
        if (slots == null || slots.Length != 3)
            slots = new Entry[3];

        for (int i = 0; i < 3; i++)
            if (slots[i] == null)
                slots[i] = new Entry();

        if (LastRollInfos == null || LastRollInfos.Length != 3)
            LastRollInfos = new RollInfo[3];
    }

    // ---------------------------
    // Dice Consume Preview
    // ---------------------------

    private bool _consumePreviewActive;
    private int _consumePreviewCount;
    private bool _consumePreviewInvalid; // true = thiếu dice
    private CombatHUD _cachedHud;

    // Cache all DiceDraggableUI instances once per show/clear cycle
    private DiceDraggableUI[] _cachedDiceUIs;

    private DiceDraggableUI FindDiceUI(DiceSpinnerGeneric die)
    {
        if (die == null) return null;
        if (_cachedDiceUIs == null)
            _cachedDiceUIs = UnityEngine.Object.FindObjectsOfType<DiceDraggableUI>(true);
        for (int i = 0; i < _cachedDiceUIs.Length; i++)
        {
            if (_cachedDiceUIs[i] != null && _cachedDiceUIs[i].dice == die)
                return _cachedDiceUIs[i];
        }
        return null;
    }

    /// <summary>
    /// Hiển thị preview dice sẽ bị consume khi hover/drag skill.
    /// diceCount = số dice skill cần (slotsRequired).
    /// spentDice = set dice đã dùng trong turn này, để bỏ qua khi đếm available.
    /// </summary>
    public void ShowConsumePreview(int diceCount, System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice = null)
    {
        EnsureSlots();
        _consumePreviewCount = Mathf.Max(0, diceCount);
        _cachedDiceUIs = UnityEngine.Object.FindObjectsOfType<DiceDraggableUI>(true);

        // Đếm dice available (active + chưa spent)
        int available = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i)) continue;
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null) continue;
            if (spentDice != null && spentDice.Contains(die)) continue;
            available++;
        }

        _consumePreviewInvalid = _consumePreviewCount > available;
        _consumePreviewActive = true;
    }

    /// <summary>
    /// Tắt preview dice consume, trả visual về bình thường.
    /// </summary>
    public void ClearConsumePreview()
    {
        if (!_consumePreviewActive && _cachedDiceUIs == null)
            return;

        _consumePreviewActive = false;
        _consumePreviewCount = 0;
        _consumePreviewInvalid = false;

        // Restore tất cả DiceDraggableUI về trạng thái bình thường
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null) continue;
            DiceDraggableUI ui = FindDiceUI(die);
            if (ui != null)
                ui.ClearPreviewTint();
        }

        _cachedDiceUIs = null;
    }

    /// <summary>
    /// Gọi mỗi frame khi preview đang active.
    /// Cập nhật visual nhấp nháy cho các dice sẽ bị consume.
    /// </summary>
    public void UpdateConsumePreviewVisuals(System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice = null)
    {
        if (!_consumePreviewActive)
            return;

        if (_cachedHud == null)
            _cachedHud = FindObjectOfType<CombatHUD>(true);

        float blinkSpeed = (_cachedHud != null) ? _cachedHud.consumePreviewBlinkSpeed : 3f;
        float minAlpha = (_cachedHud != null) ? _cachedHud.consumePreviewMinAlpha : 0.5f;
        float invalidMinAlpha = (_cachedHud != null) ? _cachedHud.consumePreviewInvalidMinAlpha : 0.6f;

        EnsureSlots();
        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);

        int consumed = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i)) continue;
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null) continue;

            DiceDraggableUI ui = FindDiceUI(die);
            if (ui == null) continue;

            if (_consumePreviewInvalid)
            {
                // Thiếu dice: tất cả dice đỏ nhẹ nhấp nháy, ẩn outline để hiện khối màu đỏ đồng nhất
                float alpha = Mathf.Lerp(invalidMinAlpha, 1f, t);
                ui.SetPreviewTint(new Color(1f, 0.3f, 0.3f, alpha), true);
                continue;
            }

            // Dice đã spent rồi thì skip (nó đã dim 50%)
            if (spentDice != null && spentDice.Contains(die))
                continue;

            if (consumed < _consumePreviewCount)
            {
                // Dice sẽ bị consume: vàng nhấp nháy
                float alpha = Mathf.Lerp(minAlpha, 1f, t);
                ui.SetPreviewTint(new Color(1f, 0.85f, 0.2f, alpha));
                consumed++;
            }
            else
            {
                // Dice không bị consume: bình thường
                ui.ClearPreviewTint();
            }
        }
    }
}
