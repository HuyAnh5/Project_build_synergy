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
                command.paymentPlan != null
                    ? command.paymentPlan.Clone()
                    : DiceCombatEnchantRuntimeUtility.BuildPaymentPlan(diceRig, command.start0, command.span);
            yield return ResolveCommittedPreSkillFaceEnchants(diceRig, faceUsePlan, player);

            int resolvedSum = TurnManagerCombatUtility.ComputeResolvedDieSum(diceRig, player, faceUsePlan, ElementType.Neutral);
            IReadOnlyList<CombatActor> aoeTargets = SkillTargetRuleUtility.IsMultiTarget(buffSkill.target)
                ? TurnManagerCombatUtility.ResolveTargets(buffSkill.target, player, command.target, party, fallbackEnemy)
                : null;

            if (logPhase)
            {
                Debug.Log(
                    $"[TM] Branch=BuffDebuff -> {buffSkill.name} targetRule={buffSkill.target} delay={buffSkill.applyDelayTurns} effects={(buffSkill.effects != null ? buffSkill.effects.Count : 0)} applyAilment={buffSkill.applyAilment}",
                    context);
            }

            yield return executor.ExecuteSkill(buffSkill, player, command.target, resolvedSum, command.maxFace, skipCost: true, aoeTargets: aoeTargets, castDiceRig: diceRig, castStart0: command.start0, castSpan: command.span, castPaymentMask: command.paymentMask);

            for (int i = 0; i < Mathf.Max(0, faceUsePlan.repeatCount); i++)
            {
                if (DiceCombatEnchantRuntimeUtility.PlayRepeatAgainPopup(diceRig, faceUsePlan))
                    yield return WaitForEnchantPopupBeat();
                yield return executor.ExecuteSkill(buffSkill, player, command.target, resolvedSum, command.maxFace, skipCost: true, aoeTargets: aoeTargets, castDiceRig: diceRig, castStart0: command.start0, castSpan: command.span, castPaymentMask: command.paymentMask);
            }

            int reloadPopups = DiceCombatEnchantRuntimeUtility.PlayCommittedReloadFaceEnchantPopups(diceRig, faceUsePlan);
            if (reloadPopups > 0)
                yield return WaitForPreSkillEnchantPopupBeat();
            DiceCombatEnchantRuntimeUtility.ResolveCommittedPostSkillFaceEnchants(diceRig, faceUsePlan, context as TurnManager);
        }
        else
        {
            DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan faceUsePlan =
                command.paymentPlan != null
                    ? command.paymentPlan.Clone()
                    : DiceCombatEnchantRuntimeUtility.BuildPaymentPlan(diceRig, command.start0, command.span);
            yield return ResolveCommittedPreSkillFaceEnchants(diceRig, faceUsePlan, player);

            ElementType dieElement = TurnManagerCombatUtility.GetResolvedDiceElement(command.runtime, command.asset);
            SkillRuntime executionRuntime = SkillPlanRuntimeUtility.EvaluateRuntimeForSkillAsset(command.asset, player, diceRig, command.start0, command.span, command.start0);
            if (executionRuntime == null)
                executionRuntime = command.runtime;
            int resolvedSum = TurnManagerCombatUtility.ComputeResolvedDieSum(diceRig, player, faceUsePlan, dieElement);
            IReadOnlyList<CombatActor> aoeTargets = TurnManagerCombatUtility.ResolveAoeTargets(executionRuntime, player, command.target, party, fallbackEnemy);

            if (logPhase)
                Debug.Log($"[TM] Branch=Runtime (Attack/Guard/Legacy) -> rt.kind={executionRuntime.kind}", context);

            yield return executor.ExecuteSkill(executionRuntime, player, command.target, resolvedSum, skipCost: true, aoeTargets: aoeTargets, castDiceRig: diceRig, castStart0: command.start0, castSpan: command.span, castPaymentMask: command.paymentMask);

            for (int i = 0; i < Mathf.Max(0, faceUsePlan.repeatCount); i++)
            {
                if (DiceCombatEnchantRuntimeUtility.PlayRepeatAgainPopup(diceRig, faceUsePlan))
                    yield return WaitForEnchantPopupBeat();
                yield return executor.ExecuteSkill(executionRuntime, player, command.target, resolvedSum, skipCost: true, aoeTargets: aoeTargets, castDiceRig: diceRig, castStart0: command.start0, castSpan: command.span, castPaymentMask: command.paymentMask);
            }

            if (IsBasicStrikeRuntime(executionRuntime))
                playerContext?.HandleBasicStrikeUse(diceRig, command.start0);

            int reloadPopups = DiceCombatEnchantRuntimeUtility.PlayCommittedReloadFaceEnchantPopups(diceRig, faceUsePlan);
            if (reloadPopups > 0)
                yield return WaitForPreSkillEnchantPopupBeat();
            DiceCombatEnchantRuntimeUtility.ResolveCommittedPostSkillFaceEnchants(diceRig, faceUsePlan, context as TurnManager);
        }
    }

    private static bool IsBasicStrikeRuntime(SkillRuntime rt)
        => rt != null && rt.coreAction == CoreAction.BasicStrike;

    private static IEnumerator WaitForEnchantPopupBeat()
    {
        yield return new WaitForSeconds(0.18f);
    }

    private static IEnumerator WaitForPreSkillEnchantPopupBeat()
    {
        yield return new WaitForSeconds(0.25f);
    }

    private static IEnumerator ResolveCommittedPreSkillFaceEnchants(
        DiceSlotRig diceRig,
        DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan faceUsePlan,
        CombatActor player)
    {
        int selfPopups = DiceCombatEnchantRuntimeUtility.ResolveCommittedSelfFaceEnchants(diceRig, faceUsePlan, player);
        if (selfPopups > 0)
            yield return WaitForPreSkillEnchantPopupBeat();

        int relayPopups = DiceCombatEnchantRuntimeUtility.ResolveCommittedRelayFaceEnchants(diceRig, faceUsePlan);
        if (relayPopups > 0)
            yield return WaitForPreSkillEnchantPopupBeat();
    }
}
