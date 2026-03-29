using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ConditionalOutcomeType
{
    None,
    DealDamage,
    ApplyBurn,
    GainGuard,
    GainAddedValue,
}

public enum ConditionalOutcomeValueMode
{
    Flat,
    X,
}

public enum BaseEffectValueMode
{
    Flat,
    X,
}

[Serializable]
public class SkillConditionalOutcomeData
{
    [ToggleLeft]
    [LabelText("Enable")]
    public bool enabled;

    [ShowIf(nameof(enabled))]
    [EnumToggleButtons]
    [LabelText("Outcome")]
    public ConditionalOutcomeType type = ConditionalOutcomeType.None;

    [ShowIf(nameof(ShowValueMode))]
    [EnumToggleButtons]
    [LabelText("Value Mode")]
    public ConditionalOutcomeValueMode valueMode = ConditionalOutcomeValueMode.Flat;

    [ShowIf(nameof(ShowFlatValue))]
    [MinValue(0)]
    [LabelText("Flat Value")]
    public int flatValue = 2;

    [ShowIf(nameof(ShowBurnTurns))]
    [MinValue(1)]
    [LabelText("Burn Turns")]
    public int burnTurns = 3;

    [ShowIf(nameof(ShowXHint))]
    [InfoBox("X dùng công thức hiện tại của skill: X = Base Value + Added Value.", InfoMessageType.Info)]
    [HideInInspector]
    public bool _xHint = true;

    private bool ShowValueMode => enabled && type != ConditionalOutcomeType.None;
    private bool ShowFlatValue => enabled && type != ConditionalOutcomeType.None && valueMode == ConditionalOutcomeValueMode.Flat;
    private bool ShowBurnTurns => enabled && type == ConditionalOutcomeType.ApplyBurn;
    private bool ShowXHint => enabled && type != ConditionalOutcomeType.None && valueMode == ConditionalOutcomeValueMode.X;
}
