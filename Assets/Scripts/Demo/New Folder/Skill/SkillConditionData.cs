using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class SkillConditionData
{
    [EnumToggleButtons]
    public SkillConditionScope scope = SkillConditionScope.SlotBound;

    [EnumToggleButtons]
    public SkillConditionKind kind = SkillConditionKind.Parity;

    // --- Parity ---
    [ShowIf("@kind == SkillConditionKind.Parity")]
    [EnumToggleButtons]
    public SkillParityRule parityRule = SkillParityRule.AnyOdd;

    // --- Threshold (uses SUM of considered dice) ---
    [ShowIf("@kind == SkillConditionKind.Threshold")]
    [EnumToggleButtons]
    public ThresholdCompare thresholdCompare = ThresholdCompare.GreaterOrEqual;

    [ShowIf("@kind == SkillConditionKind.Threshold")]
    [Min(0)]
    public int thresholdValue = 6;

    // --- Match ---
    [ShowIf("@kind == SkillConditionKind.Match")]
    [EnumToggleButtons]
    public DiceMatchRule matchRule = DiceMatchRule.AnyPair;

    // --- Straight ---
    [ShowIf("@kind == SkillConditionKind.Straight")]
    [EnumToggleButtons]
    public StraightRule straightRule = StraightRule.AnyConsecutive;

    /// <summary>
    /// Evaluate against a list of dice values.
    /// </summary>
    public bool Evaluate(IReadOnlyList<int> diceValues)
    {
        if (diceValues == null || diceValues.Count == 0)
            return false;

        switch (kind)
        {
            case SkillConditionKind.Parity:
                return EvaluateParity(diceValues, parityRule);

            case SkillConditionKind.Threshold:
                return EvaluateThreshold(diceValues, thresholdCompare, thresholdValue);

            case SkillConditionKind.Match:
                return EvaluateMatch(diceValues, matchRule);

            case SkillConditionKind.Straight:
                return EvaluateStraight(diceValues, straightRule);

            default:
                return false;
        }
    }

    private static bool EvaluateParity(IReadOnlyList<int> dice, SkillParityRule rule)
    {
        bool anyOdd = false, anyEven = false;
        bool allOdd = true, allEven = true;

        for (int i = 0; i < dice.Count; i++)
        {
            int v = dice[i];
            bool odd = (v % 2) != 0;
            anyOdd |= odd;
            anyEven |= !odd;
            allOdd &= odd;
            allEven &= !odd;
        }

        return rule switch
        {
            SkillParityRule.AnyOdd => anyOdd,
            SkillParityRule.AnyEven => anyEven,
            SkillParityRule.AllOdd => allOdd,
            SkillParityRule.AllEven => allEven,
            _ => false,
        };
    }

    private static bool EvaluateThreshold(IReadOnlyList<int> dice, ThresholdCompare cmp, int threshold)
    {
        int sum = 0;
        for (int i = 0; i < dice.Count; i++) sum += dice[i];

        return cmp switch
        {
            ThresholdCompare.GreaterOrEqual => sum >= threshold,
            ThresholdCompare.LessOrEqual => sum <= threshold,
            _ => false,
        };
    }

    private static bool EvaluateMatch(IReadOnlyList<int> dice, DiceMatchRule rule)
    {
        if (dice.Count < 2) return false;

        // Frequency table (max dice faces unknown, so use Dictionary)
        var freq = new Dictionary<int, int>();
        for (int i = 0; i < dice.Count; i++)
        {
            int v = dice[i];
            if (!freq.TryGetValue(v, out int c)) c = 0;
            freq[v] = c + 1;
        }

        bool anyPair = false;
        bool allSame = (freq.Count == 1);

        foreach (var kv in freq)
        {
            if (kv.Value >= 2) anyPair = true;
        }

        return rule switch
        {
            DiceMatchRule.AnyPair => anyPair,
            DiceMatchRule.ThreeOfAKind => (dice.Count >= 3 && allSame),
            _ => false,
        };
    }

    private static bool EvaluateStraight(IReadOnlyList<int> dice, StraightRule rule)
    {
        if (dice.Count < 2) return false;

        // Make a sorted copy
        int[] a = new int[dice.Count];
        for (int i = 0; i < dice.Count; i++) a[i] = dice[i];
        Array.Sort(a);

        bool consecutive = true;
        for (int i = 1; i < a.Length; i++)
        {
            if (a[i] != a[i - 1] + 1) { consecutive = false; break; }
        }

        if (rule == StraightRule.AnyConsecutive)
            return consecutive;

        if (a.Length == 3)
        {
            if (rule == StraightRule.Exact123) return (a[0] == 1 && a[1] == 2 && a[2] == 3);
            if (rule == StraightRule.Exact456) return (a[0] == 4 && a[1] == 5 && a[2] == 6);
        }

        return false;
    }
}

public enum SkillConditionScope
{
    SlotBound,
    Global,
}

public enum SkillConditionKind
{
    Parity,
    Threshold,
    Match,
    Straight,
}

public enum SkillParityRule
{
    AnyOdd,
    AnyEven,
    AllOdd,
    AllEven,
}

public enum ThresholdCompare
{
    GreaterOrEqual,
    LessOrEqual,
}

public enum DiceMatchRule
{
    AnyPair,
    ThreeOfAKind,
}

public enum StraightRule
{
    AnyConsecutive,
    Exact123,
    Exact456,
}
