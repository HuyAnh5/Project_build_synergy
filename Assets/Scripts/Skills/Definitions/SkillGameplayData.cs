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
    DealSecondaryDamage = 8,
    ClearGuard = 9
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

    [ShowIf(nameof(UsesHitCount))]
    [MinValue(1)]
    [LabelText("Hit Count")]
    [InfoBox("Repeats the whole skill action and its dice/attack animation. Damage is applied separately on every execution.", InfoMessageType.Info)]
    public int hitCount = 1;

    [ToggleLeft]
    [LabelText("Show In Preview")]
    public bool previewable = true;

    private bool UsesStatus()
        => type == SkillEffectType.ApplyStatus || type == SkillEffectType.ConsumeStatus;

    private bool UsesValue()
        => type != SkillEffectType.ConsumeStatus && type != SkillEffectType.ClearGuard;

    private bool UsesHitCount()
        => type == SkillEffectType.DealDamage || type == SkillEffectType.DealSecondaryDamage;

    private string BuildSummary()
    {
        string valueText = UsesValue() && value != null ? value.Summary : string.Empty;
        switch (type)
        {
            case SkillEffectType.DealDamage:
                return BuildDamageSummary("Deal", valueText);
            case SkillEffectType.DealSecondaryDamage:
                return BuildDamageSummary("Deal secondary", valueText);
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
            case SkillEffectType.ClearGuard:
                return $"Clear all Guard on {target}";
            default:
                return type.ToString();
        }
    }

    private string BuildDamageSummary(string prefix, string valueText)
    {
        int resolvedHitCount = Mathf.Max(1, hitCount);
        string repeatText = resolvedHitCount > 1 ? $" {resolvedHitCount} times" : string.Empty;
        return $"{prefix} {valueText} damage{repeatText} to {target}";
    }
}

[Serializable, InlineProperty]
public class SkillRequirementData
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-20)]
    public string Summary => condition != null && condition.HasAnyClause
        ? $"Requires {condition.logic} condition"
        : "Requirement missing condition";

    [InfoBox("Requirements block skill use when false. Put cast gates here; use Conditional Outcomes only for extra effects after a successful cast.", InfoMessageType.Info)]
    [LabelText("Type")]
    public SkillRequirementType type = SkillRequirementType.Condition;

    [LabelText("Failure")]
    public string failureText = "Requirement not met.";

    [LabelText("Tooltip Text")]
    [TextArea(1, 3)]
    public string tooltipText;

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
        ? repeatWholeSkill
            ? $"When {condition.logic} condition is met, repeat the whole skill {Mathf.Max(2, totalExecutions)} times"
            : $"When {condition.logic} condition is met"
        : "When condition is missing";

    [InfoBox("Conditional Outcomes do not block skill use. They only add the effects below when their condition is true. Use Requirements to stop a skill from being cast.", InfoMessageType.Info)]
    [FoldoutGroup("Display", expanded: true)]
    [LabelText("Tooltip Text")]
    [TextArea(1, 3)]
    public string tooltipText;

    [FoldoutGroup("When", expanded: true)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    [FoldoutGroup("Then", expanded: true)]
    [ToggleLeft]
    [LabelText("Repeat Whole Skill")]
    [InfoBox("When this condition is true, Total Executions multiplies the base damage Hit Count for this cast.", InfoMessageType.Info)]
    public bool repeatWholeSkill;

    [FoldoutGroup("Then", expanded: true)]
    [ShowIf(nameof(repeatWholeSkill))]
    [MinValue(2)]
    [LabelText("Total Executions")]
    public int totalExecutions = 2;

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
    [InfoBox("Requirements block skill use. Add dice/resource/target gates here when the skill must not cast unless the condition is true.", InfoMessageType.Info)]
    [InfoBox("None", InfoMessageType.None, nameof(HasNoRequirements))]
    [HideLabel]
    [ListDrawerSettings(Expanded = false, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<SkillRequirementData> requirements = new List<SkillRequirementData>();

    [BoxGroup("Base Effects")]
    [InfoBox("Add the effects that always run when the skill casts.", InfoMessageType.Info, nameof(HasNoBaseEffects))]
    [HideLabel]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<SkillEffectData> baseEffects = new List<SkillEffectData>();

    [BoxGroup("Conditional Outcomes")]
    [InfoBox("Conditional Outcomes are bonus/follow-up effects. They do not stop Base Effects from casting; put cast gates in Requirements.", InfoMessageType.Info)]
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

