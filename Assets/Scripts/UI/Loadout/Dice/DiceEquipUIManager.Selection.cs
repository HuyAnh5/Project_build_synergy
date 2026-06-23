using System.Collections.Generic;
using UnityEngine;

public partial class DiceEquipUIManager
{
    public bool TryGetSelectedDice(out DiceSpinnerGeneric dice)
    {
        dice = _selectedDice.Count == 1 ? _selectedDice[0].dice : null;
        return dice != null;
    }

    public int GetSelectedDiceCount()
    {
        PruneSelection();
        return _selectedDice.Count;
    }

    public void GetSelectedDice(List<DiceSpinnerGeneric> buffer)
    {
        if (buffer == null)
            return;

        buffer.Clear();
        PruneSelection();
        for (int i = 0; i < _selectedDice.Count; i++)
        {
            DiceSpinnerGeneric dice = _selectedDice[i] != null ? _selectedDice[i].dice : null;
            if (dice != null)
                buffer.Add(dice);
        }
    }

    public void ClearSelectedDice(bool instant = false)
    {
        ClearDiceSelection(instant);
    }

    public void ClearSelectedDice(
        IEnumerable<DiceSpinnerGeneric> diceBuffer,
        bool instant = false,
        bool suppressMove = false)
    {
        if (diceBuffer == null)
        {
            ClearDiceSelection(instant);
            return;
        }

        PruneSelection();

        bool changed = false;
        foreach (DiceSpinnerGeneric die in diceBuffer)
        {
            if (die == null)
                continue;

            if (!_uiByDice.TryGetValue(die, out DiceDraggableUI diceUi) || diceUi == null)
                continue;

            if (!_selectedDice.Remove(diceUi))
                continue;

            diceUi.SetSelected(false, instant, suppressMove);
            changed = true;
        }

        if (changed)
            SelectionChanged?.Invoke();
    }

    public void PlayInvalidFeedbackForDice(IEnumerable<DiceSpinnerGeneric> diceBuffer)
    {
        if (diceBuffer == null)
            return;

        foreach (DiceSpinnerGeneric die in diceBuffer)
        {
            if (die == null)
                continue;

            if (_uiByDice.TryGetValue(die, out DiceDraggableUI diceUi) && diceUi != null)
                diceUi.PlayInvalidSelectionFeedback();
        }
    }
}
