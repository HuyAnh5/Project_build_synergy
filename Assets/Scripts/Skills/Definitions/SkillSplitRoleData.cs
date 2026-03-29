using System;
using Sirenix.OdinInspector;

public enum SplitRoleBranchOutcome
{
    None,
    Burn,
    Guard,
}

[Serializable]
public class SkillSplitRoleData
{
    [ToggleLeft]
    [LabelText("Enable")]
    public bool enabled;

    [ShowIf(nameof(enabled))]
    [EnumToggleButtons]
    [LabelText("Lowest Selected")]
    public SplitRoleBranchOutcome lowestOutcome = SplitRoleBranchOutcome.None;

    [ShowIf(nameof(enabled))]
    [EnumToggleButtons]
    [LabelText("Highest Selected")]
    public SplitRoleBranchOutcome highestOutcome = SplitRoleBranchOutcome.None;

    [ShowIf(nameof(enabled))]
    [MinValue(1)]
    [LabelText("Burn Turns")]
    public int burnTurns = 3;
}
