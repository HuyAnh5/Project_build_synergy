public partial class SkillDamageSO
{
    private string BuildStandardConditionPreview()
    {
        switch (standardConditionFamily)
        {
            case SkillConditionFamily.DiceParity:
                switch (diceParityConditionPreset)
                {
                    case DiceParityConditionPreset.Even: return "C?m dice d?u ch?n";
                    case DiceParityConditionPreset.Odd: return "C?m dice d?u l?";
                }
                break;
            case SkillConditionFamily.CritFail:
                switch (critFailConditionPreset)
                {
                    case CritFailConditionPreset.Crit: return "C?m dice d?u l� Crit";
                    case CritFailConditionPreset.Fail: return "C?m dice d?u l� Fail";
                }
                break;
            case SkillConditionFamily.ExactValue:
                switch (exactConditionMode)
                {
                    case SkillExactConditionMode.DieEqualsX: return $"Die Equals X, X = {exactValueX}";
                    case SkillExactConditionMode.GroupContainsPattern: return $"Group Contains: {exactValuePattern}";
                    case SkillExactConditionMode.RandomExactNumberOwned: return "Random Exact Number Owned";
                    case SkillExactConditionMode.RandomExactNumberRandom: return "Random Exact Number Random";
                }
                break;
            case SkillConditionFamily.Resource:
                switch (resourceConditionPreset)
                {
                    case ResourceConditionPreset.CurrentFocusGreaterOrEqualN: return $"Focus hi?n t?i >= {conditionPresetValue}";
                    case ResourceConditionPreset.PlayerGuardGreaterOrEqualN: return $"Guard player >= {conditionPresetValue}";
                    case ResourceConditionPreset.TargetGuardGreaterOrEqualN: return $"Guard target >= {conditionPresetValue}";
                }
                break;
            case SkillConditionFamily.LocalGroupRelation:
                switch (localGroupRelationMode)
                {
                    case LocalGroupRelationMode.SelfPosition: return localGroupRelationSide == LocalGroupRelationSide.Left ? "Self-position: Left" : "Self-position: Right";
                    case LocalGroupRelationMode.NeighborRelation: return localGroupRelationSide == LocalGroupRelationSide.Left ? "Neighbor relation: Left" : "Neighbor relation: Right";
                    case LocalGroupRelationMode.SplitRole: return localGroupConditionPreset == LocalGroupConditionPreset.Highest ? "Split-role: Highest" : "Split-role: Lowest";
                }
                break;
            case SkillConditionFamily.TargetState:
                switch (targetStateConditionPreset)
                {
                    case TargetStateConditionPreset.TargetHasBurn: return "Target dang c� Burn";
                    case TargetStateConditionPreset.TargetHasFreeze: return "Target dang Freeze";
                    case TargetStateConditionPreset.TargetHasChilled: return "Target dang Chilled";
                    case TargetStateConditionPreset.TargetHasMark: return "Target dang c� Mark";
                    case TargetStateConditionPreset.TargetHasBleed: return "Target dang c� Bleed";
                    case TargetStateConditionPreset.TargetHasStagger: return "Target dang Stagger";
                    case TargetStateConditionPreset.StatusHistoryTodo: return "Status History (TODO - chua c� runtime logic)";
                }
                break;
            case SkillConditionFamily.BoardState:
                switch (boardStateConditionPreset)
                {
                    case BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN: return $"S? enemy c�n s?ng >= {conditionPresetValue}";
                    case BoardStateConditionPreset.EnemiesWithStatusGreaterOrEqualN: return $"S? enemy dang c� status >= {conditionPresetValue}";
                }
                break;
        }

        return "Chua c� condition chu?n";
    }
}
