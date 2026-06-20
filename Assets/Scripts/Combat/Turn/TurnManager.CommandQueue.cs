using System.Collections;
using UnityEngine;

public partial class TurnManager
{
    public void OnContinue()
    {
        if (!IsPlanning) return;
        if (ArePlayerCommandsLocked) return;
        if (_endTurnQueued) return;

        EnqueueEndTurn();
    }

    public void OnTargetClicked(CombatActor clicked)
    {
        if (ArePlayerCommandsLocked)
            return;

        if (logPhase)
            Debug.Log($"[TM] OnTargetClicked phase={phase} cursor={_cursor} clicked={(clicked ? clicked.name : "NULL")}", this);

        if (phase == Phase.Planning)
        {
            _cursor = FindNextExecutableAnchor();
            if (_cursor < 0)
                return;
        }
        else if (phase != Phase.AwaitTarget)
        {
            return;
        }

        if (!TryValidateTargetForPendingSkill(clicked, out string reason))
        {
            if (logPhase)
            {
                var asset = _board.GetCellSkillAsset(_cursor);
                Debug.LogWarning($"[TM] Target INVALID: {reason} | asset={(asset ? asset.name : "NULL")} type={(asset ? asset.GetType().Name : "NULL")}", this);
            }
            return;
        }

        EnqueueCurrentExecution(clicked);
    }

    private void EnqueueCurrentExecution(CombatActor clicked)
    {
        var asset = _board.GetCellSkillAsset(_cursor);
        var rt = _board.GetAnchorRuntime(_cursor);

        if (rt == null)
        {
            Debug.LogError($"[TM] AnchorRuntime NULL at cursor={_cursor}, asset={(asset ? asset.name : "NULL")}.", this);
            SetPhase(Phase.Planning);
            return;
        }

        if (executor == null)
        {
            Debug.LogError("[TM] Missing SkillExecutor reference!", this);
            SetPhase(Phase.Planning);
            return;
        }

        int start0 = _board.GetStartForAnchor(_cursor);
        int span = Mathf.Clamp(rt.slotsRequired, 1, 3);
        int rawSum = _board.GetDieSumForAnchor(_cursor, diceRig);
        DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan paymentPlan =
            DiceCombatEnchantRuntimeUtility.BuildPaymentPlan(diceRig, start0, span);
        if (span > 0 && paymentPlan.paidCost < span)
        {
            if (logPhase)
                Debug.LogWarning("[TM] No usable dice face available to pay this skill cost.", this);
            SetPhase(Phase.Planning);
            RefreshAllViews();
            RefreshPlanningInteractivity();
            return;
        }

        SkillRuntime paymentRuntime = SkillPlanRuntimeUtility.EvaluateRuntimeForSkillAsset(
            asset,
            player,
            diceRig,
            _cursor,
            span,
            start0,
            paymentPlan.selectedMask);
        if (paymentRuntime != null)
            rt = paymentRuntime;

        if (!TurnManagerTargetingUtility.TryValidateTargetForPendingSkill(rt, clicked, player, party, enemy, out string finalValidationReason))
        {
            if (logPhase)
                Debug.LogWarning($"[TM] Skill requirement blocked before enqueue: {finalValidationReason}", this);
            SetPhase(Phase.Planning);
            RefreshAllViews();
            RefreshPlanningInteractivity();
            return;
        }

        ElementType dieElement = GetResolvedDiceElement(rt, asset);
        int resolvedSum = TurnManagerCombatUtility.ComputeResolvedDieSum(diceRig, player, start0, span, dieElement, paymentPlan.selectedMask);
        int maxFace = ComputeMaxFace(start0, span);

        if (logPhase)
        {
            Debug.Log($"[TM] Execute cursor={_cursor} asset={(asset ? asset.name : "NULL")} type={(asset ? asset.GetType().Name : "NULL")} rt.kind={rt.kind} rt.target={rt.target} span={span} start0={start0} rawSum={rawSum} resolvedSum={resolvedSum} maxFace={maxFace} playerFocus={(player ? player.focus : -999)}", this);
        }

        MarkDiceSpentInRange(start0, span, paymentPlan.selectedMask);
        _board.ConsumeGroupAtAnchor_NoRefund(_cursor);
        _queuedPlayerCommands.Enqueue(new TurnManagerQueuedPlayerCommand
        {
            kind = TurnManagerQueuedPlayerCommandKind.Skill,
            asset = asset,
            runtime = rt,
            target = clicked,
            resolvedSum = resolvedSum,
            maxFace = maxFace,
            start0 = start0,
            span = span,
            paymentMask = paymentPlan.selectedMask,
        });

        _cursor = FindNextExecutableAnchor();
        SetPhase(Phase.Planning);
        RefreshAllViews();
        RefreshPlanningInteractivity();

        EnsureQueuedPlayerCommandProcessingStarted();
    }

    private IEnumerator ProcessQueuedPlayerCommands()
    {
        while (_queuedPlayerCommands.Count > 0)
        {
            if (TryHandleCombatDefeat())
            {
                _isProcessingQueuedPlayerCommands = false;
                yield break;
            }
            if (TryHandleCombatVictory())
            {
                _isProcessingQueuedPlayerCommands = false;
                yield break;
            }

            TurnManagerQueuedPlayerCommand command = _queuedPlayerCommands.Dequeue();
            if (command.kind == TurnManagerQueuedPlayerCommandKind.EndTurn)
            {
                _endTurnQueued = false;
                yield return ExecuteQueuedEndTurn();
                break;
            }

            yield return ExecuteQueuedCommand(command);
        }

        _isProcessingQueuedPlayerCommands = false;
        RefreshAllViews();
        RefreshPlanningInteractivity();
    }

    private IEnumerator ExecuteQueuedCommand(TurnManagerQueuedPlayerCommand command)
    {
        yield return TurnManagerCommandExecutionUtility.ExecuteQueuedCommand(command, executor, player, party, enemy, diceRig, _playerContext, logPhase, this);
        FinalizePendingUsedVisualRange(command.start0, command.span, command.paymentMask);
        ClearSelectedDiceConsumedByPaymentMask(command.paymentMask);

        if (TryHandleCombatDefeat())
            yield break;

        if (TryHandleCombatVictory())
            yield break;

        SetPhase(Phase.Planning);
        RefreshAllViews();
        RefreshPlanningInteractivity();
    }

    private IEnumerator ExecuteQueuedEndTurn()
    {
        if (TryHandleCombatDefeat())
            yield break;
        if (TryHandleCombatVictory())
            yield break;

        EndPlayerTurn_TickStatusesAndPassives();
        if (TryHandleCombatDefeat())
            yield break;

        yield return EnemyTurnThenBeginNewPlayerTurn();
    }

    private void EnqueueEndTurn()
    {
        _queuedPlayerCommands.Enqueue(new TurnManagerQueuedPlayerCommand
        {
            kind = TurnManagerQueuedPlayerCommandKind.EndTurn
        });
        _endTurnQueued = true;
        UiDragState.DeselectSkill();
        GetDiceEquipUiManager()?.ClearSelectedDice(instant: true);
        RefreshAllViews();
        RefreshPlanningInteractivity();

        EnsureQueuedPlayerCommandProcessingStarted();
    }

    private void EnsureQueuedPlayerCommandProcessingStarted()
    {
        if (_isProcessingQueuedPlayerCommands)
            return;

        _isProcessingQueuedPlayerCommands = true;
        StartCoroutine(ProcessQueuedPlayerCommands());
    }

    private void ClearSelectedDiceConsumedByPaymentMask(int paymentMask)
    {
        if (paymentMask < 0 || diceRig == null)
            return;

        DiceEquipUIManager diceEquipUiManager = GetDiceEquipUiManager();
        if (diceEquipUiManager == null)
            return;

        _usedSelectedDiceBuffer.Clear();
        for (int i = 0; i < RunInventoryManager.EQUIPPED_DICE_COUNT; i++)
        {
            if ((paymentMask & (1 << i)) == 0)
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(i);
            if (die != null)
                _usedSelectedDiceBuffer.Add(die);
        }

        if (_usedSelectedDiceBuffer.Count > 0)
            diceEquipUiManager.ClearSelectedDice(_usedSelectedDiceBuffer, instant: true);
    }

    private DiceEquipUIManager GetDiceEquipUiManager()
    {
        if (_diceEquipUiManager != null)
            return _diceEquipUiManager;

#if UNITY_2023_1_OR_NEWER
        _diceEquipUiManager = FindFirstObjectByType<DiceEquipUIManager>(FindObjectsInactive.Include);
#else
        _diceEquipUiManager = FindObjectOfType<DiceEquipUIManager>(true);
#endif
        return _diceEquipUiManager;
    }
}
