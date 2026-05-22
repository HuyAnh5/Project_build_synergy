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
            IReadOnlyList<CombatActor> aoeTargets = SkillTargetRuleUtility.IsMultiTarget(buffSkill.target)
                ? TurnManagerCombatUtility.ResolveTargets(buffSkill.target, player, command.target, party, fallbackEnemy)
                : null;

            if (logPhase)
            {
                Debug.Log(
                    $"[TM] Branch=BuffDebuff -> {buffSkill.name} targetRule={buffSkill.target} delay={buffSkill.applyDelayTurns} effects={(buffSkill.effects != null ? buffSkill.effects.Count : 0)} applyAilment={buffSkill.applyAilment}",
                    context);
            }

            yield return executor.ExecuteSkill(buffSkill, player, command.target, command.resolvedSum, command.maxFace, skipCost: true, aoeTargets: aoeTargets);
        }
        else
        {
            IReadOnlyList<CombatActor> aoeTargets = TurnManagerCombatUtility.ResolveAoeTargets(command.runtime, player, command.target, party, fallbackEnemy);

            if (logPhase)
                Debug.Log($"[TM] Branch=Runtime (Attack/Guard/Legacy) -> rt.kind={command.runtime.kind}", context);

            yield return executor.ExecuteSkill(command.runtime, player, command.target, command.resolvedSum, skipCost: true, aoeTargets: aoeTargets);

            if (IsBasicStrikeRuntime(command.runtime))
                playerContext?.HandleBasicStrikeUse(diceRig, command.start0);
        }
    }

    private static bool IsBasicStrikeRuntime(SkillRuntime rt)
        => rt != null && rt.coreAction == CoreAction.BasicStrike;
}
