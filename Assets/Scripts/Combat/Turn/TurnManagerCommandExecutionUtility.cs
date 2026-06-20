using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal static class TurnManagerCommandExecutionUtility
{
    public static IEnumerator ExecuteQueuedCommand(
        TurnManagerQueuedPlayerCommand command,
        SkillExecutor executor,
        CombatActor player,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy,
        DiceSlotRig diceRig,
        CombatActorRuntimeContext playerContext,
        bool logPhase,
        Object context)
    {
        if (command.asset == null || executor == null)
            yield break;

        if (command.asset is SkillBuffDebuffSO buffSkill)
        {
            DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan faceUsePlan =
                DiceCombatEnchantRuntimeUtility.BuildPaymentPlanFromMask(diceRig, command.paymentMask);
            yield return ResolveCommittedPreSkillFaceEnchants(diceRig, faceUsePlan, player);

            SkillRuntime executionRuntime = SkillPlanRuntimeUtility.EvaluateRuntimeForSkillAsset(command.asset, player, diceRig, command.start0, command.span, command.start0, command.paymentMask, includeSyntheticRelayAdded: false);
            if (executionRuntime == null)
                executionRuntime = command.runtime;
            int resolvedSum = TurnManagerCombatUtility.ComputeResolvedDieSum(diceRig, player, command.start0, command.span, ElementType.Neutral, command.paymentMask);
            IReadOnlyList<CombatActor> aoeTargets = SkillTargetRuleUtility.IsMultiTarget(buffSkill.target)
                ? TurnManagerCombatUtility.ResolveTargets(buffSkill.target, player, command.target, party, fallbackEnemy)
                : null;

            if (logPhase)
            {
                Debug.Log(
                    $"[TM] Branch=BuffDebuff -> {buffSkill.name} targetRule={buffSkill.target}",
                    context);
            }

            yield return ExecuteBuffSkillWithPostCastDiceFlow(command, executor, player, diceRig, buffSkill, executionRuntime, resolvedSum, aoeTargets, context as TurnManager, suppressDiceCastAnimation: false);

            int statusRepeatCount = ConsumeRepeatFirstSkillExtraCasts(player);
            for (int i = 0; i < statusRepeatCount; i++)
            {
                yield return WaitForEnchantPopupBeat();
                yield return ExecuteBuffSkillWithPostCastDiceFlow(command, executor, player, diceRig, buffSkill, executionRuntime, resolvedSum, aoeTargets, context as TurnManager, suppressDiceCastAnimation: false);
            }

            for (int i = 0; i < Mathf.Max(0, faceUsePlan.repeatCount); i++)
            {
                if (DiceCombatEnchantRuntimeUtility.PlayRepeatAgainPopup(diceRig, faceUsePlan))
                    yield return WaitForEnchantPopupBeat();
                yield return ExecuteBuffSkillWithPostCastDiceFlow(command, executor, player, diceRig, buffSkill, executionRuntime, resolvedSum, aoeTargets, context as TurnManager, suppressDiceCastAnimation: false);
            }

            DiceCombatEnchantRuntimeUtility.ResolveCommittedPostSkillFaceEnchants(diceRig, faceUsePlan, context as TurnManager);
            yield return ResolveCommittedReloadFaceEnchants(diceRig, faceUsePlan, context as TurnManager);
        }
        else
        {
            DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan faceUsePlan =
                DiceCombatEnchantRuntimeUtility.BuildPaymentPlanFromMask(diceRig, command.paymentMask);
            yield return ResolveCommittedPreSkillFaceEnchants(diceRig, faceUsePlan, player);

            ElementType dieElement = TurnManagerCombatUtility.GetResolvedDiceElement(command.runtime, command.asset);
            SkillRuntime executionRuntime = SkillPlanRuntimeUtility.EvaluateRuntimeForSkillAsset(command.asset, player, diceRig, command.start0, command.span, command.start0, command.paymentMask, includeSyntheticRelayAdded: false);
            if (executionRuntime == null)
                executionRuntime = command.runtime;
            int resolvedSum = TurnManagerCombatUtility.ComputeResolvedDieSum(diceRig, player, command.start0, command.span, dieElement, command.paymentMask);
            IReadOnlyList<CombatActor> aoeTargets = TurnManagerCombatUtility.ResolveAoeTargets(executionRuntime, player, command.target, party, fallbackEnemy);

            if (logPhase)
                Debug.Log($"[TM] Branch=Runtime (Attack/Guard/Legacy) -> rt.kind={executionRuntime.kind}", context);

            yield return executor.ExecuteSkill(executionRuntime, player, command.target, resolvedSum, skipCost: true, aoeTargets: aoeTargets, castDiceRig: diceRig, castStart0: command.start0, castSpan: command.span, castPaymentMask: command.paymentMask);

            int statusRepeatCount = ConsumeRepeatFirstSkillExtraCasts(player);
            for (int i = 0; i < statusRepeatCount; i++)
            {
                yield return WaitForEnchantPopupBeat();
                yield return executor.ExecuteSkill(executionRuntime, player, command.target, resolvedSum, skipCost: true, aoeTargets: aoeTargets, castDiceRig: diceRig, castStart0: command.start0, castSpan: command.span, castPaymentMask: command.paymentMask, suppressDiceCastAnimation: false);
            }

            for (int i = 0; i < Mathf.Max(0, faceUsePlan.repeatCount); i++)
            {
                if (DiceCombatEnchantRuntimeUtility.PlayRepeatAgainPopup(diceRig, faceUsePlan))
                    yield return WaitForEnchantPopupBeat();
                yield return executor.ExecuteSkill(executionRuntime, player, command.target, resolvedSum, skipCost: true, aoeTargets: aoeTargets, castDiceRig: diceRig, castStart0: command.start0, castSpan: command.span, castPaymentMask: command.paymentMask);
            }

            if (IsBasicStrikeRuntime(executionRuntime))
                playerContext?.HandleBasicStrikeUse(diceRig, command.start0);

            ConsumeNextSkillAddedValueIfUsed(player, executionRuntime);
            DiceCombatEnchantRuntimeUtility.ResolveCommittedPostSkillFaceEnchants(diceRig, faceUsePlan, context as TurnManager);
            yield return ResolveCommittedReloadFaceEnchants(diceRig, faceUsePlan, context as TurnManager);
        }
    }

    private static bool IsBasicStrikeRuntime(SkillRuntime rt)
        => rt != null && rt.coreAction == CoreAction.BasicStrike;

    private static IEnumerator ExecuteBuffSkillWithPostCastDiceFlow(
        TurnManagerQueuedPlayerCommand command,
        SkillExecutor executor,
        CombatActor player,
        DiceSlotRig diceRig,
        SkillBuffDebuffSO buffSkill,
        SkillRuntime executionRuntime,
        int resolvedSum,
        IReadOnlyList<CombatActor> aoeTargets,
        TurnManager turnManager,
        bool suppressDiceCastAnimation)
    {
        yield return executor.ExecuteSkill(
            buffSkill,
            player,
            command.target,
            resolvedSum,
            command.maxFace,
            skipCost: true,
            aoeTargets: aoeTargets,
            castDiceRig: diceRig,
            castStart0: command.start0,
            castSpan: command.span,
            castPaymentMask: command.paymentMask,
            suppressDiceCastAnimation: suppressDiceCastAnimation);

        yield return BuffDebuffFlowRuntimeUtility.ApplyPostCastDiceEffects(
            buffSkill,
            executionRuntime,
            player,
            command.target,
            diceRig,
            command.paymentMask,
            turnManager);
    }

    private static int ConsumeRepeatFirstSkillExtraCasts(CombatActor player)
    {
        if (player == null || player.status == null)
            return 0;

        return player.status.ConsumeRepeatFirstSkillReady();
    }

    private static void ConsumeNextSkillAddedValueIfUsed(CombatActor player, SkillRuntime rt)
    {
        if (player == null || player.status == null || rt == null)
            return;

        if (rt.ownerActionAddedValueBonus > 0)
            player.status.ConsumeNextSkillAddedValue();
    }

    private static IEnumerator WaitForEnchantPopupBeat()
    {
        yield return new WaitForSeconds(0.18f);
    }

    private static IEnumerator WaitForPreSkillEnchantPopupBeat()
    {
        yield return new WaitForSeconds(0.4f);
    }

    private static IEnumerator ResolveCommittedPreSkillFaceEnchants(
        DiceSlotRig diceRig,
        DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan faceUsePlan,
        CombatActor player)
    {
        if (diceRig == null || faceUsePlan == null)
            yield break;

        List<int> slotsToResolve = new List<int>(3);
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (DiceCombatEnchantRuntimeUtility.CanResolveCommittedPreSkillFaceEnchant(diceRig, faceUsePlan, player, slot0))
                slotsToResolve.Add(slot0);
        }

        for (int i = 0; i < slotsToResolve.Count; i++)
        {
            int popupCount = DiceCombatEnchantRuntimeUtility.ResolveCommittedPreSkillFaceEnchant(
                diceRig,
                faceUsePlan,
                player,
                slotsToResolve[i]);

            if (popupCount <= 0)
                continue;

            diceRig.RefreshRollInfoCache();
            yield return WaitForPreSkillEnchantPopupBeat();
        }
    }

    private static IEnumerator ResolveCommittedReloadFaceEnchants(
        DiceSlotRig diceRig,
        DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan faceUsePlan,
        TurnManager turnManager)
    {
        bool playedReloadPopup = DiceCombatEnchantRuntimeUtility.PlayCommittedReloadPopups(diceRig, faceUsePlan);
        if (playedReloadPopup)
            yield return WaitForEnchantPopupBeat();

        DiceCombatEnchantRuntimeUtility.ApplyCommittedReloadFaceEnchants(diceRig, faceUsePlan, turnManager);
    }
}
