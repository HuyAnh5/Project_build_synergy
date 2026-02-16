using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// KEEP legacy behaviour + ADD new APIs for the new skill system.
/// Fixes common "can't roll after refactor" by auto-creating null entries
/// and bootstrapping from legacy dice1/dice2/dice3 fields if present.
/// </summary>
public class DiceSlotRig : MonoBehaviour
{
    [Serializable]
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

    [Header("Slots (1..3)")]
    public Entry[] slots = new Entry[3];

    // -----------------------------
    // Legacy fields (optional)
    // If older prefabs/scenes used these, Unity can rebind them when you paste this file.
    // -----------------------------
    [Header("Legacy (optional) — kept for prefab/scene migration")]
    public DiceSpinnerGeneric dice1;
    public DiceSpinnerGeneric dice2;
    public DiceSpinnerGeneric dice3;

    public GameObject diceRoot1;
    public GameObject diceRoot2;
    public GameObject diceRoot3;

    public GameObject slotRoot1;
    public GameObject slotRoot2;
    public GameObject slotRoot3;

    public bool slot0Active = true;
    public bool slot1Active = true;
    public bool slot2Active = true;

    [Header("Options")]
    [Tooltip("Disable DiceSpinnerGeneric self input so ONLY TurnManager controls rolling.")]
    public bool disableDiceSelfInput = true;

    public bool HasRolledThisTurn { get; private set; } = false;

    public bool IsRolling
    {
        get
        {
            EnsureSlotsCreatedAndBootstrapped();
            for (int i = 0; i < 3; i++)
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
    /// NEW (Batch 3+): dice-layer delta from statuses/buffs (e.g., AllDiceDelta debuff).
    /// Subscribers return a delta; values are summed.
    /// </summary>
    public event Func<CombatActor, int> onComputeAllDiceDelta;

    void Awake()
    {
        EnsureSlotsCreatedAndBootstrapped();
        ApplyActiveStates();
    }

    void OnValidate()
    {
        EnsureSlotsCreatedAndBootstrapped();
        ApplyActiveStates();
    }

    public void BeginNewTurn()
    {
        HasRolledThisTurn = false;
        EnsureSlotsCreatedAndBootstrapped();
        ApplyActiveStates();
    }

    public void RollOnce()
    {
        EnsureSlotsCreatedAndBootstrapped();
        if (HasRolledThisTurn) return;
        if (IsRolling) return;

        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        EnsureSlotsCreatedAndBootstrapped();
        ApplyActiveStates();

        // Kick roll on all ACTIVE dice
        for (int i = 0; i < 3; i++)
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
        EnsureSlotsCreatedAndBootstrapped();
        if (slots == null || slot0 < 0 || slot0 > 2) return false;
        var e = slots[slot0];
        if (e == null) return false;
        if (!e.active) return false;

        // if user provided a slotRoot, it must be active too
        if (e.slotRoot != null && !e.slotRoot.activeInHierarchy) return false;

        return true;
    }

    public int GetDieValue(int slot0)
    {
        if (!HasRolledThisTurn) return 0;
        var d = GetDice(slot0);
        return d != null ? d.LastRolledValue : 0;
    }

    public int ActiveSlotCount()
    {
        int c = 0;
        for (int i = 0; i < 3; i++)
            if (IsSlotActive(i)) c++;
        return c;
    }

    public bool CanFitAtDrop(int dropSlot0, int requiredSlots)
    {
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
        EnsureSlotsCreatedAndBootstrapped();
        if (slots == null) return;

        for (int i = 0; i < Mathf.Min(3, slots.Length); i++)
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
    // NEW APIs (Batch 3): resolved die value + debuff layer
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

    // Back-compat alias
    public int GetEffectiveDieValue(int slot0, CombatActor owner) => GetResolvedDieValue(slot0, owner);

    // Legacy overload KEEP
    public int GetEffectiveDieValue(int slot0, int allDiceDelta)
    {
        int raw = GetDieValue(slot0);
        if (raw <= 0) return raw;
        int v = raw + allDiceDelta;
        if (v < 0) v = 0;
        return v;
    }

    public int GetMaxFaceValue(int slot0)
    {
        var d = GetDice(slot0);
        if (d == null || d.faces == null || d.faces.Length == 0) return 6;
        int max = 0;
        for (int i = 0; i < d.faces.Length; i++)
        {
            if (d.faces[i].value > max) max = d.faces[i].value;
        }
        return Mathf.Max(1, max);
    }

    private DiceSpinnerGeneric GetDice(int slot0)
    {
        EnsureSlotsCreatedAndBootstrapped();
        if (slots == null || slot0 < 0 || slot0 > 2) return null;
        return slots[slot0] != null ? slots[slot0].dice : null;
    }

    private void EnsureSlotsCreatedAndBootstrapped()
    {
        if (slots == null || slots.Length != 3)
            slots = new Entry[3];

        for (int i = 0; i < 3; i++)
        {
            if (slots[i] == null)
                slots[i] = new Entry();
        }

        // bootstrap dice refs from legacy fields
        if (slots[0].dice == null && dice1 != null) slots[0].dice = dice1;
        if (slots[1].dice == null && dice2 != null) slots[1].dice = dice2;
        if (slots[2].dice == null && dice3 != null) slots[2].dice = dice3;

        if (slots[0].diceRoot == null && diceRoot1 != null) slots[0].diceRoot = diceRoot1;
        if (slots[1].diceRoot == null && diceRoot2 != null) slots[1].diceRoot = diceRoot2;
        if (slots[2].diceRoot == null && diceRoot3 != null) slots[2].diceRoot = diceRoot3;

        if (slots[0].slotRoot == null && slotRoot1 != null) slots[0].slotRoot = slotRoot1;
        if (slots[1].slotRoot == null && slotRoot2 != null) slots[1].slotRoot = slotRoot2;
        if (slots[2].slotRoot == null && slotRoot3 != null) slots[2].slotRoot = slotRoot3;

        // bootstrap actives
        slots[0].active = slots[0].active && slot0Active;
        slots[1].active = slots[1].active && slot1Active;
        slots[2].active = slots[2].active && slot2Active;
    }
}
