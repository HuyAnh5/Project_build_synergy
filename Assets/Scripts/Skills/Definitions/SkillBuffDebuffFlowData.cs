using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum BuffDebuffFlowEffectType
{
    RerollUsedDice = 0,
    TransformUsedDiceHigh,
    TransformUsedDiceLow,
    RepeatFirstSkillNextTurn,
    NextSkillAddValue,
    EmberWeapon,
    GainAP = 10,
    GainGuard = 11,
    Heal = 12,
    ApplyStatus = 13
}

public enum BuffDebuffFlowTarget
{
    Self,
    UsedDice,
    SelectedTarget
}

[Serializable, InlineProperty]
public class BuffDebuffFlowEffectData
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-20)]
    public string Summary => BuildSummary();

    [LabelText("Type")]
    public BuffDebuffFlowEffectType type = BuffDebuffFlowEffectType.RerollUsedDice;

    [LabelText("Target")]
    [ShowIf(nameof(ShowsTarget))]
    public BuffDebuffFlowTarget target = BuffDebuffFlowTarget.Self;

    [LabelText("Amount")]
    [ShowIf(nameof(UsesAmount))]
    [MinValue(0)]
    public int amount = 1;

    [LabelText("Status")]
    [ShowIf(nameof(UsesStatus))]
    public StatusKind status = StatusKind.Burn;

    [LabelText("Turns")]
    [ShowIf(nameof(UsesDuration))]
    [MinValue(1)]
    public int durationTurns = 1;

    [BoxGroup("Ember Weapon")]
    [ShowIf(nameof(IsEmberWeapon))]
    [LabelText("Burn = Final Damage")]
    [ToggleLeft]
    public bool emberBurnEqualsFinalDamage = true;

    [BoxGroup("Ember Weapon")]
    [ShowIf(nameof(IsEmberWeapon))]
    [LabelText("Burn Only On Crit")]
    [ToggleLeft]
    public bool emberBurnOnCritOnly = true;

    [BoxGroup("Ember Weapon")]
    [ShowIf(nameof(IsEmberWeapon))]
    [LabelText("Burn Turns")]
    [MinValue(1)]
    public int emberBurnTurns = 3;

    [ToggleLeft]
    [LabelText("Show In Preview")]
    public bool previewable = true;

    public void NormalizeLegacyType()
    {
        int rawValue = (int)type;
        if (rawValue == 6)
            type = BuffDebuffFlowEffectType.RerollUsedDice;
    }

    private bool ShowsTarget()
        => type == BuffDebuffFlowEffectType.RerollUsedDice ||
           type == BuffDebuffFlowEffectType.TransformUsedDiceHigh ||
           type == BuffDebuffFlowEffectType.TransformUsedDiceLow ||
           type == BuffDebuffFlowEffectType.GainGuard ||
           type == BuffDebuffFlowEffectType.Heal ||
           type == BuffDebuffFlowEffectType.ApplyStatus;

    private bool UsesAmount()
        => type == BuffDebuffFlowEffectType.RepeatFirstSkillNextTurn ||
           type == BuffDebuffFlowEffectType.NextSkillAddValue ||
           type == BuffDebuffFlowEffectType.EmberWeapon ||
           type == BuffDebuffFlowEffectType.GainAP ||
           type == BuffDebuffFlowEffectType.GainGuard ||
           type == BuffDebuffFlowEffectType.Heal ||
           type == BuffDebuffFlowEffectType.ApplyStatus;

    private bool UsesDuration()
        => type == BuffDebuffFlowEffectType.EmberWeapon ||
           (type == BuffDebuffFlowEffectType.ApplyStatus && status == StatusKind.Burn);

    private bool IsEmberWeapon()
        => type == BuffDebuffFlowEffectType.EmberWeapon;

    private bool UsesStatus()
        => type == BuffDebuffFlowEffectType.ApplyStatus;

    private string BuildSummary()
    {
        switch (type)
        {
            case BuffDebuffFlowEffectType.RerollUsedDice:
                return "Reroll used dice after cast";
            case BuffDebuffFlowEffectType.TransformUsedDiceHigh:
                return "Reroll used dice to value >= current";
            case BuffDebuffFlowEffectType.TransformUsedDiceLow:
                return "Reroll used dice to value <= current";
            case BuffDebuffFlowEffectType.RepeatFirstSkillNextTurn:
                return $"First skill next turn casts +{Mathf.Max(1, amount)} time(s)";
            case BuffDebuffFlowEffectType.NextSkillAddValue:
                return $"Next skill gains +{Mathf.Max(0, amount)} added value";
            case BuffDebuffFlowEffectType.EmberWeapon:
                return $"Melee skills +{Mathf.Max(0, amount)} value for {Mathf.Max(1, durationTurns)} turn(s)";
            case BuffDebuffFlowEffectType.GainAP:
                return $"Gain {Mathf.Max(0, amount)} AP";
            case BuffDebuffFlowEffectType.GainGuard:
                return $"Gain {Mathf.Max(0, amount)} Guard on {target}";
            case BuffDebuffFlowEffectType.Heal:
                return $"Heal {target} for {Mathf.Max(0, amount)}";
            case BuffDebuffFlowEffectType.ApplyStatus:
                return $"Apply {Mathf.Max(0, amount)} {status} to {target}";
            default:
                return type.ToString();
        }
    }
}

[Serializable, InlineProperty]
public class BuffDebuffFlowConditionalOutcomeData
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-20)]
    public string Summary => condition != null && condition.HasAnyClause
        ? $"When {condition.logic} condition is met"
        : "When condition is missing";

    [FoldoutGroup("Display", expanded: true)]
    [LabelText("Tooltip Text")]
    [TextArea(1, 3)]
    public string tooltipText;

    [FoldoutGroup("When", expanded: true)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    [FoldoutGroup("Then", expanded: true)]
    [HideLabel]
    [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<BuffDebuffFlowEffectData> effects = new List<BuffDebuffFlowEffectData>();
}

[Serializable, InlineProperty]
public class BuffDebuffFlowData
{
    [BoxGroup("Description")]
    [TextArea(2, 5)]
    [LabelText("Description")]
    public string descriptionTemplate;

    [BoxGroup("Requirements")]
    [InfoBox("Requirements block skill use. Keep cast gates here; effects below stay buff/debuff-specific.", InfoMessageType.Info)]
    [InfoBox("None", InfoMessageType.None, nameof(HasNoRequirements))]
    [HideLabel]
    [ListDrawerSettings(DefaultExpandedState = false, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<SkillRequirementData> requirements = new List<SkillRequirementData>();

    [BoxGroup("Effects")]
    [InfoBox("Effects that always run when the buff/debuff skill casts.", InfoMessageType.Info, nameof(HasNoBaseEffects))]
    [HideLabel]
    [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<BuffDebuffFlowEffectData> baseEffects = new List<BuffDebuffFlowEffectData>();

    [BoxGroup("Conditional Outcomes")]
    [InfoBox("Bonus/follow-up effects. They do not block the skill cast.", InfoMessageType.Info)]
    [InfoBox("None", InfoMessageType.None, nameof(HasNoConditionalOutcomes))]
    [HideLabel]
    [ListDrawerSettings(DefaultExpandedState = false, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<BuffDebuffFlowConditionalOutcomeData> conditionalOutcomes = new List<BuffDebuffFlowConditionalOutcomeData>();

    private bool HasNoRequirements() => requirements == null || requirements.Count == 0;
    private bool HasNoBaseEffects() => baseEffects == null || baseEffects.Count == 0;
    private bool HasNoConditionalOutcomes() => conditionalOutcomes == null || conditionalOutcomes.Count == 0;
}
