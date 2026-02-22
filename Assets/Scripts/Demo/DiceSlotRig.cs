using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

public class DiceSlotRig : MonoBehaviour
{
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


    [ListDrawerSettings(Expanded = true, DraggableItems = false, ShowIndexLabels = false)]
    [LabelText("Slots (1..3)")]
    public Entry[] slots = new Entry[3];


    [Title("Options")]
    [Tooltip("Disable DiceSpinnerGeneric self input so ONLY TurnManager controls rolling.")]
    public bool disableDiceSelfInput = true;

    [ShowInInspector, ReadOnly]
    public bool HasRolledThisTurn { get; private set; } = false;

    [ShowInInspector, ReadOnly]
    public bool IsRolling
    {
        get
        {
            EnsureSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                if (!IsSlotActive(i)) continue;
                var d = GetDice(i);
                if (d != null && d.IsRolling) return true;
            }
            return false;
        }
    }

    public event Action onAllDiceRolled;

    /// <summary>
    /// Dice-layer delta from statuses/buffs (e.g., -2 All Dice for 1 turn).
    /// Subscribers return delta; values are summed.
    /// </summary>
    public event Func<CombatActor, int> onComputeAllDiceDelta;

    private void Awake()
    {
        EnsureSlots();
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

        // Kick roll on all ACTIVE dice
        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i)) continue;

            var d = GetDice(i);
            if (d == null) continue;

            d.RollRandomFace();
        }

        // Wait until all done (animation complete)
        while (IsRolling)
            yield return null;

        HasRolledThisTurn = true;
        onAllDiceRolled?.Invoke();
    }

    public bool IsSlotActive(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return false;

        var e = slots[slot0];
        if (e == null) return false;
        if (!e.active) return false;

        // if user provided a slotRoot, it must be active too
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

    public int GetDieValue(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        var d = GetDice(slot0);
        return d != null ? d.LastRolledValue : 0;
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
            var e = slots[i];
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

    // -----------------------------
    // Resolved value with debuff layer
    // -----------------------------
    public int GetResolvedDieValue(int slot0, CombatActor owner)
    {
        int raw = GetDieValue(slot0);
        if (raw <= 0) return raw;

        int delta = 0;
        if (onComputeAllDiceDelta != null)
        {
            foreach (Func<CombatActor, int> f in onComputeAllDiceDelta.GetInvocationList())
            {
                try { delta += f(owner); }
                catch { }
            }
        }

        int v = raw + delta;
        if (v < 1) v = 1;
        return v;
    }

    public int GetMaxFaceValue(int slot0)
    {
        var d = GetDice(slot0);
        if (d == null || d.faces == null || d.faces.Length == 0) return 6;

        int max = 0;
        for (int i = 0; i < d.faces.Length; i++)
            if (d.faces[i].value > max) max = d.faces[i].value;

        return Mathf.Max(1, max);
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
    }
}
