using System.Collections;
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
    }
}
