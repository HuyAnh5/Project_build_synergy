public partial class SkillDamageSO
{
    private void ApplyFirePreset()
    {
        switch (fireConditionPreset)
        {
            case FireConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case FireConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case FireConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case FireConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case FireConditionPreset.ExactAllBasesEqualN:
                condition.logic = SkillConditionLogic.All;
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case FireConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.EnemiesWithBurnGreaterOrEqualN:
                condition.scope = SkillConditionScope.Global;
                AddConditionClause(SkillConditionReference.EnemiesWithBurnCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case FireConditionPreset.TargetHasBurn:
                AddConditionClause(SkillConditionReference.TargetHasBurn, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyIcePreset()
    {
        switch (iceConditionPreset)
        {
            case IceConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case IceConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case IceConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case IceConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case IceConditionPreset.ExactAllBasesEqualN:
                condition.logic = SkillConditionLogic.All;
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case IceConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.TargetHasFreeze:
                AddConditionClause(SkillConditionReference.TargetHasFreeze, SkillConditionComparison.IsTrue);
                break;
            case IceConditionPreset.TargetHasChilled:
                AddConditionClause(SkillConditionReference.TargetHasChilled, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyLightningPreset()
    {
        switch (lightningConditionPreset)
        {
            case LightningConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case LightningConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case LightningConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case LightningConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case LightningConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case LightningConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.MarkedEnemiesGreaterOrEqualN:
                condition.scope = SkillConditionScope.Global;
                AddConditionClause(SkillConditionReference.MarkedEnemiesCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case LightningConditionPreset.TargetHasMark:
                AddConditionClause(SkillConditionReference.TargetHasMark, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyPhysicalPreset()
    {
        switch (physicalConditionPreset)
        {
            case PhysicalConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case PhysicalConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case PhysicalConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case PhysicalConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case PhysicalConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case PhysicalConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
        }
    }

    private void ApplyBleedPreset()
    {
        switch (bleedConditionPreset)
        {
            case BleedConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case BleedConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case BleedConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case BleedConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case BleedConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case BleedConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.TotalBleedOnBoardGreaterOrEqualN:
                condition.scope = SkillConditionScope.Global;
                AddConditionClause(SkillConditionReference.TotalBleedOnBoard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case BleedConditionPreset.TargetHasBleed:
                AddConditionClause(SkillConditionReference.TargetHasBleed, SkillConditionComparison.IsTrue);
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
