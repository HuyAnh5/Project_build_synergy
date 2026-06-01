using UnityEngine;

// Builds previews for legacy SkillRuntime fields that have not moved to SkillGameplayResolver yet.
public static partial class TargetPreviewBuilder
{
    // Applies legacy attack damage, status changes, and self rewards to one target preview.
    private static void BuildAttackPreview(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue, ref TargetPreviewData data)
    {
        SkillExecutor.AttackPreview ap = AttackPreviewCalculator.BuildAttackPreview(rt, caster, target, dieValue);
        ApplyDamageToData(target, ref data, ap.finalDamage, rt.bypassGuard, rt.clearsGuard, canBreakGuard: true, canConsumeStagger: ap.consumesStagger);

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(rt);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
        {
            ApplyEmberWeaponBurnPreview(rt, caster, ap.finalDamage, ref data);
            ApplyResolvedGameplayPreview(rt, caster, target, ref data);
            return;
        }

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        bool targetHadBurnBeforeHit = data.currentBurn > 0;
        bool targetHadMarkBeforeHit = data.currentMarked;
        bool targetWasFrozenOrChilled = target != null &&
                                        target.status != null &&
                                        (target.status.frozen || target.status.chilledTurns > 0);

        ApplyLegacyFirePreview(rt, caster, dieValue, targetHadBurnBeforeHit, ps, ref data);

        if (rt.applyMark)
            data.previewMarkedAfter = true;
        if (AttackPreviewCalculator.CanUseMarkPayoff(rt, target) && rt.element != ElementType.Lightning)
            data.previewMarkedAfter = false;

        if (rt.applyBleed)
            data.previewBleedAfter += GetBleedStacksToApply(rt, caster);

        if (rt.applyFreeze)
        {
            bool canFreeze = target.status == null || (!target.status.frozen && target.status.chilledTurns <= 0);
            if (canFreeze)
                data.previewFrozenAfter = true;
        }

        if (rt.element == ElementType.Ice && rt.gainIceRewardOnFrozenOrChilledHit && targetWasFrozenOrChilled)
            AddLegacyIceRewardPreview(rt, ps, ref data);

        if (rt.conditionMet && rt.conditionalOutcomeEnabled && rt.conditionalOutcomeType == ConditionalOutcomeType.GainGuard)
            data.selfGuardGain += GetConditionalGuardValue(rt, dieValue);

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.BiteTheDust))
            data.selfHealGain += Mathf.FloorToInt(Mathf.Max(0, data.currentBurn) / 5f) * 3;

        if (rt.fireGainGuardFromHighestBase)
        {
            int guardIndex = SkillBehaviorRuntimeUtility.GetHighestBaseValueIndex(rt);
            int guard = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, guardIndex);
            data.selfGuardGain += Mathf.Max(0, guard);
        }
    }

    // Applies legacy Fire Burn consume/apply/reapply preview rules.
    private static void ApplyLegacyFirePreview(
        SkillRuntime rt,
        CombatActor caster,
        int dieValue,
        bool targetHadBurnBeforeHit,
        PassiveSystem ps,
        ref TargetPreviewData data)
    {
        if (rt.consumesBurn)
            data.previewBurnAfter = 0;

        if (rt.applyBurn)
            data.previewBurnAfter += GetBurnStacksToApply(rt, dieValue, caster);

        if (rt.conditionMet && rt.conditionalOutcomeEnabled && rt.conditionalOutcomeType == ConditionalOutcomeType.ApplyBurn)
            data.previewBurnAfter += GetConditionalBurnStacks(rt, dieValue, caster);

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Hellfire) && targetHadBurnBeforeHit)
        {
            int sevensRolled = SkillBehaviorRuntimeUtility.CountBaseValuesEqual(rt, 7);
            if (sevensRolled > 0)
            {
                int reapplyBurn = (7 * sevensRolled) + (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
                data.previewBurnAfter += reapplyBurn;
            }
        }
        else if (rt.element == ElementType.Fire && rt.fireReapplyBurnPerExactBase)
        {
            bool canReapply = !rt.fireRequireBurnBeforeHitForReapply || targetHadBurnBeforeHit;
            if (canReapply)
                AddExactBaseFireReapplyPreview(rt, ps, ref data);
        }
    }

    // Adds the Fire exact-base reapply preview when matching dice are present.
    private static void AddExactBaseFireReapplyPreview(SkillRuntime rt, PassiveSystem ps, ref TargetPreviewData data)
    {
        int matchCount = SkillBehaviorRuntimeUtility.CountBaseValuesEqual(rt, rt.fireExactBaseForReapply);
        if (matchCount <= 0)
            return;

        int reapplyBurn = (Mathf.Max(0, rt.fireBurnPerExactMatch) * matchCount) +
                          (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
        data.previewBurnAfter += reapplyBurn;
    }

    // Adds Ice reward preview for attacks into Frozen or Chilled targets.
    private static void AddLegacyIceRewardPreview(SkillRuntime rt, PassiveSystem ps, ref TargetPreviewData data)
    {
        int guardReward = Mathf.Max(0, rt.iceRewardGuard);
        int focusReward = Mathf.Max(0, rt.iceRewardFocus);
        if (ps != null && focusReward > 0)
            focusReward += ps.GetFreezeBreakFocusBonusAdd();

        data.selfGuardGain += guardReward;
        data.selfFocusGain += Mathf.Max(0, focusReward);
    }

    // Adds Ember Weapon's Basic Attack Burn preview when the caster buff is active.
    private static void ApplyEmberWeaponBurnPreview(SkillRuntime rt, CombatActor caster, int finalDamage, ref TargetPreviewData data)
    {
        if (rt == null || rt.coreAction != CoreAction.BasicStrike || caster == null || caster.status == null)
            return;
        if (caster.status.emberWeaponTurns <= 0 || !caster.status.emberWeaponBurnEqualsDamage)
            return;
        if (caster.status.emberWeaponBurnOnCritOnly && !rt.localCritAny)
            return;

        int emberBurn = Mathf.Max(0, finalDamage);
        PassiveSystem passiveSystem = caster.GetComponent<PassiveSystem>();
        emberBurn += passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
        if (emberBurn > 0)
            data.previewBurnAfter += emberBurn;
    }

    // Resolves Burn stacks from legacy runtime flags and passives.
    private static int GetBurnStacksToApply(SkillRuntime rt, int dieValue, CombatActor caster)
    {
        if (rt == null || !rt.applyBurn)
            return 0;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.FireSlash))
            return rt.conditionMet ? Mathf.Max(0, rt.burnAddStacks) : 0;

        int burn = SkillOutputValueUtility.ResolveStatusStacks(
            rt.burnAddStacks,
            rt,
            rt.baseBurnValueMode,
            dieValue,
            rt.fireApplyBurnFromResolvedValue);

        if (rt.fireGrantBonusBurnOnOddBase &&
            SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int fireBaseValue) &&
            (fireBaseValue % 2) != 0)
        {
            burn += Mathf.Max(0, rt.fireOddBaseBonusBurn);
        }

        burn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
        return Mathf.Max(0, burn);
    }

    // Resolves conditional Burn stacks from legacy condition outcome fields.
    private static int GetConditionalBurnStacks(SkillRuntime rt, int dieValue, CombatActor caster)
    {
        if (rt == null || !rt.conditionMet || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType != ConditionalOutcomeType.ApplyBurn)
            return 0;

        int burn = SkillOutputValueUtility.ResolveConditionalStatusStacks(
            Mathf.Max(0, rt.conditionalOutcomeFlatValue),
            rt,
            rt.conditionalOutcomeValueMode,
            dieValue);

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        burn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
        return Mathf.Max(0, burn);
    }

    // Resolves conditional Guard from legacy condition outcome fields.
    private static int GetConditionalGuardValue(SkillRuntime rt, int dieValue)
    {
        if (rt == null || !rt.conditionMet || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType != ConditionalOutcomeType.GainGuard)
            return 0;

        return rt.conditionalOutcomeValueMode == ConditionalOutcomeValueMode.X
            ? SkillOutputValueUtility.ResolveXValue(dieValue, rt)
            : SkillOutputValueUtility.AddActionAddedValue(Mathf.Max(0, rt.conditionalOutcomeFlatValue), rt);
    }

    // Resolves Bleed stacks from legacy runtime fields and passives.
    private static int GetBleedStacksToApply(SkillRuntime rt, CombatActor caster)
    {
        if (rt == null || !rt.applyBleed)
            return 0;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        int bleed = SkillOutputValueUtility.ResolveFlatStatusStacks(rt.bleedTurns);
        bleed += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0;
        return Mathf.Max(0, bleed);
    }
}
