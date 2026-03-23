using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

public class DiceSlotRig : MonoBehaviour
{
    public const float GenericCritPercent = 0.20f;
    public const float GenericFailPercent = -0.50f;
    public const float PhysicalCritPercent = 0.50f;

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
        public bool isCrit;
        public bool isFail;

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
        public int passiveAddedValue;
        public int totalAddedValue;
        public int resolvedValue;
        public bool isCrit;
        public bool isFail;
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
    }

    private void OnValidate()
    {
        EnsureSlots();
        ApplyActiveStates();
    }

    public void BeginNewTurn()
    {
        HasRolledThisTurn = false;
        EnsureSlots();
        ClearRollInfos();
        ApplyActiveStates();
    }

    [Button(ButtonSizes.Medium)]
    public void RollOnce()
    {
        EnsureSlots();
        if (HasRolledThisTurn) return;
        if (IsRolling) return;

        StartCoroutine(RollRoutine());
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

    public bool IsSlotActive(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return false;

        Entry e = slots[slot0];
        if (e == null) return false;
        if (!e.active) return false;
        if (e.slotRoot != null && !e.slotRoot.activeInHierarchy) return false;
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
        ApplyActiveStates();
    }

    public int GetDieValue(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        DiceSpinnerGeneric d = GetDice(slot0);
        return d != null ? d.LastRolledValue : 0;
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
        int passiveAdded = ComputeAllDiceDelta(owner);
        PassiveSystem ps = owner != null ? owner.GetComponent<PassiveSystem>() : null;
        if (ps != null)
            passiveAdded += ps.GetAddedValueForDie(this, slot0);
        int totalAdded = critFailAdded + passiveAdded;
        int resolved = info.rolledValue + totalAdded;
        if (resolved < 1) resolved = 1;

        return new ResolvedDieBreakdown
        {
            baseValue = info.rolledValue,
            critFailAddedValue = critFailAdded,
            passiveAddedValue = passiveAdded,
            totalAddedValue = totalAdded,
            resolvedValue = resolved,
            isCrit = info.isCrit,
            isFail = info.isFail
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
        if (info.isFail) return FloorScaled(info.rolledValue, GenericFailPercent);
        if (!info.isCrit) return 0;

        float critPercent = (skillElement == ElementType.Physical) ? PhysicalCritPercent : GenericCritPercent;
        return FloorScaled(info.rolledValue, critPercent);
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
            if (!IsSlotActive(i))
            {
                LastRollInfos[i] = default;
                continue;
            }

            DiceSpinnerGeneric d = GetDice(i);
            if (d == null)
            {
                LastRollInfos[i] = default;
                continue;
            }

            d.GetRollExtents(out int minFace, out int maxFace);
            int rolled = d.LastRolledValue;
            bool isCrit = d.IsCritValue(rolled);
            bool isFail = d.IsFailValue(rolled);

            int genericAdded = 0;
            if (isFail) genericAdded = FloorScaled(rolled, GenericFailPercent);
            else if (isCrit) genericAdded = FloorScaled(rolled, GenericCritPercent);

            int genericResolved = rolled + genericAdded;
            if (genericResolved < 1) genericResolved = 1;

            LastRollInfos[i] = new RollInfo
            {
                rolledValue = rolled,
                minFaceAtRoll = minFace,
                maxFaceAtRoll = maxFace,
                isCrit = isCrit,
                isFail = isFail,
                genericAddedValue = genericAdded,
                genericResolvedValue = genericResolved
            };
        }
    }

    private void ClearRollInfos()
    {
        EnsureSlots();
        for (int i = 0; i < LastRollInfos.Length; i++)
            LastRollInfos[i] = default;
    }

    private DiceSpinnerGeneric GetDice(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return null;
        return slots[slot0] != null ? slots[slot0].dice : null;
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
}
