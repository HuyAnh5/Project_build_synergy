using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum SkillConditionScope
{
    SlotBound,
    Global,
}

public enum SkillConditionLogic
{
    All,
    Any,
}

public enum SkillConditionReference
{
    AnyBaseValue,
    FirstBaseValueInGroup,
    MiddleBaseValueInGroup,
    LastBaseValueInGroup,
    HighestBaseValueInGroup,
    LowestBaseValueInGroup,
    TotalBaseValueInGroup,
    TotalResolvedValueInGroup,
    AllBaseValuesOdd,
    AllBaseValuesEven,
    MixedParityInGroup,
    AnyDieCrit,
    AnyDieFail,
    FirstDieCrit,
    MiddleDieCrit,
    LastDieCrit,
    FirstDieFail,
    MiddleDieFail,
    LastDieFail,
    AllDiceCrit,
    AllDiceFail,
    CurrentFocus,
    CurrentGuard,
    TargetGuard,
    OccupiedSlots,
    RemainingSlots,
    HasFirstDieInGroup,
    HasMiddleDieInGroup,
    HasLastDieInGroup,
    HighestBaseIsInFirstSlot,
    HighestBaseIsInLastSlot,
    LowestBaseIsInFirstSlot,
    LowestBaseIsInLastSlot,
    EnemiesWithBurnCount,
    MarkedEnemiesCount,
    TotalBleedOnBoard,
    AliveEnemiesCount,
    EnemiesWithStatusCount,
    IsLeftmostAction,
    IsRightmostAction,
    TargetHasBurn,
    TargetHasFreeze,
    TargetHasChilled,
    TargetHasMark,
    TargetHasBleed,
    TargetHasStagger,
    AnyAddedValueInGroup,
    FirstAddedValueInGroup,
    MiddleAddedValueInGroup,
    LastAddedValueInGroup,
    HighestAddedValueInGroup,
    TotalAddedValueInGroup,
}

public enum SkillConditionComparison
{
    [InspectorName("Odd")]
    IsOdd,
    [InspectorName("Even")]
    IsEven,
    [InspectorName("=")]
    Equals,
    [InspectorName("!=")]
    NotEquals,
    [InspectorName(">=")]
    GreaterOrEqual,
    [InspectorName("<=")]
    LessOrEqual,
    [InspectorName("True")]
    IsTrue,
    [InspectorName("False")]
    IsFalse,
}

[Serializable, InlineProperty]
public class SkillConditionClause
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-10)]
    public string Summary => NeedsValue
        ? $"{reference} {comparison} {value}"
        : $"{reference} {comparison}";

    [LabelText("Reference")]
    public SkillConditionReference reference = SkillConditionReference.AnyBaseValue;

    [LabelText("Compare")]
    public SkillConditionComparison comparison = SkillConditionComparison.IsOdd;

    [ShowIf(nameof(NeedsValue))]
    [LabelText("Value")]
    public int value = 0;

    public bool NeedsValue =>
        comparison == SkillConditionComparison.Equals ||
        comparison == SkillConditionComparison.NotEquals ||
        comparison == SkillConditionComparison.GreaterOrEqual ||
        comparison == SkillConditionComparison.LessOrEqual;
}

[Serializable]
public class SkillConditionContext
{
    public SkillConditionScope scope = SkillConditionScope.SlotBound;
    public IReadOnlyList<int> localBaseValues;
    public IReadOnlyList<int> localOutputBaseValues;
    public IReadOnlyList<bool> localNumericFlags;
    public IReadOnlyList<int> localResolvedValues;
    public IReadOnlyList<bool> localCritFlags;
    public IReadOnlyList<bool> localFailFlags;
    public int currentFocus;
    public int currentGuard;
    public int targetGuard;
    public int occupiedSlots;
    public int remainingSlots;
    public int enemiesWithBurnCount;
    public int markedEnemiesCount;
    public int totalBleedOnBoard;
    public int aliveEnemiesCount;
    public int enemiesWithStatusCount;
    public bool isLeftmostAction;
    public bool isRightmostAction;
    public bool targetHasBurn;
    public bool targetHasFreeze;
    public bool targetHasChilled;
    public bool targetHasMark;
    public bool targetHasBleed;
    public bool targetHasStagger;
}

[Serializable]
// Runtime condition asset data and clause dispatcher for skill authoring.
public partial class SkillConditionData
{
    [HideLabel]
    [EnumToggleButtons]
    public SkillConditionScope scope = SkillConditionScope.SlotBound;

    [HideLabel]
    [EnumToggleButtons]
    public SkillConditionLogic logic = SkillConditionLogic.All;

    [HideLabel]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<SkillConditionClause> clauses = new List<SkillConditionClause>();

    public bool HasAnyClause => clauses != null && clauses.Count > 0;

    public bool Evaluate(SkillConditionContext context)
    {
        if (context == null || clauses == null || clauses.Count == 0)
            return false;

        bool anyMatched = false;
        for (int i = 0; i < clauses.Count; i++)
        {
            SkillConditionClause clause = clauses[i];
            if (clause == null)
                continue;

            bool matched = EvaluateClause(clause, context);
            if (logic == SkillConditionLogic.All && !matched)
                return false;
            if (logic == SkillConditionLogic.Any && matched)
                return true;

            anyMatched |= matched;
        }

        return logic == SkillConditionLogic.All ? anyMatched : false;
    }

    private static bool EvaluateClause(SkillConditionClause clause, SkillConditionContext context)
    {
        switch (clause.reference)
        {
            case SkillConditionReference.AnyBaseValue:
                return EvaluateAnyBaseValue(clause, context.localBaseValues, context.localNumericFlags);

            case SkillConditionReference.FirstBaseValueInGroup:
                return EvaluateIndexedValue(clause, context.localBaseValues, context.localNumericFlags, 0);

            case SkillConditionReference.MiddleBaseValueInGroup:
                return EvaluateIndexedValue(clause, context.localBaseValues, context.localNumericFlags, 1);

            case SkillConditionReference.LastBaseValueInGroup:
                return EvaluateIndexedValue(clause, context.localBaseValues, context.localNumericFlags, GetLastIndex(context.localBaseValues));

            case SkillConditionReference.HighestBaseValueInGroup:
                return EvaluateInt(clause, GetHighest(context.localBaseValues, context.localNumericFlags));

            case SkillConditionReference.LowestBaseValueInGroup:
                return EvaluateInt(clause, GetLowest(context.localBaseValues, context.localNumericFlags));

            case SkillConditionReference.TotalBaseValueInGroup:
                return EvaluateInt(clause, Sum(context.localBaseValues, context.localNumericFlags));

            case SkillConditionReference.TotalResolvedValueInGroup:
                return EvaluateInt(clause, Sum(context.localResolvedValues));

            case SkillConditionReference.AllBaseValuesOdd:
                return EvaluateBool(clause, AreAllValuesOdd(context.localBaseValues, context.localNumericFlags));

            case SkillConditionReference.AllBaseValuesEven:
                return EvaluateBool(clause, AreAllValuesEven(context.localBaseValues, context.localNumericFlags));

            case SkillConditionReference.MixedParityInGroup:
                return EvaluateBool(clause, HasMixedParity(context.localBaseValues, context.localNumericFlags));

            case SkillConditionReference.AnyDieCrit:
                return EvaluateBool(clause, AnyTrue(context.localCritFlags));

            case SkillConditionReference.AnyDieFail:
                return EvaluateBool(clause, AnyTrue(context.localFailFlags));

            case SkillConditionReference.FirstDieCrit:
                return EvaluateIndexedBool(clause, context.localCritFlags, 0);

            case SkillConditionReference.MiddleDieCrit:
                return EvaluateIndexedBool(clause, context.localCritFlags, 1);

            case SkillConditionReference.LastDieCrit:
                return EvaluateIndexedBool(clause, context.localCritFlags, GetLastIndex(context.localCritFlags));

            case SkillConditionReference.FirstDieFail:
                return EvaluateIndexedBool(clause, context.localFailFlags, 0);

            case SkillConditionReference.MiddleDieFail:
                return EvaluateIndexedBool(clause, context.localFailFlags, 1);

            case SkillConditionReference.LastDieFail:
                return EvaluateIndexedBool(clause, context.localFailFlags, GetLastIndex(context.localFailFlags));

            case SkillConditionReference.AllDiceCrit:
                return EvaluateBool(clause, AllTrue(context.localCritFlags));

            case SkillConditionReference.AllDiceFail:
                return EvaluateBool(clause, AllTrue(context.localFailFlags));

            case SkillConditionReference.CurrentFocus:
                return EvaluateInt(clause, Mathf.Max(0, context.currentFocus));

            case SkillConditionReference.CurrentGuard:
                return EvaluateInt(clause, Mathf.Max(0, context.currentGuard));

            case SkillConditionReference.TargetGuard:
                return EvaluateInt(clause, Mathf.Max(0, context.targetGuard));

            case SkillConditionReference.OccupiedSlots:
                return EvaluateInt(clause, Mathf.Max(0, context.occupiedSlots));

            case SkillConditionReference.RemainingSlots:
                return EvaluateInt(clause, Mathf.Max(0, context.remainingSlots));

            case SkillConditionReference.HasFirstDieInGroup:
                return EvaluateBool(clause, HasIndex(context.localBaseValues, 0));

            case SkillConditionReference.HasMiddleDieInGroup:
                return EvaluateBool(clause, HasIndex(context.localBaseValues, 1));

            case SkillConditionReference.HasLastDieInGroup:
                return EvaluateBool(clause, GetLastIndex(context.localBaseValues) >= 0);

            case SkillConditionReference.HighestBaseIsInFirstSlot:
                return EvaluateBool(clause, IsHighestAtIndex(context.localBaseValues, context.localNumericFlags, 0));

            case SkillConditionReference.HighestBaseIsInLastSlot:
                return EvaluateBool(clause, IsHighestAtIndex(context.localBaseValues, context.localNumericFlags, GetLastIndex(context.localBaseValues)));

            case SkillConditionReference.LowestBaseIsInFirstSlot:
                return EvaluateBool(clause, IsLowestAtIndex(context.localBaseValues, context.localNumericFlags, 0));

            case SkillConditionReference.LowestBaseIsInLastSlot:
                return EvaluateBool(clause, IsLowestAtIndex(context.localBaseValues, context.localNumericFlags, GetLastIndex(context.localBaseValues)));

            case SkillConditionReference.EnemiesWithBurnCount:
                return EvaluateInt(clause, Mathf.Max(0, context.enemiesWithBurnCount));

            case SkillConditionReference.MarkedEnemiesCount:
                return EvaluateInt(clause, Mathf.Max(0, context.markedEnemiesCount));

            case SkillConditionReference.TotalBleedOnBoard:
                return EvaluateInt(clause, Mathf.Max(0, context.totalBleedOnBoard));

            case SkillConditionReference.AliveEnemiesCount:
                return EvaluateInt(clause, Mathf.Max(0, context.aliveEnemiesCount));

            case SkillConditionReference.EnemiesWithStatusCount:
                return EvaluateInt(clause, Mathf.Max(0, context.enemiesWithStatusCount));

            case SkillConditionReference.IsLeftmostAction:
                return EvaluateBool(clause, context.isLeftmostAction);

            case SkillConditionReference.IsRightmostAction:
                return EvaluateBool(clause, context.isRightmostAction);

            case SkillConditionReference.TargetHasBurn:
                return EvaluateBool(clause, context.targetHasBurn);

            case SkillConditionReference.TargetHasFreeze:
                return EvaluateBool(clause, context.targetHasFreeze);

            case SkillConditionReference.TargetHasChilled:
                return EvaluateBool(clause, context.targetHasChilled);

            case SkillConditionReference.TargetHasMark:
                return EvaluateBool(clause, context.targetHasMark);

            case SkillConditionReference.TargetHasBleed:
                return EvaluateBool(clause, context.targetHasBleed);

            case SkillConditionReference.TargetHasStagger:
                return EvaluateBool(clause, context.targetHasStagger);

            case SkillConditionReference.AnyAddedValueInGroup:
                return EvaluateAnyAddedValue(clause, context.localOutputBaseValues, context.localBaseValues, context.localResolvedValues, context.localNumericFlags);

            case SkillConditionReference.FirstAddedValueInGroup:
                return EvaluateIndexedAddedValue(clause, context.localOutputBaseValues, context.localBaseValues, context.localResolvedValues, context.localNumericFlags, 0);

            case SkillConditionReference.MiddleAddedValueInGroup:
                return EvaluateIndexedAddedValue(clause, context.localOutputBaseValues, context.localBaseValues, context.localResolvedValues, context.localNumericFlags, 1);

            case SkillConditionReference.LastAddedValueInGroup:
                return EvaluateIndexedAddedValue(clause, context.localOutputBaseValues, context.localBaseValues, context.localResolvedValues, context.localNumericFlags, GetLastAddedValueIndex(context.localOutputBaseValues, context.localBaseValues, context.localResolvedValues));

            case SkillConditionReference.HighestAddedValueInGroup:
                return EvaluateInt(clause, GetHighestAddedValue(context.localOutputBaseValues, context.localBaseValues, context.localResolvedValues, context.localNumericFlags));

            case SkillConditionReference.TotalAddedValueInGroup:
                return EvaluateInt(clause, SumAddedValues(context.localOutputBaseValues, context.localBaseValues, context.localResolvedValues, context.localNumericFlags));

            default:
                return false;
        }
    }

}
