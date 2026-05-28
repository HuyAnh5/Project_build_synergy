public partial class SkillDamageSO
{
    private void PresetBasicStrike()
    {
        PresetMeleeStrike();
        coreAction = CoreAction.BasicStrike;
    }

    private void PresetBasicGuard()
    {
        PresetGuard();
        coreAction = CoreAction.BasicGuard;
    }

    private void PresetMeleeStrike()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Strike;
        element = ElementTag.Physical;
        range = RangeType.Melee;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;

        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    private void PresetRangedStrike()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Strike;
        element = ElementTag.Physical;
        range = RangeType.Ranged;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;

        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    private void PresetSunder()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Sunder;
        element = ElementTag.Physical;
        range = RangeType.Melee;

        bypassGuard = true;
        clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;

        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    private void PresetEffectApplier()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Effect;
        element = ElementTag.Physical;
        range = RangeType.Ranged;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = false;
        consumesBurn = false;
    }

    private void PresetGuard()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Guard;
        target = SkillTargetRule.Self;
        element = ElementTag.Neutral;
        guardValueMode = BaseEffectValueMode.X;
        guardFlat = 0;
    }
}
