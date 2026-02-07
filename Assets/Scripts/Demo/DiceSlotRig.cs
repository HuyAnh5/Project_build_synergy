using System;
using System.Collections;
using UnityEngine;

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

    [Header("Options")]
    [Tooltip("Disable DiceSpinnerGeneric self input so ONLY TurnManager controls rolling.")]
    public bool disableDiceSelfInput = true;

    public bool HasRolledThisTurn { get; private set; } = false;

    public bool IsRolling
    {
        get
        {
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

    void Awake()
    {
        ApplyActiveStates();
    }

    void OnValidate()
    {
        ApplyActiveStates();
    }

    public void BeginNewTurn()
    {
        HasRolledThisTurn = false;
        ApplyActiveStates();
    }

    public void RollOnce()
    {
        if (HasRolledThisTurn) return;
        if (IsRolling) return;

        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
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

    /// <summary>
    /// Check if a skill requiring N slots can fit starting at drop slot.
    /// 1-slot: requires that slot active.
    /// 2-slot: requires consecutive active slots (1-2 or 2-3).
    /// 3-slot: requires 1-2-3 all active.
    /// </summary>
    public bool CanFitAtDrop(int dropSlot0, int requiredSlots)
    {
        requiredSlots = Mathf.Clamp(requiredSlots, 1, 3);

        if (requiredSlots == 1)
            return IsSlotActive(dropSlot0);

        if (requiredSlots == 2)
        {
            // valid starts: 0 or 1
            if (dropSlot0 == 0) return IsSlotActive(0) && IsSlotActive(1);
            if (dropSlot0 == 1) return IsSlotActive(1) && IsSlotActive(2);
            if (dropSlot0 == 2) return IsSlotActive(1) && IsSlotActive(2); // treat dropping on 3 as (2-3)
            return false;
        }

        // requiredSlots == 3
        return IsSlotActive(0) && IsSlotActive(1) && IsSlotActive(2);
    }

    public void ApplyActiveStates()
    {
        if (slots == null) return;

        for (int i = 0; i < Mathf.Min(3, slots.Length); i++)
        {
            var e = slots[i];
            if (e == null) continue;

            // auto assign diceRoot if empty
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

    private DiceSpinnerGeneric GetDice(int slot0)
    {
        if (slots == null || slot0 < 0 || slot0 > 2) return null;
        return slots[slot0] != null ? slots[slot0].dice : null;
    }
}
