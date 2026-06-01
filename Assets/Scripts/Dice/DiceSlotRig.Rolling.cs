using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

public partial class DiceSlotRig
{
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

            // Temporarily disable opening finish stagger so every die resolves with the same timing.
            float totalDuration = OpeningRollBaseTotalTime;
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

}


