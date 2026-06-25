using System.Collections;
using UnityEngine;

public partial class TurnManager
{
    private void BeginNewPlayerTurn()
    {
        if (TryHandleCombatDefeat())
            return;

        SetPhase(Phase.Planning);
        _playerContext.Bind(player);
        _playerContext.PassiveSystem?.OnTurnStarted();
        if (_playerContext.SkillCombatState != null)
            _playerContext.SkillCombatState.BeginPlayerTurn();
        TurnManagerLifecycleUtility.BeginPlayerTurnStatusesAndFocus(player, logPhase, this);

        _spentDiceThisTurn.Clear();
        _pendingUsedVisualDiceThisTurn.Clear();
        _board.Reset();
        _queuedPlayerCommands.Clear();
        _isProcessingQueuedPlayerCommands = false;
        _endTurnQueued = false;

        if (diceRig != null)
        {
            RestoreBaselineSlots();
            ApplySlotCollapseToRig();
            diceRig.BeginNewTurn();
        }

        if (executor != null)
            executor.ResetPlayerCastVisualState();

        RefreshAllViews();
        RefreshPlanningInteractivity();
        LockPlanningUI(false);

        if (_autoRollCoroutine != null)
            StopCoroutine(_autoRollCoroutine);
        _autoRollCoroutine = StartCoroutine(AutoRollPlayerTurnIfNeeded());

        // ? Ensure enemy has intent for THIS upcoming enemy turn (STS style)
        TurnManagerViewUtility.EnsureEnemyIntentsNow(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy), player);
    }

    private IEnumerator AutoRollPlayerTurnIfNeeded()
    {
        yield return null;

        if (!autoRollOnPlayerTurnStart)
            yield break;
        if (phase != Phase.Planning)
            yield break;
        if (diceRig == null)
            yield break;
        if (ArePlayerCommandsLocked)
            yield break;
        if (diceRig.HasRolledThisTurn || diceRig.IsRolling)
            yield break;

        // Turn-start auto roll uses the dedicated opening profile on the rig.
        // Manual roll button and other rerolls still keep their normal timing path.
        diceRig.RollOnceTurnStart();
        RefreshPlanningInteractivity();
    }

    private void OnDiceRolled()
    {
        _playerContext.Bind(player);
        if (_playerContext.PassiveSystem != null)
            _playerContext.PassiveSystem.OnDiceRolled(player, diceRig);

        MarkBrokenRolledFacesSpent();
        _board.RecalculateRuntimesAndRebalance(player, diceRig);
        RefreshAllViews();
        if (diceRig != null && diceRig.HasAnySkillAffectingRollFeedbackThisTurn())
            DraggableSkillIcon.PulseAffectedSkillIconsOnce(this);
        RefreshPlanningInteractivity();
    }

    private void RefreshPlanningInteractivity()
    {
        if (phase != Phase.Planning)
        {
            if (ArePlayerCommandsLocked)
                LockPlanningUI(true);
            return;
        }

        bool shouldLock = ArePlayerCommandsLocked || _endTurnQueued || (lockPlanningUIUntilRolled && !CanInteractWithSkills);
        LockPlanningUI(shouldLock);
        RefreshContinueButtonUi();
    }

    private void LockPlanningUI(bool locked)
    {
        if (skillBarGroup)
        {
            skillBarGroup.interactable = !locked;
            skillBarGroup.blocksRaycasts = !locked;
        }
        if (slotsPanelGroup)
        {
            slotsPanelGroup.interactable = !locked;
            slotsPanelGroup.blocksRaycasts = !locked;
        }
    }

    private int ComputeAllDiceDelta(CombatActor owner)
    {
        if (owner == null || owner.status == null) return 0;
        return owner.status.GetAllDiceDelta();
    }

    private void EndPlayerTurn_TickStatusesAndPassives()
        => TurnManagerLifecycleUtility.EndPlayerTurnTickStatusesAndPassives(player, logPhase, this);

    private void ApplyPlayerSlotDebuffs()
        => TurnManagerLifecycleUtility.ApplyPlayerSlotDebuffs(diceRig, player, _baseSlotActive, ref _slotCollapseKeepIndex, logPhase, this);

    private void RestoreBaselineSlots()
        => TurnManagerLifecycleUtility.RestoreBaselineSlots(diceRig, _baseSlotActive, ref _slotCollapseKeepIndex);

    private void ApplySlotCollapseToRig()
        => TurnManagerLifecycleUtility.ApplySlotCollapseToRig(diceRig, player, ref _slotCollapseKeepIndex, logPhase, this);
}
