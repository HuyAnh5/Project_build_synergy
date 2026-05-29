using System;

/// <summary>
/// Immutable input snapshot used by enemy intent selection logic.
/// </summary>
internal sealed class EnemyIntentSelectionContext
{
    public EnemyIntentSelectionContext(
        EnemyDefinitionSO definition,
        int nextTurnIndex,
        float selfHpPct,
        int recentHistoryCount,
        Func<int, int> getCooldown,
        Func<int, int> getConsecutive,
        Func<int> peekLastHistory,
        int scriptedLoopCursor)
    {
        Definition = definition;
        NextTurnIndex = nextTurnIndex;
        SelfHpPct = selfHpPct;
        RecentHistoryCount = recentHistoryCount;
        GetCooldown = getCooldown;
        GetConsecutive = getConsecutive;
        PeekLastHistory = peekLastHistory;
        ScriptedLoopCursor = scriptedLoopCursor;
    }

    public EnemyDefinitionSO Definition { get; }

    public int NextTurnIndex { get; }

    public float SelfHpPct { get; }

    public int RecentHistoryCount { get; }

    public Func<int, int> GetCooldown { get; }

    public Func<int, int> GetConsecutive { get; }

    public Func<int> PeekLastHistory { get; }

    public int ScriptedLoopCursor { get; }
}
