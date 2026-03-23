using UnityEngine;

[DisallowMultipleComponent]
public class SkillCombatState : MonoBehaviour
{
    [SerializeField] private int executionCarryPendingNextTurn;
    [SerializeField] private int executionCarryActive;
    [SerializeField] private int lastEnemyTurnHpLost;

    private int _enemyTurnStartHp = -1;

    public int ExecutionCarryActive => Mathf.Max(0, executionCarryActive);
    public int LastEnemyTurnHpLost => Mathf.Max(0, lastEnemyTurnHpLost);

    public void ResetForBattle()
    {
        executionCarryPendingNextTurn = 0;
        executionCarryActive = 0;
        lastEnemyTurnHpLost = 0;
        _enemyTurnStartHp = -1;
    }

    public void BeginPlayerTurn()
    {
        executionCarryActive = Mathf.Max(0, executionCarryPendingNextTurn);
        executionCarryPendingNextTurn = 0;
    }

    public void QueueExecutionCarry(int amount)
    {
        if (amount <= 0)
            return;

        executionCarryPendingNextTurn = Mathf.Max(executionCarryPendingNextTurn, amount);
    }

    public void ConsumeExecutionCarry()
    {
        executionCarryActive = 0;
    }

    public void BeginEnemyTurn(int currentHp)
    {
        _enemyTurnStartHp = Mathf.Max(0, currentHp);
    }

    public void EndEnemyTurn(int currentHp)
    {
        if (_enemyTurnStartHp < 0)
        {
            lastEnemyTurnHpLost = 0;
            return;
        }

        lastEnemyTurnHpLost = Mathf.Max(0, _enemyTurnStartHp - Mathf.Max(0, currentHp));
        _enemyTurnStartHp = -1;
    }

}
