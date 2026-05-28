using System.Collections.Generic;

public partial class SkillDamageSO
{
    private void OnEnable()
    {
        SeedFireSlashGameplayDataIfEmpty();
        SeedConsumeBurnGameplayDataIfEmpty();
        SeedMigratedFireGameplayDataIfEmpty();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = name;
        }

        SeedFireSlashGameplayDataIfEmpty();
        SeedConsumeBurnGameplayDataIfEmpty();
        SeedMigratedFireGameplayDataIfEmpty();

        if (!exactConditionMigrated)
        {
            MigrateLegacyExactCondition();
            exactConditionMigrated = true;
        }

        if (exactValueX <= 0 && conditionPresetValue > 0)
        {
            exactValueX = conditionPresetValue;
        }

        if (string.IsNullOrWhiteSpace(exactValuePattern))
        {
            exactValuePattern = "1-2-3";
        }

        if (coreAction == CoreAction.BasicGuard)
        {
            kind = SkillKind.Guard;
            target = SkillTargetRule.Self;
        }
        else if (coreAction == CoreAction.BasicStrike)
        {
            if (kind != SkillKind.Attack)
            {
                kind = SkillKind.Attack;
            }

            if (group != DamageGroup.Strike)
            {
                group = DamageGroup.Strike;
            }
        }

        if (kind == SkillKind.Guard)
        {
            target = SkillTargetRule.Self;
        }

        hitAllEnemies = target == SkillTargetRule.RowEnemies || target == SkillTargetRule.AllEnemies;
        hitAllAllies = target == SkillTargetRule.RowAllies || target == SkillTargetRule.AllAllies;

        if (kind == SkillKind.Guard)
        {
            hitAllEnemies = false;
            hitAllAllies = false;
        }

        if (conditionalOutcome != null &&
            conditionalOutcome.enabled &&
            conditionalOutcome.type == ConditionalOutcomeType.ApplyBurn &&
            element != ElementTag.Fire)
        {
            conditionalOutcome.type = ConditionalOutcomeType.None;
        }

        if (baseDamageValueMode == BaseEffectValueMode.X)
        {
            dieMultiplier = 0f;
        }

        useSystemConditionPreset = conditionEditorMode == ConditionEditorMode.Builder;

        if (hasCondition && useSystemConditionPreset)
        {
            ApplySystemConditionPreset();
        }
    }

    private void MigrateLegacyExactCondition()
    {
        if (standardConditionFamily != SkillConditionFamily.ExactValue || condition == null || condition.clauses == null || condition.clauses.Count == 0)
        {
            return;
        }

        bool hasHighestEquals = false;
        bool hasLowestEquals = false;
        bool hasAnyBaseEquals = false;
        int firstEqualsValue = 0;

        for (int i = 0; i < condition.clauses.Count; i++)
        {
            SkillConditionClause clause = condition.clauses[i];
            if (clause == null || clause.comparison != SkillConditionComparison.Equals)
            {
                continue;
            }

            int rawReference = (int)clause.reference;

            if (!hasHighestEquals && (clause.reference == SkillConditionReference.HighestBaseValueInGroup || rawReference == 1))
            {
                hasHighestEquals = true;
                firstEqualsValue = clause.value;
            }

            if (!hasLowestEquals && (clause.reference == SkillConditionReference.LowestBaseValueInGroup || rawReference == 2))
            {
                hasLowestEquals = true;
                if (firstEqualsValue <= 0)
                {
                    firstEqualsValue = clause.value;
                }
            }

            if (!hasAnyBaseEquals && clause.reference == SkillConditionReference.AnyBaseValue)
            {
                hasAnyBaseEquals = true;
                if (string.IsNullOrWhiteSpace(exactValuePattern) || exactValuePattern == "1-2-3")
                {
                    exactValuePattern = clause.value.ToString();
                }
            }
        }

        if (hasHighestEquals && hasLowestEquals)
        {
            exactConditionMode = SkillExactConditionMode.DieEqualsX;
            if (firstEqualsValue > 0)
            {
                exactValueX = firstEqualsValue;
            }
        }
        else if (hasAnyBaseEquals)
        {
            exactConditionMode = SkillExactConditionMode.GroupContainsPattern;
        }
    }

    private void ApplySystemConditionPreset()
    {
        if (condition == null)
        {
            condition = new SkillConditionData();
        }

        condition.scope = SkillConditionScope.SlotBound;
        condition.logic = SkillConditionLogic.All;
        if (condition.clauses == null)
        {
            condition.clauses = new List<SkillConditionClause>();
        }
        else
        {
            condition.clauses.Clear();
        }

        ApplyStandardConditionPreset();
    }

    private bool CurrentPresetNeedsValue()
    {
        switch (standardConditionFamily)
        {
            case SkillConditionFamily.Resource:
            case SkillConditionFamily.BoardState:
                return true;
            default:
                return false;
        }
    }

    private void ApplyStandardConditionPreset()
    {
        switch (standardConditionFamily)
        {
            case SkillConditionFamily.DiceParity:
                switch (diceParityConditionPreset)
                {
                    case DiceParityConditionPreset.Even:
                        AddConditionClause(SkillConditionReference.AllBaseValuesEven, SkillConditionComparison.IsTrue);
                        break;
                    case DiceParityConditionPreset.Odd:
                        AddConditionClause(SkillConditionReference.AllBaseValuesOdd, SkillConditionComparison.IsTrue);
                        break;
                }
                break;

            case SkillConditionFamily.CritFail:
                switch (critFailConditionPreset)
                {
                    case CritFailConditionPreset.Crit:
                        AddConditionClause(SkillConditionReference.AllDiceCrit, SkillConditionComparison.IsTrue);
                        break;
                    case CritFailConditionPreset.Fail:
                        AddConditionClause(SkillConditionReference.AllDiceFail, SkillConditionComparison.IsTrue);
                        break;
                }
                break;

            case SkillConditionFamily.ExactValue:
                switch (exactConditionMode)
                {
                    case SkillExactConditionMode.DieEqualsX:
                    case SkillExactConditionMode.RandomExactNumberOwned:
                    case SkillExactConditionMode.RandomExactNumberRandom:
                        condition.logic = SkillConditionLogic.All;
                        AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                        AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                        break;
                    case SkillExactConditionMode.GroupContainsPattern:
                        AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                        break;
                }
                break;

            case SkillConditionFamily.Resource:
                switch (resourceConditionPreset)
                {
                    case ResourceConditionPreset.CurrentFocusGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                    case ResourceConditionPreset.PlayerGuardGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.CurrentGuard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                    case ResourceConditionPreset.TargetGuardGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.TargetGuard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                }
                break;

            case SkillConditionFamily.LocalGroupRelation:
                switch (localGroupRelationMode)
                {
                    case LocalGroupRelationMode.SelfPosition:
                    case LocalGroupRelationMode.NeighborRelation:
                        if (localGroupRelationSide == LocalGroupRelationSide.Left)
                        {
                            AddConditionClause(SkillConditionReference.IsLeftmostAction, SkillConditionComparison.IsTrue);
                        }
                        else
                        {
                            AddConditionClause(SkillConditionReference.IsRightmostAction, SkillConditionComparison.IsTrue);
                        }
                        break;
                    case LocalGroupRelationMode.SplitRole:
                        switch (localGroupConditionPreset)
                        {
                            case LocalGroupConditionPreset.Highest:
                                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.GreaterOrEqual, 1);
                                break;
                            case LocalGroupConditionPreset.Lowest:
                                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.GreaterOrEqual, 1);
                                break;
                        }
                        break;
                }
                break;

            case SkillConditionFamily.TargetState:
                switch (targetStateConditionPreset)
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
                    case TargetStateConditionPreset.StatusHistoryTodo:
                        break;
                }
                break;

            case SkillConditionFamily.BoardState:
                condition.scope = SkillConditionScope.Global;
                switch (boardStateConditionPreset)
                {
                    case BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.AliveEnemiesCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                    case BoardStateConditionPreset.EnemiesWithStatusGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.EnemiesWithStatusCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                }
                break;
        }
    }
}
