using System;

/// <summary>
/// Thin compatibility wrapper that preserves the old static API while delegating to an intent selector object.
/// </summary>
internal static class EnemyIntentSelectionUtility
{
    /// <summary>
    /// Chooses the next move index for an enemy definition and returns the updated scripted-loop cursor.
    /// </summary>
    public static int DecideNextMoveIndex(
        EnemyDefinitionSO definition,
        int nextTurnIndex,
        float selfHpPct,
        int recentHistoryCount,
        Func<int, int> getCooldown,
        Func<int, int> getConsecutive,
        Func<int> peekLastHistory,
        int scriptedLoopCursor,
        out int nextScriptedLoopCursor)
    {
        EnemyIntentSelectionContext context = new EnemyIntentSelectionContext(
            definition,
            nextTurnIndex,
            selfHpPct,
            recentHistoryCount,
            getCooldown,
            getConsecutive,
            peekLastHistory,
            scriptedLoopCursor);

        EnemyIntentSelector selector = new EnemyIntentSelector(context);
        return selector.DecideNextMoveIndex(out nextScriptedLoopCursor);
    }
}
