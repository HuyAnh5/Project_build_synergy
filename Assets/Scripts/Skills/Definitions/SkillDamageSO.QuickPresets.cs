public partial class SkillDamageSO
{
    private void PresetMeleeStrike()
    {
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
        kind = SkillKind.Guard;
        target = SkillTargetRule.Self;
        element = ElementTag.Neutral;
        guardValueMode = BaseEffectValueMode.X;
        guardFlat = 0;
    }
}
