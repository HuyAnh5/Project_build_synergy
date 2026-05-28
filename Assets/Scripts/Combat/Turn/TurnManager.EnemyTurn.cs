using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TurnManager
{
    private IEnumerator EnemyTurnThenBeginNewPlayerTurn()
    {
        yield return EnemyTurnRoutine();
        if (TryHandleCombatDefeat())
            yield break;
        if (TryHandleCombatVictory())
            yield break;
        BeginNewPlayerTurn();
    }

    private IEnumerator EnemyTurnRoutine()
    {
        SetPhase(Phase.EnemyTurn);
        _playerContext.Bind(player);
        yield return _enemyTurnCoordinator.Execute(_playerContext, executor, party, enemy, delayBetweenEnemyAttacks, logPhase, this);
        yield return null;
        yield break;

        SkillCombatState playerSkillState = player != null ? player.GetComponent<SkillCombatState>() : null;
        if (player != null && playerSkillState != null)
            playerSkillState.BeginEnemyTurn(player.hp);

        // ? Intent fade out when enemy turn starts (STS feel)
        TurnManagerViewUtility.FadeEnemyIntents(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy), 0.25f);

        // ? Delay tru?c enemy d?u tiï¿½n (d? khï¿½ng dï¿½nh ngay l?p t?c khi v?a b?m Continue)
        if (delayBetweenEnemyAttacks > 0f)
            yield return new WaitForSeconds(delayBetweenEnemyAttacks);

        var enemies = TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy);

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead) continue;

            bool skipTurn = false;

            if (e.status != null)
            {
                int dot = e.status.OnTurnStarted(consumeFreezeToSkipTurn: true, out skipTurn);
                if (logPhase) Debug.Log(
                    $"[TM] EnemyTurnStart {e.name}: dot={dot} skip={skipTurn} bleed={e.status.bleedStacks} burnStacks={e.status.burnStacks} burnBatches={e.status.GetBurnBatches().Count} frozen={e.status.frozen}",
                    this
                );

                if (dot > 0) e.TakeDamage(dot, bypassGuard: true);
            }

            if (e.IsDead) continue;

            if (skipTurn)
            {
                if (e.status != null) e.status.OnOwnerTurnEnded();

                // ? Nh?p d?u: skip cung delay nhu m?t lu?t ï¿½hï¿½nh d?ngï¿½
                if (delayBetweenEnemyAttacks > 0f)
                    yield return new WaitForSeconds(delayBetweenEnemyAttacks);

                continue;
            }

            if (player != null && !player.IsDead)
            {
                var brain = e.GetComponent<EnemyBrainController>();

                // n?u cï¿½ brain+definition thï¿½ cast skill th?t
                if (brain != null && brain.definition != null && brain.definition.moves != null && brain.definition.moves.Count > 0)
                {

                    if (brain.CurrentIntent.hasIntent)
                    {
                        var move = brain.definition.moves[brain.CurrentIntent.moveIndex];

                        // ch?n skill d? cast: uu tiï¿½n BuffDebuff n?u cï¿½, khï¿½ng thï¿½ Damage
                        if (move.buffDebuffSkill != null)
                        {
                            // resolve target cho buff/debuff
                            CombatActor clicked = null;
                            IReadOnlyList<CombatActor> aoe = null;

                            switch (move.buffDebuffSkill.target)
                            {
                                case SkillTargetRule.Self:
                                    clicked = e;
                                    break;

                                case SkillTargetRule.SingleEnemy:
                                    clicked = player;
                                    break;

                                case SkillTargetRule.SingleAlly:
                                    // ? heal ally y?u nh?t n?u move tag Heal
                                    if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Heal) != 0)
                                        clicked = brain.PickMostInjuredEnemyAlly(party, includeSelf: true);
                                    else
                                        clicked = e; // fallback
                                    break;

                                case SkillTargetRule.RowEnemies:
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, player, party, enemy);
                                    clicked = player;
                                    break;

                                case SkillTargetRule.AllEnemies:
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, player, party, enemy);
                                    clicked = (aoe != null && aoe.Count > 0) ? aoe[0] : player;
                                    break;

                                case SkillTargetRule.RowAllies:
                                    clicked = e;
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, clicked, party, enemy);
                                    break;

                                case SkillTargetRule.AllAllies:
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, e, party, enemy);
                                    clicked = e;
                                    break;
                            }

                            // n?u SingleAlly heal mï¿½ khï¿½ng ai thi?u mï¿½u => fallback attack nh? (ho?c skip)
                            if (move.buffDebuffSkill.target == SkillTargetRule.SingleAlly && clicked == null)
                            {
                                // fallback: dï¿½nh player b?ng damageSkill n?u cï¿½
                                if (move.damageSkill != null)
                                    yield return executor.ExecuteSkill(move.damageSkill, e, player, dieValue: 3, skipCost: true);
                            }
                            else
                            {
                                yield return executor.ExecuteSkill(
                                    move.buffDebuffSkill, e, clicked,
                                    rolledValue: 3, maxFaceValue: 6,
                                    skipCost: true,
                                    aoeTargets: aoe
                                );
                            }
                        }
                        else if (move.damageSkill != null)
                        {
                            // resolve target cho damage
                            CombatActor target = null;
                            IReadOnlyList<CombatActor> aoe = null;
                            switch (move.damageSkill.target)
                            {
                                case SkillTargetRule.Self: target = e; break;
                                case SkillTargetRule.SingleEnemy: target = player; break;
                                case SkillTargetRule.SingleAlly: target = e; break; // damage hi?m khi dï¿½ng
                                case SkillTargetRule.RowEnemies:
                                case SkillTargetRule.AllEnemies:
                                    target = player;
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.damageSkill.target, e, target, party, enemy);
                                    break;
                                case SkillTargetRule.RowAllies:
                                case SkillTargetRule.AllAllies:
                                    target = e;
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.damageSkill.target, e, target, party, enemy);
                                    break;
                                default: target = player; break;
                            }

                            if (target != null)
                                yield return executor.ExecuteSkill(move.damageSkill, e, target, dieValue: 3, skipCost: true, aoeTargets: aoe);
                        }

                        // consume intent + tick cooldown turn (t?i thi?u d? cooldown ho?t d?ng)
                        brain.ConsumeCurrentIntent();
                        brain.AdvanceTurnTick();
                        brain.DecideNextIntent(player);

                        if (delayBetweenEnemyAttacks > 0f)
                            yield return new WaitForSeconds(delayBetweenEnemyAttacks);

                        if (e.status != null) e.status.OnOwnerTurnEnded();
                        continue;
                    }
                }

                // fallback cu?i cï¿½ng n?u chua cï¿½ brain/move/skill
                player.TakeDamage(4, bypassGuard: false);
                if (delayBetweenEnemyAttacks > 0f)
                    yield return new WaitForSeconds(delayBetweenEnemyAttacks);
            }

            if (e.status != null) e.status.OnOwnerTurnEnded();

            if (player != null && player.IsDead)
                break;
        }

        if (player != null)
        {
            PassiveSystem ps = player.GetComponent<PassiveSystem>();
            if (ps == null || !ps.ShouldRetainGuardAtEndOfTurn())
                player.guardPool = 0;
            if (playerSkillState != null)
                playerSkillState.EndEnemyTurn(player.hp);
        }
        TurnManagerCombatUtility.ClearAllStagger();

        yield return null;
    }
}
