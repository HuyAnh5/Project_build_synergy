using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class EnemyTurnCoordinator
{
    private readonly SkillTargetSelectionService _targetSelectionService = new SkillTargetSelectionService();

    public IEnumerator Execute(
        CombatActorRuntimeContext playerContext,
        SkillExecutor executor,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy,
        float delayBetweenEnemyAttacks,
        bool logPhase,
        Object context)
    {
        CombatActor player = playerContext != null ? playerContext.Actor : null;
        SkillCombatState playerSkillState = playerContext != null ? playerContext.SkillCombatState : null;
        if (player != null && playerSkillState != null)
            playerSkillState.BeginEnemyTurn(player.hp);

        TurnManagerViewUtility.FadeEnemyIntents(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, fallbackEnemy), 0.25f);

        if (delayBetweenEnemyAttacks > 0f)
            yield return new WaitForSeconds(delayBetweenEnemyAttacks);

        var enemies = TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, fallbackEnemy);
        for (int i = 0; i < enemies.Count; i++)
        {
            CombatActor enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            bool skipTurn = false;
            if (enemy.status != null)
            {
                int dot = enemy.status.OnTurnStarted(consumeFreezeToSkipTurn: true, out skipTurn);
                if (logPhase)
                {
                    Debug.Log(
                        $"[TM] EnemyTurnStart {enemy.name}: dot={dot} skip={skipTurn} bleed={enemy.status.bleedStacks} burnStacks={enemy.status.burnStacks} burnBatches={enemy.status.GetBurnBatches().Count} frozen={enemy.status.frozen}",
                        context);
                }

                if (dot > 0)
                    enemy.TakeDamage(dot, bypassGuard: true);
            }

            if (enemy.IsDead)
                continue;

            if (skipTurn)
            {
                FinishEnemyTurn(enemy);
                if (delayBetweenEnemyAttacks > 0f)
                    yield return new WaitForSeconds(delayBetweenEnemyAttacks);
                continue;
            }

            if (player != null && !player.IsDead)
                yield return ExecuteEnemyAction(enemy, player, executor, party, fallbackEnemy, delayBetweenEnemyAttacks);

            FinishEnemyTurn(enemy);

            if (player != null && player.IsDead)
                break;
        }

        if (player != null)
        {
            if (playerContext == null || !playerContext.ShouldRetainGuardAtEndOfTurn())
                player.guardPool = 0;
            if (playerSkillState != null)
                playerSkillState.EndEnemyTurn(player.hp);
        }

        TurnManagerCombatUtility.ClearAllStagger();
    }

    private IEnumerator ExecuteEnemyAction(
        CombatActor enemy,
        CombatActor player,
        SkillExecutor executor,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy,
        float delayBetweenEnemyAttacks)
    {
        EnemyBrainController brain = enemy.GetComponent<EnemyBrainController>();
        if (brain != null &&
            brain.definition != null &&
            brain.definition.moves != null &&
            brain.definition.moves.Count > 0 &&
            brain.CurrentIntent.hasIntent)
        {
            var move = brain.definition.moves[brain.CurrentIntent.moveIndex];

            if (move.buffDebuffSkill != null)
            {
                CombatActor clickedTarget = ResolveBuffSkillTarget(move, brain, enemy, player, party);
                SkillTargetSelection selection = _targetSelectionService.SelectCombatTargets(
                    move.buffDebuffSkill.target,
                    enemy,
                    clickedTarget,
                    party,
                    fallbackEnemy);

                if (move.buffDebuffSkill.target == SkillTargetRule.SingleAlly && selection.PrimaryTarget == null)
                {
                    if (move.damageSkill != null)
                        yield return executor.ExecuteSkill(move.damageSkill, enemy, player, dieValue: 3, skipCost: true);
                }
                else
                {
                    yield return executor.ExecuteSkill(
                        move.buffDebuffSkill,
                        enemy,
                        selection.PrimaryTarget,
                        rolledValue: 3,
                        maxFaceValue: 6,
                        skipCost: true,
                        aoeTargets: selection.Targets);
                }
            }
            else if (move.damageSkill != null)
            {
                CombatActor primaryTarget = ResolveDamageSkillTarget(move.damageSkill.target, enemy, player);
                IReadOnlyList<CombatActor> aoeTargets = SkillTargetRuleUtility.IsMultiTarget(move.damageSkill.target)
                    ? TurnManagerCombatUtility.ResolveTargets(move.damageSkill.target, enemy, primaryTarget, party, fallbackEnemy)
                    : null;

                if (primaryTarget != null)
                    yield return executor.ExecuteSkill(move.damageSkill, enemy, primaryTarget, dieValue: 3, skipCost: true, aoeTargets: aoeTargets);
            }

            brain.ConsumeCurrentIntent();
            brain.AdvanceTurnTick();
            brain.DecideNextIntent(player);

            if (delayBetweenEnemyAttacks > 0f)
                yield return new WaitForSeconds(delayBetweenEnemyAttacks);
            yield break;
        }

        player.TakeDamage(4, bypassGuard: false);
        if (delayBetweenEnemyAttacks > 0f)
            yield return new WaitForSeconds(delayBetweenEnemyAttacks);
    }

    private static CombatActor ResolveBuffSkillTarget(
        EnemyDefinitionSO.EnemyMoveSlot move,
        EnemyBrainController brain,
        CombatActor enemy,
        CombatActor player,
        BattlePartyManager2D party)
    {
        switch (move.buffDebuffSkill.target)
        {
            case SkillTargetRule.Self:
                return enemy;
            case SkillTargetRule.SingleEnemy:
                return player;
            case SkillTargetRule.SingleAlly:
                if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Heal) != 0)
                    return brain.PickMostInjuredEnemyAlly(party, includeSelf: true);
                return enemy;
            case SkillTargetRule.RowEnemies:
            case SkillTargetRule.AllEnemies:
                return player;
            case SkillTargetRule.RowAllies:
            case SkillTargetRule.AllAllies:
                return enemy;
            default:
                return player;
        }
    }

    private static CombatActor ResolveDamageSkillTarget(
        SkillTargetRule rule,
        CombatActor enemy,
        CombatActor player)
    {
        switch (rule)
        {
            case SkillTargetRule.Self:
            case SkillTargetRule.SingleAlly:
            case SkillTargetRule.RowAllies:
            case SkillTargetRule.AllAllies:
                return enemy;
            case SkillTargetRule.RowEnemies:
            case SkillTargetRule.AllEnemies:
            case SkillTargetRule.SingleEnemy:
            default:
                return player;
        }
    }

    private static void FinishEnemyTurn(CombatActor enemy)
    {
        if (enemy != null && enemy.status != null)
            enemy.status.OnOwnerTurnEnded();
    }
}
