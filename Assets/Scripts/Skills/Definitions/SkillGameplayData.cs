using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum SkillEffectType
{
    DealDamage = 0,
    ApplyStatus = 1,
    ConsumeStatus = 2,
    Heal = 3,
    GainGuard = 4,
    GainAP = 5,
    ModifyCost = 6,
    AddValue = 7,
    DealSecondaryDamage = 8
}

public enum SkillEffectTarget
{
    SelectedEnemy = 0,
    Self = 1,
    AllEnemies = 2,
    RowEnemies = 3
}

public enum SkillValueMode
{
    Fixed,
    AddedValueScaled,
    TargetStatusStacksScaled,
    ConsumedStatusStacksScaled,
    ConsumedStatusStacksDividedScaled,
    MatchingBaseValueCountScaled,
    ActionX,
    HighestBaseValueScaled,
    FirstResolvedValueInGroup,
    LastResolvedValueInGroup,
    TotalBleedOnBoardScaled,
    LastEnemyTurnHpLostScaled
}

public enum SkillRequirementType
{
    Condition
}

public enum SkillCustomHookKind
{
    None,
    LegacyBehaviorId,
    CustomRuntimeHook
}

[Serializable, InlineProperty]
public class SkillValueData
{
    [LabelText("Base")]
    [MinValue(0)]
    public int baseAmount = 0;

    [LabelText("Mode")]
    public SkillValueMode mode = SkillValueMode.Fixed;

    [ShowIf(nameof(UsesStatusReference))]
    [LabelText("Status")]
    public StatusKind status = StatusKind.Burn;

    [ShowIf(nameof(UsesDivisor))]
    [LabelText("Divisor")]
    [MinValue(1)]
    public int divisor = 1;

    [ShowIf(nameof(UsesMatchBaseValue))]
    [LabelText("Match Base")]
    public int matchBaseValue = 7;

    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(10)]
    public string Summary
    {
        get
        {
            switch (mode)
            {
                case SkillValueMode.AddedValueScaled:
                    return $"Blue {baseAmount} + Added Value";
                case SkillValueMode.TargetStatusStacksScaled:
                    return $"{baseAmount} x target {status}";
                case SkillValueMode.ConsumedStatusStacksScaled:
                    return $"{baseAmount} x consumed {status}";
                case SkillValueMode.ConsumedStatusStacksDividedScaled:
                    return $"{baseAmount} x consumed {status} / {Mathf.Max(1, divisor)}";
                case SkillValueMode.MatchingBaseValueCountScaled:
                    return $"{baseAmount} x base={matchBaseValue} matches";
                case SkillValueMode.ActionX:
                    return "Action X";
                case SkillValueMode.HighestBaseValueScaled:
                    return "Highest Base + Added Value";
                case SkillValueMode.FirstResolvedValueInGroup:
                    return "First slot resolved value";
                case SkillValueMode.LastResolvedValueInGroup:
                    return "Last slot resolved value";
                case SkillValueMode.TotalBleedOnBoardScaled:
                    return "Total Bleed on board + Added Value";
                case SkillValueMode.LastEnemyTurnHpLostScaled:
                    return "Last enemy turn HP lost + Added Value";
                case SkillValueMode.Fixed:
                default:
                    return $"Fixed {baseAmount}";
            }
        }
    }

    private bool UsesStatusReference()
        => mode == SkillValueMode.TargetStatusStacksScaled ||
           mode == SkillValueMode.ConsumedStatusStacksScaled ||
           mode == SkillValueMode.ConsumedStatusStacksDividedScaled;

    private bool UsesDivisor()
        => mode == SkillValueMode.ConsumedStatusStacksDividedScaled;

    private bool UsesMatchBaseValue()
        => mode == SkillValueMode.MatchingBaseValueCountScaled;
}

[Serializable, InlineProperty]
public class SkillEffectData
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-20)]
    public string Summary => BuildSummary();

    [LabelText("Type")]
    public SkillEffectType type = SkillEffectType.DealDamage;

    [LabelText("Target")]
    public SkillEffectTarget target = SkillEffectTarget.SelectedEnemy;

    [ShowIf(nameof(UsesStatus))]
    [LabelText("Status")]
    public StatusKind status = StatusKind.Burn;

    [ShowIf(nameof(UsesValue))]
    [BoxGroup("Value")]
    [HideLabel]
    public SkillValueData value = new SkillValueData();

    [ToggleLeft]
    [LabelText("Show In Preview")]
    public bool previewable = true;

    private bool UsesStatus()
        => type == SkillEffectType.ApplyStatus || type == SkillEffectType.ConsumeStatus;

    private bool UsesValue()
        => type != SkillEffectType.ConsumeStatus;

    private string BuildSummary()
    {
        string valueText = UsesValue() && value != null ? value.Summary : string.Empty;
        switch (type)
        {
            case SkillEffectType.DealDamage:
                return $"Deal {valueText} damage to {target}";
            case SkillEffectType.DealSecondaryDamage:
                return $"Deal secondary {valueText} damage to {target}";
            case SkillEffectType.ApplyStatus:
                return $"Apply {valueText} {status} to {target}";
            case SkillEffectType.ConsumeStatus:
                return $"Consume {status} on {target}";
            case SkillEffectType.Heal:
                return $"Heal {target} for {valueText}";
            case SkillEffectType.GainGuard:
                return $"Gain {valueText} Guard on {target}";
            case SkillEffectType.GainAP:
                return $"Gain {valueText} AP";
            case SkillEffectType.ModifyCost:
                return $"Modify cost by {valueText}";
            case SkillEffectType.AddValue:
                return $"Add value {valueText}";
            default:
                return type.ToString();
        }
    }
}

[Serializable, InlineProperty]
public class SkillRequirementData
{
    [LabelText("Type")]
    public SkillRequirementType type = SkillRequirementType.Condition;

    [LabelText("Failure")]
    public string failureText = "Requirement not met.";

    [FoldoutGroup("Condition", expanded: true)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData
    {
        scope = SkillConditionScope.Global,
        logic = SkillConditionLogic.All,
        clauses = new List<SkillConditionClause>()
    };
}

[Serializable, InlineProperty]
public class SkillConditionalOutcomeDataV2
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-20)]
    public string Summary => condition != null && condition.HasAnyClause
        ? $"When {condition.logic} condition is met"
        : "When condition is missing";

    [FoldoutGroup("When", expanded: true)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    [FoldoutGroup("Then", expanded: true)]
    [HideLabel]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<SkillEffectData> effects = new List<SkillEffectData>();
}

[Serializable, InlineProperty]
public class SkillCustomHookData
{
    [EnumToggleButtons]
    [LabelText("Hook")]
    public SkillCustomHookKind kind = SkillCustomHookKind.None;

    [ShowIf("@kind != SkillCustomHookKind.None")]
    [TextArea(2, 4)]
    [LabelText("Reason")]
    public string reason;
}

[Serializable, InlineProperty]
public class SkillGameplayData
{
    [BoxGroup("Pipeline")]
    [ToggleLeft]
    [LabelText("Use New Gameplay Pipeline")]
    [InfoBox("OFF = legacy runtime remains source of truth. Turn ON only after this asset is migrated and tested.", InfoMessageType.Warning, nameof(ShowPipelineWarning))]
    public bool useNewGameplayPipeline = false;

    [BoxGroup("Description")]
    [TextArea(2, 5)]
    [LabelText("Description")]
    [InfoBox("Write skill tooltip text here. Preview and tooltip read this field; leave empty to auto-generate from gameplay effects.", InfoMessageType.Info)]
    public string descriptionTemplate;

    [BoxGroup("Requirements")]
    [InfoBox("None", InfoMessageType.None, nameof(HasNoRequirements))]
    [HideLabel]
    [ListDrawerSettings(Expanded = false, DraggableItems = true, ShowIndexLabels = false)]
    public List<SkillRequirementData> requirements = new List<SkillRequirementData>();

    [BoxGroup("Base Effects")]
    [InfoBox("Add the effects that always run when the skill casts.", InfoMessageType.Info, nameof(HasNoBaseEffects))]
    [HideLabel]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<SkillEffectData> baseEffects = new List<SkillEffectData>();

    [BoxGroup("Conditional Outcomes")]
    [InfoBox("None", InfoMessageType.None, nameof(HasNoConditionalOutcomes))]
    [HideLabel]
    [ListDrawerSettings(Expanded = false, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<SkillConditionalOutcomeDataV2> conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();

    [FoldoutGroup("Advanced", expanded: false)]
    [HideLabel]
    public SkillCustomHookData customHook = new SkillCustomHookData();

    private bool ShowPipelineWarning() => !useNewGameplayPipeline;
    private bool HasNoRequirements() => requirements == null || requirements.Count == 0;
    private bool HasNoBaseEffects() => baseEffects == null || baseEffects.Count == 0;
    private bool HasNoConditionalOutcomes() => conditionalOutcomes == null || conditionalOutcomes.Count == 0;
}

