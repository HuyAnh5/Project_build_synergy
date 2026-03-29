// SkillPassiveSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Passive effect runtime currently kept intentionally narrow.
/// Content depth should come from named passives in spec, not from generic %stat packages.
/// </summary>
public enum PassiveEffectId
{
    FocusBonusOnTurnStart
}

[Serializable, InlineProperty]
public class PassiveEffectEntry
{
    [BoxGroup("Effect")]
    [LabelText("Effect")]
    public PassiveEffectId id = PassiveEffectId.FocusBonusOnTurnStart;

    [BoxGroup("Effect")]
    [LabelText("+Focus")]
    public int valueI = 1;

    [BoxGroup("Condition Meta")]
    [LabelText("Primary Axis")]
    public PassiveConditionAxis primaryAxis = PassiveConditionAxis.Resource;

    [BoxGroup("Condition")]
    [ToggleLeft]
    [LabelText("Has Condition")]
    public bool hasCondition = false;

    [BoxGroup("Condition")]
    [ShowIf(nameof(hasCondition))]
    [EnumToggleButtons]
    [LabelText("Family")]
    public PassiveEffectConditionFamily conditionFamily = PassiveEffectConditionFamily.Resource;

    [BoxGroup("Condition")]
    [ShowIf("@hasCondition && conditionFamily == PassiveEffectConditionFamily.Parity")]
    [EnumToggleButtons]
    [LabelText("Option")]
    public DiceParityConditionPreset parityPreset = DiceParityConditionPreset.Even;

    [BoxGroup("Condition")]
    [ShowIf("@hasCondition && conditionFamily == PassiveEffectConditionFamily.CritFail")]
    [EnumToggleButtons]
    [LabelText("Option")]
    public CritFailConditionPreset critFailPreset = CritFailConditionPreset.Crit;

    [BoxGroup("Condition")]
    [ShowIf("@hasCondition && conditionFamily == PassiveEffectConditionFamily.ExactValue")]
    [EnumToggleButtons]
    [LabelText("Option")]
    public ExactValueConditionPreset exactValuePreset = ExactValueConditionPreset.DieEqualsX;

    [BoxGroup("Condition")]
    [ShowIf("@hasCondition && conditionFamily == PassiveEffectConditionFamily.Resource")]
    [EnumToggleButtons]
    [LabelText("Option")]
    public ResourceConditionPreset resourcePreset = ResourceConditionPreset.CurrentFocusGreaterOrEqualN;

    [BoxGroup("Condition")]
    [ShowIf("@hasCondition && conditionFamily == PassiveEffectConditionFamily.TargetState")]
    [EnumToggleButtons]
    [LabelText("Option")]
    public TargetStateConditionPreset targetStatePreset = TargetStateConditionPreset.TargetHasBurn;

    [BoxGroup("Condition")]
    [ShowIf("@hasCondition && conditionFamily == PassiveEffectConditionFamily.BoardState")]
    [EnumToggleButtons]
    [LabelText("Option")]
    public BoardStateConditionPreset boardStatePreset = BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN;

    [BoxGroup("Condition")]
    [ShowIf("@hasCondition && CurrentPresetNeedsValue()")]
    [LabelText("Preset N")]
    public int conditionPresetValue = 1;

    [BoxGroup("Condition")]
    [ShowIf(nameof(hasCondition))]
    [DisplayAsString(false)]
    [LabelText("Summary")]
    public string conditionPreviewText => BuildConditionPreview();

    [BoxGroup("Condition")]
    [ShowIf(nameof(hasCondition))]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    public void ApplyDefaults()
    {
        if (valueI == 0)
            valueI = 1;

        if (primaryAxis == PassiveConditionAxis.None)
            primaryAxis = PassiveConditionAxis.Resource;

        if (conditionPresetValue == 0)
            conditionPresetValue = 1;

        if (condition == null)
            condition = new SkillConditionData();
    }

    public void RebuildConditionFromPreset()
    {
        if (condition == null)
            condition = new SkillConditionData();

        condition.scope = SkillConditionScope.Global;
        condition.logic = SkillConditionLogic.All;
        if (condition.clauses == null)
            condition.clauses = new List<SkillConditionClause>();
        else
            condition.clauses.Clear();

        if (!hasCondition)
            return;

        switch (conditionFamily)
        {
            case PassiveEffectConditionFamily.Parity:
                ApplyParityPreset();
                break;
            case PassiveEffectConditionFamily.CritFail:
                ApplyCritFailPreset();
                break;
            case PassiveEffectConditionFamily.ExactValue:
                ApplyExactValuePreset();
                break;
            case PassiveEffectConditionFamily.Resource:
                ApplyResourcePreset();
                break;
            case PassiveEffectConditionFamily.TargetState:
                ApplyTargetStatePreset();
                break;
            case PassiveEffectConditionFamily.BoardState:
                ApplyBoardStatePreset();
                break;
        }
    }

    private bool CurrentPresetNeedsValue()
    {
        switch (conditionFamily)
        {
            case PassiveEffectConditionFamily.ExactValue:
            case PassiveEffectConditionFamily.Resource:
            case PassiveEffectConditionFamily.BoardState:
                return true;
            default:
                return false;
        }
    }

    private string BuildConditionPreview()
    {
        switch (conditionFamily)
        {
            case PassiveEffectConditionFamily.Parity:
                switch (parityPreset)
                {
                    case DiceParityConditionPreset.AnyBaseOdd: return "Có die Base lẻ";
                    case DiceParityConditionPreset.AnyBaseEven: return "Có die Base chẵn";
                    case DiceParityConditionPreset.AllBasesOdd: return "Tất cả dice đều Base lẻ";
                    case DiceParityConditionPreset.AllBasesEven: return "Tất cả dice đều Base chẵn";
                    case DiceParityConditionPreset.MixedParity: return "Roll có cả chẵn lẫn lẻ";
                    case DiceParityConditionPreset.HighestBaseOdd: return "Die Base cao nhất là lẻ";
                    case DiceParityConditionPreset.HighestBaseEven: return "Die Base cao nhất là chẵn";
                    case DiceParityConditionPreset.LowestBaseOdd: return "Die Base thấp nhất là lẻ";
                    case DiceParityConditionPreset.LowestBaseEven: return "Die Base thấp nhất là chẵn";
                    case DiceParityConditionPreset.FirstBaseOdd: return "Die đầu là lẻ";
                    case DiceParityConditionPreset.FirstBaseEven: return "Die đầu là chẵn";
                    case DiceParityConditionPreset.MiddleBaseOdd: return "Die giữa là lẻ";
                    case DiceParityConditionPreset.MiddleBaseEven: return "Die giữa là chẵn";
                    case DiceParityConditionPreset.LastBaseOdd: return "Die cuối là lẻ";
                    case DiceParityConditionPreset.LastBaseEven: return "Die cuối là chẵn";
                }
                break;

            case PassiveEffectConditionFamily.CritFail:
                switch (critFailPreset)
                {
                    case CritFailConditionPreset.AnyDieCrit: return "Có Crit trong cụm";
                    case CritFailConditionPreset.AnyDieFail: return "Có Fail trong cụm";
                    case CritFailConditionPreset.FirstDieCrit: return "Die đầu là Crit";
                    case CritFailConditionPreset.FirstDieFail: return "Die đầu là Fail";
                    case CritFailConditionPreset.LastDieCrit: return "Die cuối là Crit";
                    case CritFailConditionPreset.LastDieFail: return "Die cuối là Fail";
                }
                break;

            case PassiveEffectConditionFamily.ExactValue:
                switch (exactValuePreset)
                {
                    case ExactValueConditionPreset.AnyBaseEqualsN: return $"Có die Base = {conditionPresetValue}";
                    case ExactValueConditionPreset.HighestBaseEqualsN: return $"Die cao nhất = {conditionPresetValue}";
                    case ExactValueConditionPreset.LowestBaseEqualsN: return $"Die thấp nhất = {conditionPresetValue}";
                    case ExactValueConditionPreset.AllBasesEqualN: return $"Tất cả dice = {conditionPresetValue}";
                    case ExactValueConditionPreset.FirstBaseEqualsN: return $"Die đầu = {conditionPresetValue}";
                    case ExactValueConditionPreset.MiddleBaseEqualsN: return $"Die giữa = {conditionPresetValue}";
                    case ExactValueConditionPreset.LastBaseEqualsN: return $"Die cuối = {conditionPresetValue}";
                }
                break;

            case PassiveEffectConditionFamily.Resource:
                return resourcePreset == ResourceConditionPreset.CurrentGuardGreaterOrEqualN
                    ? $"Guard hiện tại >= {conditionPresetValue}"
                    : $"Focus hiện tại >= {conditionPresetValue}";

            case PassiveEffectConditionFamily.TargetState:
                switch (targetStatePreset)
                {
                    case TargetStateConditionPreset.TargetHasBurn: return "Target có Burn";
                    case TargetStateConditionPreset.TargetHasFreeze: return "Target đang Freeze";
                    case TargetStateConditionPreset.TargetHasChilled: return "Target đang Chilled";
                    case TargetStateConditionPreset.TargetHasMark: return "Target có Mark";
                    case TargetStateConditionPreset.TargetHasBleed: return "Target có Bleed";
                    case TargetStateConditionPreset.TargetHasStagger: return "Target đang Stagger";
                }
                break;

            case PassiveEffectConditionFamily.BoardState:
                switch (boardStatePreset)
                {
                    case BoardStateConditionPreset.EnemiesWithBurnGreaterOrEqualN: return $"Enemy có Burn >= {conditionPresetValue}";
                    case BoardStateConditionPreset.MarkedEnemiesGreaterOrEqualN: return $"Enemy có Mark >= {conditionPresetValue}";
                    case BoardStateConditionPreset.TotalBleedOnBoardGreaterOrEqualN: return $"Total Bleed trên board >= {conditionPresetValue}";
                    case BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN: return $"Enemy còn sống >= {conditionPresetValue}";
                }
                break;
        }

        return "Passive condition chưa được cấu hình";
    }

    private void ApplyParityPreset()
    {
        switch (parityPreset)
        {
            case DiceParityConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case DiceParityConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case DiceParityConditionPreset.AllBasesOdd:
                AddConditionClause(SkillConditionReference.AllBaseValuesOdd, SkillConditionComparison.IsTrue);
                break;
            case DiceParityConditionPreset.AllBasesEven:
                AddConditionClause(SkillConditionReference.AllBaseValuesEven, SkillConditionComparison.IsTrue);
                break;
            case DiceParityConditionPreset.MixedParity:
                AddConditionClause(SkillConditionReference.MixedParityInGroup, SkillConditionComparison.IsTrue);
                break;
            case DiceParityConditionPreset.HighestBaseOdd:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.IsOdd);
                break;
            case DiceParityConditionPreset.HighestBaseEven:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.IsEven);
                break;
            case DiceParityConditionPreset.LowestBaseOdd:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.IsOdd);
                break;
            case DiceParityConditionPreset.LowestBaseEven:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.IsEven);
                break;
            case DiceParityConditionPreset.FirstBaseOdd:
                AddConditionClause(SkillConditionReference.FirstBaseValueInGroup, SkillConditionComparison.IsOdd);
                break;
            case DiceParityConditionPreset.FirstBaseEven:
                AddConditionClause(SkillConditionReference.FirstBaseValueInGroup, SkillConditionComparison.IsEven);
                break;
            case DiceParityConditionPreset.MiddleBaseOdd:
                AddConditionClause(SkillConditionReference.MiddleBaseValueInGroup, SkillConditionComparison.IsOdd);
                break;
            case DiceParityConditionPreset.MiddleBaseEven:
                AddConditionClause(SkillConditionReference.MiddleBaseValueInGroup, SkillConditionComparison.IsEven);
                break;
            case DiceParityConditionPreset.LastBaseOdd:
                AddConditionClause(SkillConditionReference.LastBaseValueInGroup, SkillConditionComparison.IsOdd);
                break;
            case DiceParityConditionPreset.LastBaseEven:
                AddConditionClause(SkillConditionReference.LastBaseValueInGroup, SkillConditionComparison.IsEven);
                break;
        }
    }

    private void ApplyCritFailPreset()
    {
        switch (critFailPreset)
        {
            case CritFailConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case CritFailConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case CritFailConditionPreset.FirstDieCrit:
                AddConditionClause(SkillConditionReference.FirstDieCrit, SkillConditionComparison.IsTrue);
                break;
            case CritFailConditionPreset.FirstDieFail:
                AddConditionClause(SkillConditionReference.FirstDieFail, SkillConditionComparison.IsTrue);
                break;
            case CritFailConditionPreset.LastDieCrit:
                AddConditionClause(SkillConditionReference.LastDieCrit, SkillConditionComparison.IsTrue);
                break;
            case CritFailConditionPreset.LastDieFail:
                AddConditionClause(SkillConditionReference.LastDieFail, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyExactValuePreset()
    {
        switch (exactValuePreset)
        {
            case ExactValueConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case ExactValueConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case ExactValueConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case ExactValueConditionPreset.AllBasesEqualN:
                condition.logic = SkillConditionLogic.All;
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case ExactValueConditionPreset.FirstBaseEqualsN:
                AddConditionClause(SkillConditionReference.FirstBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case ExactValueConditionPreset.MiddleBaseEqualsN:
                AddConditionClause(SkillConditionReference.MiddleBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case ExactValueConditionPreset.LastBaseEqualsN:
                AddConditionClause(SkillConditionReference.LastBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
        }
    }

    private void ApplyResourcePreset()
    {
        switch (resourcePreset)
        {
            case ResourceConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case ResourceConditionPreset.CurrentGuardGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentGuard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
        }
    }

    private void ApplyTargetStatePreset()
    {
        switch (targetStatePreset)
        {
            case TargetStateConditionPreset.TargetHasBurn:
                AddConditionClause(SkillConditionReference.TargetHasBurn, SkillConditionComparison.IsTrue);
                break;
            case TargetStateConditionPreset.TargetHasFreeze:
                AddConditionClause(SkillConditionReference.TargetHasFreeze, SkillConditionComparison.IsTrue);
                break;
            case TargetStateConditionPreset.TargetHasChilled:
                AddConditionClause(SkillConditionReference.TargetHasChilled, SkillConditionComparison.IsTrue);
                break;
            case TargetStateConditionPreset.TargetHasMark:
                AddConditionClause(SkillConditionReference.TargetHasMark, SkillConditionComparison.IsTrue);
                break;
            case TargetStateConditionPreset.TargetHasBleed:
                AddConditionClause(SkillConditionReference.TargetHasBleed, SkillConditionComparison.IsTrue);
                break;
            case TargetStateConditionPreset.TargetHasStagger:
                AddConditionClause(SkillConditionReference.TargetHasStagger, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyBoardStatePreset()
    {
        switch (boardStatePreset)
        {
            case BoardStateConditionPreset.EnemiesWithBurnGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.EnemiesWithBurnCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case BoardStateConditionPreset.MarkedEnemiesGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.MarkedEnemiesCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case BoardStateConditionPreset.TotalBleedOnBoardGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.TotalBleedOnBoard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.AliveEnemiesCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
        }
    }

    private void AddConditionClause(SkillConditionReference reference, SkillConditionComparison comparison, int value = 0)
    {
        condition.clauses.Add(new SkillConditionClause
        {
            reference = reference,
            comparison = comparison,
            value = value
        });
    }
}

[CreateAssetMenu(menuName = "Game/Skill/Passive", fileName = "SkillPassive_")]
public class SkillPassiveSO : ScriptableObject
{
    [TabGroup("Tabs", "Overview")]
    [HorizontalGroup("Top", Width = 70)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [TabGroup("Tabs", "Overview")]
    [VerticalGroup("Top/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [TabGroup("Tabs", "Overview")]
    [VerticalGroup("Top/Info")]
    [TextArea(2, 4)]
    public string description;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Metadata")]
    [InlineProperty]
    public SkillSpecMetadata spec = new SkillSpecMetadata();

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Metadata")]
    [LabelText("Behavior Id")]
    public PassiveBehaviorId behaviorId = PassiveBehaviorId.None;

    // ---- Quick Add (Foldout) ----
    [TabGroup("Tabs", "Effects")]
    [FoldoutGroup("Tabs/Effects/Quick Add", expanded: false)]
    [ButtonGroup("Tabs/Effects/Quick Add/Row0")]
    [Button("Turn Start +1 Focus")]
    private void AddTurnStartFocus() => AddEffect(e => { e.id = PassiveEffectId.FocusBonusOnTurnStart; e.valueI = 1; });

    // ---- Effects ----
    [TabGroup("Tabs", "Effects")]
    [TitleGroup("Tabs/Effects/Effect List", "Effects", Alignment = TitleAlignments.Centered)]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    public List<PassiveEffectEntry> effects = new();

    private void AddEffect(Action<PassiveEffectEntry> init)
    {
        if (effects == null) effects = new List<PassiveEffectEntry>();
        var e = new PassiveEffectEntry();
        e.ApplyDefaults();
        init?.Invoke(e);
        e.RebuildConditionFromPreset();
        effects.Add(e);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        if (effects == null) effects = new List<PassiveEffectEntry>();
        for (int i = 0; i < effects.Count; i++)
        {
            PassiveEffectEntry effect = effects[i];
            if (effect == null)
                continue;

            if (effect.condition == null)
                effect.condition = new SkillConditionData();
            effect.RebuildConditionFromPreset();
        }
    }
}
