public enum FireConditionPreset
{
    None,
    AnyBaseOdd,
    AnyBaseEven,
    AnyDieCrit,
    AnyDieFail,
    ExactAllBasesEqualN,
    AnyBaseEqualsN,
    HighestBaseEqualsN,
    LowestBaseEqualsN,
    CurrentFocusGreaterOrEqualN,
    OccupiedSlotsEqualsN,
    RemainingSlotsEqualsN,
    EnemiesWithBurnGreaterOrEqualN,
    TargetHasBurn,
}

public enum SkillConditionFamily
{
    DiceParity,
    CritFail,
    ExactValue,
    LocalGroupRelation,
    Resource,
    TargetState,
    BoardState,
}

public enum ConditionEditorMode
{
    Builder,
    Advanced,
}

public enum DiceParityConditionPreset
{
    Even,
    Odd,
    AnyBaseOdd,
    AnyBaseEven,
    AllBasesOdd,
    AllBasesEven,
    MixedParity,
    HighestBaseOdd,
    HighestBaseEven,
    LowestBaseOdd,
    LowestBaseEven,
    FirstBaseOdd,
    FirstBaseEven,
    MiddleBaseOdd,
    MiddleBaseEven,
    LastBaseOdd,
    LastBaseEven,
}

public enum CritFailConditionPreset
{
    Crit,
    Fail,
    AnyDieCrit,
    AnyDieFail,
    FirstDieCrit,
    FirstDieFail,
    LastDieCrit,
    LastDieFail,
}

public enum ExactValueConditionPreset
{
    DieEqualsX = 0,
    GroupContainsPattern = 1,
    RandomExactNumberOwned = 2,
    RandomExactNumberRandom = 3,
    AnyBaseEqualsN,
    HighestBaseEqualsN,
    LowestBaseEqualsN,
    AllBasesEqualN,
    FirstBaseEqualsN,
    MiddleBaseEqualsN,
    LastBaseEqualsN,
}

public enum ResourceConditionPreset
{
    CurrentFocusGreaterOrEqualN,
    PlayerGuardGreaterOrEqualN,
    TargetGuardGreaterOrEqualN,
    CurrentGuardGreaterOrEqualN,
}

public enum LocalGroupConditionPreset
{
    Highest,
    Lowest,
}

public enum LocalGroupRelationMode
{
    SelfPosition,
    NeighborRelation,
    SplitRole,
}

public enum LocalGroupRelationSide
{
    Left,
    Right,
}

public enum SkillExactConditionMode
{
    DieEqualsX,
    GroupContainsPattern,
    RandomExactNumberOwned,
    RandomExactNumberRandom,
}

public enum TargetStateConditionPreset
{
    TargetHasBurn,
    TargetHasFreeze,
    TargetHasChilled,
    TargetHasMark,
    TargetHasBleed,
    TargetHasStagger,
    StatusHistoryTodo,
}

public enum BoardStateConditionPreset
{
    AliveEnemiesGreaterOrEqualN,
    EnemiesWithStatusGreaterOrEqualN,
    EnemiesWithBurnGreaterOrEqualN,
    MarkedEnemiesGreaterOrEqualN,
    TotalBleedOnBoardGreaterOrEqualN,
}

public enum PassiveConditionAxis
{
    None,
    RuleBending,
    Resource,
    CritFail,
    Parity,
    ExactValue,
    TargetState,
    BoardState,
}

public enum PassiveEffectConditionFamily
{
    Parity,
    CritFail,
    ExactValue,
    Resource,
    TargetState,
    BoardState,
}

public enum IceConditionPreset
{
    None,
    AnyBaseOdd,
    AnyBaseEven,
    AnyDieCrit,
    AnyDieFail,
    ExactAllBasesEqualN,
    AnyBaseEqualsN,
    HighestBaseEqualsN,
    LowestBaseEqualsN,
    CurrentFocusGreaterOrEqualN,
    OccupiedSlotsEqualsN,
    RemainingSlotsEqualsN,
    TargetHasFreeze,
    TargetHasChilled,
}

public enum LightningConditionPreset
{
    None,
    AnyBaseOdd,
    AnyBaseEven,
    AnyDieCrit,
    AnyDieFail,
    AnyBaseEqualsN,
    HighestBaseEqualsN,
    LowestBaseEqualsN,
    CurrentFocusGreaterOrEqualN,
    OccupiedSlotsEqualsN,
    RemainingSlotsEqualsN,
    MarkedEnemiesGreaterOrEqualN,
    TargetHasMark,
}

public enum PhysicalConditionPreset
{
    None,
    AnyBaseOdd,
    AnyBaseEven,
    AnyDieCrit,
    AnyDieFail,
    AnyBaseEqualsN,
    HighestBaseEqualsN,
    LowestBaseEqualsN,
    CurrentFocusGreaterOrEqualN,
    OccupiedSlotsEqualsN,
    RemainingSlotsEqualsN,
}

public enum BleedConditionPreset
{
    None,
    AnyBaseOdd,
    AnyBaseEven,
    AnyDieCrit,
    AnyDieFail,
    AnyBaseEqualsN,
    HighestBaseEqualsN,
    LowestBaseEqualsN,
    CurrentFocusGreaterOrEqualN,
    OccupiedSlotsEqualsN,
    RemainingSlotsEqualsN,
    TotalBleedOnBoardGreaterOrEqualN,
    TargetHasBleed,
}
