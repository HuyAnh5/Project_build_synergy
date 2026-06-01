public partial class TurnManager
{
    public System.Collections.Generic.IReadOnlyCollection<DiceSpinnerGeneric> SpentDiceThisTurn => _spentDiceThisTurn;

    public bool IsDieSpentThisTurn(DiceSpinnerGeneric die)
        => die != null && _spentDiceThisTurn.Contains(die);

    public bool IsDiePendingUsedVisualThisTurn(DiceSpinnerGeneric die)
        => die != null && _pendingUsedVisualDiceThisTurn.Contains(die);

    public bool ShouldShowDieAsSpentVisual(DiceSpinnerGeneric die)
        => die != null && _spentDiceThisTurn.Contains(die) && !_pendingUsedVisualDiceThisTurn.Contains(die);

    public bool ShouldDimDieAsSpent(DiceSpinnerGeneric die)
        => ShouldShowDieAsSpentVisual(die);

    public bool RestoreDieToAvailableThisTurn(DiceSpinnerGeneric die)
    {
        if (die == null)
            return false;

        bool removed = _spentDiceThisTurn.Remove(die);
        if (!removed)
            return false;

        _pendingUsedVisualDiceThisTurn.Remove(die);

        if (diceRig != null)
            diceRig.RefreshRollInfoCache();

        if (IsPlanning)
        {
            _board.RecalculateRuntimesAndRebalance(player, diceRig);
            RefreshAllViews();
            RefreshPlanningInteractivity();
        }

        return true;
    }

    public void RefreshPlanningAfterDiceAvailabilityChanged()
    {
        if (diceRig != null)
            diceRig.RefreshRollInfoCache();

        if (IsPlanning)
        {
            _board.RecalculateRuntimesAndRebalance(player, diceRig);
            RefreshAllViews();
            RefreshPlanningInteractivity();
        }
    }

    public void RefreshPlanningAfterDiceAvailabilityChanged(DiceSpinnerGeneric die)
    {
        if (die != null)
            die.onRollComplete -= RefreshPlanningAfterDiceAvailabilityChanged;
        RefreshPlanningAfterDiceAvailabilityChanged();
    }

    private void FinalizePendingUsedVisualRange(int start0, int span, int paymentMask = -1)
    {
        if (diceRig == null || diceRig.slots == null)
            return;

        for (int i = start0; i < start0 + span && i < diceRig.slots.Length; i++)
        {
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0)
                continue;
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die != null)
                _pendingUsedVisualDiceThisTurn.Remove(die);
        }
    }
}
