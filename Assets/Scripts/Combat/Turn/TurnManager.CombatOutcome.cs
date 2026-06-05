public partial class TurnManager
{
    private bool TryHandleCombatVictory()
    {
        if (_victoryResolvedThisCombat)
            return true;

        if (!IsCombatWon())
            return false;

        _victoryResolvedThisCombat = true;

        if (diceRig != null)
            DiceCombatEnchantRuntimeUtility.ApplyVictoryWholeDieEffects(diceRig);

        SetPhase(Phase.Planning);
        RefreshAllViews();
        RefreshPlanningInteractivity();
        CombatVictoryResolved?.Invoke();
        return true;
    }

    private bool TryHandleCombatDefeat()
    {
        if (_defeatResolvedThisCombat)
            return true;

        if (!IsCombatLost())
            return false;

        _defeatResolvedThisCombat = true;
        SetPhase(Phase.EnemyTurn);
        LockPlanningUI(true);
        RefreshAllViews();
        return true;
    }

    private bool IsCombatWon()
    {
        if (IsCombatLost())
            return false;

        return TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy).Count <= 0;
    }

    private bool IsCombatLost()
    {
        return player == null || player.IsDead;
    }

    public void SetPlayerInteractionLocked(bool locked)
    {
        if (_externalPlayerInteractionLock == locked)
            return;

        _externalPlayerInteractionLock = locked;
        RefreshPlanningInteractivity();
    }
}
