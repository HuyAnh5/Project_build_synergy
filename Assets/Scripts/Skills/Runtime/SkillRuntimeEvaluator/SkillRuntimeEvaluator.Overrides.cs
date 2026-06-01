using UnityEngine;

// Applies conditional runtime overrides from SkillDamageSO authoring data.
public static partial class SkillRuntimeEvaluator
{
    // Mutates the runtime only after the skill condition has evaluated true.
    private static void ApplyDamageOverrides(ref SkillRuntime rt, SkillDamageConditionalOverrides ov)
    {
        if (ov == null) return;

        if (ov.overrideSlotsRequired)
            rt.slotsRequired = Mathf.Clamp(ov.slotsRequired, 1, 3);

        if (ov.overrideIdentity)
            ApplyIdentityOverrides(ref rt, ov);

        if (ov.overrideCost)
        {
            rt.focusCost = Mathf.Max(0, ov.focusCost);
            rt.focusGainOnCast = ov.focusGainOnCast;
        }

        if (ov.overrideDamage)
        {
            rt.dieMultiplier = ov.dieMultiplier;
            rt.flatDamage = ov.flatDamage;
        }

        if (ov.overrideSunderBonus)
        {
            rt.sunderBonusIfTargetHasGuard = ov.sunderBonusIfTargetHasGuard;
            rt.sunderGuardDamageMultiplier = ov.sunderGuardDamageMultiplier;
        }

        if (ov.overrideGuard)
        {
            rt.guardValueMode = ov.guardValueMode;
            rt.guardFlat = ov.guardFlat;
        }

        if (ov.overrideSpecialCombat)
        {
            rt.bypassGuard = ov.bypassGuard;
            rt.clearsGuard = ov.clearsGuard;
            rt.canUseMarkMultiplier = ov.canUseMarkMultiplier;
        }

        if (ov.overrideBurnSpender)
        {
            rt.consumesBurn = ov.consumesBurn;
            rt.burnDamagePerStack = ov.burnDamagePerStack;
        }

        if (ov.overrideApplyStatus)
            ApplyStatusOverrides(ref rt, ov);

        if (ov.overrideVfx)
            rt.projectilePrefab = ov.projectilePrefab;

        ApplyOverrideSafety(ref rt);
    }

    // Applies identity, target, element, group, and range overrides.
    private static void ApplyIdentityOverrides(ref SkillRuntime rt, SkillDamageConditionalOverrides ov)
    {
        rt.kind = ov.kind;
        rt.useV2Targeting = true;
        rt.targetRuleV2 = ov.target;

        rt.group = ov.group;
        rt.element = (ElementType)(int)ov.element;
        rt.range = ov.range;

        rt.hitAllEnemies = (ov.target == SkillTargetRule.RowEnemies || ov.target == SkillTargetRule.AllEnemies);
        rt.hitAllAllies = (ov.target == SkillTargetRule.RowAllies || ov.target == SkillTargetRule.AllAllies);

        rt.target = SkillTargetRuleUtility.IsEnemySideTarget(ov.target)
            ? TargetRule.Enemy
            : TargetRule.Self;
    }

    // Applies status application overrides.
    private static void ApplyStatusOverrides(ref SkillRuntime rt, SkillDamageConditionalOverrides ov)
    {
        rt.applyBurn = ov.applyBurn;
        rt.burnAddStacks = ov.burnAddStacks;
        rt.burnRefreshTurns = ov.burnRefreshTurns;

        rt.applyMark = ov.applyMark;

        rt.applyBleed = ov.applyBleed;
        rt.bleedTurns = ov.bleedTurns;

        rt.applyFreeze = ov.applyFreeze;
        rt.freezeChance = ov.freezeChance;
    }

    // Reapplies targeting safety after overrides.
    private static void ApplyOverrideSafety(ref SkillRuntime rt)
    {
        if (rt.kind == SkillKind.Guard)
        {
            rt.targetRuleV2 = SkillTargetRule.Self;
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }
        if (rt.target == TargetRule.Self) rt.hitAllEnemies = false;
        if (rt.target == TargetRule.Enemy) rt.hitAllAllies = false;
    }
}
