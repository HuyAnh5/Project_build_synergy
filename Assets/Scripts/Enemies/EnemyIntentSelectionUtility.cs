using System;
using System.Collections.Generic;
using UnityEngine;

internal static class EnemyIntentSelectionUtility
{
    public static int DecideNextMoveIndex(
        EnemyDefinitionSO definition,
        int nextTurnIndex,
        float selfHpPct,
        int recentHistoryCount,
        Func<int, int> getCooldown,
        Func<int, int> getConsecutive,
        Func<int> peekLastHistory)
    {
        if (definition == null || definition.moves == null || definition.moves.Count == 0)
            return -1;

        int forcedIndex = TryPickForcedMoveIndex(
            definition,
            nextTurnIndex,
            selfHpPct,
            recentHistoryCount,
            getCooldown,
            getConsecutive,
            peekLastHistory);
        if (forcedIndex >= 0)
            return forcedIndex;

        List<int> candidates = BuildCandidateIndices(
            definition,
            nextTurnIndex,
            selfHpPct,
            recentHistoryCount,
            getCooldown,
            getConsecutive,
            peekLastHistory,
            ignoreCooldownAndRepeatRules: false);
        if (candidates.Count == 0)
        {
            candidates = BuildCandidateIndices(
                definition,
                nextTurnIndex,
                selfHpPct,
                recentHistoryCount,
                getCooldown,
                getConsecutive,
                peekLastHistory,
                ignoreCooldownAndRepeatRules: true);
        }

        if (candidates.Count == 0)
            return -1;

        return WeightedPick(definition, candidates);
    }

    private static int TryPickForcedMoveIndex(
        EnemyDefinitionSO definition,
        int nextTurnIndex,
        float selfHpPct,
        int recentHistoryCount,
        Func<int, int> getCooldown,
        Func<int, int> getConsecutive,
        Func<int> peekLastHistory)
    {
        for (int i = 0; i < definition.moves.Count; i++)
        {
            EnemyDefinitionSO.EnemyMoveSlot move = definition.moves[i];
            if (move.forceOnTurn <= 0 || move.forceOnTurn != nextTurnIndex)
                continue;
            if (!IsMoveBaseEligible(move, nextTurnIndex, selfHpPct))
                continue;
            if (getCooldown(i) > 0)
                continue;
            if (getConsecutive(i) >= Mathf.Max(1, move.maxConsecutive))
                continue;
            if (ViolatesNoRepeat(definition, move, i, recentHistoryCount, peekLastHistory))
                continue;

            return i;
        }

        return -1;
    }

    private static List<int> BuildCandidateIndices(
        EnemyDefinitionSO definition,
        int nextTurnIndex,
        float selfHpPct,
        int recentHistoryCount,
        Func<int, int> getCooldown,
        Func<int, int> getConsecutive,
        Func<int> peekLastHistory,
        bool ignoreCooldownAndRepeatRules)
    {
        List<int> candidates = new List<int>(definition.moves.Count);
        for (int i = 0; i < definition.moves.Count; i++)
        {
            EnemyDefinitionSO.EnemyMoveSlot move = definition.moves[i];
            if (!IsMoveBaseEligible(move, nextTurnIndex, selfHpPct))
                continue;
            if (move.weight <= 0)
                continue;
            if (!ignoreCooldownAndRepeatRules && getCooldown(i) > 0)
                continue;
            if (getConsecutive(i) >= Mathf.Max(1, move.maxConsecutive))
                continue;
            if (!ignoreCooldownAndRepeatRules && ViolatesNoRepeat(definition, move, i, recentHistoryCount, peekLastHistory))
                continue;

            candidates.Add(i);
        }

        return candidates;
    }

    private static bool IsMoveBaseEligible(EnemyDefinitionSO.EnemyMoveSlot move, int nextTurnIndex, float selfHpPct)
    {
        if (move == null || !move.HasAnySkill)
            return false;
        if (nextTurnIndex < move.minTurnIndex)
            return false;
        if (selfHpPct < move.hpPctMin || selfHpPct > move.hpPctMax)
            return false;

        return true;
    }

    private static bool ViolatesNoRepeat(
        EnemyDefinitionSO definition,
        EnemyDefinitionSO.EnemyMoveSlot move,
        int moveIndex,
        int recentHistoryCount,
        Func<int> peekLastHistory)
    {
        if (!definition.noRepeatTwice || move.ignoreNoRepeat || recentHistoryCount <= 0)
            return false;

        return peekLastHistory() == moveIndex;
    }

    private static int WeightedPick(EnemyDefinitionSO definition, List<int> candidateIndices)
    {
        int total = 0;
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int idx = candidateIndices[i];
            total += Mathf.Max(0, definition.moves[idx].weight);
        }

        if (total <= 0)
            return candidateIndices[UnityEngine.Random.Range(0, candidateIndices.Count)];

        int roll = UnityEngine.Random.Range(0, total);
        int accumulated = 0;
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int idx = candidateIndices[i];
            accumulated += Mathf.Max(0, definition.moves[idx].weight);
            if (roll < accumulated)
                return idx;
        }

        return candidateIndices[candidateIndices.Count - 1];
    }
}
