using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Encapsulates the move-selection rules for one enemy turn.
/// </summary>
internal sealed class EnemyIntentSelector
{
    private readonly EnemyIntentSelectionContext m_context;

    public EnemyIntentSelector(EnemyIntentSelectionContext context)
    {
        m_context = context;
    }

    /// <summary>
    /// Resolves the next move index using forced, scripted, or weighted rules in that order.
    /// </summary>
    public int DecideNextMoveIndex(out int nextScriptedLoopCursor)
    {
        nextScriptedLoopCursor = m_context != null ? m_context.ScriptedLoopCursor : 0;
        if (m_context == null || m_context.Definition == null || m_context.Definition.moves == null || m_context.Definition.moves.Count == 0)
        {
            return -1;
        }

        int forcedIndex = TryPickForcedMoveIndex();
        if (forcedIndex >= 0)
        {
            return forcedIndex;
        }

        if (m_context.Definition.intentSelectionMode == EnemyDefinitionSO.EnemyIntentSelectionMode.ScriptedLoop)
        {
            return PickScriptedLoopMove(out nextScriptedLoopCursor);
        }

        return PickWeightedMove();
    }

    private int PickWeightedMove()
    {
        List<int> candidates = BuildCandidateIndices(ignoreCooldownAndRepeatRules: false);
        if (candidates.Count == 0)
        {
            candidates = BuildCandidateIndices(ignoreCooldownAndRepeatRules: true);
        }

        if (candidates.Count == 0)
        {
            return -1;
        }

        return WeightedPick(candidates);
    }

    private int PickScriptedLoopMove(out int nextScriptedLoopCursor)
    {
        int moveCount = m_context.Definition.moves.Count;
        int currentIndex = Mathf.Clamp(m_context.ScriptedLoopCursor, 0, moveCount - 1);
        int loopBackIndex = Mathf.Clamp(m_context.Definition.loopBackToMoveNumber, 0, moveCount - 1);

        for (int checkedCount = 0; checkedCount < moveCount; checkedCount++)
        {
            EnemyDefinitionSO.EnemyMoveSlot move = m_context.Definition.moves[currentIndex];
            int candidateIndex = currentIndex;
            currentIndex = AdvanceScriptedLoopCursor(currentIndex, moveCount, loopBackIndex);

            if (IsMoveBaseEligible(move))
            {
                nextScriptedLoopCursor = currentIndex;
                return candidateIndex;
            }
        }

        nextScriptedLoopCursor = currentIndex;
        return -1;
    }

    private int AdvanceScriptedLoopCursor(int currentIndex, int moveCount, int loopBackIndex)
    {
        int nextIndex = currentIndex + 1;
        if (nextIndex >= moveCount)
        {
            nextIndex = loopBackIndex;
        }

        return Mathf.Clamp(nextIndex, 0, moveCount - 1);
    }

    private int TryPickForcedMoveIndex()
    {
        for (int i = 0; i < m_context.Definition.moves.Count; i++)
        {
            EnemyDefinitionSO.EnemyMoveSlot move = m_context.Definition.moves[i];
            if (move.forceOnTurn <= 0 || move.forceOnTurn != m_context.NextTurnIndex)
            {
                continue;
            }

            if (!IsMoveBaseEligible(move))
            {
                continue;
            }

            if (m_context.GetCooldown(i) > 0)
            {
                continue;
            }

            if (m_context.GetConsecutive(i) >= Mathf.Max(1, move.maxConsecutive))
            {
                continue;
            }

            if (ViolatesNoRepeat(move, i))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private List<int> BuildCandidateIndices(bool ignoreCooldownAndRepeatRules)
    {
        List<int> candidates = new List<int>(m_context.Definition.moves.Count);
        for (int i = 0; i < m_context.Definition.moves.Count; i++)
        {
            EnemyDefinitionSO.EnemyMoveSlot move = m_context.Definition.moves[i];
            if (!IsMoveBaseEligible(move))
            {
                continue;
            }

            if (move.weight <= 0)
            {
                continue;
            }

            if (!ignoreCooldownAndRepeatRules && m_context.GetCooldown(i) > 0)
            {
                continue;
            }

            if (m_context.GetConsecutive(i) >= Mathf.Max(1, move.maxConsecutive))
            {
                continue;
            }

            if (!ignoreCooldownAndRepeatRules && ViolatesNoRepeat(move, i))
            {
                continue;
            }

            candidates.Add(i);
        }

        return candidates;
    }

    private bool IsMoveBaseEligible(EnemyDefinitionSO.EnemyMoveSlot move)
    {
        if (move == null || !move.HasAnySkill)
        {
            return false;
        }

        if (m_context.NextTurnIndex < move.minTurnIndex)
        {
            return false;
        }

        if (m_context.SelfHpPct < move.hpPctMin || m_context.SelfHpPct > move.hpPctMax)
        {
            return false;
        }

        return true;
    }

    private bool ViolatesNoRepeat(EnemyDefinitionSO.EnemyMoveSlot move, int moveIndex)
    {
        if (!m_context.Definition.noRepeatTwice || move.ignoreNoRepeat || m_context.RecentHistoryCount <= 0)
        {
            return false;
        }

        return m_context.PeekLastHistory() == moveIndex;
    }

    private int WeightedPick(List<int> candidateIndices)
    {
        int total = 0;
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int index = candidateIndices[i];
            total += Mathf.Max(0, m_context.Definition.moves[index].weight);
        }

        if (total <= 0)
        {
            return candidateIndices[Random.Range(0, candidateIndices.Count)];
        }

        int roll = Random.Range(0, total);
        int accumulated = 0;
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int index = candidateIndices[i];
            accumulated += Mathf.Max(0, m_context.Definition.moves[index].weight);
            if (roll < accumulated)
            {
                return index;
            }
        }

        return candidateIndices[candidateIndices.Count - 1];
    }
}
