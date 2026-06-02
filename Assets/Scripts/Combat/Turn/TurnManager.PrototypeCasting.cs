using UnityEngine;

public partial class TurnManager
{
    public bool CanPrototypeCastSkillNow(ScriptableObject activeSkill)
    {
        if (!CanInteractWithSkills)
            return false;
        if (player == null || activeSkill == null)
            return false;
        if (activeSkill is SkillPassiveSO)
            return false;
        if (_board.IsSkillEquipped(activeSkill))
            return false;
        if (SkillActiveStateUtility.BlocksRecastWhileActive(activeSkill) &&
            SkillActiveStateUtility.IsSkillActiveOnPlayer(activeSkill, player, out _))
            return false;

        if (!TryResolvePrototypeCastPlacement(activeSkill, out _, out _, commit: false))
            return false;

        if (!TryGetPrototypeSkillTooltipRuntime(activeSkill, out SkillRuntime runtime) || runtime == null)
            return false;

        return HasAnyValidPrototypeTarget(runtime);
    }

    public bool TryGetPrototypeSkillTooltipRuntime(ScriptableObject activeSkill, out SkillRuntime runtime)
    {
        runtime = null;

        if (activeSkill == null || activeSkill is SkillPassiveSO)
            return false;
        if (player == null)
            return false;

        int span = GetSkillSpan(activeSkill);
        if (span <= 0)
            return false;

        if (!_board.TryFindEmptyPlacement(span, IsSlotAssignable0, out int start0, out int anchor0))
            return false;
        if (!AreSlotsActiveInRange(start0, span))
            return false;
        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = _board.Capture(player);
        _board.PlaceGroup(start0, anchor0, span, activeSkill);
        bool ok = _board.RecalculateRuntimesAndRebalance(player, diceRig);
        if (ok)
            runtime = _board.GetAnchorRuntime(anchor0);

        _board.Restore(snap, player);
        return ok && runtime != null;
    }

    public bool TryGetPrototypeSkillPreviewDieValue(ScriptableObject activeSkill, SkillRuntime runtime, out int dieValue)
    {
        dieValue = 0;
        if (activeSkill == null || runtime == null || diceRig == null || player == null || !diceRig.HasRolledThisTurn)
            return false;

        if (!TryResolvePrototypeCastPlacement(activeSkill, out int start0, out _, commit: false))
            return false;

        int slotsNeeded = Mathf.Clamp(runtime.slotsRequired, 1, 3);
        ElementType skillElement = runtime.kind == SkillKind.Attack
            ? runtime.element
            : TurnManagerCombatUtility.GetResolvedDiceElement(runtime, activeSkill);
        dieValue = DiceCombatEnchantRuntimeUtility.ComputeCommittedPreviewDieSum(diceRig, player, start0, slotsNeeded, skillElement);
        return true;
    }

    public bool TryGetPrototypeSkillPreviewPaymentPlan(
        ScriptableObject activeSkill,
        out DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan paymentPlan,
        out int slotsRequired)
    {
        paymentPlan = null;
        slotsRequired = 0;

        if (activeSkill == null || diceRig == null || !diceRig.HasRolledThisTurn)
            return false;

        if (!TryResolvePrototypeCastPlacement(activeSkill, out int start0, out _, commit: false))
            return false;

        slotsRequired = Mathf.Clamp(GetSkillSpan(activeSkill), 1, 3);
        paymentPlan = DiceCombatEnchantRuntimeUtility.BuildPaymentPlan(diceRig, start0, slotsRequired);
        return paymentPlan != null;
    }

    public bool TryCastDraggedSkillToTarget(ScriptableObject activeSkill, CombatActor clicked)
    {
        if (!CanInteractWithSkills || player == null || activeSkill == null || clicked == null)
            return false;
        if (activeSkill is SkillPassiveSO)
            return false;
        if (_board.IsSkillEquipped(activeSkill))
            return false;

        int span = GetSkillSpan(activeSkill);
        if (span <= 0)
            return false;

        if (!TryResolvePrototypeCastPlacement(activeSkill, out _, out int anchor0, commit: true))
        {
            RefreshAllViews();
            return false;
        }

        _cursor = anchor0;
        if (!TryValidateTargetForPendingSkill(clicked, out _))
        {
            _board.ClearGroupAtAnchor(anchor0, player);
            _board.RecalculateRuntimesAndRebalance(player, diceRig);
            RefreshAllViews();
            return false;
        }

        EnqueueCurrentExecution(clicked);
        RefreshAllViews();
        return true;
    }

    public bool TryCastDraggedSkillToSelf(ScriptableObject activeSkill)
    {
        if (player == null)
            return false;
        return TryCastDraggedSkillToTarget(activeSkill, player);
    }

    private bool TryResolvePrototypeCastPlacement(ScriptableObject activeSkill, out int start0, out int anchor0, bool commit)
    {
        start0 = -1;
        anchor0 = -1;

        int span = GetSkillSpan(activeSkill);
        if (span <= 0)
            return false;

        if (!_board.TryFindEmptyPlacement(span, IsSlotAssignable0, out start0, out anchor0))
            return false;
        if (!AreSlotsActiveInRange(start0, span))
            return false;
        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = _board.Capture(player);
        _board.PlaceGroup(start0, anchor0, span, activeSkill);
        bool ok = _board.RecalculateRuntimesAndRebalance(player, diceRig);
        if (!ok)
        {
            _board.Restore(snap, player);
            return false;
        }

        if (!commit)
            _board.Restore(snap, player);

        return true;
    }

    private bool HasAnyValidPrototypeTarget(SkillRuntime runtime)
    {
        if (runtime == null || player == null)
            return false;

        if (!runtime.useV2Targeting)
            return true;

        switch (runtime.targetRuleV2)
        {
            case SkillTargetRule.Self:
                return SkillUsageRequirementUtility.IsTargetRequirementMet(runtime, player);

            case SkillTargetRule.SingleEnemy:
            case SkillTargetRule.RowEnemies:
            case SkillTargetRule.AllEnemies:
            {
                var enemies = TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy);
                for (int i = 0; i < enemies.Count; i++)
                {
                    CombatActor candidate = enemies[i];
                    if (candidate == null || candidate.IsDead)
                        continue;
                    if (TurnManagerTargetingUtility.IsValidTargetForPendingSkill(runtime, candidate, player, party, enemy))
                        return true;
                }

                return false;
            }

            case SkillTargetRule.SingleAlly:
            case SkillTargetRule.RowAllies:
            case SkillTargetRule.AllAllies:
            {
                var allies = TurnManagerCombatUtility.ResolveAliveAlliesSnapshot(party, player);
                for (int i = 0; i < allies.Count; i++)
                {
                    CombatActor candidate = allies[i];
                    if (candidate == null || candidate.IsDead)
                        continue;
                    if (TurnManagerTargetingUtility.IsValidTargetForPendingSkill(runtime, candidate, player, party, enemy))
                        return true;
                }

                return false;
            }
        }

        return true;
    }
}
